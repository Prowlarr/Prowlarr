using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Indexers.Exceptions
{
    public class IndexerAuthException : NzbDroneException
    {
        public IndexerAuthException(string message, params object[] args)
            : base(message, args)
        {
        }

        public IndexerAuthException(string message)
            : base(message)
        {
        }
    }
}
