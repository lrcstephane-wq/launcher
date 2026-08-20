using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;
using Microsoft.Win32;

namespace Ideo.TopSolidLauncher.Views;

public partial class CardEditorWindow : Window
{
    private readonly LauncherCatalog _catalog;
    private readonly List<TagChoice> _tagChoices;
    public LauncherCard Result { get; }

    public CardEditorWindow(LauncherCatalog catalog, LauncherCard card, bool isNew)
    {
        InitializeComponent();
        _catalog = catalog;
        Result = card.Clone();
        if (isNew)
            Result.Id = Guid.NewGuid();

        HeadingText.Text = isNew ? "Nouveau raccourci" : "Modifier le raccourci";
        TitleTextBox.Text = Result.Title;
        SubtitleTextBox.Text = Result.Subtitle;
        TargetTextBox.Text = Result.TargetPath;
        ArgumentsTextBox.Text = Result.Arguments;
        WorkingDirectoryTextBox.Text = Result.WorkingDirectory;
        LogoTextBox.Text = Result.LogoPath;
        RunAsAdminCheckBox.IsChecked = Result.RunAsAdministrator;
        MinimizeCheckBox.IsChecked = Result.MinimizeAfterLaunch;

        GroupComboBox.ItemsSource = catalog.Groups.OrderBy(group => group.SortOrder).ToArray();
        GroupComboBox.SelectedValue = Result.GroupId;
        if (GroupComboBox.SelectedIndex < 0)
            GroupComboBox.SelectedIndex = 0;

        ColorComboBox.ItemsSource = new[]
        {
            "#2E69B3", "#265192", "#4D8BD4", "#697D93", "#6B5BA7",
            "#C98247", "#B55858", "#4F8A6D", "#A1A1A1"
        };
        ColorComboBox.Text = Result.AccentColor;

        _tagChoices = catalog.Tags
            .OrderBy(tag => tag.Category).ThenBy(tag => tag.SortOrder).ThenBy(tag => tag.Name)
            .Select(tag => new TagChoice(tag, Result.TagIds.Contains(tag.Id)))
            .ToList();
        TagsItemsControl.ItemsSource = _tagChoices;
        UpdateColorPreview();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ReadForm();
        var structure = CardValidationService.ValidateForSave(Result);
        if (!structure.IsValid)
        {
            MessageBox.Show(structure.Message, "Raccourci incomplet", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var readiness = CardValidationService.Validate(Result);
        if (!readiness.IsValid && MessageBox.Show(
                $"Cette carte ne pourra pas être lancée sur ce poste pour le moment :\n\n{readiness.Message}\n\nL'enregistrer malgré tout ?",
                "Cible indisponible", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        DialogResult = true;
    }

    private void Test_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ReadForm();
            var process = new CardLauncherService().Launch(Result);
            MessageBox.Show(
                process is null ? "La commande a été transmise à Windows." : "La commande a démarré correctement.",
                "Test de la commande", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Test impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BrowseTarget_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choisir l'application ou le fichier à lancer",
            Filter = "Applications et raccourcis|*.exe;*.bat;*.cmd;*.lnk|Tous les fichiers|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true)
            return;

        if (dialog.FileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            ImportShortcut(dialog.FileName);
        else
        {
            TargetTextBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(WorkingDirectoryTextBox.Text))
                WorkingDirectoryTextBox.Text = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        }
    }

    private void BrowseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choisir un logo",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico|Tous les fichiers|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true)
            LogoTextBox.Text = dialog.FileName;
    }

    private void ImportShortcut_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Importer un raccourci Windows",
            Filter = "Raccourcis Windows|*.lnk",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true)
            ImportShortcut(dialog.FileName);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetShortcutPath(e.Data) is null ? DragDropEffects.None : DragDropEffects.Copy;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        var path = GetShortcutPath(e.Data);
        if (path is not null)
            ImportShortcut(path);
    }

    private void ImportShortcut(string path)
    {
        try
        {
            var shortcut = ShortcutImportService.Read(path);
            TitleTextBox.Text = shortcut.Title;
            TargetTextBox.Text = shortcut.TargetPath;
            ArgumentsTextBox.Text = shortcut.Arguments;
            WorkingDirectoryTextBox.Text = shortcut.WorkingDirectory;
            if (Path.GetExtension(shortcut.IconPath).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".ico")
                LogoTextBox.Text = shortcut.IconPath;
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Import impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ColorComboBox_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdateColorPreview();

    private void ColorComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) => UpdateColorPreview();

    private void UpdateColorPreview()
    {
        try
        {
            ColorPreview.Background = (Brush)new BrushConverter().ConvertFromString(ColorComboBox.Text)!;
        }
        catch
        {
            ColorPreview.Background = Brushes.Transparent;
        }
    }

    private void ReadForm()
    {
        Result.Title = TitleTextBox.Text.Trim();
        Result.Subtitle = SubtitleTextBox.Text.Trim();
        Result.GroupId = GroupComboBox.SelectedValue is Guid groupId ? groupId : _catalog.Groups.First().Id;
        Result.TargetPath = TargetTextBox.Text.Trim().Trim('"');
        Result.Arguments = ArgumentsTextBox.Text.Trim();
        Result.WorkingDirectory = WorkingDirectoryTextBox.Text.Trim().Trim('"');
        Result.LogoPath = LogoTextBox.Text.Trim().Trim('"');
        Result.AccentColor = NormalizeColor(ColorComboBox.Text);
        Result.TagIds = _tagChoices.Where(choice => choice.IsSelected).Select(choice => choice.Tag.Id).ToList();
        Result.RunAsAdministrator = RunAsAdminCheckBox.IsChecked == true;
        Result.MinimizeAfterLaunch = MinimizeCheckBox.IsChecked == true;
    }

    private static string NormalizeColor(string color)
    {
        try
        {
            _ = (Color)ColorConverter.ConvertFromString(color);
            return color.Trim();
        }
        catch
        {
            return "#2E69B3";
        }
    }

    private static string? GetShortcutPath(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop) || data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files)
            return null;
        return files[0].EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ? files[0] : null;
    }

    public sealed class TagChoice : INotifyPropertyChanged
    {
        private bool _isSelected;
        public LauncherTag Tag { get; }
        public string Display => $"{Tag.Category} · {Tag.Name}";
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public TagChoice(LauncherTag tag, bool selected) { Tag = tag; _isSelected = selected; }
    }
}
