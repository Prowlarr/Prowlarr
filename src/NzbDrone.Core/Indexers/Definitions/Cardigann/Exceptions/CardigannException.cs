using System;
using NzbDrone.Core.Indexers.Exceptions;

namespace NzbDrone.Core.Indexers.Definitions.Cardigann.Exceptions
{
    public class CardigannException : IndexerException
    {
        public CardigannException(string message)
            : base(message)
        {
        }

        public CardigannException(string message, params object[] args)
            : base(message, args)
        {
        }

        public CardigannException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public CardigannException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
