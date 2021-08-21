using System;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.IndexerProxies.FlareSolverr
{
    public class FlareSolverrException : NzbDroneException
    {
        public FlareSolverrException(string message)
            : base(message)
        {
        }

        public FlareSolverrException(string message, params object[] args)
            : base(message, args)
        {
        }

        public FlareSolverrException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
