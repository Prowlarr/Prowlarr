using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Gazelle;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions;

public class BjShare : GazelleBase<BjShareSettings>
{
    public override string Name => "BJ-Share";
    public override string[] IndexerUrls => new[] { "https://bj-share.info/" };
    public override string Description => "BJ-Share is a Private PT-BR torrent site";
    public override string Language => "pt-BR";
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

    public BjShare(IIndexerHttpClient httpClient,
                   IEventAggregator eventAggregator,
                   IIndexerStatusService indexerStatusService,
                   IConfigService configService,
                   Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new BjShareRequestGenerator(Settings, Capabilities, _httpClient, _logger);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new BjShareParser(Settings, Capabilities, _logger);
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

    protected override IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            TvSearchParams = new List<TvSearchParam>
            {
                TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
            },
            MovieSearchParams = new List<MovieSearchParam>
            {
                MovieSearchParam.Q
            }
        };

        caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Filmes");
        caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "TV");
        caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.PC, "Aplicativos");
        caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.PCGames, "Jogos");
        caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.BooksComics, "Mangás");
        caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.TV, "Vídeos de TV");
        caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Other, "Outros");
        caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.TVSport, "Esportes");
        caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.BooksMagazines, "Revistas");
        caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.Books, "E-Books");
        caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.AudioAudiobook, "Audiobook");
        caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.BooksComics, "Histórias em Quadrinhos");
        caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.TV, "Stand Up Comedy");
        caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.TV, "TV/Anime");
        caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.XXXImageSet, "Fotos Adultas");
        caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.TVOther, "Desenho Animado");
        caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.TVDocumentary, "Documentários");
        caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.Other, "Cursos");
        caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.XXX, "Filmes Adultos");
        caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.XXXOther, "Jogos Adultos");
        caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.XXXOther, "Mangás Adultos");
        caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.XXXOther, "Animes Adultos");
        caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.XXXOther, "HQ Adultos");

        return caps;
    }
}

public class BjShareRequestGenerator : GazelleRequestGenerator
{
    protected override bool ImdbInTags => false;
    private readonly BjShareSettings _settings;

    public BjShareRequestGenerator(BjShareSettings settings,
                                   IndexerCapabilities capabilities,
                                   IIndexerHttpClient httpClient,
                                   Logger logger)
        : base(settings, capabilities, httpClient, logger)
    {
        _settings = settings;
    }

    protected override string GetSearchTerm(string term)
    {
        // Strip SXX/SXXEXX patterns (BJ-Share doesn't use them in search)
        var cleaned = Regex.Replace(term, @"\b[Ss]\d{2,3}([Ee]\d{2,3})?\b", "");

        // Strip (YYYY) year patterns that cause false negatives
        cleaned = Regex.Replace(cleaned, @"\s*\(\d{4}\)\s*", " ");

        // Normalize whitespace
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        return cleaned;
    }

    protected override NameValueCollection GetBasicSearchParameters(SearchCriteriaBase searchCriteria, string term)
    {
        var parameters = base.GetBasicSearchParameters(searchCriteria, term);

        if (_settings.FreeleechOnly)
        {
            parameters.Set("freetorrent", "1");
        }

        return parameters;
    }
}

public class BjShareParser : GazelleParser
{
    private readonly Logger _logger;

    private static readonly Regex EnUsTitleRegex = new(@"\[([^\[\]/]+)\]\s*$", RegexOptions.Compiled);

    public BjShareParser(BjShareSettings settings, IndexerCapabilities capabilities, Logger logger)
        : base(settings, capabilities)
    {
        _logger = logger;
    }

    public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

        if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            if (indexerResponse.HttpResponse.HasHttpRedirect)
            {
                _logger.Warn("Redirected to {0} from indexer request", indexerResponse.HttpResponse.RedirectUrl);

                if (indexerResponse.HttpResponse.RedirectUrl.ContainsIgnoreCase("/login.php"))
                {
                    CookiesUpdater(null, null);
                    throw new IndexerException(indexerResponse, "We are being redirected to the login page. Most likely your session expired or was killed. Recheck your cookie or credentials and try testing the indexer.");
                }

                throw new IndexerException(indexerResponse, $"Redirected to {indexerResponse.HttpResponse.RedirectUrl} from indexer request");
            }

            STJson.TryDeserialize<GazelleErrorResponse>(indexerResponse.Content, out var errorResponse);

            throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request: {errorResponse?.Error ?? "Check the logs for more information."}");
        }

        if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
        {
            throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
        }

        var jsonResponse = new HttpResponse<GazelleResponse>(indexerResponse.HttpResponse);
        if (jsonResponse.Resource.Status != "success" ||
            jsonResponse.Resource.Status.IsNullOrWhiteSpace() ||
            jsonResponse.Resource.Response == null)
        {
            return releaseInfos;
        }

        foreach (var result in jsonResponse.Resource.Response.Results)
        {
            var posterUrl = GetPosterUrl(result.Cover);
            var groupTitle = ParseTitle(result.GroupName);
            int.TryParse(result.GroupYear, out var groupYear);

            if (result.Torrents != null)
            {
                foreach (var torrent in result.Torrents)
                {
                    var isFreeLeech = torrent.IsFreeLeech || torrent.IsNeutralLeech || torrent.IsPersonalFreeLeech;

                    if (Settings.UseFreeleechToken == (int)GazelleFreeleechTokenAction.Required && !torrent.CanUseToken && !isFreeLeech)
                    {
                        continue;
                    }

                    var id = torrent.TorrentId;
                    var infoUrl = GetInfoUrl(result.GroupId, id);

                    var title = BuildTitle(groupTitle, groupYear, torrent);

                    var release = new TorrentInfo
                    {
                        Guid = infoUrl,
                        InfoUrl = infoUrl,
                        DownloadUrl = GetDownloadUrl(id, torrent.CanUseToken && !isFreeLeech),
                        Title = WebUtility.HtmlDecode(title),
                        Container = torrent.Encoding,
                        Files = torrent.FileCount,
                        Grabs = torrent.Snatches ?? 0,
                        Codec = torrent.Format,
                        Size = long.Parse(torrent.Size),
                        Seeders = int.Parse(torrent.Seeders),
                        Peers = int.Parse(torrent.Leechers) + int.Parse(torrent.Seeders),
                        PublishDate = torrent.Time.ToUniversalTime(),
                        Scene = torrent.Scene,
                        PosterUrl = posterUrl,
                        DownloadVolumeFactor = isFreeLeech ? 0 : 1,
                        UploadVolumeFactor = torrent.IsNeutralLeech ? 0 : 1,
                        MinimumRatio = 1,
                        MinimumSeedTime = 604800 // 7 days
                    };

                    var category = torrent.Category;
                    if (category == null || category.Contains("Select Category"))
                    {
                        release.Categories = Capabilities.Categories.MapTrackerCatToNewznab("1");
                    }
                    else
                    {
                        release.Categories = Capabilities.Categories.MapTrackerCatDescToNewznab(category);
                    }

                    releaseInfos.Add(release);
                }
            }
            else
            {
                var isFreeLeech = result.IsFreeLeech || result.IsNeutralLeech || result.IsPersonalFreeLeech;

                if (Settings.UseFreeleechToken == (int)GazelleFreeleechTokenAction.Required && !result.CanUseToken && !isFreeLeech)
                {
                    continue;
                }

                var id = result.TorrentId;
                var infoUrl = GetInfoUrl(result.GroupId, id);

                var title = groupTitle;
                if (groupYear > 0)
                {
                    title += $" ({groupYear})";
                }

                var release = new TorrentInfo
                {
                    Guid = infoUrl,
                    InfoUrl = infoUrl,
                    DownloadUrl = GetDownloadUrl(id, result.CanUseToken && !isFreeLeech),
                    Title = WebUtility.HtmlDecode(title),
                    Size = long.Parse(result.Size),
                    Seeders = int.Parse(result.Seeders),
                    Peers = int.Parse(result.Leechers) + int.Parse(result.Seeders),
                    Files = result.FileCount,
                    Grabs = result.Snatches ?? 0,
                    PublishDate = long.TryParse(result.GroupTime, out var num) ? DateTimeOffset.FromUnixTimeSeconds(num).UtcDateTime : DateTimeUtil.FromFuzzyTime((string)result.GroupTime),
                    PosterUrl = posterUrl,
                    DownloadVolumeFactor = isFreeLeech ? 0 : 1,
                    UploadVolumeFactor = result.IsNeutralLeech ? 0 : 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 604800
                };

                var category = result.Category;
                if (category == null || category.Contains("Select Category"))
                {
                    release.Categories = Capabilities.Categories.MapTrackerCatToNewznab("1");
                }
                else
                {
                    release.Categories = Capabilities.Categories.MapTrackerCatDescToNewznab(category);
                }

                releaseInfos.Add(release);
            }
        }

        return releaseInfos
            .OrderByDescending(o => o.PublishDate)
            .ToArray();
    }

    private static string ParseTitle(string groupName)
    {
        if (groupName.IsNullOrWhiteSpace())
        {
            return groupName;
        }

        // BJ-Share title format: "PT-BR title [en-US title] [year]"
        // Remove trailing [year] if present
        var title = Regex.Replace(groupName, @"\s*\[\d{4}\]\s*$", "").Trim();

        // Extract en-US title from brackets if available
        var match = EnUsTitleRegex.Match(title);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return title;
    }

    private static string BuildTitle(string groupTitle, int groupYear, GazelleTorrent torrent)
    {
        var parts = new List<string> { groupTitle };

        if (groupYear > 0)
        {
            parts.Add($"({groupYear})");
        }

        if (torrent.RemasterTitle.IsNotNullOrWhiteSpace())
        {
            parts.Add(torrent.RemasterTitle);
        }

        var mediaInfo = new List<string>();
        if (torrent.Format.IsNotNullOrWhiteSpace())
        {
            mediaInfo.Add(torrent.Format);
        }

        if (torrent.Encoding.IsNotNullOrWhiteSpace())
        {
            mediaInfo.Add(torrent.Encoding);
        }

        if (torrent.Media.IsNotNullOrWhiteSpace())
        {
            mediaInfo.Add(torrent.Media);
        }

        if (mediaInfo.Any())
        {
            parts.Add($"[{string.Join(" / ", mediaInfo)}]");
        }

        return string.Join(" ", parts);
    }
}

public class BjShareSettings : GazelleUserPassOrCookieSettings
{
    private static readonly GazelleUserPassOrCookieValidator<BjShareSettings> Validator = new();

    [FieldDefinition(6, Label = "Freeleech Only", Type = FieldType.Checkbox, HelpText = "Search freeleech torrents only")]
    public bool FreeleechOnly { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
