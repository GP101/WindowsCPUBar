using System.Runtime.InteropServices;

namespace WindowsCpuBar;

internal sealed record ProcessMetadata(string ProcessType, string ServiceNames);

internal static class ProcessMetadataProvider
{
    public const string NormalProcessType = "Normal";
    public const string BackgroundProcessType = "Background";
    public const string ServiceProcessType = "Service";

    private const uint ScManagerEnumerateService = 0x0004;
    private const uint ServiceWin32 = 0x00000030;
    private const uint ServiceStateAll = 0x00000003;
    private const int ScEnumProcessInfo = 0;
    private const int ErrorMoreData = 234;

    public static IReadOnlyDictionary<int, ProcessMetadata> GetMetadataByPid()
    {
        var visiblePids = GetVisibleTopLevelProcessIds();
        var servicesByPid = GetServiceNamesByPid();
        var pids = new HashSet<int>(visiblePids);
        pids.UnionWith(servicesByPid.Keys);

        var metadata = new Dictionary<int, ProcessMetadata>();
        foreach (var pid in pids)
        {
            if (servicesByPid.TryGetValue(pid, out var serviceNames))
            {
                metadata[pid] = new ProcessMetadata(ServiceProcessType, string.Join(", ", serviceNames));
            }
            else
            {
                metadata[pid] = new ProcessMetadata(
                    visiblePids.Contains(pid) ? NormalProcessType : BackgroundProcessType,
                    string.Empty);
            }
        }

        return metadata;
    }

    private static HashSet<int> GetVisibleTopLevelProcessIds()
    {
        var pids = new HashSet<int>();
        EnumWindows((window, _) =>
        {
            if (IsWindowVisible(window))
            {
                GetWindowThreadProcessId(window, out var pid);
                if (pid != 0)
                {
                    pids.Add((int)pid);
                }
            }

            return true;
        }, IntPtr.Zero);
        return pids;
    }

    private static Dictionary<int, List<string>> GetServiceNamesByPid()
    {
        var servicesByPid = new Dictionary<int, List<string>>();
        var manager = OpenSCManager(null, null, ScManagerEnumerateService);
        if (manager == IntPtr.Zero)
        {
            return servicesByPid;
        }

        try
        {
            EnumServicesStatusEx(manager, ScEnumProcessInfo, ServiceWin32, ServiceStateAll, IntPtr.Zero, 0,
                out var requiredBytes, out _, IntPtr.Zero);

            if (requiredBytes == 0 || Marshal.GetLastWin32Error() != ErrorMoreData)
            {
                return servicesByPid;
            }

            var buffer = Marshal.AllocHGlobal((int)requiredBytes);
            try
            {
                if (!EnumServicesStatusEx(manager, ScEnumProcessInfo, ServiceWin32, ServiceStateAll, buffer,
                        requiredBytes, out _, out var returnedServices, IntPtr.Zero))
                {
                    return servicesByPid;
                }

                var itemSize = Marshal.SizeOf<EnumServiceStatusProcess>();
                for (var index = 0; index < (int)returnedServices; index++)
                {
                    var item = Marshal.PtrToStructure<EnumServiceStatusProcess>(
                        IntPtr.Add(buffer, checked(index * itemSize)));
                    var pid = (int)item.ServiceStatusProcess.ProcessId;
                    if (pid == 0)
                    {
                        continue;
                    }

                    var name = Marshal.PtrToStringUni(item.DisplayName) ?? Marshal.PtrToStringUni(item.ServiceName);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (!servicesByPid.TryGetValue(pid, out var names))
                    {
                        names = new List<string>();
                        servicesByPid.Add(pid, names);
                    }

                    names.Add(name);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch
        {
            // Service metadata is optional; CPU sampling must continue without it.
        }
        finally
        {
            CloseServiceHandle(manager);
        }

        return servicesByPid;
    }

    private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenSCManager(string? machineName, string? databaseName, uint desiredAccess);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool EnumServicesStatusEx(
        IntPtr serviceManager,
        int infoLevel,
        uint serviceType,
        uint serviceState,
        IntPtr services,
        uint bufferSize,
        out uint bytesNeeded,
        out uint servicesReturned,
        IntPtr resumeHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CloseServiceHandle(IntPtr serviceHandle);

    [StructLayout(LayoutKind.Sequential)]
    private struct EnumServiceStatusProcess
    {
        public IntPtr ServiceName;
        public IntPtr DisplayName;
        public ServiceStatusProcess ServiceStatusProcess;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ServiceStatusProcess
    {
        public uint ServiceType;
        public uint CurrentState;
        public uint ControlsAccepted;
        public uint Win32ExitCode;
        public uint ServiceSpecificExitCode;
        public uint CheckPoint;
        public uint WaitHint;
        public uint ProcessId;
        public uint ServiceFlags;
    }
}
