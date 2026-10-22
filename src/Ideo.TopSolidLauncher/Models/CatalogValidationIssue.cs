namespace Ideo.TopSolidLauncher.Models;

public enum CatalogIssueKind
{
    Card,
    Resource
}

public sealed class CatalogValidationIssue
{
    public CatalogIssueKind Kind { get; init; }
    public Guid ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string DisplayLocation => string.IsNullOrWhiteSpace(LocationName) ? ItemName : $"{ItemName} · {LocationName}";
}
