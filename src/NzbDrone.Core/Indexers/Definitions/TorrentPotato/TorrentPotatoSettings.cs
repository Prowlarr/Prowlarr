using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.TorrentPotato
{
    public class TorrentPotatoSettingsValidator : AbstractValidator<TorrentPotatoSettings>
    {
        public TorrentPotatoSettingsValidator()
        {
            RuleFor(c => c.User).NotEmpty();
            RuleFor(c => c.Passkey).NotEmpty();

            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.BaseSettings).SetValidator(new IndexerCommonSettingsValidator());
            RuleFor(c => c.TorrentBaseSettings).SetValidator(new IndexerTorrentSettingsValidator());
        }
    }

    public class TorrentPotatoSettings : ITorrentIndexerSettings
    {
        private static readonly TorrentPotatoSettingsValidator Validator = new ();

        [FieldDefinition(0, Label = "API URL", HelpText = "URL to TorrentPotato API")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Indexer Username", Privacy = PrivacyLevel.UserName)]
        public string User { get; set; }

        [FieldDefinition(3, Label = "Passkey", HelpText = "Indexer Passkey", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Passkey { get; set; }

        [FieldDefinition(20)]
        public IndexerBaseSettings BaseSettings { get; set; } = new ();

        [FieldDefinition(21)]
        public IndexerTorrentBaseSettings TorrentBaseSettings { get; set; } = new ();

        public virtual NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
