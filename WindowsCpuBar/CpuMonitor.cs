using System.Runtime.InteropServices;

namespace WindowsCpuBar;

internal sealed class CpuMonitor : IDisposable
{
    private ulong _idleTime;
    private ulong _kernelTime;
    private ulong _userTime;
    private bool _hasBaseline;

    public double UsagePercent { get; private set; }

    public void Sample()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return;
        }

        var idleTime = FileTimeToUInt64(idle);
        var kernelTime = FileTimeToUInt64(kernel);
        var userTime = FileTimeToUInt64(user);

        if (!_hasBaseline)
        {
            _idleTime = idleTime;
            _kernelTime = kernelTime;
            _userTime = userTime;
            _hasBaseline = true;
            UsagePercent = 0;
            return;
        }

        var idleDelta = idleTime - _idleTime;
        var kernelDelta = kernelTime - _kernelTime;
        var userDelta = userTime - _userTime;

        _idleTime = idleTime;
        _kernelTime = kernelTime;
        _userTime = userTime;

        var total = kernelDelta + userDelta;
        if (total == 0)
        {
            UsagePercent = 0;
            return;
        }

        var busy = total - idleDelta;
        UsagePercent = Math.Clamp(busy * 100.0 / total, 0, 100);
    }

    public void Dispose()
    {
    }

    private static ulong FileTimeToUInt64(FILETIME fileTime)
    {
        return ((ulong)fileTime.dwHighDateTime << 32) | fileTime.dwLowDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(
        out FILETIME idleTime,
        out FILETIME kernelTime,
        out FILETIME userTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }
}
