namespace Ideo.TopSolidLauncher.Models;

public sealed class CatalogAuditEntry
{
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string UserName { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
