using BenchmarkDotNet.Attributes;
using NzbDrone.Core.Parser;

namespace NzbDrone.Benchmark.Test.ParserTests
{
    [InProcess]
    public class ParseUtilFixture
    {
        [Benchmark]
        [Arguments("123456789")]
        [Arguments("")]
        [Arguments("asd8f7asdf")]
        [Arguments("sdf")]
        public void parse_long_from_string(string dateInput)
        {
            ParseUtil.GetLongFromString(dateInput);
        }
    }
}
