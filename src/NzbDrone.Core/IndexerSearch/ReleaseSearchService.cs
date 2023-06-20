using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Events;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.IndexerSearch
{
    public interface ISearchForNzb
    {
        Task<NewznabResults> Search(NewznabRequest request, List<int> indexerIds, bool interactiveSearch);
    }

    public class ReleaseSearchService : ISearchForNzb
    {
        private readonly IIndexerLimitService _indexerLimitService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IIndexerFactory _indexerFactory;
        private readonly Logger _logger;

        public ReleaseSearchService(IEventAggregator eventAggregator,
                                IIndexerFactory indexerFactory,
                                IIndexerLimitService indexerLimitService,
                                Logger logger)
        {
            _eventAggregator = eventAggregator;
            _indexerFactory = indexerFactory;
            _indexerLimitService = indexerLimitService;
            _logger = logger;
        }

        public Task<NewznabResults> Search(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            switch (request.t)
            {
                case "movie":
                    return MovieSearch(request, indexerIds, interactiveSearch);
                case "music":
                    return MusicSearch(request, indexerIds, interactiveSearch);
                case "tvsearch":
                    return TvSearch(request, indexerIds, interactiveSearch);
                case "book":
                    return BookSearch(request, indexerIds, interactiveSearch);
                default:
                    return BasicSearch(request, indexerIds, interactiveSearch);
            }
        }

        private async Task<NewznabResults> MovieSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var searchSpec = Get<MovieSearchCriteria>(request, indexerIds, interactiveSearch);

            var imdbId = ParseUtil.GetImdbId(request.imdbid);

            searchSpec.ImdbId = imdbId?.ToString("D7");
            searchSpec.TmdbId = request.tmdbid;
            searchSpec.TraktId = request.traktid;
            searchSpec.DoubanId = request.doubanid;
            searchSpec.Year = request.year;
            searchSpec.Genre = request.genre;

            return new NewznabResults { Releases = await Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec) };
        }

        private async Task<NewznabResults> MusicSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var searchSpec = Get<MusicSearchCriteria>(request, indexerIds, interactiveSearch);

            searchSpec.Artist = request.artist;
            searchSpec.Album = request.album;
            searchSpec.Label = request.label;
            searchSpec.Genre = request.genre;
            searchSpec.Track = request.track;
            searchSpec.Year = request.year;

            return new NewznabResults { Releases = await Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec) };
        }

        private async Task<NewznabResults> TvSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var searchSpec = Get<TvSearchCriteria>(request, indexerIds, interactiveSearch);

            var imdbId = ParseUtil.GetImdbId(request.imdbid);

            searchSpec.ImdbId = imdbId?.ToString("D7");
            searchSpec.Season = request.season;
            searchSpec.Episode = request.ep;
            searchSpec.TvdbId = request.tvdbid;
            searchSpec.TraktId = request.traktid;
            searchSpec.TmdbId = request.tmdbid;
            searchSpec.DoubanId = request.doubanid;
            searchSpec.RId = request.rid;
            searchSpec.TvMazeId = request.tvmazeid;
            searchSpec.Year = request.year;
            searchSpec.Genre = request.genre;

            return new NewznabResults { Releases = await Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec) };
        }

        private async Task<NewznabResults> BookSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var searchSpec = Get<BookSearchCriteria>(request, indexerIds, interactiveSearch);

            searchSpec.Author = request.author;
            searchSpec.Title = request.title;
            searchSpec.Publisher = request.publisher;
            searchSpec.Year = request.year;
            searchSpec.Genre = request.genre;

            return new NewznabResults { Releases = await Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec) };
        }

        private async Task<NewznabResults> BasicSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var searchSpec = Get<BasicSearchCriteria>(request, indexerIds, interactiveSearch);

            return new NewznabResults { Releases = await Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec) };
        }

        private TSpec Get<TSpec>(NewznabRequest query, List<int> indexerIds, bool interactiveSearch)
            where TSpec : SearchCriteriaBase, new()
        {
            var spec = new TSpec()
            {
                InteractiveSearch = interactiveSearch
            };

            if (query.cat != null)
            {
                spec.Categories = query.cat.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => int.Parse(s)).ToArray();
            }
            else
            {
                spec.Categories = Array.Empty<int>();
            }

            spec.SearchTerm = query.q?.Trim();
            spec.SearchType = query.t;
            spec.Limit = query.limit;
            spec.Offset = query.offset;
            spec.Source = query.source;
            spec.Host = query.host;

            spec.IndexerIds = indexerIds;

            return spec;
        }

        private async Task<IList<ReleaseInfo>> Dispatch(Func<IIndexer, Task<IndexerPageableQueryResult>> searchAction, SearchCriteriaBase criteriaBase)
        {
            var indexers = _indexerFactory.Enabled();

            if (criteriaBase.IndexerIds is { Count: > 0 })
            {
                indexers = indexers.Where(i => criteriaBase.IndexerIds.Contains(i.Definition.Id) ||
                    (criteriaBase.IndexerIds.Contains(-1) && i.Protocol == DownloadProtocol.Usenet) ||
                    (criteriaBase.IndexerIds.Contains(-2) && i.Protocol == DownloadProtocol.Torrent))
                    .ToList();

                if (indexers.Count == 0)
                {
                    _logger.Debug("Search failed due to all selected indexers being unavailable: {0}", string.Join(", ", criteriaBase.IndexerIds));

                    throw new SearchFailedException("Search failed due to all selected indexers being unavailable");
                }
            }

            if (criteriaBase.Categories is { Length: > 0 })
            {
                //Only query supported indexers
                indexers = indexers.Where(i => ((IndexerDefinition)i.Definition).Capabilities.Categories.SupportedCategories(criteriaBase.Categories).Any()).ToList();

                if (indexers.Count == 0)
                {
                    _logger.Debug("All provided categories are unsupported by selected indexers: {0}", string.Join(", ", criteriaBase.Categories));

                    return Array.Empty<ReleaseInfo>();
                }
            }

            _logger.ProgressInfo("Searching indexer(s): [{0}] for {1}", string.Join(", ", indexers.Select(i => i.Definition.Name).ToList()), criteriaBase.ToString());

            var tasks = indexers.Select(x => DispatchIndexer(searchAction, x, criteriaBase));

            var batch = await Task.WhenAll(tasks);

            var reports = batch.SelectMany(x => x).ToList();

            _logger.Debug("Total of {0} reports were found for {1} from {2} indexer(s)", reports.Count, criteriaBase, indexers.Count);

            return reports;
        }

        private async Task<IList<ReleaseInfo>> DispatchIndexer(Func<IIndexer, Task<IndexerPageableQueryResult>> searchAction, IIndexer indexer, SearchCriteriaBase criteriaBase)
        {
            if (_indexerLimitService.AtQueryLimit((IndexerDefinition)indexer.Definition))
            {
                return Array.Empty<ReleaseInfo>();
            }

            try
            {
                var indexerReports = await searchAction(indexer);

                var releases = indexerReports.Releases;

                //Filter results to only those in searched categories
                if (criteriaBase.Categories.Length > 0)
                {
                    var expandedQueryCats = ((IndexerDefinition)indexer.Definition).Capabilities.Categories.ExpandTorznabQueryCategories(criteriaBase.Categories);

                    releases = releases.Where(result => result.Categories?.Any() != true || expandedQueryCats.Intersect(result.Categories.Select(c => c.Id)).Any()).ToList();

                    if (releases.Count != indexerReports.Releases.Count)
                    {
                        _logger.Trace("{0} {1} Releases which didn't contain search categories [{2}] were filtered", indexerReports.Releases.Count - releases.Count, indexer.Name, string.Join(", ", expandedQueryCats));
                    }
                }

                foreach (var query in indexerReports.Queries)
                {
                    _eventAggregator.PublishEvent(new IndexerQueryEvent(indexer.Definition.Id, criteriaBase, query));
                }

                return releases;
            }
            catch (Exception e)
            {
                _eventAggregator.PublishEvent(new IndexerQueryEvent(indexer.Definition.Id, criteriaBase, new IndexerQueryResult()));
                _logger.Error(e, "Error while searching for {0}", criteriaBase);
            }

            return Array.Empty<ReleaseInfo>();
        }
    }
}
