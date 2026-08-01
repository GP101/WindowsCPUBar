using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WindowsCpuBar;

internal readonly record struct ProcessCpuEntry(
    int Pid,
    string Name,
    double CpuPercent,
    long WorkingSetBytes,
    string ProcessType,
    string ServiceNames);

internal sealed class ProcessCpuSampler : IDisposable
{
    private readonly Dictionary<int, long> _baselines = new();
    private ulong _idleTime;
    private ulong _kernelTime;
    private ulong _userTime;
    private bool _hasSystemBaseline;

    public bool HasBaseline { get; private set; }

    public void Sample()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return;
        }

        var idleTime = FileTimeToUInt64(idle);
        var kernelTime = FileTimeToUInt64(kernel);
        var userTime = FileTimeToUInt64(user);

        if (!_hasSystemBaseline)
        {
            _idleTime = idleTime;
            _kernelTime = kernelTime;
            _userTime = userTime;
            _hasSystemBaseline = true;
            CaptureProcessBaselines();
            HasBaseline = false;
            return;
        }

        var kernelDelta = kernelTime - _kernelTime;
        var userDelta = userTime - _userTime;
        _idleTime = idleTime;
        _kernelTime = kernelTime;
        _userTime = userTime;

        var systemDelta = kernelDelta + userDelta;
        if (systemDelta == 0)
        {
            return;
        }

        var currentPids = new HashSet<int>();
        var samples = new List<(int Pid, string Name, double CpuPercent, long WorkingSetBytes)>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    var pid = process.Id;
                    currentPids.Add(pid);
                    var processTime = process.TotalProcessorTime.Ticks;

                    if (!_baselines.TryGetValue(pid, out var previousTime))
                    {
                        _baselines[pid] = processTime;
                        continue;
                    }

                    var processDelta = processTime - previousTime;
                    _baselines[pid] = processTime;

                    if (processDelta <= 0)
                    {
                        continue;
                    }

                    var cpuPercent = 100.0 * processDelta / systemDelta;
                    if (cpuPercent <= 0)
                    {
                        continue;
                    }

                    var name = process.ProcessName;
                    var workingSet = process.WorkingSet64;
                    samples.Add((pid, name, cpuPercent, workingSet));
                }
                catch
                {
                    // Skip inaccessible processes.
                }
            }
        }

        PruneBaselines(currentPids);
        var metadataByPid = ProcessMetadataProvider.GetMetadataByPid();
        _lastEntries = samples
            .Select(sample =>
            {
                metadataByPid.TryGetValue(sample.Pid, out var metadata);
                return new ProcessCpuEntry(
                    sample.Pid,
                    sample.Name,
                    sample.CpuPercent,
                    sample.WorkingSetBytes,
                    metadata?.ProcessType ?? ProcessMetadataProvider.BackgroundProcessType,
                    metadata?.ServiceNames ?? string.Empty);
            })
            .ToList();
        HasBaseline = true;
    }

    private List<ProcessCpuEntry> _lastEntries = new();

    private void CaptureProcessBaselines()
    {
        _baselines.Clear();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    _baselines[process.Id] = process.TotalProcessorTime.Ticks;
                }
                catch
                {
                    // Skip inaccessible processes.
                }
            }
        }
    }

    private void PruneBaselines(HashSet<int> currentPids)
    {
        var stalePids = _baselines.Keys.Where(pid => !currentPids.Contains(pid)).ToList();
        foreach (var pid in stalePids)
        {
            _baselines.Remove(pid);
        }
    }

    public IReadOnlyList<ProcessCpuEntry> GetTopProcesses(int count)
    {
        return _lastEntries
            .OrderByDescending(e => e.CpuPercent)
            .ThenByDescending(e => e.WorkingSetBytes)
            .Take(count)
            .ToList();
    }

    public void Dispose()
    {
        _baselines.Clear();
        _lastEntries.Clear();
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
