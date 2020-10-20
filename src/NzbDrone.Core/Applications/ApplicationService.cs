using NLog;

namespace NzbDrone.Core.Applications
{
    public class ApplicationService
    {
        private readonly IApplicationsFactory _applicationsFactory;
        private readonly Logger _logger;

        public ApplicationService(IApplicationsFactory applicationsFactory, Logger logger)
        {
            _applicationsFactory = applicationsFactory;
            _logger = logger;
        }
    }
}
