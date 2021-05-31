namespace NzbDrone.Core.IndexerSearch
{
    public class NewznabRequest
    {
        public string t { get; set; }
        public string q { get; set; }
        public string cat { get; set; }
        public string imdbid { get; set; }
        public int? tmdbid { get; set; }
        public string extended { get; set; }
        public int? limit { get; set; }
        public int? offset { get; set; }
        public int? rid { get; set; }
        public int? tvmazeid { get; set; }
        public int? traktid { get; set; }
        public int? tvdbid { get; set; }
        public int? season { get; set; }
        public string ep { get; set; }
        public string album { get; set; }
        public string artist { get; set; }
        public string label { get; set; }
        public string track { get; set; }
        public int? year { get; set; }
        public string genre { get; set; }
        public string author { get; set; }
        public string title { get; set; }
        public string configured { get; set; }
        public string source { get; set; }
        public string host { get; set; }
        public string server { get; set; }
    }
}
