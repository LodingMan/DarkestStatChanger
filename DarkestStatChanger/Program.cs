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
                var ex = e.Exception;
                var code = (ex is DscException dsc) ? dsc.Code : "E900";
                WriteCrashLog(code, ex);
                MessageBox.Show(
                    $"Error {code}\n\n{ex.Message}",
                    $"Unexpected Error ({code})",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                var code = (ex is DscException dsc) ? dsc.Code : "E901";
                WriteCrashLog(code, ex);
            };

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                var code = (ex is DscException dsc) ? dsc.Code : "E902";
                WriteCrashLog(code, ex);
                MessageBox.Show(
                    $"Error {code}\n\n{ex.Message}",
                    $"Startup Error ({code})",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void WriteCrashLog(string code, Exception ex)
        {
            try
            {
                var content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {code}\n" +
                              $"{ex}\n" +
                              $"--- Stack Trace ---\n{ex?.StackTrace}\n";
                System.IO.File.WriteAllText("crash_log.txt", content);
            }
            catch { /* crash log 자체가 실패해도 앱을 종료시키지 않음 */ }
        }
    }
}
