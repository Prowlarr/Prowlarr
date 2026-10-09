using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;

namespace NzbDrone.Core.Indexers.Definitions.BigBbs;

public class BigBbsSettings : UserPassTorrentBaseSettings
{
    [FieldDefinition(4, Label = "Freeleech Only", Type = FieldType.Checkbox, HelpText = "Show freeleech releases only")]
    public bool FreeleechOnly { get; set; }

    [FieldDefinition(5, Type = FieldType.Select, Label = "Sort By", SelectOptions = typeof(BigBbsSortBy), HelpText = "Sort requested from site")]
    public BigBbsSortBy SortBy { get; set; } = BigBbsSortBy.Added;

    [FieldDefinition(6, Type = FieldType.Select, Label = "Sort Order", SelectOptions = typeof(BigBbsSortOrder), HelpText = "Order requested from site")]
    public BigBbsSortOrder SortOrder { get; set; } = BigBbsSortOrder.Desc;
}

public enum BigBbsSortBy
{
    [FieldOption(Label = "Created")]
    Added = 0,

    [FieldOption(Label = "Seeders")]
    Seeders = 1,

    [FieldOption(Label = "Size")]
    Size = 2
}

public enum BigBbsSortOrder
{
    [FieldOption(Label = "Descending")]
    Desc = 0,

    [FieldOption(Label = "Ascending")]
    Asc = 1
}
