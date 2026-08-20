namespace Ideo.TopSolidLauncher.Models;

public sealed record CardValidationResult(bool IsValid, string Message)
{
    public static CardValidationResult Valid() => new(true, string.Empty);
    public static CardValidationResult Invalid(string message) => new(false, message);
}
