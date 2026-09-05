using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MouseMeter.Infrastructure;

/// <summary>
/// Setzt die native Windows-Titelleiste auf den dunklen Modus, damit sie zur
/// Oberfläche passt. Ohne diesen Aufruf bliebe die Leiste hell.
/// </summary>
public static class WindowTheme
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>
    /// Aktiviert die dunkle Titelleiste. Der Aufruf ist bewusst fehlertolerant:
    /// auf älteren Windows-Versionen bleibt das Fenster einfach hell.
    /// </summary>
    public static void ApplyDarkTitleBar(Window window)
    {
        void Apply()
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            int enabled = 1;
            try
            {
                DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
            }
            catch (DllNotFoundException)
            {
                // Kein DWM verfuegbar - Standarddarstellung ist völlig ausreichend.
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        if (window.IsLoaded)
        {
            Apply();
        }
        else
        {
            window.SourceInitialized += (_, _) => Apply();
        }
    }
}
