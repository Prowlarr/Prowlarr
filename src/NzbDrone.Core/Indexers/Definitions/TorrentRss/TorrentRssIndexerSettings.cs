using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.TorrentRss
{
    public class TorrentRssIndexerSettingsValidator : AbstractValidator<TorrentRssIndexerSettings>
    {
        public TorrentRssIndexerSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();

            RuleFor(c => c.BaseSettings).SetValidator(new IndexerCommonSettingsValidator());
            RuleFor(c => c.TorrentBaseSettings).SetValidator(new IndexerTorrentSettingsValidator());
        }
    }

    public class TorrentRssIndexerSettings : ITorrentIndexerSettings
    {
        private static readonly TorrentRssIndexerSettingsValidator Validator = new ();

        public TorrentRssIndexerSettings()
        {
            BaseUrl = string.Empty;
            AllowZeroSize = false;
        }

        [FieldDefinition(0, Label = "Full RSS Feed URL", HelpTextWarning = "To sync to your apps you will need to include the 8000(Other) in the Sync Categories", HelpLink = "https://wiki.servarr.com/en/prowlarr/faq#can-i-add-any-generic-torrent-rss-feed")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Cookie", HelpText = "If your site requires a login cookie to access the RSS, you'll have to retrieve it via a browser.", HelpLink = "https://wiki.servarr.com/useful-tools#finding-cookies")]
        public string Cookie { get; set; }

        [FieldDefinition(2, Type = FieldType.Checkbox, Label = "Allow Zero Size", HelpText="Enabling this will allow you to use feeds that don't specify release size, but be careful, size related checks will not be performed.")]
        public bool AllowZeroSize { get; set; }

        [FieldDefinition(3, Type = FieldType.Number, Label = "Default Release Size", HelpText="Add a default size for feeds with missing sizes.", Unit = "MB", Advanced = true)]
        public double? DefaultReleaseSize { get; set; }

        [FieldDefinition(20)]
        public IndexerBaseSettings BaseSettings { get; set; } = new ();

        [FieldDefinition(21)]
        public IndexerTorrentBaseSettings TorrentBaseSettings { get; set; } = new ();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
