using System.Collections.ObjectModel;
using System.IO;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private string _searchText = string.Empty;
    private string _statusMessage = "Prêt";

    public LauncherCatalog Catalog { get; private set; }
    public UserSettings Settings { get; }
    public CatalogService CatalogService { get; }
    public SettingsService SettingsService { get; }
    public ObservableCollection<FilterCategoryViewModel> FilterCategories { get; } = [];
    public ObservableCollection<GroupViewModel> VisibleGroups { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                RefreshVisibleCards();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool CompactMode
    {
        get => Settings.CompactMode;
        set
        {
            if (Settings.CompactMode == value)
                return;
            Settings.CompactMode = value;
            OnPropertyChanged();
            SettingsService.Save(Settings);
        }
    }

    public int VisibleCardCount => VisibleGroups.Sum(group => group.Cards.Count);
    public int TotalCardCount => Catalog.Cards.Count;
    public string CountLabel => $"{VisibleCardCount} sur {TotalCardCount} raccourcis";
    public IEnumerable<SavedView> SavedViews => Settings.SavedViews;

    public MainViewModel(
        LauncherCatalog catalog,
        UserSettings settings,
        CatalogService catalogService,
        SettingsService settingsService)
    {
        Catalog = catalog;
        Settings = settings;
        CatalogService = catalogService;
        SettingsService = settingsService;
        BuildFilters();
        RefreshVisibleCards();
    }

    public void Rebuild(LauncherCatalog? replacement = null)
    {
        if (replacement is not null)
            Catalog = replacement;
        BuildFilters();
        RefreshVisibleCards();
        OnPropertyChanged(nameof(SavedViews));
    }

    public IReadOnlyList<Guid> SelectedTagIds() => FilterCategories
        .SelectMany(category => category.Options)
        .Where(option => option.IsSelected)
        .Select(option => option.Tag.Id)
        .ToArray();

    public void ClearFilters()
    {
        foreach (var option in FilterCategories.SelectMany(category => category.Options))
            option.IsSelected = false;
        SearchText = string.Empty;
        RefreshVisibleCards();
    }

    public void ApplySavedView(SavedView view)
    {
        foreach (var option in FilterCategories.SelectMany(category => category.Options))
            option.IsSelected = view.SelectedTagIds.Contains(option.Tag.Id);
        SearchText = view.SearchText;
        RefreshVisibleCards();
    }

    public SavedView SaveCurrentView(string name)
    {
        var view = new SavedView
        {
            Name = name.Trim(),
            SearchText = SearchText,
            SelectedTagIds = SelectedTagIds().ToList()
        };
        Settings.SavedViews.Add(view);
        SettingsService.Save(Settings);
        OnPropertyChanged(nameof(SavedViews));
        return view;
    }

    public void DeleteSavedView(SavedView view)
    {
        Settings.SavedViews.RemoveAll(item => item.Id == view.Id);
        SettingsService.Save(Settings);
        OnPropertyChanged(nameof(SavedViews));
    }

    public void ToggleFavorite(CardViewModel card)
    {
        card.IsFavorite = !card.IsFavorite;
        if (card.IsFavorite)
        {
            if (!Settings.FavoriteCardIds.Contains(card.Model.Id))
                Settings.FavoriteCardIds.Add(card.Model.Id);
        }
        else
        {
            Settings.FavoriteCardIds.Remove(card.Model.Id);
        }
        SettingsService.Save(Settings);
        RefreshVisibleCards();
    }

    public void RecordLaunch(LauncherCard card)
    {
        Settings.RecentCardIds.Remove(card.Id);
        Settings.RecentCardIds.Insert(0, card.Id);
        if (Settings.RecentCardIds.Count > 20)
            Settings.RecentCardIds.RemoveRange(20, Settings.RecentCardIds.Count - 20);
        SettingsService.Save(Settings);
        RefreshVisibleCards();
    }

    public void SaveCatalog(string message)
    {
        CatalogService.Save(Catalog);
        StatusMessage = message;
        Rebuild();
    }

    public void RefreshVisibleCards()
    {
        VisibleGroups.Clear();

        var selectedByCategory = FilterCategories
            .Select(category => new
            {
                category.Name,
                Ids = category.Options.Where(option => option.IsSelected).Select(option => option.Tag.Id).ToHashSet()
            })
            .Where(category => category.Ids.Count > 0)
            .ToArray();

        var search = SearchText.Trim();
        foreach (var group in Catalog.Groups.OrderBy(item => item.SortOrder).ThenBy(item => item.Name))
        {
            var cards = Catalog.Cards
                .Where(card => card.GroupId == group.Id)
                .Where(card => selectedByCategory.All(category => card.TagIds.Any(category.Ids.Contains)))
                .Where(card => MatchesSearch(card, search))
                .Select(card => new CardViewModel(
                    card,
                    Catalog.Tags.Where(tag => card.TagIds.Contains(tag.Id)).OrderBy(tag => tag.SortOrder).ToArray(),
                    Settings.FavoriteCardIds.Contains(card.Id),
                    Path.GetDirectoryName(CatalogService.CatalogPath)!))
                .OrderByDescending(card => card.IsFavorite)
                .ThenBy(card => RecentIndex(card.Model.Id))
                .ThenBy(card => card.Model.SortOrder)
                .ThenBy(card => card.Title)
                .ToArray();

            if (cards.Length > 0 || string.IsNullOrEmpty(search) && selectedByCategory.Length == 0)
                VisibleGroups.Add(new GroupViewModel(group, cards));
        }

        OnPropertyChanged(nameof(VisibleCardCount));
        OnPropertyChanged(nameof(TotalCardCount));
        OnPropertyChanged(nameof(CountLabel));
    }

    private bool MatchesSearch(LauncherCard card, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        var tagText = string.Join(' ', Catalog.Tags.Where(tag => card.TagIds.Contains(tag.Id)).Select(tag => tag.Name));
        var haystack = $"{card.Title} {card.Subtitle} {card.TargetPath} {card.Arguments} {tagText}";
        return haystack.Contains(search, StringComparison.CurrentCultureIgnoreCase);
    }

    private int RecentIndex(Guid cardId)
    {
        var index = Settings.RecentCardIds.IndexOf(cardId);
        return index < 0 ? int.MaxValue : index;
    }

    private void BuildFilters()
    {
        var selectedIds = SelectedTagIds().ToHashSet();
        FilterCategories.Clear();
        foreach (var category in Catalog.Tags
                     .GroupBy(tag => string.IsNullOrWhiteSpace(tag.Category) ? "Autre" : tag.Category)
                     .OrderBy(group => CategoryOrder(group.Key))
                     .ThenBy(group => group.Key))
        {
            var options = category.OrderBy(tag => tag.SortOrder).ThenBy(tag => tag.Name)
                .Select(tag => new FilterOptionViewModel(tag)).ToArray();
            foreach (var option in options)
            {
                option.IsSelected = selectedIds.Contains(option.Tag.Id);
                option.SelectionChanged += (_, _) => RefreshVisibleCards();
            }
            FilterCategories.Add(new FilterCategoryViewModel(category.Key, options));
        }
    }

    private static int CategoryOrder(string category) => category.ToUpperInvariant() switch
    {
        "USAGE" => 0,
        "VERSION" => 1,
        "CHANTS" => 2,
        "ANNÉE" or "ANNEE" => 3,
        "CLIENT" => 4,
        "APPLICATION" => 5,
        _ => 100
    };
}
