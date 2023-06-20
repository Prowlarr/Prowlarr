using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Applications;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.IndexerProxies;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Notifications;

namespace NzbDrone.Core.Tags
{
    public interface ITagService
    {
        Tag GetTag(int tagId);
        Tag GetTag(string tag);
        List<Tag> GetTags(IEnumerable<int> ids);
        TagDetails Details(int tagId);
        List<TagDetails> Details();
        List<Tag> All();
        Tag Add(Tag tag);
        Tag Update(Tag tag);
        void Delete(int tagId);
    }

    public class TagService : ITagService
    {
        private readonly ITagRepository _repo;
        private readonly IEventAggregator _eventAggregator;
        private readonly INotificationFactory _notificationFactory;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IIndexerProxyFactory _indexerProxyFactory;
        private readonly IApplicationFactory _applicationFactory;

        public TagService(ITagRepository repo,
                          IEventAggregator eventAggregator,
                          INotificationFactory notificationFactory,
                          IIndexerFactory indexerFactory,
                          IIndexerProxyFactory indexerProxyFactory,
                          IApplicationFactory applicationFactory)
        {
            _repo = repo;
            _eventAggregator = eventAggregator;
            _notificationFactory = notificationFactory;
            _indexerFactory = indexerFactory;
            _indexerProxyFactory = indexerProxyFactory;
            _applicationFactory = applicationFactory;
        }

        public Tag GetTag(int tagId)
        {
            return _repo.Get(tagId);
        }

        public Tag GetTag(string tag)
        {
            if (tag.All(char.IsDigit))
            {
                return _repo.Get(int.Parse(tag));
            }
            else
            {
                return _repo.GetByLabel(tag);
            }
        }

        public List<Tag> GetTags(IEnumerable<int> ids)
        {
            return _repo.Get(ids).ToList();
        }

        public TagDetails Details(int tagId)
        {
            var tag = GetTag(tagId);
            var notifications = _notificationFactory.AllForTag(tagId);
            var indexers = _indexerFactory.AllForTag(tagId);
            var indexerProxies = _indexerProxyFactory.AllForTag(tagId);
            var applications = _applicationFactory.AllForTag(tagId);

            return new TagDetails
            {
                Id = tagId,
                Label = tag.Label,
                NotificationIds = notifications.Select(c => c.Id).ToList(),
                IndexerIds = indexers.Select(c => c.Id).ToList(),
                IndexerProxyIds = indexerProxies.Select(c => c.Id).ToList(),
                ApplicationIds = applications.Select(c => c.Id).ToList()
            };
        }

        public List<TagDetails> Details()
        {
            var tags = All();
            var notifications = _notificationFactory.All();
            var indexers = _indexerFactory.All();
            var indexerProxies = _indexerProxyFactory.All();
            var applications = _applicationFactory.All();

            var details = new List<TagDetails>();

            foreach (var tag in tags)
            {
                details.Add(new TagDetails
                {
                    Id = tag.Id,
                    Label = tag.Label,
                    NotificationIds = notifications.Where(c => c.Tags.Contains(tag.Id)).Select(c => c.Id).ToList(),
                    IndexerIds = indexers.Where(c => c.Tags.Contains(tag.Id)).Select(c => c.Id).ToList(),
                    IndexerProxyIds = indexerProxies.Where(c => c.Tags.Contains(tag.Id)).Select(c => c.Id).ToList(),
                    ApplicationIds = applications.Where(c => c.Tags.Contains(tag.Id)).Select(c => c.Id).ToList()
                });
            }

            return details;
        }

        public List<Tag> All()
        {
            return _repo.All().OrderBy(t => t.Label).ToList();
        }

        public Tag Add(Tag tag)
        {
            var existingTag = _repo.FindByLabel(tag.Label);

            if (existingTag != null)
            {
                return existingTag;
            }

            tag.Label = tag.Label.ToLowerInvariant();

            _repo.Insert(tag);
            _eventAggregator.PublishEvent(new TagsUpdatedEvent());

            return tag;
        }

        public Tag Update(Tag tag)
        {
            tag.Label = tag.Label.ToLowerInvariant();

            _repo.Update(tag);
            _eventAggregator.PublishEvent(new TagsUpdatedEvent());

            return tag;
        }

        public void Delete(int tagId)
        {
            var details = Details(tagId);
            if (details.InUse)
            {
                throw new ModelConflictException(typeof(Tag), tagId, $"'{details.Label}' cannot be deleted since it's still in use");
            }

            _repo.Delete(tagId);
            _eventAggregator.PublishEvent(new TagsUpdatedEvent());
        }
    }
}
