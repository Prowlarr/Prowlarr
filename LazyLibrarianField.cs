namespace NzbDrone.Core.Applications.LazyLibrarian
{
    public class LazyLibrarianField
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public string Unit { get; set; }
        public string HelpText { get; set; }
        public string HelpLink { get; set; }
        public object Value { get; set; }
        public string Type { get; set; }
        public bool Advanced { get; set; }
        public string Section { get; set; }
        public string Hidden { get; set; }

        public LazyLibrarianField Clone()
        {
            return (LazyLibrarianField)MemberwiseClone();
        }
    }
}
