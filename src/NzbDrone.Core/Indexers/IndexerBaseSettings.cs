using FluentValidation;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Indexers
{
    public class IndexerCommonSettingsValidator : AbstractValidator<IndexerBaseSettings>
    {
        public IndexerCommonSettingsValidator()
        {
            RuleFor(c => c.QueryLimit)
                .GreaterThan(0)
                .When(c => c.QueryLimit.HasValue)
                .WithMessage("Should be greater than zero");

            RuleFor(c => c.GrabLimit)
                .GreaterThan(0)
                .When(c => c.GrabLimit.HasValue)
                .WithMessage("Should be greater than zero");
        }
    }

    public class IndexerBaseSettings
    {
        [FieldDefinition(1, Type = FieldType.Number, Label = "IndexerSettingsQueryLimit", HelpText = "IndexerSettingsQueryLimitHelpText", Advanced = true)]
        public int? QueryLimit { get; set; }

        [FieldDefinition(2, Type = FieldType.Number, Label = "IndexerSettingsGrabLimit", HelpText = "IndexerSettingsGrabLimitHelpText", Advanced = true)]
        public int? GrabLimit { get; set; }

        [FieldDefinition(3, Type = FieldType.Select, Label = "IndexerSettingsLimitsUnit", SelectOptions = typeof(IndexerLimitsUnit), HelpText = "IndexerSettingsLimitsUnitHelpText", Advanced = true)]
        public int LimitsUnit { get; set; } = (int)IndexerLimitsUnit.Day;
    }

    public enum IndexerLimitsUnit
    {
        Day = 0,
        Hour = 1
    }
}
