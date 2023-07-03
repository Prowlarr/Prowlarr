using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Indexers
{
    public static class NewznabStandardCategory
    {
        public static readonly IndexerCategory ZedOther = new (0000, "Other");
        public static readonly IndexerCategory ZedOtherMisc = new (0010, "Other/Misc");
        public static readonly IndexerCategory ZedOtherHashed = new (0020, "Other/Hashed");

        public static readonly IndexerCategory Console = new (1000, "Console");
        public static readonly IndexerCategory ConsoleNDS = new (1010, "Console/NDS");
        public static readonly IndexerCategory ConsolePSP = new (1020, "Console/PSP");
        public static readonly IndexerCategory ConsoleWii = new (1030, "Console/Wii");
        public static readonly IndexerCategory ConsoleXBox = new (1040, "Console/XBox");
        public static readonly IndexerCategory ConsoleXBox360 = new (1050, "Console/XBox 360");
        public static readonly IndexerCategory ConsoleWiiware = new (1060, "Console/Wiiware");
        public static readonly IndexerCategory ConsoleXBox360DLC = new (1070, "Console/XBox 360 DLC");
        public static readonly IndexerCategory ConsolePS3 = new (1080, "Console/PS3");
        public static readonly IndexerCategory ConsoleOther = new (1090, "Console/Other");
        public static readonly IndexerCategory Console3DS = new (1110, "Console/3DS");
        public static readonly IndexerCategory ConsolePSVita = new (1120, "Console/PS Vita");
        public static readonly IndexerCategory ConsoleWiiU = new (1130, "Console/WiiU");
        public static readonly IndexerCategory ConsoleXBoxOne = new (1140, "Console/XBox One");
        public static readonly IndexerCategory ConsolePS4 = new (1180, "Console/PS4");

        public static readonly IndexerCategory Movies = new (2000, "Movies");
        public static readonly IndexerCategory MoviesForeign = new (2010, "Movies/Foreign");
        public static readonly IndexerCategory MoviesOther = new (2020, "Movies/Other");
        public static readonly IndexerCategory MoviesSD = new (2030, "Movies/SD");
        public static readonly IndexerCategory MoviesHD = new (2040, "Movies/HD");
        public static readonly IndexerCategory MoviesUHD = new (2045, "Movies/UHD");
        public static readonly IndexerCategory MoviesBluRay = new (2050, "Movies/BluRay");
        public static readonly IndexerCategory Movies3D = new (2060, "Movies/3D");
        public static readonly IndexerCategory MoviesDVD = new (2070, "Movies/DVD");
        public static readonly IndexerCategory MoviesWEBDL = new (2080, "Movies/WEB-DL");
        public static readonly IndexerCategory Moviesx265 = new (2090, "Movies/x265");

        public static readonly IndexerCategory Audio = new (3000, "Audio");
        public static readonly IndexerCategory AudioMP3 = new (3010, "Audio/MP3");
        public static readonly IndexerCategory AudioVideo = new (3020, "Audio/Video");
        public static readonly IndexerCategory AudioAudiobook = new (3030, "Audio/Audiobook");
        public static readonly IndexerCategory AudioLossless = new (3040, "Audio/Lossless");
        public static readonly IndexerCategory AudioOther = new (3050, "Audio/Other");
        public static readonly IndexerCategory AudioForeign = new (3060, "Audio/Foreign");

        public static readonly IndexerCategory PC = new (4000, "PC");
        public static readonly IndexerCategory PC0day = new (4010, "PC/0day");
        public static readonly IndexerCategory PCISO = new (4020, "PC/ISO");
        public static readonly IndexerCategory PCMac = new (4030, "PC/Mac");
        public static readonly IndexerCategory PCMobileOther = new (4040, "PC/Mobile-Other");
        public static readonly IndexerCategory PCGames = new (4050, "PC/Games");
        public static readonly IndexerCategory PCMobileiOS = new (4060, "PC/Mobile-iOS");
        public static readonly IndexerCategory PCMobileAndroid = new (4070, "PC/Mobile-Android");

        public static readonly IndexerCategory TV = new (5000, "TV");
        public static readonly IndexerCategory TVWEBDL = new (5010, "TV/WEB-DL");
        public static readonly IndexerCategory TVForeign = new (5020, "TV/Foreign");
        public static readonly IndexerCategory TVSD = new (5030, "TV/SD");
        public static readonly IndexerCategory TVHD = new (5040, "TV/HD");
        public static readonly IndexerCategory TVUHD = new (5045, "TV/UHD");
        public static readonly IndexerCategory TVOther = new (5050, "TV/Other");
        public static readonly IndexerCategory TVSport = new (5060, "TV/Sport");
        public static readonly IndexerCategory TVAnime = new (5070, "TV/Anime");
        public static readonly IndexerCategory TVDocumentary = new (5080, "TV/Documentary");
        public static readonly IndexerCategory TVx265 = new (5090, "TV/x265");

        public static readonly IndexerCategory XXX = new (6000, "XXX");
        public static readonly IndexerCategory XXXDVD = new (6010, "XXX/DVD");
        public static readonly IndexerCategory XXXWMV = new (6020, "XXX/WMV");
        public static readonly IndexerCategory XXXXviD = new (6030, "XXX/XviD");
        public static readonly IndexerCategory XXXx264 = new (6040, "XXX/x264");
        public static readonly IndexerCategory XXXUHD = new (6045, "XXX/UHD");
        public static readonly IndexerCategory XXXPack = new (6050, "XXX/Pack");
        public static readonly IndexerCategory XXXImageSet = new (6060, "XXX/ImageSet");
        public static readonly IndexerCategory XXXOther = new (6070, "XXX/Other");
        public static readonly IndexerCategory XXXSD = new (6080, "XXX/SD");
        public static readonly IndexerCategory XXXWEBDL = new (6090, "XXX/WEB-DL");

        public static readonly IndexerCategory Books = new (7000, "Books");
        public static readonly IndexerCategory BooksMags = new (7010, "Books/Mags");
        public static readonly IndexerCategory BooksEBook = new (7020, "Books/EBook");
        public static readonly IndexerCategory BooksComics = new (7030, "Books/Comics");
        public static readonly IndexerCategory BooksTechnical = new (7040, "Books/Technical");
        public static readonly IndexerCategory BooksOther = new (7050, "Books/Other");
        public static readonly IndexerCategory BooksForeign = new (7060, "Books/Foreign");

        public static readonly IndexerCategory Other = new (8000, "Other");
        public static readonly IndexerCategory OtherMisc = new (8010, "Other/Misc");
        public static readonly IndexerCategory OtherHashed = new (8020, "Other/Hashed");

        public static readonly IndexerCategory[] ParentCats =
        {
            ZedOther,
            Console,
            Movies,
            Audio,
            PC,
            TV,
            XXX,
            Books,
            Other
        };

        public static readonly IndexerCategory[] AllCats =
        {
            Console,
            ConsoleNDS,
            ConsolePSP,
            ConsoleWii,
            ConsoleXBox,
            ConsoleXBox360,
            ConsoleWiiware,
            ConsoleXBox360DLC,
            ConsolePS3,
            ConsoleOther,
            Console3DS,
            ConsolePSVita,
            ConsoleWiiU,
            ConsoleXBoxOne,
            ConsolePS4,
            Movies,
            MoviesForeign,
            MoviesOther,
            MoviesSD,
            MoviesHD,
            MoviesUHD,
            MoviesBluRay,
            Movies3D,
            MoviesDVD,
            MoviesWEBDL,
            Moviesx265,
            Audio,
            AudioMP3,
            AudioVideo,
            AudioAudiobook,
            AudioLossless,
            AudioOther,
            AudioForeign,
            PC,
            PC0day,
            PCISO,
            PCMac,
            PCMobileOther,
            PCGames,
            PCMobileiOS,
            PCMobileAndroid,
            TV,
            TVWEBDL,
            TVForeign,
            TVSD,
            TVHD,
            TVUHD,
            TVOther,
            TVSport,
            TVAnime,
            TVDocumentary,
            TVx265,
            XXX,
            XXXDVD,
            XXXWMV,
            XXXXviD,
            XXXx264,
            XXXUHD,
            XXXPack,
            XXXImageSet,
            XXXOther,
            XXXSD,
            XXXWEBDL,
            Books,
            BooksMags,
            BooksEBook,
            BooksComics,
            BooksTechnical,
            BooksOther,
            BooksForeign,
            Other,
            OtherMisc,
            OtherHashed,
            ZedOther,
            ZedOtherHashed,
            ZedOtherMisc
        };

        static NewznabStandardCategory()
        {
            ZedOther.SubCategories.AddRange(new List<IndexerCategory> { ZedOtherMisc, ZedOtherHashed });
            Console.SubCategories.AddRange(
                new List<IndexerCategory>
                {
                    ConsoleNDS,
                    ConsolePSP,
                    ConsoleWii,
                    ConsoleXBox,
                    ConsoleXBox360,
                    ConsoleWiiware,
                    ConsoleXBox360DLC,
                    ConsolePS3,
                    ConsoleOther,
                    Console3DS,
                    ConsolePSVita,
                    ConsoleWiiU,
                    ConsoleXBoxOne,
                    ConsolePS4
                });
            Movies.SubCategories.AddRange(
                new List<IndexerCategory>
                {
                    MoviesForeign,
                    MoviesOther,
                    MoviesSD,
                    MoviesHD,
                    MoviesUHD,
                    MoviesBluRay,
                    Movies3D,
                    MoviesDVD,
                    MoviesWEBDL,
                    Moviesx265
                });
            Audio.SubCategories.AddRange(
                new List<IndexerCategory>
                {
                    AudioMP3,
                    AudioVideo,
                    AudioAudiobook,
                    AudioLossless,
                    AudioOther,
                    AudioForeign
                });
            PC.SubCategories.AddRange(
                new List<IndexerCategory>
                {
                    PC0day,
                    PCISO,
                    PCMac,
                    PCMobileOther,
                    PCGames,
                    PCMobileiOS,
                    PCMobileAndroid
                });
            TV.SubCategories.AddRange(
                new List<IndexerCategory>
                {
                    TVWEBDL,
                    TVForeign,
                    TVSD,
                    TVHD,
                    TVUHD,
                    TVOther,
                    TVSport,
                    TVAnime,
                    TVDocumentary,
                    TVx265
                });
            XXX.SubCategories.AddRange(
                new List<IndexerCategory>
                {
                    XXXDVD,
                    XXXWMV,
                    XXXXviD,
                    XXXx264,
                    XXXUHD,
                    XXXPack,
                    XXXImageSet,
                    XXXOther,
                    XXXSD,
                    XXXWEBDL
                });
            Books.SubCategories.AddRange(
                new List<IndexerCategory>
                {
                    BooksMags,
                    BooksEBook,
                    BooksComics,
                    BooksTechnical,
                    BooksOther,
                    BooksForeign
                });
            Other.SubCategories.AddRange(new List<IndexerCategory> { OtherMisc, OtherHashed });
        }

        public static string GetCatDesc(int torznabCatId) =>
            AllCats.FirstOrDefault(c => c.Id == torznabCatId)?.Name ?? string.Empty;

        public static IndexerCategory GetCatByName(string name) => AllCats.FirstOrDefault(c => c.Name == name);
    }
}
