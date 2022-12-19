using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Settings
{
    public class NoAuthSettingsValidator : AbstractValidator<NoAuthTorrentBaseSettings>
    {
        public NoAuthSettingsValidator()
        {
            RuleFor(x => x.BaseSettings).SetValidator(new IndexerCommonSettingsValidator());
            RuleFor(x => x.TorrentBaseSettings).SetValidator(new IndexerTorrentSettingsValidator());
        }
    }

    public class NoAuthTorrentBaseSettings : ITorrentIndexerSettings
    {
        private static readonly NoAuthSettingsValidator Validator = new NoAuthSettingsValidator();

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(10)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        [FieldDefinition(11)]
        public IndexerTorrentBaseSettings TorrentBaseSettings { get; set; } = new IndexerTorrentBaseSettings();

        public virtual NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
