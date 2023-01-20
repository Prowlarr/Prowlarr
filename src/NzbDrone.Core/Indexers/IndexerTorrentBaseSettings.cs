using System;
using FluentValidation;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Indexers
{
    public class IndexerTorrentSettingsValidator : AbstractValidator<IndexerTorrentBaseSettings>
    {
        public IndexerTorrentSettingsValidator(double seedRatioMinimum = 0.0, int seedTimeMinimum = 0, int seasonPackSeedTimeMinimum = 0)
        {
            RuleFor(c => c.AppMinimumSeeders).GreaterThan(0)
                .When(c => c.AppMinimumSeeders.HasValue)
                .WithMessage("Should be greater than zero");

            RuleFor(c => c.SeedRatio).GreaterThan(0.0)
                .When(c => c.SeedRatio.HasValue)
                .WithMessage("Should be greater than zero");

            RuleFor(c => c.SeedTime).GreaterThan(0)
                .When(c => c.SeedTime.HasValue)
                .WithMessage("Should be greater than zero");

            RuleFor(c => c.PackSeedTime).GreaterThan(0)
                .When(c => c.PackSeedTime.HasValue)
                .WithMessage("Should be greater than zero");

            if (seedRatioMinimum != 0.0)
            {
                RuleFor(c => c.SeedRatio).GreaterThanOrEqualTo(seedRatioMinimum)
                    .When(c => c.SeedRatio > 0.0)
                    .WithMessage($"Under {seedRatioMinimum} leads to H&R");
            }

            if (seedTimeMinimum != 0)
            {
                RuleFor(c => c.SeedTime).GreaterThanOrEqualTo(seedTimeMinimum)
                    .When(c => c.SeedTime > 0)
                    .WithMessage($"Under {seedTimeMinimum} leads to H&R");
            }

            if (seasonPackSeedTimeMinimum != 0)
            {
                RuleFor(c => c.PackSeedTime).GreaterThanOrEqualTo(seasonPackSeedTimeMinimum)
                    .When(c => c.PackSeedTime > 0)
                    .WithMessage($"Under {seasonPackSeedTimeMinimum} leads to H&R");
            }
        }
    }

    public class IndexerTorrentBaseSettings
    {
        private static readonly IndexerTorrentSettingsValidator Validator = new ();

        [FieldDefinition(1, Type = FieldType.Number, Label = "Apps Minimum Seeders", HelpText = "Minimum seeders required by the Applications for the indexer to grab, empty is Sync profile's default", Advanced = true)]
        public int? AppMinimumSeeders { get; set; }

        [FieldDefinition(2, Type = FieldType.Textbox, Label = "Seed Ratio", HelpText = "The ratio a torrent should reach before stopping, empty is app's default", Advanced = true)]
        public double? SeedRatio { get; set; }

        [FieldDefinition(3, Type = FieldType.Number, Label = "Seed Time", HelpText = "The time a torrent should be seeded before stopping, empty is app's default", Unit = "minutes", Advanced = true)]
        public int? SeedTime { get; set; }

        [FieldDefinition(4, Type = FieldType.Number, Label = "Pack Seed Time", HelpText = "The time a pack (season or discography) torrent should be seeded before stopping, empty is app's default", Unit = "minutes", Advanced = true)]
        public int? PackSeedTime { get; set; }
    }
}
