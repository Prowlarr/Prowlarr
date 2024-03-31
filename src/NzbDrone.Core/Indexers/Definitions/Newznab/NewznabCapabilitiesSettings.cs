using System.Collections.Generic;

namespace NzbDrone.Core.Indexers.Newznab;

public class NewznabCapabilitiesSettings
{
    public bool SupportsRawSearch { get; set; }

    public List<SearchParam> SearchParams { get; set; } = new ();

    public List<TvSearchParam> TvSearchParams { get; set; } = new ();

    public List<MovieSearchParam> MovieSearchParams { get; set; } = new ();

    public List<MusicSearchParam> MusicSearchParams { get; set; } = new ();

    public List<BookSearchParam> BookSearchParams { get; set; } = new ();

    public List<IndexerCategory> Categories { get; set; } = new ();

    public NewznabCapabilitiesSettings()
    {
    }

    public NewznabCapabilitiesSettings(IndexerCapabilities capabilities)
    {
        SupportsRawSearch = capabilities?.SupportsRawSearch ?? false;
        SearchParams = capabilities?.SearchParams;
        TvSearchParams = capabilities?.TvSearchParams;
        MovieSearchParams = capabilities?.MovieSearchParams;
        MusicSearchParams = capabilities?.MusicSearchParams;
        BookSearchParams = capabilities?.BookSearchParams;
        Categories = capabilities?.Categories.GetTorznabCategoryList();
    }
}
