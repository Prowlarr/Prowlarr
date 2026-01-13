using System;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
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
            // Arrange
            var settings = new ListenarrSettings { BaseUrl = "http://localhost:5000", ApiKey = "abc123" };

            var json = "[ { \"id\": 42, \"name\": \"Test\", \"implementation\": \"Newznab\", \"fields\": [ { \"name\": \"baseUrl\", \"value\": \"http://localhost:5000/1/api\" }, { \"name\": \"apiKey\", \"value\": \"x\" } ] } ]";

            HttpRequest capturedRequest = null;

            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Execute(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(req =>
                {
                    capturedRequest = req;
                    return new HttpResponse(req, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), json, 0, HttpStatusCode.OK, new Version("1.0"));
                });

            // Act
            var result = Subject.GetIndexers(settings);

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            capturedRequest.Headers.GetSingleValue("X-Api-Key").Should().Be("abc123");

            // Accept either singular /api/v1/indexer or legacy /api/indexer in requests
            (capturedRequest.Url.ToString().Contains("/api/indexer") || capturedRequest.Url.ToString().Contains("/api/v1/indexer")).Should().BeTrue();
        }

        [Test]
        public void GetIndexerSchema_should_handle_single_object_response_with_fields_object()
        {
            // Arrange
            var settings = new ListenarrSettings { BaseUrl = "http://localhost:5000", ApiKey = "abc123" };

            // Schema returned as an object with fields as an object (name -> definition)
            var json = "{ \"id\": 1, \"implementation\": \"Newznab\", \"fields\": { \"baseUrl\": { \"type\": \"text\" }, \"apiKey\": { \"type\": \"text\" } } }";

            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Execute(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(req => new HttpResponse(req, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), json, 0, HttpStatusCode.OK, new Version("1.0")));

            // Act
            var result = Subject.GetIndexerSchema(settings);

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            result[0].Fields.Should().NotBeNull();
            result[0].Fields.Count.Should().Be(2);
            result[0].Fields.Should().Contain(f => f.Name == "baseUrl");
            result[0].Fields.Should().Contain(f => f.Name == "apiKey");
        }

        [Test]
        public void GetIndexerSchema_should_expand_implementations_array_into_multiple_schemas()
        {
            // Arrange
            var settings = new ListenarrSettings { BaseUrl = "http://localhost:5000", ApiKey = "abc123" };

            var json = "{ \"fields\": [ { \"name\": \"baseUrl\", \"type\": \"text\" } ], \"implementations\": [\"Newznab\",\"Torznab\"] }";

            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Execute(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(req => new HttpResponse(req, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), json, 0, HttpStatusCode.OK, new Version("1.0")));

            // Act
            var result = Subject.GetIndexerSchema(settings);

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result.Should().Contain(r => r.Implementation == "Newznab");
            result.Should().Contain(r => r.Implementation == "Torznab");
        }

        [Test]
        public void Execute_should_throw_application_exception_when_unauthorized()
        {
            // Arrange
            var settings = new ListenarrSettings { BaseUrl = "http://localhost:5000", ApiKey = "bad" };

            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Execute(It.IsAny<HttpRequest>()))
                .Throws(new HttpException(new HttpResponse(new HttpRequest("http://localhost/"), new HttpHeader(), new CookieCollection(), "unauthorized", 0, HttpStatusCode.Unauthorized, new Version("1.0"))));

            // Act / Assert
            Assert.Throws<NzbDrone.Common.Http.HttpException>(() => Subject.GetIndexers(settings));

            // No warning is logged by GetIndexers on unauthorized (it throws before the API-key-specific log path)
            ExceptionVerification.ExpectedWarns(0);
        }
    }
}
