using System;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Torznab
{
    public class TorznabSettingsValidator : AbstractValidator<TorznabSettings>
    {
        private static readonly string[] ApiKeyWhiteList = Array.Empty<string>();

        private static bool ShouldHaveApiKey(TorznabSettings settings)
        {
            return settings.BaseUrl != null && ApiKeyWhiteList.Any(c => settings.BaseUrl.ToLowerInvariant().Contains(c));
        }

        private static readonly Regex AdditionalParametersRegex = new Regex(@"(&.+?\=.+?)+", RegexOptions.Compiled);

        public TorznabSettingsValidator()
        {
            RuleFor(x => x.BaseSettings).SetValidator(new IndexerCommonSettingsValidator());
            RuleFor(x => x.TorrentBaseSettings).SetValidator(new IndexerTorrentSettingsValidator());
            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.ApiPath).ValidUrlBase("/api");
            RuleFor(c => c.ApiKey).NotEmpty().When(ShouldHaveApiKey);
            RuleFor(c => c.AdditionalParameters).Matches(AdditionalParametersRegex)
                                                .When(c => !c.AdditionalParameters.IsNullOrWhiteSpace());
        }
    }

    public class TorznabSettings : NewznabSettings, ITorrentIndexerSettings
    {
        private static readonly TorznabSettingsValidator Validator = new ();

        [FieldDefinition(3)]
        public IndexerTorrentBaseSettings TorrentBaseSettings { get; set; } = new ();

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
