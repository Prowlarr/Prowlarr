using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Test.Framework;

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

        private IndexerDefinition GivenIndexer(int id)
        {
            return new IndexerDefinition
            {
                Id = id,
                Name = $"SomeSite {id}",
                Settings = new CardigannSettings { DefinitionFile = "somesite" }
            };
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
    }
}
