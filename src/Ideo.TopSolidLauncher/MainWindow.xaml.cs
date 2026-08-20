using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

    public MainWindow()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        _catalogService = new CatalogService(_settings.CatalogPath);

        LauncherCatalog catalog;
        try
        {
            catalog = _catalogService.LoadOrCreate();
        }
        catch (Exception exception)
        {
            LogService.Write("Le catalogue configuré est illisible. Repli sur le catalogue local.", exception);
            MessageBox.Show(
                "Le catalogue configuré ne peut pas être ouvert. Le catalogue local va être utilisé.\n\n" + exception.Message,
                "Catalogue indisponible", MessageBoxButton.OK, MessageBoxImage.Warning);
            _catalogService.UseCatalog(AppPaths.DefaultCatalogPath);
            _settings.CatalogPath = string.Empty;
            _settingsService.Save(_settings);
            catalog = _catalogService.LoadOrCreate();
        }

        _viewModel = new MainViewModel(catalog, _settings, _catalogService, _settingsService);
        DataContext = _viewModel;
        Width = Math.Max(MinWidth, _settings.WindowWidth);
        Height = Math.Max(MinHeight, _settings.WindowHeight);
        VersionText.Text = $"Version {UpdateService.CurrentVersion.ToString(3)}";
        UpdateCatalogLocation();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e) =>
        await CheckForUpdateAsync(showUpToDateMessage: false);

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            _settings.WindowWidth = ActualWidth;
            _settings.WindowHeight = ActualHeight;
        }
        _settingsService.Save(_settings);
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e) => _viewModel.SearchText = string.Empty;

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearFilters();
        _viewModel.StatusMessage = "Filtres réinitialisés";
    }

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
        var dialog = new CardEditorWindow(_viewModel.Catalog, card.Model, isNew: false) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;
        PrepareCard(dialog.Result);
        var index = _viewModel.Catalog.Cards.FindIndex(item => item.Id == card.Model.Id);
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
        ReindexCards(card.Model.GroupId);
        SaveCatalog($"« {card.Title} » a été supprimé");
    }

    private void LaunchCard_Click(object sender, RoutedEventArgs e)
    {
        if (Context<CardViewModel>(sender) is not { } card)
            return;
        try
        {
            var process = _launcherService.Launch(card.Model);
            LogService.Write(process is null
                ? "La commande a été transmise à Windows sans identifiant de processus."
                : $"Processus démarré avec l'identifiant {process.Id}.");
            _viewModel.RecordLaunch(card.Model);
            _viewModel.StatusMessage = $"« {card.Title} » a été lancé";
            if (card.Model.MinimizeAfterLaunch)
                WindowState = WindowState.Minimized;
        }
        catch (Exception exception)
        {
            LogService.Write($"Échec du lancement de « {card.Title} ».", exception);
            MessageBox.Show($"Impossible de lancer « {card.Title} ».\n\n{exception.Message}",
                "Erreur de lancement", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Favorite_Click(object sender, RoutedEventArgs e)
    {
        if (Context<CardViewModel>(sender) is { } card)
            _viewModel.ToggleFavorite(card);
    }

    private void ManageTags_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new TagManagerWindow(_viewModel.Catalog) { Owner = this };
        dialog.ShowDialog();
        if (dialog.HasChanges)
            SaveCatalog("Les tags ont été mis à jour");
    }

    private void DetectTopSolid_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var installations = TopSolidDiscovery.FindInstallations();
            var missing = installations.Where(installation => !_viewModel.Catalog.Cards.Any(card =>
                string.Equals(Environment.ExpandEnvironmentVariables(card.TargetPath), installation.ExecutablePath,
                    StringComparison.OrdinalIgnoreCase))).ToArray();
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
                _viewModel.Catalog.Cards.Add(new LauncherCard
                {
                    Title = $"TopSolid'Wood {installation.Version}",
                    Subtitle = "Base de travail",
                    GroupId = group.Id,
                    TargetPath = installation.ExecutablePath,
                    Arguments = installation.Arguments,
                    WorkingDirectory = installation.FolderPath,
                    AccentColor = "#2E69B3",
                    TagIds = [workTag.Id, appTag.Id, yearTag.Id, versionTag.Id],
                    SortOrder = _viewModel.Catalog.Cards.Count(card => card.GroupId == group.Id),
                    MinimizeAfterLaunch = true
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

    private void OpenSettingsMenu_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsButton.ContextMenu is not { } menu) return;
        foreach (var item in menu.Items.OfType<MenuItem>())
            if (item.Header?.ToString() == "Vue compacte") item.IsChecked = _viewModel.CompactMode;
        menu.PlacementTarget = SettingsButton;
        menu.IsOpen = true;
    }

    private void ToggleCompact_Click(object sender, RoutedEventArgs e) =>
        _viewModel.CompactMode = !_viewModel.CompactMode;

    private void OpenCardMenu_Click(object sender, RoutedEventArgs e) => OpenButtonMenu(sender);
    private void OpenGroupMenu_Click(object sender, RoutedEventArgs e) => OpenButtonMenu(sender);

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
            _viewModel.Rebuild(catalog);
            UpdateCatalogLocation();
            _viewModel.StatusMessage = "Le catalogue partagé est actif";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Catalogue inaccessible", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private void ReloadCatalog_Click(object sender, RoutedEventArgs e) => ReloadCatalog();

    private void OpenLog_Click(object sender, RoutedEventArgs e)
    {
        try { LogService.OpenLogFolder(); }
        catch (Exception exception) { MessageBox.Show(exception.Message, "Journal inaccessible", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

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

    private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
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
        MoveCardTo(source.Model, target.Model.GroupId, target.Model.SortOrder);
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
        CatalogLocationText.Text = string.IsNullOrWhiteSpace(_settings.CatalogPath) ? "Catalogue local" : "Catalogue partagé";
        CatalogLocationText.ToolTip = _catalogService.CatalogPath;
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
            ReloadCatalog();
            return false;
        }
    }

    private void ReloadCatalog()
    {
        try
        {
            var catalog = _catalogService.LoadOrCreate();
            _viewModel.Rebuild(catalog);
            _viewModel.StatusMessage = "Le catalogue a été rechargé";
        }
        catch (Exception exception)
        {
            LogService.Write("Impossible de recharger le catalogue.", exception);
            MessageBox.Show(exception.Message, "Rechargement impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
        else if (e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            ReloadCatalog();
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
}
