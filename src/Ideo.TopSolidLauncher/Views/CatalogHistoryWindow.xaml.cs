using System.Diagnostics;
using System.IO;
using System.Windows;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.Views;

public partial class CatalogHistoryWindow : Window
{
    private readonly string _backupFolder;
    public string? SelectedBackupPath { get; private set; }
    public CatalogHistoryWindow(LauncherCatalog catalog, IReadOnlyList<CatalogBackupInfo> backups, string catalogPath)
    {
        InitializeComponent();
        WindowThemeService.ApplyDarkTitleBar(this);
        _backupFolder = Path.Combine(Path.GetDirectoryName(catalogPath)!, "Backups");
        AuditList.ItemsSource = catalog.AuditTrail.OrderByDescending(entry => entry.TimestampUtc).Select(entry => new AuditRow(entry.Action,
            $"{entry.TimestampUtc.ToLocalTime():dd/MM/yyyy HH:mm} · {entry.UserName} · {entry.MachineName}")).ToArray();
        BackupsList.ItemsSource = backups;
    }
    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (BackupsList.SelectedItem is not CatalogBackupInfo backup) return;
        if (MessageBox.Show($"Restaurer la sauvegarde du {backup.CreatedLocal:dd/MM/yyyy à HH:mm:ss} ?\n\nLe catalogue actuel sera lui-même sauvegardé.", "Restaurer", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        SelectedBackupPath = backup.Path;
        DialogResult = true;
    }
    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_backupFolder);
        Process.Start(new ProcessStartInfo { FileName = _backupFolder, UseShellExecute = true });
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private sealed record AuditRow(string Action, string Details);
}
