using System;
using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public class AvistazSettingsValidator : NoAuthSettingsValidator<AvistazSettings>
    {
        public AvistazSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
            RuleFor(c => c.Pid).NotEmpty();
        }
    }

    public class AvistazSettings : NoAuthTorrentBaseSettings
    {
        private static readonly AvistazSettingsValidator Validator = new();

        public string Token { get; set; } = string.Empty;

        [FieldDefinition(2, Label = "Username", HelpText = "IndexerAvistazSettingsUsernameHelpText", HelpTextWarning = "IndexerAvistazSettingsUsernameHelpTextWarning", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "IndexerAvistazSettingsPasswordHelpText", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "PID", HelpText = "IndexerAvistazSettingsPidHelpText", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Pid { get; set; }

        [FieldDefinition(5, Label = "IndexerSettingsFreeleechOnly", Type = FieldType.Checkbox, HelpText = "IndexerAvistazSettingsFreeleechOnlyHelpText")]
        public bool FreeleechOnly { get; set; }

        [FieldDefinition(6, Label = "IndexerAvistazSettingsSearchAudioLanguages", Type = FieldType.Select, SelectOptions = typeof(AvistaZAudioLanguage), HelpText = "IndexerAvistazSettingsSearchAudioLanguagesHelpText", Advanced = true)]
        public IEnumerable<int> SearchAudioLanguages { get; set; } = Array.Empty<int>();

        [FieldDefinition(7, Label = "IndexerAvistazSettingsSearchSubtitleLanguages", Type = FieldType.Select, SelectOptions = typeof(AvistaZSubtitleLanguage), HelpText = "IndexerAvistazSettingsSearchSubtitleLanguagesHelpText", Advanced = true)]
        public IEnumerable<int> SearchSubtitleLanguages { get; set; } = Array.Empty<int>();

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum AvistaZAudioLanguage
    {
        [FieldOption(Label = "Arabic")]
        Arabic = 7,

        [FieldOption(Label = "Assamese")]
        Assamese = 10,

        [FieldOption(Label = "Basque")]
        Basque = 17,

        [FieldOption(Label = "Bengali")]
        Bengali = 19,

        [FieldOption(Label = "Bosnian")]
        Bosnian = 23,

        [FieldOption(Label = "Brazilian Portuguese")]
        BrazilianPortuguese = 189,

        [FieldOption(Label = "Burmese")]
        Burmese = 26,

        [FieldOption(Label = "Cantonese")]
        Cantonese = 27,

        [FieldOption(Label = "Catalan")]
        Catalan = 28,

        [FieldOption(Label = "Central Khmer")]
        CentralKhmer = 29,

        [FieldOption(Label = "Chamorro")]
        Chamorro = 30,

        [FieldOption(Label = "Chinese")]
        Chinese = 33,

        [FieldOption(Label = "Croatian")]
        Croatian = 39,

        [FieldOption(Label = "Czech")]
        Czech = 40,

        [FieldOption(Label = "Danish")]
        Danish = 41,

        [FieldOption(Label = "Dutch")]
        Dutch = 43,

        [FieldOption(Label = "Dzongkha")]
        Dzongkha = 44,

        [FieldOption(Label = "English")]
        English = 45,

        [FieldOption(Label = "Esperanto")]
        Esperanto = 46,

        [FieldOption(Label = "Filipino")]
        Filipino = 188,

        [FieldOption(Label = "Finnish")]
        Finnish = 51,

        [FieldOption(Label = "French")]
        French = 52,

        [FieldOption(Label = "German")]
        German = 58,

        [FieldOption(Label = "Greek")]
        Greek = 59,

        [FieldOption(Label = "Gujarati")]
        Gujarati = 61,

        [FieldOption(Label = "Hausa")]
        Hausa = 63,

        [FieldOption(Label = "Hebrew")]
        Hebrew = 64,

        [FieldOption(Label = "Hindi")]
        Hindi = 66,

        [FieldOption(Label = "Hungarian")]
        Hungarian = 68,

        [FieldOption(Label = "Indonesian")]
        Indonesian = 72,

        [FieldOption(Label = "Inuktitut")]
        Inuktitut = 75,

        [FieldOption(Label = "Italian")]
        Italian = 78,

        [FieldOption(Label = "Japanese")]
        Japanese = 79,

        [FieldOption(Label = "Javanese")]
        Javanese = 80,

        [FieldOption(Label = "Kannada")]
        Kannada = 82,

        [FieldOption(Label = "Kashmiri")]
        Kashmiri = 84,

        [FieldOption(Label = "Kazakh")]
        Kazakh = 85,

        [FieldOption(Label = "Kirghiz")]
        Kirghiz = 88,

        [FieldOption(Label = "Korean")]
        Korean = 91,

        [FieldOption(Label = "Lao")]
        Lao = 94,

        [FieldOption(Label = "Malay")]
        Malay = 104,

        [FieldOption(Label = "Malayalam")]
        Malayalam = 105,

        [FieldOption(Label = "Mandarin")]
        Mandarin = 107,

        [FieldOption(Label = "Marathi")]
        Marathi = 110,

        [FieldOption(Label = "Mongolian")]
        Mongolian = 112,

        [FieldOption(Label = "Nepali")]
        Nepali = 118,

        [FieldOption(Label = "Ojibwa")]
        Ojibwa = 123,

        [FieldOption(Label = "Oriya")]
        Oriya = 124,

        [FieldOption(Label = "Pali")]
        Pali = 127,

        [FieldOption(Label = "Panjabi")]
        Panjabi = 128,

        [FieldOption(Label = "Persian")]
        Persian = 129,

        [FieldOption(Label = "Polish")]
        Polish = 130,

        [FieldOption(Label = "Portuguese")]
        Portuguese = 131,

        [FieldOption(Label = "Quechua")]
        Quechua = 133,

        [FieldOption(Label = "Romanian")]
        Romanian = 134,

        [FieldOption(Label = "Russian")]
        Russian = 137,

        [FieldOption(Label = "Sanskrit")]
        Sanskrit = 140,

        [FieldOption(Label = "Sinhala")]
        Sinhala = 146,

        [FieldOption(Label = "Slovak")]
        Slovak = 147,

        [FieldOption(Label = "Spanish")]
        Spanish = 151,

        [FieldOption(Label = "Swedish")]
        Swedish = 155,

        [FieldOption(Label = "Tagalog")]
        Tagalog = 156,

        [FieldOption(Label = "Tamil")]
        Tamil = 159,

        [FieldOption(Label = "Telugu")]
        Telugu = 161,

        [FieldOption(Label = "Thai")]
        Thai = 162,

        [FieldOption(Label = "Tibetan")]
        Tibetan = 163,

        [FieldOption(Label = "Turkish")]
        Turkish = 168,

        [FieldOption(Label = "Uighur")]
        Uighur = 171,

        [FieldOption(Label = "Ukrainian")]
        Ukrainian = 172,

        [FieldOption(Label = "Urdu")]
        Urdu = 173,

        [FieldOption(Label = "Vietnamese")]
        Vietnamese = 176,

        [FieldOption(Label = "Xhosa")]
        Xhosa = 182,
    }

    public enum AvistaZSubtitleLanguage
    {
        [FieldOption(Label = "Abkhazian")]
        Abkhazian = 1,

        [FieldOption(Label = "Afar")]
        Afar = 2,

        [FieldOption(Label = "Afrikaans")]
        Afrikaans = 3,

        [FieldOption(Label = "Albanian")]
        Albanian = 5,

        [FieldOption(Label = "Amharic")]
        Amharic = 6,

        [FieldOption(Label = "Arabic")]
        Arabic = 7,

        [FieldOption(Label = "Basque")]
        Basque = 17,

        [FieldOption(Label = "Belarusian")]
        Belarusian = 18,

        [FieldOption(Label = "Bengali")]
        Bengali = 19,

        [FieldOption(Label = "Norwegian Bokmål")]
        NorwegianBokmal = 22,

        [FieldOption(Label = "Bosnian")]
        Bosnian = 23,

        [FieldOption(Label = "Bulgarian")]
        Bulgarian = 25,

        [FieldOption(Label = "Brazilian Portuguese")]
        BrazilianPortuguese = 189,

        [FieldOption(Label = "Burmese")]
        Burmese = 26,

        [FieldOption(Label = "Cantonese")]
        Cantonese = 27,

        [FieldOption(Label = "Catalan")]
        Catalan = 28,

        [FieldOption(Label = "Central Khmer")]
        CentralKhmer = 29,

        [FieldOption(Label = "Chichewa")]
        Chichewa = 32,

        [FieldOption(Label = "Chinese")]
        Chinese = 33,

        [FieldOption(Label = "Corsican")]
        Corsican = 37,

        [FieldOption(Label = "Croatian")]
        Croatian = 39,

        [FieldOption(Label = "Czech")]
        Czech = 40,

        [FieldOption(Label = "Danish")]
        Danish = 41,

        [FieldOption(Label = "Dutch")]
        Dutch = 43,

        [FieldOption(Label = "English")]
        English = 45,

        [FieldOption(Label = "Esperanto")]
        Esperanto = 46,

        [FieldOption(Label = "Estonian")]
        Estonian = 47,

        [FieldOption(Label = "Fijian")]
        Fijian = 50,

        [FieldOption(Label = "Filipino")]
        Filipino = 188,

        [FieldOption(Label = "Finnish")]
        Finnish = 51,

        [FieldOption(Label = "French")]
        French = 52,

        [FieldOption(Label = "Galician")]
        Galician = 55,

        [FieldOption(Label = "Georgian")]
        Georgian = 57,

        [FieldOption(Label = "German")]
        German = 58,

        [FieldOption(Label = "Greek")]
        Greek = 59,

        [FieldOption(Label = "Gujarati")]
        Gujarati = 61,

        [FieldOption(Label = "Haitian")]
        Haitian = 62,

        [FieldOption(Label = "Hebrew")]
        Hebrew = 64,

        [FieldOption(Label = "Hindi")]
        Hindi = 66,

        [FieldOption(Label = "Hungarian")]
        Hungarian = 68,

        [FieldOption(Label = "Icelandic")]
        Icelandic = 69,

        [FieldOption(Label = "Indonesian")]
        Indonesian = 72,

        [FieldOption(Label = "Irish")]
        Irish = 77,

        [FieldOption(Label = "Italian")]
        Italian = 78,

        [FieldOption(Label = "Japanese")]
        Japanese = 79,

        [FieldOption(Label = "Javanese")]
        Javanese = 80,

        [FieldOption(Label = "Kannada")]
        Kannada = 82,

        [FieldOption(Label = "Kazakh")]
        Kazakh = 85,

        [FieldOption(Label = "Korean")]
        Korean = 91,

        [FieldOption(Label = "Kurdish")]
        Kurdish = 93,

        [FieldOption(Label = "Lao")]
        Lao = 94,

        [FieldOption(Label = "Latin")]
        Latin = 95,

        [FieldOption(Label = "Latvian")]
        Latvian = 96,

        [FieldOption(Label = "Lithuanian")]
        Lithuanian = 99,

        [FieldOption(Label = "Macedonian")]
        Macedonian = 102,

        [FieldOption(Label = "Malagasy")]
        Malagasy = 103,

        [FieldOption(Label = "Malay")]
        Malay = 104,

        [FieldOption(Label = "Malayalam")]
        Malayalam = 105,

        [FieldOption(Label = "Mandarin")]
        Mandarin = 107,

        [FieldOption(Label = "Maori")]
        Maori = 109,

        [FieldOption(Label = "Marathi")]
        Marathi = 110,

        [FieldOption(Label = "Mongolian")]
        Mongolian = 112,

        [FieldOption(Label = "Nepali")]
        Nepali = 118,

        [FieldOption(Label = "Norwegian")]
        Norwegian = 120,

        [FieldOption(Label = "Oriya")]
        Oriya = 124,

        [FieldOption(Label = "Panjabi")]
        Panjabi = 128,

        [FieldOption(Label = "Persian")]
        Persian = 129,

        [FieldOption(Label = "Polish")]
        Polish = 130,

        [FieldOption(Label = "Portuguese")]
        Portuguese = 131,

        [FieldOption(Label = "Romanian")]
        Romanian = 134,

        [FieldOption(Label = "Romansh")]
        Romansh = 135,

        [FieldOption(Label = "Russian")]
        Russian = 137,

        [FieldOption(Label = "Sardinian")]
        Sardinian = 141,

        [FieldOption(Label = "Serbian")]
        Serbian = 142,

        [FieldOption(Label = "Sinhala")]
        Sinhala = 146,

        [FieldOption(Label = "Slovak")]
        Slovak = 147,

        [FieldOption(Label = "Slovenian")]
        Slovenian = 148,

        [FieldOption(Label = "Somali")]
        Somali = 149,

        [FieldOption(Label = "Spanish")]
        Spanish = 151,

        [FieldOption(Label = "Sundanese")]
        Sundanese = 152,

        [FieldOption(Label = "Swahili")]
        Swahili = 153,

        [FieldOption(Label = "Swedish")]
        Swedish = 155,

        [FieldOption(Label = "Tagalog")]
        Tagalog = 156,

        [FieldOption(Label = "Tamil")]
        Tamil = 159,

        [FieldOption(Label = "Telugu")]
        Telugu = 161,

        [FieldOption(Label = "Thai")]
        Thai = 162,

        [FieldOption(Label = "Turkish")]
        Turkish = 168,

        [FieldOption(Label = "Twi")]
        Twi = 170,

        [FieldOption(Label = "Ukrainian")]
        Ukrainian = 172,

        [FieldOption(Label = "Urdu")]
        Urdu = 173,

        [FieldOption(Label = "Venda")]
        Venda = 175,

        [FieldOption(Label = "Vietnamese")]
        Vietnamese = 176,

        [FieldOption(Label = "Volapük")]
        Volapuk = 177,

        [FieldOption(Label = "Welsh")]
        Welsh = 179,

        [FieldOption(Label = "Wolof")]
        Wolof = 181,
    }
}
