using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Http.Proxy;
using NzbDrone.Core.Configuration.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Security;

namespace NzbDrone.Core.Configuration
{
    public enum ConfigKey
    {
        DownloadedMoviesFolder
    }

    public class ConfigService : IConfigService
    {
        private readonly IConfigRepository _repository;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;
        private static Dictionary<string, string> _cache;

        public ConfigService(IConfigRepository repository, IEventAggregator eventAggregator, Logger logger)
        {
            _repository = repository;
            _eventAggregator = eventAggregator;
            _logger = logger;
            _cache = new Dictionary<string, string>();
        }

        private Dictionary<string, object> AllWithDefaults()
        {
            var dict = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            var type = GetType();
            var properties = type.GetProperties();

            foreach (var propertyInfo in properties)
            {
                var value = propertyInfo.GetValue(this, null);
                dict.Add(propertyInfo.Name, value);
            }

            return dict;
        }

        public void SaveConfigDictionary(Dictionary<string, object> configValues)
        {
            var allWithDefaults = AllWithDefaults();

            foreach (var configValue in configValues)
            {
                allWithDefaults.TryGetValue(configValue.Key, out var currentValue);
                if (currentValue == null || configValue.Value == null)
                {
                    continue;
                }

                var equal = configValue.Value.ToString().Equals(currentValue.ToString());

                if (!equal)
                {
                    SetValue(configValue.Key, configValue.Value.ToString());
                }
            }

            _eventAggregator.PublishEvent(new ConfigSavedEvent());
        }

        public bool IsDefined(string key)
        {
            return _repository.Get(key.ToLower()) != null;
        }

        public int HistoryCleanupDays
        {
            get { return GetValueInt("HistoryCleanupDays", 30); }
            set { SetValue("HistoryCleanupDays", value); }
        }

        public bool LogIndexerResponse
        {
            get { return GetValueBoolean("LogIndexerResponse", false); }

            set { SetValue("LogIndexerResponse", value); }
        }

        public int FirstDayOfWeek
        {
            get { return GetValueInt("FirstDayOfWeek", (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek); }

            set { SetValue("FirstDayOfWeek", value); }
        }

        public string CalendarWeekColumnHeader
        {
            get { return GetValue("CalendarWeekColumnHeader", "ddd M/D"); }

            set { SetValue("CalendarWeekColumnHeader", value); }
        }

        public string ShortDateFormat
        {
            get { return GetValue("ShortDateFormat", "MMM D YYYY"); }

            set { SetValue("ShortDateFormat", value); }
        }

        public string LongDateFormat
        {
            get { return GetValue("LongDateFormat", "dddd, MMMM D YYYY"); }

            set { SetValue("LongDateFormat", value); }
        }

        public string TimeFormat
        {
            get { return GetValue("TimeFormat", "h(:mm)a"); }

            set { SetValue("TimeFormat", value); }
        }

        public bool ShowRelativeDates
        {
            get { return GetValueBoolean("ShowRelativeDates", true); }

            set { SetValue("ShowRelativeDates", value); }
        }

        public bool EnableColorImpairedMode
        {
            get { return GetValueBoolean("EnableColorImpairedMode", false); }

            set { SetValue("EnableColorImpairedMode", value); }
        }

        public string UILanguage
        {
            get { return GetValue("UILanguage", "en"); }

            set { SetValue("UILanguage", value); }
        }

        public string PlexClientIdentifier => GetValue("PlexClientIdentifier", Guid.NewGuid().ToString(), true);

        public string RijndaelPassphrase => GetValue("RijndaelPassphrase", Guid.NewGuid().ToString(), true);

        public string HmacPassphrase => GetValue("HmacPassphrase", Guid.NewGuid().ToString(), true);

        public string RijndaelSalt => GetValue("RijndaelSalt", Guid.NewGuid().ToString(), true);

        public string HmacSalt => GetValue("HmacSalt", Guid.NewGuid().ToString(), true);

        public string DownloadProtectionKey => GetValue("DownloadProtectionKey", Guid.NewGuid().ToString().Replace("-", ""), true);

        public bool ProxyEnabled => GetValueBoolean("ProxyEnabled", false);

        public ProxyType ProxyType => GetValueEnum<ProxyType>("ProxyType", ProxyType.Http);

        public string ProxyHostname => GetValue("ProxyHostname", string.Empty);

        public int ProxyPort => GetValueInt("ProxyPort", 8080);

        public string ProxyUsername => GetValue("ProxyUsername", string.Empty);

        public string ProxyPassword => GetValue("ProxyPassword", string.Empty);

        public string ProxyBypassFilter => GetValue("ProxyBypassFilter", string.Empty);

        public bool ProxyBypassLocalAddresses => GetValueBoolean("ProxyBypassLocalAddresses", true);

        public string BackupFolder => GetValue("BackupFolder", "Backups");

        public int BackupInterval => GetValueInt("BackupInterval", 7);

        public int BackupRetention => GetValueInt("BackupRetention", 28);

        public CertificateValidationType CertificateValidation =>
            GetValueEnum("CertificateValidation", CertificateValidationType.Enabled);

        public string ApplicationUrl => GetValue("ApplicationUrl", string.Empty);

        public bool TrustCgnatIpAddresses
        {
            get { return GetValueBoolean("TrustCgnatIpAddresses", false); }
            set { SetValue("TrustCgnatIpAddresses", value); }
        }

        private string GetValue(string key)
        {
            return GetValue(key, string.Empty);
        }

        private bool GetValueBoolean(string key, bool defaultValue = false)
        {
            return Convert.ToBoolean(GetValue(key, defaultValue));
        }

        private int GetValueInt(string key, int defaultValue = 0)
        {
            return Convert.ToInt32(GetValue(key, defaultValue));
        }

        private T GetValueEnum<T>(string key, T defaultValue)
        {
            return (T)Enum.Parse(typeof(T), GetValue(key, defaultValue), true);
        }

        public string GetValue(string key, object defaultValue, bool persist = false)
        {
            key = key.ToLowerInvariant();
            Ensure.That(key, () => key).IsNotNullOrWhiteSpace();

            EnsureCache();

            if (_cache.TryGetValue(key, out var dbValue) && dbValue != null && !string.IsNullOrEmpty(dbValue))
            {
                return dbValue;
            }

            _logger.Trace("Using default config value for '{0}' defaultValue:'{1}'", key, defaultValue);

            if (persist)
            {
                SetValue(key, defaultValue.ToString());
            }

            return defaultValue.ToString();
        }

        private void SetValue(string key, bool value)
        {
            SetValue(key, value.ToString());
        }

        private void SetValue(string key, int value)
        {
            SetValue(key, value.ToString());
        }

        private void SetValue(string key, Enum value)
        {
            SetValue(key, value.ToString().ToLower());
        }

        private void SetValue(string key, string value)
        {
            key = key.ToLowerInvariant();

            _logger.Trace("Writing Setting to database. Key:'{0}' Value:'{1}'", key, value);
            _repository.Upsert(key, value);

            ClearCache();
        }

        private void EnsureCache()
        {
            lock (_cache)
            {
                if (!_cache.Any())
                {
                    var all = _repository.All();
                    _cache = all.ToDictionary(c => c.Key.ToLower(), c => c.Value);
                }
            }
        }

        private static void ClearCache()
        {
            lock (_cache)
            {
                _cache = new Dictionary<string, string>();
            }
        }
    }
}
