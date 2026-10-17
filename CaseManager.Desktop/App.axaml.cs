using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CaseManager.Desktop.ViewModels;
using CaseManager.Desktop.Views;
using CaseManager.Core.Crypto;
using CaseManager.Core.Services;
using CaseManager.Core.Storage;
using CaseManager.Core.Models;
using Avalonia.Controls;


namespace CaseManager.Desktop;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var pwdWin = new PasswordWindow();
            pwdWin.Closed += (_, _) =>
            {
                if (pwdWin.Repository == null)
                {
                    desktop.Shutdown();
                    return;
                }

                var service = new CaseService(pwdWin.Repository);
                var vm = new MainWindowViewModel(service);
                var main = new MainWindow { DataContext = vm };
                MainWindow = main;
                main.Show();
            };
            pwdWin.Show();
        }
        base.OnFrameworkInitializationCompleted();
    }
}