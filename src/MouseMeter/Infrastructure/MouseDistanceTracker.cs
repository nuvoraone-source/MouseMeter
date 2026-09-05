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

    /// <summary>Seit Start dieser Sitzung insgesamt zurückgelegte Strecke in Metern.</summary>
    public double TotalDistanceMeters { get; private set; }

    /// <summary>Seit <see cref="Start"/> vergangene Zeit - Grundlage für die Durchschnittsgeschwindigkeit.</summary>
    public TimeSpan Elapsed => _runtime.Elapsed;

    public MouseDistanceTracker()
    {
        _metersPerPixel = MetersPerInch / ReadSystemDpi();
        _hook.MouseMoved += OnMouseMoved;
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
