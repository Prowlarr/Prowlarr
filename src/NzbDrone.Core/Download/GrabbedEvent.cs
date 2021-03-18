using NzbDrone.Common.Messaging;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public class GrabbedEvent : IEvent
    {
        public ReleaseInfo Release { get; private set; }
        public int DownloadClientId { get; set; }
        public string DownloadClient { get; set; }
        public string DownloadClientName { get; set; }
        public string DownloadId { get; set; }

        public GrabbedEvent(ReleaseInfo release)
        {
            Release = release;
        }
    }
}
