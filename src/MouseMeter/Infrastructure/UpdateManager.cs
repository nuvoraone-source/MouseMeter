using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using MouseMeter.Infrastructure.Storage;

namespace MouseMeter.Infrastructure;

/// <summary>Eine auf GitHub gefundene, neuere MouseMeter-Version.</summary>
public sealed record UpdateInfo(string Version, DateTime PublishedAtUtc, string DownloadUrl, string? ChecksumUrl, string ZipAssetName);

/// <summary>Ergebnis eines Installationsversuchs, für eine passende Rückmeldung an den Nutzer.</summary>
public enum UpdateApplyResult
{
    Started,
    ChecksumMismatch,
    InvalidPackage,
    Failed,
}

/// <summary>
/// Prüft auf GitHub, ob eine neuere MouseMeter-Version veröffentlicht wurde
/// (Einstellungen &gt; "Automatisch nach Updates suchen", per Standard
/// aktiviert; einmal 1 Stunde nach dem Start, danach alle 24 Stunden). Ein
/// gefundenes Update wird NICHT automatisch installiert - erst wenn der
/// Nutzer das rote "Update"-Lämpchen in der Statusleiste anklickt, prüft
/// <see cref="DownloadAndInstallAsync"/> die Prüfsumme des Downloads und
/// installiert ihn. Da Windows die laufende EXE nicht überschreiben lässt,
/// kopiert ein kurzlebiges PowerShell-Skript die neuen Dateien erst, nachdem
/// MouseMeter beendet wurde, und startet die App danach neu. Bleibt dieser
/// Neustart aus (z. B. weil der PC währenddessen heruntergefahren wird),
/// startet MouseMeter regulär beim nächsten Windows-Start über den
/// unverändert bestehen bleibenden Autostart-Eintrag (<see cref="AutostartManager"/>).
/// </summary>
public static class UpdateManager
{
    private const string RepoOwnerAndName = "nuvoraone-source/MouseMeter";
    public static readonly TimeSpan FirstCheckDelay = TimeSpan.FromHours(1);
    public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    /// <summary>
    /// Prüft (falls fällig und aktiviert) auf ein Update - lädt dabei nichts
    /// herunter. Läuft im Hintergrund und wirft nie: ein Fehlschlag (kein
    /// Internet, GitHub nicht erreichbar, o. ä.) bleibt unsichtbar und wird
    /// beim nächsten fälligen Versuch wiederholt.
    /// </summary>
    public static async Task<UpdateInfo?> CheckForUpdateAsync(AppState state, Action save, bool force = false)
    {
        if (!force)
        {
            if (!state.AutoUpdateEnabled)
            {
                return null;
            }

            if (DateTime.UtcNow - state.LastUpdateCheckUtc < CheckInterval)
            {
                return null;
            }
        }

        state.LastUpdateCheckUtc = DateTime.UtcNow;
        save();

        try
        {
            return await FetchLatestAsync().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<UpdateInfo?> FetchLatestAsync()
    {
        using HttpClient http = CreateClient();

        string json = await http.GetStringAsync($"https://api.github.com/repos/{RepoOwnerAndName}/releases/latest")
            .ConfigureAwait(false);

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        string? tag = root.TryGetProperty("tag_name", out JsonElement tagEl) ? tagEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        string remoteVersionText = tag.StartsWith('v') ? tag[1..] : tag;
        if (!Version.TryParse(remoteVersionText, out Version? remoteVersion)
            || !Version.TryParse(AppInfo.Version, out Version? localVersion)
            || remoteVersion <= localVersion)
        {
            return null;
        }

        DateTime publishedAt = root.TryGetProperty("published_at", out JsonElement publishedEl)
            && publishedEl.TryGetDateTime(out DateTime parsed)
                ? parsed.ToUniversalTime()
                : DateTime.UtcNow;

        string? downloadUrl = null;
        string? zipName = null;
        if (root.TryGetProperty("assets", out JsonElement assets))
        {
            foreach (JsonElement asset in assets.EnumerateArray())
            {
                string? name = asset.TryGetProperty("name", out JsonElement nameEl) ? nameEl.GetString() : null;
                if (name is not null && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.TryGetProperty("browser_download_url", out JsonElement urlEl) ? urlEl.GetString() : null;
                    zipName = name;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(downloadUrl) || zipName is null)
        {
            return null;
        }

        // Die Prüfsumme wird als eigener Anhang mit demselben Namen plus
        // ".sha256" erwartet (bei jedem Release mit hochgeladen).
        string checksumName = zipName + ".sha256";
        string? checksumUrl = null;
        if (root.TryGetProperty("assets", out JsonElement assetsAgain))
        {
            foreach (JsonElement asset in assetsAgain.EnumerateArray())
            {
                string? name = asset.TryGetProperty("name", out JsonElement nameEl) ? nameEl.GetString() : null;
                if (string.Equals(name, checksumName, StringComparison.OrdinalIgnoreCase))
                {
                    checksumUrl = asset.TryGetProperty("browser_download_url", out JsonElement urlEl) ? urlEl.GetString() : null;
                    break;
                }
            }
        }

        return new UpdateInfo(remoteVersionText, publishedAt, downloadUrl, checksumUrl, zipName);
    }

    /// <summary>
    /// Lädt das Update herunter, prüft seine SHA-256-Prüfsumme und installiert
    /// es nur, wenn sie übereinstimmt. Wird vom Nutzer manuell ausgelöst
    /// (Klick auf das Update-Lämpchen), nicht automatisch.
    /// </summary>
    public static async Task<UpdateApplyResult> DownloadAndInstallAsync(UpdateInfo info, Action applyAndShutdown)
    {
        try
        {
            using HttpClient http = CreateClient();

            string updateRoot = Path.Combine(Path.GetTempPath(), $"MouseMeter-update-{Guid.NewGuid():N}");
            string zipPath = Path.Combine(updateRoot, "update.zip");
            string extractedDir = Path.Combine(updateRoot, "extracted");
            Directory.CreateDirectory(updateRoot);

            byte[] zipBytes = await http.GetByteArrayAsync(info.DownloadUrl).ConfigureAwait(false);
            await File.WriteAllBytesAsync(zipPath, zipBytes).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(info.ChecksumUrl))
            {
                Directory.Delete(updateRoot, recursive: true);
                return UpdateApplyResult.ChecksumMismatch;
            }

            string checksumText = await http.GetStringAsync(info.ChecksumUrl).ConfigureAwait(false);
            string? expectedHash = checksumText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            string actualHash = Convert.ToHexString(SHA256.HashData(zipBytes));

            if (string.IsNullOrWhiteSpace(expectedHash)
                || !string.Equals(expectedHash.Trim(), actualHash, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Delete(updateRoot, recursive: true);
                return UpdateApplyResult.ChecksumMismatch;
            }

            ZipFile.ExtractToDirectory(zipPath, extractedDir, overwriteFiles: true);

            // Falls das Release-Zip die Dateien in einem Unterordner enthält
            // (z. B. "MouseMeter-1.3.0/MouseMeter.exe"), statt direkt im
            // Wurzelverzeichnis, wird dieser eine Unterordner als eigentliche
            // Quelle verwendet - sonst würde beim Kopieren ein falscher
            // Ordner entstehen.
            string sourceDir = extractedDir;
            string[] topEntries = Directory.GetFileSystemEntries(extractedDir);
            if (!File.Exists(Path.Combine(extractedDir, "MouseMeter.exe"))
                && topEntries.Length == 1
                && Directory.Exists(topEntries[0]))
            {
                sourceDir = topEntries[0];
            }

            if (!File.Exists(Path.Combine(sourceDir, "MouseMeter.exe")))
            {
                Directory.Delete(updateRoot, recursive: true);
                return UpdateApplyResult.InvalidPackage;
            }

            string installDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
            string scriptPath = Path.Combine(updateRoot, "apply-update.ps1");
            File.WriteAllText(scriptPath, BuildUpdateScript(Environment.ProcessId, sourceDir, installDir, updateRoot));

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            Process.Start(psi);

            applyAndShutdown();
            return UpdateApplyResult.Started;
        }
        catch
        {
            return UpdateApplyResult.Failed;
        }
    }

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppInfo.Name}/{AppInfo.Version}");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return http;
    }

    private static string BuildUpdateScript(int processId, string sourceDir, string installDir, string updateRoot) =>
        "try { Wait-Process -Id " + processId + " -Timeout 30 -ErrorAction SilentlyContinue } catch {}\n" +
        "Start-Sleep -Seconds 1\n" +
        $"robocopy \"{sourceDir}\" \"{installDir}\" /E /IS /IT /NFL /NDL /NJH /NJS /NP | Out-Null\n" +
        "Start-Sleep -Milliseconds 500\n" +
        $"try {{ Start-Process -FilePath \"{installDir}\\MouseMeter.exe\" -ArgumentList \"{AutostartManager.StartMinimizedArg}\" }} catch {{}}\n" +
        "Start-Sleep -Seconds 2\n" +
        $"Remove-Item -Path \"{updateRoot}\" -Recurse -Force -ErrorAction SilentlyContinue\n";
}
