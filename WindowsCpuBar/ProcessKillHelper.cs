using System.Diagnostics;

namespace WindowsCpuBar;

internal static class ProcessKillHelper
{
    public static bool TryKill(int pid, out string? errorMessage)
    {
        if (pid == Environment.ProcessId)
        {
            errorMessage = "This application cannot terminate itself.";
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(pid);
            process.Kill(entireProcessTree: true);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
