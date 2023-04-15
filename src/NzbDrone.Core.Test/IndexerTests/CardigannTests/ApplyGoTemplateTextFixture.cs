using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.CardigannTests
{
    public class ApplyGoTemplateTextFixture : CoreTest<CardigannBase>
    {
        private Dictionary<string, object> _variables;
        private CardigannDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _variables = new Dictionary<string, object>
            {
                [".Config.sitelink"] = "https://somesite.com/",
                [".True"] = "True",
                [".False"] = null,
                [".Today.Year"] = DateTime.Today.Year.ToString(),
                [".Categories"] = new string[] { "tv", "movies" }
            };

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

        [TestCase("{{ range .Categories}}&categories[]={{.}}{{end}}", "&categories[]=tv&categories[]=movies")]
        [TestCase("{{ range $i, $e := .Categories}}&categories[{{$i}}]={{.}}{{end}}", "&categories[0]=tv&categories[1]=movies")]
        [TestCase("{{ range $index, $element := .Categories}}&categories[{{$index}}]={{.}}+postIndex[{{$index}}]{{end}}", "&categories[0]=tv+postIndex[0]&categories[1]=movies+postIndex[1]")]
        public void should_handle_range_statements(string template, string expected)
        {
            var result = Subject.ApplyGoTemplateText(template, _variables);

            result.Should().Be(expected);
        }

        [TestCase("{{ re_replace .Query.Keywords \"[^a-zA-Z0-9]+\" \"%\" }}", "abc%def")]
        public void should_handle_re_replace_statements(string template, string expected)
        {
            _variables[".Query.Keywords"] = string.Join(" ", new List<string> { "abc", "def" });

            var result = Subject.ApplyGoTemplateText(template, _variables);

            result.Should().Be(expected);
        }

        [TestCase("{{ join .Categories \", \" }}", "tv, movies")]
        public void should_handle_join_statements(string template, string expected)
        {
            var result = Subject.ApplyGoTemplateText(template, _variables);

            result.Should().Be(expected);
        }

        [TestCase("{{ .Today.Year }}")]
        public void should_handle_variables_statements(string template)
        {
            var result = Subject.ApplyGoTemplateText(template, _variables);

            result.Should().Be(DateTime.Now.Year.ToString());
        }

        [TestCase("{{if .False }}0{{else}}1{{end}}", "1")]
        [TestCase("{{if .True }}0{{else}}1{{end}}", "0")]
        public void should_handle_if_statements(string template, string expected)
        {
            var result = Subject.ApplyGoTemplateText(template, _variables);

            result.Should().Be(expected);
        }
    }
}
