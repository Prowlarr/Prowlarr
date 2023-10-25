using FluentValidation.Validators;
using NzbDrone.Core.Profiles;

namespace NzbDrone.Core.Validation
{
    public class AppProfileExistsValidator : PropertyValidator
    {
        private readonly IAppProfileService _appProfileService;

        public AppProfileExistsValidator(IAppProfileService appProfileService)
        {
            _appProfileService = appProfileService;
        }

        protected override string GetDefaultMessageTemplate() => "App Profile does not exist";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            return context?.PropertyValue == null || _appProfileService.Exists((int)context.PropertyValue);
        }
    }
}
