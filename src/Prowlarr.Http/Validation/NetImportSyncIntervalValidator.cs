using FluentValidation.Validators;

namespace Prowlarr.Http.Validation
{
    public class ImportListSyncIntervalValidator : PropertyValidator
    {
        protected override string GetDefaultMessageTemplate() => "Must be between 10 and 1440 or 0 to disable";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            var value = (int)context.PropertyValue;

            if (value == 0)
            {
                return true;
            }

            return value is >= 10 and <= 1440;
        }
    }
}
