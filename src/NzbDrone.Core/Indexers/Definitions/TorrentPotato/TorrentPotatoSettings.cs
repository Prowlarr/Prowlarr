using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.TorrentPotato
{
    public class TorrentPotatoSettingsValidator : AbstractValidator<TorrentPotatoSettings>
    {
        public TorrentPotatoSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
        }
    }

    public class TorrentPotatoSettings : IIndexerSettings
    {
        private static readonly TorrentPotatoSettingsValidator Validator = new TorrentPotatoSettingsValidator();

        public TorrentPotatoSettings()
        {
            BaseUrl = "http://127.0.0.1";
            MultiLanguages = new List<int>();
        }

        [FieldDefinition(0, Label = "API URL", HelpText = "URL to TorrentPotato api.")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Username", HelpText = "The username you use at your indexer.", Privacy = PrivacyLevel.UserName)]
        public string User { get; set; }

        [FieldDefinition(2, Label = "Passkey", HelpText = "The password you use at your Indexer.", Privacy = PrivacyLevel.Password)]
        public string Passkey { get; set; }

        [FieldDefinition(3, Type = FieldType.Select, SelectOptions = typeof(LanguageFieldConverter), Label = "Multi Languages", HelpText = "What languages are normally in a multi release on this indexer?", Advanced = true)]
        public IEnumerable<int> MultiLanguages { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
