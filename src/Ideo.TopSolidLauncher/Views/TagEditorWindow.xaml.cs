using System.Windows;
using System.Windows.Media;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.Views;

public partial class TagEditorWindow : Window
{
    public LauncherTag Result { get; }

    public TagEditorWindow(LauncherTag tag, bool isNew)
    {
        InitializeComponent();
        WindowThemeService.ApplyDarkTitleBar(this);
        Result = new LauncherTag
        {
            Id = isNew ? Guid.NewGuid() : tag.Id,
            Name = tag.Name,
            Category = tag.Category,
            Description = tag.Description,
            Color = tag.Color,
            SortOrder = tag.SortOrder
        };
        NameTextBox.Text = tag.Name;
        CategoryTextBox.Text = tag.Category;
        DescriptionTextBox.Text = tag.Description;
        Title = isNew ? "Nouveau tag" : "Modifier le tag";
        HeadingText.Text = Title;

        var colors = new List<ColorChoice>
        {
            new("Bleu Idéo", "#2E69B3"), new("Bleu profond", "#265192"),
            new("Bleu clair", "#4D8BD4"), new("Bleu gris", "#697D93"),
            new("Violet", "#6B5BA7"), new("Orange", "#C98247"),
            new("Rouge", "#B55858"), new("Vert", "#4F8A6D"), new("Gris", "#A1A1A1")
        };
        if (colors.All(color => !color.Hex.Equals(tag.Color, StringComparison.OrdinalIgnoreCase)))
            colors.Add(new ColorChoice("Personnalisée", tag.Color));
        ColorTextBox.Text = tag.Color;
        ColorComboBox.ItemsSource = colors;
        ColorComboBox.SelectedItem = colors.First(color => color.Hex.Equals(tag.Color, StringComparison.OrdinalIgnoreCase));
        UpdateColorPreview();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        var category = CategoryTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
        {
            FormMessageText.Text = "Le nom et la catégorie sont obligatoires.";
            return;
        }
        var color = ColorTextBox.Text.Trim();
        if (!color.StartsWith('#') || color.Length is not (7 or 9) ||
            !uint.TryParse(color[1..], System.Globalization.NumberStyles.HexNumber, null, out _))
        {
            FormMessageText.Text = "La couleur doit être au format #RRGGBB ou #AARRGGBB.";
            return;
        }

        Result.Name = name;
        Result.Category = category;
        Result.Description = DescriptionTextBox.Text.Trim();
        Result.Color = color.ToUpperInvariant();
        DialogResult = true;
    }

    private void ColorComboBox_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ColorComboBox.SelectedItem is ColorChoice color && ColorTextBox.Text != color.Hex)
            ColorTextBox.Text = color.Hex;
        UpdateColorPreview();
    }

    private void ColorTextBox_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateColorPreview();

    private void UpdateColorPreview()
    {
        try
        {
            ColorPreview.Background = (Brush)new BrushConverter().ConvertFromString(ColorTextBox.Text)!;
        }
        catch
        {
            ColorPreview.Background = Brushes.Transparent;
        }
    }

    private sealed record ColorChoice(string Name, string Hex);
}
