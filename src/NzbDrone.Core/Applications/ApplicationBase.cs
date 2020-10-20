using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Applications
{
    public abstract class ApplicationBase<TSettings> : IApplications
        where TSettings : IProviderConfig, new()
    {
        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public ProviderDefinition Definition { get; set; }
        public abstract ValidationResult Test();

        protected TSettings Settings => (TSettings)Definition.Settings;

        public override string ToString()
        {
            return GetType().Name;
        }

        public virtual IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                var config = (IProviderConfig)new TSettings();

                yield return new ApplicationDefinition
                {
                    Name = GetType().Name,
                    SyncLevel = ApplicationSyncLevel.AddOnly,
                    Implementation = GetType().Name,
                    Settings = config
                };
            }
        }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }
    }
}
