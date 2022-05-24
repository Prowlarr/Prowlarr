using System.Collections.Generic;
using NzbDrone.Common.Reflection;
using NzbDrone.Core.ThingiProvider;
using Prowlarr.Http.ClientSchema;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1
{
    public class ProviderResource<T> : RestResource
    {
        public string Name { get; set; }
        public List<Field> Fields { get; set; }
        public string ImplementationName { get; set; }
        public string Implementation { get; set; }
        public string ConfigContract { get; set; }
        public string InfoLink { get; set; }
        public ProviderMessage Message { get; set; }
        public HashSet<int> Tags { get; set; }

        public List<T> Presets { get; set; }
    }

    public class ProviderResourceMapper<TProviderResource, TProviderDefinition>
        where TProviderResource : ProviderResource<TProviderResource>, new()
        where TProviderDefinition : ProviderDefinition, new()
    {
        public virtual TProviderResource ToResource(TProviderDefinition definition)
        {
            return new TProviderResource
            {
                Id = definition.Id,

                Name = definition.Name,
                ImplementationName = definition.ImplementationName,
                Implementation = definition.Implementation,
                ConfigContract = definition.ConfigContract,
                Message = definition.Message,
                Tags = definition.Tags,
                Fields = SchemaBuilder.ToSchema(definition.Settings),

                //prowlarr/supported#{0} is an disambagation page. the # should be a header on the page with appropiate details/link
                // note: indexers get a different link
                InfoLink = string.Format("https://wiki.servarr.com/prowlarr/supported#{0}",
                    definition.Implementation.ToLower().Replace(' ', '-'))
            };
        }

        public virtual TProviderDefinition ToModel(TProviderResource resource, TProviderDefinition existingDefinition)
        {
            if (resource == null)
            {
                return default(TProviderDefinition);
            }

            var definition = new TProviderDefinition
            {
                Id = resource.Id,

                Name = resource.Name,
                ImplementationName = resource.ImplementationName,
                Implementation = resource.Implementation,
                ConfigContract = resource.ConfigContract,
                Message = resource.Message,
                Tags = resource.Tags ?? new HashSet<int>()
            };

            var configContract = ReflectionExtensions.CoreAssembly.FindTypeByName(definition.ConfigContract);
            definition.Settings = (IProviderConfig)SchemaBuilder.ReadFromSchema(resource.Fields, configContract, existingDefinition?.Settings);

            return definition;
        }
    }
}
