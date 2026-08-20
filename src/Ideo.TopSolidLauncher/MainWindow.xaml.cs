using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Ideo.TopSolidLauncher;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshInstallations();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshInstallations();

    private void OpenLog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LogService.OpenLogFolder();
        }
        catch (Exception exception)
        {
            LogService.Write("Impossible d'ouvrir le dossier du journal.", exception);
            MessageBox.Show(exception.Message, "Launcher TopSolid", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RefreshInstallations()
    {
        try
        {
            var installations = TopSolidDiscovery.FindInstallations();
            InstallationsList.ItemsSource = installations;
            LogService.Write($"Détection terminée : {installations.Count} installation(s).");
            foreach (var installation in installations)
                LogService.Write($"Installation détectée : {installation.CommandLine}");
            StatusText.Text = installations.Count switch
            {
                0 => @"Aucune installation détectée dans C:\Missler\V6xx\bin.",
                1 => "1 version de TopSolid détectée.",
                _ => $"{installations.Count} versions de TopSolid détectées."
            };
        }
        catch (Exception exception)
        {
            LogService.Write("Échec de la détection des installations.", exception);
            InstallationsList.ItemsSource = null;
            StatusText.Text = $"Détection impossible : {exception.Message}";
        }
    }

    private void Launch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TopSolidInstallation installation })
            return;

        try
        {
            LogService.Write($"Commande lancée : {installation.CommandLine}");
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = installation.ExecutablePath,
                Arguments = installation.Arguments,
                WorkingDirectory = installation.FolderPath,
                UseShellExecute = true
            });
            LogService.Write(process is null
                ? "La commande a été transmise à Windows sans identifiant de processus."
                : $"Processus démarré avec l'identifiant {process.Id}.");

            // No process uniqueness check: parallel TopSolid instances remain possible.
            WindowState = WindowState.Minimized;
        }
        catch (Exception exception)
        {
            LogService.Write($"Échec du lancement de TopSolid {installation.Version}.", exception);
            MessageBox.Show(
                $"Impossible de démarrer TopSolid {installation.Version}.\n\n{exception.Message}",
                "Erreur de lancement",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
