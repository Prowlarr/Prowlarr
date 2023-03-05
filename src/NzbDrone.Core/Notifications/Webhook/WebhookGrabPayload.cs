using NzbDrone.Core.Indexers.Events;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookGrabPayload : WebhookPayload
    {
        public WebhookRelease Release { get; set; }
        public GrabTrigger Trigger { get; set; }
        public string Source { get; set; }
        public string Host { get; set; }
        public string DownloadClient { get; set; }
        public string DownloadClientType { get; set; }
        public string DownloadId { get; set; }
    }
}
