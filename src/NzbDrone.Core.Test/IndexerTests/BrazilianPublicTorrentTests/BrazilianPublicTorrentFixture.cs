using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Test.IndexerTests.BrazilianPublicTorrentTests
{
    [TestFixture]
    public class BrazilianPublicTorrentFixture : CoreTest
    {
        [Test]
        public void request_generator_uses_site_routes_instead_of_internal_placeholder_slug()
        {
            var subject = Mocker.Resolve<HDRTorrent>();
            subject.Definition = Definition("HDRTorrent", "https://hdrtorrents.net/");

            var request = subject.GetRequestGenerator()
                .GetSearchRequests(new MovieSearchCriteria { SearchTerm = "Avatar Fogo e Cinzas", InteractiveSearch = true })
                .GetAllTiers()
                .Single()
                .Single();

            request.Url.FullUri.Should().StartWith("https://hdrtorrents.net/filmes/avatar_fogo_e_cinzas/");
            request.Url.FullUri.Should().NotContain("__prowlarr");
        }

        [Test]
        public async Task apachetorrent_uses_homepage_token_cookie_and_referer_for_search()
        {
            var subject = Mocker.Resolve<ApacheTorrent>();
            subject.Definition = Definition("ApacheTorrent", "https://apachetorrents.com/");
            var requestedUrls = new List<string>();
            var searchRequest = default(HttpRequest);

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), subject.Definition))
                .ReturnsAsync((HttpRequest request, IndexerDefinition _) =>
                {
                    requestedUrls.Add(request.Url.FullUri);

                    if (request.Url.FullUri == "https://apachetorrents.com/")
                    {
                        return Response(request, "<html><input type=\"hidden\" name=\"token\" value=\"abc123\"></html>", Cookies(request.Url.FullUri, "PHPSESSID", "session-1"));
                    }

                    if (request.Url.FullUri.Contains("index.php?hp_bot_check=&token=abc123&busca=Avatar"))
                    {
                        searchRequest = request;
                        return Response(request, SearchCard("https://apachetorrents.com/avatar/", "Avatar"));
                    }

                    if (request.Url.FullUri == "https://apachetorrents.com/avatar/")
                    {
                        return Response(request, Details("Avatar", "Filme", "10.5 GB", "2022", Magnet("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Avatar.2022.1080p.Dual"), "1080p / 10.5 GB"));
                    }

                    throw new AssertionException("Unexpected request " + request.Url.FullUri);
                });

            var releases = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar", Categories = [2000], InteractiveSearch = true })).Releases;

            requestedUrls.Should().Contain("https://apachetorrents.com/");
            requestedUrls.Should().Contain(u => u.Contains("index.php?hp_bot_check=&token=abc123&busca=Avatar"));
            searchRequest.Cookies.Should().ContainKey("PHPSESSID");
            searchRequest.Headers.GetSingleValue("Referer").Should().Be("https://apachetorrents.com/");
            releases.Should().HaveCount(1);

            var release = releases.Single().Should().BeOfType<TorrentInfo>().Subject;
            release.Title.Should().Be("Avatar 2022 1080p Dual");
            release.Categories.Should().Contain(NewznabStandardCategory.Movies);
            release.Size.Should().Be(11274289152);
            release.MagnetUrl.Should().StartWith("magnet:?xt=urn:btih:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        }

        [Test]
        public async Task redetorrent_extracts_detail_url_from_ancestor_anchor()
        {
            var subject = Mocker.Resolve<RedeTorrent>();
            subject.Definition = Definition("RedeTorrent", "https://redestorrents.com/");

            SetupTokenSite(
                subject.Definition,
                "https://redestorrents.com/",
                "rede-token",
                "Matrix",
                "<a href=\"https://redestorrents.com/matrix/\"><article class=\"custom-card\"><h2 itemprop=\"headline\">Matrix</h2></article></a>",
                "https://redestorrents.com/matrix/",
                Details("Matrix", "Filme", "3.93 GB", "1999", Magnet("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "Matrix.1999.720p.Dual"), "720p / 3.93 GB"));

            var releases = (await subject.Fetch(new BasicSearchCriteria { SearchTerm = "Matrix", Categories = [2000], InteractiveSearch = true })).Releases;

            releases.Should().HaveCount(1);
            releases.Single().InfoUrl.Should().Be("https://redestorrents.com/matrix/");
        }

        [Test]
        public async Task hdrtorrent_movie_search_uses_filmes_route()
        {
            var subject = Mocker.Resolve<HDRTorrent>();
            subject.Definition = Definition("HDRTorrent", "https://hdrtorrents.net/");
            var requestedUrls = new List<string>();

            SetupHdr(subject.Definition, requestedUrls, request =>
            {
                if (request.Url.FullUri == "https://hdrtorrents.net/filmes/avatar_fogo_e_cinzas/")
                {
                    return Response(request, HdrCard("Avatar Fogo e Cinzas", "https://hdrtorrents.net/avatar-fogo-e-cinzas/"));
                }

                if (request.Url.FullUri == "https://hdrtorrents.net/avatar-fogo-e-cinzas/")
                {
                    return Response(request, HdrDetails("Avatar Fogo e Cinzas", "Filme", "7.1 GB", Magnet("cccccccccccccccccccccccccccccccccccccccc", "Avatar.Fogo.e.Cinzas.2025.2160p.Dual"), "2160p / 7.1 GB"));
                }

                throw new AssertionException("Unexpected request " + request.Url.FullUri);
            });

            var releases = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar Fogo e Cinzas", InteractiveSearch = true })).Releases;

            requestedUrls.Should().Contain("https://hdrtorrents.net/filmes/avatar_fogo_e_cinzas/");
            requestedUrls.Should().NotContain("https://hdrtorrents.net/series/avatar_fogo_e_cinzas/");
            releases.Should().HaveCount(1);
            releases.Single().Categories.Should().Contain(NewznabStandardCategory.Movies);
        }

        [Test]
        public async Task hdrtorrent_tv_search_uses_series_route_and_filters_requested_season()
        {
            var subject = Mocker.Resolve<HDRTorrent>();
            subject.Definition = Definition("HDRTorrent", "https://hdrtorrents.net/");
            var requestedUrls = new List<string>();

            SetupHdr(subject.Definition, requestedUrls, request =>
            {
                if (request.Url.FullUri == "https://hdrtorrents.net/series/avatar_o_ultimo_mestre_do_ar/")
                {
                    return Response(request,
                        HdrCard("Avatar O Ultimo Mestre do Ar - 1ª Temporada", "https://hdrtorrents.net/avatar-s01/") +
                        HdrCard("Avatar O Ultimo Mestre do Ar - 2ª Temporada", "https://hdrtorrents.net/avatar-s02/"));
                }

                if (request.Url.FullUri == "https://hdrtorrents.net/avatar-s01/")
                {
                    return Response(request, HdrDetails("Avatar O Ultimo Mestre do Ar - 1ª Temporada", "Serie", "4.0 GB", Magnet("dddddddddddddddddddddddddddddddddddddddd", "Avatar.O.Ultimo.Mestre.Do.Ar.S01.1080p"), "1080p / 4.0 GB"));
                }

                if (request.Url.FullUri == "https://hdrtorrents.net/avatar-s02/")
                {
                    return Response(request, HdrDetails("Avatar O Ultimo Mestre do Ar - 2ª Temporada", "Serie", "5.0 GB", Magnet("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee", "Avatar.O.Ultimo.Mestre.Do.Ar.2a.Temporada.1080p"), "1080p / 5.0 GB"));
                }

                throw new AssertionException("Unexpected request " + request.Url.FullUri);
            });

            var releases = (await subject.Fetch(new TvSearchCriteria { SearchTerm = "Avatar O Ultimo Mestre do Ar", Season = 2, Categories = [5000], InteractiveSearch = true })).Releases;

            requestedUrls.Should().Contain("https://hdrtorrents.net/series/avatar_o_ultimo_mestre_do_ar/");
            releases.Should().HaveCount(1);
            var release = releases.Single();
            release.Title.Should().Contain("S02");
            release.Title.Should().NotContain("S01");
            release.Categories.Should().Contain(NewznabStandardCategory.TV);
        }

        [Test]
        public async Task hdrtorrent_basic_search_queries_filmes_and_series_and_dedupes_by_infohash()
        {
            var subject = Mocker.Resolve<HDRTorrent>();
            subject.Definition = Definition("HDRTorrent", "https://hdrtorrents.net/");
            var requestedUrls = new List<string>();

            SetupHdr(subject.Definition, requestedUrls, request =>
            {
                if (request.Url.FullUri == "https://hdrtorrents.net/filmes/avatar_o_caminho_da_agua/")
                {
                    return Response(request, HdrCard("Avatar O Caminho da Agua", "https://hdrtorrents.net/avatar-filme/"));
                }

                if (request.Url.FullUri == "https://hdrtorrents.net/series/avatar_o_caminho_da_agua/")
                {
                    return Response(request, HdrCard("Avatar O Caminho da Agua", "https://hdrtorrents.net/avatar-serie/"));
                }

                if (request.Url.FullUri is "https://hdrtorrents.net/avatar-filme/" or "https://hdrtorrents.net/avatar-serie/")
                {
                    return Response(request, HdrDetails("Avatar O Caminho da Agua", "Filme", "7.1 GB", Magnet("ffffffffffffffffffffffffffffffffffffffff", "Avatar.O.Caminho.da.Agua.2022.2160p.Dual"), "2160p / 7.1 GB"));
                }

                throw new AssertionException("Unexpected request " + request.Url.FullUri);
            });

            var releases = (await subject.Fetch(new BasicSearchCriteria { SearchTerm = "Avatar O Caminho da Agua", InteractiveSearch = true })).Releases;

            requestedUrls.Should().Contain("https://hdrtorrents.net/filmes/avatar_o_caminho_da_agua/");
            requestedUrls.Should().Contain("https://hdrtorrents.net/series/avatar_o_caminho_da_agua/");
            releases.Should().HaveCount(1);
            releases.Single().Guid.Should().Contain("ffffffffffffffffffffffffffffffffffffffff");
        }

        [Test]
        public async Task release_size_uses_explicit_magnet_option_size()
        {
            var subject = Mocker.Resolve<ApacheTorrent>();
            subject.Definition = Definition("ApacheTorrent", "https://apachetorrents.com/");

            SetupTokenSite(
                subject.Definition,
                "https://apachetorrents.com/",
                "abc123",
                "Avatar",
                SearchCard("https://apachetorrents.com/avatar/", "Avatar 4K"),
                "https://apachetorrents.com/avatar/",
                Details("Avatar 4K", "Filme", "20 GB", "2022", Magnet("1111111111111111111111111111111111111111", "Avatar.2022.720p.Dual"), "720p / 3.93 GB"));

            var release = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar", InteractiveSearch = true })).Releases.Single();

            release.Size.Should().Be(4219805368);
            release.Title.Should().Contain("720p");
        }

        [Test]
        public async Task release_size_uses_explicit_size_from_magnet_container()
        {
            var subject = Mocker.Resolve<ApacheTorrent>();
            subject.Definition = Definition("ApacheTorrent", "https://apachetorrents.com/");

            SetupTokenSite(
                subject.Definition,
                "https://apachetorrents.com/",
                "abc123",
                "Avatar",
                SearchCard("https://apachetorrents.com/avatar/", "Avatar"),
                "https://apachetorrents.com/avatar/",
                """
                <section id="informacoes"><p>
                <strong>Título:</strong> Avatar<br>
                <strong>Categoria:</strong> Filme<br>
                <strong>Áudio:</strong> Dual<br>
                </p></section>
                <ul class="lista-download">
                    <li>720p / 3.93 GB <a class="btn" href="magnet:?xt=urn:btih:8888888888888888888888888888888888888888&dn=Avatar.2022.720p.Dual">MAGNET</a></li>
                </ul>
                """);

            var release = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar", InteractiveSearch = true })).Releases.Single();

            release.Size.Should().Be(4219805368);
        }

        [Test]
        public async Task release_size_uses_magnet_xl_parameter()
        {
            var subject = Mocker.Resolve<ApacheTorrent>();
            subject.Definition = Definition("ApacheTorrent", "https://apachetorrents.com/");

            SetupTokenSite(
                subject.Definition,
                "https://apachetorrents.com/",
                "abc123",
                "Avatar",
                SearchCard("https://apachetorrents.com/avatar/", "Avatar"),
                "https://apachetorrents.com/avatar/",
                DetailsWithoutSize("Avatar", "Filme", "magnet:?xt=urn:btih:9999999999999999999999999999999999999999&dn=Avatar.2022.1080p.Dual&xl=123456789", "Download"));

            var release = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar", InteractiveSearch = true })).Releases.Single();

            release.Size.Should().Be(123456789);
        }

        [Test]
        public async Task release_size_uses_global_spec_size_when_single_magnet_exists()
        {
            var subject = Mocker.Resolve<RedeTorrent>();
            subject.Definition = Definition("RedeTorrent", "https://redestorrents.com/");

            SetupTokenSite(
                subject.Definition,
                "https://redestorrents.com/",
                "abc123",
                "Avatar",
                "<article><a href=\"https://redestorrents.com/avatar/\">Avatar</a></article>",
                "https://redestorrents.com/avatar/",
                """
                <h1>Avatar</h1>
                <div class="spec-card-glass"><small>Tamanho do Arquivo</small><strong>20.96 GB</strong></div>
                <div class="download-row">
                    <div class="download-name">Download</div>
                    <a href="magnet:?xt=urn:btih:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaab&dn=Avatar.2022.1080p.Dual">Download</a>
                </div>
                """);

            var release = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar", InteractiveSearch = true })).Releases.Single();

            release.Size.Should().Be(22505628631);
        }

        [Test]
        public async Task global_spec_size_is_not_applied_to_multiple_magnet_options()
        {
            var subject = Mocker.Resolve<RedeTorrent>();
            subject.Definition = Definition("RedeTorrent", "https://redestorrents.com/");

            SetupTokenSite(
                subject.Definition,
                "https://redestorrents.com/",
                "abc123",
                "Avatar",
                "<article><a href=\"https://redestorrents.com/avatar/\">Avatar</a></article>",
                "https://redestorrents.com/avatar/",
                """
                <h1>Avatar</h1>
                <div class="spec-card-glass"><small>Tamanho do Arquivo</small><strong>20.96 GB</strong></div>
                <div class="download-row">
                    <div class="download-name">720p</div>
                    <a href="magnet:?xt=urn:btih:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb&dn=Avatar.2022.720p.Dual">Download</a>
                </div>
                <div class="download-row">
                    <div class="download-name">1080p</div>
                    <a href="magnet:?xt=urn:btih:cccccccccccccccccccccccccccccccccccccccc&dn=Avatar.2022.1080p.Dual">Download</a>
                </div>
                """);

            var releases = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar", InteractiveSearch = true })).Releases;

            releases.Should().HaveCount(2);
            releases.Should().OnlyContain(release => release.Size == null);
        }

        [Test]
        public async Task release_size_remains_null_when_no_explicit_size_exists()
        {
            var subject = Mocker.Resolve<ApacheTorrent>();
            subject.Definition = Definition("ApacheTorrent", "https://apachetorrents.com/");

            SetupTokenSite(
                subject.Definition,
                "https://apachetorrents.com/",
                "abc123",
                "Avatar",
                SearchCard("https://apachetorrents.com/avatar/", "Avatar"),
                "https://apachetorrents.com/avatar/",
                DetailsWithoutSize("Avatar", "Filme", Magnet("2222222222222222222222222222222222222222", "Avatar.2022.1080p.Dual"), "1080p"));

            var release = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar", InteractiveSearch = true })).Releases.Single();

            release.Size.Should().BeNull();
        }

        [Test]
        public async Task missing_seeders_and_peers_remain_null()
        {
            var subject = Mocker.Resolve<ApacheTorrent>();
            subject.Definition = Definition("ApacheTorrent", "https://apachetorrents.com/");

            SetupTokenSite(
                subject.Definition,
                "https://apachetorrents.com/",
                "abc123",
                "Avatar",
                SearchCard("https://apachetorrents.com/avatar/", "Avatar"),
                "https://apachetorrents.com/avatar/",
                Details("Avatar", "Filme", "1 GB", "2022", Magnet("3333333333333333333333333333333333333333", "Avatar.2022.1080p.Dual"), "1080p / 1 GB"));

            var release = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar", InteractiveSearch = true })).Releases.Single().Should().BeOfType<TorrentInfo>().Subject;

            release.Seeders.Should().BeNull();
            release.Peers.Should().BeNull();
        }

        [Test]
        public async Task movie_search_discards_season_results()
        {
            var subject = Mocker.Resolve<ApacheTorrent>();
            subject.Definition = Definition("ApacheTorrent", "https://apachetorrents.com/");

            SetupTokenSite(
                subject.Definition,
                "https://apachetorrents.com/",
                "abc123",
                "Avatar",
                SearchCard("https://apachetorrents.com/avatar-s02/", "Avatar - 2ª Temporada"),
                "https://apachetorrents.com/avatar-s02/",
                Details("Avatar - 2ª Temporada", "Serie", "5 GB", "2022", Magnet("4444444444444444444444444444444444444444", "Avatar.2a.Temporada.1080p"), "1080p / 5 GB"));

            var releases = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar", InteractiveSearch = true })).Releases;

            releases.Should().BeEmpty();
        }

        [Test]
        public async Task magnet_dn_parameter_is_used_as_option_title()
        {
            var subject = Mocker.Resolve<ApacheTorrent>();
            subject.Definition = Definition("ApacheTorrent", "https://apachetorrents.com/");

            SetupTokenSite(
                subject.Definition,
                "https://apachetorrents.com/",
                "abc123",
                "Avatar",
                SearchCard("https://apachetorrents.com/avatar/", "Avatar"),
                "https://apachetorrents.com/avatar/",
                Details("Avatar", "Filme", "1 GB", "2022", Magnet("5555555555555555555555555555555555555555", "Avatar.Directors.Cut.1080p.Dual"), "botao magnet sem titulo util / 1 GB"));

            var release = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar", InteractiveSearch = true })).Releases.Single();

            release.Title.Should().Be("Avatar Directors Cut 1080p Dual");
        }

        [Test]
        public async Task portuguese_metadata_labels_are_extracted_from_detail_page()
        {
            var subject = Mocker.Resolve<HDRTorrent>();
            subject.Definition = Definition("HDRTorrent", "https://hdrtorrents.net/");

            SetupHdr(subject.Definition, new List<string>(), request =>
            {
                if (request.Url.FullUri == "https://hdrtorrents.net/filmes/cidade_de_deus/")
                {
                    return Response(request, HdrCard("Cidade de Deus", "https://hdrtorrents.net/cidade-de-deus/"));
                }

                if (request.Url.FullUri == "https://hdrtorrents.net/cidade-de-deus/")
                {
                    return Response(request, HdrDetails("Cidade de Deus", "Filme", "2 GB", Magnet("6666666666666666666666666666666666666666", "Cidade.de.Deus.2002.1080p.Dual"), "1080p / 2 GB", "2002-08-30T00:00:00Z"));
                }

                throw new AssertionException("Unexpected request " + request.Url.FullUri);
            });

            var release = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Cidade de Deus", InteractiveSearch = true })).Releases.Single();

            release.Title.Should().Contain("Cidade de Deus 2002 1080p Dual");
            release.PublishDate.Should().Be(new DateTime(2002, 8, 30, 0, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public async Task multiple_size_units_do_not_contaminate_explicit_option_size()
        {
            var subject = Mocker.Resolve<ApacheTorrent>();
            subject.Definition = Definition("ApacheTorrent", "https://apachetorrents.com/");

            SetupTokenSite(
                subject.Definition,
                "https://apachetorrents.com/",
                "abc123",
                "Avatar",
                SearchCard("https://apachetorrents.com/avatar/", "Avatar 4K 20 GB"),
                "https://apachetorrents.com/avatar/",
                Details("Avatar 4K", "Filme", "20 GB", "2022", Magnet("7777777777777777777777777777777777777777", "Avatar.2022.720p.Dual"), "720p / 3.93 GB / publicação 20 GB"));

            var release = (await subject.Fetch(new MovieSearchCriteria { SearchTerm = "Avatar", InteractiveSearch = true })).Releases.Single();

            release.Size.Should().Be(4219805368);
        }

        private void SetupTokenSite(ProviderDefinition definition, string baseUrl, string token, string query, string searchHtml, string detailUrl, string detailHtml)
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), (IndexerDefinition)definition))
                .ReturnsAsync((HttpRequest request, IndexerDefinition _) =>
                {
                    if (request.Url.FullUri == baseUrl)
                    {
                        return Response(request, $"<html><input name=\"token\" value=\"{token}\"></html>", Cookies(request.Url.FullUri, "PHPSESSID", "session"));
                    }

                    if (request.Url.FullUri.Contains($"index.php?hp_bot_check=&token={token}&busca={WebUtility.UrlEncode(query)}"))
                    {
                        return Response(request, searchHtml);
                    }

                    if (request.Url.FullUri == detailUrl)
                    {
                        return Response(request, detailHtml);
                    }

                    throw new AssertionException("Unexpected request " + request.Url.FullUri);
                });
        }

        private void SetupHdr(ProviderDefinition definition, List<string> requestedUrls, Func<HttpRequest, HttpResponse> responder)
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), (IndexerDefinition)definition))
                .ReturnsAsync((HttpRequest request, IndexerDefinition _) =>
                {
                    requestedUrls.Add(request.Url.FullUri);
                    return responder(request);
                });
        }

        private static IndexerDefinition Definition(string name, string baseUrl)
        {
            return new IndexerDefinition
            {
                Name = name,
                Settings = new NoAuthTorrentBaseSettings
                {
                    BaseUrl = baseUrl
                }
            };
        }

        private static HttpResponse Response(HttpRequest request, string content, CookieCollection cookies = null)
        {
            return new HttpResponse(request, new HttpHeader { { "Content-Type", "text/html; charset=utf-8" } }, cookies ?? new CookieCollection(), content);
        }

        private static CookieCollection Cookies(string url, string name, string value)
        {
            return new CookieCollection
            {
                new Cookie(name, value, "/", new Uri(url).Host)
            };
        }

        private static string SearchCard(string detailUrl, string title)
        {
            return $"<div class=\"capaname\"><a href=\"{detailUrl}\" title=\"{title}\">{title}</a></div>";
        }

        private static string HdrCard(string title, string detailUrl)
        {
            return $"<div class=\"col\"><a class=\"media-card-link\" href=\"{detailUrl}\">{title}</a></div>";
        }

        private static string Details(string title, string type, string size, string year, string magnet, string optionLabel)
        {
            return $"""
                <section id="informacoes"><p>
                <strong>Título:</strong> {title}<br>
                <strong>Categoria:</strong> {type}<br>
                <strong>Tamanho:</strong> {size}<br>
                <strong>Ano de Lançamento:</strong> {year}<br>
                <strong>Áudio:</strong> Dual<br>
                </p></section>
                <a class="btn" href="{magnet}">{optionLabel}</a>
                """;
        }

        private static string DetailsWithoutSize(string title, string type, string magnet, string optionLabel)
        {
            return $"""
                <section id="informacoes"><p>
                <strong>Título:</strong> {title}<br>
                <strong>Categoria:</strong> {type}<br>
                <strong>Áudio:</strong> Dual<br>
                </p></section>
                <a class="btn" href="{magnet}">{optionLabel}</a>
                """;
        }

        private static string HdrDetails(string title, string type, string size, string magnet, string optionLabel, string published = "2024-01-01T00:00:00Z")
        {
            return $"""
                <meta property="article:published_time" content="{published}">
                <div class="infos"><p>
                <b>Título:</b> {title}<br>
                <b>Categoria:</b> {type}<br>
                <b>Tamanho:</b> {size}<br>
                <b>Ano de Lançamento:</b> 2002<br>
                <b>Áudio:</b> Dual<br>
                </p></div>
                <p class="download-row">{optionLabel} <a href="{magnet}">MAGNET</a></p>
                """;
        }

        private static string Magnet(string infoHash, string title)
        {
            return $"magnet:?xt=urn:btih:{infoHash}&dn={WebUtility.UrlEncode(title)}";
        }
    }
}
