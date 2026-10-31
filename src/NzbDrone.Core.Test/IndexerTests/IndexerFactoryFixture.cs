using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests
{
    [TestFixture]
    public class IndexerFactoryFixture : DbTest<IndexerFactory, IndexerDefinition>
    {
        private CardigannDefinition _cardigannDefinition;

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IIndexerRepository>(Mocker.Resolve<IndexerRepository>());
            Mocker.SetConstant<IEnumerable<IIndexer>>(new List<IIndexer>());

            _cardigannDefinition = new CardigannDefinition
            {
                Encoding = "UTF-8",
                Type = "private",
                Links = new List<string> { "https://somesite.com/" },
                Settings = new List<SettingsField>
                {
                    new() { Name = "username", Label = "Username", Type = "text" }
                },
                Login = new LoginBlock { Captcha = new CaptchaBlock() },
                Caps = new CapabilitiesBlock
                {
                    Modes = new Dictionary<string, List<string>>
                    {
                        { "search", new List<string> { "q" } }
                    }
                }
            };

            Mocker.GetMock<IIndexerDefinitionUpdateService>()
                .Setup(s => s.GetCachedDefinition("somesite"))
                .Returns(_cardigannDefinition);
        }

        private IndexerDefinition GivenCardigannIndexer()
        {
            var indexer = Builder<IndexerDefinition>.CreateNew()
                .With(i => i.Id = 0)
                .With(i => i.Implementation = nameof(Cardigann))
                .With(i => i.ConfigContract = nameof(CardigannSettings))
                .With(i => i.Settings = new CardigannSettings { DefinitionFile = "somesite" })
                .BuildNew();

            Db.Insert(indexer);

            return indexer;
        }

        [Test]
        public void should_add_captcha_field_without_changing_cached_definition()
        {
            var indexer = GivenCardigannIndexer();

            var result = Subject.Get(indexer.Id);

            result.ExtraFields.Should().Contain(f => f.Type == "cardigannCaptcha");
            result.ExtraFields.Should().NotBeSameAs(_cardigannDefinition.Settings);
            _cardigannDefinition.Settings.Should().NotContain(f => f.Type == "cardigannCaptcha");
        }
    }
}
