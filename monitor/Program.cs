using System;
using System.Windows.Forms;

namespace RDPMonitor
{
    static class Program
    {
        public static string CurrentLanguage = "UA";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Default language is UA, can be changed in UI
            CurrentLanguage = "UA";
            
            Application.Run(new MainForm());
        }
    }
}
