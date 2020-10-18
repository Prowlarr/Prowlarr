using NzbDrone.Core.Configuration;

namespace Prowlarr.Api.V1.Config
{
    public class UiConfigModule : RadarrConfigModule<UiConfigResource>
    {
        public UiConfigModule(IConfigService configService)
            : base(configService)
        {
        }

        protected override UiConfigResource ToResource(IConfigService model)
        {
            return UiConfigResourceMapper.ToResource(model);
        }
    }
}
