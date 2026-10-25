using System;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace Modules_Runtime_ER
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, "Modules_Runtime_ER_SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "Modules Runtime is already running.",
                        "Already Running",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                if (!IsRunningAsAdmin())
                {
                    var result = MessageBox.Show(
                        "Modules Runtime is not running as Administrator.",
                        "Administrator Rights Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                        return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Main_Form_ER());
            }
        }

        private static bool IsRunningAsAdmin()
        {
            try
            {
                var identity  = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }
    }
}
