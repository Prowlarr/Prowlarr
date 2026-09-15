using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Localization;
using NzbDrone.Test.Common;
using Prowlarr.Api.V1.Indexers;
using Prowlarr.Http.ClientSchema;

namespace NzbDrone.Api.Test.Indexers
{
    [TestFixture]
    public class IndexerResourceFixture : TestBase
    {
        private IndexerResourceMapper _mapper;

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<ILocalizationService>()
                .Setup(s => s.GetLocalizedString(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Dictionary<string, object>>((s, d) => s);

            SchemaBuilder.Initialize(Mocker.Container);

            _mapper = new IndexerResourceMapper(Mocker.GetMock<IIndexerDefinitionUpdateService>().Object);
        }

        private static IndexerDefinition GivenDefinition(string userAgent) => new()
        {
            Id = 1,
            Name = "Test",
            ImplementationName = "Newznab",
            Implementation = nameof(Newznab),
            ConfigContract = nameof(NewznabSettings),
            Settings = new NewznabSettings { BaseUrl = "https://example.com" },
            Capabilities = new IndexerCapabilities(),
            UserAgent = userAgent
        };

        // The API serializer is configured with DefaultIgnoreCondition = WhenWritingNull, so a null here
        // would be dropped from the payload entirely and the edit form would read undefined for it.
        [Test]
        public void should_not_return_a_null_user_agent_when_unset()
        {
            var resource = _mapper.ToResource(GivenDefinition(null));

            resource.UserAgent.Should().NotBeNull();
            resource.UserAgent.Should().BeEmpty();
        }

        [Test]
        public void should_return_the_configured_user_agent()
        {
            var resource = _mapper.ToResource(GivenDefinition("CustomUA/1.0"));

            resource.UserAgent.Should().Be("CustomUA/1.0");
        }

        [TestCase("")]
        [TestCase("   ")]
        public void should_store_a_blank_user_agent_as_null(string userAgent)
        {
            var resource = _mapper.ToResource(GivenDefinition("CustomUA/1.0"));
            resource.UserAgent = userAgent;

            var definition = _mapper.ToModel(resource, GivenDefinition("CustomUA/1.0"));

            definition.UserAgent.Should().BeNull();
        }

        [Test]
        public void should_round_trip_a_configured_user_agent()
        {
            var resource = _mapper.ToResource(GivenDefinition("CustomUA/1.0"));

            var definition = _mapper.ToModel(resource, GivenDefinition("CustomUA/1.0"));

            definition.UserAgent.Should().Be("CustomUA/1.0");
        }
    }
}
