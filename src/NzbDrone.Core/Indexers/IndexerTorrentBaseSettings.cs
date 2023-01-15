using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers
{
    public class IndexerTorrentSettingsValidator : AbstractValidator<IndexerTorrentBaseSettings>
    {
        public IndexerTorrentSettingsValidator(double seedRatioMinimum = 0.0, int seedTimeMinimum = 0, int seasonPackSeedTimeMinimum = 0)
        {
            RuleFor(c => c.SeedRatio).GreaterThan(0.0)
                .When(c => c.SeedRatio.HasValue)
                .AsWarning().WithMessage("Should be greater than zero");

            RuleFor(c => c.SeedTime).GreaterThan(0)
                .When(c => c.SeedTime.HasValue)
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
        }
    }

    public class IndexerTorrentBaseSettings
    {
        private static readonly IndexerTorrentSettingsValidator Validator = new IndexerTorrentSettingsValidator();

        [FieldDefinition(1, Type = FieldType.Textbox, Label = "Seed Ratio", HelpText = "The ratio a torrent should reach before stopping, empty is app's default", Advanced = true)]
        public double? SeedRatio { get; set; }

        [FieldDefinition(2, Type = FieldType.Number, Label = "Seed Time", HelpText = "The time a torrent should be seeded before stopping, empty is app's default", Unit = "minutes", Advanced = true)]
        public int? SeedTime { get; set; }

        [FieldDefinition(3, Type = FieldType.Number, Label = "Pack Seed Time", HelpText = "The time a pack (season or discography) torrent should be seeded before stopping, empty is app's default", Unit = "minutes", Advanced = true)]
        public int? PackSeedTime { get; set; }
    }
}
