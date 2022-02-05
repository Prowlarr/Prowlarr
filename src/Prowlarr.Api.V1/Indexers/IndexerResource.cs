using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Cardigann;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Parser;
using Prowlarr.Http.ClientSchema;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerResource : ProviderResource<IndexerResource>
    {
        public string[] IndexerUrls { get; set; }
        public string[] LegacyUrls { get; set; }
        public string DefinitionFile { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string Encoding { get; set; }
        public bool Enable { get; set; }
        public bool Redirect { get; set; }
        public bool SupportsRss { get; set; }
        public bool SupportsSearch { get; set; }
        public bool SupportsRedirect { get; set; }
        public int AppProfileId { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public IndexerPrivacy Privacy { get; set; }
        public IndexerCapabilityResource Capabilities { get; set; }
        public int Priority { get; set; }
        public DateTime Added { get; set; }
        public IndexerStatusResource Status { get; set; }
        public string SortName { get; set; }
    }

    public class IndexerResourceMapper : ProviderResourceMapper<IndexerResource, IndexerDefinition>
    {
        private readonly IIndexerDefinitionUpdateService _definitionService;

        public IndexerResourceMapper(IIndexerDefinitionUpdateService definitionService)
        {
            _definitionService = definitionService;
        }

        public override IndexerResource ToResource(IndexerDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var resource = base.ToResource(definition);

            var infoLinkName = definition.ImplementationName;

            if (definition.Implementation == typeof(Cardigann).Name)
            {
                var extraFields = definition.ExtraFields?.Select((x, i) => MapField(x, i)).ToList() ?? new List<Field>();

                resource.Fields.AddRange(extraFields);

                var settings = (CardigannSettings)definition.Settings;
                foreach (var setting in settings.ExtraFieldData)
                {
                    var field = extraFields.FirstOrDefault(x => x.Name == setting.Key);

                    //Use values from db for all but info type fields
                    if (field != null && field.Type != "info")
                    {
                        field.Value = setting.Value;
                    }
                }

                infoLinkName = definition.DefinitionFile;
            }

            resource.DefinitionFile = definition.DefinitionFile ?? definition.ImplementationName;
            resource.InfoLink = string.Format("https://wiki.servarr.com/prowlarr/supported-indexers#{0}", infoLinkName.ToLower().Replace(' ', '-'));
            resource.AppProfileId = definition.AppProfileId;
            resource.IndexerUrls = definition.IndexerUrls;
            resource.LegacyUrls = definition.LegacyUrls;
            resource.Description = definition.Description;
            resource.Language = definition.Language;
            resource.Encoding = definition.Encoding?.EncodingName ?? null;
            resource.Enable = definition.Enable;
            resource.Redirect = definition.Redirect;
            resource.SupportsRss = definition.SupportsRss;
            resource.SupportsSearch = definition.SupportsSearch;
            resource.SupportsRedirect = definition.SupportsRedirect;
            resource.Capabilities = definition.Capabilities.ToResource();
            resource.Protocol = definition.Protocol;
            resource.Privacy = definition.Privacy;
            resource.Priority = definition.Priority;
            resource.Added = definition.Added;
            resource.SortName = definition.Name.NormalizeTitle();

            return resource;
        }

        public override IndexerDefinition ToModel(IndexerResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            var definition = base.ToModel(resource);
            definition.DefinitionFile = resource.DefinitionFile;

            if (resource.Implementation == typeof(Cardigann).Name)
            {
                var standardFields = base.ToResource(definition).Fields.Select(x => x.Name).ToList();

                var settings = (CardigannSettings)definition.Settings;

                var cardigannDefinition = _definitionService.GetCachedDefinition(definition.DefinitionFile);

                foreach (var field in resource.Fields)
                {
                    if (!standardFields.Contains(field.Name))
                    {
                        if (field.Name == "cardigannCaptcha")
                        {
                            settings.ExtraFieldData["CAPTCHA"] = field.Value?.ToString() ?? string.Empty;
                        }
                        else
                        {
                            var cardigannSetting = cardigannDefinition.Settings.FirstOrDefault(x => x.Name == field.Name);
                            settings.ExtraFieldData[field.Name] = MapValue(cardigannSetting, field.Value);
                        }
                    }
                }
            }

            definition.AppProfileId = resource.AppProfileId;
            definition.Enable = resource.Enable;
            definition.Redirect = resource.Redirect;
            definition.IndexerUrls = resource.IndexerUrls;
            definition.Priority = resource.Priority;
            definition.Privacy = resource.Privacy;
            definition.Added = resource.Added;

            return definition;
        }

        private object MapValue(SettingsField setting, object value)
        {
            if (setting.Type == "select")
            {
                return value.ToString().ParseInt64() ?? 0;
            }
            else if (setting.Type == "checkbox")
            {
                if (bool.TryParse(value.ToString(), out var result))
                {
                    return result;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return value?.ToString() ?? string.Empty;
            }
        }

        private Field MapField(SettingsField setting, int order)
        {
            var field = new Field
            {
                Name = setting.Name,
                Label = setting.Label,
                Order = order,
                Type = setting.Type == "text" ? "textbox" : setting.Type
            };

            if (setting.Type == "select")
            {
                var sorted = setting.Options.OrderBy(x => x.Key).ToList();
                field.SelectOptions = sorted.Select((x, i) => new SelectOption
                {
                    Value = i,
                    Name = x.Value
                }).ToList();

                field.Value = sorted.Select(x => x.Key).ToList().IndexOf(setting.Default);
            }
            else if (setting.Type == "checkbox")
            {
                if (bool.TryParse(setting.Default, out var value))
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
                field.Value = setting.Default;
            }

            return field;
        }

        public List<IndexerResource> ToResource(IEnumerable<IndexerDefinition> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
