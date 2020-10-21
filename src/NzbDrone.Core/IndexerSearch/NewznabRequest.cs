namespace NzbDrone.Core.IndexerSearch
{
    public class NewznabRequest
    {
        public int id { get; set; }
        public string t { get; set; }
        public string q { get; set; }
        public string cat { get; set; }
        public string imdbid { get; set; }
        public string tmdbid { get; set; }
        public string extended { get; set; }
        public string limit { get; set; }
        public string offset { get; set; }
        public string rid { get; set; }
        public string tvdbid { get; set; }
        public string season { get; set; }
        public string ep { get; set; }
        public string album { get; set; }
        public string artist { get; set; }
        public string label { get; set; }
        public string track { get; set; }
        public string year { get; set; }
        public string genre { get; set; }
        public string author { get; set; }
        public string title { get; set; }
        public string configured { get; set; }
    }
}
