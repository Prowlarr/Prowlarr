namespace NzbDrone.Core.Applications.Sonarr
{
    public class SonarrField
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public string Type { get; set; }
        public bool Advanced { get; set; }
        public string Section { get; set; }
        public string Hidden { get; set; }

        public SonarrField Clone()
        {
            return (SonarrField)MemberwiseClone();
        }
    }
}
