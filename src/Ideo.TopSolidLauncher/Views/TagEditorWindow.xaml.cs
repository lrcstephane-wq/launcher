using System.Windows;
using Ideo.TopSolidLauncher.Models;

namespace Ideo.TopSolidLauncher.Views;

public partial class TagEditorWindow : Window
{
    public LauncherTag Result { get; }

    public TagEditorWindow(LauncherTag tag, bool isNew)
    {
        InitializeComponent();
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
        ColorTextBox.Text = tag.Color;
        Title = isNew ? "Nouveau tag" : "Modifier le tag";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        var category = CategoryTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
        {
            MessageBox.Show("Le nom et la catégorie sont obligatoires.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var color = ColorTextBox.Text.Trim();
        if (!color.StartsWith('#') || color.Length is not (7 or 9) ||
            !uint.TryParse(color[1..], System.Globalization.NumberStyles.HexNumber, null, out _))
        {
            MessageBox.Show("La couleur doit être au format #RRGGBB ou #AARRGGBB.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Result.Name = name;
        Result.Category = category;
        Result.Description = DescriptionTextBox.Text.Trim();
        Result.Color = color.ToUpperInvariant();
        DialogResult = true;
    }
}
