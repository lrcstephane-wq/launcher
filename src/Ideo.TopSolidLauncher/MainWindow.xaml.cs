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

    private void RefreshInstallations()
    {
        try
        {
            var installations = TopSolidDiscovery.FindInstallations();
            InstallationsList.ItemsSource = installations;
            StatusText.Text = installations.Count switch
            {
                0 => @"Aucune installation détectée dans C:\Missler\V6xx\bin.",
                1 => "1 version de TopSolid détectée.",
                _ => $"{installations.Count} versions de TopSolid détectées."
            };
        }
        catch (Exception exception)
        {
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
            Process.Start(new ProcessStartInfo
            {
                FileName = installation.ExecutablePath,
                Arguments = installation.Arguments,
                WorkingDirectory = installation.FolderPath,
                UseShellExecute = true
            });

            // No process uniqueness check: parallel TopSolid instances remain possible.
            WindowState = WindowState.Minimized;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Impossible de démarrer TopSolid {installation.Version}.\n\n{exception.Message}",
                "Erreur de lancement",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
