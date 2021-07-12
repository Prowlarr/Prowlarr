using NzbDrone.Core.Annotations;

// ReSharper disable InconsistentNaming
namespace NzbDrone.Core.Indexers.Definitions.Xthor
{
    public enum XthorAccent
    {
        [FieldOption(Hint = "All Voices (default)")]
        All = 0,

        [FieldOption(Hint = "Françaises")]
        VFF = 1,

        [FieldOption(Hint = "Quebecoises")]
        VFQ = 2,

        [FieldOption(Hint = "Françaises et Québécoises")]
        VF = 47,

        [FieldOption(Hint = "Anglaises")]
        EN = 3,

        [FieldOption(Hint = "Japonaises")]
        JP = 4,

        [FieldOption(Hint = "Espagnoles")]
        ES = 5,

        [FieldOption(Hint = "Allemandes")]
        DE = 6,

        [FieldOption(Hint = "Chinoises")]
        CH = 7,

        [FieldOption(Hint = "Italiennes")]
        IT = 8
    }

    public enum XthorPagesNumber
    {
        [FieldOption(Label = "1", Hint = "1 (32 results - default / best perf.)")]
        one = 1,

        [FieldOption(Label = "2", Hint = "2 (64 results)")]
        two = 2,

        [FieldOption(Label = "3", Hint = "3 (96 results)")]
        three = 3,

        [FieldOption(Label = "4", Hint = "4 (128 results - hard limit max)")]
        four = 4,
    }
}
