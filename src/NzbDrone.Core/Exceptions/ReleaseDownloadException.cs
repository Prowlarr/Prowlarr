using System;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Exceptions
{
    public class ReleaseDownloadException : NzbDroneException
    {
        public ReleaseDownloadException(string message, params object[] args)
            : base(message, args)
        {
        }

        public ReleaseDownloadException(string message)
            : base(message)
        {
        }

        public ReleaseDownloadException(string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }

        public ReleaseDownloadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
