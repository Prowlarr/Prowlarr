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
            capturedRequest.Url.ToString().Should().Contain("/api/indexer");
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
            Assert.Throws<ApplicationException>(() => Subject.GetIndexers(settings));

            // expected error was logged
            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
