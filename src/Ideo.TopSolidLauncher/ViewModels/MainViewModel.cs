using System.Collections.ObjectModel;
using System.IO;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private string _searchText = string.Empty;
    private string _statusMessage = "Prêt";
    private bool _isBulkSelectionMode;
    private readonly HashSet<Guid> _selectedCardIds = [];

    public LauncherCatalog Catalog { get; private set; }
    public UserSettings Settings { get; }
    public CatalogService CatalogService { get; }
    public SettingsService SettingsService { get; }
    public ObservableCollection<FilterCategoryViewModel> FilterCategories { get; } = [];
    public ObservableCollection<GroupViewModel> VisibleGroups { get; } = [];
    public ObservableCollection<SavedView> SavedViews { get; }
    public ObservableCollection<ResourceViewModel> Resources { get; } = [];

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

    public bool ShowFavoritesOnly
    {
        get => Settings.ShowFavoritesOnly;
        set
        {
            if (Settings.ShowFavoritesOnly == value) return;
            Settings.ShowFavoritesOnly = value;
            OnPropertyChanged();
            SettingsService.Save(Settings);
            RefreshVisibleCards();
        }
    }

    public int VisibleCardCount => VisibleGroups.Sum(group => group.Cards.Count);
    public int TotalCardCount => Catalog.Cards.Count;
    public string CountLabel => $"{VisibleCardCount} sur {TotalCardCount} raccourcis";
    public bool IsBulkSelectionMode
    {
        get => _isBulkSelectionMode;
        set
        {
            if (!SetProperty(ref _isBulkSelectionMode, value)) return;
            if (!value) ClearBulkSelection();
        }
    }
    public int BulkSelectedCount => _selectedCardIds.Count;
    public string BulkSelectionLabel => BulkSelectedCount == 1 ? "1 carte sélectionnée" : $"{BulkSelectedCount} cartes sélectionnées";
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
        SavedViews = new ObservableCollection<SavedView>(settings.SavedViews);
        BuildFilters();
        BuildResources();
        RefreshVisibleCards();
    }

    public void Rebuild(LauncherCatalog? replacement = null)
    {
        if (replacement is not null)
            Catalog = replacement;
        BuildFilters();
        BuildResources();
        RefreshVisibleCards();
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
        ShowFavoritesOnly = false;
        SearchText = string.Empty;
        RefreshVisibleCards();
    }

    public void ApplySavedView(SavedView view)
    {
        foreach (var option in FilterCategories.SelectMany(category => category.Options))
            option.IsSelected = view.SelectedTagIds.Contains(option.Tag.Id);
        ShowFavoritesOnly = view.ShowFavoritesOnly;
        SearchText = view.SearchText;
        RefreshVisibleCards();
    }

    public SavedView SaveCurrentView(string name)
    {
        var view = new SavedView
        {
            Name = name.Trim(),
            SearchText = SearchText,
            SelectedTagIds = SelectedTagIds().ToList(),
            ShowFavoritesOnly = ShowFavoritesOnly
        };
        SavedViews.Add(view);
        PersistSavedViews();
        return view;
    }

    public void DeleteSavedView(SavedView view)
    {
        SavedViews.Remove(view);
        PersistSavedViews();
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

    public void RecordLaunch(LauncherCard card, LauncherLaunchProfile profile)
    {
        Settings.RecentCardIds.Remove(card.Id);
        Settings.RecentCardIds.Insert(0, card.Id);
        if (Settings.RecentCardIds.Count > 20)
            Settings.RecentCardIds.RemoveRange(20, Settings.RecentCardIds.Count - 20);
        Settings.LastLaunchProfileIds[card.Id] = profile.Id;
        SettingsService.Save(Settings);
        RefreshVisibleCards();
    }

    public void RecordResourceOpen(LauncherResource resource, LauncherResourceLocation location)
    {
        Settings.LastResourceLocationIds[resource.Id] = location.Id;
        SettingsService.Save(Settings);
        BuildResources();
    }

    public void SaveCatalog(string message)
    {
        CatalogService.Save(Catalog, auditAction: message);
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
                .Where(card => !ShowFavoritesOnly || Settings.FavoriteCardIds.Contains(card.Id))
                .Where(card => selectedByCategory.All(category => card.TagIds.Any(category.Ids.Contains)))
                .Where(card => MatchesSearch(card, search))
                .Select(card => new CardViewModel(
                    card,
                    Catalog.Tags.Where(tag => card.TagIds.Contains(tag.Id)).OrderBy(tag => tag.CategorySortOrder).ThenBy(tag => tag.SortOrder).ToArray(),
                    Settings.FavoriteCardIds.Contains(card.Id),
                    _selectedCardIds.Contains(card.Id),
                    Path.GetDirectoryName(CatalogService.CatalogPath)!,
                    Settings.LastLaunchProfileIds.TryGetValue(card.Id, out var profileId) ? profileId : null))
                .OrderBy(card => card.Model.SortOrder)
                .ThenBy(card => card.Title)
                .ToArray();

            foreach (var card in cards)
                card.SelectionChanged += Card_SelectionChanged;

            if (cards.Length > 0 || string.IsNullOrEmpty(search) && selectedByCategory.Length == 0)
                VisibleGroups.Add(new GroupViewModel(group, cards, Settings.CollapsedGroupIds.Contains(group.Id)));
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
        var profileText = string.Join(' ', card.LaunchProfiles.Select(profile =>
            $"{profile.Name} {profile.TargetPath} {profile.Arguments} {profile.WorkingDirectory} " +
            string.Join(' ', profile.QuickAccessLinks.Select(link => $"{link.Label} {link.Path}"))));
        var haystack = $"{card.Title} {card.Subtitle} {tagText} {profileText}";
        return haystack.Contains(search, StringComparison.CurrentCultureIgnoreCase);
    }

    private void PersistSavedViews()
    {
        Settings.SavedViews = SavedViews.ToList();
        SettingsService.Save(Settings);
    }

    private void BuildFilters()
    {
        var selectedIds = SelectedTagIds().ToHashSet();
        FilterCategories.Clear();
        foreach (var category in Catalog.Tags
                     .GroupBy(tag => string.IsNullOrWhiteSpace(tag.Category) ? "Autre" : tag.Category)
                     .OrderBy(group => group.Min(tag => tag.CategorySortOrder))
                     .ThenBy(group => group.Key))
        {
            var options = category.OrderBy(tag => tag.SortOrder).ThenBy(tag => tag.Name)
                .Select(tag => new FilterOptionViewModel(tag)).ToArray();
            foreach (var option in options)
            {
                option.IsSelected = selectedIds.Contains(option.Tag.Id);
                option.SelectionChanged += (_, _) => RefreshVisibleCards();
            }
            FilterCategories.Add(new FilterCategoryViewModel(category.Key, category.Min(tag => tag.CategorySortOrder), options));
        }
    }

    public bool ReorderCategory(FilterCategoryViewModel source, FilterCategoryViewModel target, bool insertAfter)
    {
        if (source.Name.Equals(target.Name, StringComparison.CurrentCultureIgnoreCase)) return false;
        var sourceIndex = FilterCategories.IndexOf(source);
        var targetIndex = FilterCategories.IndexOf(target);
        if (sourceIndex < 0 || targetIndex < 0) return false;
        var destinationIndex = targetIndex + (insertAfter ? 1 : 0);
        if (sourceIndex < destinationIndex) destinationIndex--;
        destinationIndex = Math.Clamp(destinationIndex, 0, FilterCategories.Count - 1);
        if (sourceIndex == destinationIndex) return false;
        FilterCategories.Move(sourceIndex, destinationIndex);
        for (var index = 0; index < FilterCategories.Count; index++)
            foreach (var tag in Catalog.Tags.Where(tag => tag.Category.Equals(FilterCategories[index].Name, StringComparison.CurrentCultureIgnoreCase)))
                tag.CategorySortOrder = index;
        return true;
    }

    public bool ReorderTag(FilterOptionViewModel source, FilterOptionViewModel target, bool insertAfter)
    {
        if (!source.Tag.Category.Equals(target.Tag.Category, StringComparison.CurrentCultureIgnoreCase) || source.Tag.Id == target.Tag.Id)
            return false;
        var category = FilterCategories.FirstOrDefault(item => item.Name.Equals(source.Tag.Category, StringComparison.CurrentCultureIgnoreCase));
        if (category is null) return false;
        var sourceIndex = category.Options.IndexOf(source);
        var targetIndex = category.Options.IndexOf(target);
        if (sourceIndex < 0 || targetIndex < 0) return false;
        var destinationIndex = targetIndex + (insertAfter ? 1 : 0);
        if (sourceIndex < destinationIndex) destinationIndex--;
        destinationIndex = Math.Clamp(destinationIndex, 0, category.Options.Count - 1);
        if (sourceIndex == destinationIndex) return false;
        category.Options.Move(sourceIndex, destinationIndex);
        for (var index = 0; index < category.Options.Count; index++)
            category.Options[index].Tag.SortOrder = index;
        return true;
    }

    public bool ReorderCard(CardViewModel source, GroupViewModel destinationGroup, CardViewModel? target, bool insertAfter)
    {
        if (target?.Model.Id == source.Model.Id) return false;

        var sourceGroupId = source.Model.GroupId;
        var sourceGroup = VisibleGroups.FirstOrDefault(group => group.Cards.Any(card => card.Model.Id == source.Model.Id));
        var sourceViewModel = sourceGroup?.Cards.FirstOrDefault(card => card.Model.Id == source.Model.Id) ?? source;
        var currentCards = Catalog.Cards.Where(card => card.GroupId == sourceGroupId)
            .OrderBy(card => card.SortOrder).ThenBy(card => card.Title).ToList();
        var currentIndex = currentCards.FindIndex(card => card.Id == source.Model.Id);

        var destinationCards = Catalog.Cards
            .Where(card => card.GroupId == destinationGroup.Model.Id && card.Id != source.Model.Id)
            .OrderBy(card => card.SortOrder).ThenBy(card => card.Title).ToList();
        var destinationIndex = target is null
            ? destinationCards.Count
            : destinationCards.FindIndex(card => card.Id == target.Model.Id) + (insertAfter ? 1 : 0);
        if (destinationIndex < 0) return false;
        destinationIndex = Math.Clamp(destinationIndex, 0, destinationCards.Count);
        if (sourceGroupId == destinationGroup.Model.Id && currentIndex == destinationIndex) return false;

        source.Model.GroupId = destinationGroup.Model.Id;
        destinationCards.Insert(destinationIndex, source.Model);
        for (var index = 0; index < destinationCards.Count; index++) destinationCards[index].SortOrder = index;
        if (sourceGroupId != destinationGroup.Model.Id)
        {
            var remaining = Catalog.Cards.Where(card => card.GroupId == sourceGroupId)
                .OrderBy(card => card.SortOrder).ThenBy(card => card.Title).ToArray();
            for (var index = 0; index < remaining.Length; index++) remaining[index].SortOrder = index;
        }

        sourceGroup?.Cards.Remove(sourceViewModel);
        var targetViewModel = target is null
            ? null
            : destinationGroup.Cards.FirstOrDefault(card => card.Model.Id == target.Model.Id);
        var visibleIndex = targetViewModel is null
            ? destinationGroup.Cards.Count
            : destinationGroup.Cards.IndexOf(targetViewModel) + (insertAfter ? 1 : 0);
        destinationGroup.Cards.Insert(Math.Clamp(visibleIndex, 0, destinationGroup.Cards.Count), sourceViewModel);
        sourceGroup?.NotifyCardCountChanged();
        if (!ReferenceEquals(sourceGroup, destinationGroup)) destinationGroup.NotifyCardCountChanged();
        return true;
    }

    public IReadOnlyList<LauncherCard> SelectedCards() => Catalog.Cards.Where(card => _selectedCardIds.Contains(card.Id)).ToArray();

    public void ClearBulkSelection()
    {
        _selectedCardIds.Clear();
        foreach (var card in VisibleGroups.SelectMany(group => group.Cards)) card.IsSelected = false;
        NotifyBulkSelection();
    }

    private void Card_SelectionChanged(object? sender, EventArgs e)
    {
        if (sender is not CardViewModel card) return;
        if (card.IsSelected) _selectedCardIds.Add(card.Model.Id);
        else _selectedCardIds.Remove(card.Model.Id);
        NotifyBulkSelection();
    }

    private void NotifyBulkSelection()
    {
        OnPropertyChanged(nameof(BulkSelectedCount));
        OnPropertyChanged(nameof(BulkSelectionLabel));
    }

    private void BuildResources()
    {
        Resources.Clear();
        foreach (var resource in Catalog.Resources.OrderBy(resource => resource.SortOrder).ThenBy(resource => resource.Name))
            Resources.Add(new ResourceViewModel(resource,
                Settings.LastResourceLocationIds.TryGetValue(resource.Id, out var locationId) ? locationId : null));
    }
}
