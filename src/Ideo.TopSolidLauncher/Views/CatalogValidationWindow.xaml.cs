using System.Windows;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.Views;

public partial class CatalogValidationWindow : Window
{
    public CatalogValidationIssue? SelectedIssue { get; private set; }
    public CatalogValidationWindow(IReadOnlyList<CatalogValidationIssue> issues, int totalChecked)
    {
        InitializeComponent();
        WindowThemeService.ApplyDarkTitleBar(this);
        IssuesList.ItemsSource = issues;
        SummaryText.Text = issues.Count == 0
            ? $"{totalChecked} élément(s) contrôlé(s) · aucune anomalie détectée."
            : $"{totalChecked} élément(s) contrôlé(s) · {issues.Count} anomalie(s) à corriger.";
    }
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (IssuesList.SelectedItem is not CatalogValidationIssue issue) return;
        SelectedIssue = issue;
        DialogResult = true;
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
