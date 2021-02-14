using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.TorrentPotato
{
    public class TorrentPotatoSettingsValidator : AbstractValidator<TorrentPotatoSettings>
    {
        public TorrentPotatoSettingsValidator()
        {
        }
    }

    public class TorrentPotatoSettings : IProviderConfig
    {
        private static readonly TorrentPotatoSettingsValidator Validator = new TorrentPotatoSettingsValidator();

        public TorrentPotatoSettings()
        {
        }

        [FieldDefinition(1, Label = "Username", HelpText = "The username you use at your indexer.", Privacy = PrivacyLevel.UserName)]
        public string User { get; set; }

        [FieldDefinition(2, Label = "Passkey", HelpText = "The password you use at your Indexer.", Privacy = PrivacyLevel.Password)]
        public string Passkey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
