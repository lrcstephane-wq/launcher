using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;
using Ideo.TopSolidLauncher.ViewModels;
using Microsoft.Win32;

namespace Ideo.TopSolidLauncher.Views;

public partial class ResourceEditorWindow : Window
{
    private readonly ObservableCollection<LocationEditorItem> _locations;
    private Guid _defaultLocationId;
    public LauncherResource Result { get; }

    public ResourceEditorWindow(LauncherResource resource, bool isNew)
    {
        InitializeComponent();
        WindowThemeService.ApplyDarkTitleBar(this);
        Result = resource.Clone();
        if (isNew) Result.Id = Guid.NewGuid();
        HeadingText.Text = isNew ? "Nouvelle ressource" : "Modifier la ressource";
        NameTextBox.Text = Result.Name;
        var icons = new[] { new IconChoice("Dossier", "\uE8B7"), new IconChoice("Clé / licence", "\uE8D7"), new IconChoice("Lien web", "\uE774"), new IconChoice("Document", "\uE8A5"), new IconChoice("Outil", "\uE90F") };
        IconComboBox.ItemsSource = icons;
        IconComboBox.SelectedValue = Result.IconGlyph;
        if (IconComboBox.SelectedIndex < 0) IconComboBox.SelectedIndex = 0;
        _defaultLocationId = Result.DefaultLocationId;
        _locations = new ObservableCollection<LocationEditorItem>(Result.Locations.OrderBy(item => item.SortOrder).Select(item => new LocationEditorItem(item, item.Id == _defaultLocationId)));
        LocationsItems.ItemsSource = _locations;
    }

    private void AddLocation_Click(object sender, RoutedEventArgs e)
    {
        var location = new LauncherResourceLocation { Name = _locations.Count == 0 ? "Local" : $"Emplacement {_locations.Count + 1}", SortOrder = _locations.Count };
        if (_locations.Count == 0) _defaultLocationId = location.Id;
        _locations.Add(new LocationEditorItem(location, location.Id == _defaultLocationId));
    }

    private void RemoveLocation_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not LocationEditorItem item) return;
        _locations.Remove(item);
        if (_defaultLocationId == item.Id) _defaultLocationId = _locations.FirstOrDefault()?.Id ?? Guid.Empty;
        RefreshDefaults();
    }

    private void SetDefaultLocation_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not LocationEditorItem item) return;
        _defaultLocationId = item.Id;
        RefreshDefaults();
    }

    private void BrowseLocation_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not LocationEditorItem item) return;
        var target = ResourceService.ResolveTarget(item.Target);
        var folderDialog = new OpenFolderDialog { Title = $"Choisir l'emplacement « {item.Name} »", InitialDirectory = Directory.Exists(target) ? target : null };
        if (folderDialog.ShowDialog(this) == true) item.Target = folderDialog.FolderName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text)) { FormMessageText.Text = "Le nom est obligatoire."; return; }
        if (_locations.Count == 0) { FormMessageText.Text = "Ajoutez au moins un emplacement."; return; }
        if (_locations.Any(item => string.IsNullOrWhiteSpace(item.Name))) { FormMessageText.Text = "Chaque emplacement doit avoir un nom."; return; }
        if (_locations.Select(item => item.Name.Trim()).Distinct(StringComparer.CurrentCultureIgnoreCase).Count() != _locations.Count) { FormMessageText.Text = "Chaque emplacement doit avoir un nom différent."; return; }
        Result.Name = NameTextBox.Text.Trim();
        Result.IconGlyph = IconComboBox.SelectedValue?.ToString() ?? "\uE8B7";
        Result.Locations = _locations.Select((item, index) => item.ToModel(index)).ToList();
        Result.DefaultLocationId = Result.Locations.Any(item => item.Id == _defaultLocationId) ? _defaultLocationId : Result.Locations[0].Id;
        DialogResult = true;
    }

    private void RefreshDefaults() { foreach (var item in _locations) item.IsDefault = item.Id == _defaultLocationId; }
    private sealed record IconChoice(string Name, string Glyph);

    public sealed class LocationEditorItem : ObservableObject
    {
        private string _name;
        private string _target;
        private bool _isDefault;
        public Guid Id { get; }
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public string Target { get => _target; set => SetProperty(ref _target, value); }
        public bool IsDefault { get => _isDefault; set => SetProperty(ref _isDefault, value); }
        public LocationEditorItem(LauncherResourceLocation model, bool isDefault) { Id = model.Id; _name = model.Name; _target = model.Target; _isDefault = isDefault; }
        public LauncherResourceLocation ToModel(int order) => new() { Id = Id, Name = Name.Trim(), Target = Target.Trim().Trim('"'), SortOrder = order };
    }
}
