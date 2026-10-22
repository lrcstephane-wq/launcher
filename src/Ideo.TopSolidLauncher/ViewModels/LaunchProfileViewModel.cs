using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class LaunchProfileViewModel
{
    public LauncherLaunchProfile Model { get; }
    public string Name => Model.Name;
    public CardValidationResult Validation => CardValidationService.ValidateProfile(Model);
    public bool IsAvailable => Validation.IsValid;
    public string StatusGlyph => IsAvailable ? "●" : "!";
    public string ToolTip => IsAvailable ? $"Lancer avec le mode « {Name} »" : Validation.Message;

    public LaunchProfileViewModel(LauncherLaunchProfile model) => Model = model;
}
