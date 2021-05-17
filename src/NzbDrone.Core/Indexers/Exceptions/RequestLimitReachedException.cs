using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Indexers.Exceptions
{
    public class RequestLimitReachedException : NzbDroneException
    {
        private readonly IndexerResponse _indexerResponse;

        public RequestLimitReachedException(IndexerResponse response, string message, params object[] args)
            : base(message, args)
        {
            _indexerResponse = response;
        }

        public RequestLimitReachedException(IndexerResponse response, string message)
            : base(message)
        {
            _indexerResponse = response;
        }

        public IndexerResponse Response => _indexerResponse;
    }
}
