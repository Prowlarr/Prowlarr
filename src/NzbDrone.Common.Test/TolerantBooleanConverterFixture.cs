using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Common.Test
{
    [TestFixture]
    public class TolerantBooleanConverterFixture
    {
        private class Holder
        {
            public bool Value { get; set; }
        }

        private static bool Read(string json)
        {
            var options = STJson.GetSerializerSettings();
            options.Converters.Insert(0, new TolerantBooleanConverter());

            return System.Text.Json.JsonSerializer.Deserialize<Holder>($"{{\"Value\":{json}}}", options).Value;
        }

        [TestCase("\"1\"", true)]
        [TestCase("\"0\"", false)]
        [TestCase("\"true\"", true)]
        [TestCase("\"false\"", false)]
        [TestCase("\"\"", false)]
        [TestCase("null", false)]
        [TestCase("true", true)]
        [TestCase("false", false)]
        [TestCase("1", true)]
        [TestCase("0", false)]
        public void should_read_boolean_in_any_published_shape(string json, bool expected)
        {
            Read(json).Should().Be(expected);
        }
    }
}
