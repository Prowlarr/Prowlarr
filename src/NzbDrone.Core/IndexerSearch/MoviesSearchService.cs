using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class MovieSearchService : IExecute<MoviesSearchCommand>
    {
        private readonly ISearchForNzb _nzbSearchService;
        private readonly Logger _logger;

        public MovieSearchService(ISearchForNzb nzbSearchService,
                                  Logger logger)
        {
            _nzbSearchService = nzbSearchService;
            _logger = logger;
        }

        public void Execute(MoviesSearchCommand message)
        {
            var decisions = _nzbSearchService.MovieSearch(message.SearchTerm, false, false);

            _logger.ProgressInfo("Movie search completed. {0} reports downloaded.", decisions.Count);
        }
    }
}
