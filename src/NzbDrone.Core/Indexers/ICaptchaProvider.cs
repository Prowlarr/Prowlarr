namespace NzbDrone.Core.Indexers
{
    public interface ICaptchaProvider
    {
        string Captcha { get; set; }
    }
}
