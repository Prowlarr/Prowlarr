using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers.Definitions.Gazelle;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Redacted : TorrentIndexerBase<RedactedSettings>
    {
        public override string Name => "Redacted";
        public override string[] IndexerUrls => new[] { "https://redacted.ch/" };
        public override string Description => "REDActed (Aka.PassTheHeadPhones) is one of the most well-known music trackers.";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        public override bool SupportsRedirect => true;
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(3);

        public Redacted(IIndexerHttpClient httpClient,
                        IEventAggregator eventAggregator,
                        IIndexerStatusService indexerStatusService,
                        IConfigService configService,
                        Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new RedactedRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new RedactedParser(Settings, Capabilities.Categories);
        }

        protected override IList<ReleaseInfo> CleanupReleases(IEnumerable<ReleaseInfo> releases, SearchCriteriaBase searchCriteria)
        {
            var cleanReleases = base.CleanupReleases(releases, searchCriteria);

            if (searchCriteria.IsRssSearch)
            {
                cleanReleases = cleanReleases.Take(50).ToList();
            }

            return cleanReleases;
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                LimitsDefault = 50,
                LimitsMax = 50,
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q, MusicSearchParam.Artist, MusicSearchParam.Album, MusicSearchParam.Year
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.PC, "Applications");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.BooksEBook, "E-Books");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.AudioAudiobook, "Audiobooks");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Other, "E-Learning Videos");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.Other, "Comedy");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.BooksComics, "Comics");

            return caps;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var request = new HttpRequestBuilder(link.AbsoluteUri)
                .SetHeader("Authorization", Settings.Apikey)
                .Build();

            byte[] fileData;

            try
            {
                var response = await _httpClient.ExecuteProxiedAsync(request, Definition);
                fileData = response.ResponseData;

                if (Settings.UseFreeleechToken == (int)RedactedUseFreeleechTokens.Preferred
                    && fileData.Length >= 1
                    && fileData[0] != 'd' // simple test for torrent vs HTML content
                    && link.Query.Contains("usetoken=1"))
                {
                    var html = Encoding.GetString(fileData);

                    if (html.Contains("You do not have any freeleech tokens left.")
                        || html.Contains("You do not have enough freeleech tokens")
                        || html.Contains("This torrent is too large.")
                        || html.Contains("You cannot use tokens here"))
                    {
                        // Try to download again without usetoken
                        request.Url = new HttpUri(link.ToString().Replace("&usetoken=1", ""));

                        response = await _httpClient.ExecuteProxiedAsync(request, Definition);
                        fileData = response.ResponseData;
                    }
                }

                _logger.Debug("Downloaded for release finished ({0} bytes from {1})", fileData.Length, link.AbsoluteUri);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.Error(ex, "Downloading file for release failed since it no longer exists ({0})", link.AbsoluteUri);
                    throw new ReleaseUnavailableException("Download failed", ex);
                }

                if (ex.Response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.Error("API Grab Limit reached for {0}", link.AbsoluteUri);
                }
                else
                {
                    _logger.Error(ex, "Downloading for release failed ({0})", link.AbsoluteUri);
                }

                throw new ReleaseDownloadException("Download failed", ex);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Downloading for release failed ({0})", link.AbsoluteUri);

                throw new ReleaseDownloadException("Download failed", ex);
            }
            catch (Exception)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Error("Download failed");
                throw;
            }

            ValidateDownloadData(fileData);

            return fileData;
        }
    }

    public class RedactedRequestGenerator : IIndexerRequestGenerator
    {
        private readonly RedactedSettings _settings;
        private readonly IndexerCapabilities _capabilities;

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public RedactedRequestGenerator(RedactedSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            if (searchCriteria.Artist.IsNotNullOrWhiteSpace() && searchCriteria.Artist != "VA")
            {
                parameters.Set("artistname", searchCriteria.Artist);
            }

            if (searchCriteria.Album.IsNotNullOrWhiteSpace())
            {
                parameters.Set("groupname", searchCriteria.Album);
            }

            if (searchCriteria.Year.HasValue)
            {
                parameters.Set("year", searchCriteria.Year.ToString());
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria, NameValueCollection parameters)
        {
            var term = searchCriteria.SanitizedSearchTerm.Trim();

            parameters.Set("action", "browse");
            parameters.Set("order_by", "time");
            parameters.Set("order_way", "desc");

            if (term.IsNotNullOrWhiteSpace())
            {
                parameters.Set("searchstr", term);
            }

            var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

            if (queryCats.Any())
            {
                queryCats.ForEach(cat => parameters.Set($"filter_cat[{cat}]", "1"));
            }

            if (_settings.FreeloadOnly)
            {
                parameters.Set("freetorrent", "4");
            }

            var searchUrl = _settings.BaseUrl.TrimEnd('/') + $"/ajax.php?{parameters.GetQueryString()}";

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);
            request.HttpRequest.Headers.Set("Authorization", _settings.Apikey);

            yield return request;
        }
    }

    public class RedactedParser : IParseIndexerResponse
    {
        private readonly RedactedSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public RedactedParser(RedactedSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = new HttpResponse<GazelleResponse>(indexerResponse.HttpResponse);
            if (jsonResponse.Resource.Status != "success" ||
                string.IsNullOrWhiteSpace(jsonResponse.Resource.Status) ||
                jsonResponse.Resource.Response == null)
            {
                return torrentInfos;
            }

            foreach (var result in jsonResponse.Resource.Response.Results)
            {
                if (result.Torrents != null)
                {
                    foreach (var torrent in result.Torrents)
                    {
                        // skip non-freeload results when freeload only is set
                        if (_settings.FreeloadOnly && !torrent.IsFreeload)
                        {
                            continue;
                        }

                        var id = torrent.TorrentId;

                        var title = GetTitle(result, torrent);
                        var infoUrl = GetInfoUrl(result.GroupId, id);

                        var release = new TorrentInfo
                        {
                            Guid = infoUrl,
                            InfoUrl = infoUrl,
                            DownloadUrl = GetDownloadUrl(id, torrent.CanUseToken && !torrent.IsFreeload),
                            Title = WebUtility.HtmlDecode(title),
                            Artist = WebUtility.HtmlDecode(result.Artist),
                            Album = WebUtility.HtmlDecode(result.GroupName),
                            Year = int.Parse(result.GroupYear),
                            Container = torrent.Encoding,
                            Codec = torrent.Format,
                            Size = long.Parse(torrent.Size),
                            Seeders = int.Parse(torrent.Seeders),
                            Peers = int.Parse(torrent.Leechers) + int.Parse(torrent.Seeders),
                            PublishDate = torrent.Time.ToUniversalTime(),
                            Scene = torrent.Scene,
                            Files = torrent.FileCount,
                            Grabs = torrent.Snatches,
                            DownloadVolumeFactor = torrent.IsFreeLeech || torrent.IsNeutralLeech || torrent.IsFreeload || torrent.IsPersonalFreeLeech ? 0 : 1,
                            UploadVolumeFactor = torrent.IsNeutralLeech || torrent.IsFreeload ? 0 : 1
                        };

                        var category = torrent.Category;
                        if (category == null || category.Contains("Select Category"))
                        {
                            release.Categories = _categories.MapTrackerCatToNewznab("1");
                        }
                        else
                        {
                            release.Categories = _categories.MapTrackerCatDescToNewznab(category);
                        }

                        torrentInfos.Add(release);
                    }
                }

                // Non-Audio files are formatted a little differently (1:1 for group and torrents)
                else
                {
                    // skip non-freeload results when freeload only is set
                    if (_settings.FreeloadOnly && !result.IsFreeload)
                    {
                        continue;
                    }

                    var id = result.TorrentId;
                    var infoUrl = GetInfoUrl(result.GroupId, id);

                    var release = new TorrentInfo
                    {
                        Guid = infoUrl,
                        Title = WebUtility.HtmlDecode(result.GroupName),
                        Size = long.Parse(result.Size),
                        DownloadUrl = GetDownloadUrl(id, result.CanUseToken && !result.IsFreeload),
                        InfoUrl = infoUrl,
                        Seeders = int.Parse(result.Seeders),
                        Peers = int.Parse(result.Leechers) + int.Parse(result.Seeders),
                        PublishDate = DateTimeOffset.FromUnixTimeSeconds(ParseUtil.CoerceLong(result.GroupTime)).UtcDateTime,
                        Files = result.FileCount,
                        Grabs = result.Snatches,
                        DownloadVolumeFactor = result.IsFreeLeech || result.IsNeutralLeech || result.IsFreeload || result.IsPersonalFreeLeech ? 0 : 1,
                        UploadVolumeFactor = result.IsNeutralLeech || result.IsFreeload ? 0 : 1
                    };

                    var category = result.Category;
                    if (category == null || category.Contains("Select Category"))
                    {
                        release.Categories = _categories.MapTrackerCatToNewznab("1");
                    }
                    else
                    {
                        release.Categories = _categories.MapTrackerCatDescToNewznab(category);
                    }

                    torrentInfos.Add(release);
                }
            }

            // order by date
            return
                torrentInfos
                    .OrderByDescending(o => o.PublishDate)
                    .ToArray();
        }

        private string GetTitle(GazelleRelease result, GazelleTorrent torrent)
        {
            var title = $"{result.Artist} - {result.GroupName} ({result.GroupYear})";

            if (result.ReleaseType.IsNotNullOrWhiteSpace() && result.ReleaseType != "Unknown")
            {
                title += " [" + result.ReleaseType + "]";
            }

            if (torrent.RemasterTitle.IsNotNullOrWhiteSpace())
            {
                title += $" [{$"{torrent.RemasterTitle} {torrent.RemasterYear}".Trim()}]";
            }

            var flags = new List<string>
            {
                $"{torrent.Format} {torrent.Encoding}",
                $"{torrent.Media}"
            };

            if (torrent.HasLog)
            {
                flags.Add("Log (" + torrent.LogScore + "%)");
            }

            if (torrent.HasCue)
            {
                flags.Add("Cue");
            }

            return $"{title} [{string.Join(" / ", flags)}]";
        }

        private string GetDownloadUrl(int torrentId, bool canUseToken)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/ajax.php")
                .AddQueryParam("action", "download")
                .AddQueryParam("id", torrentId);

            if (_settings.UseFreeleechToken is (int)RedactedUseFreeleechTokens.Preferred or (int)RedactedUseFreeleechTokens.Required && canUseToken)
            {
                url = url.AddQueryParam("usetoken", "1");
            }

            return url.FullUri;
        }

        private string GetInfoUrl(string groupId, int torrentId)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("id", groupId)
                .AddQueryParam("torrentid", torrentId);

            return url.FullUri;
        }
    }

    public class RedactedSettingsValidator : NoAuthSettingsValidator<RedactedSettings>
    {
        public RedactedSettingsValidator()
        {
            RuleFor(c => c.Apikey).NotEmpty();
        }
    }

    public class RedactedSettings : NoAuthTorrentBaseSettings
    {
        private static readonly RedactedSettingsValidator Validator = new ();

        public RedactedSettings()
        {
            Apikey = "";
            UseFreeleechToken = (int)RedactedUseFreeleechTokens.Never;
        }

        [FieldDefinition(2, Label = "API Key", Privacy = PrivacyLevel.ApiKey, HelpText = "API Key from the Site (Found in Settings => Access Settings)")]
        public string Apikey { get; set; }

        [FieldDefinition(3, Type = FieldType.Select, Label = "Use Freeleech Tokens", SelectOptions = typeof(RedactedUseFreeleechTokens), HelpText = "When to use freeleech tokens")]
        public int UseFreeleechToken { get; set; }

        [FieldDefinition(4, Label = "Freeload Only", Type = FieldType.Checkbox, Advanced = true, HelpTextWarning = "Search freeload torrents only. End date: 31 January 2024, 23:59 UTC.")]
        public bool FreeloadOnly { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    internal enum RedactedUseFreeleechTokens
    {
        [FieldOption(Label = "Never", Hint = "Do not use tokens")]
        Never = 0,

        [FieldOption(Label = "Preferred", Hint = "Use token if possible")]
        Preferred = 1,

        [FieldOption(Label = "Required", Hint = "Abort download if unable to use token")]
        Required = 2,
    }
}
