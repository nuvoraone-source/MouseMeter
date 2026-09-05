using System.Runtime.InteropServices;

namespace MouseMeter.Infrastructure;

/// <summary>Welche Maustaste ein Klick ausgelöst hat.</summary>
public enum MouseButtonKind
{
    Left,
    Right,
    Middle,
}

/// <summary>
/// Systemweiter Low-Level-Mausposition-Hook (WH_MOUSE_LL). Meldet jede
/// Mausbewegung und jeden Tastendruck unabhängig davon, welches Fenster
/// gerade den Fokus hat - das ist nötig, damit MouseMeter die Aktivität
/// auch dann erfasst, wenn eine andere Anwendung im Vordergrund läuft
/// (Gesamtkonzept Abschnitt 1 und 31: automatische Erfassung im Hintergrund).
///
/// Es werden ausschließlich Bildschirmkoordinaten und der gedrückte
/// Maustastentyp gelesen - keine Fensterinhalte, keine Texteingaben.
/// </summary>
public sealed class GlobalMouseHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;
    private const int WmMButtonDown = 0x0207;

    private delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsLlHookStruct
    {
        public Point Pt;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nint DwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandle(string? lpModuleName);

    // Referenz auf dem Feld halten, sonst sammelt der GC den Delegate ein,
    // während der native Hook noch aktiv ist.
    private readonly LowLevelMouseProc _proc;
    private nint _hookHandle;

    /// <summary>Aktuelle Position des Mauszeigers in Bildschirm-Pixeln.</summary>
    public event Action<int, int>? MouseMoved;

    /// <summary>Ausgelöst, sobald eine Maustaste gedrückt wird.</summary>
    public event Action<MouseButtonKind>? ButtonDown;

    public GlobalMouseHook()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != 0)
        {
            return;
        }

        using var curModule = System.Diagnostics.Process.GetCurrentProcess().MainModule
            ?? throw new InvalidOperationException("Hauptmodul des Prozesses nicht verfügbar.");

        nint moduleHandle = GetModuleHandle(curModule.ModuleName);
        _hookHandle = SetWindowsHookEx(WhMouseLl, _proc, moduleHandle, 0);
    }

    public void Stop()
    {
        if (_hookHandle == 0)
        {
            return;
        }

        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = 0;
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            switch ((int)wParam)
            {
                case WmMouseMove:
                    var data = Marshal.PtrToStructure<MsLlHookStruct>(lParam);
                    MouseMoved?.Invoke(data.Pt.X, data.Pt.Y);
                    break;
                case WmLButtonDown:
                    ButtonDown?.Invoke(MouseButtonKind.Left);
                    break;
                case WmRButtonDown:
                    ButtonDown?.Invoke(MouseButtonKind.Right);
                    break;
                case WmMButtonDown:
                    ButtonDown?.Invoke(MouseButtonKind.Middle);
                    break;
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}
