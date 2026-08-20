using System.Windows;

namespace Ideo.TopSolidLauncher.Views;

public partial class PromptWindow : Window
{
    public string Value => ValueTextBox.Text.Trim();

    public PromptWindow(string title, string prompt, string initialValue = "")
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        ValueTextBox.Text = initialValue;
        Loaded += (_, _) =>
        {
            ValueTextBox.Focus();
            ValueTextBox.SelectAll();
        };
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ValueTextBox.Text))
        {
            MessageBox.Show("Saisissez une valeur.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DialogResult = true;
    }
}
