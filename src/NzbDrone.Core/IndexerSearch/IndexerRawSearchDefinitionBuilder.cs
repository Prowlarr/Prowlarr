using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.IndexerSearch
{
    internal static class IndexerRawSearchDefinitionBuilder
    {
        public static IndexerDefinition Build(IndexerDefinition definition)
        {
            if (definition?.Settings == null)
            {
                return definition;
            }

            var clonedSettings = CloneSettings(definition.Settings);
            ApplyConstraintOverrides(clonedSettings);

            var clonedDefinition = CloneDefinition(definition, clonedSettings);

            if (clonedSettings is CardigannSettings cardigannSettings)
            {
                ApplyCardigannOverrides(cardigannSettings, clonedDefinition.ExtraFields);
            }

            return clonedDefinition;
        }

        private static IProviderConfig CloneSettings(IProviderConfig settings)
        {
            var serialized = JsonConvert.SerializeObject(settings);
            return (IProviderConfig)JsonConvert.DeserializeObject(serialized, settings.GetType());
        }

        private static IndexerDefinition CloneDefinition(IndexerDefinition definition, IProviderConfig settings)
        {
            return new IndexerDefinition
            {
                Id = definition.Id,
                Name = definition.Name,
                ImplementationName = definition.ImplementationName,
                Implementation = definition.Implementation,
                ConfigContract = definition.ConfigContract,
                Enable = definition.Enable,
                Message = definition.Message,
                Tags = definition.Tags != null ? new HashSet<int>(definition.Tags) : new HashSet<int>(),
                Settings = settings,
                IndexerUrls = definition.IndexerUrls,
                LegacyUrls = definition.LegacyUrls,
                Description = definition.Description,
                Encoding = definition.Encoding,
                Language = definition.Language,
                Protocol = definition.Protocol,
                Privacy = definition.Privacy,
                SupportsRss = definition.SupportsRss,
                SupportsSearch = definition.SupportsSearch,
                SupportsRedirect = definition.SupportsRedirect,
                SupportsPagination = definition.SupportsPagination,
                Capabilities = definition.Capabilities,
                Priority = definition.Priority,
                Redirect = definition.Redirect,
                DownloadClientId = definition.DownloadClientId,
                Added = definition.Added,
                AppProfileId = definition.AppProfileId,
                AppProfile = definition.AppProfile,
                ExtraFields = definition.ExtraFields?.Select(CloneExtraField).ToList() ?? new List<SettingsField>()
            };
        }

        private static SettingsField CloneExtraField(SettingsField field)
        {
            return new SettingsField
            {
                Name = field.Name,
                Type = field.Type,
                Label = field.Label,
                Default = field.Default,
                Defaults = field.Defaults?.ToArray(),
                Options = field.Options != null ? new Dictionary<string, string>(field.Options) : null
            };
        }

        private static void ApplyConstraintOverrides(object target)
        {
            if (target == null)
            {
                return;
            }

            var targetType = target.GetType();
            object defaults = null;

            try
            {
                defaults = Activator.CreateInstance(targetType);
            }
            catch
            {
                // Best-effort only. If a type cannot be constructed we can still apply explicit false/null resets.
            }

            foreach (var property in targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var currentValue = property.GetValue(target);
                var defaultValue = defaults != null ? property.GetValue(defaults) : null;

                if (TryGetResetBehavior(property, out var resetBehavior))
                {
                    property.SetValue(target, BuildResetValue(property.PropertyType, resetBehavior, defaultValue));
                    continue;
                }

                if (ShouldRecurse(property.PropertyType, currentValue))
                {
                    ApplyConstraintOverrides(currentValue);
                }
            }
        }

        private static bool TryGetResetBehavior(PropertyInfo property, out SearchConstraintResetBehavior resetBehavior)
        {
            var explicitAttribute = property.GetCustomAttribute<SearchConstraintAttribute>();
            if (explicitAttribute != null)
            {
                resetBehavior = explicitAttribute.ResetBehavior;
                return true;
            }

            if (property.Name.EndsWith("Only", StringComparison.OrdinalIgnoreCase))
            {
                resetBehavior = property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?)
                    ? SearchConstraintResetBehavior.False
                    : SearchConstraintResetBehavior.Default;
                return true;
            }

            if (property.Name.Contains("UseFreeleechToken", StringComparison.OrdinalIgnoreCase))
            {
                resetBehavior = SearchConstraintResetBehavior.Default;
                return true;
            }

            resetBehavior = default;
            return false;
        }

        private static object BuildResetValue(Type propertyType, SearchConstraintResetBehavior resetBehavior, object defaultValue)
        {
            return resetBehavior switch
            {
                SearchConstraintResetBehavior.False when propertyType == typeof(bool?) => (bool?)false,
                SearchConstraintResetBehavior.False => false,
                SearchConstraintResetBehavior.Empty => string.Empty,
                SearchConstraintResetBehavior.Null => null,
                _ => defaultValue ?? (propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null)
            };
        }

        private static bool ShouldRecurse(Type propertyType, object currentValue)
        {
            if (currentValue == null || propertyType == typeof(string))
            {
                return false;
            }

            if (typeof(IDictionary).IsAssignableFrom(propertyType) || typeof(IEnumerable).IsAssignableFrom(propertyType))
            {
                return false;
            }

            return propertyType.IsClass;
        }

        private static void ApplyCardigannOverrides(CardigannSettings settings, List<SettingsField> extraFields)
        {
            if (settings.ExtraFieldData == null || extraFields == null)
            {
                return;
            }

            foreach (var field in extraFields)
            {
                if (!ShouldResetCardigannField(field))
                {
                    continue;
                }

                settings.ExtraFieldData[field.Name] = GetCardigannResetValue(field);
            }
        }

        private static bool ShouldResetCardigannField(SettingsField field)
        {
            var combined = $"{field.Name} {field.Label}".ToLowerInvariant();

            return combined.Contains("freeleech") ||
                   combined.Contains("freeload") ||
                   combined.Contains("limited") ||
                   (combined.Contains("token") && combined.Contains("free"));
        }

        private static object GetCardigannResetValue(SettingsField field)
        {
            if (string.Equals(field.Type, "checkbox", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (field.Default.IsNotNullOrWhiteSpace())
            {
                if (int.TryParse(field.Default, out var number))
                {
                    return number;
                }

                if (bool.TryParse(field.Default, out var boolean))
                {
                    return boolean;
                }

                return field.Default;
            }

            return string.Equals(field.Type, "select", StringComparison.OrdinalIgnoreCase) ? string.Empty : null;
        }
    }
}
