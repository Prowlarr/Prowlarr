using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.TorrentPotato
{
    public class TorrentPotatoSettingsValidator : AbstractValidator<TorrentPotatoSettings>
    {
        public TorrentPotatoSettingsValidator()
        {
        }
    }

    public class TorrentPotatoSettings : IIndexerSettings
    {
        private static readonly TorrentPotatoSettingsValidator Validator = new TorrentPotatoSettingsValidator();

        public TorrentPotatoSettings()
        {
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Indexer Username", Privacy = PrivacyLevel.UserName)]
        public string User { get; set; }

        [FieldDefinition(3, Label = "Passkey", HelpText = "Indexer Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Passkey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
