using System;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test.Http
{
    [TestFixture]
    public class HttpRequestBuilderFixture : TestBase
    {
        [OneTimeSetUp]
        public void RegisterEncodingProvider()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [TestCase("http://host/{seg}/some", "http://host/dir/some")]
        [TestCase("http://host/some/{seg}", "http://host/some/dir")]
        public void should_add_single_segment_url_segments(string url, string result)
        {
            var requestBuilder = new HttpRequestBuilder(url);

            requestBuilder.SetSegment("seg", "dir");

            requestBuilder.Build().Url.Should().Be(result);
        }

        [Test]
        public void shouldnt_add_value_for_nonexisting_segment()
        {
            var requestBuilder = new HttpRequestBuilder("http://host/{seg}/some");
            Assert.Throws<InvalidOperationException>(() => requestBuilder.SetSegment("seg2", "dir"));
        }

        [Test]
        public void should_remove_duplicated_slashes()
        {
            var builder = new HttpRequestBuilder("http://domain/");

            var request = builder.Resource("/v1/").Build();

            request.Url.FullUri.Should().Be("http://domain/v1/");
        }

        [Test]
        public void should_encode_form_parameters_with_utf8_by_default()
        {
            var builder = new HttpRequestBuilder("http://domain/login")
                .Post()
                .AddFormParameter("username", "Привет");

            var request = builder.Build();
            var body = Encoding.UTF8.GetString(request.ContentData);

            // UTF-8 encoding: Привет = %D0%9F%D1%80%D0%B8%D0%B2%D0%B5%D1%82
            body.Should().Contain("username=%D0%9F%D1%80%D0%B8%D0%B2%D0%B5%D1%82");
        }

        [Test]
        public void should_encode_form_parameters_with_windows_1251_for_cyrillic()
        {
            var windows1251 = Encoding.GetEncoding("windows-1251");

            var builder = new HttpRequestBuilder("http://domain/login")
                .Post()
                .SetEncoding(windows1251)
                .AddFormParameter("username", "Привет");

            var request = builder.Build();
            var body = windows1251.GetString(request.ContentData);

            // Windows-1251 encoding: Привет = %CF%F0%E8%E2%E5%F2
            body.Should().Contain("username=%CF%F0%E8%E2%E5%F2");
        }

        [Test]
        public void should_encode_form_parameters_with_iso_8859_1_for_extended_latin()
        {
            var iso88591 = Encoding.GetEncoding("iso-8859-1");

            var builder = new HttpRequestBuilder("http://domain/login")
                .Post()
                .SetEncoding(iso88591)
                .AddFormParameter("username", "café");

            var request = builder.Build();
            var body = iso88591.GetString(request.ContentData);

            // ISO-8859-1 encoding: é = %E9
            body.Should().Contain("username=caf%E9");
        }

        [Test]
        public void should_encode_form_parameters_ascii_same_regardless_of_encoding()
        {
            var windows1251 = Encoding.GetEncoding("windows-1251");

            var builder = new HttpRequestBuilder("http://domain/login")
                .Post()
                .SetEncoding(windows1251)
                .AddFormParameter("username", "testuser")
                .AddFormParameter("password", "pass123");

            var request = builder.Build();
            var body = windows1251.GetString(request.ContentData);

            body.Should().Be("username=testuser&password=pass123");
        }
    }
}
