using System.Windows;
using Tcd.App.Core;

namespace Tcd.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        MainCore.Instance.Initialize();

        var window = new MainWindow();
        window.Show();
    }
}

