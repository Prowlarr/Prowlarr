using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test.Http
{
    [TestFixture]
    public class HttpHeaderFixture : TestBase
    {
        [TestCase("text/html; charset=\"utf-8\"", "utf-8")]
        [TestCase("text/html; charset=utf-8", "utf-8")]
        public void should_get_encoding_from_content_type_header(string contentType, string charsetExpected)
        {
            var headers = new NameValueCollection();

            headers.Add("Content-Type", contentType);

            var httpheader = new HttpHeader(headers);

            httpheader.GetEncodingFromContentType().Should().Be(Encoding.GetEncoding(charsetExpected));
        }

        [TestCase("text/html; charset=asdasd")]
        public void should_throw_when_invalid_encoding_is_in_content_type_header(string contentType)
        {
            var headers = new NameValueCollection();

            headers.Add("Content-Type", contentType);

            var httpheader = new HttpHeader(headers);

            Action action = () => httpheader.GetEncodingFromContentType();
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void should_parse_cookie_with_trailing_semi_colon()
        {
            var cookies = HttpHeader.ParseCookies("uid=123456; pass=123456b2f3abcde42ac3a123f3f1fc9f;");

            cookies.Count.Should().Be(2);
            cookies.First().Key.Should().Be("uid");
            cookies.First().Value.Should().Be("123456");
            cookies.Last().Key.Should().Be("pass");
            cookies.Last().Value.Should().Be("123456b2f3abcde42ac3a123f3f1fc9f");
        }

        [Test]
        public void should_keep_multiple_set_cookie_headers_separate()
        {
            var responseMessage = new HttpResponseMessage();
            responseMessage.Headers.TryAddWithoutValidation("Set-Cookie", "XSRF-TOKEN=cookie1; Max-Age=7200; path=/; secure; samesite=lax");
            responseMessage.Headers.TryAddWithoutValidation("Set-Cookie", "laravel_session=cookie2; Max-Age=7200; path=/; secure; httponly; samesite=lax");

            var response = new HttpResponse(new HttpRequest("https://localhost"), new HttpHeader(responseMessage.Headers), new CookieCollection(), Array.Empty<byte>());

            response.GetCookieHeaders().Should().HaveCount(2);

            var uri = new Uri("https://localhost");
            var container = new CookieContainer();

            foreach (var cookieHeader in response.GetCookieHeaders())
            {
                container.SetCookies(uri, cookieHeader);
            }

            var cookies = container.GetCookies(uri);

            cookies.Should().HaveCount(2);
            cookies["XSRF-TOKEN"].Value.Should().Be("cookie1");
            cookies["laravel_session"].Value.Should().Be("cookie2");
        }
    }
}
