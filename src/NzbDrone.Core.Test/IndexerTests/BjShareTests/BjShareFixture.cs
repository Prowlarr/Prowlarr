using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Test.IndexerTests.BjShareTests
{
    [TestFixture]
    public class BjShareFixture
    {
        private static IndexerResponse CreateResponse(string content)
        {
            var httpRequest = new HttpRequest("https://bj-share.info/torrents.php?searchstr=test&action=basic&searchsubmit=1");
            var httpResponse = new HttpResponse(httpRequest, new HttpHeader(), new CookieCollection(), Encoding.UTF8.GetBytes(content));

            return new IndexerResponse(new IndexerRequest(httpRequest), httpResponse);
        }

        [Test]
        public void should_parse_individual_torrent_row_from_search_results()
        {
            const string html = @"
                <table class=""torrent_table cats grouping"" id=""torrent_table"">
                    <tr class=""torrent"">
                        <td></td>
                        <td class=""center cats_col""><a href='/torrents.php?filter_cat%5b2%5d=2'><img alt='Seriados' title='Seriados' /></a></td>
                        <td class=""big_info"">
                            <div class=""group_info clear"">
                                <span style=""padding-left: 0px"">
                                    <span class=""add_bookmark float_right""><a href=""#"" class=""tooltip bookmarklink_torrent_111111"" title=""Adicionar aos Favoritos""><i class=""fad fa-bookmark""></i></a></span>
                                    <span class=""download_torrent float_right""><a href=""torrents.php?action=download&amp;id=222222&amp;source=browse"" class=""tooltip"" title=""Baixar""><i class=""fad fa-download""></i></a>&nbsp;&nbsp;</span>
                                </span>
                                <a href=""series.php?id=3333"">Cidade Invisivel [Invisible City]</a> - <a href=""torrents.php?id=111111&amp;torrentid=222222"" class=""tooltip"" title=""View torrent group"" dir=""ltr""></a> [2021]
                                <div class=""torrent_info"" data-imdbid="""" data-audiotype=""Legendado"" data-videocodec=""x264"" data-audiocodec=""AC3"" data-language=""Portugues"" data-format=""MKV"" data-resolution=""HD"" data-name=""Cidade Invisivel [Invisible City] -  [2021]"" data-localizedname="""" data-year=""2021"">[MKV / x264 / HDTV / HD / Legendado / <strong class=""torrent_label bjtooltip free"" title=""Free"">Free</strong>]</div>
                                <div class=""tags""></div>
                            </div>
                        </td>
                        <td><a href=""/user.php?username=""></a></td>
                        <td class=""number_column nobr""><span class=""time bjtooltip"" title=""May 02 2021, 20:22"">5 anos atras</span></td>
                        <td class=""number_column nobr"">92.05 GiB</td>
                        <td class=""number_column"">121</td>
                        <td class=""number_column"">6</td>
                        <td class=""number_column"">2</td>
                    </tr>
                </table>";

            var parser = new BjShareParser(new IndexerCapabilitiesCategories());

            var release = parser.ParseResponse(CreateResponse(html)).Single() as TorrentInfo;

            release.Title.Should().Be("Invisible City 2021 MKV / x264 / HDTV / 720p / Legendado");
            release.DownloadUrl.Should().Be("https://bj-share.info/torrents.php?action=download&id=222222&source=browse");
            release.InfoUrl.Should().Be("https://bj-share.info/torrents.php?id=111111&torrentid=222222");
            release.PublishDate.Should().Be(DateTime.Parse("May 02 2021, 20:22", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            release.Size.Should().Be(98837938176);
            release.Grabs.Should().Be(121);
            release.Seeders.Should().Be(6);
            release.Peers.Should().Be(8);
        }

        [Test]
        public void should_parse_full_grouped_tv_search_results_table()
        {
            const string html = @"
                <table class=""torrent_table cats grouping"" id=""torrent_table"">
                    <tr class=""colhead"">
                        <td class=""small""></td>
                        <td class=""small cats_col""></td>
                        <td onclick=""window.location='torrents.php?way=desc&order=name&searchstr=Journey+Beyond+S03E07&amp;tags_type=0&amp;action=basic&amp;searchsubmit=1'"" style=""cursor: pointer"">Nome</td>
                        <td>Uploader</td>
                        <td onclick=""window.location='torrents.php?way=asc&order=time&searchstr=Journey+Beyond+S03E07&amp;tags_type=0&amp;action=basic&amp;searchsubmit=1'"" style=""cursor: pointer"">Lancado ha</td>
                        <td onclick=""window.location='torrents.php?way=desc&order=size&searchstr=Journey+Beyond+S03E07&amp;tags_type=0&amp;action=basic&amp;searchsubmit=1'"" style=""cursor: pointer"">Tamanho</td>
                        <td class=""center""><i class=""fas fa-redo""></i></td>
                        <td class=""center""><i class=""fas fa-arrow-up""></i></td>
                        <td class=""center""><i class=""fas fa-arrow-down""></i></td>
                    </tr>
                    <tr class=""group"">
                        <td class=""center"">
                            <div id=""showimg_765432"" class=""hide_torrents"">
                                <a href=""#"" class=""tooltip show_torrents_link"" onclick=""toggle_group(765432, this, event)"" title=""Collapse this group.""></a>
                            </div>
                        </td>
                        <td class=""center cats_col""><a href='/torrents.php?filter_cat%5b2%5d=2'><img width='50' height='50' src='static/common/newcaticons3/seriadob4.svg' alt='Seriados' title='Seriados' class='brackets tooltip' /></a></td>
                        <td class=""big_info"">
                            <div class=""group_info clear"">
                                <a href=""series.php?id=4444"">Viagem Alem do Tempo [Journey Beyond]</a> - <a href=""torrents.php?id=765432"" class=""tooltip"" title=""View torrent group"" dir=""ltr"">S03E07</a> [2027]
                                <span class=""add_bookmark float_right""><a href=""#"" class=""tooltip bookmarklink_torrent_765432"" title=""Adicionar aos Favoritos""><i class=""fad fa-bookmark""></i></a></span>
                                <br/>
                                <div class=""tags""></div>
                            </div>
                        </td>
                        <td>&nbsp;</td>
                        <td class=""number_column nobr upload_time""><span class=""time bjtooltip"" title=""Mar 26 2027, 22:06"">1 semana atras</span></td>
                        <td class=""number_column nobr"">10.45 GiB (Max)</td>
                        <td class=""number_column"">394</td>
                        <td class=""number_column"">91</td>
                        <td class=""number_column"">0</td>
                    </tr>
                    <tr class=""group_torrent groupid_765432 edition_0"">
                        <td colspan=""3"">
                            <span><a href=""torrents.php?action=download&amp;id=888001&amp;source=browse"" class=""tooltip"" title=""Baixar""><i class=""fad fa-download""></i></a></span>
                            &nbsp;-&gt;&nbsp; <a href=""torrents.php?id=765432&amp;torrentid=888001"">[MKV / H.264 / WEB-DL / Full HD / Dolby Atmos / Dual Audio / <strong class=""torrentinfo_release"">StreamBox</strong> / <strong style=""color:red"">WANDER</strong> / <strong class=""torrent_label bjtooltip free"" title=""Free"">Free</strong>]</a>
                        </td>
                        <td><a href=""/user.php?username=""></a><br /><br /><span style=""color:red;font-weight:bold;float:none""><a href=""torrents.php?action=team&TeamID=55"" style=""color:red;font-weight:bold;float:none""><span style=""color:red;font-weight:bold;float:none"">WANDER</span></a></span></td>
                        <td class=""nobr""><span class=""time bjtooltip"" title=""Mar 26 2027, 22:02"">1 semana atras</span></td>
                        <td class=""number_column nobr"">4.58 GiB</td>
                        <td class=""number_column"">286</td>
                        <td class=""number_column"">74</td>
                        <td class=""number_column"">5</td>
                    </tr>
                    <tr class=""group_torrent groupid_765432 edition_0"">
                        <td colspan=""3"">
                            <span><a href=""torrents.php?action=download&amp;id=888002&amp;source=browse"" class=""tooltip"" title=""Baixar""><i class=""fad fa-download""></i></a></span>
                            &nbsp;-&gt;&nbsp; <a href=""torrents.php?id=765432&amp;torrentid=888002"">[MKV / H.265 / WEB-DL / 4K / Dolby Atmos / 10-bit / Dolby Vision / HDR10+ / Dual Audio / <strong class=""torrentinfo_release"">StreamBox</strong> / <strong class=""torrent_label bjtooltip free"" title=""Free"">Free</strong>]</a>
                        </td>
                        <td><a href=""/user.php?username=""></a></td>
                        <td class=""nobr""><span class=""time bjtooltip"" title=""Mar 26 2027, 22:06"">1 semana atras</span></td>
                        <td class=""number_column nobr"">10.45 GiB</td>
                        <td class=""number_column"">108</td>
                        <td class=""number_column"">17</td>
                        <td class=""number_column"">0</td>
                    </tr>
                </table>";

            var parser = new BjShareParser(new IndexerCapabilitiesCategories());

            var releases = parser.ParseResponse(CreateResponse(html)).Cast<TorrentInfo>().ToList();

            releases.Should().HaveCount(2);
            releases[0].Title.Should().Be("Journey Beyond 2027 S03E07 MKV / H.264 / WEB-DL / 1080p / Dolby Atmos / Dual Audio / StreamBox / WANDER");
            releases[0].DownloadUrl.Should().Be("https://bj-share.info/torrents.php?action=download&id=888001&source=browse");
            releases[0].InfoUrl.Should().Be("https://bj-share.info/torrents.php?id=765432&torrentid=888001");
            releases[0].PublishDate.Should().Be(DateTime.Parse("Mar 26 2027, 22:02", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            releases[0].Size.Should().Be(4917737472);
            releases[0].Seeders.Should().Be(74);
            releases[0].Peers.Should().Be(79);

            releases[1].Title.Should().Be("Journey Beyond 2027 S03E07 MKV / H.265 / WEB-DL / 2160p / Dolby Atmos / 10-bit / Dolby Vision / HDR10+ / Dual Audio / StreamBox");
            releases[1].DownloadUrl.Should().Be("https://bj-share.info/torrents.php?action=download&id=888002&source=browse");
            releases[1].Size.Should().Be(11220601856);
            releases[1].Seeders.Should().Be(17);
            releases[1].Peers.Should().Be(17);
        }

        [Test]
        public void should_parse_full_grouped_movie_search_results_table_with_year_outside_anchor()
        {
            const string html = @"
                <table class=""torrent_table cats grouping"" id=""torrent_table"">
                    <tr class=""colhead"">
                        <td class=""small""></td>
                        <td class=""small cats_col""></td>
                        <td>Nome</td>
                        <td>Uploader</td>
                        <td>Lancado ha</td>
                        <td>Tamanho</td>
                        <td class=""center""><i class=""fas fa-redo""></i></td>
                        <td class=""center""><i class=""fas fa-arrow-up""></i></td>
                        <td class=""center""><i class=""fas fa-arrow-down""></i></td>
                    </tr>
                    <tr class=""group"">
                        <td class=""center"">
                            <div id=""showimg_654321"" class=""hide_torrents"">
                                <a href=""#"" class=""tooltip show_torrents_link"" onclick=""toggle_group(654321, this, event)"" title=""Collapse this group.""></a>
                            </div>
                        </td>
                        <td class=""center cats_col""><a href='/torrents.php?filter_cat%5b1%5d=1'><img width='50' height='50' src='static/common/newcaticons3/filmes4.svg' alt='Filmes' title='Filmes' class='brackets tooltip' /></a></td>
                        <td class=""big_info"">
                            <div class=""group_info clear"">
                                <a href=""torrents.php?id=654321"" class=""tooltip"" title=""View torrent group"" dir=""ltr"">A Lua de Papel [Paper Moonlight]</a> [1989]
                                <span class=""add_bookmark float_right""><a href=""#"" class=""tooltip bookmarklink_torrent_654321"" title=""Adicionar aos Favoritos""><i class=""fad fa-bookmark""></i></a></span>
                                <br/>
                                <div class=""tags""></div>
                            </div>
                        </td>
                        <td>&nbsp;</td>
                        <td class=""number_column nobr upload_time""><span class=""time bjtooltip"" title=""Dec 07 2019, 15:46"">6 anos atras</span></td>
                        <td class=""number_column nobr"">12.90 GiB (Max)</td>
                        <td class=""number_column"">40</td>
                        <td class=""number_column"">0</td>
                        <td class=""number_column"">1</td>
                    </tr>
                    <tr class=""group_torrent groupid_654321 edition_0"">
                        <td colspan=""3"">
                            <span><a href=""torrents.php?action=download&amp;id=240001&amp;source=browse"" class=""tooltip"" title=""Baixar""><i class=""fad fa-download""></i></a></span>
                            &nbsp;-&gt;&nbsp; <a href=""torrents.php?id=654321&amp;torrentid=240001"">[MKV / H.264 / Blu-ray / Full HD / Legendado / <strong class=""torrent_label bjtooltip free"" title=""Free"">Free</strong>]</a>
                        </td>
                        <td><a href=""/user.php?username=""></a></td>
                        <td class=""nobr""><span class=""time bjtooltip"" title=""Dec 07 2019, 15:46"">6 anos atras</span></td>
                        <td class=""number_column nobr"">12.90 GiB</td>
                        <td class=""number_column"">40</td>
                        <td class=""number_column"">0</td>
                        <td class=""number_column"">1</td>
                    </tr>
                </table>";

            var parser = new BjShareParser(new IndexerCapabilitiesCategories());

            var release = parser.ParseResponse(CreateResponse(html)).Single() as TorrentInfo;

            release.Title.Should().Be("Paper Moonlight 1989 MKV / H.264 / Blu-ray / 1080p / Legendado");
            release.DownloadUrl.Should().Be("https://bj-share.info/torrents.php?action=download&id=240001&source=browse");
            release.InfoUrl.Should().Be("https://bj-share.info/torrents.php?id=654321&torrentid=240001");
            release.PublishDate.Should().Be(DateTime.Parse("Dec 07 2019, 15:46", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            release.Size.Should().Be(13851269120);
            release.Grabs.Should().Be(40);
            release.Seeders.Should().Be(0);
            release.Peers.Should().Be(1);
        }
    }
}
