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
        VF2 = 47,

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
        IT = 8,

        [FieldOption(Hint = "Coréennes")]
        KR = 9,

        [FieldOption(Hint = "Danoises")]
        DK = 10,

        [FieldOption(Hint = "Russes")]
        RU = 11,

        [FieldOption(Hint = "Portugaises")]
        PT = 12,

        [FieldOption(Hint = "Hindi")]
        IN = 13,

        [FieldOption(Hint = "Hollandaises")]
        NL = 14,

        [FieldOption(Hint = "Suédoises")]
        SE = 15,

        [FieldOption(Hint = "Norvégiennes")]
        NO = 16,

        [FieldOption(Hint = "Thaïlandaises")]
        TH = 17,

        [FieldOption(Hint = "Hébreu")]
        HE = 18,

        [FieldOption(Hint = "Persanes")]
        PE = 19,

        [FieldOption(Hint = "Arabes")]
        AR = 20,

        [FieldOption(Hint = "Turques")]
        TR = 21,

        [FieldOption(Hint = "Hongroises")]
        HU = 22,

        [FieldOption(Hint = "Polonaises")]
        PL = 23,

        [FieldOption(Hint = "Finnoises")]
        FI = 24,

        [FieldOption(Hint = "Indonésiennes")]
        ID = 25,

        [FieldOption(Hint = "Roumaines")]
        RO = 26,

        [FieldOption(Hint = "Malaisiennes")]
        MY = 27,

        [FieldOption(Hint = "Estoniennes")]
        EE = 28,

        [FieldOption(Hint = "Islandaises")]
        IS = 29,

        [FieldOption(Hint = "Grecques")]
        GR = 30,

        [FieldOption(Hint = "Serbes")]
        RS = 31,

        [FieldOption(Hint = "Norvégiennes (2)")]
        NOB = 32,

        [FieldOption(Hint = "Ukrainiennes")]
        UA = 33,

        [FieldOption(Hint = "Bulgares")]
        BG = 34,

        [FieldOption(Hint = "Tagalogues")]
        PH = 35,

        [FieldOption(Hint = "Xhosa")]
        XH = 36,

        [FieldOption(Hint = "Kurdes")]
        KU = 37,

        [FieldOption(Hint = "Bengali")]
        BE = 38,

        [FieldOption(Hint = "Amhariques")]
        AM = 39,

        [FieldOption(Hint = "Bosniaques")]
        BO = 40,

        [FieldOption(Hint = "Malayalam")]
        MA = 41,

        [FieldOption(Hint = "Télougou")]
        TE = 42,

        [FieldOption(Hint = "Bambara")]
        BA = 43,

        [FieldOption(Hint = "Catalanes")]
        CT = 44,

        [FieldOption(Hint = "Tchèques")]
        CZ = 45,

        [FieldOption(Hint = "Afrikaans")]
        AF = 46,
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
