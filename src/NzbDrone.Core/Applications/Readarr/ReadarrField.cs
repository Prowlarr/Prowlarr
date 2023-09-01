namespace NzbDrone.Core.Applications.Readarr
{
    public class ReadarrField
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public string Type { get; set; }
        public bool Advanced { get; set; }
        public string Section { get; set; }
        public string Hidden { get; set; }

        public ReadarrField Clone()
        {
            return (ReadarrField)MemberwiseClone();
        }
    }
}
