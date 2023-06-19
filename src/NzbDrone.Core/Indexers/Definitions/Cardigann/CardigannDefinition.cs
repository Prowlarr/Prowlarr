using System;
using System.Collections.Generic;

namespace NzbDrone.Core.Indexers.Definitions.Cardigann
{
    // A Dictionary allowing the same key multiple times
    public class KeyValuePairList : List<KeyValuePair<string, SelectorBlock>>, IDictionary<string, SelectorBlock>
    {
        public SelectorBlock this[string key]
        {
            get => throw new NotImplementedException();

            set => Add(new KeyValuePair<string, SelectorBlock>(key, value));
        }

        public ICollection<string> Keys => throw new NotImplementedException();

        public ICollection<SelectorBlock> Values => throw new NotImplementedException();

        public void Add(string key, SelectorBlock value) => Add(new KeyValuePair<string, SelectorBlock>(key, value));

        public bool ContainsKey(string key) => throw new NotImplementedException();

        public bool Remove(string key) => throw new NotImplementedException();

        public bool TryGetValue(string key, out SelectorBlock value) => throw new NotImplementedException();
    }

    // Cardigann yaml classes
    public class CardigannDefinition
    {
        public CardigannDefinition()
        {
            Legacylinks = new List<string>();
        }

        public string Id { get; set; }
        public List<SettingsField> Settings { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Language { get; set; }
        public string Encoding { get; set; }
        public double? RequestDelay { get; set; }
        public List<string> Links { get; set; }
        public List<string> Legacylinks { get; set; }
        public bool Followredirect { get; set; }
        public bool TestLinkTorrent { get; set; } = true;
        public List<string> Certificates { get; set; }
        public CapabilitiesBlock Caps { get; set; }
        public LoginBlock Login { get; set; }
        public RatioBlock Ratio { get; set; }
        public SearchBlock Search { get; set; }
        public DownloadBlock Download { get; set; }
    }

    public class SettingsField
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Label { get; set; }
        public string Default { get; set; }
        public string[] Defaults { get; set; }
        public Dictionary<string, string> Options { get; set; }
    }

    public class CategorymappingBlock
    {
        public string Id { get; set; }
        public string Cat { get; set; }
        public string Desc { get; set; }
        public bool Default { get; set; }
    }

    public class CapabilitiesBlock
    {
        public Dictionary<string, string> Categories { get; set; }
        public List<CategorymappingBlock> Categorymappings { get; set; }
        public Dictionary<string, List<string>> Modes { get; set; }
        public bool Allowrawsearch { get; set; }
    }

    public class CaptchaBlock
    {
        public string Type { get; set; }
        public string Selector { get; set; }
        public string Image { get => throw new Exception("Deprecated, please use Login.Captcha.Selector instead"); set => throw new Exception("Deprecated, please use login/captcha/selector instead of image"); }
        public string Input { get; set; }
    }

    public class LoginBlock
    {
        public string Path { get; set; }
        public string Submitpath { get; set; }
        public List<string> Cookies { get; set; }
        public string Method { get; set; }
        public string Form { get; set; }
        public bool Selectors { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
        public Dictionary<string, SelectorBlock> Selectorinputs { get; set; }
        public Dictionary<string, SelectorBlock> Getselectorinputs { get; set; }
        public List<ErrorBlock> Error { get; set; }
        public PageTestBlock Test { get; set; }
        public CaptchaBlock Captcha { get; set; }
        public Dictionary<string, List<string>> Headers { get; set; }
    }

    public class ErrorBlock
    {
        public string Path { get; set; }
        public string Selector { get; set; }
        public SelectorBlock Message { get; set; }
    }

    public class SelectorBlock
    {
        public string Selector { get; set; }
        public bool Optional { get; set; }
        public string Default { get; set; }
        public string Text { get; set; }
        public string Attribute { get; set; }
        public string Remove { get; set; }
        public List<FilterBlock> Filters { get; set; }
        public Dictionary<string, string> Case { get; set; }
    }

    public class FilterBlock
    {
        public string Name { get; set; }
        public dynamic Args { get; set; }
    }

    public class PageTestBlock
    {
        public string Path { get; set; }
        public string Selector { get; set; }
    }

    public class RatioBlock : SelectorBlock
    {
        public string Path { get; set; }
    }

    public class SearchBlock
    {
        public string Path { get; set; }
        public List<SearchPathBlock> Paths { get; set; }
        public Dictionary<string, List<string>> Headers { get; set; }
        public List<FilterBlock> Keywordsfilters { get; set; }
        public bool AllowEmptyInputs { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
        public List<ErrorBlock> Error { get; set; }
        public List<FilterBlock> Preprocessingfilters { get; set; }
        public RowsBlock Rows { get; set; }
        public KeyValuePairList Fields { get; set; }
    }

    public class RowsBlock : SelectorBlock
    {
        public int After { get; set; }
        public SelectorBlock Dateheaders { get; set; }
        public SelectorBlock Count { get; set; }
        public bool Multiple { get; set; }
        public bool MissingAttributeEqualsNoResults { get; set; }
    }

    public class SearchPathBlock : RequestBlock
    {
        public List<string> Categories { get; set; }
        public bool Inheritinputs { get; set; } = true;
        public bool Followredirect { get; set; }
        public ResponseBlock Response { get; set; }
    }

    public class RequestBlock
    {
        public string Path { get; set; }
        public string Method { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
        public string Queryseparator { get; set; } = "&";
    }

    public class DownloadBlock
    {
        public List<SelectorField> Selectors { get; set; }
        public string Method { get; set; }
        public BeforeBlock Before { get; set; }
        public InfohashBlock Infohash { get; set; }
        public Dictionary<string, List<string>> Headers { get; set; }
    }

    public class InfohashBlock
    {
        public SelectorField Hash { get; set; }
        public SelectorField Title { get; set; }
        public bool Usebeforeresponse { get; set; }
    }

    public class SelectorField
    {
        public string Selector { get; set; }
        public string Attribute { get; set; }
        public bool Usebeforeresponse { get; set; }
        public List<FilterBlock> Filters { get; set; }
    }

    public class BeforeBlock : RequestBlock
    {
        public SelectorField Pathselector { get; set; }
    }

    public class ResponseBlock
    {
        public string Type { get; set; }
        public string NoResultsMessage { get; set; }
    }
}
