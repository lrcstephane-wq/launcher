using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.Views;

public enum QuickPaletteItemKind { LaunchProfile, ResourceLocation }

public sealed class QuickPaletteItem
{
    public QuickPaletteItemKind Kind { get; init; }
    public Guid ParentId { get; init; }
    public Guid ChildId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Keywords { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = "\uE768";
    public bool IsAvailable { get; init; }
    public string Status => IsAvailable ? string.Empty : "Indisponible";
}

public partial class QuickPaletteWindow : Window
{
    private readonly QuickPaletteItem[] _allItems;
    private readonly ObservableCollection<QuickPaletteItem> _visible = [];
    public QuickPaletteItem? SelectedItem { get; private set; }

    public QuickPaletteWindow(LauncherCatalog catalog)
    {
        InitializeComponent();
        WindowThemeService.ApplyDarkTitleBar(this);
        _allItems = BuildItems(catalog).ToArray();
        ResultsList.ItemsSource = _visible;
        Refresh(string.Empty);
        Loaded += (_, _) => { SearchBox.Focus(); Keyboard.Focus(SearchBox); };
    }

    private static IEnumerable<QuickPaletteItem> BuildItems(LauncherCatalog catalog)
    {
        foreach (var card in catalog.Cards.OrderBy(card => card.SortOrder))
        {
            var tags = string.Join(' ', catalog.Tags.Where(tag => card.TagIds.Contains(tag.Id)).Select(tag => tag.Name));
            foreach (var profile in card.LaunchProfiles.OrderBy(profile => profile.SortOrder))
            {
                var valid = CardValidationService.ValidateProfile(profile).IsValid;
                yield return new QuickPaletteItem
                {
                    Kind = QuickPaletteItemKind.LaunchProfile,
                    ParentId = card.Id,
                    ChildId = profile.Id,
                    Title = card.Title,
                    Subtitle = $"Mode {profile.Name} · {profile.TargetPath}",
                    Keywords = $"{card.Title} {card.Subtitle} {profile.Name} {profile.TargetPath} {profile.Arguments} {tags}",
                    IconGlyph = "\uE768",
                    IsAvailable = valid
                };
            }
        }
        foreach (var resource in catalog.Resources.OrderBy(resource => resource.SortOrder))
        foreach (var location in resource.Locations.OrderBy(location => location.SortOrder))
        {
            yield return new QuickPaletteItem
            {
                Kind = QuickPaletteItemKind.ResourceLocation,
                ParentId = resource.Id,
                ChildId = location.Id,
                Title = resource.Name,
                Subtitle = $"{location.Name} · {location.Target}",
                Keywords = $"{resource.Name} {location.Name} {location.Target}",
                IconGlyph = resource.IconGlyph,
                IsAvailable = ResourceService.Validate(location).IsValid
            };
        }
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => Refresh(SearchBox.Text);
    private void Refresh(string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _visible.Clear();
        foreach (var item in _allItems.Where(item => terms.All(term => item.Keywords.Contains(term, StringComparison.CurrentCultureIgnoreCase))).Take(60)) _visible.Add(item);
        if (_visible.Count > 0) ResultsList.SelectedIndex = 0;
    }

    private void Open_Click(object sender, RoutedEventArgs e) => AcceptSelection();
    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => AcceptSelection();
    private void AcceptSelection()
    {
        if (ResultsList.SelectedItem is not QuickPaletteItem item) return;
        if (!item.IsAvailable) { HintText.Text = "Cet élément est indisponible sur ce poste."; return; }
        SelectedItem = item;
        DialogResult = true;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); e.Handled = true; }
        else if (e.Key == Key.Enter) { AcceptSelection(); e.Handled = true; }
        else if (e.Key is Key.Down or Key.Up)
        {
            var delta = e.Key == Key.Down ? 1 : -1;
            ResultsList.SelectedIndex = Math.Clamp(ResultsList.SelectedIndex + delta, 0, Math.Max(0, _visible.Count - 1));
            ResultsList.ScrollIntoView(ResultsList.SelectedItem);
            e.Handled = true;
        }
    }
}
