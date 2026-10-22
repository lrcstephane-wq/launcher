using System.Windows;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.Views;

public partial class ResourceManagerWindow : Window
{
    private readonly LauncherCatalog _catalog;
    public bool HasChanges { get; private set; }

    public ResourceManagerWindow(LauncherCatalog catalog)
    {
        InitializeComponent();
        WindowThemeService.ApplyDarkTitleBar(this);
        _catalog = catalog;
        Refresh();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var resource = new LauncherResource { SortOrder = _catalog.Resources.Count };
        var dialog = new ResourceEditorWindow(resource, true) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        _catalog.Resources.Add(dialog.Result);
        HasChanges = true;
        Refresh();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ResourceRow row) return;
        var resource = _catalog.Resources.First(item => item.Id == row.Id);
        var dialog = new ResourceEditorWindow(resource, false) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        _catalog.Resources[_catalog.Resources.IndexOf(resource)] = dialog.Result;
        HasChanges = true;
        Refresh();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ResourceRow row) return;
        var resource = _catalog.Resources.First(item => item.Id == row.Id);
        if (MessageBox.Show($"Supprimer la ressource « {resource.Name} » ?", "Supprimer", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _catalog.Resources.Remove(resource);
        Reindex();
        HasChanges = true;
        Refresh();
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) => Move(sender, -1);
    private void MoveDown_Click(object sender, RoutedEventArgs e) => Move(sender, 1);

    private void Move(object sender, int direction)
    {
        if ((sender as FrameworkElement)?.DataContext is not ResourceRow row) return;
        var ordered = _catalog.Resources.OrderBy(item => item.SortOrder).ToList();
        var index = ordered.FindIndex(item => item.Id == row.Id);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= ordered.Count) return;
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var i = 0; i < ordered.Count; i++) ordered[i].SortOrder = i;
        HasChanges = true;
        Refresh();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Reindex()
    {
        var ordered = _catalog.Resources.OrderBy(item => item.SortOrder).ToArray();
        for (var i = 0; i < ordered.Length; i++) ordered[i].SortOrder = i;
    }

    private void Refresh() => ResourcesList.ItemsSource = _catalog.Resources.OrderBy(item => item.SortOrder)
        .Select(item => new ResourceRow(item.Id, item.Name, item.IconGlyph,
            item.Locations.Count == 0 ? "Aucun emplacement" : string.Join("  ·  ", item.Locations.OrderBy(location => location.SortOrder).Select(location => location.Name))))
        .ToArray();

    private sealed record ResourceRow(Guid Id, string Name, string IconGlyph, string LocationSummary);
}
