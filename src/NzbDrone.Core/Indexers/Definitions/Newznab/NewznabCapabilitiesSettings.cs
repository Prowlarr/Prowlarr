using System.Collections.Generic;

namespace NzbDrone.Core.Indexers.Newznab;

public class NewznabCapabilitiesSettings
{
    public bool SupportsRawSearch { get; set; }

    public List<SearchParam> SearchParams { get; set; }

    public List<TvSearchParam> TvSearchParams { get; set; }

    public List<MovieSearchParam> MovieSearchParams { get; set; }

    public List<MusicSearchParam> MusicSearchParams { get; set; }

    public List<BookSearchParam> BookSearchParams { get; set; }

    public List<IndexerCategory> Categories { get; set; }

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
