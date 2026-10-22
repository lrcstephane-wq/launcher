using System.IO;
using System.Windows;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;
using Microsoft.Win32;

namespace Ideo.TopSolidLauncher.Views;

public partial class BulkEditWindow : Window
{
    public bool ChangeGroup => ChangeGroupCheckBox.IsChecked == true;
    public Guid? GroupId => GroupComboBox.SelectedValue is Guid id ? id : null;
    public IReadOnlyList<Guid> TagIdsToAdd => AddTagsList.SelectedItems.Cast<TagRow>().Select(row => row.Id).ToArray();
    public IReadOnlyList<Guid> TagIdsToRemove => RemoveTagsList.SelectedItems.Cast<TagRow>().Select(row => row.Id).ToArray();
    public bool AddQuickAccess => AddAccessCheckBox.IsChecked == true;
    public string QuickAccessLabel => AccessLabelTextBox.Text.Trim();
    public string QuickAccessPath => AccessPathTextBox.Text.Trim().Trim('"');
    public bool ReplacePathRoot => ReplaceRootCheckBox.IsChecked == true;
    public string OldRoot => OldRootTextBox.Text.Trim().Trim('"');
    public string NewRoot => NewRootTextBox.Text.Trim().Trim('"');

    public BulkEditWindow(LauncherCatalog catalog, int selectedCount)
    {
        InitializeComponent();
        WindowThemeService.ApplyDarkTitleBar(this);
        CountText.Text = $"{selectedCount} carte(s) sélectionnée(s). Seules les opérations cochées seront appliquées.";
        GroupComboBox.ItemsSource = catalog.Groups.OrderBy(group => group.SortOrder).ToArray();
        GroupComboBox.SelectedIndex = 0;
        var tags = catalog.Tags.OrderBy(tag => tag.CategorySortOrder).ThenBy(tag => tag.SortOrder).Select(tag => new TagRow(tag.Id, $"{tag.Category} · {tag.Name}")).ToArray();
        AddTagsList.ItemsSource = tags;
        RemoveTagsList.ItemsSource = tags;
    }

    private void BrowseAccess_Click(object sender, RoutedEventArgs e)
    {
        var initial = Environment.ExpandEnvironmentVariables(AccessPathTextBox.Text.Trim().Trim('"'));
        var dialog = new OpenFolderDialog { Title = "Choisir le dossier à ajouter", InitialDirectory = Directory.Exists(initial) ? initial : null };
        if (dialog.ShowDialog(this) == true) AccessPathTextBox.Text = dialog.FolderName;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (ChangeGroup && GroupId is null) { FormMessageText.Text = "Choisissez un groupe."; return; }
        if (TagIdsToAdd.Intersect(TagIdsToRemove).Any()) { FormMessageText.Text = "Un même tag ne peut pas être ajouté et retiré."; return; }
        if (AddQuickAccess && (string.IsNullOrWhiteSpace(QuickAccessLabel) || string.IsNullOrWhiteSpace(QuickAccessPath))) { FormMessageText.Text = "Renseignez le nom et le chemin de l’accès rapide."; return; }
        if (ReplacePathRoot && (string.IsNullOrWhiteSpace(OldRoot) || string.IsNullOrWhiteSpace(NewRoot))) { FormMessageText.Text = "Renseignez l’ancienne et la nouvelle racine."; return; }
        if (!ChangeGroup && TagIdsToAdd.Count == 0 && TagIdsToRemove.Count == 0 && !AddQuickAccess && !ReplacePathRoot) { FormMessageText.Text = "Sélectionnez au moins une opération."; return; }
        DialogResult = true;
    }

    private sealed record TagRow(Guid Id, string Display);
}
