using System.Windows;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.Views;

public sealed class BatchShortcutItem
{
    public string SourcePath { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string TargetPath { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
    public string WorkingDirectory { get; init; } = string.Empty;
    public string IconPath { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool CanImport { get; init; }
    public bool IsSelected { get; set; }
}

public partial class ShortcutBatchImportWindow : Window
{
    private readonly BatchShortcutItem[] _items;
    public IReadOnlyList<BatchShortcutItem> SelectedItems => _items.Where(item => item.CanImport && item.IsSelected).ToArray();

    public ShortcutBatchImportWindow(IEnumerable<string> paths, IEnumerable<string> existingCommands)
    {
        InitializeComponent();
        WindowThemeService.ApplyDarkTitleBar(this);
        var commands = existingCommands.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _items = paths.Distinct(StringComparer.OrdinalIgnoreCase).Select(path => Read(path, commands)).ToArray();
        ItemsList.ItemsSource = _items;
        SummaryText.Text = $"{_items.Count(item => item.CanImport)} raccourci(s) prêt(s) · {_items.Count(item => !item.CanImport)} ignoré(s)";
    }

    private static BatchShortcutItem Read(string path, HashSet<string> existingCommands)
    {
        try
        {
            var data = ShortcutImportService.Read(path);
            var key = $"{data.TargetPath}|{data.Arguments}";
            var duplicate = existingCommands.Contains(key);
            return new BatchShortcutItem
            {
                SourcePath = path, Title = data.Title, TargetPath = data.TargetPath, Arguments = data.Arguments,
                WorkingDirectory = data.WorkingDirectory, IconPath = data.IconPath,
                CanImport = !duplicate && !string.IsNullOrWhiteSpace(data.TargetPath), IsSelected = !duplicate && !string.IsNullOrWhiteSpace(data.TargetPath),
                Status = duplicate ? "Déjà présent" : string.IsNullOrWhiteSpace(data.TargetPath) ? "Cible vide" : "Prêt"
            };
        }
        catch (Exception exception)
        {
            return new BatchShortcutItem { SourcePath = path, Title = System.IO.Path.GetFileNameWithoutExtension(path), Status = exception.Message, CanImport = false };
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItems.Count == 0) { SummaryText.Text = "Sélectionnez au moins un raccourci valide."; return; }
        DialogResult = true;
    }
}
