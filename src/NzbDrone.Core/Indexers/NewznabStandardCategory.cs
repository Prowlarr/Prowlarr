using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Indexers
{
    public static class NewznabStandardCategory
    {
        public static readonly IndexerCategory ZedOther = new IndexerCategory(0000, "Other");
        public static readonly IndexerCategory ZedOtherMisc = new IndexerCategory(0010, "Other/Misc");
        public static readonly IndexerCategory ZedOtherHashed = new IndexerCategory(0020, "Other/Hashed");

        public static readonly IndexerCategory Console = new IndexerCategory(1000, "Console");
        public static readonly IndexerCategory ConsoleNDS = new IndexerCategory(1010, "Console/NDS");
        public static readonly IndexerCategory ConsolePSP = new IndexerCategory(1020, "Console/PSP");
        public static readonly IndexerCategory ConsoleWii = new IndexerCategory(1030, "Console/Wii");
        public static readonly IndexerCategory ConsoleXBox = new IndexerCategory(1040, "Console/XBox");
        public static readonly IndexerCategory ConsoleXBox360 = new IndexerCategory(1050, "Console/XBox 360");
        public static readonly IndexerCategory ConsoleWiiware = new IndexerCategory(1060, "Console/Wiiware");
        public static readonly IndexerCategory ConsoleXBox360DLC = new IndexerCategory(1070, "Console/XBox 360 DLC");
        public static readonly IndexerCategory ConsolePS3 = new IndexerCategory(1080, "Console/PS3");
        public static readonly IndexerCategory ConsoleOther = new IndexerCategory(1090, "Console/Other");
        public static readonly IndexerCategory Console3DS = new IndexerCategory(1110, "Console/3DS");
        public static readonly IndexerCategory ConsolePSVita = new IndexerCategory(1120, "Console/PS Vita");
        public static readonly IndexerCategory ConsoleWiiU = new IndexerCategory(1130, "Console/WiiU");
        public static readonly IndexerCategory ConsoleXBoxOne = new IndexerCategory(1140, "Console/XBox One");
        public static readonly IndexerCategory ConsolePS4 = new IndexerCategory(1180, "Console/PS4");

        public static readonly IndexerCategory Movies = new IndexerCategory(2000, "Movies");
        public static readonly IndexerCategory MoviesForeign = new IndexerCategory(2010, "Movies/Foreign");
        public static readonly IndexerCategory MoviesOther = new IndexerCategory(2020, "Movies/Other");
        public static readonly IndexerCategory MoviesSD = new IndexerCategory(2030, "Movies/SD");
        public static readonly IndexerCategory MoviesHD = new IndexerCategory(2040, "Movies/HD");
        public static readonly IndexerCategory MoviesUHD = new IndexerCategory(2045, "Movies/UHD");
        public static readonly IndexerCategory MoviesBluRay = new IndexerCategory(2050, "Movies/BluRay");
        public static readonly IndexerCategory Movies3D = new IndexerCategory(2060, "Movies/3D");
        public static readonly IndexerCategory MoviesDVD = new IndexerCategory(2070, "Movies/DVD");
        public static readonly IndexerCategory MoviesWEBDL = new IndexerCategory(2080, "Movies/WEB-DL");

        public static readonly IndexerCategory Audio = new IndexerCategory(3000, "Audio");
        public static readonly IndexerCategory AudioMP3 = new IndexerCategory(3010, "Audio/MP3");
        public static readonly IndexerCategory AudioVideo = new IndexerCategory(3020, "Audio/Video");
        public static readonly IndexerCategory AudioAudiobook = new IndexerCategory(3030, "Audio/Audiobook");
        public static readonly IndexerCategory AudioLossless = new IndexerCategory(3040, "Audio/Lossless");
        public static readonly IndexerCategory AudioOther = new IndexerCategory(3050, "Audio/Other");
        public static readonly IndexerCategory AudioForeign = new IndexerCategory(3060, "Audio/Foreign");

        public static readonly IndexerCategory PC = new IndexerCategory(4000, "PC");
        public static readonly IndexerCategory PC0day = new IndexerCategory(4010, "PC/0day");
        public static readonly IndexerCategory PCISO = new IndexerCategory(4020, "PC/ISO");
        public static readonly IndexerCategory PCMac = new IndexerCategory(4030, "PC/Mac");
        public static readonly IndexerCategory PCMobileOther = new IndexerCategory(4040, "PC/Mobile-Other");
        public static readonly IndexerCategory PCGames = new IndexerCategory(4050, "PC/Games");
        public static readonly IndexerCategory PCMobileiOS = new IndexerCategory(4060, "PC/Mobile-iOS");
        public static readonly IndexerCategory PCMobileAndroid = new IndexerCategory(4070, "PC/Mobile-Android");

        public static readonly IndexerCategory TV = new IndexerCategory(5000, "TV");
        public static readonly IndexerCategory TVWEBDL = new IndexerCategory(5010, "TV/WEB-DL");
        public static readonly IndexerCategory TVForeign = new IndexerCategory(5020, "TV/Foreign");
        public static readonly IndexerCategory TVSD = new IndexerCategory(5030, "TV/SD");
        public static readonly IndexerCategory TVHD = new IndexerCategory(5040, "TV/HD");
        public static readonly IndexerCategory TVUHD = new IndexerCategory(5045, "TV/UHD");
        public static readonly IndexerCategory TVOther = new IndexerCategory(5050, "TV/Other");
        public static readonly IndexerCategory TVSport = new IndexerCategory(5060, "TV/Sport");
        public static readonly IndexerCategory TVAnime = new IndexerCategory(5070, "TV/Anime");
        public static readonly IndexerCategory TVDocumentary = new IndexerCategory(5080, "TV/Documentary");

        public static readonly IndexerCategory XXX = new IndexerCategory(6000, "XXX");
        public static readonly IndexerCategory XXXDVD = new IndexerCategory(6010, "XXX/DVD");
        public static readonly IndexerCategory XXXWMV = new IndexerCategory(6020, "XXX/WMV");
        public static readonly IndexerCategory XXXXviD = new IndexerCategory(6030, "XXX/XviD");
        public static readonly IndexerCategory XXXx264 = new IndexerCategory(6040, "XXX/x264");
        public static readonly IndexerCategory XXXUHD = new IndexerCategory(6045, "XXX/UHD");
        public static readonly IndexerCategory XXXPack = new IndexerCategory(6050, "XXX/Pack");
        public static readonly IndexerCategory XXXImageSet = new IndexerCategory(6060, "XXX/ImageSet");
        public static readonly IndexerCategory XXXOther = new IndexerCategory(6070, "XXX/Other");
        public static readonly IndexerCategory XXXSD = new IndexerCategory(6080, "XXX/SD");
        public static readonly IndexerCategory XXXWEBDL = new IndexerCategory(6090, "XXX/WEB-DL");

        public static readonly IndexerCategory Books = new IndexerCategory(7000, "Books");
        public static readonly IndexerCategory BooksMags = new IndexerCategory(7010, "Books/Mags");
        public static readonly IndexerCategory BooksEBook = new IndexerCategory(7020, "Books/EBook");
        public static readonly IndexerCategory BooksComics = new IndexerCategory(7030, "Books/Comics");
        public static readonly IndexerCategory BooksTechnical = new IndexerCategory(7040, "Books/Technical");
        public static readonly IndexerCategory BooksOther = new IndexerCategory(7050, "Books/Other");
        public static readonly IndexerCategory BooksForeign = new IndexerCategory(7060, "Books/Foreign");

        public static readonly IndexerCategory Other = new IndexerCategory(8000, "Other");
        public static readonly IndexerCategory OtherMisc = new IndexerCategory(8010, "Other/Misc");
        public static readonly IndexerCategory OtherHashed = new IndexerCategory(8020, "Other/Hashed");

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
            ZedOther,
            ZedOtherHashed,
            ZedOtherMisc,
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
            OtherHashed
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
                    MoviesWEBDL
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
                    TVDocumentary
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
