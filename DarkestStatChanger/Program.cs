using System;
using System.Windows.Forms;

namespace DarkestStatChanger
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) =>
            {
                System.IO.File.WriteAllText("crash_log.txt",
                    $"{DateTime.Now}\n{e.Exception}\n{e.Exception.StackTrace}");
                MessageBox.Show(e.Exception.ToString(), "Error");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                System.IO.File.WriteAllText("crash_log.txt",
                    $"{DateTime.Now}\n{ex}");
            };
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("crash_log.txt",
                    $"{DateTime.Now}\n{ex}\n{ex.StackTrace}");
                MessageBox.Show(ex.ToString(), "Startup Error");
            }
        }
    }
}
