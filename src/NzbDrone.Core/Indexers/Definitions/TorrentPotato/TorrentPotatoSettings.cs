using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.TorrentPotato
{
    public class TorrentPotatoSettingsValidator : NoAuthSettingsValidator<TorrentPotatoSettings>
    {
        public TorrentPotatoSettingsValidator()
        {
            RuleFor(c => c.User).NotEmpty();
            RuleFor(c => c.Passkey).NotEmpty();
        }
    }

    public class TorrentPotatoSettings : NoAuthTorrentBaseSettings
    {
        private static readonly TorrentPotatoSettingsValidator Validator = new ();

        [FieldDefinition(2, Label = "Username", HelpText = "Indexer Username", Privacy = PrivacyLevel.UserName)]
        public string User { get; set; }

        [FieldDefinition(3, Label = "Passkey", HelpText = "Indexer Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Passkey { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
