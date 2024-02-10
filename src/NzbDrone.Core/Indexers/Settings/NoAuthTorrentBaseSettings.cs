using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Settings
{
    public class NoAuthSettingsValidator<T> : AbstractValidator<T>
        where T : NoAuthTorrentBaseSettings
    {
        public NoAuthSettingsValidator()
        {
            RuleFor(c => c.BaseSettings).SetValidator(new IndexerCommonSettingsValidator());
            RuleFor(c => c.TorrentBaseSettings).SetValidator(new IndexerTorrentSettingsValidator());
        }
    }

    public class NoAuthTorrentBaseSettings : ITorrentIndexerSettings
    {
        private static readonly NoAuthSettingsValidator<NoAuthTorrentBaseSettings> Validator = new ();

        [FieldDefinition(1, Label = "IndexerSettingsBaseUrl", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "IndexerSettingsBaseUrlHelpText")]
        public string BaseUrl { get; set; }

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
