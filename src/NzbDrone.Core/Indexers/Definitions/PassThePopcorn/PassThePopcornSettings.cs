using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.PassThePopcorn
{
    public class PassThePopcornSettingsValidator : AbstractValidator<PassThePopcornSettings>
    {
        public PassThePopcornSettingsValidator()
        {
            RuleFor(c => c.APIUser).NotEmpty();
            RuleFor(c => c.APIKey).NotEmpty();
        }
    }

    public class PassThePopcornSettings : IProviderConfig
    {
        private static readonly PassThePopcornSettingsValidator Validator = new PassThePopcornSettingsValidator();

        public PassThePopcornSettings()
        {
        }

        [FieldDefinition(0, Label = "APIUser", HelpText = "These settings are found in your PassThePopcorn security settings (Edit Profile > Security).", Privacy = PrivacyLevel.UserName)]
        public string APIUser { get; set; }

        [FieldDefinition(1, Label = "APIKey", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string APIKey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
