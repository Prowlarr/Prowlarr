using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NLog;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.CardigannTests
{
    public class ApplyFiltersFixture : CoreTest<ApplyFiltersFixture.TestCardigannIndexer>
    {
        private CardigannDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _definition = Builder<CardigannDefinition>.CreateNew()
                                                      .With(x => x.Encoding = "UTF-8")
                                                      .With(x => x.Links = new List<string>
                                                      {
                                                          "https://somesite.com/"
                                                      })
                                                      .With(x => x.Caps = new CapabilitiesBlock
                                                      {
                                                          Modes = new Dictionary<string, List<string>>
                                                          {
                                                              { "search", new List<string> { "q" } }
                                                          }
                                                      })
                                                      .Build();

            Mocker.SetConstant<CardigannDefinition>(_definition);
        }

        [TestCase("bWFnbmV0Oj94dD11cm46YnRpaDoxMjM0NTY3ODkwYWJjZGVmMTIzNDU2Nzg5MGFiY2RlZjEyMzQ1Njc4", "magnet:?xt=urn:btih:1234567890abcdef1234567890abcdef12345678")]
        [TestCase("aGVsbG8gd29ybGQ=", "hello world")]
        public void should_decode_base64(string input, string expected)
        {
            var filters = new List<FilterBlock> { new FilterBlock { Name = "b64decode" } };

            var result = Subject.ApplyFiltersPublic(input, filters);

            result.Should().Be(expected);
        }

        [TestCase("aGVsbG8gd29ybGQ", "hello world")]
        [TestCase("aGVsbG\n 8gd29ybGQ\t\n", "hello world")]
        [TestCase(" aGVsbG8gd29ybGQ= ", "hello world")]
        public void should_decode_base64_missing_padding_or_containing_whitespace(string input, string expected)
        {
            var filters = new List<FilterBlock> { new FilterBlock { Name = "b64decode" } };

            var result = Subject.ApplyFiltersPublic(input, filters);

            result.Should().Be(expected);
        }

        [TestCase("this is not valid base64!!!")]
        public void should_throw_on_non_base64_characters(string input)
        {
            var filters = new List<FilterBlock> { new FilterBlock { Name = "b64decode" } };

            Assert.Throws<FormatException>(() => Subject.ApplyFiltersPublic(input, filters));
        }

        [TestCase("aGVsbG8gd29ybGQ==")]
        public void should_throw_on_extra_padding_after_a_complete_quantum(string input)
        {
            // This input is already a multiple of 4 characters, so the filter's
            // padding-tolerance logic leaves it untouched; it is still invalid
            // base64 because "==" here is trailing padding with no incomplete
            // quantum before it, so Convert.FromBase64String must reject it.
            var filters = new List<FilterBlock> { new FilterBlock { Name = "b64decode" } };

            Assert.Throws<FormatException>(() => Subject.ApplyFiltersPublic(input, filters));
        }

        // Exposes the protected ApplyFilters method from CardigannBase for direct testing.
        public class TestCardigannIndexer : CardigannBase
        {
            public TestCardigannIndexer(IConfigService configService, CardigannDefinition definition, Logger logger)
                : base(configService, definition, logger)
            {
            }

            public string ApplyFiltersPublic(string data, List<FilterBlock> filters, Dictionary<string, object> variables = null)
            {
                return ApplyFilters(data, filters, variables);
            }
        }
    }
}
