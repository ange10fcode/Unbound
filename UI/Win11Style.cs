using System;
using System.Runtime.InteropServices;

namespace Unbound.UI;

internal static class Win11Style
{
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmWindowCornerPreference = 33;
    private const int DwmWindowCornerPreferenceRound = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int value,
        int size);

    public static void Apply(IntPtr hwnd)
    {
        try
        {
            int dark = 1;
            _ = DwmSetWindowAttribute(
                hwnd,
                DwmUseImmersiveDarkMode,
                ref dark,
                sizeof(int));

            int rounded = DwmWindowCornerPreferenceRound;
            _ = DwmSetWindowAttribute(
                hwnd,
                DwmWindowCornerPreference,
                ref rounded,
                sizeof(int));
        }
        catch
        {
            // Windows 10 and older builds simply keep the standard title bar.
        }
    }
}
