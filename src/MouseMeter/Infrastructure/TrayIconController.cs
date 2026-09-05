using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace MouseMeter.Infrastructure;

/// <summary>
/// Verwaltet das Symbol in der Windows-Ablage (neben der Uhr), das MouseMeter
/// beim Minimieren anzeigt, statt einen Eintrag in der Taskleiste zu belegen.
/// Implementiert direkt über die native Shell-API (Shell_NotifyIcon), damit
/// keine zusätzliche Windows-Forms-Abhängigkeit ins Projekt kommt.
///
/// Ein einfacher Klick stellt das Fenster wieder her, ein Rechtsklick öffnet
/// ein kleines Menü zum Wiederherstellen oder Beenden.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private const int WmTrayIcon = 0x8001; // WM_APP + 1
    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonUp = 0x0205;
    private const int NimAdd = 0x00000000;
    private const int NimDelete = 0x00000002;
    private const int NifMessage = 0x00000001;
    private const int NifIcon = 0x00000002;
    private const int NifTip = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int CbSize;
        public nint HWnd;
        public int UId;
        public int UFlags;
        public int UCallbackMessage;
        public nint HIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string SzTip;
    }

    [DllImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NotifyIconData data);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct Win32Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Win32Point point);

    private readonly Window _window;
    private readonly nint _hwnd;
    private readonly HwndSource _hwndSource;
    private readonly ContextMenu _menu;
    private readonly Icon _icon;
    private NotifyIconData _data;
    private bool _isVisible;

    /// <summary>Ausgelöst, wenn das Fenster aus der Ablage wiederhergestellt werden soll.</summary>
    public event Action? RestoreRequested;

    /// <summary>Ausgelöst, wenn die Anwendung über das Ablage-Menü beendet werden soll.</summary>
    public event Action? ExitRequested;

    public TrayIconController(Window window, Icon icon)
    {
        _window = window;
        _icon = icon;
        _hwnd = new WindowInteropHelper(window).EnsureHandle();

        _hwndSource = HwndSource.FromHwnd(_hwnd)
            ?? throw new InvalidOperationException("Kein HwndSource für das Hauptfenster verfügbar.");
        _hwndSource.AddHook(WndProc);

        _menu = BuildContextMenu();

        _data = new NotifyIconData
        {
            CbSize = Marshal.SizeOf<NotifyIconData>(),
            HWnd = _hwnd,
            UId = 1,
            UFlags = NifIcon | NifMessage | NifTip,
            UCallbackMessage = WmTrayIcon,
            HIcon = icon.Handle,
            SzTip = $"{AppInfo.Name} - {AppInfo.Tagline}",
        };
    }

    private ContextMenu BuildContextMenu()
    {
        var openItem = new MenuItem { Header = "MouseMeter öffnen" };
        openItem.Click += (_, _) => RestoreRequested?.Invoke();

        var exitItem = new MenuItem { Header = "Beenden" };
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        var menu = new ContextMenu();
        menu.Items.Add(openItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);
        return menu;
    }

    /// <summary>Zeigt das Ablage-Symbol an (beim Minimieren in den Hintergrund).</summary>
    public void Show()
    {
        if (_isVisible)
        {
            return;
        }

        Shell_NotifyIcon(NimAdd, ref _data);
        _isVisible = true;
    }

    /// <summary>Blendet das Ablage-Symbol wieder aus (Fenster ist sichtbar).</summary>
    public void Hide()
    {
        if (!_isVisible)
        {
            return;
        }

        Shell_NotifyIcon(NimDelete, ref _data);
        _isVisible = false;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg != WmTrayIcon)
        {
            return nint.Zero;
        }

        switch ((int)lParam)
        {
            case WmLButtonUp:
                RestoreRequested?.Invoke();
                handled = true;
                break;
            case WmRButtonUp:
                ShowContextMenu();
                handled = true;
                break;
        }

        return nint.Zero;
    }

    private void ShowContextMenu()
    {
        GetCursorPos(out var cursor);

        // Empfohlenes Vorgehen für Ablage-Menüs: das (unsichtbare) Fenster kurz
        // in den Vordergrund holen, damit sich das Menü bei einem Klick daneben
        // zuverlässig wieder schließt.
        SetForegroundWindow(_hwnd);

        _menu.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
        _menu.PlacementTarget = _window;
        _menu.HorizontalOffset = cursor.X;
        _menu.VerticalOffset = cursor.Y;
        _menu.IsOpen = true;
    }

    public void Dispose()
    {
        Hide();
        _hwndSource.RemoveHook(WndProc);
        _icon.Dispose();
    }
}
