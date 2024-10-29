using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using FluentValidation;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    [Obsolete("Site has removed API access.")]
    public class NzbIndex : UsenetIndexerBase<NzbIndexSettings>
    {
        public override string Name => "NZBIndex";
        public override string[] IndexerUrls => new[] { "https://nzbindex.com/" };
        public override string Description => "A Usenet Indexer";
        public override IndexerPrivacy Privacy => IndexerPrivacy.SemiPrivate;
        public override bool SupportsPagination => true;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public NzbIndex(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, nzbValidationService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NzbIndexRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new NzbIndexParser(Settings, Capabilities.Categories);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            // TODO build this out more
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Other, "a2000.beeld.binaries");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.Other, "a2000.binaries");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Other, "a2000.erotica.binaries");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.Other, "a2000.games.binaries");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Other, "a2000.geluid.binaries");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.Other, "a.b");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Other, "a.b.0day.stuffz");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.Other, "a.b.1place4nzb");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.Other, "a.b.3d");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.Other, "a.b.a51");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.Other, "a.b.aa");
            caps.Categories.AddCategoryMapping(904, NewznabStandardCategory.Other, "a.b.all-your-base-are-belong-to-us");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.Other, "a.b.alt");
            caps.Categories.AddCategoryMapping(905, NewznabStandardCategory.Other, "a.b.alt5");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.Other, "a.b.amazing");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.Other, "a.b.amp");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.Other, "a.b.android");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.TVAnime, "a.b.anime");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.Other, "a.b.anime.german");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.Other, "a.b.anime.repost");
            caps.Categories.AddCategoryMapping(906, NewznabStandardCategory.Other, "a.b.appletv");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.Other, "a.b.applications");
            caps.Categories.AddCategoryMapping(907, NewznabStandardCategory.Other, "a.b.aquaria");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.Other, "a.b.archive.encrypted");
            caps.Categories.AddCategoryMapping(867, NewznabStandardCategory.Other, "a.b.art-of-usenet");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.Other, "a.b.asianusenet");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.Other, "a.b.astronomy");
            caps.Categories.AddCategoryMapping(946, NewznabStandardCategory.Other, "a.b.atari");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.Other, "a.b.ath");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.Other, "a.b.aubergine");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.Other, "a.b.audio.warez");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.Other, "a.b.audiobooks");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.Other, "a.b.b4e");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.Other, "a.b.b4e.erotica");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.Other, "a.b.barbarella");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.Other, "a.b.bbs");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.Other, "a.b.bd.french");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.Other, "a.b.beatles");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.Other, "a.b.big");
            caps.Categories.AddCategoryMapping(947, NewznabStandardCategory.Other, "a.b.bitburger");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.Other, "a.b.bloaf");
            caps.Categories.AddCategoryMapping(36, NewznabStandardCategory.Other, "a.b.blu-ray");
            caps.Categories.AddCategoryMapping(37, NewznabStandardCategory.Other, "a.b.blu-ray.subtitles");
            caps.Categories.AddCategoryMapping(35, NewznabStandardCategory.Other, "a.b.blue-ray");
            caps.Categories.AddCategoryMapping(38, NewznabStandardCategory.Other, "a.b.bollywood");
            caps.Categories.AddCategoryMapping(39, NewznabStandardCategory.Other, "a.b.bollywood.movies");
            caps.Categories.AddCategoryMapping(40, NewznabStandardCategory.Other, "a.b.boneless");
            caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.Other, "a.b.boneless.nl");
            caps.Categories.AddCategoryMapping(944, NewznabStandardCategory.Other, "a.b.bos");
            caps.Categories.AddCategoryMapping(951, NewznabStandardCategory.Other, "a.b.brg");
            caps.Categories.AddCategoryMapping(42, NewznabStandardCategory.Other, "a.b.british.documentaries");
            caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.Other, "a.b.british.drama");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.Other, "a.b.brothers-of-usenet.game");
            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.Other, "a.b.brothers-of-usenet.movie");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.Other, "a.b.brothers-of-usenet.musik");
            caps.Categories.AddCategoryMapping(866, NewznabStandardCategory.Other, "a.b.bungabunga");
            caps.Categories.AddCategoryMapping(908, NewznabStandardCategory.Other, "a.b.bungalow");
            caps.Categories.AddCategoryMapping(909, NewznabStandardCategory.Other, "a.b.busca-usenet");
            caps.Categories.AddCategoryMapping(948, NewznabStandardCategory.Other, "a.b.butthedd");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.Other, "a.b.buttnuggets");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.Other, "a.b.cartoons.french");
            caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.Other, "a.b.cartoons.french.animes-fansub");
            caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.Other, "a.b.cartoons.french.reposts");
            caps.Categories.AddCategoryMapping(910, NewznabStandardCategory.Other, "a.b.cats");
            caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.Other, "a.b.cavebox");
            caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.Other, "a.b.cbts");
            caps.Categories.AddCategoryMapping(53, NewznabStandardCategory.Other, "a.b.cccb");
            caps.Categories.AddCategoryMapping(54, NewznabStandardCategory.Other, "a.b.cd");
            caps.Categories.AddCategoryMapping(55, NewznabStandardCategory.Other, "a.b.cd.image");
            caps.Categories.AddCategoryMapping(56, NewznabStandardCategory.Other, "a.b.cd.image.0-day");
            caps.Categories.AddCategoryMapping(57, NewznabStandardCategory.Other, "a.b.cd.image.clonecd");
            caps.Categories.AddCategoryMapping(58, NewznabStandardCategory.Console, "a.b.cd.image.dreamcast");
            caps.Categories.AddCategoryMapping(59, NewznabStandardCategory.Other, "a.b.cd.image.french");
            caps.Categories.AddCategoryMapping(60, NewznabStandardCategory.Other, "a.b.cd.image.game");
            caps.Categories.AddCategoryMapping(61, NewznabStandardCategory.Console, "a.b.cd.image.gamecube");
            caps.Categories.AddCategoryMapping(62, NewznabStandardCategory.Other, "a.b.cd.image.games");
            caps.Categories.AddCategoryMapping(63, NewznabStandardCategory.Other, "a.b.cd.image.highspeed");
            caps.Categories.AddCategoryMapping(64, NewznabStandardCategory.Other, "a.b.cd.image.iso");
            caps.Categories.AddCategoryMapping(65, NewznabStandardCategory.Other, "a.b.cd.image.linux");
            caps.Categories.AddCategoryMapping(66, NewznabStandardCategory.Other, "a.b.cd.image.other");
            caps.Categories.AddCategoryMapping(67, NewznabStandardCategory.Console, "a.b.cd.image.playstation");
            caps.Categories.AddCategoryMapping(68, NewznabStandardCategory.Console, "a.b.cd.image.playstation2");
            caps.Categories.AddCategoryMapping(69, NewznabStandardCategory.Console, "a.b.cd.image.playstation2.dvdiso");
            caps.Categories.AddCategoryMapping(70, NewznabStandardCategory.Console, "a.b.cd.image.playstation2.repost");
            caps.Categories.AddCategoryMapping(71, NewznabStandardCategory.Console, "a.b.cd.image.ps2.dvdiso");
            caps.Categories.AddCategoryMapping(72, NewznabStandardCategory.Other, "a.b.cd.image.repost");
            caps.Categories.AddCategoryMapping(73, NewznabStandardCategory.Other, "a.b.cd.image.reposts");
            caps.Categories.AddCategoryMapping(74, NewznabStandardCategory.Other, "a.b.cd.image.twilights");
            caps.Categories.AddCategoryMapping(75, NewznabStandardCategory.Other, "a.b.cd.image.winapps");
            caps.Categories.AddCategoryMapping(76, NewznabStandardCategory.ConsoleXBox, "a.b.cd.image.xbox");
            caps.Categories.AddCategoryMapping(77, NewznabStandardCategory.Other, "a.b.cd.images");
            caps.Categories.AddCategoryMapping(78, NewznabStandardCategory.Other, "a.b.cd.images.games");
            caps.Categories.AddCategoryMapping(79, NewznabStandardCategory.Other, "a.b.cd.other");
            caps.Categories.AddCategoryMapping(80, NewznabStandardCategory.Other, "a.b.chakotay");
            caps.Categories.AddCategoryMapping(911, NewznabStandardCategory.Other, "a.b.chello");
            caps.Categories.AddCategoryMapping(81, NewznabStandardCategory.Other, "a.b.chello.nl");
            caps.Categories.AddCategoryMapping(82, NewznabStandardCategory.Other, "a.b.classic.tv.shows");
            caps.Categories.AddCategoryMapping(87, NewznabStandardCategory.Other, "a.b.comic-strips");
            caps.Categories.AddCategoryMapping(83, NewznabStandardCategory.BooksComics, "a.b.comics");
            caps.Categories.AddCategoryMapping(84, NewznabStandardCategory.BooksComics, "a.b.comics.british");
            caps.Categories.AddCategoryMapping(85, NewznabStandardCategory.BooksComics, "a.b.comics.dcp");
            caps.Categories.AddCategoryMapping(86, NewznabStandardCategory.BooksComics, "a.b.comics.reposts");
            caps.Categories.AddCategoryMapping(88, NewznabStandardCategory.Other, "a.b.comp");
            caps.Categories.AddCategoryMapping(89, NewznabStandardCategory.Other, "a.b.console.ps3");
            caps.Categories.AddCategoryMapping(90, NewznabStandardCategory.Other, "a.b.conspiracy");
            caps.Categories.AddCategoryMapping(91, NewznabStandardCategory.Other, "a.b.coolkidweb");
            caps.Categories.AddCategoryMapping(92, NewznabStandardCategory.Other, "a.b.cores");
            caps.Categories.AddCategoryMapping(93, NewznabStandardCategory.Other, "a.b.criterion");
            caps.Categories.AddCategoryMapping(94, NewznabStandardCategory.Other, "a.b.crosspost2");
            caps.Categories.AddCategoryMapping(873, NewznabStandardCategory.Other, "a.b.csv");
            caps.Categories.AddCategoryMapping(95, NewznabStandardCategory.Other, "a.b.ctb");
            caps.Categories.AddCategoryMapping(96, NewznabStandardCategory.Other, "a.b.danskefilm");
            caps.Categories.AddCategoryMapping(97, NewznabStandardCategory.Other, "a.b.dc");
            caps.Categories.AddCategoryMapping(98, NewznabStandardCategory.Other, "a.b.ddf");
            caps.Categories.AddCategoryMapping(99, NewznabStandardCategory.Other, "a.b.de");
            caps.Categories.AddCategoryMapping(100, NewznabStandardCategory.Other, "a.b.department");
            caps.Categories.AddCategoryMapping(101, NewznabStandardCategory.Other, "a.b.department.pron");
            caps.Categories.AddCategoryMapping(102, NewznabStandardCategory.Other, "a.b.dgma");
            caps.Categories.AddCategoryMapping(103, NewznabStandardCategory.Other, "a.b.divx");
            caps.Categories.AddCategoryMapping(104, NewznabStandardCategory.Other, "a.b.divx.french");
            caps.Categories.AddCategoryMapping(105, NewznabStandardCategory.Other, "a.b.divx.german");
            caps.Categories.AddCategoryMapping(106, NewznabStandardCategory.Other, "a.b.divx.movies");
            caps.Categories.AddCategoryMapping(107, NewznabStandardCategory.Other, "a.b.divx.sweden");
            caps.Categories.AddCategoryMapping(108, NewznabStandardCategory.Other, "a.b.documentaries");
            caps.Categories.AddCategoryMapping(109, NewznabStandardCategory.Other, "a.b.documentaries.french");
            caps.Categories.AddCategoryMapping(110, NewznabStandardCategory.Other, "a.b.dominion");
            caps.Categories.AddCategoryMapping(111, NewznabStandardCategory.Other, "a.b.dominion.silly-group");
            caps.Categories.AddCategoryMapping(912, NewznabStandardCategory.Other, "a.b.downunder");
            caps.Categories.AddCategoryMapping(112, NewznabStandardCategory.Other, "a.b.dragonball");
            caps.Categories.AddCategoryMapping(113, NewznabStandardCategory.Other, "a.b.dream");
            caps.Categories.AddCategoryMapping(117, NewznabStandardCategory.Other, "a.b.dream-teamers");
            caps.Categories.AddCategoryMapping(114, NewznabStandardCategory.Other, "a.b.dream.movie");
            caps.Categories.AddCategoryMapping(115, NewznabStandardCategory.Other, "a.b.dream.musik");
            caps.Categories.AddCategoryMapping(116, NewznabStandardCategory.Other, "a.b.dreamcast");
            caps.Categories.AddCategoryMapping(118, NewznabStandardCategory.Other, "a.b.drummers");
            caps.Categories.AddCategoryMapping(119, NewznabStandardCategory.Other, "a.b.drwho");
            caps.Categories.AddCategoryMapping(120, NewznabStandardCategory.Other, "a.b.dump");
            caps.Categories.AddCategoryMapping(121, NewznabStandardCategory.Other, "a.b.dutch.ebook");
            caps.Categories.AddCategoryMapping(122, NewznabStandardCategory.Other, "a.b.dvd");
            caps.Categories.AddCategoryMapping(156, NewznabStandardCategory.Other, "a.b.dvd-covers");
            caps.Categories.AddCategoryMapping(159, NewznabStandardCategory.Other, "a.b.dvd-r");
            caps.Categories.AddCategoryMapping(123, NewznabStandardCategory.Other, "a.b.dvd.animation");
            caps.Categories.AddCategoryMapping(124, NewznabStandardCategory.Other, "a.b.dvd.anime");
            caps.Categories.AddCategoryMapping(125, NewznabStandardCategory.Other, "a.b.dvd.anime.repost");
            caps.Categories.AddCategoryMapping(126, NewznabStandardCategory.Other, "a.b.dvd.asian");
            caps.Categories.AddCategoryMapping(127, NewznabStandardCategory.Other, "a.b.dvd.classic.movies");
            caps.Categories.AddCategoryMapping(128, NewznabStandardCategory.Other, "a.b.dvd.classics");
            caps.Categories.AddCategoryMapping(129, NewznabStandardCategory.Other, "a.b.dvd.criterion");
            caps.Categories.AddCategoryMapping(130, NewznabStandardCategory.Other, "a.b.dvd.english");
            caps.Categories.AddCategoryMapping(131, NewznabStandardCategory.Other, "a.b.dvd.erotica");
            caps.Categories.AddCategoryMapping(132, NewznabStandardCategory.Other, "a.b.dvd.erotica.classics");
            caps.Categories.AddCategoryMapping(133, NewznabStandardCategory.Other, "a.b.dvd.erotica.male");
            caps.Categories.AddCategoryMapping(134, NewznabStandardCategory.Other, "a.b.dvd.french");
            caps.Categories.AddCategoryMapping(135, NewznabStandardCategory.Other, "a.b.dvd.french.repost");
            caps.Categories.AddCategoryMapping(136, NewznabStandardCategory.Other, "a.b.dvd.genealogy");
            caps.Categories.AddCategoryMapping(137, NewznabStandardCategory.Other, "a.b.dvd.german");
            caps.Categories.AddCategoryMapping(138, NewznabStandardCategory.Other, "a.b.dvd.german.repost");
            caps.Categories.AddCategoryMapping(139, NewznabStandardCategory.Other, "a.b.dvd.image");
            caps.Categories.AddCategoryMapping(140, NewznabStandardCategory.Other, "a.b.dvd.image.wii");
            caps.Categories.AddCategoryMapping(141, NewznabStandardCategory.Other, "a.b.dvd.italian");
            caps.Categories.AddCategoryMapping(142, NewznabStandardCategory.Other, "a.b.dvd.midnightmovies");
            caps.Categories.AddCategoryMapping(143, NewznabStandardCategory.Other, "a.b.dvd.misc");
            caps.Categories.AddCategoryMapping(144, NewznabStandardCategory.Other, "a.b.dvd.movies");
            caps.Categories.AddCategoryMapping(145, NewznabStandardCategory.Other, "a.b.dvd.music");
            caps.Categories.AddCategoryMapping(146, NewznabStandardCategory.Other, "a.b.dvd.music.classical");
            caps.Categories.AddCategoryMapping(147, NewznabStandardCategory.Other, "a.b.dvd.ntsc");
            caps.Categories.AddCategoryMapping(148, NewznabStandardCategory.Other, "a.b.dvd.repost");
            caps.Categories.AddCategoryMapping(149, NewznabStandardCategory.Other, "a.b.dvd.spanish");
            caps.Categories.AddCategoryMapping(150, NewznabStandardCategory.Other, "a.b.dvd.swedish");
            caps.Categories.AddCategoryMapping(151, NewznabStandardCategory.Other, "a.b.dvd.war");
            caps.Categories.AddCategoryMapping(152, NewznabStandardCategory.Other, "a.b.dvd2svcd");
            caps.Categories.AddCategoryMapping(153, NewznabStandardCategory.Other, "a.b.dvd9");
            caps.Categories.AddCategoryMapping(154, NewznabStandardCategory.Other, "a.b.dvdcore");
            caps.Categories.AddCategoryMapping(155, NewznabStandardCategory.Other, "a.b.dvdcovers");
            caps.Categories.AddCategoryMapping(157, NewznabStandardCategory.Other, "a.b.dvdnordic.org");
            caps.Categories.AddCategoryMapping(158, NewznabStandardCategory.Other, "a.b.dvdr");
            caps.Categories.AddCategoryMapping(168, NewznabStandardCategory.Other, "a.b.dvdr-tv");
            caps.Categories.AddCategoryMapping(160, NewznabStandardCategory.Other, "a.b.dvdr.asian");
            caps.Categories.AddCategoryMapping(161, NewznabStandardCategory.Other, "a.b.dvdr.french");
            caps.Categories.AddCategoryMapping(162, NewznabStandardCategory.Other, "a.b.dvdr.german");
            caps.Categories.AddCategoryMapping(163, NewznabStandardCategory.Other, "a.b.dvdr.repost");
            caps.Categories.AddCategoryMapping(164, NewznabStandardCategory.Other, "a.b.dvdrcore");
            caps.Categories.AddCategoryMapping(165, NewznabStandardCategory.Other, "a.b.dvdrip");
            caps.Categories.AddCategoryMapping(166, NewznabStandardCategory.Other, "a.b.dvdrs");
            caps.Categories.AddCategoryMapping(167, NewznabStandardCategory.Other, "a.b.dvdrs.pw");
            caps.Categories.AddCategoryMapping(169, NewznabStandardCategory.Other, "a.b.dvds");
            caps.Categories.AddCategoryMapping(171, NewznabStandardCategory.Books, "a.b.e-book");
            caps.Categories.AddCategoryMapping(172, NewznabStandardCategory.Books, "a.b.e-book.fantasy");
            caps.Categories.AddCategoryMapping(173, NewznabStandardCategory.Books, "a.b.e-book.flood");
            caps.Categories.AddCategoryMapping(176, NewznabStandardCategory.BooksForeign, "a.b.e-book.german");
            caps.Categories.AddCategoryMapping(177, NewznabStandardCategory.BooksMags, "a.b.e-book.magazines");
            caps.Categories.AddCategoryMapping(178, NewznabStandardCategory.Books, "a.b.e-book.rpg");
            caps.Categories.AddCategoryMapping(179, NewznabStandardCategory.Books, "a.b.e-book.technical");
            caps.Categories.AddCategoryMapping(180, NewznabStandardCategory.Books, "a.b.e-books");
            caps.Categories.AddCategoryMapping(182, NewznabStandardCategory.BooksForeign, "a.b.e-books.german");
            caps.Categories.AddCategoryMapping(170, NewznabStandardCategory.Books, "a.b.ebook");
            caps.Categories.AddCategoryMapping(174, NewznabStandardCategory.Books, "a.b.ebook.french");
            caps.Categories.AddCategoryMapping(175, NewznabStandardCategory.Books, "a.b.ebook.german");
            caps.Categories.AddCategoryMapping(913, NewznabStandardCategory.Books, "a.b.ebook.magazines");
            caps.Categories.AddCategoryMapping(181, NewznabStandardCategory.Books, "a.b.ebooks.german");
            caps.Categories.AddCategoryMapping(183, NewznabStandardCategory.Other, "a.b.echange-web");
            caps.Categories.AddCategoryMapping(184, NewznabStandardCategory.Other, "a.b.emulator");
            caps.Categories.AddCategoryMapping(185, NewznabStandardCategory.Other, "a.b.emulators");
            caps.Categories.AddCategoryMapping(186, NewznabStandardCategory.Other, "a.b.emulators.arcade");
            caps.Categories.AddCategoryMapping(187, NewznabStandardCategory.Other, "a.b.emulators.gameboy.advance");
            caps.Categories.AddCategoryMapping(188, NewznabStandardCategory.Other, "a.b.emulators.mame");
            caps.Categories.AddCategoryMapping(189, NewznabStandardCategory.Other, "a.b.emulators.misc");
            caps.Categories.AddCategoryMapping(190, NewznabStandardCategory.Other, "a.b.emulators.nintendo");
            caps.Categories.AddCategoryMapping(191, NewznabStandardCategory.Other, "a.b.emulators.nintendo-64");
            caps.Categories.AddCategoryMapping(192, NewznabStandardCategory.Other, "a.b.emulators.nintendo-ds");
            caps.Categories.AddCategoryMapping(193, NewznabStandardCategory.Other, "a.b.emulators.playstation");
            caps.Categories.AddCategoryMapping(930, NewznabStandardCategory.Other, "a.b.encrypted");
            caps.Categories.AddCategoryMapping(194, NewznabStandardCategory.BooksEBook, "a.b.epub");
            caps.Categories.AddCategoryMapping(195, NewznabStandardCategory.BooksEBook, "a.b.epub.dutch");
            caps.Categories.AddCategoryMapping(196, NewznabStandardCategory.XXX, "a.b.erotica");
            caps.Categories.AddCategoryMapping(206, NewznabStandardCategory.XXX, "a.b.erotica-underground");
            caps.Categories.AddCategoryMapping(197, NewznabStandardCategory.XXX, "a.b.erotica.collections.rars");
            caps.Categories.AddCategoryMapping(198, NewznabStandardCategory.XXX, "a.b.erotica.divx");
            caps.Categories.AddCategoryMapping(199, NewznabStandardCategory.XXXDVD, "a.b.erotica.dvd");
            caps.Categories.AddCategoryMapping(200, NewznabStandardCategory.XXX, "a.b.erotica.nospam.creampie");
            caps.Categories.AddCategoryMapping(201, NewznabStandardCategory.XXX, "a.b.erotica.older-woman");
            caps.Categories.AddCategoryMapping(202, NewznabStandardCategory.XXX, "a.b.erotica.pornstars.80s");
            caps.Categories.AddCategoryMapping(203, NewznabStandardCategory.XXX, "a.b.erotica.pornstars.90s");
            caps.Categories.AddCategoryMapping(928, NewznabStandardCategory.XXX, "a.b.erotica.sex");
            caps.Categories.AddCategoryMapping(204, NewznabStandardCategory.XXX, "a.b.erotica.urine");
            caps.Categories.AddCategoryMapping(205, NewznabStandardCategory.XXX, "a.b.erotica.vcd");
            caps.Categories.AddCategoryMapping(207, NewznabStandardCategory.Other, "a.b.etc");
            caps.Categories.AddCategoryMapping(914, NewznabStandardCategory.Other, "a.b.faded-glory");
            caps.Categories.AddCategoryMapping(208, NewznabStandardCategory.Other, "a.b.fetish.scat");
            caps.Categories.AddCategoryMapping(209, NewznabStandardCategory.Other, "a.b.film");
            caps.Categories.AddCategoryMapping(210, NewznabStandardCategory.Other, "a.b.filmclub");
            caps.Categories.AddCategoryMapping(211, NewznabStandardCategory.Other, "a.b.fitness");
            caps.Categories.AddCategoryMapping(871, NewznabStandardCategory.Other, "a.b.flowed");
            caps.Categories.AddCategoryMapping(941, NewznabStandardCategory.Other, "a.b.font");
            caps.Categories.AddCategoryMapping(212, NewznabStandardCategory.Other, "a.b.fonts");
            caps.Categories.AddCategoryMapping(213, NewznabStandardCategory.Other, "a.b.fonts.floods");
            caps.Categories.AddCategoryMapping(214, NewznabStandardCategory.Other, "a.b.formula1");
            caps.Categories.AddCategoryMapping(215, NewznabStandardCategory.Other, "a.b.freeware");
            caps.Categories.AddCategoryMapping(216, NewznabStandardCategory.Other, "a.b.freewareclub");
            caps.Categories.AddCategoryMapping(217, NewznabStandardCategory.Other, "a.b.french");
            caps.Categories.AddCategoryMapping(218, NewznabStandardCategory.Other, "a.b.french-tv");
            caps.Categories.AddCategoryMapping(949, NewznabStandardCategory.Other, "a.b.friends");
            caps.Categories.AddCategoryMapping(870, NewznabStandardCategory.Other, "a.b.frogs");
            caps.Categories.AddCategoryMapping(219, NewznabStandardCategory.Other, "a.b.fta");
            caps.Categories.AddCategoryMapping(220, NewznabStandardCategory.Other, "a.b.ftb");
            caps.Categories.AddCategoryMapping(221, NewznabStandardCategory.Other, "a.b.ftd");
            caps.Categories.AddCategoryMapping(222, NewznabStandardCategory.Other, "a.b.ftd.nzb");
            caps.Categories.AddCategoryMapping(223, NewznabStandardCategory.Other, "a.b.ftn");
            caps.Categories.AddCategoryMapping(224, NewznabStandardCategory.Other, "a.b.ftn.applications");
            caps.Categories.AddCategoryMapping(225, NewznabStandardCategory.Other, "a.b.ftn.games");
            caps.Categories.AddCategoryMapping(226, NewznabStandardCategory.Other, "a.b.ftn.movie");
            caps.Categories.AddCategoryMapping(227, NewznabStandardCategory.Other, "a.b.ftn.nzb");
            caps.Categories.AddCategoryMapping(228, NewznabStandardCategory.Other, "a.b.ftr");
            caps.Categories.AddCategoryMapping(229, NewznabStandardCategory.Other, "a.b.ftwclub");
            caps.Categories.AddCategoryMapping(230, NewznabStandardCategory.Other, "a.b.fz");
            caps.Categories.AddCategoryMapping(231, NewznabStandardCategory.Other, "a.b.galaxy4all");
            caps.Categories.AddCategoryMapping(232, NewznabStandardCategory.Other, "a.b.game");
            caps.Categories.AddCategoryMapping(233, NewznabStandardCategory.Console, "a.b.gamecube");
            caps.Categories.AddCategoryMapping(234, NewznabStandardCategory.Console, "a.b.games");
            caps.Categories.AddCategoryMapping(235, NewznabStandardCategory.Console, "a.b.games.adventures");
            caps.Categories.AddCategoryMapping(236, NewznabStandardCategory.Console, "a.b.games.dox");
            caps.Categories.AddCategoryMapping(237, NewznabStandardCategory.Console, "a.b.games.encrypted");
            caps.Categories.AddCategoryMapping(238, NewznabStandardCategory.Console, "a.b.games.kidstuff");
            caps.Categories.AddCategoryMapping(239, NewznabStandardCategory.Console, "a.b.games.kidstuff.nl");
            caps.Categories.AddCategoryMapping(240, NewznabStandardCategory.Console, "a.b.games.nintendo3ds");
            caps.Categories.AddCategoryMapping(241, NewznabStandardCategory.Console, "a.b.games.nintendods");
            caps.Categories.AddCategoryMapping(242, NewznabStandardCategory.Console, "a.b.games.repost");
            caps.Categories.AddCategoryMapping(243, NewznabStandardCategory.Console, "a.b.games.reposts");
            caps.Categories.AddCategoryMapping(244, NewznabStandardCategory.ConsoleWii, "a.b.games.wii");
            caps.Categories.AddCategoryMapping(245, NewznabStandardCategory.Console, "a.b.games.worms");
            caps.Categories.AddCategoryMapping(246, NewznabStandardCategory.ConsoleXBox, "a.b.games.xbox");
            caps.Categories.AddCategoryMapping(247, NewznabStandardCategory.ConsoleXBox360, "a.b.games.xbox360");
            caps.Categories.AddCategoryMapping(248, NewznabStandardCategory.Other, "a.b.german.divx");
            caps.Categories.AddCategoryMapping(249, NewznabStandardCategory.Other, "a.b.german.movies");
            caps.Categories.AddCategoryMapping(250, NewznabStandardCategory.Other, "a.b.german.mp3");
            caps.Categories.AddCategoryMapping(251, NewznabStandardCategory.Other, "a.b.ghosts");
            caps.Categories.AddCategoryMapping(252, NewznabStandardCategory.Other, "a.b.global.quake");
            caps.Categories.AddCategoryMapping(872, NewznabStandardCategory.Other, "a.b.goat");
            caps.Categories.AddCategoryMapping(253, NewznabStandardCategory.Other, "a.b.goonies");
            caps.Categories.AddCategoryMapping(254, NewznabStandardCategory.Other, "a.b.gougouland");
            caps.Categories.AddCategoryMapping(915, NewznabStandardCategory.Other, "a.b.graveyard");
            caps.Categories.AddCategoryMapping(255, NewznabStandardCategory.Other, "a.b.guitar.tab");
            caps.Categories.AddCategoryMapping(256, NewznabStandardCategory.Other, "a.b.hd.ger.moviez");
            caps.Categories.AddCategoryMapping(257, NewznabStandardCategory.TV, "a.b.hdtv");
            caps.Categories.AddCategoryMapping(258, NewznabStandardCategory.Other, "a.b.hdtv.french");
            caps.Categories.AddCategoryMapping(259, NewznabStandardCategory.Other, "a.b.hdtv.french.repost");
            caps.Categories.AddCategoryMapping(260, NewznabStandardCategory.TVForeign, "a.b.hdtv.german");
            caps.Categories.AddCategoryMapping(261, NewznabStandardCategory.TVForeign, "a.b.hdtv.german-audio");
            caps.Categories.AddCategoryMapping(262, NewznabStandardCategory.Other, "a.b.hdtv.repost");
            caps.Categories.AddCategoryMapping(263, NewznabStandardCategory.TV, "a.b.hdtv.tv-episodes");
            caps.Categories.AddCategoryMapping(264, NewznabStandardCategory.TV, "a.b.hdtv.x264");
            caps.Categories.AddCategoryMapping(265, NewznabStandardCategory.TVForeign, "a.b.hdtv.x264.french");
            caps.Categories.AddCategoryMapping(266, NewznabStandardCategory.Other, "a.b.highspeed");
            caps.Categories.AddCategoryMapping(267, NewznabStandardCategory.Other, "a.b.hitoshirezu");
            caps.Categories.AddCategoryMapping(268, NewznabStandardCategory.Other, "a.b.holiday");
            caps.Categories.AddCategoryMapping(269, NewznabStandardCategory.Other, "a.b.hotrod");
            caps.Categories.AddCategoryMapping(270, NewznabStandardCategory.Other, "a.b.hou");
            caps.Categories.AddCategoryMapping(271, NewznabStandardCategory.Other, "a.b.howard-stern");
            caps.Categories.AddCategoryMapping(272, NewznabStandardCategory.Other, "a.b.howard-stern.on-demand");
            caps.Categories.AddCategoryMapping(273, NewznabStandardCategory.Other, "a.b.hunters");
            caps.Categories.AddCategoryMapping(274, NewznabStandardCategory.Other, "a.b.hunters.movie");
            caps.Categories.AddCategoryMapping(275, NewznabStandardCategory.Other, "a.b.hunters.musik");
            caps.Categories.AddCategoryMapping(277, NewznabStandardCategory.Other, "a.b.ibm-pc");
            caps.Categories.AddCategoryMapping(278, NewznabStandardCategory.Other, "a.b.ibm-pc.games");
            caps.Categories.AddCategoryMapping(279, NewznabStandardCategory.Other, "a.b.ibm-pc.warez");
            caps.Categories.AddCategoryMapping(276, NewznabStandardCategory.Other, "a.b.ibm.pc.warez");
            caps.Categories.AddCategoryMapping(280, NewznabStandardCategory.Other, "a.b.ijsklontje");
            caps.Categories.AddCategoryMapping(281, NewznabStandardCategory.Other, "a.b.illuminaten");
            caps.Categories.AddCategoryMapping(282, NewznabStandardCategory.Other, "a.b.image");
            caps.Categories.AddCategoryMapping(283, NewznabStandardCategory.Other, "a.b.image.cd.french");
            caps.Categories.AddCategoryMapping(284, NewznabStandardCategory.Other, "a.b.image.games");
            caps.Categories.AddCategoryMapping(285, NewznabStandardCategory.Other, "a.b.images");
            caps.Categories.AddCategoryMapping(287, NewznabStandardCategory.Other, "a.b.images.afos-fans");
            caps.Categories.AddCategoryMapping(286, NewznabStandardCategory.Other, "a.b.images.afos.fans");
            caps.Categories.AddCategoryMapping(288, NewznabStandardCategory.Other, "a.b.inner-sanctum");
            caps.Categories.AddCategoryMapping(289, NewznabStandardCategory.Other, "a.b.insiderz");
            caps.Categories.AddCategoryMapping(935, NewznabStandardCategory.Other, "a.b.ipod.videos");
            caps.Categories.AddCategoryMapping(936, NewznabStandardCategory.Other, "a.b.ipod.videos.movies");
            caps.Categories.AddCategoryMapping(290, NewznabStandardCategory.Other, "a.b.iso");
            caps.Categories.AddCategoryMapping(291, NewznabStandardCategory.Other, "a.b.japan.aidoru");
            caps.Categories.AddCategoryMapping(292, NewznabStandardCategory.Other, "a.b.japan.fashion.j-class");
            caps.Categories.AddCategoryMapping(293, NewznabStandardCategory.Other, "a.b.japan.iroppoi");
            caps.Categories.AddCategoryMapping(294, NewznabStandardCategory.Other, "a.b.jph");
            caps.Categories.AddCategoryMapping(295, NewznabStandardCategory.Other, "a.b.just4fun.nl");
            caps.Categories.AddCategoryMapping(296, NewznabStandardCategory.Other, "a.b.karagarga");
            caps.Categories.AddCategoryMapping(297, NewznabStandardCategory.Other, "a.b.kenpsx");
            caps.Categories.AddCategoryMapping(298, NewznabStandardCategory.Other, "a.b.kleverig");
            caps.Categories.AddCategoryMapping(299, NewznabStandardCategory.Other, "a.b.korea");
            caps.Categories.AddCategoryMapping(300, NewznabStandardCategory.Other, "a.b.laumovie.nl");
            caps.Categories.AddCategoryMapping(301, NewznabStandardCategory.Other, "a.b.librateam");
            caps.Categories.AddCategoryMapping(302, NewznabStandardCategory.Other, "a.b.lou");
            caps.Categories.AddCategoryMapping(303, NewznabStandardCategory.Other, "a.b.lucas-arts");
            caps.Categories.AddCategoryMapping(304, NewznabStandardCategory.Other, "a.b.mac");
            caps.Categories.AddCategoryMapping(305, NewznabStandardCategory.Other, "a.b.mac.applications");
            caps.Categories.AddCategoryMapping(306, NewznabStandardCategory.Other, "a.b.mac.apps");
            caps.Categories.AddCategoryMapping(307, NewznabStandardCategory.Other, "a.b.mac.audio");
            caps.Categories.AddCategoryMapping(308, NewznabStandardCategory.Other, "a.b.mac.cd-images");
            caps.Categories.AddCategoryMapping(309, NewznabStandardCategory.Other, "a.b.mac.games");
            caps.Categories.AddCategoryMapping(310, NewznabStandardCategory.Other, "a.b.mac.osx.apps");
            caps.Categories.AddCategoryMapping(311, NewznabStandardCategory.Other, "a.b.madcow.highspeed");
            caps.Categories.AddCategoryMapping(312, NewznabStandardCategory.Other, "a.b.magic");
            caps.Categories.AddCategoryMapping(313, NewznabStandardCategory.Other, "a.b.masternzb");
            caps.Categories.AddCategoryMapping(314, NewznabStandardCategory.Other, "a.b.matrix");
            caps.Categories.AddCategoryMapping(315, NewznabStandardCategory.Other, "a.b.misc");
            caps.Categories.AddCategoryMapping(929, NewznabStandardCategory.Other, "a.b.misc.xxx");
            caps.Categories.AddCategoryMapping(316, NewznabStandardCategory.Other, "a.b.mma");
            caps.Categories.AddCategoryMapping(317, NewznabStandardCategory.Other, "a.b.models");
            caps.Categories.AddCategoryMapping(318, NewznabStandardCategory.Other, "a.b.mojo");
            caps.Categories.AddCategoryMapping(319, NewznabStandardCategory.Other, "a.b.mom");
            caps.Categories.AddCategoryMapping(320, NewznabStandardCategory.Other, "a.b.mom.xxx");
            caps.Categories.AddCategoryMapping(321, NewznabStandardCategory.Other, "a.b.monster-movies");
            caps.Categories.AddCategoryMapping(322, NewznabStandardCategory.Other, "a.b.monter-movies");
            caps.Categories.AddCategoryMapping(323, NewznabStandardCategory.Other, "a.b.moovee");
            caps.Categories.AddCategoryMapping(324, NewznabStandardCategory.Other, "a.b.mou");
            caps.Categories.AddCategoryMapping(325, NewznabStandardCategory.Movies, "a.b.movie");
            caps.Categories.AddCategoryMapping(326, NewznabStandardCategory.Other, "a.b.moviereleases.nl");
            caps.Categories.AddCategoryMapping(327, NewznabStandardCategory.Movies, "a.b.movies");
            caps.Categories.AddCategoryMapping(328, NewznabStandardCategory.Movies, "a.b.movies.arthouse");
            caps.Categories.AddCategoryMapping(329, NewznabStandardCategory.Movies, "a.b.movies.classic");
            caps.Categories.AddCategoryMapping(330, NewznabStandardCategory.Movies, "a.b.movies.divx");
            caps.Categories.AddCategoryMapping(331, NewznabStandardCategory.MoviesForeign, "a.b.movies.divx.france");
            caps.Categories.AddCategoryMapping(332, NewznabStandardCategory.MoviesForeign, "a.b.movies.divx.french");
            caps.Categories.AddCategoryMapping(333, NewznabStandardCategory.MoviesForeign, "a.b.movies.divx.french.old");
            caps.Categories.AddCategoryMapping(334, NewznabStandardCategory.MoviesForeign, "a.b.movies.divx.french.reposts");
            caps.Categories.AddCategoryMapping(335, NewznabStandardCategory.MoviesForeign, "a.b.movies.divx.french.vost");
            caps.Categories.AddCategoryMapping(336, NewznabStandardCategory.MoviesForeign, "a.b.movies.divx.german");
            caps.Categories.AddCategoryMapping(337, NewznabStandardCategory.Movies, "a.b.movies.divx.repost");
            caps.Categories.AddCategoryMapping(338, NewznabStandardCategory.MoviesForeign, "a.b.movies.divx.russian");
            caps.Categories.AddCategoryMapping(339, NewznabStandardCategory.MoviesForeign, "a.b.movies.dutch");
            caps.Categories.AddCategoryMapping(340, NewznabStandardCategory.MoviesForeign, "a.b.movies.dutch.repost");
            caps.Categories.AddCategoryMapping(341, NewznabStandardCategory.Movies, "a.b.movies.dvd");
            caps.Categories.AddCategoryMapping(342, NewznabStandardCategory.Movies, "a.b.movies.dvd-r");
            caps.Categories.AddCategoryMapping(343, NewznabStandardCategory.XXX, "a.b.movies.erotica");
            caps.Categories.AddCategoryMapping(344, NewznabStandardCategory.MoviesForeign, "a.b.movies.french");
            caps.Categories.AddCategoryMapping(943, NewznabStandardCategory.Movies, "a.b.movies.from.hell");
            caps.Categories.AddCategoryMapping(345, NewznabStandardCategory.Movies, "a.b.movies.gay");
            caps.Categories.AddCategoryMapping(346, NewznabStandardCategory.MoviesForeign, "a.b.movies.german");
            caps.Categories.AddCategoryMapping(347, NewznabStandardCategory.MoviesForeign, "a.b.movies.italian.divx");
            caps.Categories.AddCategoryMapping(348, NewznabStandardCategory.Movies, "a.b.movies.kidstuff");
            caps.Categories.AddCategoryMapping(349, NewznabStandardCategory.Movies, "a.b.movies.martial.arts");
            caps.Categories.AddCategoryMapping(350, NewznabStandardCategory.Movies, "a.b.movies.mkv");
            caps.Categories.AddCategoryMapping(351, NewznabStandardCategory.Movies, "a.b.movies.purity");
            caps.Categories.AddCategoryMapping(352, NewznabStandardCategory.Movies, "a.b.movies.repost");
            caps.Categories.AddCategoryMapping(353, NewznabStandardCategory.Movies, "a.b.movies.shadowrealm");
            caps.Categories.AddCategoryMapping(354, NewznabStandardCategory.MoviesForeign, "a.b.movies.spanish");
            caps.Categories.AddCategoryMapping(355, NewznabStandardCategory.MoviesForeign, "a.b.movies.swedish");
            caps.Categories.AddCategoryMapping(356, NewznabStandardCategory.Movies, "a.b.movies.thelostmovies");
            caps.Categories.AddCategoryMapping(357, NewznabStandardCategory.Movies, "a.b.movies.war");
            caps.Categories.AddCategoryMapping(358, NewznabStandardCategory.Movies, "a.b.movies.x264");
            caps.Categories.AddCategoryMapping(359, NewznabStandardCategory.MoviesSD, "a.b.movies.xvid");
            caps.Categories.AddCategoryMapping(360, NewznabStandardCategory.Other, "a.b.movies.zeromovies");
            caps.Categories.AddCategoryMapping(361, NewznabStandardCategory.Other, "a.b.moviez.ger");
            caps.Categories.AddCategoryMapping(362, NewznabStandardCategory.Other, "a.b.mp3");
            caps.Categories.AddCategoryMapping(363, NewznabStandardCategory.Other, "a.b.mp3.abooks");
            caps.Categories.AddCategoryMapping(364, NewznabStandardCategory.Other, "a.b.mp3.audiobooks");
            caps.Categories.AddCategoryMapping(365, NewznabStandardCategory.Other, "a.b.mp3.audiobooks.highspeed");
            caps.Categories.AddCategoryMapping(366, NewznabStandardCategory.Other, "a.b.mp3.audiobooks.repost");
            caps.Categories.AddCategoryMapping(367, NewznabStandardCategory.Other, "a.b.mp3.bootlegs");
            caps.Categories.AddCategoryMapping(368, NewznabStandardCategory.Other, "a.b.mp3.comedy");
            caps.Categories.AddCategoryMapping(369, NewznabStandardCategory.Other, "a.b.mp3.complete_cd");
            caps.Categories.AddCategoryMapping(370, NewznabStandardCategory.Other, "a.b.mp3.dance");
            caps.Categories.AddCategoryMapping(371, NewznabStandardCategory.Other, "a.b.mp3.full_albums");
            caps.Categories.AddCategoryMapping(372, NewznabStandardCategory.Other, "a.b.mp3.german.hoerbuecher");
            caps.Categories.AddCategoryMapping(373, NewznabStandardCategory.Other, "a.b.mp3.hoerspiele");
            caps.Categories.AddCategoryMapping(374, NewznabStandardCategory.Other, "a.b.mpeg");
            caps.Categories.AddCategoryMapping(375, NewznabStandardCategory.Other, "a.b.mpeg.video");
            caps.Categories.AddCategoryMapping(376, NewznabStandardCategory.Other, "a.b.mpeg.video.music");
            caps.Categories.AddCategoryMapping(377, NewznabStandardCategory.Other, "a.b.mpeg.videos");
            caps.Categories.AddCategoryMapping(378, NewznabStandardCategory.Other, "a.b.mpeg.videos.country");
            caps.Categories.AddCategoryMapping(379, NewznabStandardCategory.Other, "a.b.mpeg.videos.german");
            caps.Categories.AddCategoryMapping(380, NewznabStandardCategory.Other, "a.b.mpeg.videos.music");
            caps.Categories.AddCategoryMapping(864, NewznabStandardCategory.Other, "a.b.ms-windows");
            caps.Categories.AddCategoryMapping(381, NewznabStandardCategory.Other, "a.b.mst3k.riffs.etc.nopasswords");
            caps.Categories.AddCategoryMapping(382, NewznabStandardCategory.Other, "a.b.multimedia");
            caps.Categories.AddCategoryMapping(383, NewznabStandardCategory.Other, "a.b.multimedia.24");
            caps.Categories.AddCategoryMapping(384, NewznabStandardCategory.Other, "a.b.multimedia.alias");
            caps.Categories.AddCategoryMapping(385, NewznabStandardCategory.Other, "a.b.multimedia.anime");
            caps.Categories.AddCategoryMapping(386, NewznabStandardCategory.Other, "a.b.multimedia.anime.highspeed");
            caps.Categories.AddCategoryMapping(387, NewznabStandardCategory.Other, "a.b.multimedia.anime.repost");
            caps.Categories.AddCategoryMapping(388, NewznabStandardCategory.Other, "a.b.multimedia.aviation");
            caps.Categories.AddCategoryMapping(389, NewznabStandardCategory.Other, "a.b.multimedia.babylon5");
            caps.Categories.AddCategoryMapping(390, NewznabStandardCategory.Other, "a.b.multimedia.bdsm");
            caps.Categories.AddCategoryMapping(391, NewznabStandardCategory.Other, "a.b.multimedia.buffy-v-slayer");
            caps.Categories.AddCategoryMapping(392, NewznabStandardCategory.Other, "a.b.multimedia.cartoons");
            caps.Categories.AddCategoryMapping(393, NewznabStandardCategory.Other, "a.b.multimedia.cartoons.looneytunes");
            caps.Categories.AddCategoryMapping(394, NewznabStandardCategory.Other, "a.b.multimedia.cartoons.repost");
            caps.Categories.AddCategoryMapping(395, NewznabStandardCategory.Other, "a.b.multimedia.charmed");
            caps.Categories.AddCategoryMapping(396, NewznabStandardCategory.Other, "a.b.multimedia.chinese");
            caps.Categories.AddCategoryMapping(398, NewznabStandardCategory.Other, "a.b.multimedia.classic-films");
            caps.Categories.AddCategoryMapping(397, NewznabStandardCategory.Other, "a.b.multimedia.classical.treblevoices");
            caps.Categories.AddCategoryMapping(399, NewznabStandardCategory.Other, "a.b.multimedia.comedy");
            caps.Categories.AddCategoryMapping(400, NewznabStandardCategory.Other, "a.b.multimedia.comedy.british");
            caps.Categories.AddCategoryMapping(401, NewznabStandardCategory.Other, "a.b.multimedia.cooking");
            caps.Categories.AddCategoryMapping(402, NewznabStandardCategory.Other, "a.b.multimedia.csi");
            caps.Categories.AddCategoryMapping(403, NewznabStandardCategory.Other, "a.b.multimedia.disney");
            caps.Categories.AddCategoryMapping(404, NewznabStandardCategory.Other, "a.b.multimedia.disney.parks");
            caps.Categories.AddCategoryMapping(405, NewznabStandardCategory.Other, "a.b.multimedia.divx");
            caps.Categories.AddCategoryMapping(406, NewznabStandardCategory.Other, "a.b.multimedia.documentaries");
            caps.Categories.AddCategoryMapping(407, NewznabStandardCategory.Other, "a.b.multimedia.elvispresley");
            caps.Categories.AddCategoryMapping(408, NewznabStandardCategory.Other, "a.b.multimedia.erotic.playboy");
            caps.Categories.AddCategoryMapping(409, NewznabStandardCategory.XXX, "a.b.multimedia.erotica");
            caps.Categories.AddCategoryMapping(410, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.amateur");
            caps.Categories.AddCategoryMapping(411, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.amature");
            caps.Categories.AddCategoryMapping(412, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.anime");
            caps.Categories.AddCategoryMapping(413, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.asian");
            caps.Categories.AddCategoryMapping(414, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.black");
            caps.Categories.AddCategoryMapping(415, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.interracial");
            caps.Categories.AddCategoryMapping(416, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.lesbians");
            caps.Categories.AddCategoryMapping(417, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.male");
            caps.Categories.AddCategoryMapping(418, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.male.repost");
            caps.Categories.AddCategoryMapping(419, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.plumpers");
            caps.Categories.AddCategoryMapping(420, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.repost");
            caps.Categories.AddCategoryMapping(421, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.strap-on-sex");
            caps.Categories.AddCategoryMapping(422, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.transexuals");
            caps.Categories.AddCategoryMapping(423, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.transsexuals");
            caps.Categories.AddCategoryMapping(424, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.urine");
            caps.Categories.AddCategoryMapping(425, NewznabStandardCategory.XXX, "a.b.multimedia.erotica.voyeurism");
            caps.Categories.AddCategoryMapping(426, NewznabStandardCategory.Other, "a.b.multimedia.fitness");
            caps.Categories.AddCategoryMapping(427, NewznabStandardCategory.Other, "a.b.multimedia.futurama");
            caps.Categories.AddCategoryMapping(428, NewznabStandardCategory.Other, "a.b.multimedia.horror");
            caps.Categories.AddCategoryMapping(429, NewznabStandardCategory.Other, "a.b.multimedia.japanese");
            caps.Categories.AddCategoryMapping(430, NewznabStandardCategory.Other, "a.b.multimedia.japanese.repost");
            caps.Categories.AddCategoryMapping(431, NewznabStandardCategory.Other, "a.b.multimedia.late-night-talkshows");
            caps.Categories.AddCategoryMapping(432, NewznabStandardCategory.Other, "a.b.multimedia.mst3k");
            caps.Categories.AddCategoryMapping(433, NewznabStandardCategory.Other, "a.b.multimedia.musicals");
            caps.Categories.AddCategoryMapping(434, NewznabStandardCategory.XXX, "a.b.multimedia.nude.celebrities");
            caps.Categories.AddCategoryMapping(435, NewznabStandardCategory.Other, "a.b.multimedia.prince");
            caps.Categories.AddCategoryMapping(436, NewznabStandardCategory.Other, "a.b.multimedia.rail");
            caps.Categories.AddCategoryMapping(437, NewznabStandardCategory.Other, "a.b.multimedia.rap");
            caps.Categories.AddCategoryMapping(438, NewznabStandardCategory.Other, "a.b.multimedia.repost");
            caps.Categories.AddCategoryMapping(439, NewznabStandardCategory.Other, "a.b.multimedia.reposts");
            caps.Categories.AddCategoryMapping(441, NewznabStandardCategory.Other, "a.b.multimedia.sci-fi");
            caps.Categories.AddCategoryMapping(440, NewznabStandardCategory.Other, "a.b.multimedia.scifi");
            caps.Categories.AddCategoryMapping(442, NewznabStandardCategory.Other, "a.b.multimedia.scifi-and-fantasy");
            caps.Categories.AddCategoryMapping(443, NewznabStandardCategory.Other, "a.b.multimedia.sd-6");
            caps.Categories.AddCategoryMapping(444, NewznabStandardCategory.Other, "a.b.multimedia.sitcoms");
            caps.Categories.AddCategoryMapping(445, NewznabStandardCategory.Other, "a.b.multimedia.smallville");
            caps.Categories.AddCategoryMapping(446, NewznabStandardCategory.Other, "a.b.multimedia.sports");
            caps.Categories.AddCategoryMapping(447, NewznabStandardCategory.Other, "a.b.multimedia.startrek");
            caps.Categories.AddCategoryMapping(449, NewznabStandardCategory.Other, "a.b.multimedia.teen-idols");
            caps.Categories.AddCategoryMapping(448, NewznabStandardCategory.Other, "a.b.multimedia.teen.male");
            caps.Categories.AddCategoryMapping(450, NewznabStandardCategory.Other, "a.b.multimedia.thai");
            caps.Categories.AddCategoryMapping(451, NewznabStandardCategory.Other, "a.b.multimedia.utilities");
            caps.Categories.AddCategoryMapping(452, NewznabStandardCategory.Other, "a.b.multimedia.vietnamese");
            caps.Categories.AddCategoryMapping(453, NewznabStandardCategory.Other, "a.b.multimedia.vintage-film");
            caps.Categories.AddCategoryMapping(454, NewznabStandardCategory.Other, "a.b.multimedia.vintage-film.pre-1960");
            caps.Categories.AddCategoryMapping(455, NewznabStandardCategory.Other, "a.b.multimedia.vintage-tv");
            caps.Categories.AddCategoryMapping(456, NewznabStandardCategory.Audio, "a.b.music");
            caps.Categories.AddCategoryMapping(457, NewznabStandardCategory.Audio, "a.b.music.classical");
            caps.Categories.AddCategoryMapping(458, NewznabStandardCategory.Audio, "a.b.music.dvd");
            caps.Categories.AddCategoryMapping(459, NewznabStandardCategory.Audio, "a.b.music.heavy-metal");
            caps.Categories.AddCategoryMapping(460, NewznabStandardCategory.Audio, "a.b.music.jungle");
            caps.Categories.AddCategoryMapping(461, NewznabStandardCategory.Audio, "a.b.music.makers.samples");
            caps.Categories.AddCategoryMapping(462, NewznabStandardCategory.AudioMP3, "a.b.music.mp3");
            caps.Categories.AddCategoryMapping(463, NewznabStandardCategory.Audio, "a.b.music.oasis");
            caps.Categories.AddCategoryMapping(464, NewznabStandardCategory.Audio, "a.b.music.springsteen");
            caps.Categories.AddCategoryMapping(465, NewznabStandardCategory.Audio, "a.b.music.techno");
            caps.Categories.AddCategoryMapping(466, NewznabStandardCategory.AudioVideo, "a.b.music.videos");
            caps.Categories.AddCategoryMapping(467, NewznabStandardCategory.Other, "a.b.mutlimedia");
            caps.Categories.AddCategoryMapping(468, NewznabStandardCategory.Other, "a.b.nerodigital");
            caps.Categories.AddCategoryMapping(469, NewznabStandardCategory.Other, "a.b.new-movies");
            caps.Categories.AddCategoryMapping(470, NewznabStandardCategory.Other, "a.b.newzbin");
            caps.Categories.AddCategoryMapping(875, NewznabStandardCategory.Other, "a.b.newznzb.alpha");
            caps.Categories.AddCategoryMapping(876, NewznabStandardCategory.Other, "a.b.newznzb.beta");
            caps.Categories.AddCategoryMapping(877, NewznabStandardCategory.Other, "a.b.newznzb.bravo");
            caps.Categories.AddCategoryMapping(878, NewznabStandardCategory.Other, "a.b.newznzb.charlie");
            caps.Categories.AddCategoryMapping(879, NewznabStandardCategory.Other, "a.b.newznzb.delta");
            caps.Categories.AddCategoryMapping(880, NewznabStandardCategory.Other, "a.b.newznzb.echo");
            caps.Categories.AddCategoryMapping(881, NewznabStandardCategory.Other, "a.b.newznzb.foxtrot");
            caps.Categories.AddCategoryMapping(882, NewznabStandardCategory.Other, "a.b.newznzb.golf");
            caps.Categories.AddCategoryMapping(883, NewznabStandardCategory.Other, "a.b.newznzb.hotel");
            caps.Categories.AddCategoryMapping(884, NewznabStandardCategory.Other, "a.b.newznzb.india");
            caps.Categories.AddCategoryMapping(885, NewznabStandardCategory.Other, "a.b.newznzb.juliet");
            caps.Categories.AddCategoryMapping(886, NewznabStandardCategory.Other, "a.b.newznzb.juliett");
            caps.Categories.AddCategoryMapping(887, NewznabStandardCategory.Other, "a.b.newznzb.kilo");
            caps.Categories.AddCategoryMapping(888, NewznabStandardCategory.Other, "a.b.newznzb.lima");
            caps.Categories.AddCategoryMapping(889, NewznabStandardCategory.Other, "a.b.newznzb.mike");
            caps.Categories.AddCategoryMapping(890, NewznabStandardCategory.Other, "a.b.newznzb.november");
            caps.Categories.AddCategoryMapping(891, NewznabStandardCategory.Other, "a.b.newznzb.novemeber");
            caps.Categories.AddCategoryMapping(892, NewznabStandardCategory.Other, "a.b.newznzb.oscar");
            caps.Categories.AddCategoryMapping(893, NewznabStandardCategory.Other, "a.b.newznzb.papa");
            caps.Categories.AddCategoryMapping(894, NewznabStandardCategory.Other, "a.b.newznzb.quebec");
            caps.Categories.AddCategoryMapping(895, NewznabStandardCategory.Other, "a.b.newznzb.romeo");
            caps.Categories.AddCategoryMapping(896, NewznabStandardCategory.Other, "a.b.newznzb.sierra");
            caps.Categories.AddCategoryMapping(897, NewznabStandardCategory.Other, "a.b.newznzb.tango");
            caps.Categories.AddCategoryMapping(898, NewznabStandardCategory.Other, "a.b.newznzb.uniform");
            caps.Categories.AddCategoryMapping(899, NewznabStandardCategory.Other, "a.b.newznzb.victor");
            caps.Categories.AddCategoryMapping(900, NewznabStandardCategory.Other, "a.b.newznzb.whiskey");
            caps.Categories.AddCategoryMapping(901, NewznabStandardCategory.Other, "a.b.newznzb.xray");
            caps.Categories.AddCategoryMapping(902, NewznabStandardCategory.Other, "a.b.newznzb.yankee");
            caps.Categories.AddCategoryMapping(903, NewznabStandardCategory.Other, "a.b.newznzb.zulu");
            caps.Categories.AddCategoryMapping(471, NewznabStandardCategory.Other, "a.b.nfonews");
            caps.Categories.AddCategoryMapping(472, NewznabStandardCategory.Other, "a.b.nintendo.ds");
            caps.Categories.AddCategoryMapping(473, NewznabStandardCategory.Other, "a.b.nirpaia");
            caps.Categories.AddCategoryMapping(474, NewznabStandardCategory.Other, "a.b.nl");
            caps.Categories.AddCategoryMapping(475, NewznabStandardCategory.Other, "a.b.noprobs");
            caps.Categories.AddCategoryMapping(476, NewznabStandardCategory.Other, "a.b.nordic.apps");
            caps.Categories.AddCategoryMapping(477, NewznabStandardCategory.Other, "a.b.nordic.dvd");
            caps.Categories.AddCategoryMapping(478, NewznabStandardCategory.Other, "a.b.nordic.dvdr");
            caps.Categories.AddCategoryMapping(479, NewznabStandardCategory.Other, "a.b.nordic.password.protected");
            caps.Categories.AddCategoryMapping(480, NewznabStandardCategory.Other, "a.b.nordic.xvid");
            caps.Categories.AddCategoryMapping(481, NewznabStandardCategory.Other, "a.b.norge");
            caps.Categories.AddCategoryMapping(482, NewznabStandardCategory.Other, "a.b.nospam.cheerleaders");
            caps.Categories.AddCategoryMapping(483, NewznabStandardCategory.Other, "a.b.nospam.female.bodyhair");
            caps.Categories.AddCategoryMapping(484, NewznabStandardCategory.Other, "a.b.nospam.female.bodyhair.pubes");
            caps.Categories.AddCategoryMapping(485, NewznabStandardCategory.Other, "a.b.nospam.female.short-hair");
            caps.Categories.AddCategoryMapping(486, NewznabStandardCategory.Other, "a.b.nospam.multimedia.erotica");
            caps.Categories.AddCategoryMapping(487, NewznabStandardCategory.Other, "a.b.nospam.multimedia.facials");
            caps.Categories.AddCategoryMapping(488, NewznabStandardCategory.Other, "a.b.nospam.prao");
            caps.Categories.AddCategoryMapping(489, NewznabStandardCategory.Other, "a.b.novarip");
            caps.Categories.AddCategoryMapping(490, NewznabStandardCategory.Other, "a.b.nzb");
            caps.Categories.AddCategoryMapping(491, NewznabStandardCategory.Other, "a.b.nzb-nordic");
            caps.Categories.AddCategoryMapping(916, NewznabStandardCategory.Other, "a.b.nzbc");
            caps.Categories.AddCategoryMapping(492, NewznabStandardCategory.Other, "a.b.nzbpirates");
            caps.Categories.AddCategoryMapping(493, NewznabStandardCategory.Other, "a.b.nzbs4u");
            caps.Categories.AddCategoryMapping(494, NewznabStandardCategory.Other, "a.b.nzm");
            caps.Categories.AddCategoryMapping(495, NewznabStandardCategory.Other, "a.b.old.games");
            caps.Categories.AddCategoryMapping(496, NewznabStandardCategory.Other, "a.b.operaworld");
            caps.Categories.AddCategoryMapping(497, NewznabStandardCategory.Other, "a.b.opie-and-anthony");
            caps.Categories.AddCategoryMapping(917, NewznabStandardCategory.Other, "a.b.outlaws");
            caps.Categories.AddCategoryMapping(498, NewznabStandardCategory.Other, "a.b.paranormal");
            caps.Categories.AddCategoryMapping(499, NewznabStandardCategory.Other, "a.b.paxer");
            caps.Categories.AddCategoryMapping(500, NewznabStandardCategory.Other, "a.b.pcgame");
            caps.Categories.AddCategoryMapping(501, NewznabStandardCategory.Other, "a.b.picasa_benelux_team");
            caps.Categories.AddCategoryMapping(502, NewznabStandardCategory.Other, "a.b.pictures");
            caps.Categories.AddCategoryMapping(503, NewznabStandardCategory.Other, "a.b.pictures.bluebird");
            caps.Categories.AddCategoryMapping(504, NewznabStandardCategory.Other, "a.b.pictures.bluebird.reposts");
            caps.Categories.AddCategoryMapping(505, NewznabStandardCategory.Other, "a.b.pictures.cd-covers");
            caps.Categories.AddCategoryMapping(510, NewznabStandardCategory.Other, "a.b.pictures.comic-strips");
            caps.Categories.AddCategoryMapping(506, NewznabStandardCategory.Other, "a.b.pictures.comics");
            caps.Categories.AddCategoryMapping(507, NewznabStandardCategory.Other, "a.b.pictures.comics.complete");
            caps.Categories.AddCategoryMapping(508, NewznabStandardCategory.Other, "a.b.pictures.comics.dcp");
            caps.Categories.AddCategoryMapping(509, NewznabStandardCategory.Other, "a.b.pictures.comics.reposts");
            caps.Categories.AddCategoryMapping(511, NewznabStandardCategory.Other, "a.b.pictures.diva");
            caps.Categories.AddCategoryMapping(918, NewznabStandardCategory.Other, "a.b.pictures.earlmiller");
            caps.Categories.AddCategoryMapping(512, NewznabStandardCategory.XXXImageSet, "a.b.pictures.erotica");
            caps.Categories.AddCategoryMapping(513, NewznabStandardCategory.XXXImageSet, "a.b.pictures.erotica.anime");
            caps.Categories.AddCategoryMapping(514, NewznabStandardCategory.XXXImageSet, "a.b.pictures.erotica.comics");
            caps.Categories.AddCategoryMapping(515, NewznabStandardCategory.XXXImageSet, "a.b.pictures.erotica.femdom");
            caps.Categories.AddCategoryMapping(516, NewznabStandardCategory.XXXImageSet, "a.b.pictures.erotica.fetish.latex");
            caps.Categories.AddCategoryMapping(517, NewznabStandardCategory.XXXImageSet, "a.b.pictures.erotica.lactating");
            caps.Categories.AddCategoryMapping(518, NewznabStandardCategory.XXXImageSet, "a.b.pictures.erotica.scanmaster");
            caps.Categories.AddCategoryMapping(519, NewznabStandardCategory.XXXImageSet, "a.b.pictures.erotica.smoking");
            caps.Categories.AddCategoryMapping(520, NewznabStandardCategory.XXXImageSet, "a.b.pictures.erotica.spanking");
            caps.Categories.AddCategoryMapping(521, NewznabStandardCategory.XXXImageSet, "a.b.pictures.erotica.urine");
            caps.Categories.AddCategoryMapping(522, NewznabStandardCategory.Other, "a.b.pictures.manga");
            caps.Categories.AddCategoryMapping(523, NewznabStandardCategory.Other, "a.b.pictures.nude");
            caps.Categories.AddCategoryMapping(524, NewznabStandardCategory.Other, "a.b.pictures.photo-modeling");
            caps.Categories.AddCategoryMapping(525, NewznabStandardCategory.Other, "a.b.pictures.rika-nishimura");
            caps.Categories.AddCategoryMapping(526, NewznabStandardCategory.Other, "a.b.pictures.sierra");
            caps.Categories.AddCategoryMapping(527, NewznabStandardCategory.Other, "a.b.pictures.sierra.offtopic");
            caps.Categories.AddCategoryMapping(528, NewznabStandardCategory.Other, "a.b.pictures.tinygirls");
            caps.Categories.AddCategoryMapping(529, NewznabStandardCategory.Other, "a.b.pictures.utilities");
            caps.Categories.AddCategoryMapping(530, NewznabStandardCategory.Other, "a.b.pictures.vintage.magazines");
            caps.Categories.AddCategoryMapping(531, NewznabStandardCategory.Other, "a.b.pictures.wallpaper");
            caps.Categories.AddCategoryMapping(532, NewznabStandardCategory.Other, "a.b.pictures.youth-and-beauty");
            caps.Categories.AddCategoryMapping(919, NewznabStandardCategory.Other, "a.b.pl.divx");
            caps.Categories.AddCategoryMapping(533, NewznabStandardCategory.Other, "a.b.pl.multimedia");
            caps.Categories.AddCategoryMapping(534, NewznabStandardCategory.Other, "a.b.pl.multimedia.reposts");
            caps.Categories.AddCategoryMapping(535, NewznabStandardCategory.Other, "a.b.pocketpc.gps");
            caps.Categories.AddCategoryMapping(536, NewznabStandardCategory.Other, "a.b.pro-wrestling");
            caps.Categories.AddCategoryMapping(537, NewznabStandardCategory.Other, "a.b.psp");
            caps.Categories.AddCategoryMapping(538, NewznabStandardCategory.Other, "a.b.punk");
            caps.Categories.AddCategoryMapping(539, NewznabStandardCategory.Other, "a.b.putteam");
            caps.Categories.AddCategoryMapping(540, NewznabStandardCategory.Other, "a.b.pwp");
            caps.Categories.AddCategoryMapping(541, NewznabStandardCategory.Other, "a.b.rar.pw-required");
            caps.Categories.AddCategoryMapping(869, NewznabStandardCategory.Other, "a.b.ratcave");
            caps.Categories.AddCategoryMapping(542, NewznabStandardCategory.Other, "a.b.ratdvd.german");
            caps.Categories.AddCategoryMapping(543, NewznabStandardCategory.Other, "a.b.remixes.mp3");
            caps.Categories.AddCategoryMapping(544, NewznabStandardCategory.Other, "a.b.residents");
            caps.Categories.AddCategoryMapping(545, NewznabStandardCategory.Other, "a.b.rock-n-roll");
            caps.Categories.AddCategoryMapping(546, NewznabStandardCategory.Other, "a.b.roger");
            caps.Categories.AddCategoryMapping(547, NewznabStandardCategory.Other, "a.b.rusenet.org");
            caps.Categories.AddCategoryMapping(934, NewznabStandardCategory.Other, "a.b.sacd.iso");
            caps.Categories.AddCategoryMapping(548, NewznabStandardCategory.Other, "a.b.scary.exe.files");
            caps.Categories.AddCategoryMapping(945, NewznabStandardCategory.Other, "a.b.sea-monkeys");
            caps.Categories.AddCategoryMapping(549, NewznabStandardCategory.Other, "a.b.series.tv.divx.french");
            caps.Categories.AddCategoryMapping(550, NewznabStandardCategory.Other, "a.b.series.tv.divx.french.reposts");
            caps.Categories.AddCategoryMapping(551, NewznabStandardCategory.Other, "a.b.series.tv.french");
            caps.Categories.AddCategoryMapping(552, NewznabStandardCategory.Other, "a.b.shareware");
            caps.Categories.AddCategoryMapping(553, NewznabStandardCategory.Other, "a.b.sheet-music");
            caps.Categories.AddCategoryMapping(554, NewznabStandardCategory.Other, "a.b.shitsony");
            caps.Categories.AddCategoryMapping(555, NewznabStandardCategory.Other, "a.b.skewed");
            caps.Categories.AddCategoryMapping(556, NewznabStandardCategory.Other, "a.b.sleazemovies");
            caps.Categories.AddCategoryMapping(557, NewznabStandardCategory.Other, "a.b.smallville");
            caps.Categories.AddCategoryMapping(558, NewznabStandardCategory.Other, "a.b.smoking.videos");
            caps.Categories.AddCategoryMapping(559, NewznabStandardCategory.Other, "a.b.software");
            caps.Categories.AddCategoryMapping(931, NewznabStandardCategory.Other, "a.b.solar");
            caps.Categories.AddCategoryMapping(560, NewznabStandardCategory.Other, "a.b.solar-xl");
            caps.Categories.AddCategoryMapping(561, NewznabStandardCategory.Other, "a.b.sony.psp");
            caps.Categories.AddCategoryMapping(562, NewznabStandardCategory.Other, "a.b.sony.psp.movies");
            caps.Categories.AddCategoryMapping(563, NewznabStandardCategory.Other, "a.b.sound.mp3");
            caps.Categories.AddCategoryMapping(564, NewznabStandardCategory.Other, "a.b.sound.mp3.complete_cd");
            caps.Categories.AddCategoryMapping(565, NewznabStandardCategory.Other, "a.b.sound.radio.oldtime");
            caps.Categories.AddCategoryMapping(566, NewznabStandardCategory.Other, "a.b.sound.utilities");
            caps.Categories.AddCategoryMapping(567, NewznabStandardCategory.Other, "a.b.sounds");
            caps.Categories.AddCategoryMapping(568, NewznabStandardCategory.Other, "a.b.sounds.1940s.mp3");
            caps.Categories.AddCategoryMapping(569, NewznabStandardCategory.Other, "a.b.sounds.1950s.mp3");
            caps.Categories.AddCategoryMapping(570, NewznabStandardCategory.Other, "a.b.sounds.1960s.mp3");
            caps.Categories.AddCategoryMapping(571, NewznabStandardCategory.Other, "a.b.sounds.1970s.mp3");
            caps.Categories.AddCategoryMapping(572, NewznabStandardCategory.Other, "a.b.sounds.1980s.mp3");
            caps.Categories.AddCategoryMapping(573, NewznabStandardCategory.Other, "a.b.sounds.78rpm-era");
            caps.Categories.AddCategoryMapping(574, NewznabStandardCategory.Other, "a.b.sounds.aac");
            caps.Categories.AddCategoryMapping(575, NewznabStandardCategory.Other, "a.b.sounds.anime");
            caps.Categories.AddCategoryMapping(576, NewznabStandardCategory.Other, "a.b.sounds.audiobook");
            caps.Categories.AddCategoryMapping(577, NewznabStandardCategory.Other, "a.b.sounds.audiobooks");
            caps.Categories.AddCategoryMapping(578, NewznabStandardCategory.Other, "a.b.sounds.audiobooks.scifi-fantasy");
            caps.Categories.AddCategoryMapping(579, NewznabStandardCategory.Other, "a.b.sounds.country.mp3");
            caps.Categories.AddCategoryMapping(580, NewznabStandardCategory.Other, "a.b.sounds.dts");
            caps.Categories.AddCategoryMapping(581, NewznabStandardCategory.Other, "a.b.sounds.flac");
            caps.Categories.AddCategoryMapping(582, NewznabStandardCategory.Other, "a.b.sounds.flac.classical");
            caps.Categories.AddCategoryMapping(583, NewznabStandardCategory.Other, "a.b.sounds.flac.jazz");
            caps.Categories.AddCategoryMapping(584, NewznabStandardCategory.Other, "a.b.sounds.jpop");
            caps.Categories.AddCategoryMapping(585, NewznabStandardCategory.Other, "a.b.sounds.karaoke");
            caps.Categories.AddCategoryMapping(586, NewznabStandardCategory.Other, "a.b.sounds.korean");
            caps.Categories.AddCategoryMapping(587, NewznabStandardCategory.Other, "a.b.sounds.lossless");
            caps.Categories.AddCategoryMapping(588, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.1960s");
            caps.Categories.AddCategoryMapping(589, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.1970s");
            caps.Categories.AddCategoryMapping(590, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.1980s");
            caps.Categories.AddCategoryMapping(591, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.1990s");
            caps.Categories.AddCategoryMapping(592, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.2000s");
            caps.Categories.AddCategoryMapping(933, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.24bit");
            caps.Categories.AddCategoryMapping(593, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.blues");
            caps.Categories.AddCategoryMapping(594, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.classical");
            caps.Categories.AddCategoryMapping(595, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.country");
            caps.Categories.AddCategoryMapping(596, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.flac");
            caps.Categories.AddCategoryMapping(597, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.french");
            caps.Categories.AddCategoryMapping(598, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.jazz");
            caps.Categories.AddCategoryMapping(599, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.metal");
            caps.Categories.AddCategoryMapping(600, NewznabStandardCategory.AudioLossless, "a.b.sounds.lossless.rap-hiphop");
            caps.Categories.AddCategoryMapping(601, NewznabStandardCategory.Other, "a.b.sounds.midi");
            caps.Categories.AddCategoryMapping(602, NewznabStandardCategory.Other, "a.b.sounds.misc");
            caps.Categories.AddCategoryMapping(603, NewznabStandardCategory.Other, "a.b.sounds.monkeysaudio");
            caps.Categories.AddCategoryMapping(604, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3");
            caps.Categories.AddCategoryMapping(605, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.1940s");
            caps.Categories.AddCategoryMapping(606, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.1950s");
            caps.Categories.AddCategoryMapping(607, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.1960s");
            caps.Categories.AddCategoryMapping(608, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.1970s");
            caps.Categories.AddCategoryMapping(609, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.1980s");
            caps.Categories.AddCategoryMapping(610, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.1990s");
            caps.Categories.AddCategoryMapping(611, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.2000s");
            caps.Categories.AddCategoryMapping(940, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.acoustic");
            caps.Categories.AddCategoryMapping(612, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.alternative-rock");
            caps.Categories.AddCategoryMapping(613, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.ambient");
            caps.Categories.AddCategoryMapping(614, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.audiobooks");
            caps.Categories.AddCategoryMapping(874, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.big-band");
            caps.Categories.AddCategoryMapping(615, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.bluegrass");
            caps.Categories.AddCategoryMapping(616, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.blues");
            caps.Categories.AddCategoryMapping(617, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.books");
            caps.Categories.AddCategoryMapping(618, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.bootlegs");
            caps.Categories.AddCategoryMapping(619, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.brazilian");
            caps.Categories.AddCategoryMapping(620, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.celtic");
            caps.Categories.AddCategoryMapping(621, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.chinese");
            caps.Categories.AddCategoryMapping(622, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.christian");
            caps.Categories.AddCategoryMapping(623, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.christmas");
            caps.Categories.AddCategoryMapping(626, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.classic-rock");
            caps.Categories.AddCategoryMapping(624, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.classical");
            caps.Categories.AddCategoryMapping(625, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.classical-guitar");
            caps.Categories.AddCategoryMapping(627, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.comedy");
            caps.Categories.AddCategoryMapping(629, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.complete-cd");
            caps.Categories.AddCategoryMapping(628, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.complete_cd");
            caps.Categories.AddCategoryMapping(630, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.country");
            caps.Categories.AddCategoryMapping(631, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.dance");
            caps.Categories.AddCategoryMapping(632, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.dancehall");
            caps.Categories.AddCategoryMapping(932, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.disco");
            caps.Categories.AddCategoryMapping(633, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.disney");
            caps.Categories.AddCategoryMapping(634, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.easy-listening");
            caps.Categories.AddCategoryMapping(635, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.electronic");
            caps.Categories.AddCategoryMapping(636, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.emo");
            caps.Categories.AddCategoryMapping(637, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.extreme-metal");
            caps.Categories.AddCategoryMapping(638, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.folk");
            caps.Categories.AddCategoryMapping(639, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.french");
            caps.Categories.AddCategoryMapping(640, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.full_album");
            caps.Categories.AddCategoryMapping(641, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.full_albums");
            caps.Categories.AddCategoryMapping(642, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.german");
            caps.Categories.AddCategoryMapping(643, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.german.charts");
            caps.Categories.AddCategoryMapping(644, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.german.hoerbuecher");
            caps.Categories.AddCategoryMapping(645, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.german.hoerspiele");
            caps.Categories.AddCategoryMapping(646, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.german.music");
            caps.Categories.AddCategoryMapping(647, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.goa-trance");
            caps.Categories.AddCategoryMapping(648, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.gothic-industrial");
            caps.Categories.AddCategoryMapping(649, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.heavy-metal");
            caps.Categories.AddCategoryMapping(650, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.holland");
            caps.Categories.AddCategoryMapping(651, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.holland.piraat");
            caps.Categories.AddCategoryMapping(652, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.hollands");
            caps.Categories.AddCategoryMapping(653, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.house");
            caps.Categories.AddCategoryMapping(654, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.indie");
            caps.Categories.AddCategoryMapping(655, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.jazz");
            caps.Categories.AddCategoryMapping(656, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.jazz.vocals");
            caps.Categories.AddCategoryMapping(657, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.karaoke");
            caps.Categories.AddCategoryMapping(658, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.latin");
            caps.Categories.AddCategoryMapping(659, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.lounge");
            caps.Categories.AddCategoryMapping(660, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.metal.full-albums");
            caps.Categories.AddCategoryMapping(661, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.modern-composers");
            caps.Categories.AddCategoryMapping(662, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.musicals");
            caps.Categories.AddCategoryMapping(663, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.new-age");
            caps.Categories.AddCategoryMapping(664, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.norwegian");
            caps.Categories.AddCategoryMapping(665, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.nu-jazz");
            caps.Categories.AddCategoryMapping(666, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.pop");
            caps.Categories.AddCategoryMapping(667, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.prog");
            caps.Categories.AddCategoryMapping(668, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.punk");
            caps.Categories.AddCategoryMapping(669, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.rap-hiphop");
            caps.Categories.AddCategoryMapping(670, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.reggae");
            caps.Categories.AddCategoryMapping(671, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.repost");
            caps.Categories.AddCategoryMapping(672, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.rock");
            caps.Categories.AddCategoryMapping(673, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.rock.full-album");
            caps.Categories.AddCategoryMapping(674, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.rock.full-albums");
            caps.Categories.AddCategoryMapping(675, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.secular");
            caps.Categories.AddCategoryMapping(676, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.soul-rhythm-and-blues");
            caps.Categories.AddCategoryMapping(677, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.sound-effects");
            caps.Categories.AddCategoryMapping(678, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.soundtracks");
            caps.Categories.AddCategoryMapping(920, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.stoner");
            caps.Categories.AddCategoryMapping(679, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.techno");
            caps.Categories.AddCategoryMapping(680, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.video-games");
            caps.Categories.AddCategoryMapping(681, NewznabStandardCategory.AudioMP3, "a.b.sounds.mp3.world-music");
            caps.Categories.AddCategoryMapping(682, NewznabStandardCategory.Other, "a.b.sounds.music");
            caps.Categories.AddCategoryMapping(683, NewznabStandardCategory.Other, "a.b.sounds.music.classical");
            caps.Categories.AddCategoryMapping(684, NewznabStandardCategory.Other, "a.b.sounds.music.opera");
            caps.Categories.AddCategoryMapping(685, NewznabStandardCategory.Other, "a.b.sounds.music.rock.metal");
            caps.Categories.AddCategoryMapping(686, NewznabStandardCategory.Other, "a.b.sounds.nl.hoorspel");
            caps.Categories.AddCategoryMapping(687, NewznabStandardCategory.Other, "a.b.sounds.ogg");
            caps.Categories.AddCategoryMapping(688, NewznabStandardCategory.Other, "a.b.sounds.pac");
            caps.Categories.AddCategoryMapping(689, NewznabStandardCategory.Other, "a.b.sounds.radio.bbc");
            caps.Categories.AddCategoryMapping(690, NewznabStandardCategory.Other, "a.b.sounds.radio.british");
            caps.Categories.AddCategoryMapping(691, NewznabStandardCategory.Other, "a.b.sounds.radio.coasttocoast.am");
            caps.Categories.AddCategoryMapping(692, NewznabStandardCategory.Other, "a.b.sounds.radio.misc");
            caps.Categories.AddCategoryMapping(693, NewznabStandardCategory.Other, "a.b.sounds.radio.mp3");
            caps.Categories.AddCategoryMapping(694, NewznabStandardCategory.Other, "a.b.sounds.radio.oldtime");
            caps.Categories.AddCategoryMapping(695, NewznabStandardCategory.Other, "a.b.sounds.radio.oldtime.highspeed");
            caps.Categories.AddCategoryMapping(696, NewznabStandardCategory.Other, "a.b.sounds.samples");
            caps.Categories.AddCategoryMapping(697, NewznabStandardCategory.Other, "a.b.sounds.samples.music");
            caps.Categories.AddCategoryMapping(698, NewznabStandardCategory.Other, "a.b.sounds.utilities");
            caps.Categories.AddCategoryMapping(699, NewznabStandardCategory.Other, "a.b.sounds.whitburn.country");
            caps.Categories.AddCategoryMapping(700, NewznabStandardCategory.Other, "a.b.sounds.whitburn.lossless");
            caps.Categories.AddCategoryMapping(701, NewznabStandardCategory.Other, "a.b.sounds.whitburn.pop");
            caps.Categories.AddCategoryMapping(702, NewznabStandardCategory.Other, "a.b.sounds.whitburn.reposts");
            caps.Categories.AddCategoryMapping(703, NewznabStandardCategory.Other, "a.b.southern-charms");
            caps.Categories.AddCategoryMapping(704, NewznabStandardCategory.Other, "a.b.southern-charms.pictures");
            caps.Categories.AddCategoryMapping(705, NewznabStandardCategory.Other, "a.b.southpark");
            caps.Categories.AddCategoryMapping(706, NewznabStandardCategory.Other, "a.b.squaresoft");
            caps.Categories.AddCategoryMapping(707, NewznabStandardCategory.Other, "a.b.stargate-atlantis");
            caps.Categories.AddCategoryMapping(708, NewznabStandardCategory.Other, "a.b.stargate-sg1");
            caps.Categories.AddCategoryMapping(709, NewznabStandardCategory.Other, "a.b.startrek");
            caps.Categories.AddCategoryMapping(710, NewznabStandardCategory.Other, "a.b.starwars");
            caps.Categories.AddCategoryMapping(711, NewznabStandardCategory.Other, "a.b.stripboeken.nl");
            caps.Categories.AddCategoryMapping(712, NewznabStandardCategory.Other, "a.b.superman");
            caps.Categories.AddCategoryMapping(713, NewznabStandardCategory.Other, "a.b.svcd");
            caps.Categories.AddCategoryMapping(714, NewznabStandardCategory.Other, "a.b.swe");
            caps.Categories.AddCategoryMapping(715, NewznabStandardCategory.Other, "a.b.swebinz");
            caps.Categories.AddCategoryMapping(716, NewznabStandardCategory.Other, "a.b.swedish");
            caps.Categories.AddCategoryMapping(717, NewznabStandardCategory.Other, "a.b.swedvdr");
            caps.Categories.AddCategoryMapping(718, NewznabStandardCategory.Other, "a.b.tatu");
            caps.Categories.AddCategoryMapping(719, NewznabStandardCategory.Other, "a.b.team-casanova");
            caps.Categories.AddCategoryMapping(720, NewznabStandardCategory.Other, "a.b.teevee");
            caps.Categories.AddCategoryMapping(721, NewznabStandardCategory.Other, "a.b.test");
            caps.Categories.AddCategoryMapping(722, NewznabStandardCategory.Other, "a.b.the-portal");
            caps.Categories.AddCategoryMapping(723, NewznabStandardCategory.Other, "a.b.the-terminal");
            caps.Categories.AddCategoryMapping(921, NewznabStandardCategory.Other, "a.b.thundernews");
            caps.Categories.AddCategoryMapping(724, NewznabStandardCategory.Other, "a.b.tiesto");
            caps.Categories.AddCategoryMapping(725, NewznabStandardCategory.Other, "a.b.town");
            caps.Categories.AddCategoryMapping(726, NewznabStandardCategory.Other, "a.b.town.cine");
            caps.Categories.AddCategoryMapping(727, NewznabStandardCategory.Other, "a.b.town.serien");
            caps.Categories.AddCategoryMapping(728, NewznabStandardCategory.Other, "a.b.town.xxx");
            caps.Categories.AddCategoryMapping(729, NewznabStandardCategory.Other, "a.b.triballs");
            caps.Categories.AddCategoryMapping(730, NewznabStandardCategory.Other, "a.b.tun");
            caps.Categories.AddCategoryMapping(731, NewznabStandardCategory.TV, "a.b.tv");
            caps.Categories.AddCategoryMapping(732, NewznabStandardCategory.TVForeign, "a.b.tv.aus");
            caps.Categories.AddCategoryMapping(733, NewznabStandardCategory.TV, "a.b.tv.big-brother");
            caps.Categories.AddCategoryMapping(734, NewznabStandardCategory.TVForeign, "a.b.tv.canadian");
            caps.Categories.AddCategoryMapping(735, NewznabStandardCategory.TVForeign, "a.b.tv.deutsch");
            caps.Categories.AddCategoryMapping(736, NewznabStandardCategory.TVForeign, "a.b.tv.deutsch.dokumentation");
            caps.Categories.AddCategoryMapping(737, NewznabStandardCategory.TV, "a.b.tv.farscape");
            caps.Categories.AddCategoryMapping(738, NewznabStandardCategory.TV, "a.b.tv.friends");
            caps.Categories.AddCategoryMapping(739, NewznabStandardCategory.TV, "a.b.tv.simpsons");
            caps.Categories.AddCategoryMapping(740, NewznabStandardCategory.TVForeign, "a.b.tv.swedish");
            caps.Categories.AddCategoryMapping(741, NewznabStandardCategory.TV, "a.b.tv.us-sitcoms");
            caps.Categories.AddCategoryMapping(742, NewznabStandardCategory.TV, "a.b.tvseries");
            caps.Categories.AddCategoryMapping(868, NewznabStandardCategory.TV, "a.b.tvshows");
            caps.Categories.AddCategoryMapping(743, NewznabStandardCategory.Other, "a.b.u-4all");
            caps.Categories.AddCategoryMapping(744, NewznabStandardCategory.Other, "a.b.u4e");
            caps.Categories.AddCategoryMapping(745, NewznabStandardCategory.Other, "a.b.ucc");
            caps.Categories.AddCategoryMapping(746, NewznabStandardCategory.Other, "a.b.ufg");
            caps.Categories.AddCategoryMapping(747, NewznabStandardCategory.Other, "a.b.ufo");
            caps.Categories.AddCategoryMapping(748, NewznabStandardCategory.Other, "a.b.ufo.files");
            caps.Categories.AddCategoryMapping(749, NewznabStandardCategory.Other, "a.b.uhq");
            caps.Categories.AddCategoryMapping(750, NewznabStandardCategory.Other, "a.b.underground");
            caps.Categories.AddCategoryMapping(751, NewznabStandardCategory.Other, "a.b.united-forums");
            caps.Categories.AddCategoryMapping(752, NewznabStandardCategory.Other, "a.b.unity");
            caps.Categories.AddCategoryMapping(753, NewznabStandardCategory.Other, "a.b.usc");
            caps.Categories.AddCategoryMapping(754, NewznabStandardCategory.Other, "a.b.usenet");
            caps.Categories.AddCategoryMapping(922, NewznabStandardCategory.Other, "a.b.usenet-of-inferno");
            caps.Categories.AddCategoryMapping(923, NewznabStandardCategory.Other, "a.b.usenet-of-outlaws");
            caps.Categories.AddCategoryMapping(761, NewznabStandardCategory.Other, "a.b.usenet-space-cowboys");
            caps.Categories.AddCategoryMapping(762, NewznabStandardCategory.Other, "a.b.usenet-world.com");
            caps.Categories.AddCategoryMapping(755, NewznabStandardCategory.Other, "a.b.usenet2day");
            caps.Categories.AddCategoryMapping(756, NewznabStandardCategory.Other, "a.b.usenetdevils");
            caps.Categories.AddCategoryMapping(757, NewznabStandardCategory.Other, "a.b.usenetrevo.serien");
            caps.Categories.AddCategoryMapping(758, NewznabStandardCategory.Other, "a.b.usenetrevolution");
            caps.Categories.AddCategoryMapping(759, NewznabStandardCategory.Other, "a.b.usenetrevolution.musik");
            caps.Categories.AddCategoryMapping(760, NewznabStandardCategory.Other, "a.b.usenetrevolution.xxx");
            caps.Categories.AddCategoryMapping(763, NewznabStandardCategory.Other, "a.b.usewarez");
            caps.Categories.AddCategoryMapping(764, NewznabStandardCategory.Other, "a.b.uzenet");
            caps.Categories.AddCategoryMapping(765, NewznabStandardCategory.Other, "a.b.vcd");
            caps.Categories.AddCategoryMapping(766, NewznabStandardCategory.Other, "a.b.vcd.french");
            caps.Categories.AddCategoryMapping(767, NewznabStandardCategory.Other, "a.b.vcd.highspeed");
            caps.Categories.AddCategoryMapping(768, NewznabStandardCategory.Other, "a.b.vcd.other");
            caps.Categories.AddCategoryMapping(769, NewznabStandardCategory.Other, "a.b.vcd.repost");
            caps.Categories.AddCategoryMapping(770, NewznabStandardCategory.Other, "a.b.vcd.svcd");
            caps.Categories.AddCategoryMapping(771, NewznabStandardCategory.Other, "a.b.vcd.svcd.repost");
            caps.Categories.AddCategoryMapping(772, NewznabStandardCategory.Other, "a.b.vcd.westerns");
            caps.Categories.AddCategoryMapping(773, NewznabStandardCategory.Other, "a.b.vcd.xxx");
            caps.Categories.AddCategoryMapping(774, NewznabStandardCategory.Other, "a.b.vcdz");
            caps.Categories.AddCategoryMapping(775, NewznabStandardCategory.Other, "a.b.verified.photoshoots");
            caps.Categories.AddCategoryMapping(776, NewznabStandardCategory.Other, "a.b.vesdaris");
            caps.Categories.AddCategoryMapping(824, NewznabStandardCategory.Other, "a.b.w-software");
            caps.Categories.AddCategoryMapping(777, NewznabStandardCategory.Other, "a.b.wallpaper");
            caps.Categories.AddCategoryMapping(778, NewznabStandardCategory.Other, "a.b.warcraft");
            caps.Categories.AddCategoryMapping(779, NewznabStandardCategory.Other, "a.b.wares");
            caps.Categories.AddCategoryMapping(780, NewznabStandardCategory.Other, "a.b.warez");
            caps.Categories.AddCategoryMapping(813, NewznabStandardCategory.PC, "a.b.warez-pc");
            caps.Categories.AddCategoryMapping(814, NewznabStandardCategory.PC0day, "a.b.warez-pc.0-day");
            caps.Categories.AddCategoryMapping(781, NewznabStandardCategory.PC0day, "a.b.warez.0-day");
            caps.Categories.AddCategoryMapping(782, NewznabStandardCategory.PC0day, "a.b.warez.0-day.games");
            caps.Categories.AddCategoryMapping(783, NewznabStandardCategory.PC, "a.b.warez.autocad");
            caps.Categories.AddCategoryMapping(784, NewznabStandardCategory.PC, "a.b.warez.educational");
            caps.Categories.AddCategoryMapping(785, NewznabStandardCategory.PC, "a.b.warez.flightsim");
            caps.Categories.AddCategoryMapping(786, NewznabStandardCategory.PC, "a.b.warez.games");
            caps.Categories.AddCategoryMapping(788, NewznabStandardCategory.PC, "a.b.warez.ibm-pc");
            caps.Categories.AddCategoryMapping(789, NewznabStandardCategory.PC, "a.b.warez.ibm-pc.0-day");
            caps.Categories.AddCategoryMapping(790, NewznabStandardCategory.PC, "a.b.warez.ibm-pc.games");
            caps.Categories.AddCategoryMapping(791, NewznabStandardCategory.PC, "a.b.warez.ibm-pc.german");
            caps.Categories.AddCategoryMapping(792, NewznabStandardCategory.PC, "a.b.warez.ibm-pc.ms-beta");
            caps.Categories.AddCategoryMapping(793, NewznabStandardCategory.PC, "a.b.warez.ibm-pc.o-day");
            caps.Categories.AddCategoryMapping(794, NewznabStandardCategory.PC, "a.b.warez.ibm-pc.os");
            caps.Categories.AddCategoryMapping(787, NewznabStandardCategory.PC, "a.b.warez.ibm.pc");
            caps.Categories.AddCategoryMapping(795, NewznabStandardCategory.PC, "a.b.warez.linux");
            caps.Categories.AddCategoryMapping(796, NewznabStandardCategory.PC, "a.b.warez.palmpilot");
            caps.Categories.AddCategoryMapping(797, NewznabStandardCategory.PC, "a.b.warez.pocketpc");
            caps.Categories.AddCategoryMapping(798, NewznabStandardCategory.PC, "a.b.warez.pocketpc.gps");
            caps.Categories.AddCategoryMapping(799, NewznabStandardCategory.PC, "a.b.warez.pocketpc.movies");
            caps.Categories.AddCategoryMapping(800, NewznabStandardCategory.PC, "a.b.warez.quebec-hackers");
            caps.Categories.AddCategoryMapping(801, NewznabStandardCategory.PC, "a.b.warez.quebec-hackers.d");
            caps.Categories.AddCategoryMapping(802, NewznabStandardCategory.PC, "a.b.warez.quebec-hackers.dvd");
            caps.Categories.AddCategoryMapping(803, NewznabStandardCategory.PC, "a.b.warez.raptorweb");
            caps.Categories.AddCategoryMapping(804, NewznabStandardCategory.PC, "a.b.warez.smartphone");
            caps.Categories.AddCategoryMapping(805, NewznabStandardCategory.PC, "a.b.warez.uk");
            caps.Categories.AddCategoryMapping(806, NewznabStandardCategory.PC, "a.b.warez.uk.mp3");
            caps.Categories.AddCategoryMapping(807, NewznabStandardCategory.PC, "a.b.warez.win2000");
            caps.Categories.AddCategoryMapping(808, NewznabStandardCategory.PC, "a.b.warez.win95-apps");
            caps.Categories.AddCategoryMapping(809, NewznabStandardCategory.PC, "a.b.warez.win95-games");
            caps.Categories.AddCategoryMapping(810, NewznabStandardCategory.Other, "a.b.warez4kiddies");
            caps.Categories.AddCategoryMapping(811, NewznabStandardCategory.Other, "a.b.warez4kiddies.apps");
            caps.Categories.AddCategoryMapping(812, NewznabStandardCategory.Other, "a.b.warez4kiddies.mp3");
            caps.Categories.AddCategoryMapping(815, NewznabStandardCategory.Other, "a.b.wb");
            caps.Categories.AddCategoryMapping(924, NewznabStandardCategory.Other, "a.b.webcam");
            caps.Categories.AddCategoryMapping(925, NewznabStandardCategory.Other, "a.b.webcam.videos");
            caps.Categories.AddCategoryMapping(816, NewznabStandardCategory.Other, "a.b.welovehelix");
            caps.Categories.AddCategoryMapping(817, NewznabStandardCategory.Other, "a.b.welovelori");
            caps.Categories.AddCategoryMapping(818, NewznabStandardCategory.Other, "a.b.whitburn");
            caps.Categories.AddCategoryMapping(863, NewznabStandardCategory.Other, "a.b.windows");
            caps.Categories.AddCategoryMapping(950, NewznabStandardCategory.Other, "a.b.wiseguys");
            caps.Categories.AddCategoryMapping(819, NewznabStandardCategory.Other, "a.b.witchblade");
            caps.Categories.AddCategoryMapping(821, NewznabStandardCategory.Other, "a.b.wmv-hd");
            caps.Categories.AddCategoryMapping(820, NewznabStandardCategory.Other, "a.b.wmvhd");
            caps.Categories.AddCategoryMapping(926, NewznabStandardCategory.Other, "a.b.wolfsteamers.info");
            caps.Categories.AddCategoryMapping(942, NewznabStandardCategory.Other, "a.b.wood");
            caps.Categories.AddCategoryMapping(822, NewznabStandardCategory.Other, "a.b.world-languages");
            caps.Categories.AddCategoryMapping(823, NewznabStandardCategory.Other, "a.b.worms");
            caps.Categories.AddCategoryMapping(825, NewznabStandardCategory.Other, "a.b.ww2mwa");
            caps.Categories.AddCategoryMapping(826, NewznabStandardCategory.Other, "a.b.x");
            caps.Categories.AddCategoryMapping(831, NewznabStandardCategory.Other, "a.b.x-files");
            caps.Categories.AddCategoryMapping(827, NewznabStandardCategory.Other, "a.b.x264");
            caps.Categories.AddCategoryMapping(828, NewznabStandardCategory.Other, "a.b.x2l");
            caps.Categories.AddCategoryMapping(829, NewznabStandardCategory.Other, "a.b.x2l.nzb");
            caps.Categories.AddCategoryMapping(830, NewznabStandardCategory.Other, "a.b.xbox");
            caps.Categories.AddCategoryMapping(832, NewznabStandardCategory.Other, "a.b.xvid");
            caps.Categories.AddCategoryMapping(833, NewznabStandardCategory.Other, "a.b.xvid.movies");
            caps.Categories.AddCategoryMapping(834, NewznabStandardCategory.Other, "a.b.xxibite");
            caps.Categories.AddCategoryMapping(835, NewznabStandardCategory.Other, "a.b.xylo");
            caps.Categories.AddCategoryMapping(836, NewznabStandardCategory.Other, "a.b.zines");
            caps.Categories.AddCategoryMapping(837, NewznabStandardCategory.Other, "alt.chello.binaries");
            caps.Categories.AddCategoryMapping(838, NewznabStandardCategory.Other, "alt.dvdnordic.org");
            caps.Categories.AddCategoryMapping(839, NewznabStandardCategory.Other, "alt.games.microsoft.flight-sim");
            caps.Categories.AddCategoryMapping(840, NewznabStandardCategory.Other, "alt.games.video.xbox");
            caps.Categories.AddCategoryMapping(841, NewznabStandardCategory.Other, "alt.games.warcraft");
            caps.Categories.AddCategoryMapping(842, NewznabStandardCategory.Other, "alt.nl.ftp.binaries");
            caps.Categories.AddCategoryMapping(843, NewznabStandardCategory.Other, "alt.no-advertising.files.audio.mp3.techno");
            caps.Categories.AddCategoryMapping(844, NewznabStandardCategory.Other, "alt.sex.erotica");
            caps.Categories.AddCategoryMapping(865, NewznabStandardCategory.Other, "alt.windows7.general");
            caps.Categories.AddCategoryMapping(845, NewznabStandardCategory.Other, "dk.binaer.film");
            caps.Categories.AddCategoryMapping(846, NewznabStandardCategory.Other, "dk.binaer.film.divx");
            caps.Categories.AddCategoryMapping(847, NewznabStandardCategory.Other, "dk.binaer.musik");
            caps.Categories.AddCategoryMapping(848, NewznabStandardCategory.Other, "dk.binaer.tv");
            caps.Categories.AddCategoryMapping(849, NewznabStandardCategory.Other, "dk.binaries.film");
            caps.Categories.AddCategoryMapping(927, NewznabStandardCategory.Other, "es.binaries.bd");
            caps.Categories.AddCategoryMapping(850, NewznabStandardCategory.Other, "es.binarios.hd");
            caps.Categories.AddCategoryMapping(851, NewznabStandardCategory.Other, "es.binarios.misc");
            caps.Categories.AddCategoryMapping(852, NewznabStandardCategory.Other, "es.binarios.sexo");
            caps.Categories.AddCategoryMapping(853, NewznabStandardCategory.Other, "esp.binarios.misc");
            caps.Categories.AddCategoryMapping(854, NewznabStandardCategory.Other, "esp.binarios.series.misc");
            caps.Categories.AddCategoryMapping(855, NewznabStandardCategory.Other, "korea.binaries.movies");
            caps.Categories.AddCategoryMapping(856, NewznabStandardCategory.Other, "korea.binaries.tv");
            caps.Categories.AddCategoryMapping(857, NewznabStandardCategory.Other, "korea.binaries.warez");
            caps.Categories.AddCategoryMapping(858, NewznabStandardCategory.Other, "nl.media.dvd");
            caps.Categories.AddCategoryMapping(859, NewznabStandardCategory.Other, "planet.binaries.games");
            caps.Categories.AddCategoryMapping(860, NewznabStandardCategory.Other, "planet.binaries.movies");
            caps.Categories.AddCategoryMapping(861, NewznabStandardCategory.Other, "planet.binaries.sounds");
            caps.Categories.AddCategoryMapping(862, NewznabStandardCategory.Other, "uk.games.video.xbox");

            return caps;
        }
    }

    public class NzbIndexRequestGenerator : IIndexerRequestGenerator
    {
        private readonly NzbIndexSettings _settings;
        private readonly IndexerCapabilities _capabilities;

        public NzbIndexRequestGenerator(NzbIndexSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedTvSearchString}", searchCriteria.Categories, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories, searchCriteria.Limit ?? 100, searchCriteria.Offset ?? 0));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, int limit, int offset)
        {
            var queryCollection = new List<KeyValuePair<string, string>>
            {
                { "key", _settings.ApiKey },
                { "max", limit.ToString() },
                { "q", term },
                { "p", (offset / limit).ToString() }
            };

            if (categories != null)
            {
                foreach (var cat in _capabilities.Categories.MapTorznabCapsToTrackers(categories))
                {
                    queryCollection.Add("g[]", $"{cat}");
                }
            }

            var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/v3/search/?{queryCollection.GetQueryString()}";

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

            yield return request;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NzbIndexParser : IParseIndexerResponse
    {
        private readonly NzbIndexSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        private static readonly Regex ParseTitleRegex = new (@"\""(?<title>[^:\/]*?)(?:\.(rar|nfo|mkv|par2|001|nzb|url|zip|r[0-9]{2}))?\""", RegexOptions.Compiled);

        public NzbIndexParser(NzbIndexSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, "Unexpected response status {0} code from indexer request", indexerResponse.HttpResponse.StatusCode);
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var releaseInfos = new List<ReleaseInfo>();

            // TODO Deserialize to TorrentSyndikatResponse Type
            var jsonContent = JObject.Parse(indexerResponse.Content);

            foreach (var row in jsonContent.Value<JArray>("results"))
            {
                var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

                var id = row.Value<string>("id");
                var details = _settings.BaseUrl + "collection/" + id;

                var parsedTitle = ParseTitleRegex.Match(row.Value<string>("name"));

                if (!parsedTitle.Success || parsedTitle.Groups["title"].Value.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var release = new ReleaseInfo
                {
                    Guid = details,
                    InfoUrl = details,
                    DownloadUrl = _settings.BaseUrl + "download/" + id,
                    Title = parsedTitle.Groups["title"].Value,
                    Categories = row.Value<JArray>("group_ids").SelectMany(g => _categories.MapTrackerCatToNewznab(g.Value<string>())).Distinct().ToList(),
                    PublishDate = dateTime.AddMilliseconds(row.Value<long>("posted")).ToLocalTime(),
                    Size = row.Value<long>("size"),
                    Files = row.Value<int>("file_count")
                };

                releaseInfos.Add(release);
            }

            return releaseInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NzbIndexSettingsValidator : AbstractValidator<NzbIndexSettings>
    {
        public NzbIndexSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
        }
    }

    public class NzbIndexSettings : IIndexerSettings
    {
        private static readonly NzbIndexSettingsValidator Validator = new ();

        public NzbIndexSettings()
        {
            ApiKey = "";
        }

        [FieldDefinition(1, Label = "IndexerSettingsBaseUrl", HelpText = "IndexerSettingsBaseUrlHelpText", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "ApiKey", HelpText = "IndexerNzbIndexSettingsApiKeyHelpText", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(3)]
        public IndexerBaseSettings BaseSettings { get; set; } = new ();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
