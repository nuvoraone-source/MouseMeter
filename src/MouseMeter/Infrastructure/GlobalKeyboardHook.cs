using System.Runtime.InteropServices;

namespace MouseMeter.Infrastructure;

/// <summary>
/// Systemweiter Low-Level-Tastaturhook (WH_KEYBOARD_LL). Meldet ausschließlich
/// den virtuellen Tastencode und ob eine Taste gedrückt oder losgelassen
/// wurde - niemals das resultierende Zeichen, den Fenstertext oder die
/// aktive Anwendung. MouseMeter kann daraus keinen eingegebenen Text
/// rekonstruieren (Gesamtkonzept Abschnitt 16 und 30, Datenschutz).
/// </summary>
public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

    private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public uint VkCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public nint DwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandle(string? lpModuleName);

    // Referenz auf dem Feld halten, sonst sammelt der GC den Delegate ein,
    // während der native Hook noch aktiv ist.
    private readonly LowLevelKeyboardProc _proc;
    private nint _hookHandle;

    /// <summary>Ausgelöst, wenn eine Taste heruntergedrückt wird (Parameter: virtueller Tastencode).</summary>
    public event Action<int>? KeyDown;

    /// <summary>Ausgelöst, wenn eine Taste losgelassen wird (Parameter: virtueller Tastencode).</summary>
    public event Action<int>? KeyUp;

    public GlobalKeyboardHook()
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
        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _proc, moduleHandle, 0);
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
            var data = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            int vkCode = (int)data.VkCode;

            switch ((int)wParam)
            {
                case WmKeyDown:
                case WmSysKeyDown:
                    KeyDown?.Invoke(vkCode);
                    break;
                case WmKeyUp:
                case WmSysKeyUp:
                    KeyUp?.Invoke(vkCode);
                    break;
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}
