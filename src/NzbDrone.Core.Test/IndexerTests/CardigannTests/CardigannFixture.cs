using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Test.IndexerTests.CardigannTests
{
    [TestFixture]
    public class CardigannFixture : CoreTest<Cardigann>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());

            Mocker.GetMock<IIndexerDefinitionUpdateService>()
                .Setup(s => s.GetCachedDefinition("somesite"))
                .Returns(new CardigannDefinition
                {
                    Encoding = "UTF-8",
                    Links = new List<string> { "https://somesite.com/" },
                    Caps = new CapabilitiesBlock(),
                    Search = new SearchBlock()
                });
        }

        private IndexerDefinition GivenIndexer(int id, CardigannSettings settings = null)
        {
            return new IndexerDefinition
            {
                Id = id,
                Name = $"SomeSite {id}",
                Settings = settings ?? new CardigannSettings { DefinitionFile = "somesite" }
            };
        }

        private List<HttpRequest> GivenCaptchaSite()
        {
            Mocker.GetMock<IIndexerDefinitionUpdateService>()
                .Setup(s => s.GetCachedDefinition("somesite"))
                .Returns(new CardigannDefinition
                {
                    Encoding = "UTF-8",
                    Links = new List<string> { "https://somesite.com/" },
                    Settings = new List<SettingsField>(),
                    Caps = new CapabilitiesBlock(),
                    Search = new SearchBlock(),
                    Login = new LoginBlock
                    {
                        Method = "form",
                        Path = "login.php",
                        Captcha = new CaptchaBlock { Type = "image", Selector = "img#captcha", Input = "captcha" }
                    }
                });

            var requests = new List<HttpRequest>();

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(c => c.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), It.IsAny<ProviderDefinition>()))
                .Callback<HttpRequest, ProviderDefinition>((r, _) => requests.Add(r))
                .ReturnsAsync((HttpRequest r, ProviderDefinition _) =>
                {
                    var content = r.Url.Path == "/login.php"
                        ? "<html><form action=\"takelogin.php\"><input name=\"captcha\"></form><img id=\"captcha\" src=\"captcha.php\"></html>"
                        : string.Empty;

                    return new HttpResponse(r, new HttpHeader(), new CookieCollection(), content);
                });

            return requests;
        }

        [Test]
        public void should_not_share_request_generator_between_indexers_with_same_definition()
        {
            var first = GivenIndexer(1);
            var second = GivenIndexer(2);

            Subject.Definition = first;
            var firstGenerator = (CardigannRequestGenerator)Subject.GetRequestGenerator();

            Subject.Definition = second;
            var secondGenerator = (CardigannRequestGenerator)Subject.GetRequestGenerator();

            secondGenerator.Should().NotBeSameAs(firstGenerator);
            firstGenerator.Definition.Should().BeSameAs(first);
            firstGenerator.Settings.Should().BeSameAs(first.Settings);
        }

        [Test]
        public void should_reuse_request_generator_for_same_indexer()
        {
            Subject.Definition = GivenIndexer(1);
            var firstGenerator = Subject.GetRequestGenerator();

            Subject.Definition = GivenIndexer(1);
            var secondGenerator = Subject.GetRequestGenerator();

            secondGenerator.Should().BeSameAs(firstGenerator);
        }

        [Test]
        public async Task should_login_with_captcha_from_setup_for_unsaved_indexer()
        {
            var requests = GivenCaptchaSite();

            Subject.Definition = GivenIndexer(0);
            Subject.RequestAction("checkCaptcha", new Dictionary<string, string>());

            var settings = new CardigannSettings { DefinitionFile = "somesite" };
            settings.ExtraFieldData["CAPTCHA"] = "abcd";
            Subject.Definition = GivenIndexer(0, settings);

            var generator = (CardigannRequestGenerator)Subject.GetRequestGenerator();
            await generator.DoLogin();

            requests.Should().ContainSingle(r => r.Url.Path == "/login.php");

            var loginRequest = requests.Single(r => r.Method == HttpMethod.Post);
            loginRequest.Url.Path.Should().Be("/takelogin.php");
            loginRequest.GetContent().Should().Contain("captcha=abcd");
        }
    }
}
