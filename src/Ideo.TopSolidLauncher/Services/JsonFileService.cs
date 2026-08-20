using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ideo.TopSolidLauncher.Services;

internal static class JsonFileService
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static T? Read<T>(string path)
    {
        if (!File.Exists(path))
            return default;

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, Options);
    }

    public static void WriteAtomic<T>(string path, T value)
    {
        var folder = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Le dossier de destination est introuvable.");
        Directory.CreateDirectory(folder);

        var temporaryPath = Path.Combine(folder, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, value, Options);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
