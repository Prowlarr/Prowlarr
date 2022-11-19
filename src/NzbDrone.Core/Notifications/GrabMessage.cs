using NzbDrone.Core.Indexers.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Notifications
{
    public class GrabMessage
    {
        public ReleaseInfo Release { get; set; }

        public bool Successful { get; set; }
        public string Host { get; set; }
        public string Source { get; set; }
        public GrabTrigger GrabTrigger { get; set; }
        public bool Redirect { get; set; }
        public string Message { get; set; }
        public string DownloadClientType { get; set; }
        public string DownloadClientName { get; set; }
        public string DownloadId { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
