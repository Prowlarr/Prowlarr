using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Http;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.IndexerProxies
{
    public abstract class IndexerProxyBase<TSettings> : IIndexerProxy
        where TSettings : IIndexerProxySettings, new()
    {
        public abstract string Name { get; }
        public virtual ProviderMessage Message => null;

        public Type ConfigContract => typeof(TSettings);

        public IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                var config = (IProviderConfig)new TSettings();

                yield return new IndexerProxyDefinition
                {
                    Name = GetType().Name,
                    Implementation = GetType().Name,
                    Settings = config
                };
            }
        }

        public ProviderDefinition Definition { get; set; }
        public abstract ValidationResult Test();

        public abstract HttpRequest PreRequest(HttpRequest request);
        public virtual HttpResponse PostResponse(HttpResponse response)
        {
            return response;
        }

        protected TSettings Settings => (TSettings)Definition.Settings;

        public override string ToString()
        {
            return GetType().Name;
        }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }
    }
}
