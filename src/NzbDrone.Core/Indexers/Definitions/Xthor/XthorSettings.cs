using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.Xthor
{
    public class XthorSettingsValidator : NoAuthSettingsValidator<XthorSettings>
    {
    }

    public class XthorSettings : NoAuthTorrentBaseSettings
    {
        private static readonly XthorSettingsValidator Validator = new ();

        public XthorSettings()
        {
            BaseUrl = "https://api.xthor.tk/";
            Passkey = "";
            FreeleechOnly = false;
            Accent = 0;
            EnhancedFrenchAccent = false;
            NeedMultiReplacement = false;
            MultiReplacement = "";
            SubReplacement = "";
            EnhancedAnime = true;
            ByPassPageForTmDbid = true;
            MaxPages = 1;
        }

        [FieldDefinition(2, Label = "Passkey", Privacy = PrivacyLevel.Password, Type = FieldType.Password, HelpText = "Site Passkey")]
        public string Passkey { get; set; }

        [FieldDefinition(3, Label = "Freeleech only", Privacy = PrivacyLevel.Normal, Type = FieldType.Checkbox, HelpText = "If you want to discover only freeleech torrents to not impact your ratio, check the related box.")]
        public bool FreeleechOnly { get; set; }

        [FieldDefinition(4, Label = "Specific language", Type = FieldType.Select, SelectOptions = typeof(XthorAccent), HelpText = "You can scope your searches with a specific language / accent.")]
        public int Accent { get; set; }

        [FieldDefinition(5, Label = "Do you want to use enhanced FRENCH search?", Type = FieldType.Checkbox, HelpText = "If you search for VFF or VFQ accent, it will also search with VFF+VFQ accent.")]
        public bool EnhancedFrenchAccent { get; set; }

        [FieldDefinition(6, Label = "Replace MULTI keyword", Type = FieldType.Checkbox, HelpText = "Useful if you want MULTI release to be parsed as another language")]
        public bool NeedMultiReplacement { get; set; }

        [FieldDefinition(7, Label = "MULTI replacement", Type = FieldType.Textbox, HelpText = "Word used to replace \"MULTI\" keyword in release title")]
        public string MultiReplacement { get; set; }

        [FieldDefinition(8, Label = "SUB replacement", Type = FieldType.Textbox, HelpText = "Do you want to replace \"VOSTFR\" and \"SUBFRENCH\" with specific word?")]
        public string SubReplacement { get; set; }

        [FieldDefinition(9, Label = "Do you want to use enhanced ANIME search?", Type = FieldType.Checkbox, HelpText = "if you have \"Anime\", this will improve queries made to this tracker related to this type when making searches. (This will change the episode number to EXXX)")]
        public bool EnhancedAnime { get; set; }

        [FieldDefinition(10, Label = "Do you want to bypass max pages for TMDB searches? (Radarr) - Hard limit of 4", Type = FieldType.Checkbox, HelpText = "(recommended) this indexer is compatible with TMDB queries (for movies only), so when requesting content with an TMDB ID, we will search directly ID on API. Results will be more accurate, so you can enable a max pages bypass for this query type.", Advanced = true)]
        public bool ByPassPageForTmDbid { get; set; }

        [FieldDefinition(11, Label = "How many pages do you want to follow?", Type = FieldType.Select, SelectOptions = typeof(XthorPagesNumber), HelpText = "(not recommended) you can increase max pages to follow when making a request. But be aware that this API is very buggy on tracker side, most of time, results of next pages are same as the first page. Even if we deduplicate rows, you will loose performance for the same results.", Advanced = true)]
        public int MaxPages { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
