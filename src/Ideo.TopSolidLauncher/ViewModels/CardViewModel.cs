using System.IO;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class CardViewModel : ObservableObject
{
    private bool _isFavorite;
    private bool _isSelected;

    public LauncherCard Model { get; }
    public IReadOnlyList<LauncherTag> Tags { get; }
    public IReadOnlyList<LaunchProfileViewModel> LaunchProfiles { get; }
    public LaunchProfileViewModel ActiveProfile { get; }
    public IReadOnlyList<QuickAccessViewModel> QuickAccessLinks { get; }
    public IReadOnlyList<QuickAccessViewModel> DisplayedQuickAccessLinks { get; }
    public string Title => Model.Title;
    public string Subtitle => Model.Subtitle;
    public string TargetPath => ActiveProfile.Model.TargetPath;
    public string AccentColor => Model.AccentColor;
    public string CommandLine => $"{ActiveProfile.Model.TargetPath} {ActiveProfile.Model.Arguments}".TrimEnd();
    private readonly string _catalogFolder;

    public string LogoSource => IsSupportedImage(ResolveLogoPath(Model.LogoPath))
        ? ResolveLogoPath(Model.LogoPath)
        : "pack://application:,,,/Assets/IdeoMark.png";
    public CardValidationResult Validation => CardValidationService.Validate(Model, ActiveProfile.Model);
    public bool IsValid => Validation.IsValid;
    public string ValidationMessage => Validation.IsValid ? "Prêt à lancer" : Validation.Message;
    public string FavoriteGlyph => IsFavorite ? "★" : "☆";
    public string LaunchButtonLabel => $"Lancer · {ActiveProfile.Name}";
    public bool HasMultipleLaunchProfiles => LaunchProfiles.Count > 1;
    public bool HasQuickAccessLinks => QuickAccessLinks.Count > 0;

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (SetProperty(ref _isFavorite, value))
                OnPropertyChanged(nameof(FavoriteGlyph));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
                SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? SelectionChanged;

    public CardViewModel(LauncherCard model, IReadOnlyList<LauncherTag> tags, bool isFavorite, bool isSelected,
        string catalogFolder, Guid? lastLaunchProfileId)
    {
        Model = model;
        Tags = tags;
        LaunchProfiles = model.LaunchProfiles.OrderBy(profile => profile.SortOrder).Select(profile => new LaunchProfileViewModel(profile)).ToArray();
        ActiveProfile = LaunchProfiles.FirstOrDefault(profile => profile.Model.Id == lastLaunchProfileId)
                        ?? LaunchProfiles.FirstOrDefault(profile => profile.Model.Id == model.DefaultLaunchProfileId)
                        ?? LaunchProfiles.First();
        QuickAccessLinks = ActiveProfile.Model.QuickAccessLinks
            .Where(link => !string.IsNullOrWhiteSpace(link.Path))
            .Select(link => new QuickAccessViewModel(link, catalogFolder))
            .ToArray();
        DisplayedQuickAccessLinks = QuickAccessLinks.Take(2).ToArray();
        _isFavorite = isFavorite;
        _isSelected = isSelected;
        _catalogFolder = catalogFolder;
    }

    private string ResolveLogoPath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path ?? string.Empty);
        return Path.IsPathRooted(expanded) ? expanded : Path.Combine(_catalogFolder, expanded);
    }

    private static bool IsSupportedImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        if (!File.Exists(path))
            return false;
        return Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".ico";
    }
}
