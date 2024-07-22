using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers
{
    public abstract class IndexerBase<TSettings> : IIndexer
        where TSettings : IIndexerSettings, new()
    {
        protected readonly IIndexerStatusService _indexerStatusService;
        protected readonly IConfigService _configService;
        protected readonly Logger _logger;

        public abstract string Name { get; }
        public abstract string[] IndexerUrls { get; }
        public abstract string[] LegacyUrls { get; }
        public abstract string Description { get; }
        public abstract Encoding Encoding { get; }
        public abstract string Language { get; }
        public abstract bool FollowRedirect { get; }
        public abstract DownloadProtocol Protocol { get; }
        public abstract IndexerPrivacy Privacy { get; }
        public int Priority { get; set; }
        public bool Redirect { get; set; }

        public abstract bool SupportsRss { get; }
        public abstract bool SupportsSearch { get; }
        public abstract bool SupportsRedirect { get; }
        public abstract bool SupportsPagination { get; }
        public abstract IndexerCapabilities Capabilities { get; protected set; }

        public IndexerBase(IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
        {
            _indexerStatusService = indexerStatusService;
            _configService = configService;
            _logger = logger;
        }

        public Type ConfigContract => typeof(TSettings);

        public bool IsObsolete()
        {
            var attributes = GetType().GetCustomAttributes(false);

            return attributes.OfType<ObsoleteAttribute>().Any();
        }

        public virtual ProviderMessage Message => null;

        public virtual IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                var config = (IProviderConfig)new TSettings();

                yield return new IndexerDefinition
                {
                    Name = Name ?? GetType().Name,
                    Enable = config.Validate().IsValid && SupportsRss,
                    Implementation = GetType().Name,
                    Settings = config
                };
            }
        }

        public virtual ProviderDefinition Definition { get; set; }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "getUrls")
            {
                var links = IndexerUrls;

                return new
                {
                    options = links.Select(d => new { Value = d, Name = d })
                };
            }

            return null;
        }

        protected TSettings Settings => GetDefaultBaseUrl((TSettings)Definition.Settings);

        public abstract Task<IndexerPageableQueryResult> Fetch(MovieSearchCriteria searchCriteria);
        public abstract Task<IndexerPageableQueryResult> Fetch(MusicSearchCriteria searchCriteria);
        public abstract Task<IndexerPageableQueryResult> Fetch(TvSearchCriteria searchCriteria);
        public abstract Task<IndexerPageableQueryResult> Fetch(BookSearchCriteria searchCriteria);
        public abstract Task<IndexerPageableQueryResult> Fetch(BasicSearchCriteria searchCriteria);
        public abstract Task<IndexerDownloadResponse> Download(Uri link);

        public abstract IndexerCapabilities GetCapabilities();

        protected virtual IList<ReleaseInfo> CleanupReleases(IEnumerable<ReleaseInfo> releases, SearchCriteriaBase searchCriteria)
        {
            var result = releases.ToList();

            result.ForEach(c =>
            {
                //Set GUID if not set
                if (c.Guid.IsNullOrWhiteSpace())
                {
                    if (c.DownloadUrl.IsNotNullOrWhiteSpace())
                    {
                        c.Guid = c.DownloadUrl;
                    }
                    else if (Protocol == DownloadProtocol.Torrent && ((TorrentInfo)c).MagnetUrl.IsNotNullOrWhiteSpace())
                    {
                        c.Guid = ((TorrentInfo)c).MagnetUrl;
                    }
                    else if (c.InfoUrl.IsNotNullOrWhiteSpace())
                    {
                        c.Guid = c.InfoUrl;
                    }
                }

                //Set common props
                c.IndexerId = Definition.Id;
                c.Indexer = Definition.Name;
                c.DownloadProtocol = Protocol;
                c.IndexerPrivacy = ((IndexerDefinition)Definition).Privacy;
                c.IndexerPriority = ((IndexerDefinition)Definition).Priority;

                //Add common flags
                if (Protocol == DownloadProtocol.Torrent && c is TorrentInfo torrentRelease)
                {
                    if (torrentRelease.DownloadVolumeFactor == 0.0)
                    {
                        torrentRelease.IndexerFlags.Add(IndexerFlag.FreeLeech);
                    }
                    else if (torrentRelease.DownloadVolumeFactor == 0.5)
                    {
                        torrentRelease.IndexerFlags.Add(IndexerFlag.HalfLeech);
                    }

                    if (torrentRelease.UploadVolumeFactor == 0.0)
                    {
                        torrentRelease.IndexerFlags.Add(IndexerFlag.NeutralLeech);
                    }
                    else if (torrentRelease.UploadVolumeFactor == 2.0)
                    {
                        torrentRelease.IndexerFlags.Add(IndexerFlag.DoubleUpload);
                    }

                    if (torrentRelease.Scene.GetValueOrDefault(false))
                    {
                        torrentRelease.IndexerFlags.Add(IndexerFlag.Scene);
                    }
                }
            });

            return result.DistinctBy(v => v.Guid).ToList();
        }

        protected virtual IEnumerable<ReleaseInfo> FilterReleasesByQuery(IEnumerable<ReleaseInfo> releases, SearchCriteriaBase searchCriteria)
        {
            var commonWords = new[] { "and", "the", "an", "of" };

            if (!searchCriteria.IsRssSearch && !searchCriteria.IsIdSearch)
            {
                var splitRegex = new Regex("[^\\w]+");

                // split search term to individual terms for less aggressive filtering, filter common terms
                var terms = splitRegex.Split(searchCriteria.SearchTerm).Where(t => t.IsNotNullOrWhiteSpace() && t.Length > 1 && !commonWords.ContainsIgnoreCase(t)).ToArray();

                // check in title and description for any term searched for
                releases = releases.Where(r =>
                {
                    var matches = terms.Where(t => (r.Title.IsNotNullOrWhiteSpace() && r.Title.ContainsIgnoreCase(t)) || (r.Description.IsNotNullOrWhiteSpace() && r.Description.ContainsIgnoreCase(t)));

                    return terms.Length > 1 ? matches.Skip(1).Any() : matches.Any();
                }).ToList();
            }

            return releases;
        }

        protected virtual TSettings GetDefaultBaseUrl(TSettings settings)
        {
            var defaultLink = IndexerUrls.FirstOrDefault();

            if (settings.BaseUrl.IsNullOrWhiteSpace() && defaultLink.IsNotNullOrWhiteSpace())
            {
                settings.BaseUrl = defaultLink;
            }
            else if (settings.BaseUrl.IsNotNullOrWhiteSpace() && LegacyUrls.Contains(settings.BaseUrl))
            {
                _logger.Debug(string.Format("Changing legacy site link from {0} to {1}", settings.BaseUrl, defaultLink));
                settings.BaseUrl = defaultLink;
            }

            return settings;
        }

        public ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                Test(failures).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Test aborted due to exception");
                failures.Add(new ValidationFailure(string.Empty, "Test was aborted due to an error: " + ex.Message));
            }

            return new ValidationResult(failures);
        }

        protected abstract Task Test(List<ValidationFailure> failures);

        public override string ToString()
        {
            return Definition.Name;
        }
    }
}
