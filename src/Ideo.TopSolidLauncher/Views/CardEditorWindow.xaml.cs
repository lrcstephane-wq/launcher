using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;
using Ideo.TopSolidLauncher.ViewModels;
using Microsoft.Win32;

namespace Ideo.TopSolidLauncher.Views;

public partial class CardEditorWindow : Window
{
    private readonly LauncherCatalog _catalog;
    private readonly List<TagChoice> _tagChoices;
    private readonly ObservableCollection<LaunchProfileEditorItem> _profiles;
    private Guid _defaultProfileId;
    public LauncherCard Result { get; }

    public CardEditorWindow(LauncherCatalog catalog, LauncherCard card, bool isNew)
    {
        InitializeComponent();
        WindowThemeService.ApplyDarkTitleBar(this);
        _catalog = catalog;
        Result = card.Clone();
        if (isNew) Result.Id = Guid.NewGuid();
        if (Result.LaunchProfiles.Count == 0)
        {
            var profile = new LauncherLaunchProfile();
            Result.LaunchProfiles.Add(profile);
            Result.DefaultLaunchProfileId = profile.Id;
        }

        HeadingText.Text = isNew ? "Nouveau raccourci" : "Modifier le raccourci";
        TitleTextBox.Text = Result.Title;
        SubtitleTextBox.Text = Result.Subtitle;
        LogoTextBox.Text = Result.LogoPath;
        GroupComboBox.ItemsSource = catalog.Groups.OrderBy(group => group.SortOrder).ToArray();
        GroupComboBox.SelectedValue = Result.GroupId;
        if (GroupComboBox.SelectedIndex < 0) GroupComboBox.SelectedIndex = 0;

        var colors = ColorChoices();
        if (colors.All(color => !color.Hex.Equals(Result.AccentColor, StringComparison.OrdinalIgnoreCase)))
            colors.Add(new ColorChoice("Personnalisée", Result.AccentColor));
        ColorTextBox.Text = Result.AccentColor;
        ColorComboBox.ItemsSource = colors;
        ColorComboBox.SelectedItem = colors.First(color => color.Hex.Equals(Result.AccentColor, StringComparison.OrdinalIgnoreCase));

        _tagChoices = catalog.Tags.OrderBy(tag => tag.CategorySortOrder).ThenBy(tag => tag.SortOrder)
            .Select(tag => new TagChoice(tag, Result.TagIds.Contains(tag.Id))).ToList();
        TagsItemsControl.ItemsSource = _tagChoices;
        _defaultProfileId = Result.DefaultLaunchProfileId;
        _profiles = new ObservableCollection<LaunchProfileEditorItem>(Result.LaunchProfiles.OrderBy(profile => profile.SortOrder)
            .Select(profile => new LaunchProfileEditorItem(profile, profile.Id == _defaultProfileId)));
        ProfilesList.ItemsSource = _profiles;
        ProfilesList.SelectedIndex = 0;
        UpdateColorPreview();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ReadForm();
        var structure = CardValidationService.ValidateForSave(Result);
        if (!structure.IsValid)
        {
            FormMessageText.Text = structure.Message;
            return;
        }

        var unavailable = Result.LaunchProfiles.Count(profile => !CardValidationService.ValidateProfile(profile).IsValid);
        if (unavailable > 0 && MessageBox.Show(
                $"{unavailable} mode(s) ne sont pas disponibles sur ce poste. Ils resteront enregistrés pour les autres postes.\n\nEnregistrer quand même ?",
                "Modes indisponibles", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        DialogResult = true;
    }

    private void Test_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not LaunchProfileEditorItem selected) return;
        try
        {
            ReadForm();
            var profile = Result.LaunchProfiles.First(item => item.Id == selected.Id);
            new CardLauncherService().Launch(Result, profile);
            FormMessageText.Text = $"Le mode « {profile.Name} » a démarré.";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Test impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AddProfile_Click(object sender, RoutedEventArgs e)
    {
        var profile = new LauncherLaunchProfile { Name = $"Mode {_profiles.Count + 1}", SortOrder = _profiles.Count };
        var item = new LaunchProfileEditorItem(profile, _profiles.Count == 0);
        _profiles.Add(item);
        if (_profiles.Count == 1) _defaultProfileId = item.Id;
        RefreshDefaultFlags();
        ProfilesList.SelectedItem = item;
    }

    private void DuplicateProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not LaunchProfileEditorItem selected) return;
        var clone = selected.ToModel();
        clone.Id = Guid.NewGuid();
        clone.Name += " — copie";
        clone.SortOrder = _profiles.Count;
        var item = new LaunchProfileEditorItem(clone, false);
        _profiles.Add(item);
        ProfilesList.SelectedItem = item;
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not LaunchProfileEditorItem selected) return;
        if (_profiles.Count == 1)
        {
            FormMessageText.Text = "Une carte doit conserver au moins un mode.";
            return;
        }
        var index = ProfilesList.SelectedIndex;
        _profiles.Remove(selected);
        if (_defaultProfileId == selected.Id) _defaultProfileId = _profiles[0].Id;
        ReindexProfiles();
        RefreshDefaultFlags();
        ProfilesList.SelectedIndex = Math.Min(index, _profiles.Count - 1);
    }

    private void SetDefaultProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not LaunchProfileEditorItem selected) return;
        _defaultProfileId = selected.Id;
        RefreshDefaultFlags();
    }

    private void MoveProfileUp_Click(object sender, RoutedEventArgs e) => MoveProfile(-1);
    private void MoveProfileDown_Click(object sender, RoutedEventArgs e) => MoveProfile(1);

    private void MoveProfile(int direction)
    {
        var index = ProfilesList.SelectedIndex;
        var target = index + direction;
        if (index < 0 || target < 0 || target >= _profiles.Count) return;
        _profiles.Move(index, target);
        ReindexProfiles();
        ProfilesList.SelectedIndex = target;
    }

    private void AddQuickAccess_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is LaunchProfileEditorItem selected)
            selected.QuickAccessLinks.Add(new LauncherQuickAccess());
    }

    private void RemoveQuickAccess_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is LaunchProfileEditorItem selected && (sender as FrameworkElement)?.DataContext is LauncherQuickAccess access)
            selected.QuickAccessLinks.Remove(access);
    }

    private void BrowseQuickAccess_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not LauncherQuickAccess access) return;
        var initial = Environment.ExpandEnvironmentVariables(access.Path.Trim().Trim('"'));
        var dialog = new OpenFolderDialog { Title = $"Choisir le dossier « {access.Label} »", InitialDirectory = Directory.Exists(initial) ? initial : null };
        if (dialog.ShowDialog(this) == true) access.Path = dialog.FolderName;
    }

    private void BrowseTarget_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not LaunchProfileEditorItem selected) return;
        var dialog = new OpenFileDialog
        {
            Title = "Choisir l'application ou le fichier",
            Filter = "Applications et raccourcis|*.exe;*.bat;*.cmd;*.lnk|Tous les fichiers|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true) return;
        if (dialog.FileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) ImportShortcut(dialog.FileName, selected);
        else
        {
            selected.TargetPath = dialog.FileName;
            if (string.IsNullOrWhiteSpace(selected.WorkingDirectory)) selected.WorkingDirectory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        }
    }

    private void BrowseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Choisir un logo", Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico|Tous les fichiers|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) LogoTextBox.Text = dialog.FileName;
    }

    private void ImportShortcut_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Importer un raccourci Windows", Filter = "Raccourcis Windows|*.lnk", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true && ProfilesList.SelectedItem is LaunchProfileEditorItem selected)
            ImportShortcut(dialog.FileName, selected);
    }

    private void ImportShortcut(string path, LaunchProfileEditorItem selected)
    {
        try
        {
            var shortcut = ShortcutImportService.Read(path);
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text) || TitleTextBox.Text == "Nouvelle application") TitleTextBox.Text = shortcut.Title;
            selected.TargetPath = shortcut.TargetPath;
            selected.Arguments = shortcut.Arguments;
            selected.WorkingDirectory = shortcut.WorkingDirectory;
            if (Path.GetExtension(shortcut.IconPath).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".ico") LogoTextBox.Text = shortcut.IconPath;
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Import impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetDroppedPath(e.Data) is null ? DragDropEffects.None : DragDropEffects.Copy;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (ProfilesList.SelectedItem is not LaunchProfileEditorItem selected) return;
        var path = GetDroppedPath(e.Data);
        if (path?.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) == true) ImportShortcut(path, selected);
        else if (path is not null && Directory.Exists(path))
            selected.QuickAccessLinks.Add(new LauncherQuickAccess { Label = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), Path = path });
    }

    private void ProfilesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => FormMessageText.Text = string.Empty;

    private void ColorComboBox_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ColorComboBox.SelectedItem is ColorChoice color && ColorTextBox.Text != color.Hex) ColorTextBox.Text = color.Hex;
        UpdateColorPreview();
    }

    private void ColorTextBox_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateColorPreview();

    private void ReadForm()
    {
        Result.Title = TitleTextBox.Text.Trim();
        Result.Subtitle = SubtitleTextBox.Text.Trim();
        Result.GroupId = GroupComboBox.SelectedValue is Guid groupId ? groupId : _catalog.Groups.First().Id;
        Result.LogoPath = LogoTextBox.Text.Trim().Trim('"');
        Result.AccentColor = NormalizeColor(ColorTextBox.Text);
        Result.TagIds = _tagChoices.Where(choice => choice.IsSelected).Select(choice => choice.Tag.Id).ToList();
        Result.LaunchProfiles = _profiles.Select(item => item.ToModel()).ToList();
        Result.DefaultLaunchProfileId = Result.LaunchProfiles.Any(profile => profile.Id == _defaultProfileId) ? _defaultProfileId : Result.LaunchProfiles.First().Id;
    }

    private void RefreshDefaultFlags()
    {
        foreach (var profile in _profiles) profile.IsDefault = profile.Id == _defaultProfileId;
    }

    private void ReindexProfiles()
    {
        for (var index = 0; index < _profiles.Count; index++) _profiles[index].SortOrder = index;
    }

    private void UpdateColorPreview()
    {
        try { ColorPreview.Background = (Brush)new BrushConverter().ConvertFromString(ColorTextBox.Text)!; }
        catch { ColorPreview.Background = Brushes.Transparent; }
    }

    private static List<ColorChoice> ColorChoices() =>
    [
        new("Bleu Idéo", "#2E69B3"), new("Bleu profond", "#265192"), new("Bleu clair", "#4D8BD4"),
        new("Bleu gris", "#697D93"), new("Violet", "#6B5BA7"), new("Orange", "#C98247"),
        new("Rouge", "#B55858"), new("Vert", "#4F8A6D"), new("Gris", "#A1A1A1")
    ];

    private static string NormalizeColor(string color)
    {
        var value = color.Trim();
        return value.StartsWith('#') && value.Length is 7 or 9 && uint.TryParse(value[1..], System.Globalization.NumberStyles.HexNumber, null, out _)
            ? value.ToUpperInvariant() : "#2E69B3";
    }

    private static string? GetDroppedPath(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop) || data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files) return null;
        return files[0].EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || Directory.Exists(files[0]) ? files[0] : null;
    }

    public sealed class LaunchProfileEditorItem : ObservableObject
    {
        private string _name;
        private string _targetPath;
        private string _arguments;
        private string _workingDirectory;
        private bool _runAsAdministrator;
        private bool _minimizeAfterLaunch;
        private bool _isDefault;

        public Guid Id { get; set; }
        public int SortOrder { get; set; }
        public ObservableCollection<LauncherQuickAccess> QuickAccessLinks { get; }
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public string TargetPath { get => _targetPath; set => SetProperty(ref _targetPath, value); }
        public string Arguments { get => _arguments; set => SetProperty(ref _arguments, value); }
        public string WorkingDirectory { get => _workingDirectory; set => SetProperty(ref _workingDirectory, value); }
        public bool RunAsAdministrator { get => _runAsAdministrator; set => SetProperty(ref _runAsAdministrator, value); }
        public bool MinimizeAfterLaunch { get => _minimizeAfterLaunch; set => SetProperty(ref _minimizeAfterLaunch, value); }
        public bool IsDefault { get => _isDefault; set => SetProperty(ref _isDefault, value); }

        public LaunchProfileEditorItem(LauncherLaunchProfile profile, bool isDefault)
        {
            Id = profile.Id;
            SortOrder = profile.SortOrder;
            _name = profile.Name;
            _targetPath = profile.TargetPath;
            _arguments = profile.Arguments;
            _workingDirectory = profile.WorkingDirectory;
            _runAsAdministrator = profile.RunAsAdministrator;
            _minimizeAfterLaunch = profile.MinimizeAfterLaunch;
            _isDefault = isDefault;
            QuickAccessLinks = new ObservableCollection<LauncherQuickAccess>(profile.QuickAccessLinks.Select(link => link.Clone()));
        }

        public LauncherLaunchProfile ToModel() => new()
        {
            Id = Id,
            Name = Name.Trim(),
            TargetPath = TargetPath.Trim().Trim('"'),
            Arguments = Arguments.Trim(),
            WorkingDirectory = WorkingDirectory.Trim().Trim('"'),
            QuickAccessLinks = QuickAccessLinks.Select(link => new LauncherQuickAccess { Label = link.Label.Trim(), Path = link.Path.Trim().Trim('"') }).ToList(),
            SortOrder = SortOrder,
            RunAsAdministrator = RunAsAdministrator,
            MinimizeAfterLaunch = MinimizeAfterLaunch
        };
    }

    public sealed class TagChoice : INotifyPropertyChanged
    {
        private bool _isSelected;
        public LauncherTag Tag { get; }
        public string Display => $"{Tag.Category} · {Tag.Name}";
        public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        public TagChoice(LauncherTag tag, bool selected) { Tag = tag; _isSelected = selected; }
    }

    public sealed record ColorChoice(string Name, string Hex);
}
