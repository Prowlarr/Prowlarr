using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Applications;
using NzbDrone.Core.Configuration;
using NzbDrone.Test.Common;
using static NzbDrone.Core.Applications.IndexerNameTemplateDefaults;

namespace NzbDrone.Core.Test.IndexerTests
{
    [TestFixture]
    public class IndexerNameTemplateServiceFixture : TestBase<IndexerNameTemplateService>
    {
        private Mock<IConfigService> _configService;

        [SetUp]
        public void Setup()
        {
            _configService = Mocker.GetMock<IConfigService>();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void FormatIndexerName_should_return_empty_when_invalid_input(string indexerName)
        {
            _configService.Setup(s => s.IndexerNameTemplate).Returns("{name} ({instance})");

            var result = Subject.FormatIndexerName(indexerName, "Prowlarr");

            result.Should().Be("");
        }

        [Test]
        public void FormatIndexerName_should_format_with_template()
        {
            _configService.Setup(s => s.IndexerNameTemplate).Returns("{name} ({instance})");

            var result = Subject.FormatIndexerName("MyIndexer", "MyProwlarr");

            result.Should().Be("MyIndexer (MyProwlarr)");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void FormatIndexerName_should_use_fallback_instance_name(string instanceName)
        {
            _configService.Setup(s => s.IndexerNameTemplate).Returns("{name} ({instance})");

            var result = Subject.FormatIndexerName("MyIndexer", instanceName);

            result.Should().Be($"MyIndexer ({DefaultInstanceName})");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void FormatIndexerName_should_return_original_name_when_no_template(string template)
        {
            _configService.Setup(s => s.IndexerNameTemplate).Returns(template);

            var result = Subject.FormatIndexerName("MyIndexer", "MyProwlarr");

            result.Should().Be("MyIndexer");
        }

        [Test]
        public void FormatIndexerName_should_handle_custom_template()
        {
            _configService.Setup(s => s.IndexerNameTemplate).Returns("[{instance}] {name}");

            var result = Subject.FormatIndexerName("MyIndexer", "MyProwlarr");

            result.Should().Be("[MyProwlarr] MyIndexer");
        }
    }
}
