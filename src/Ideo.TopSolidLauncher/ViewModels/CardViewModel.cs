using System.IO;
using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class CardViewModel : ObservableObject
{
    private bool _isFavorite;

    public LauncherCard Model { get; }
    public IReadOnlyList<LauncherTag> Tags { get; }
    public string Title => Model.Title;
    public string Subtitle => Model.Subtitle;
    public string TargetPath => Model.TargetPath;
    public string AccentColor => Model.AccentColor;
    public string CommandLine => $"{Model.TargetPath} {Model.Arguments}".TrimEnd();
    public string LogoSource => IsSupportedImage(Model.LogoPath)
        ? Environment.ExpandEnvironmentVariables(Model.LogoPath)
        : "pack://application:,,,/Assets/IdeoMark.png";
    public CardValidationResult Validation => CardValidationService.Validate(Model);
    public bool IsValid => Validation.IsValid;
    public string ValidationMessage => Validation.IsValid ? "Prêt à lancer" : Validation.Message;
    public string FavoriteGlyph => IsFavorite ? "★" : "☆";

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (SetProperty(ref _isFavorite, value))
                OnPropertyChanged(nameof(FavoriteGlyph));
        }
    }

    public CardViewModel(LauncherCard model, IReadOnlyList<LauncherTag> tags, bool isFavorite)
    {
        Model = model;
        Tags = tags;
        _isFavorite = isFavorite;
    }

    private static bool IsSupportedImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var expanded = Environment.ExpandEnvironmentVariables(path);
        if (!File.Exists(expanded))
            return false;
        return Path.GetExtension(expanded).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".ico";
    }
}
