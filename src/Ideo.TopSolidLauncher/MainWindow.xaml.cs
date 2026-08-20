using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Ideo.TopSolidLauncher;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {UpdateService.CurrentVersion.ToString(3)}";
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshInstallations();
        await CheckForUpdateAsync(showUpToDateMessage: false);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshInstallations();

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e) =>
        await CheckForUpdateAsync(showUpToDateMessage: true);

    private async Task CheckForUpdateAsync(bool showUpToDateMessage)
    {
        UpdateButton.IsEnabled = false;
        try
        {
            LogService.Write("Recherche d'une mise à jour.");
            var update = await UpdateService.FindUpdateAsync();
            if (update is null)
            {
                LogService.Write("Aucune mise à jour disponible.");
                if (showUpToDateMessage)
                    MessageBox.Show("Le launcher est à jour.", "Mise à jour", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            LogService.Write($"Mise à jour disponible : {update.TagName}.");
            var answer = MessageBox.Show(
                $"La version {update.TagName.TrimStart('v', 'V')} est disponible.\n\nLa télécharger et l'installer maintenant ?",
                "Mise à jour disponible",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (answer != MessageBoxResult.Yes)
                return;

            StatusText.Text = "Téléchargement de la mise à jour…";
            var progress = new Progress<int>(value => StatusText.Text = $"Téléchargement de la mise à jour : {value} %");
            await UpdateService.DownloadAndInstallAsync(update, progress);
            LogService.Write("Mise à jour téléchargée. Redémarrage du launcher.");
            Application.Current.Shutdown();
        }
        catch (Exception exception)
        {
            LogService.Write("Échec de la mise à jour.", exception);
            if (showUpToDateMessage)
                MessageBox.Show(
                    $"Impossible de vérifier ou d'installer la mise à jour.\n\n{exception.Message}",
                    "Mise à jour",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
        }
        finally
        {
            UpdateButton.IsEnabled = true;
        }
    }

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
