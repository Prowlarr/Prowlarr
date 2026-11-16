using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Test.IndexerTests
{
    [TestFixture]
    public class IndexerQueryTimeoutFixture : CoreTest<Newznab>
    {
        private NewznabSettings _settings;
        private List<HttpRequest> _requests;

        [SetUp]
        public void Setup()
        {
            _settings = new NewznabSettings { BaseUrl = "http://indexer.local/" };

            Subject.Definition = new IndexerDefinition
            {
                Id = 5,
                Name = "Newznab",
                Settings = _settings
            };

            var caps = new IndexerCapabilities();
            caps.Categories.AddCategoryMapping(2000, NewznabStandardCategory.Movies, "Movies");

            Mocker.GetMock<INewznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<NewznabSettings>(), It.IsAny<IndexerDefinition>()))
                .Returns(caps);

            var feed = ReadAllText(@"Files/Indexers/Newznab/newznab_nzb_su.xml");

            _requests = new List<HttpRequest>();

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), It.IsAny<ProviderDefinition>()))
                .Callback<HttpRequest, ProviderDefinition>((r, _) => _requests.Add(r))
                .ReturnsAsync((HttpRequest r, ProviderDefinition _) => new HttpResponse(r, new HttpHeader(), new CookieCollection(), feed));
        }

        private Task FetchAsync()
        {
            return Subject.Fetch(new MovieSearchCriteria { Categories = new[] { 2000 }, SearchTerm = "test" });
        }

        [Test]
        public async Task should_not_set_request_timeout_when_query_timeout_is_not_set()
        {
            _settings.BaseSettings.QueryLimit = 10;

            await FetchAsync();

            _requests.Should().NotBeEmpty();
            _requests.Should().OnlyContain(r => r.RequestTimeout == TimeSpan.Zero);
        }

        [TestCase(10)]
        [TestCase(30)]
        [TestCase(300)]
        public async Task should_set_request_timeout_from_query_timeout(int seconds)
        {
            _settings.BaseSettings.QueryTimeout = seconds;

            await FetchAsync();

            _requests.Should().NotBeEmpty();
            _requests.Should().OnlyContain(r => r.RequestTimeout == TimeSpan.FromSeconds(seconds));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public async Task should_ignore_non_positive_query_timeout(int seconds)
        {
            _settings.BaseSettings.QueryTimeout = seconds;

            await FetchAsync();

            _requests.Should().NotBeEmpty();
            _requests.Should().OnlyContain(r => r.RequestTimeout == TimeSpan.Zero);
        }

        [TestCase(1)]
        [TestCase(9)]
        public async Task should_raise_query_timeout_that_bypassed_validation_to_minimum(int seconds)
        {
            _settings.BaseSettings.QueryTimeout = seconds;

            await FetchAsync();

            _requests.Should().NotBeEmpty();
            _requests.Should().OnlyContain(r => r.RequestTimeout == TimeSpan.FromSeconds(IndexerBaseSettings.MinQueryTimeout));
        }

        [TestCase(301)]
        [TestCase(int.MaxValue)]
        public async Task should_cap_query_timeout_that_bypassed_validation(int seconds)
        {
            _settings.BaseSettings.QueryTimeout = seconds;

            await FetchAsync();

            _requests.Should().NotBeEmpty();
            _requests.Should().OnlyContain(r => r.RequestTimeout == TimeSpan.FromSeconds(IndexerBaseSettings.MaxQueryTimeout));
        }
    }
}
