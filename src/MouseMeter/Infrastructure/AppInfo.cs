using System.Diagnostics;
using System.Reflection;

namespace MouseMeter.Infrastructure;

/// <summary>Anwendungsweite Stammdaten (Name, Claim, Version).</summary>
public static class AppInfo
{
    public const string Name = "MouseMeter";
    public const string Tagline = "Every Move Counts";

    /// <summary>Version im Format "0.1.0".</summary>
    public static string Version { get; } = ReadVersion();

    /// <summary>Version mit Praefix, z. B. "v0.1.0".</summary>
    public static string DisplayVersion => "v" + Version;

    private static string ReadVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            // Beim Build aus einem Git-Repo hängt der SDK ggf. "+<commit>" an.
            int plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        var fileVersion = FileVersionInfo.GetVersionInfo(Environment.ProcessPath ?? string.Empty).ProductVersion;
        return string.IsNullOrWhiteSpace(fileVersion) ? "0.0.0" : fileVersion;
    }
}
