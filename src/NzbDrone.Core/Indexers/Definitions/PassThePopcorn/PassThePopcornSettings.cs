using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.PassThePopcorn
{
    public class PassThePopcornSettingsValidator : NoAuthSettingsValidator<PassThePopcornSettings>
    {
        public PassThePopcornSettingsValidator()
        {
            RuleFor(c => c.APIUser).NotEmpty();
            RuleFor(c => c.APIKey).NotEmpty();
        }
    }

    public class PassThePopcornSettings : NoAuthTorrentBaseSettings
    {
        private static readonly PassThePopcornSettingsValidator Validator = new ();

        [FieldDefinition(2, Label = "IndexerSettingsApiUser", HelpText = "IndexerPassThePopcornSettingsApiUserHelpText", Privacy = PrivacyLevel.UserName)]
        public string APIUser { get; set; }

        [FieldDefinition(3, Label = "ApiKey", HelpText = "IndexerPassThePopcornSettingsApiKeyHelpText", Privacy = PrivacyLevel.ApiKey)]
        public string APIKey { get; set; }

        [FieldDefinition(4, Label = "IndexerSettingsFreeleechOnly", HelpText = "IndexerPassThePopcornSettingsFreeleechOnlyHelpText", Type = FieldType.Checkbox)]
        public bool FreeleechOnly { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
