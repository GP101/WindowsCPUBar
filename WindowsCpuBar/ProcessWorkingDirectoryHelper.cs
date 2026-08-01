using System.Runtime.InteropServices;
using System.Text;

namespace WindowsCpuBar;

internal static class ProcessWorkingDirectoryHelper
{
    private const uint ProcessQueryInformation = 0x0400;
    private const uint ProcessVmRead = 0x0010;
    private const int ProcessBasicInformation = 0;

    public static string? TryGetCurrentDirectory(int processId)
    {
        var handle = OpenProcess(ProcessQueryInformation | ProcessVmRead, false, processId);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var isWow64 = false;
            if (Environment.Is64BitOperatingSystem)
            {
                IsWow64Process(handle, out isWow64);
            }

            var use32BitLayout = isWow64 || IntPtr.Size == 4;
            return ReadCurrentDirectory(handle, use32BitLayout);
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    private static string? ReadCurrentDirectory(IntPtr processHandle, bool use32BitLayout)
    {
        if (!TryQueryBasicInformation(processHandle, use32BitLayout, out var pebAddress) || pebAddress == IntPtr.Zero)
        {
            return null;
        }

        var processParametersOffset = use32BitLayout ? 0x10 : 0x20;
        var currentDirectoryOffset = use32BitLayout ? 0x24 : 0x38;

        var processParameters = ReadPointer(processHandle, IntPtr.Add(pebAddress, processParametersOffset), use32BitLayout);
        if (processParameters == IntPtr.Zero)
        {
            return null;
        }

        return ReadUnicodeString(processHandle, IntPtr.Add(processParameters, currentDirectoryOffset), use32BitLayout);
    }

    private static bool TryQueryBasicInformation(IntPtr processHandle, bool use32BitLayout, out IntPtr pebAddress)
    {
        pebAddress = IntPtr.Zero;

        if (use32BitLayout)
        {
            var information = default(ProcessBasicInformation32);
            var status = NtQueryInformationProcess(
                processHandle,
                ProcessBasicInformation,
                ref information,
                Marshal.SizeOf<ProcessBasicInformation32>(),
                out _);
            if (status != 0)
            {
                return false;
            }

            pebAddress = information.PebBaseAddress;
            return true;
        }

        var information64 = default(ProcessBasicInformation64);
        var status64 = NtQueryInformationProcess64(
            processHandle,
            ProcessBasicInformation,
            ref information64,
            Marshal.SizeOf<ProcessBasicInformation64>(),
            out _);
        if (status64 != 0)
        {
            return false;
        }

        pebAddress = information64.PebBaseAddress;
        return true;
    }

    private static string? ReadUnicodeString(IntPtr processHandle, IntPtr unicodeStringAddress, bool use32BitLayout)
    {
        if (!TryRead(processHandle, unicodeStringAddress, 2, out var lengthBytes))
        {
            return null;
        }

        var length = BitConverter.ToUInt16(lengthBytes, 0);
        if (length == 0)
        {
            return null;
        }

        var bufferPointerOffset = use32BitLayout ? 4 : 8;
        var bufferAddress = ReadPointer(processHandle, IntPtr.Add(unicodeStringAddress, bufferPointerOffset), use32BitLayout);
        if (bufferAddress == IntPtr.Zero)
        {
            return null;
        }

        if (!TryRead(processHandle, bufferAddress, length, out var stringBytes))
        {
            return null;
        }

        return Encoding.Unicode.GetString(stringBytes);
    }

    private static IntPtr ReadPointer(IntPtr processHandle, IntPtr address, bool use32BitLayout)
    {
        var size = use32BitLayout ? 4 : 8;
        if (!TryRead(processHandle, address, size, out var bytes))
        {
            return IntPtr.Zero;
        }

        return use32BitLayout
            ? (IntPtr)BitConverter.ToInt32(bytes, 0)
            : (IntPtr)BitConverter.ToInt64(bytes, 0);
    }

    private static bool TryRead(IntPtr processHandle, IntPtr address, int size, out byte[] buffer)
    {
        buffer = new byte[size];
        return ReadProcessMemory(processHandle, address, buffer, size, out var bytesRead) && bytesRead == size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation32
    {
        public int ExitStatus;
        public IntPtr PebBaseAddress;
        public IntPtr AffinityMask;
        public int BasePriority;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation64
    {
        public IntPtr ExitStatus;
        public IntPtr PebBaseAddress;
        public IntPtr AffinityMask;
        public IntPtr BasePriority;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        ref ProcessBasicInformation32 processInformation,
        int processInformationLength,
        out int returnLength);

    [DllImport("ntdll.dll", EntryPoint = "NtQueryInformationProcess")]
    private static extern int NtQueryInformationProcess64(
        IntPtr processHandle,
        int processInformationClass,
        ref ProcessBasicInformation64 processInformation,
        int processInformationLength,
        out int returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        int dwSize,
        out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);
}
