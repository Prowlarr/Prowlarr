using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRelease
    {
        public WebhookRelease()
        {
        }

        public WebhookRelease(ReleaseInfo release)
        {
            ReleaseTitle = release.Title;
            Indexer = release.Indexer;
            Size = release.Size;
        }

        public string ReleaseTitle { get; set; }
        public string Indexer { get; set; }
        public long? Size { get; set; }
    }
}
