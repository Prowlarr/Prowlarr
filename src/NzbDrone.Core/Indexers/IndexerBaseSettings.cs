using FluentValidation;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Indexers
{
    public class IndexerCommonSettingsValidator : AbstractValidator<IndexerBaseSettings>
    {
        public IndexerCommonSettingsValidator()
        {
        }
    }

    public class IndexerBaseSettings
    {
        private static readonly IndexerCommonSettingsValidator Validator = new IndexerCommonSettingsValidator();

        [FieldDefinition(1, Type = FieldType.Number, Label = "Query Limit", HelpText = "The number of queries per day Prowlarr will allow to the site", Advanced = true)]
        public int? QueryLimit { get; set; }

        [FieldDefinition(2, Type = FieldType.Number, Label = "Grab Limit", HelpText = "The number of grabs per day Prowlarr will allow to the site", Advanced = true)]
        public int? GrabLimit { get; set; }
    }
}
