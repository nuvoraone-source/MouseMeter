using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using MouseMeter.Infrastructure.Storage;

namespace MouseMeter.Infrastructure;

/// <summary>
/// Prüft im Hintergrund auf GitHub, ob eine neuere MouseMeter-Version
/// veröffentlicht wurde, lädt sie bei Bedarf herunter und installiert sie
/// automatisch (Einstellungen &gt; "Updates automatisch installieren", per
/// Standard aktiviert). Da Windows die laufende EXE nicht überschreiben
/// lässt, kopiert ein kurzlebiges PowerShell-Skript die neuen Dateien erst,
/// nachdem MouseMeter beendet wurde, und startet die App danach neu. Bleibt
/// dieser Neustart aus (z. B. weil der PC währenddessen heruntergefahren
/// wird), startet MouseMeter regulär beim nächsten Windows-Start über den
/// unverändert bestehen bleibenden Autostart-Eintrag (<see cref="AutostartManager"/>).
/// </summary>
public static class UpdateManager
{
    private const string RepoOwnerAndName = "nuvoraone-source/MouseMeter";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    /// <summary>
    /// Prüft (falls fällig und aktiviert) auf ein Update und installiert es
    /// bei Bedarf. Läuft komplett im Hintergrund und wirft nie - ein
    /// Fehlschlag (kein Internet, GitHub nicht erreichbar, o. ä.) bleibt für
    /// den Nutzer unsichtbar und wird beim nächsten Programmstart erneut
    /// versucht.
    /// </summary>
    public static async Task CheckAndApplyAsync(AppState state, Action save, Action applyAndShutdown)
    {
        if (!state.AutoUpdateEnabled)
        {
            return;
        }

        if (DateTime.UtcNow - state.LastUpdateCheckUtc < CheckInterval)
        {
            return;
        }

        state.LastUpdateCheckUtc = DateTime.UtcNow;
        save();

        try
        {
            await RunAsync(applyAndShutdown).ConfigureAwait(false);
        }
        catch
        {
            // Stiller Fehlschlag: Netzwerkprobleme oder ein nicht erreichbares
            // GitHub duerfen den Nutzer nie stoeren, der naechste taegliche
            // Versuch reicht.
        }
    }

    private static async Task RunAsync(Action applyAndShutdown)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppInfo.Name}/{AppInfo.Version}");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        http.Timeout = TimeSpan.FromSeconds(20);

        string json = await http.GetStringAsync($"https://api.github.com/repos/{RepoOwnerAndName}/releases/latest")
            .ConfigureAwait(false);

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        string? tag = root.TryGetProperty("tag_name", out JsonElement tagEl) ? tagEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        string remoteVersionText = tag.StartsWith('v') ? tag[1..] : tag;
        if (!Version.TryParse(remoteVersionText, out Version? remoteVersion)
            || !Version.TryParse(AppInfo.Version, out Version? localVersion)
            || remoteVersion <= localVersion)
        {
            return;
        }

        string? downloadUrl = null;
        if (root.TryGetProperty("assets", out JsonElement assets))
        {
            foreach (JsonElement asset in assets.EnumerateArray())
            {
                string? name = asset.TryGetProperty("name", out JsonElement nameEl) ? nameEl.GetString() : null;
                if (name is not null && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.TryGetProperty("browser_download_url", out JsonElement urlEl) ? urlEl.GetString() : null;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            return;
        }

        string updateRoot = Path.Combine(Path.GetTempPath(), $"MouseMeter-update-{Guid.NewGuid():N}");
        string zipPath = Path.Combine(updateRoot, "update.zip");
        string extractedDir = Path.Combine(updateRoot, "extracted");
        Directory.CreateDirectory(updateRoot);

        await using (Stream download = await http.GetStreamAsync(downloadUrl).ConfigureAwait(false))
        await using (FileStream fileStream = File.Create(zipPath))
        {
            await download.CopyToAsync(fileStream).ConfigureAwait(false);
        }

        ZipFile.ExtractToDirectory(zipPath, extractedDir, overwriteFiles: true);

        // Falls das Release-Zip die Dateien in einem Unterordner enthält
        // (z. B. "MouseMeter-1.3.0/MouseMeter.exe"), statt direkt im Wurzel-
        // verzeichnis, wird dieser eine Unterordner als eigentliche Quelle
        // verwendet - sonst würde beim Kopieren ein falscher Ordner entstehen.
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
            // Kein gültiges Release-Paket (unerwarteter Inhalt) - lieber
            // nichts installieren als eine kaputte Installation hinterlassen.
            Directory.Delete(updateRoot, recursive: true);
            return;
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
