using System.Windows;
using System.Windows.Controls;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Views;

public partial class TagManagerWindow : Window
{
    private readonly LauncherCatalog _catalog;
    public bool HasChanges { get; private set; }

    public TagManagerWindow(LauncherCatalog catalog)
    {
        InitializeComponent();
        _catalog = catalog;
        RefreshList();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new TagEditorWindow(new LauncherTag { Color = "#2E69B3", SortOrder = _catalog.Tags.Count }, true) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        _catalog.Tags.Add(dialog.Result);
        HasChanges = true;
        RefreshList();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not LauncherTag tag) return;
        var dialog = new TagEditorWindow(tag, false) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        tag.Name = dialog.Result.Name;
        tag.Category = dialog.Result.Category;
        tag.Color = dialog.Result.Color;
        HasChanges = true;
        RefreshList();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not LauncherTag tag) return;
        var count = _catalog.Cards.Count(card => card.TagIds.Contains(tag.Id));
        var message = count == 0
            ? $"Supprimer le tag « {tag.Name} » ?"
            : $"Le tag « {tag.Name} » est utilisé sur {count} carte(s). Le supprimer partout ?";
        if (MessageBox.Show(message, "Supprimer le tag", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        _catalog.Tags.Remove(tag);
        foreach (var card in _catalog.Cards)
            card.TagIds.Remove(tag.Id);
        HasChanges = true;
        RefreshList();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void RefreshList() => TagsList.ItemsSource = _catalog.Tags
        .OrderBy(tag => tag.Category).ThenBy(tag => tag.SortOrder).ThenBy(tag => tag.Name).ToArray();
}
