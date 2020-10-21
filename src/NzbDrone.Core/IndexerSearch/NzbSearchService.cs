using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.IndexerSearch
{
    public interface ISearchForNzb
    {
        List<ReleaseInfo> Search(string query, List<int> indexerIds, bool userInvokedSearch, bool interactiveSearch);
        NewznabResults Search(NewznabRequest request, List<int> indexerIds, bool userInvokedSearch, bool interactiveSearch);
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

        public List<ReleaseInfo> Search(string query, List<int> indexerIds, bool userInvokedSearch, bool interactiveSearch)
        {
            var searchSpec = Get<MovieSearchCriteria>(query, indexerIds, userInvokedSearch, interactiveSearch);

            return Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
        }

        public NewznabResults Search(NewznabRequest request, List<int> indexerIds, bool userInvokedSearch, bool interactiveSearch)
        {
            var searchSpec = Get<MovieSearchCriteria>(request.q, indexerIds, userInvokedSearch, interactiveSearch);

            return new NewznabResults { Releases = Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec) };
        }

        private TSpec Get<TSpec>(string query, List<int> indexerIds, bool userInvokedSearch, bool interactiveSearch)
            where TSpec : SearchCriteriaBase, new()
        {
            var spec = new TSpec()
            {
                UserInvokedSearch = userInvokedSearch,
                InteractiveSearch = interactiveSearch
            };

            spec.SceneTitles = new List<string> { query };
            spec.IndexerIds = indexerIds;

            return spec;
        }

        private List<ReleaseInfo> Dispatch(Func<IIndexer, IEnumerable<ReleaseInfo>> searchAction, SearchCriteriaBase criteriaBase)
        {
            var indexers = criteriaBase.InteractiveSearch ?
                _indexerFactory.InteractiveSearchEnabled() :
                _indexerFactory.AutomaticSearchEnabled();

            if (criteriaBase.IndexerIds != null && criteriaBase.IndexerIds.Count > 0)
            {
                indexers = indexers.Where(i => criteriaBase.IndexerIds.Contains(i.Definition.Id)).ToList();
            }

            var reports = new List<ReleaseInfo>();

            _logger.ProgressInfo("Searching {0} indexers for {1}", indexers.Count, criteriaBase.QueryTitles.Join(", "));

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

                        _eventAggregator.PublishEvent(new IndexerQueryEvent(indexer.Definition.Id, criteriaBase.QueryTitles.Join(", ")));

                        lock (reports)
                        {
                            reports.AddRange(indexerReports);
                        }
                    }
                    catch (Exception e)
                    {
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
