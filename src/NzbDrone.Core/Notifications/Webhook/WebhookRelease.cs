using System;
using System.Collections.Generic;
using System.Linq;
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
            Categories = release.Categories.Select(f => f.Name).ToList();
            Genres = release.Genres.ToList();
            IndexerFlags = release.IndexerFlags.Select(f => f.Name).ToHashSet();
            PublishDate = release.PublishDate;
        }

        public string ReleaseTitle { get; set; }
        public string Indexer { get; set; }
        public long? Size { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Genres { get; set; }
        public HashSet<string> IndexerFlags { get; set; }
        public DateTime? PublishDate { get; set; }
    }
}
