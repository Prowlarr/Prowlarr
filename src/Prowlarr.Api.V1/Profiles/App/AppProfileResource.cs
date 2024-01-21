using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Profiles;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Profiles.App
{
    public class AppProfileResource : RestResource
    {
        public string Name { get; set; }
        public bool EnableRss { get; set; }
        public bool EnableAutomaticSearch { get; set; }
        public bool EnableInteractiveSearch { get; set; }
        public int MinimumSeeders { get; set; }
    }

    public static class ProfileResourceMapper
    {
        public static AppProfileResource ToResource(this AppSyncProfile model)
        {
            if (model == null)
            {
                return null;
            }

            return new AppProfileResource
            {
                Id = model.Id,
                Name = model.Name,
                EnableRss = model.EnableRss,
                EnableAutomaticSearch = model.EnableAutomaticSearch,
                EnableInteractiveSearch = model.EnableInteractiveSearch,
                MinimumSeeders = model.MinimumSeeders
            };
        }

        public static AppSyncProfile ToModel(this AppProfileResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new AppSyncProfile
            {
                Id = resource.Id,
                Name = resource.Name,
                EnableRss = resource.EnableRss,
                EnableAutomaticSearch = resource.EnableAutomaticSearch,
                EnableInteractiveSearch = resource.EnableInteractiveSearch,
                MinimumSeeders = resource.MinimumSeeders
            };
        }

        public static List<AppProfileResource> ToResource(this IEnumerable<AppSyncProfile> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
