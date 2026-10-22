using Ideo.TopSolidLauncher.Models;
using Ideo.TopSolidLauncher.Services;

namespace Ideo.TopSolidLauncher.ViewModels;

public sealed class ResourceLocationViewModel
{
    public LauncherResourceLocation Model { get; }
    public string Name => Model.Name;
    public string Target => ResourceService.ResolveTarget(Model.Target);
    public CardValidationResult Validation => ResourceService.Validate(Model);
    public bool IsAvailable => Validation.IsValid;
    public string ToolTip => IsAvailable ? Target : $"{Validation.Message}\n{Target}";

    public ResourceLocationViewModel(LauncherResourceLocation model) => Model = model;
}

public sealed class ResourceViewModel
{
    public LauncherResource Model { get; }
    public string Name => Model.Name;
    public string IconGlyph => Model.IconGlyph;
    public IReadOnlyList<ResourceLocationViewModel> Locations { get; }
    public ResourceLocationViewModel? ActiveLocation { get; }
    public bool HasAvailableLocation => Locations.Any(location => location.IsAvailable);
    public string ButtonLabel => ActiveLocation is null ? Name : Locations.Count > 1 ? $"{Name} · {ActiveLocation.Name}" : Name;
    public string ToolTip => ActiveLocation?.ToolTip ?? "Aucun emplacement configuré.";

    public ResourceViewModel(LauncherResource model, Guid? lastLocationId)
    {
        Model = model;
        Locations = model.Locations.OrderBy(location => location.SortOrder).Select(location => new ResourceLocationViewModel(location)).ToArray();
        ActiveLocation = Locations.FirstOrDefault(location => location.Model.Id == lastLocationId)
                         ?? Locations.FirstOrDefault(location => location.Model.Id == model.DefaultLocationId)
                         ?? Locations.FirstOrDefault();
    }
}
