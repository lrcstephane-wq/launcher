using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Ideo.TopSolidLauncher.Services;

public static class WindowThemeService
{
    private const int DwmUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmUseImmersiveDarkMode = 20;

    public static void ApplyDarkTitleBar(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            try
            {
                var handle = new WindowInteropHelper(window).Handle;
                var enabled = 1;
                if (DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
                    DwmSetWindowAttribute(handle, DwmUseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));
            }
            catch
            {
                // Les versions anciennes de Windows conservent simplement leur barre de titre native.
            }
        };
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int value, int valueSize);
}
