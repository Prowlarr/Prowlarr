using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannSettingsValidator : AbstractValidator<CardigannSettings>
    {
        public CardigannSettingsValidator()
        {
            RuleFor(c => c).Custom((c, context) =>
            {
                if (c.Categories.Empty())
                {
                    context.AddFailure("'Categories' must be provided");
                }
            });
        }
    }

    public class CardigannSettings : IIndexerSettings
    {
        private static readonly CardigannSettingsValidator Validator = new CardigannSettingsValidator();

        public CardigannSettings()
        {
            Categories = new[] { 2000, 2010, 2020, 2030, 2035, 2040, 2045, 2050, 2060 };
            MultiLanguages = new List<int>();
            ExtraFieldData = new Dictionary<string, object>();
        }

        [FieldDefinition(0, Hidden = HiddenType.Hidden)]
        public string DefinitionFile { get; set; }

        public Dictionary<string, object> ExtraFieldData { get; set; }

        public string BaseUrl { get; set; }

        [FieldDefinition(1000, Type = FieldType.Select, SelectOptions = typeof(LanguageFieldConverter), Label = "Multi Languages", HelpText = "What languages are normally in a multi release on this indexer?", Advanced = true)]
        public IEnumerable<int> MultiLanguages { get; set; }

        [FieldDefinition(1001, Label = "Categories", HelpText = "Comma Separated list, leave blank to disable all categories", Advanced = true)]
        public IEnumerable<int> Categories { get; set; }

        // Field 8 is used by TorznabSettings MinimumSeeders
        // If you need to add another field here, update TorznabSettings as well and this comment
        public virtual NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
