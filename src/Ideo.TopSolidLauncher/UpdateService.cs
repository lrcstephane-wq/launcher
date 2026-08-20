using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Ideo.TopSolidLauncher;

public static class UpdateService
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/lrcstephane-wq/launcher/releases/latest";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static Version CurrentVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public static async Task<UpdateInfo?> FindUpdateAsync(CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(LatestReleaseUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken);
        if (release is null || !TryParseVersion(release.TagName, out var releaseVersion))
            return null;

        var asset = release.Assets.FirstOrDefault(item =>
            string.Equals(item.Name, "Launcher.exe", StringComparison.OrdinalIgnoreCase));

        if (asset is null || Normalize(releaseVersion) <= Normalize(CurrentVersion))
            return null;

        return new UpdateInfo(releaseVersion, release.TagName, asset.DownloadUrl, asset.Size, asset.Digest);
    }

    public static async Task DownloadAndInstallAsync(
        UpdateInfo update,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var currentExecutable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Le chemin du launcher actuel est introuvable.");

        var updateFolder = Path.Combine(Path.GetTempPath(), "IdeoTopSolidLauncher", update.TagName);
        Directory.CreateDirectory(updateFolder);
        var downloadedExecutable = Path.Combine(updateFolder, "Launcher.exe");

        using var response = await HttpClient.GetAsync(
            update.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalLength = response.Content.Headers.ContentLength ?? update.Size;
        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var destination = File.Create(downloadedExecutable))
        {
            var buffer = new byte[81920];
            long downloaded = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                downloaded += read;
                if (totalLength > 0)
                    progress?.Report((int)(downloaded * 100 / totalLength));
            }
        }

        var downloadedSize = new FileInfo(downloadedExecutable).Length;
        if (downloadedSize == 0 || (update.Size > 0 && downloadedSize != update.Size))
            throw new InvalidDataException("Le fichier de mise à jour téléchargé est incomplet.");

        await VerifyDigestAsync(downloadedExecutable, update.Digest, cancellationToken);
        StartReplacementScript(downloadedExecutable, currentExecutable);
    }

    private static void StartReplacementScript(string downloadedExecutable, string currentExecutable)
    {
        var processId = Environment.ProcessId;
        var scriptPath = Path.Combine(Path.GetTempPath(), $"IdeoLauncherUpdate-{Guid.NewGuid():N}.cmd");
        var script = $"""
            @echo off
            :wait
            tasklist /FI "PID eq {processId}" 2>NUL | find "{processId}" >NUL
            if not errorlevel 1 (
              timeout /T 1 /NOBREAK >NUL
              goto wait
            )
            copy /Y "{downloadedExecutable}" "{currentExecutable}" >NUL
            start "" "{currentExecutable}"
            del /Q "{downloadedExecutable}" >NUL 2>&1
            del /Q "%~f0" >NUL 2>&1
            """;

        File.WriteAllText(scriptPath, script, Encoding.ASCII);
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{scriptPath}\"\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static async Task VerifyDigestAsync(string path, string? digest, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(digest) || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            return;

        await using var stream = File.OpenRead(path);
        var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
        var expectedHash = digest["sha256:".Length..];
        if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("La signature SHA-256 de la mise à jour est incorrecte.");
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Ideo-TopSolidLauncher");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private static bool TryParseVersion(string tag, out Version version)
    {
        var value = tag.Trim().TrimStart('v', 'V').Split('-')[0];
        return Version.TryParse(value, out version!);
    }

    private static Version Normalize(Version version) => new(
        version.Major,
        Math.Max(0, version.Minor),
        Math.Max(0, version.Build),
        Math.Max(0, version.Revision));

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = string.Empty;

        [JsonPropertyName("assets")]
        public GitHubAsset[] Assets { get; init; } = [];
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string DownloadUrl { get; init; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; init; }

        [JsonPropertyName("digest")]
        public string? Digest { get; init; }
    }
}

public sealed record UpdateInfo(
    Version Version,
    string TagName,
    string DownloadUrl,
    long Size,
    string? Digest);
