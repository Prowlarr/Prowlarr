using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Configuration.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Localization
{
    public interface ILocalizationService
    {
        Dictionary<string, string> GetLocalizationDictionary();

        string GetLocalizedString(string phrase);
        string GetLocalizedString(string phrase, Dictionary<string, object> tokens);
        IEnumerable<LocalizationOption> GetLocalizationOptions();
    }

    public class LocalizationService : ILocalizationService, IHandleAsync<ConfigSavedEvent>
    {
        private const string DefaultCulture = "en";
        private static readonly Regex TokenRegex = new Regex(@"(?:\{)(?<token>[a-z0-9]+)(?:\})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private readonly ICached<Dictionary<string, string>> _cache;

        private readonly IConfigService _configService;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly Logger _logger;

        public LocalizationService(IConfigService configService,
                                   IAppFolderInfo appFolderInfo,
                                   ICacheManager cacheManager,
                                   Logger logger)
        {
            _configService = configService;
            _appFolderInfo = appFolderInfo;
            _cache = cacheManager.GetCache<Dictionary<string, string>>(typeof(Dictionary<string, string>), "localization");
            _logger = logger;
        }

        public Dictionary<string, string> GetLocalizationDictionary()
        {
            var language = GetLanguageFileName();

            return GetLocalizationDictionary(language);
        }

        public string GetLocalizedString(string phrase)
        {
            return GetLocalizedString(phrase, new Dictionary<string, object>());
        }

        public string GetLocalizedString(string phrase, Dictionary<string, object> tokens)
        {
            var language = GetLanguageFileName();

            if (string.IsNullOrEmpty(phrase))
            {
                throw new ArgumentNullException(nameof(phrase));
            }

            if (language == null)
            {
                language = DefaultCulture;
            }

            var dictionary = GetLocalizationDictionary(language);

            if (dictionary.TryGetValue(phrase, out var value))
            {
                return ReplaceTokens(value, tokens);
            }

            return phrase;
        }

        public IEnumerable<LocalizationOption> GetLocalizationOptions()
        {
            yield return new LocalizationOption("العربية", "ar");
            yield return new LocalizationOption("Български", "bg");
            yield return new LocalizationOption("বাংলা (বাংলাদেশ)", "bn");
            yield return new LocalizationOption("Bosanski", "bs");
            yield return new LocalizationOption("Català", "ca");
            yield return new LocalizationOption("Čeština", "cs");
            yield return new LocalizationOption("Dansk", "da");
            yield return new LocalizationOption("Deutsch", "de");
            yield return new LocalizationOption("English", "en");
            yield return new LocalizationOption("Ελληνικά", "el");
            yield return new LocalizationOption("Español", "es");
            yield return new LocalizationOption("Español (Latino)", "es_MX");
            yield return new LocalizationOption("Eesti", "et");
            yield return new LocalizationOption("فارسی", "fa");
            yield return new LocalizationOption("Suomi", "fi");
            yield return new LocalizationOption("Français", "fr");
            yield return new LocalizationOption("עִבְרִית", "he");
            yield return new LocalizationOption("हिन्दी", "hi");
            yield return new LocalizationOption("Hrvatski", "hr");
            yield return new LocalizationOption("Magyar", "hu");
            yield return new LocalizationOption("Indonesia", "id");
            yield return new LocalizationOption("Íslenska", "is");
            yield return new LocalizationOption("Italiano", "it");
            yield return new LocalizationOption("日本語", "ja");
            yield return new LocalizationOption("한국어", "ko");
            yield return new LocalizationOption("Lietuvių", "lt");
            yield return new LocalizationOption("Norsk bokmål", "nb_NO");
            yield return new LocalizationOption("Nederlands", "nl");
            yield return new LocalizationOption("Polski", "pl");
            yield return new LocalizationOption("Português", "pt");
            yield return new LocalizationOption("Português (Brasil)", "pt_BR");
            yield return new LocalizationOption("Românește", "ro");
            yield return new LocalizationOption("Русский", "ru");
            yield return new LocalizationOption("Slovenčina", "sk");
            yield return new LocalizationOption("српски", "sr");
            yield return new LocalizationOption("Svenska", "sv");
            yield return new LocalizationOption("தமிழ்", "ta");
            yield return new LocalizationOption("ภาษาไทย", "th");
            yield return new LocalizationOption("Türkçe", "tr");
            yield return new LocalizationOption("Українська", "uk");
            yield return new LocalizationOption("Tiếng Việt", "vi");
            yield return new LocalizationOption("汉语 (简化字)", "zh_CN");
            yield return new LocalizationOption("漢語 (繁体字)", "zh_TW");
        }

        public string GetLanguageIdentifier()
        {
            return GetLocalizationOptions().FirstOrDefault(l => l.Value == _configService.UILanguage)?.Value ?? DefaultCulture;
        }

        private string ReplaceTokens(string input, Dictionary<string, object> tokens)
        {
            tokens.TryAdd("appName", "Prowlarr");

            return TokenRegex.Replace(input, match =>
            {
                var tokenName = match.Groups["token"].Value;

                tokens.TryGetValue(tokenName, out var token);

                return token?.ToString() ?? $"{{{tokenName}}}";
            });
        }

        private string GetLanguageFileName()
        {
            return GetLanguageIdentifier().Replace("-", "_").ToLowerInvariant();
        }

        private Dictionary<string, string> GetLocalizationDictionary(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentNullException(nameof(language));
            }

            var startupFolder = _appFolderInfo.StartUpFolder;

            var prefix = Path.Combine(startupFolder, "Localization", "Core");
            var key = prefix + language;

            return _cache.Get("localization", () => GetDictionary(prefix, language, DefaultCulture + ".json").GetAwaiter().GetResult());
        }

        private async Task<Dictionary<string, string>> GetDictionary(string prefix, string culture, string baseFilename)
        {
            if (string.IsNullOrEmpty(culture))
            {
                throw new ArgumentNullException(nameof(culture));
            }

            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var baseFilenamePath = Path.Combine(prefix, baseFilename);

            var alternativeFilenamePath = Path.Combine(prefix, GetResourceFilename(culture));

            await CopyInto(dictionary, baseFilenamePath).ConfigureAwait(false);

            if (culture.Contains('_'))
            {
                var languageBaseFilenamePath = Path.Combine(prefix, GetResourceFilename(culture.Split('_')[0]));
                await CopyInto(dictionary, languageBaseFilenamePath).ConfigureAwait(false);
            }

            await CopyInto(dictionary, alternativeFilenamePath).ConfigureAwait(false);

            return dictionary;
        }

        private async Task CopyInto(IDictionary<string, string> dictionary, string resourcePath)
        {
            if (!File.Exists(resourcePath))
            {
                _logger.Error("Missing translation/culture resource: {0}", resourcePath);
                return;
            }

            await using var fs = File.OpenRead(resourcePath);
            var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(fs);

            foreach (var key in dict.Keys)
            {
                dictionary[key] = dict[key];
            }
        }

        private static string GetResourceFilename(string culture)
        {
            var parts = culture.Split('_');

            if (parts.Length == 2)
            {
                culture = parts[0].ToLowerInvariant() + "_" + parts[1].ToUpperInvariant();
            }
            else
            {
                culture = culture.ToLowerInvariant();
            }

            return culture + ".json";
        }

        public void HandleAsync(ConfigSavedEvent message)
        {
            _cache.Clear();
        }
    }
}
