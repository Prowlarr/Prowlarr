namespace NzbDrone.Core.Applications.Radarr
{
    public class RadarrField
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public string Type { get; set; }
        public bool Advanced { get; set; }
        public string Section { get; set; }
        public string Hidden { get; set; }

        public RadarrField Clone()
        {
            return (RadarrField)MemberwiseClone();
        }
    }
}
