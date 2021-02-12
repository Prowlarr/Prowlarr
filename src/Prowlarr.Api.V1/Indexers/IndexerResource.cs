using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Cardigann;
using Prowlarr.Http.ClientSchema;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerResource : ProviderResource
    {
        public bool EnableRss { get; set; }
        public bool EnableAutomaticSearch { get; set; }
        public bool EnableInteractiveSearch { get; set; }
        public bool SupportsRss { get; set; }
        public bool SupportsSearch { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public IndexerPrivacy Privacy { get; set; }
        public IndexerCapabilityResource Capabilities { get; set; }
        public int Priority { get; set; }
        public DateTime Added { get; set; }
        public IndexerStatusResource Status { get; set; }
    }

    public class IndexerResourceMapper : ProviderResourceMapper<IndexerResource, IndexerDefinition>
    {
        public override IndexerResource ToResource(IndexerDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var resource = base.ToResource(definition);

            if (definition.Implementation == typeof(Cardigann).Name)
            {
                var extraFields = definition.ExtraFields?.Select((x, i) => MapField(x, i)).ToList() ?? new List<Field>();

                resource.Fields.AddRange(extraFields);

                var settings = (CardigannSettings)definition.Settings;
                foreach (var setting in settings.ExtraFieldData)
                {
                    var field = extraFields.FirstOrDefault(x => x.Name == setting.Key);
                    if (field != null)
                    {
                        field.Value = setting.Value;
                    }
                }
            }

            resource.EnableRss = definition.EnableRss;
            resource.EnableAutomaticSearch = definition.EnableAutomaticSearch;
            resource.EnableInteractiveSearch = definition.EnableInteractiveSearch;
            resource.SupportsRss = definition.SupportsRss;
            resource.SupportsSearch = definition.SupportsSearch;
            resource.Capabilities = definition.Capabilities.ToResource();
            resource.Protocol = definition.Protocol;
            resource.Privacy = definition.Privacy;
            resource.Priority = definition.Priority;
            resource.Added = definition.Added;

            return resource;
        }

        public override IndexerDefinition ToModel(IndexerResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            var definition = base.ToModel(resource);

            if (resource.Implementation == typeof(Cardigann).Name)
            {
                var standardFields = base.ToResource(definition).Fields.Select(x => x.Name).ToList();

                var settings = (CardigannSettings)definition.Settings;
                foreach (var field in resource.Fields)
                {
                    if (!standardFields.Contains(field.Name))
                    {
                        settings.ExtraFieldData[field.Name] = field.Value;
                    }
                }
            }

            definition.EnableRss = resource.EnableRss;
            definition.EnableAutomaticSearch = resource.EnableAutomaticSearch;
            definition.EnableInteractiveSearch = resource.EnableInteractiveSearch;
            definition.Priority = resource.Priority;
            definition.Privacy = resource.Privacy;
            definition.Added = resource.Added;

            return definition;
        }

        private Field MapField(SettingsField fieldAttribute, int order)
        {
            var field = new Field
            {
                Name = fieldAttribute.Name,
                Label = fieldAttribute.Label,
                Order = order,
                Type = fieldAttribute.Type == "text" ? "textbox" : fieldAttribute.Type
            };

            if (fieldAttribute.Type == "select")
            {
                var sorted = fieldAttribute.Options.OrderBy(x => x.Key).ToList();
                field.SelectOptions = sorted.Select((x, i) => new SelectOption
                {
                    Value = i,
                    Name = x.Value
                }).ToList();

                field.Value = sorted.Select(x => x.Key).ToList().IndexOf(fieldAttribute.Default);
            }
            else if (fieldAttribute.Type == "checkbox")
            {
                if (bool.TryParse(fieldAttribute.Default, out var value))
                {
                    field.Value = value;
                }
                else
                {
                    field.Value = false;
                }
            }
            else
            {
                field.Value = fieldAttribute.Default;
            }

            return field;
        }
    }
}
