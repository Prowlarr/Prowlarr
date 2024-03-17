using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers
{
    public class IndexerTorrentSettingsValidator : AbstractValidator<IndexerTorrentBaseSettings>
    {
        public IndexerTorrentSettingsValidator(double seedRatioMinimum = 0.0, int seedTimeMinimum = 0, int seasonPackSeedTimeMinimum = 0)
        {
            RuleFor(c => c.AppMinimumSeeders).GreaterThan(0)
                .When(c => c.AppMinimumSeeders.HasValue)
                .AsWarning().WithMessage("Should be greater than zero");

            RuleFor(c => c.SeedRatio).GreaterThan(0.0)
                .When(c => c.SeedRatio.HasValue)
                .AsWarning().WithMessage("Should be greater than zero");

            RuleFor(c => c.SeedTime).GreaterThan(0)
                .When(c => c.SeedTime.HasValue)
                .AsWarning().WithMessage("Should be greater than zero");

            RuleFor(c => c.PackSeedTime).GreaterThan(0)
                .When(c => c.PackSeedTime.HasValue)
                .AsWarning().WithMessage("Should be greater than zero");

            if (seedRatioMinimum != 0.0)
            {
                RuleFor(c => c.SeedRatio).GreaterThanOrEqualTo(seedRatioMinimum)
                    .When(c => c.SeedRatio > 0.0)
                    .AsWarning()
                    .WithMessage($"Under {seedRatioMinimum} leads to H&R");
            }

            if (seedTimeMinimum != 0)
            {
                RuleFor(c => c.SeedTime).GreaterThanOrEqualTo(seedTimeMinimum)
                    .When(c => c.SeedTime > 0)
                    .AsWarning()
                    .WithMessage($"Under {seedTimeMinimum} leads to H&R");
            }

            if (seasonPackSeedTimeMinimum != 0)
            {
                RuleFor(c => c.PackSeedTime).GreaterThanOrEqualTo(seasonPackSeedTimeMinimum)
                    .When(c => c.PackSeedTime > 0)
                    .AsWarning()
                    .WithMessage($"Under {seasonPackSeedTimeMinimum} leads to H&R");
            }
        }
    }

    public class IndexerTorrentBaseSettings
    {
        [FieldDefinition(1, Type = FieldType.Number, Label = "IndexerSettingsAppsMinimumSeeders", HelpText = "IndexerSettingsAppsMinimumSeedersHelpText", Advanced = true)]
        public int? AppMinimumSeeders { get; set; }

        [FieldDefinition(2, Type = FieldType.Textbox, Label = "IndexerSettingsSeedRatio", HelpText = "IndexerSettingsSeedRatioHelpText")]
        public double? SeedRatio { get; set; }

        [FieldDefinition(3, Type = FieldType.Number, Label = "IndexerSettingsSeedTime", Unit = "minutes", HelpText = "IndexerSettingsSeedTimeHelpText", Advanced = true)]
        public int? SeedTime { get; set; }

        [FieldDefinition(4, Type = FieldType.Number, Label = "IndexerSettingsPackSeedTime", HelpText = "IndexerSettingsPackSeedTimeIndexerHelpText", Unit = "minutes", Advanced = true)]
        public int? PackSeedTime { get; set; }
    }
}
