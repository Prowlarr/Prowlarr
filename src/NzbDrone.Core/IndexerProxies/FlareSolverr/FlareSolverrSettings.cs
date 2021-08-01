using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.IndexerProxies.FlareSolverr
{
    public class FlareSolverrSettingsValidator : AbstractValidator<FlareSolverrSettings>
    {
        public FlareSolverrSettingsValidator()
        {
            RuleFor(c => c.Host).NotEmpty();
        }
    }

    public class FlareSolverrSettings : IIndexerProxySettings
    {
        private static readonly FlareSolverrSettingsValidator Validator = new FlareSolverrSettingsValidator();

        public FlareSolverrSettings()
        {
            Host = "http://localhost:8191/";
        }

        [FieldDefinition(0, Label = "Host")]
        public string Host { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
