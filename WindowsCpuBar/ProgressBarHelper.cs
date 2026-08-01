using System.Runtime.InteropServices;

namespace WindowsCpuBar;

internal static class ProgressBarHelper
{
    private const int PbmSetBarColor = 0x409;
    private const int PbmSetBkColor = 0x2001;

    public static void SetBarColor(ProgressBar progressBar, Color color)
    {
        SendMessage(progressBar.Handle, PbmSetBarColor, 0, ColorToRgb(color));
    }

    public static void SetBackgroundColor(ProgressBar progressBar, Color color)
    {
        SendMessage(progressBar.Handle, PbmSetBkColor, 0, ColorToRgb(color));
    }

    private static int ColorToRgb(Color color) => color.R | (color.G << 8) | (color.B << 16);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
}
