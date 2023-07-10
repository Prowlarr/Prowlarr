using System;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Notifications;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Notifications
{
    [V1ApiController]
    public class NotificationController : ProviderControllerBase<NotificationResource, NotificationBulkResource, INotification, NotificationDefinition>
    {
        public static readonly NotificationResourceMapper ResourceMapper = new ();
        public static readonly NotificationBulkResourceMapper BulkResourceMapper = new ();

        public NotificationController(NotificationFactory notificationFactory)
            : base(notificationFactory, "notification", ResourceMapper, BulkResourceMapper)
        {
        }

        [NonAction]
        public override ActionResult<NotificationResource> UpdateProvider([FromBody] NotificationBulkResource providerResource)
        {
            throw new NotImplementedException();
        }

        [NonAction]
        public override object DeleteProviders([FromBody] NotificationBulkResource resource)
        {
            throw new NotImplementedException();
        }
    }
}
