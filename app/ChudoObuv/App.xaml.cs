using System.Windows;
using ChudoObuv.Tools;

namespace ChudoObuv;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length > 0 && string.Equals(e.Args[0], "--selftest", StringComparison.OrdinalIgnoreCase))
        {
            var report = e.Args.Length > 1
                ? e.Args[1]
                : System.IO.Path.Combine(AppContext.BaseDirectory, "selftest.txt");
            SelfTest.Run(report);
            Shutdown();
            return;
        }

        var window = new MainWindow();
        window.Show();
    }
}
