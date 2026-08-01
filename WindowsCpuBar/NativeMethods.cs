using System.Runtime.InteropServices;

namespace WindowsCpuBar;

internal static class NativeMethods
{
    public const int WmNcPaint = 0x0085;
    public const int WmNcActivate = 0x0086;
    public const int WmSetIcon = 0x0080;

    public const int IconSmall = 0;
    public const int IconBig = 1;

    [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public const uint RdwInvalidate = 0x0001;
    public const uint RdwFrame = 0x0400;
    public const uint RdwUpdatedNow = 0x0100;

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("user32.dll")]
    public static extern bool RedrawWindow(
        IntPtr hWnd,
        IntPtr lprcUpdate,
        IntPtr hrgnUpdate,
        uint flags);
}
