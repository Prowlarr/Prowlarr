using System;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Applications.Listenarr;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Applications.Listenarr
{
    [TestFixture]
    public class ListenarrV1ProxyFixture : TestBase<ListenarrV1Proxy>
    {
        [Test]
        public void GetIndexers_should_deserialize_json_and_set_api_key_header()
        {
            var settings = new ListenarrSettings { BaseUrl = "http://localhost:4545", ApiKey = "abc123" };

            var responseJson = new[]
            {
                new
                {
                    Id = "42",
                    Name = "Test",
                    Implementation = "Newznab",
                    Fields = new[]
                    {
                        new { name = "baseUrl", value = "http://localhost:4545/1/api" },
                        new { name = "apiKey", value = "x" },
                    }
                }
            }.ToJson();

            HttpRequest capturedRequest = null;

            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Execute(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(req =>
                {
                    capturedRequest = req;
                    return new HttpResponse(req, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), System.Text.Encoding.UTF8.GetBytes(responseJson), 0, HttpStatusCode.OK, new Version("1.0"));
                });

            var result = Subject.GetIndexers(settings);

            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            capturedRequest.Headers.GetSingleValue("X-Api-Key").Should().Be("abc123");

            (capturedRequest.Url.ToString().Contains("/api/indexer") || capturedRequest.Url.ToString().Contains("/api/v1/indexer")).Should().BeTrue();
        }

        [Test]
        public void GetIndexerSchema_should_handle_single_object_response_with_fields_object()
        {
            var settings = new ListenarrSettings { BaseUrl = "http://localhost:4545", ApiKey = "abc123" };

            var json = "[ { \"id\": 1, \"implementation\": \"Newznab\", \"fields\": [ { \"name\": \"baseUrl\", \"type\": \"text\" }, { \"name\": \"apiKey\", \"type\": \"text\" } ] } ]";

            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Execute(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(req => new HttpResponse(req, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), json, 0, HttpStatusCode.OK, new Version("1.0")));

            var result = Subject.GetIndexerSchema(settings);

            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            result[0].Fields.Should().NotBeNull();
            result[0].Fields.Count.Should().Be(2);
            result[0].Fields.Should().Contain(f => f.Name == "baseUrl");
            result[0].Fields.Should().Contain(f => f.Name == "apiKey");
        }

        [Test]
        public void GetIndexerSchema_should_preserve_implementations_array_as_list()
        {
            var settings = new ListenarrSettings { BaseUrl = "http://localhost:4545", ApiKey = "abc123" };

            var json = "[ { \"fields\": [ { \"name\": \"baseUrl\", \"type\": \"text\" } ], \"implementations\": [\"Newznab\",\"Torznab\"] } ]";

            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Execute(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(req => new HttpResponse(req, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), json, 0, HttpStatusCode.OK, new Version("1.0")));

            var result = Subject.GetIndexerSchema(settings);

            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            result[0].Implementations.Should().NotBeNull();
            result[0].Implementations.Should().Contain("Newznab");
            result[0].Implementations.Should().Contain("Torznab");
        }

        [Test]
        public void Execute_should_throw_application_exception_when_unauthorized()
        {
            var settings = new ListenarrSettings { BaseUrl = "http://localhost:4545", ApiKey = "bad" };

            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Execute(It.IsAny<HttpRequest>()))
                .Throws(new HttpException(new HttpResponse(new HttpRequest("http://localhost/"), new HttpHeader(), new CookieCollection(), "unauthorized", 0, HttpStatusCode.Unauthorized, new Version("1.0"))));

            Assert.Throws<NzbDrone.Common.Http.HttpException>(() => Subject.GetIndexers(settings));

            ExceptionVerification.ExpectedWarns(0);
        }
    }
}
