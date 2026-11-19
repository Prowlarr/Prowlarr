namespace NzbDrone.Core.Applications.Sportarr
{
    public class SportarrField
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public SportarrField Clone()
        {
            return (SportarrField)MemberwiseClone();
        }
    }
}
