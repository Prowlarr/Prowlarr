using NzbDrone.Common.Exceptions;
using NzbDrone.Core.Indexers.Cardigann;

namespace NzbDrone.Core.Indexers.Definitions.Cardigann
{
    public class CardigannConfigException : NzbDroneException
    {
        private readonly CardigannDefinition _configuration;

        public CardigannConfigException(CardigannDefinition config, string message, params object[] args)
            : base(message, args)
        {
            _configuration = config;
        }

        public CardigannConfigException(CardigannDefinition config, string message)
            : base(message)
        {
            _configuration = config;
        }

        public CardigannDefinition Configuration => _configuration;
    }
}
