using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers
{
    public class IndexerCommonSettingsValidator : AbstractValidator<IndexerBaseSettings>
    {
        public IndexerCommonSettingsValidator()
        {
            RuleFor(c => c.QueryLimit).GreaterThan(0).When(c => c.QueryLimit.HasValue).WithMessage("Should be greater than zero");

            RuleFor(c => c.GrabLimit).GreaterThan(0).When(c => c.GrabLimit.HasValue).WithMessage("Should be greater than zero");
        }
    }

    public class IndexerBaseSettings
    {
        private static readonly IndexerCommonSettingsValidator Validator = new IndexerCommonSettingsValidator();

        [FieldDefinition(1, Type = FieldType.Number, Label = "Query Limit", HelpText = "The number of queries within a rolling 24 hour period Prowlarr will allow to the site", Advanced = true)]
        public int? QueryLimit { get; set; }

        [FieldDefinition(2, Type = FieldType.Number, Label = "Grab Limit", HelpText = "The number of grabs within a rolling 24 hour period Prowlarr will allow to the site", Advanced = true)]
        public int? GrabLimit { get; set; }
    }
}
