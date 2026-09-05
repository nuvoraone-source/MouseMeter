using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MouseMeter.Infrastructure;

/// <summary>
/// Berechnet aus den Mausbewegungen die zurückgelegte Strecke in Metern
/// (Gesamtkonzept Abschnitt 8). Die Pixelbewegung des Zeigers wird über die
/// Bildschirmauflösung (DPI) in eine physische Strecke umgerechnet: bei der
/// Standardauflösung von 96 DPI entsprechen 96 Pixel einem Zoll (2,54 cm).
///
/// Läuft vollständig lokal und verarbeitet ausschließlich Koordinaten -
/// keine Fensterinhalte oder Texte (siehe Abschnitt 30, Datenschutz).
/// </summary>
public sealed class MouseDistanceTracker : IDisposable
{
    private const double MetersPerInch = 0.0254;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    private readonly GlobalMouseHook _hook = new();
    private readonly Stopwatch _runtime = new();
    private readonly double _metersPerPixel;

    private bool _hasLastPosition;
    private int _lastX;
    private int _lastY;
    private TimeSpan _seedElapsed = TimeSpan.Zero;

    /// <summary>Heute insgesamt zurückgelegte Strecke in Metern (inklusive vor einem Neustart erfasster Werte).</summary>
    public double TotalDistanceMeters { get; private set; }

    /// <summary>
    /// Heutige Erfassungsdauer - Grundlage für die Durchschnittsgeschwindigkeit.
    /// Setzt sich aus einem beim Programmstart geladenen Sockelwert (falls die
    /// Anwendung heute schon einmal lief) und der Laufzeit dieser Sitzung zusammen.
    /// </summary>
    public TimeSpan Elapsed => _seedElapsed + _runtime.Elapsed;

    public MouseDistanceTracker()
    {
        _metersPerPixel = MetersPerInch / ReadSystemDpi();
        _hook.MouseMoved += OnMouseMoved;
    }

    /// <summary>
    /// Setzt Strecke und Erfassungsdauer auf zuvor gespeicherte Werte, bevor
    /// <see cref="Start"/> aufgerufen wird - damit läuft die heutige Statistik
    /// nach einem Neustart von MouseMeter nahtlos weiter, statt bei Null
    /// anzufangen (Gesamtkonzept Abschnitt 24).
    /// </summary>
    public void Seed(double distanceMeters, TimeSpan elapsed)
    {
        TotalDistanceMeters = distanceMeters;
        _seedElapsed = elapsed;
    }

    /// <summary>Setzt die heutige Strecke und Erfassungsdauer zurück (Tageswechsel, Abschnitt 25).</summary>
    public void ResetForNewDay()
    {
        TotalDistanceMeters = 0;
        _seedElapsed = TimeSpan.Zero;
        _runtime.Restart();
    }

    public void Start()
    {
        _hook.Start();
        _runtime.Start();
    }

    private void OnMouseMoved(int x, int y)
    {
        if (_hasLastPosition)
        {
            double dx = x - _lastX;
            double dy = y - _lastY;
            double pixelDistance = Math.Sqrt(dx * dx + dy * dy);
            TotalDistanceMeters += pixelDistance * _metersPerPixel;
        }

        _lastX = x;
        _lastY = y;
        _hasLastPosition = true;
    }

    private static double ReadSystemDpi()
    {
        try
        {
            uint dpi = GetDpiForSystem();
            return dpi > 0 ? dpi : 96.0;
        }
        catch (EntryPointNotFoundException)
        {
            // Windows-Versionen vor 10/1607 kennen GetDpiForSystem nicht - Standardwert verwenden.
            return 96.0;
        }
    }

    public void Dispose() => _hook.Dispose();
}
