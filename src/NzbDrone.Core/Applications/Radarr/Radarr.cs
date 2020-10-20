using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Applications.Radarr
{
    public class Radarr : ApplicationBase<RadarrSettings>
    {
        public override string Name => "Radarr";

        private readonly IRadarrV3Proxy _radarrV3Proxy;

        public Radarr(IRadarrV3Proxy radarrV3Proxy)
        {
            _radarrV3Proxy = radarrV3Proxy;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_radarrV3Proxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
