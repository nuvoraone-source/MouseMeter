using System.IO;
using System.Text.Json;

namespace MouseMeter.Infrastructure.Storage;

/// <summary>
/// Lädt und speichert den Anwendungszustand als JSON-Datei.
///
/// MouseMeter ist portabel (Gesamtkonzept Abschnitt 4): die Daten werden
/// deshalb bevorzugt in einem Ordner neben der EXE abgelegt, klar getrennt
/// vom Programm selbst (Unterordner "data"). Lässt sich dieser Ordner nicht
/// beschreiben - etwa weil die Anwendung aus einem schreibgeschützten
/// Verzeichnis wie "Programme" läuft -, weicht MouseMeter automatisch auf
/// das Benutzerprofil (%AppData%) aus.
/// </summary>
public sealed class DataStore
{
    private const string FileName = "mousemeter-data.json";
    private const string BackupFileName = "mousemeter-data.backup.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>Pfad, unter dem die Daten tatsächlich abgelegt werden (nach Ermittlung des Speicherorts).</summary>
    public string FilePath { get; }

    /// <summary>Pfad der zuletzt erfolgreich geschriebenen Vorgänger-Version - Rettungsanker bei beschädigter Hauptdatei.</summary>
    private string BackupFilePath { get; }

    public DataStore()
    {
        FilePath = ResolveFilePath();
        BackupFilePath = Path.Combine(Path.GetDirectoryName(FilePath)!, BackupFileName);
    }

    private static string ResolveFilePath()
    {
        string exeDir = AppContext.BaseDirectory;
        string portableDir = Path.Combine(exeDir, "data");

        if (TryEnsureWritableDirectory(portableDir))
        {
            return Path.Combine(portableDir, FileName);
        }

        // Portabler Ordner nicht beschreibbar (z. B. "Programme") - ins Benutzerprofil ausweichen.
        string fallbackDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MouseMeter");
        Directory.CreateDirectory(fallbackDir);
        return Path.Combine(fallbackDir, FileName);
    }

    private static bool TryEnsureWritableDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            string probePath = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}");
            File.WriteAllText(probePath, string.Empty);
            File.Delete(probePath);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// Lädt den gespeicherten Zustand. Ist die Hauptdatei beschädigt oder
    /// nicht lesbar, wird stattdessen die zuletzt erfolgreich geschriebene
    /// Sicherungskopie geladen (siehe <see cref="Save"/>) - so geht die
    /// gesamte Historie nicht schon bei einem einzelnen defekten Schreibvorgang
    /// verloren. Existiert auch die überhaupt keine, beginnt MouseMeter
    /// einfach wieder bei Null, anstatt abzustürzen.
    /// </summary>
    public AppState Load()
    {
        AppState? state = TryLoad(FilePath);
        if (state is not null)
        {
            return state;
        }

        state = TryLoad(BackupFilePath);
        return state ?? new AppState();
    }

    private static AppState? TryLoad(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppState>(json, SerializerOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Speichert den Zustand. Schreibt zunächst in eine temporäre Datei und
    /// ersetzt die eigentliche Datei erst danach - so bleibt bei einem
    /// Absturz mitten im Schreibvorgang immer eine vollständige Version
    /// erhalten. Die bisherige Hauptdatei wird zuvor als Sicherungskopie
    /// abgelegt, damit ein beschädigtes oder unvollständiges Schreiben nicht
    /// die gesamte bisherige Historie mitreißt (siehe <see cref="Load"/>).
    /// </summary>
    public void Save(AppState state)
    {
        try
        {
            if (File.Exists(FilePath))
            {
                File.Copy(FilePath, BackupFilePath, overwrite: true);
            }

            string json = JsonSerializer.Serialize(state, SerializerOptions);
            string tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, FilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Ein einzelner fehlgeschlagener Speichervorgang darf MouseMeter nicht zum Absturz bringen.
        }
    }
}
