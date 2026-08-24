using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Parser;
using Prowlarr.Http.ClientSchema;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerResource : ProviderResource<IndexerResource>
    {
        public string[] IndexerUrls { get; set; }
        public string[] LegacyUrls { get; set; }
        public string DefinitionName { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string Encoding { get; set; }
        public bool Enable { get; set; }
        public bool Redirect { get; set; }
        public bool SupportsRss { get; set; }
        public bool SupportsSearch { get; set; }
        public bool SupportsRedirect { get; set; }
        public bool SupportsPagination { get; set; }
        public int AppProfileId { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public IndexerPrivacy Privacy { get; set; }
        public IndexerCapabilityResource Capabilities { get; set; }
        public int Priority { get; set; }
        public int DownloadClientId { get; set; }
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

            resource.DefinitionName = definition.ImplementationName;

            var infoLinkName = definition.ImplementationName;

            if (definition.Implementation == nameof(Cardigann))
            {
                var extraFields = definition.ExtraFields?.Select((field, i) => MapCardigannField(definition, field, i)).ToList() ?? new List<Field>();

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

                resource.DefinitionName = settings.DefinitionFile;
                infoLinkName = settings.DefinitionFile;
            }

            resource.InfoLink = $"https://wiki.servarr.com/prowlarr/supported-indexers#{infoLinkName.ToLower().Replace(' ', '-')}";
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
            resource.SupportsPagination = definition.SupportsPagination;
            resource.Capabilities = definition.Capabilities.ToResource();
            resource.Protocol = definition.Protocol;
            resource.Privacy = definition.Privacy;
            resource.Priority = definition.Priority;
            resource.DownloadClientId = definition.DownloadClientId;
            resource.Added = definition.Added;
            resource.SortName = definition.Name.NormalizeTitle();

            return resource;
        }

        public override IndexerDefinition ToModel(IndexerResource resource, IndexerDefinition existingDefinition)
        {
            if (resource == null)
            {
                return null;
            }

            var definition = base.ToModel(resource, existingDefinition);

            if (resource.Implementation == nameof(Cardigann))
            {
                var standardFields = base.ToResource(definition).Fields.Select(x => x.Name).ToList();

                var settings = (CardigannSettings)definition.Settings;

                if (settings.DefinitionFile.IsNotNullOrWhiteSpace())
                {
                    var cardigannDefinition = _definitionService.GetCachedDefinition(settings.DefinitionFile);

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

                                if (cardigannSetting == null)
                                {
                                    throw new ArgumentOutOfRangeException(field.Name, "Unknown Cardigann setting.");
                                }

                                settings.ExtraFieldData[field.Name] = MapValue(cardigannSetting, field.Value);
                            }
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
            definition.DownloadClientId = resource.DownloadClientId;
            definition.Added = resource.Added;

            return definition;
        }

        private object MapValue(SettingsField setting, object value)
        {
            return setting.Type switch
            {
                "select" => value.ToString().ParseInt64() ?? 0,
                "checkbox" => bool.TryParse(value.ToString(), out var result) && result,
                _ => value?.ToString() ?? string.Empty
            };
        }

        private Field MapCardigannField(IndexerDefinition definition, SettingsField setting, int order)
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
                field.Value = bool.TryParse(setting.Default, out var value) && value;
            }
            else if (setting.Type is "info_cookie" or "info_flaresolverr" or "info_useragent" or "info_category_8000")
            {
                field.Type = "info";

                switch (setting.Type)
                {
                    case "info_cookie":
                        field.Label = "How to get the Cookie";
                        field.Value = "<ol><li>Login to this tracker with your browser</li><li>If present in the login page, ensure you have the <b>Remember me</b> ticked and the <b>Log Me Out if IP Changes</b> unticked when you login</li><li>Navigate to the web site's torrent search page to view the list of available torrents for download</li><li>Open the <b>DevTools</b> panel by pressing <b>F12</b></li><li>Select the <b>Network</b> tab</li><li>Click on the <b>Doc</b> button (Chrome Browser) or <b>HTML</b> button (FireFox)</li><li>Refresh the page by pressing <b>F5</b></li><li>Click on the first row entry</li><li>Select the <b>Headers</b> tab on the Right panel</li><li>Find <b>'cookie:'</b> in the <b>Request Headers</b> section</li><li><b>Select</b> and <b>Copy</b> the whole cookie string <i>(everything after 'cookie: ')</i> and <b>Paste</b> here.</li></ol>";
                        field.HelpLink = "https://wiki.servarr.com/useful-tools#finding-cookies";
                        break;
                    case "info_useragent":
                        field.Label = "How to get the User-Agent";
                        field.Value = "<ol><li>From the same place you fetched the cookie,</li><li>Find <b>'user-agent:'</b> in the <b>Request Headers</b> section</li><li><b>Select</b> and <b>Copy</b> the whole user-agent string <i>(everything after 'user-agent: ')</i> and <b>Paste</b> here.</li></ol>";
                        field.HelpLink = "https://wiki.servarr.com/useful-tools#finding-cookies";
                        break;
                    case "info_flaresolverr":
                        field.Label = "FlareSolverr Info";
                        field.Value = "This site may use Cloudflare DDoS Protection, therefore Prowlarr requires <a href=\"https://wiki.servarr.com/prowlarr/faq#can-i-use-flaresolverr-indexers\" target=\"_blank\" rel=\"noreferrer\">FlareSolverr</a> to access it.";
                        field.HelpLink = "https://wiki.servarr.com/prowlarr/faq#can-i-use-flaresolverr-indexers";
                        break;
                    case "info_category_8000":
                        field.Label = $"About {definition.Name} Categories";
                        field.Value = $"{definition.Name} does not return categories in its search results. To sync to your apps, include 8000(Other) in your Apps' Sync Categories.";
                        field.HelpLink = "https://wiki.servarr.com/prowlarr/faq#prowlarr-will-not-sync-x-indexer-to-app";
                        break;
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
