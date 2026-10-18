using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.IndexerTests
{
    public class IndexerServiceFixture : DbTest<IndexerFactory, IndexerDefinition>
    {
        private List<IIndexer> _indexers;

        [SetUp]
        public void Setup()
        {
            _indexers = new List<IIndexer>();

            _indexers.Add(Mocker.Resolve<Newznab>());

            Mocker.SetConstant<IEnumerable<IIndexer>>(_indexers);
        }

        [Test]
        public void should_restore_newznab_preset_metadata_after_loading_saved_settings()
        {
            var repo = Mocker.Resolve<IndexerRepository>();
            Mocker.SetConstant<IIndexerRepository>(repo);
            var indexer = (Newznab)_indexers.Single();
            var definition = indexer.DefaultDefinitions.Cast<IndexerDefinition>().Single(d => d.Name == "Curupira");
            definition.Name = "My Curupira";
            repo.Insert(definition);

            var stored = repo.Get(definition.Id);
            stored.Language.Should().BeNull();
            stored.Description.Should().BeNull();
            indexer.Definition = stored;
            Subject.SetProviderCharacteristics(indexer, stored);

            stored.Language.Should().Be("pt-BR");
            stored.Description.Should().Be("Curupira is a Brazilian private Usenet indexer");
        }

        [Test]
        public void should_remove_missing_indexers_on_startup()
        {
            var repo = Mocker.Resolve<IndexerRepository>();

            Mocker.SetConstant<IIndexerRepository>(repo);

            var existingIndexers = Builder<IndexerDefinition>.CreateNew().BuildNew();
            existingIndexers.ConfigContract = nameof(NewznabSettings);

            repo.Insert(existingIndexers);

            Subject.Handle(new ApplicationStartedEvent());

            AllStoredModels.Should().NotContain(c => c.Id == existingIndexers.Id);

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
