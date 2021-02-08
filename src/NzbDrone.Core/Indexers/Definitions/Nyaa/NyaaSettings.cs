using System.Text.RegularExpressions;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Nyaa
{
    public class NyaaSettingsValidator : AbstractValidator<NyaaSettings>
    {
        public NyaaSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.AdditionalParameters).Matches("(&[a-z]+=[a-z0-9_]+)*", RegexOptions.IgnoreCase);
        }
    }

    public class NyaaSettings : IIndexerSettings
    {
        private static readonly NyaaSettingsValidator Validator = new NyaaSettingsValidator();

        public NyaaSettings()
        {
            BaseUrl = "";
            AdditionalParameters = "";
        }

        [FieldDefinition(0, Label = "Website URL")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Additional Parameters", Advanced = true, HelpText = "Please note if you change the category you will have to add required/restricted rules about the subgroups to avoid foreign language releases.")]
        public string AdditionalParameters { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
