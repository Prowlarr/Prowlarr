using System;

namespace NzbDrone.Common.Exceptions
{
    public class ProwlarrStartupException : NzbDroneException
    {
        public ProwlarrStartupException(string message, params object[] args)
            : base("Prowlarr failed to start: " + string.Format(message, args))
        {
        }

        public ProwlarrStartupException(string message)
            : base("Prowlarr failed to start: " + message)
        {
        }

        public ProwlarrStartupException()
            : base("Prowlarr failed to start")
        {
        }

        public ProwlarrStartupException(Exception innerException, string message, params object[] args)
            : base("Prowlarr failed to start: " + string.Format(message, args), innerException)
        {
        }

        public ProwlarrStartupException(Exception innerException, string message)
            : base("Prowlarr failed to start: " + message, innerException)
        {
        }

        public ProwlarrStartupException(Exception innerException)
            : base("Prowlarr failed to start: " + innerException.Message)
        {
        }
    }
}
