namespace NzbDrone.Core.Indexers.Definitions.Cardigann.Exceptions
{
    public class CardigannConfigException : CardigannException
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
