using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Events;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.IndexerSearch
{
    public interface ISearchForNzb
    {
        NewznabResults Search(NewznabRequest request, List<int> indexerIds, bool interactiveSearch);
    }

    public class NzbSearchService : ISearchForNzb
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IIndexerFactory _indexerFactory;
        private readonly Logger _logger;

        public NzbSearchService(IEventAggregator eventAggregator,
                                IIndexerFactory indexerFactory,
                                Logger logger)
        {
            _eventAggregator = eventAggregator;
            _indexerFactory = indexerFactory;
            _logger = logger;
        }

        public NewznabResults Search(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var results = new NewznabResults();

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

        private NewznabResults MovieSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var searchSpec = Get<MovieSearchCriteria>(request, indexerIds, interactiveSearch);

            searchSpec.ImdbId = request.imdbid;
            searchSpec.TmdbId = request.tmdbid;
            searchSpec.TraktId = request.traktid;

            return new NewznabResults { Releases = Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec) };
        }

        private NewznabResults MusicSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var searchSpec = Get<MusicSearchCriteria>(request, indexerIds, interactiveSearch);

            return new NewznabResults { Releases = Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec) };
        }

        private NewznabResults TvSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var searchSpec = Get<TvSearchCriteria>(request, indexerIds, interactiveSearch);

            searchSpec.Season = request.season;
            searchSpec.Ep = request.ep;
            searchSpec.TvdbId = request.tvdbid;
            searchSpec.ImdbId = request.imdbid;
            searchSpec.TraktId = request.traktid;
            searchSpec.RId = request.rid;
            searchSpec.TvMazeId = request.tvmazeid;

            return new NewznabResults { Releases = Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec) };
        }

        private NewznabResults BookSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var searchSpec = Get<TvSearchCriteria>(request, indexerIds, interactiveSearch);

            return new NewznabResults { Releases = Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec) };
        }

        private NewznabResults BasicSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var searchSpec = Get<BasicSearchCriteria>(request, indexerIds, interactiveSearch);

            return new NewznabResults { Releases = Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec) };
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

            spec.SearchTerm = query.q;
            spec.SearchType = query.t;
            spec.Limit = query.limit;
            spec.Offset = query.offset;
            spec.Source = query.source;

            spec.IndexerIds = indexerIds;

            return spec;
        }

        private List<ReleaseInfo> Dispatch(Func<IIndexer, IndexerPageableQueryResult> searchAction, SearchCriteriaBase criteriaBase)
        {
            var indexers = criteriaBase.InteractiveSearch ?
                _indexerFactory.InteractiveSearchEnabled() :
                _indexerFactory.AutomaticSearchEnabled();

            if (criteriaBase.IndexerIds != null && criteriaBase.IndexerIds.Count > 0)
            {
                indexers = indexers.Where(i => criteriaBase.IndexerIds.Contains(i.Definition.Id) ||
                    (criteriaBase.IndexerIds.Contains(-1) && i.Protocol == DownloadProtocol.Usenet) ||
                    (criteriaBase.IndexerIds.Contains(-2) && i.Protocol == DownloadProtocol.Torrent))
                    .ToList();
            }

            var reports = new List<ReleaseInfo>();

            _logger.ProgressInfo("Searching {0} indexers for {1}", indexers.Count, criteriaBase.SearchTerm);

            var taskList = new List<Task>();
            var taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

            foreach (var indexer in indexers)
            {
                var indexerLocal = indexer;

                taskList.Add(taskFactory.StartNew(() =>
                {
                    try
                    {
                        var indexerReports = searchAction(indexerLocal);

                        lock (reports)
                        {
                            reports.AddRange(indexerReports.Releases);
                        }

                        foreach (var query in indexerReports.Queries)
                        {
                            _eventAggregator.PublishEvent(new IndexerQueryEvent(indexer.Definition.Id, criteriaBase, query.ElapsedTime, true, indexerReports.Releases.Count()));
                        }
                    }
                    catch (Exception e)
                    {
                        _eventAggregator.PublishEvent(new IndexerQueryEvent(indexer.Definition.Id, criteriaBase, 0, false));
                        _logger.Error(e, "Error while searching for {0}", criteriaBase);
                    }
                }).LogExceptions());
            }

            Task.WaitAll(taskList.ToArray());

            _logger.Debug("Total of {0} reports were found for {1} from {2} indexers", reports.Count, criteriaBase, indexers.Count);

            return reports;
        }
    }
}
