using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Speedometer_GTA_5____Windows_From
{
    class Program
    {
        const string processName = "GTA5";
        static Process process;

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            Process process = Process.GetProcessesByName(processName).FirstOrDefault();
            if (process != null)
            {
                SetForegroundWindow(process.MainWindowHandle);
            }
            else
            {
                ;
            }
        }
    }
}