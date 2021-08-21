using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.IndexerProxies.Socks4
{
    public class Socks4SettingsValidator : AbstractValidator<Socks4Settings>
    {
        public Socks4SettingsValidator()
        {
            RuleFor(c => c.Host).NotEmpty();
            RuleFor(c => c.Port).NotEmpty();
        }
    }

    public class Socks4Settings : IIndexerProxySettings
    {
        private static readonly Socks4SettingsValidator Validator = new Socks4SettingsValidator();

        public Socks4Settings()
        {
            Username = "";
            Password = "";
        }

        [FieldDefinition(0, Label = "Host")]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port")]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
