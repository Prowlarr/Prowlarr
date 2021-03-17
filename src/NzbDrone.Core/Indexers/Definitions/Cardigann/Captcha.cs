namespace NzbDrone.Core.Indexers.Definitions.Cardigann
{
    public class Captcha
    {
        public string Type { get; set; } = "image";
        public string ContentType { get; set; }
        public byte[] ImageData { get; set; }
    }
}
