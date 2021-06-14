using System;

namespace NzbDrone.Core.Exceptions
{
    public class ReleaseUnavailableException : ReleaseDownloadException
    {
        public ReleaseUnavailableException(string message, params object[] args)
            : base(message, args)
        {
        }

        public ReleaseUnavailableException(string message)
            : base(message)
        {
        }

        public ReleaseUnavailableException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }

        public ReleaseUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
