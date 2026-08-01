using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WindowsCpuBar;

internal static class TaskbarHistoryIcon
{
    private static Icon? _defaultSmall;
    private static Icon? _defaultLarge;
    private static Icon? _historySmall;
    private static Icon? _historyLarge;
    private static bool _isHistoryActive;

    public static void Initialize(Icon? formIcon)
    {
        DisposeIcons(ref _defaultSmall);
        DisposeIcons(ref _defaultLarge);

        using var source = ResolveSourceIcon(formIcon);
        _defaultSmall = new Icon(source, 16, 16);
        _defaultLarge = new Icon(source, 32, 32);
    }

    public static void Apply(IntPtr hwnd, CpuHistoryBuffer history, Color barColor)
    {
        if (history.Count <= 0 || _defaultSmall == null || _defaultLarge == null)
        {
            return;
        }

        DisposeIcons(ref _historySmall);
        DisposeIcons(ref _historyLarge);

        _historySmall = CreateHistoryIcon(16, history, barColor);
        _historyLarge = CreateHistoryIcon(32, history, barColor);

        NativeMethods.SendMessage(hwnd, NativeMethods.WmSetIcon, (IntPtr)NativeMethods.IconSmall, _historySmall.Handle);
        NativeMethods.SendMessage(hwnd, NativeMethods.WmSetIcon, (IntPtr)NativeMethods.IconBig, _historyLarge.Handle);
        _isHistoryActive = true;
    }

    public static void Restore(IntPtr hwnd)
    {
        if (!_isHistoryActive || _defaultSmall == null || _defaultLarge == null)
        {
            return;
        }

        NativeMethods.SendMessage(hwnd, NativeMethods.WmSetIcon, (IntPtr)NativeMethods.IconSmall, _defaultSmall.Handle);
        NativeMethods.SendMessage(hwnd, NativeMethods.WmSetIcon, (IntPtr)NativeMethods.IconBig, _defaultLarge.Handle);

        DisposeIcons(ref _historySmall);
        DisposeIcons(ref _historyLarge);
        _isHistoryActive = false;
    }

    public static void DisposeAll()
    {
        _isHistoryActive = false;
        DisposeIcons(ref _historySmall);
        DisposeIcons(ref _historyLarge);
        DisposeIcons(ref _defaultSmall);
        DisposeIcons(ref _defaultLarge);
    }

    private static Icon ResolveSourceIcon(Icon? formIcon)
    {
        if (formIcon != null)
        {
            return (Icon)formIcon.Clone();
        }

        var extracted = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (extracted != null)
        {
            return extracted;
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    private static Icon CreateHistoryIcon(int size, CpuHistoryBuffer history, Color barColor)
    {
        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.FromArgb(255, 28, 28, 28));
            CpuSparklineRenderer.Draw(
                graphics,
                new Rectangle(0, 0, size, size),
                history,
                barColor,
                Color.FromArgb(255, 40, 40, 40),
                drawCurrentMarker: size >= 24);
        }

        return CloneIconFromBitmap(bitmap);
    }

    private static Icon CloneIconFromBitmap(Bitmap bitmap)
    {
        var handle = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static void DisposeIcons(ref Icon? icon)
    {
        icon?.Dispose();
        icon = null;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
