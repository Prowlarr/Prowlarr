using System.Windows.Forms;
using Prowlarr.Host;

namespace NzbDrone
{
    public class MessageBoxUserAlert : IUserAlert
    {
        public void Alert(string message)
        {
            MessageBox.Show(text: message, buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning, caption: "NzbDrone");
        }
    }
}
