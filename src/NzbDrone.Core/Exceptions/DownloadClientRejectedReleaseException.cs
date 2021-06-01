using System;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Exceptions
{
    public class DownloadClientRejectedReleaseException : ReleaseDownloadException
    {
        public ReleaseInfo Release { get; set; }
        public DownloadClientRejectedReleaseException(ReleaseInfo release, string message, params object[] args)
            : base(message, args)
        {
            Release = release;
        }

        public DownloadClientRejectedReleaseException(ReleaseInfo release, string message)
            : base(message)
        {
            Release = release;
        }

        public DownloadClientRejectedReleaseException(ReleaseInfo release, string message, Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
            Release = release;
        }

        public DownloadClientRejectedReleaseException(ReleaseInfo release, string message, Exception innerException)
            : base(message, innerException)
        {
            Release = release;
        }
    }
}
