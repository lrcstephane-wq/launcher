using System.Windows;

namespace Ideo.TopSolidLauncher;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (_, eventArgs) =>
        {
            LogService.Write("Erreur WPF non gérée.", eventArgs.Exception);
            MessageBox.Show(
                $"Une erreur inattendue est survenue.\n\nLe journal se trouve ici :\n{LogService.LogPath}",
                "Launcher TopSolid",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            eventArgs.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            LogService.Write("Erreur système non gérée.", eventArgs.ExceptionObject as Exception);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        LogService.Write($"Démarrage du launcher {UpdateService.CurrentVersion.ToString(3)} sur {Environment.OSVersion} (.NET {Environment.Version}).");
        base.OnStartup(e);
    }
}
