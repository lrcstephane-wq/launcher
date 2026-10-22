using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;
using Ideo.TopSolidLauncher.ViewModels;
using Ideo.TopSolidLauncher.Views;
using Microsoft.Win32;

namespace Ideo.TopSolidLauncher;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly UserSettings _settings;
    private readonly CatalogService _catalogService;
    private readonly CardLauncherService _launcherService = new();
    private readonly MainViewModel _viewModel;
    private UpdateInfo? _availableUpdate;
    private Point _dragStart;
    private readonly DispatcherTimer _catalogMonitorTimer;
    private bool _catalogReloadPending;
    private string? _unavailableSharedCatalogPath;

    public MainWindow()
    {
        InitializeComponent();
        WindowThemeService.ApplyDarkTitleBar(this);
        _settings = _settingsService.Load();
        _catalogService = new CatalogService(_settings.CatalogPath);

        LauncherCatalog catalog;
        try
        {
            catalog = _catalogService.LoadOrCreate();
        }
        catch (Exception exception)
        {
            LogService.Write("Le catalogue configuré est illisible. Repli temporaire sur le catalogue local.", exception);
            _unavailableSharedCatalogPath = string.IsNullOrWhiteSpace(_settings.CatalogPath) ? null : _settings.CatalogPath;
            _catalogService.UseCatalog(AppPaths.DefaultCatalogPath);
            catalog = _catalogService.LoadOrCreate();
            MessageBox.Show(
                _unavailableSharedCatalogPath is null
                    ? "Le catalogue local ne peut pas être ouvert. Un nouveau catalogue local a été chargé.\n\n" + exception.Message
                    : "Le catalogue partagé est temporairement indisponible. Le catalogue local est utilisé pour cette session, sans oublier le chemin réseau.\n\n" + exception.Message,
                "Catalogue indisponible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _viewModel = new MainViewModel(catalog, _settings, _catalogService, _settingsService);
        DataContext = _viewModel;
        Width = Math.Max(MinWidth, _settings.WindowWidth);
        Height = Math.Max(MinHeight, _settings.WindowHeight);
        if (_settings.WindowLeft is { } left && _settings.WindowTop is { } top &&
            left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100 &&
            top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 100 &&
            left + Width > SystemParameters.VirtualScreenLeft + 100 &&
            top + Height > SystemParameters.VirtualScreenTop + 100)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }
        if (_settings.IsMaximized)
            WindowState = WindowState.Maximized;
        VersionText.Text = $"Version {UpdateService.CurrentVersion.ToString(3)}";
        UpdateCatalogLocation();
        ApplyFilterPaneState();
        UpdateCurrentDate();
        _catalogMonitorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _catalogMonitorTimer.Tick += CatalogMonitorTimer_Tick;
        Activated += MainWindow_Activated;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _catalogMonitorTimer.Start();
        await CheckForUpdateAsync(showUpToDateMessage: false);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _catalogMonitorTimer.Stop();
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;
        _settings.WindowWidth = bounds.Width;
        _settings.WindowHeight = bounds.Height;
        _settings.WindowLeft = bounds.Left;
        _settings.WindowTop = bounds.Top;
        _settings.IsMaximized = WindowState == WindowState.Maximized;
        _settingsService.Save(_settings);
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e) => _viewModel.SearchText = string.Empty;

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearFilters();
        _viewModel.StatusMessage = "Filtres réinitialisés";
    }

    #region Cartes et lancement

    private void NewCard_Click(object sender, RoutedEventArgs e) => AddCard(_viewModel.Catalog.Groups.OrderBy(group => group.SortOrder).First().Id);

    private void NewCardInGroup_Click(object sender, RoutedEventArgs e)
    {
        if (Context<GroupViewModel>(sender) is { } group)
            AddCard(group.Model.Id);
    }

    private void AddCard(Guid groupId)
    {
        var card = new LauncherCard
        {
            GroupId = groupId,
            SortOrder = _viewModel.Catalog.Cards.Count(item => item.GroupId == groupId),
            MinimizeAfterLaunch = true
        };
        var dialog = new CardEditorWindow(_viewModel.Catalog, card, isNew: true) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;
        PrepareCard(dialog.Result);
        _viewModel.Catalog.Cards.Add(dialog.Result);
        SaveCatalog($"« {dialog.Result.Title} » a été ajouté");
    }

    private void EditCard_Click(object sender, RoutedEventArgs e)
    {
        if (Context<CardViewModel>(sender) is not { } card)
            return;
        EditCard(card.Model);
    }

    private void EditCard(LauncherCard card)
    {
        var dialog = new CardEditorWindow(_viewModel.Catalog, card, isNew: false) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;
        PrepareCard(dialog.Result);
        var index = _viewModel.Catalog.Cards.FindIndex(item => item.Id == card.Id);
        if (index >= 0)
            _viewModel.Catalog.Cards[index] = dialog.Result;
        SaveCatalog($"« {dialog.Result.Title} » a été modifié");
    }

    private void DuplicateCard_Click(object sender, RoutedEventArgs e)
    {
        if (Context<CardViewModel>(sender) is not { } source)
            return;
        var copy = source.Model.Clone();
        copy.Id = Guid.NewGuid();
        var profileIds = copy.LaunchProfiles.ToDictionary(profile => profile.Id, _ => Guid.NewGuid());
        foreach (var profile in copy.LaunchProfiles) profile.Id = profileIds[profile.Id];
        copy.DefaultLaunchProfileId = profileIds.TryGetValue(copy.DefaultLaunchProfileId, out var defaultId)
            ? defaultId : copy.LaunchProfiles.First().Id;
        copy.Title += " — copie";
        copy.SortOrder = _viewModel.Catalog.Cards.Count(card => card.GroupId == copy.GroupId);
        var dialog = new CardEditorWindow(_viewModel.Catalog, copy, isNew: false) { Owner = this, Title = "Dupliquer un raccourci" };
        if (dialog.ShowDialog() != true)
            return;
        PrepareCard(dialog.Result);
        _viewModel.Catalog.Cards.Add(dialog.Result);
        SaveCatalog($"« {dialog.Result.Title} » a été créé");
    }

    private void DeleteCard_Click(object sender, RoutedEventArgs e)
    {
        if (Context<CardViewModel>(sender) is not { } card)
            return;
        if (MessageBox.Show($"Supprimer définitivement « {card.Title} » ?", "Supprimer le raccourci",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        _viewModel.Catalog.Cards.RemoveAll(item => item.Id == card.Model.Id);
        _settings.FavoriteCardIds.Remove(card.Model.Id);
        _settings.RecentCardIds.Remove(card.Model.Id);
        _settings.LastLaunchProfileIds.Remove(card.Model.Id);
        ReindexCards(card.Model.GroupId);
        SaveCatalog($"« {card.Title} » a été supprimé");
    }

    private void LaunchCard_Click(object sender, RoutedEventArgs e)
    {
        if (Context<CardViewModel>(sender) is not { } card)
            return;
        try
        {
            LaunchCard(card, card.ActiveProfile);
        }
        catch (Exception exception)
        {
            HandleLaunchError(card.Title, exception);
        }
    }

    private void LaunchMode_Click(object sender, RoutedEventArgs e)
    {
        if (Context<LaunchProfileViewModel>(sender) is not { } profile) return;
        var card = _viewModel.Catalog.Cards.FirstOrDefault(item => item.LaunchProfiles.Any(candidate => candidate.Id == profile.Model.Id));
        if (card is null) return;
        var cardViewModel = _viewModel.VisibleGroups.SelectMany(group => group.Cards).FirstOrDefault(item => item.Model.Id == card.Id)
                            ?? new CardViewModel(card,
                                _viewModel.Catalog.Tags.Where(tag => card.TagIds.Contains(tag.Id)).ToArray(),
                                _settings.FavoriteCardIds.Contains(card.Id), false,
                                Path.GetDirectoryName(_catalogService.CatalogPath)!, profile.Model.Id);
        try { LaunchCard(cardViewModel, profile); }
        catch (Exception exception) { HandleLaunchError(card.Title, exception); }
    }

    private void LaunchCard(CardViewModel card, LaunchProfileViewModel profile)
    {
        var process = _launcherService.Launch(card.Model, profile.Model);
            LogService.Write(process is null
                ? "La commande a été transmise à Windows sans identifiant de processus."
                : $"Processus démarré avec l'identifiant {process.Id}.");
            _viewModel.RecordLaunch(card.Model, profile.Model);
            _viewModel.StatusMessage = $"« {card.Title} » a été lancé avec « {profile.Name} »";
            if (profile.Model.MinimizeAfterLaunch)
                WindowState = WindowState.Minimized;
    }

    private static void HandleLaunchError(string title, Exception exception)
    {
        LogService.Write($"Échec du lancement de « {title} ».", exception);
        MessageBox.Show($"Impossible de lancer « {title} ».\n\n{exception.Message}",
            "Erreur de lancement", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void Favorite_Click(object sender, RoutedEventArgs e)
    {
        if (Context<CardViewModel>(sender) is { } card)
            _viewModel.ToggleFavorite(card);
    }

    private void OpenQuickAccess_Click(object sender, RoutedEventArgs e)
    {
        if (Context<QuickAccessViewModel>(sender) is { } access)
            OpenQuickAccess(access);
    }

    private void OpenQuickAccessMenu_Click(object sender, RoutedEventArgs e)
    {
        if (Context<QuickAccessViewModel>(sender) is { } access)
            OpenQuickAccess(access);
    }

    private void CopyQuickAccess_Click(object sender, RoutedEventArgs e)
    {
        if (Context<QuickAccessViewModel>(sender) is not { } access)
            return;
        try
        {
            Clipboard.SetText(access.ResolvedPath);
            _viewModel.StatusMessage = $"Chemin « {access.Label} » copié";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Copie impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenQuickAccess(QuickAccessViewModel access)
    {
        try
        {
            var path = access.ResolvedPath;
            if (!Directory.Exists(path))
            {
                MessageBox.Show(
                    $"Le dossier « {access.Label} » est introuvable ou inaccessible.\n\n{path}",
                    "Dossier inaccessible", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Dossier inaccessible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenResource_Click(object sender, RoutedEventArgs e)
    {
        if (Context<ResourceViewModel>(sender) is not { } resource) return;
        if (resource.ActiveLocation is null || !resource.ActiveLocation.IsAvailable)
        {
            ManageResources_Click(sender, e);
            return;
        }
        OpenResource(resource.Model, resource.ActiveLocation.Model);
    }

    private void OpenResourceLocation_Click(object sender, RoutedEventArgs e)
    {
        if (Context<ResourceLocationViewModel>(sender) is not { } location) return;
        var resource = _viewModel.Catalog.Resources.FirstOrDefault(item => item.Locations.Any(candidate => candidate.Id == location.Model.Id));
        if (resource is not null) OpenResource(resource, location.Model);
    }

    private void OpenResource(LauncherResource resource, LauncherResourceLocation location)
    {
        try
        {
            ResourceService.Open(resource, location);
            _viewModel.RecordResourceOpen(resource, location);
            _viewModel.StatusMessage = $"Ressource « {resource.Name} » ouverte depuis « {location.Name} »";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Ressource inaccessible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    #endregion

    #region Tags, détection et groupes

    private void ManageTags_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new TagManagerWindow(_viewModel.Catalog) { Owner = this };
        dialog.ShowDialog();
        if (dialog.HasChanges)
            SaveCatalog("Les tags ont été mis à jour");
    }

    private void ManageResources_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ResourceManagerWindow(_viewModel.Catalog) { Owner = this };
        dialog.ShowDialog();
        if (dialog.HasChanges) SaveCatalog("Les ressources partagées ont été mises à jour");
    }

    private void DetectTopSolid_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var installations = TopSolidDiscovery.FindInstallations();
            var missing = installations.Where(installation => !_viewModel.Catalog.Cards.Any(card => card.LaunchProfiles.Any(profile =>
                string.Equals(Environment.ExpandEnvironmentVariables(profile.TargetPath), installation.ExecutablePath,
                    StringComparison.OrdinalIgnoreCase)))).ToArray();
            if (missing.Length == 0)
            {
                MessageBox.Show("Toutes les versions détectées possèdent déjà une carte.", "Détection TopSolid",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show($"{missing.Length} nouvelle(s) version(s) détectée(s). Créer les cartes correspondantes ?",
                    "Détection TopSolid", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var group = _viewModel.Catalog.Groups.FirstOrDefault(item => item.Name.Equals("Environnements Idéo", StringComparison.OrdinalIgnoreCase))
                        ?? _viewModel.Catalog.Groups.OrderBy(item => item.SortOrder).First();
            var workTag = EnsureTag("Travail", "Usage", "#2E69B3");
            var appTag = EnsureTag("TopSolid'Wood", "Application", "#52667D");
            var yearTag = EnsureTag("2021", "Année", "#6B5BA7");
            foreach (var installation in missing)
            {
                var versionTag = EnsureTag(installation.Version, "Version", "#C98247");
                var profile = new LauncherLaunchProfile
                {
                    TargetPath = installation.ExecutablePath,
                    Arguments = installation.Arguments,
                    WorkingDirectory = installation.FolderPath,
                    MinimizeAfterLaunch = true
                };
                _viewModel.Catalog.Cards.Add(new LauncherCard
                {
                    Title = $"TopSolid'Wood {installation.Version}",
                    Subtitle = "Base de travail",
                    GroupId = group.Id,
                    DefaultLaunchProfileId = profile.Id,
                    LaunchProfiles = [profile],
                    AccentColor = "#2E69B3",
                    TagIds = [workTag.Id, appTag.Id, yearTag.Id, versionTag.Id],
                    SortOrder = _viewModel.Catalog.Cards.Count(card => card.GroupId == group.Id)
                });
            }
            SaveCatalog($"{missing.Length} carte(s) TopSolid ont été ajoutées");
        }
        catch (Exception exception)
        {
            LogService.Write("La détection TopSolid a échoué.", exception);
            MessageBox.Show(exception.Message, "Détection impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AddGroup_Click(object sender, RoutedEventArgs e)
    {
        var prompt = new PromptWindow("Nouveau groupe", "Nom du groupe") { Owner = this };
        if (prompt.ShowDialog() != true)
            return;
        _viewModel.Catalog.Groups.Add(new LauncherGroup
        {
            Name = prompt.Value,
            SortOrder = _viewModel.Catalog.Groups.Count
        });
        SaveCatalog($"Le groupe « {prompt.Value} » a été créé");
    }

    private void RenameGroup_Click(object sender, RoutedEventArgs e)
    {
        if (Context<GroupViewModel>(sender) is not { } group)
            return;
        var prompt = new PromptWindow("Renommer le groupe", "Nouveau nom", group.Name) { Owner = this };
        if (prompt.ShowDialog() != true)
            return;
        group.Model.Name = prompt.Value;
        SaveCatalog($"Le groupe est maintenant nommé « {prompt.Value} »");
    }

    private void DeleteGroup_Click(object sender, RoutedEventArgs e)
    {
        if (Context<GroupViewModel>(sender) is not { } group)
            return;
        if (_viewModel.Catalog.Groups.Count == 1)
        {
            MessageBox.Show("Le catalogue doit conserver au moins un groupe.", "Supprimer le groupe",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var destination = _viewModel.Catalog.Groups.OrderBy(item => item.SortOrder).First(item => item.Id != group.Model.Id);
        var message = group.Cards.Count == 0
            ? $"Supprimer le groupe « {group.Name} » ?"
            : $"Supprimer le groupe « {group.Name} » et déplacer ses cartes vers « {destination.Name} » ?";
        if (MessageBox.Show(message, "Supprimer le groupe", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        foreach (var card in _viewModel.Catalog.Cards.Where(card => card.GroupId == group.Model.Id))
        {
            card.GroupId = destination.Id;
            card.SortOrder = _viewModel.Catalog.Cards.Count(item => item.GroupId == destination.Id);
        }
        _viewModel.Catalog.Groups.Remove(group.Model);
        ReindexGroups();
        ReindexCards(destination.Id);
        SaveCatalog($"Le groupe « {group.Name} » a été supprimé");
    }

    private void MoveGroupUp_Click(object sender, RoutedEventArgs e) => MoveGroup(Context<GroupViewModel>(sender), -1);
    private void MoveGroupDown_Click(object sender, RoutedEventArgs e) => MoveGroup(Context<GroupViewModel>(sender), 1);

    private void MoveGroup(GroupViewModel? group, int direction)
    {
        if (group is null) return;
        var ordered = _viewModel.Catalog.Groups.OrderBy(item => item.SortOrder).ToList();
        var index = ordered.FindIndex(item => item.Id == group.Model.Id);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= ordered.Count) return;
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var i = 0; i < ordered.Count; i++) ordered[i].SortOrder = i;
        SaveCatalog("L'ordre des groupes a été modifié");
    }

    private void ToggleGroupCollapse_Click(object sender, RoutedEventArgs e)
    {
        if (Context<GroupViewModel>(sender) is not { } group)
            return;
        group.IsCollapsed = !group.IsCollapsed;
        _settings.CollapsedGroupIds.Remove(group.Model.Id);
        if (group.IsCollapsed)
            _settings.CollapsedGroupIds.Add(group.Model.Id);
        _settingsService.Save(_settings);
    }

    #endregion

    #region Ordre des cartes et vues

    private void MoveCardUp_Click(object sender, RoutedEventArgs e) => MoveCard(Context<CardViewModel>(sender), -1);
    private void MoveCardDown_Click(object sender, RoutedEventArgs e) => MoveCard(Context<CardViewModel>(sender), 1);

    private void MoveCard(CardViewModel? card, int direction)
    {
        if (card is null) return;
        var ordered = _viewModel.Catalog.Cards.Where(item => item.GroupId == card.Model.GroupId)
            .OrderBy(item => item.SortOrder).ToList();
        var index = ordered.FindIndex(item => item.Id == card.Model.Id);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= ordered.Count) return;
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var i = 0; i < ordered.Count; i++) ordered[i].SortOrder = i;
        SaveCatalog("L'ordre des cartes a été modifié");
    }

    private void SaveView_Click(object sender, RoutedEventArgs e)
    {
        var prompt = new PromptWindow("Enregistrer la vue", "Nom de cette combinaison de recherche et de filtres") { Owner = this };
        if (prompt.ShowDialog() != true)
            return;
        _viewModel.SaveCurrentView(prompt.Value);
        _viewModel.StatusMessage = $"La vue « {prompt.Value} » a été enregistrée";
    }

    private void ApplySavedView_Click(object sender, RoutedEventArgs e)
    {
        if (Context<SavedView>(sender) is { } view)
            _viewModel.ApplySavedView(view);
    }

    private void DeleteSavedView_Click(object sender, RoutedEventArgs e)
    {
        if (Context<SavedView>(sender) is { } view)
            _viewModel.DeleteSavedView(view);
    }

    #endregion

    #region Menus et catalogue

    private void OpenSettingsMenu_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsButton.ContextMenu is not { } menu) return;
        foreach (var item in menu.Items.OfType<MenuItem>())
            if (item.Header?.ToString() == "Vue compacte") item.IsChecked = _viewModel.CompactMode;
        menu.PlacementTarget = SettingsButton;
        menu.IsOpen = true;
    }

    private void OpenCatalogMenu_Click(object sender, RoutedEventArgs e)
    {
        if (CatalogStatusButton.ContextMenu is not { } menu) return;
        menu.PlacementTarget = CatalogStatusButton;
        menu.IsOpen = true;
    }

    private void ToggleCompact_Click(object sender, RoutedEventArgs e) =>
        _viewModel.CompactMode = !_viewModel.CompactMode;

    private void ToggleFilterPane_Click(object sender, RoutedEventArgs e)
    {
        _settings.IsFilterPaneCollapsed = !_settings.IsFilterPaneCollapsed;
        ApplyFilterPaneState();
        _settingsService.Save(_settings);
    }

    private void ApplyFilterPaneState()
    {
        var collapsed = _settings.IsFilterPaneCollapsed;
        FilterColumn.Width = new GridLength(collapsed ? 46 : 272);
        FilterContent.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        FilterPaneToggleButton.Content = collapsed ? "›" : "‹";
        FilterPaneToggleButton.ToolTip = collapsed ? "Afficher les filtres" : "Masquer les filtres";
        FilterPaneToggleButton.Margin = collapsed ? new Thickness(0, 18, 8, 0) : new Thickness(0, 18, 8, 0);
    }

    private void OpenCardMenu_Click(object sender, RoutedEventArgs e) => OpenButtonMenu(sender);
    private void OpenGroupMenu_Click(object sender, RoutedEventArgs e) => OpenButtonMenu(sender);
    private void OpenLaunchModeMenu_Click(object sender, RoutedEventArgs e) => OpenButtonMenu(sender);
    private void OpenResourceMenu_Click(object sender, RoutedEventArgs e) => OpenButtonMenu(sender);

    private static void OpenButtonMenu(object sender)
    {
        if (sender is not Button { ContextMenu: { } menu } button) return;
        menu.PlacementTarget = button;
        menu.IsOpen = true;
    }

    private void ChooseCatalog_Click(object sender, RoutedEventArgs e)
    {
        var choice = MessageBox.Show(
            "Oui : utiliser un catalogue JSON existant.\nNon : créer une copie du catalogue actuel dans un nouvel emplacement.",
            "Catalogue partagé", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (choice == MessageBoxResult.Cancel) return;

        string? path;
        if (choice == MessageBoxResult.Yes)
        {
            var dialog = new OpenFileDialog { Title = "Choisir le catalogue partagé", Filter = "Catalogue Idéo|*.json", CheckFileExists = true };
            path = dialog.ShowDialog(this) == true ? dialog.FileName : null;
        }
        else
        {
            var dialog = new SaveFileDialog { Title = "Créer le catalogue partagé", Filter = "Catalogue Idéo|*.json", FileName = "catalog.json" };
            path = dialog.ShowDialog(this) == true ? dialog.FileName : null;
        }
        if (path is null) return;

        try
        {
            LauncherCatalog catalog;
            if (choice == MessageBoxResult.Yes)
            {
                _catalogService.UseCatalog(path);
                catalog = _catalogService.LoadOrCreate();
            }
            else
            {
                _catalogService.CreateCopyAt(path, _viewModel.Catalog);
                _catalogService.UseCatalog(path);
                catalog = _catalogService.LoadOrCreate();
            }
            _settings.CatalogPath = path;
            _settingsService.Save(_settings);
            _unavailableSharedCatalogPath = null;
            _viewModel.Rebuild(catalog);
            UpdateCatalogLocation();
            _viewModel.StatusMessage = "Le catalogue partagé est actif";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Catalogue inaccessible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UseLocalCatalog_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_settings.CatalogPath) && _unavailableSharedCatalogPath is null)
        {
            _viewModel.StatusMessage = "Le catalogue local est déjà actif";
            return;
        }

        if (MessageBox.Show(
                "Revenir au catalogue local de ce poste ? Le catalogue partagé ne sera pas supprimé.",
                "Catalogue local", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            _catalogService.UseCatalog(AppPaths.DefaultCatalogPath);
            var catalog = _catalogService.LoadOrCreate();
            _settings.CatalogPath = string.Empty;
            _settingsService.Save(_settings);
            _unavailableSharedCatalogPath = null;
            _viewModel.Rebuild(catalog);
            UpdateCatalogLocation();
            _viewModel.StatusMessage = "Le catalogue local est actif";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Catalogue local inaccessible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ImportCatalog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Importer un catalogue", Filter = "Catalogue Idéo|*.json", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        ImportCatalogFrom(dialog.FileName, "Importer le catalogue");
    }

    private void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        var backupFolder = Path.Combine(Path.GetDirectoryName(_catalogService.CatalogPath)!, "Backups");
        var dialog = new OpenFileDialog
        {
            Title = "Restaurer une sauvegarde du catalogue",
            Filter = "Sauvegardes du catalogue|catalog-*.json|Fichiers JSON|*.json",
            CheckFileExists = true,
            InitialDirectory = Directory.Exists(backupFolder) ? backupFolder : Path.GetDirectoryName(_catalogService.CatalogPath)
        };
        if (dialog.ShowDialog(this) != true) return;
        ImportCatalogFrom(dialog.FileName, "Restaurer la sauvegarde");
    }

    private void ImportCatalogFrom(string path, string title)
    {
        try
        {
            var imported = _catalogService.Import(path);
            if (MessageBox.Show($"Remplacer le catalogue actuel par {imported.Cards.Count} carte(s) importée(s) ?\n\nUne sauvegarde automatique sera créée.",
                    title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            _catalogService.Save(imported);
            _viewModel.Rebuild(imported);
            _viewModel.StatusMessage = "Le catalogue a été importé";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Import impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportCatalog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Exporter le catalogue",
            Filter = "Catalogue Idéo|*.json",
            FileName = $"catalog-ideo-{DateTime.Now:yyyyMMdd}.json"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            _catalogService.Export(_viewModel.Catalog, dialog.FileName);
            _viewModel.StatusMessage = "Une copie du catalogue a été exportée";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Export impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenCatalogFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = Path.GetDirectoryName(_catalogService.CatalogPath)!;
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Dossier inaccessible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ReloadCatalog_Click(object sender, RoutedEventArgs e) => ReloadCatalog(showErrors: true);

    private void OpenLog_Click(object sender, RoutedEventArgs e)
    {
        try { LogService.OpenLogFolder(); }
        catch (Exception exception) { MessageBox.Show(exception.Message, "Journal inaccessible", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void OpenTopSolidDownloads_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = DefaultCatalogFactory.TopSolidDownloadsUrl, UseShellExecute = true });
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Page inaccessible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ValidateCatalog_Click(object sender, RoutedEventArgs e)
    {
        var folder = Path.GetDirectoryName(_catalogService.CatalogPath)!;
        var issues = CatalogValidationService.Validate(_viewModel.Catalog, folder);
        var total = _viewModel.Catalog.Cards.Sum(card => card.LaunchProfiles.Count) +
                    _viewModel.Catalog.Resources.Sum(resource => resource.Locations.Count);
        var dialog = new CatalogValidationWindow(issues, total) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.SelectedIssue is null) return;
        if (dialog.SelectedIssue.Kind == CatalogIssueKind.Card)
        {
            var card = _viewModel.Catalog.Cards.FirstOrDefault(item => item.Id == dialog.SelectedIssue.ItemId);
            if (card is not null) EditCard(card);
        }
        else ManageResources_Click(sender, e);
    }

    private void OpenCatalogHistory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CatalogHistoryWindow(_viewModel.Catalog, _catalogService.GetBackups(), _catalogService.CatalogPath) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.SelectedBackupPath is not null)
            ImportCatalogFrom(dialog.SelectedBackupPath, "Restaurer la sauvegarde");
    }

    private void ToggleBulkMode_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsBulkSelectionMode = !_viewModel.IsBulkSelectionMode;
        BulkModeButton.Content = _viewModel.IsBulkSelectionMode ? "Annuler la sélection" : "Sélectionner";
    }

    private void BulkEdit_Click(object sender, RoutedEventArgs e)
    {
        var cards = _viewModel.SelectedCards();
        if (cards.Count == 0)
        {
            _viewModel.StatusMessage = "Sélectionnez au moins une carte";
            return;
        }
        var dialog = new BulkEditWindow(_viewModel.Catalog, cards.Count) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        foreach (var card in cards)
        {
            if (dialog.ChangeGroup && dialog.GroupId is Guid groupId) card.GroupId = groupId;
            foreach (var tagId in dialog.TagIdsToAdd)
                if (!card.TagIds.Contains(tagId)) card.TagIds.Add(tagId);
            card.TagIds.RemoveAll(dialog.TagIdsToRemove.Contains);
            foreach (var profile in card.LaunchProfiles)
            {
                if (dialog.AddQuickAccess && profile.QuickAccessLinks.All(link => !link.Label.Equals(dialog.QuickAccessLabel, StringComparison.CurrentCultureIgnoreCase) || !link.Path.Equals(dialog.QuickAccessPath, StringComparison.OrdinalIgnoreCase)))
                    profile.QuickAccessLinks.Add(new LauncherQuickAccess { Label = dialog.QuickAccessLabel, Path = dialog.QuickAccessPath });
                if (!dialog.ReplacePathRoot) continue;
                profile.TargetPath = ReplaceIgnoreCase(profile.TargetPath, dialog.OldRoot, dialog.NewRoot);
                profile.Arguments = ReplaceIgnoreCase(profile.Arguments, dialog.OldRoot, dialog.NewRoot);
                profile.WorkingDirectory = ReplaceIgnoreCase(profile.WorkingDirectory, dialog.OldRoot, dialog.NewRoot);
                foreach (var access in profile.QuickAccessLinks)
                    access.Path = ReplaceIgnoreCase(access.Path, dialog.OldRoot, dialog.NewRoot);
            }
        }
        foreach (var group in _viewModel.Catalog.Groups) ReindexCards(group.Id);
        _viewModel.IsBulkSelectionMode = false;
        BulkModeButton.Content = "Sélectionner";
        SaveCatalog($"Modification en masse de {cards.Count} carte(s)");
    }

    private static string ReplaceIgnoreCase(string source, string oldValue, string newValue) =>
        string.IsNullOrEmpty(source) || string.IsNullOrEmpty(oldValue) ? source : source.Replace(oldValue, newValue, StringComparison.OrdinalIgnoreCase);

    private void OpenQuickPalette_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new QuickPaletteWindow(_viewModel.Catalog) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.SelectedItem is null) return;
        var item = dialog.SelectedItem;
        if (item.Kind == QuickPaletteItemKind.LaunchProfile)
        {
            var card = _viewModel.Catalog.Cards.First(card => card.Id == item.ParentId);
            var profile = card.LaunchProfiles.First(profile => profile.Id == item.ChildId);
            var cardViewModel = new CardViewModel(card, _viewModel.Catalog.Tags.Where(tag => card.TagIds.Contains(tag.Id)).ToArray(),
                _settings.FavoriteCardIds.Contains(card.Id), false, Path.GetDirectoryName(_catalogService.CatalogPath)!, profile.Id);
            try { LaunchCard(cardViewModel, new LaunchProfileViewModel(profile)); }
            catch (Exception exception) { HandleLaunchError(card.Title, exception); }
        }
        else
        {
            var resource = _viewModel.Catalog.Resources.First(resource => resource.Id == item.ParentId);
            OpenResource(resource, resource.Locations.First(location => location.Id == item.ChildId));
        }
    }

    private void ImportShortcuts_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Choisir les raccourcis Windows", Filter = "Raccourcis Windows|*.lnk", Multiselect = true, CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) ImportShortcutPaths(dialog.FileNames);
    }

    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(CardViewModel))) return;
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void Window_PreviewDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetData(DataFormats.FileDrop) is not string[] paths) return;
        var shortcuts = paths.SelectMany(path => Directory.Exists(path)
            ? Directory.EnumerateFiles(path, "*.lnk", SearchOption.TopDirectoryOnly)
            : path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ? [path] : []).ToArray();
        if (shortcuts.Length > 0) ImportShortcutPaths(shortcuts);
        e.Handled = true;
    }

    private void ImportShortcutPaths(IEnumerable<string> paths)
    {
        var existing = _viewModel.Catalog.Cards.SelectMany(card => card.LaunchProfiles)
            .Select(profile => $"{profile.TargetPath}|{profile.Arguments}");
        var dialog = new ShortcutBatchImportWindow(paths, existing) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var group = _viewModel.Catalog.Groups.OrderBy(item => item.SortOrder).First();
        foreach (var item in dialog.SelectedItems)
        {
            var profile = new LauncherLaunchProfile
            {
                Name = "Par défaut", TargetPath = item.TargetPath, Arguments = item.Arguments,
                WorkingDirectory = item.WorkingDirectory, MinimizeAfterLaunch = true
            };
            var card = new LauncherCard
            {
                Title = item.Title, GroupId = group.Id, LogoPath = item.IconPath,
                DefaultLaunchProfileId = profile.Id, LaunchProfiles = [profile],
                SortOrder = _viewModel.Catalog.Cards.Count(candidate => candidate.GroupId == group.Id)
            };
            PrepareCard(card);
            _viewModel.Catalog.Cards.Add(card);
        }
        SaveCatalog($"Import de {dialog.SelectedItems.Count} raccourci(s) Windows");
    }

    #endregion

    #region Mise à jour automatique

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e) =>
        await CheckForUpdateAsync(showUpToDateMessage: true);

    private async Task CheckForUpdateAsync(bool showUpToDateMessage)
    {
        UpdateButton.IsEnabled = false;
        try
        {
            _viewModel.StatusMessage = "Recherche d'une mise à jour…";
            _availableUpdate ??= await UpdateService.FindUpdateAsync();
            if (_availableUpdate is null)
            {
                _viewModel.StatusMessage = "Le launcher est à jour";
                if (showUpToDateMessage)
                    MessageBox.Show("Le launcher est à jour.", "Mise à jour", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            UpdateButton.Content = $"Installer {_availableUpdate.TagName}";
            _viewModel.StatusMessage = $"Mise à jour {_availableUpdate.TagName} disponible";
            if (!showUpToDateMessage)
                return;

            var answer = MessageBox.Show(
                $"La version {_availableUpdate.TagName.TrimStart('v', 'V')} est disponible.\n\nLa télécharger et l'installer maintenant ?",
                "Mise à jour disponible", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer != MessageBoxResult.Yes) return;

            var progress = new Progress<int>(value => _viewModel.StatusMessage = $"Téléchargement : {value} %");
            await UpdateService.DownloadAndInstallAsync(_availableUpdate, progress);
            LogService.Write("Mise à jour téléchargée. Redémarrage du launcher.");
            Application.Current.Shutdown();
        }
        catch (Exception exception)
        {
            LogService.Write("Échec de la mise à jour.", exception);
            _viewModel.StatusMessage = "La vérification de mise à jour a échoué";
            if (showUpToDateMessage)
                MessageBox.Show($"Impossible de vérifier ou d'installer la mise à jour.\n\n{exception.Message}",
                    "Mise à jour", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            UpdateButton.IsEnabled = true;
        }
    }

    #endregion

    #region Glisser-déposer

    private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
    }

    private void CardDragHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        _dragStart = e.GetPosition(this);

    private void CardDragHandle_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || (sender as FrameworkElement)?.DataContext is not CardViewModel card) return;
        var position = e.GetPosition(this);
        if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        DragDrop.DoDragDrop((DependencyObject)sender, card, DragDropEffects.Move);
        e.Handled = true;
    }

    private void Card_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || sender is not Border { DataContext: CardViewModel card } border)
            return;
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null)
            return;
        var position = e.GetPosition(this);
        if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;
        DragDrop.DoDragDrop(border, card, DragDropEffects.Move);
    }

    private void Card_DragOver(object sender, DragEventArgs e) => SetDragEffect(e);
    private void Group_DragOver(object sender, DragEventArgs e) => SetDragEffect(e);

    private static void SetDragEffect(DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(CardViewModel)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void Card_Drop(object sender, DragEventArgs e)
    {
        if (sender is not Border { DataContext: CardViewModel target } || e.Data.GetData(typeof(CardViewModel)) is not CardViewModel source || source.Model.Id == target.Model.Id)
            return;
        var border = (Border)sender;
        var insertAfter = e.GetPosition(border).X > border.ActualWidth / 2;
        MoveCardTo(source.Model, target.Model.GroupId, target.Model.SortOrder + (insertAfter ? 1 : 0));
        e.Handled = true;
    }

    private void Group_Drop(object sender, DragEventArgs e)
    {
        if (sender is not Border { DataContext: GroupViewModel target } || e.Data.GetData(typeof(CardViewModel)) is not CardViewModel source)
            return;
        var order = _viewModel.Catalog.Cards.Count(card => card.GroupId == target.Model.Id);
        MoveCardTo(source.Model, target.Model.Id, order);
        e.Handled = true;
    }

    private void MoveCardTo(LauncherCard card, Guid destinationGroupId, int destinationOrder)
    {
        var sourceGroupId = card.GroupId;
        var destinationCards = _viewModel.Catalog.Cards
            .Where(item => item.GroupId == destinationGroupId && item.Id != card.Id)
            .OrderBy(item => item.SortOrder).ToList();
        destinationOrder = Math.Clamp(destinationOrder, 0, destinationCards.Count);
        card.GroupId = destinationGroupId;
        destinationCards.Insert(destinationOrder, card);
        for (var i = 0; i < destinationCards.Count; i++) destinationCards[i].SortOrder = i;
        if (sourceGroupId != destinationGroupId) ReindexCards(sourceGroupId);
        SaveCatalog("La carte a été déplacée");
    }

    private void FilterGrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => _dragStart = e.GetPosition(this);

    private void CategoryGrip_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || (sender as FrameworkElement)?.DataContext is not FilterCategoryViewModel category) return;
        if (!PassedDragThreshold(e.GetPosition(this))) return;
        DragDrop.DoDragDrop((DependencyObject)sender, category, DragDropEffects.Move);
        e.Handled = true;
    }

    private void TagGrip_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || (sender as FrameworkElement)?.DataContext is not FilterOptionViewModel option) return;
        if (!PassedDragThreshold(e.GetPosition(this))) return;
        DragDrop.DoDragDrop((DependencyObject)sender, option, DragDropEffects.Move);
        e.Handled = true;
    }

    private bool PassedDragThreshold(Point position) =>
        Math.Abs(position.X - _dragStart.X) >= SystemParameters.MinimumHorizontalDragDistance ||
        Math.Abs(position.Y - _dragStart.Y) >= SystemParameters.MinimumVerticalDragDistance;

    private void FilterCategory_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(FilterCategoryViewModel)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void FilterCategory_Drop(object sender, DragEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not FilterCategoryViewModel target ||
            e.Data.GetData(typeof(FilterCategoryViewModel)) is not FilterCategoryViewModel source) return;
        _viewModel.ReorderCategory(source, target);
        SaveCatalog("L'ordre des catégories de tags a été modifié");
        e.Handled = true;
    }

    private void FilterTag_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(FilterOptionViewModel)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void FilterTag_Drop(object sender, DragEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not FilterOptionViewModel target ||
            e.Data.GetData(typeof(FilterOptionViewModel)) is not FilterOptionViewModel source) return;
        if (_viewModel.ReorderTag(source, target)) SaveCatalog("L'ordre des tags a été modifié");
        e.Handled = true;
    }

    #endregion

    #region Helpers et raccourcis clavier

    private void ReindexCards(Guid groupId)
    {
        var cards = _viewModel.Catalog.Cards.Where(card => card.GroupId == groupId).OrderBy(card => card.SortOrder).ToArray();
        for (var i = 0; i < cards.Length; i++) cards[i].SortOrder = i;
    }

    private void ReindexGroups()
    {
        var groups = _viewModel.Catalog.Groups.OrderBy(group => group.SortOrder).ToArray();
        for (var i = 0; i < groups.Length; i++) groups[i].SortOrder = i;
    }

    private void UpdateCatalogLocation()
    {
        if (_unavailableSharedCatalogPath is not null)
        {
            CatalogLocationText.Text = "Catalogue partagé indisponible";
            CatalogPathText.Text = _unavailableSharedCatalogPath;
            CatalogStatusDot.Fill = new SolidColorBrush(Color.FromRgb(216, 155, 84));
        }
        else
        {
            var isShared = !string.IsNullOrWhiteSpace(_settings.CatalogPath) &&
                           PathsEqual(_settings.CatalogPath, _catalogService.CatalogPath);
            CatalogLocationText.Text = isShared ? "Catalogue partagé" : "Catalogue local";
            CatalogPathText.Text = _catalogService.CatalogPath;
            CatalogStatusDot.Fill = new SolidColorBrush(isShared
                ? Color.FromRgb(77, 139, 212)
                : Color.FromRgb(127, 184, 148));
        }

        CatalogStatusButton.ToolTip = CatalogPathText.Text;
        CatalogPathText.ToolTip = CatalogPathText.Text;
    }

    private bool SaveCatalog(string message)
    {
        try
        {
            _viewModel.SaveCatalog(message);
            return true;
        }
        catch (Exception exception)
        {
            LogService.Write("Impossible d'enregistrer le catalogue.", exception);
            MessageBox.Show(
                $"Les changements n'ont pas été enregistrés. Le catalogue va être rechargé pour éviter d'écraser des données.\n\n{exception.Message}",
                "Enregistrement impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
            ReloadCatalog(showErrors: false);
            return false;
        }
    }

    private void ReloadCatalog(bool showErrors, bool externalChange = false)
    {
        var previousPath = _catalogService.CatalogPath;
        var reconnecting = _unavailableSharedCatalogPath is not null;
        try
        {
            if (reconnecting)
                _catalogService.UseCatalog(_unavailableSharedCatalogPath!);

            var catalog = _catalogService.LoadOrCreate();
            _viewModel.Rebuild(catalog);
            if (reconnecting)
                _unavailableSharedCatalogPath = null;
            _catalogReloadPending = false;
            UpdateCatalogLocation();
            _viewModel.StatusMessage = externalChange
                ? "Le catalogue partagé a été actualisé automatiquement"
                : "Le catalogue a été rechargé";
        }
        catch (Exception exception)
        {
            LogService.Write("Impossible de recharger le catalogue.", exception);
            if (reconnecting)
            {
                _catalogService.UseCatalog(previousPath);
                try { _catalogService.LoadOrCreate(); } catch { /* Le catalogue local déjà chargé reste affiché. */ }
            }
            if (showErrors)
                MessageBox.Show(exception.Message, "Rechargement impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CatalogMonitorTimer_Tick(object? sender, EventArgs e)
    {
        UpdateCurrentDate();
        if (_unavailableSharedCatalogPath is not null || !_catalogService.HasChangedExternally())
            return;
        if (!IsEnabled)
        {
            _catalogReloadPending = true;
            return;
        }
        ReloadCatalog(showErrors: false, externalChange: true);
    }

    private void MainWindow_Activated(object? sender, EventArgs e)
    {
        if (!_catalogReloadPending || !IsEnabled) return;
        ReloadCatalog(showErrors: false, externalChange: true);
    }

    private static bool PathsEqual(string first, string second)
    {
        try
        {
            return string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void UpdateCurrentDate()
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var value = DateTime.Now.ToString("dddd d MMMM yyyy", culture);
        CurrentDateText.Text = culture.TextInfo.ToTitleCase(value);
    }

    private void PrepareCard(LauncherCard card) => card.LogoPath = _catalogService.ImportCardLogo(card);

    private LauncherTag EnsureTag(string name, string category, string color)
    {
        var existing = _viewModel.Catalog.Tags.FirstOrDefault(tag =>
            tag.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            tag.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;
        var tag = new LauncherTag
        {
            Name = name,
            Category = category,
            Color = color,
            CategorySortOrder = _viewModel.Catalog.Tags.FirstOrDefault(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase))?.CategorySortOrder
                                ?? (_viewModel.Catalog.Tags.Select(item => item.CategorySortOrder).DefaultIfEmpty(-1).Max() + 1),
            SortOrder = _viewModel.Catalog.Tags.Count(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
        };
        _viewModel.Catalog.Tags.Add(tag);
        return tag;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.N && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            NewCard_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.K && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            OpenQuickPalette_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            ReloadCatalog(showErrors: true);
            e.Handled = true;
        }
    }

    private static T? Context<T>(object sender) where T : class => (sender as FrameworkElement)?.DataContext as T;

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T result) return result;
            source = source is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(source) : null;
        }
        return null;
    }

    #endregion
}
