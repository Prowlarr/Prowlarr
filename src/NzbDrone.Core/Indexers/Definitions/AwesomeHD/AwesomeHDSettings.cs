using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.AwesomeHD
{
    public class AwesomeHDSettingsValidator : AbstractValidator<AwesomeHDSettings>
    {
        public AwesomeHDSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.Passkey).NotEmpty();
        }
    }

    public class AwesomeHDSettings : IIndexerSettings
    {
        private static readonly AwesomeHDSettingsValidator Validator = new AwesomeHDSettingsValidator();

        public AwesomeHDSettings()
        {
            BaseUrl = "https://awesome-hd.me";
            MultiLanguages = new List<int>();
        }

        [FieldDefinition(0, Label = "API URL", Advanced = true, HelpText = "Do not change this unless you know what you're doing. Since you Passkey will be sent to that host.")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Type = FieldType.Select, SelectOptions = typeof(LanguageFieldConverter), Label = "Multi Languages", HelpText = "What languages are normally in a multi release on this indexer?", Advanced = true)]
        public IEnumerable<int> MultiLanguages { get; set; }

        [FieldDefinition(2, Label = "Passkey", Privacy = PrivacyLevel.ApiKey)]
        public string Passkey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
