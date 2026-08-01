using System.Runtime.InteropServices;



namespace WindowsCpuBar;



internal enum TaskbarProgressState

{

    NoProgress = 0,

    Indeterminate = 0x1,

    Normal = 0x2,

    Error = 0x4,

    Paused = 0x8

}



internal static class TaskbarProgress

{

    private static ITaskbarList3? _taskbarList;
    private static bool _initFailed;

    private static ITaskbarList3? GetTaskbarList()

    {

        if (_taskbarList != null)

        {

            return _taskbarList;

        }

        if (_initFailed)

        {

            return null;

        }

        try

        {

            var type = Type.GetTypeFromCLSID(new Guid("56FDF344-FD6D-433a-A558-3AACB9382371"));

            if (type == null)

            {

                _initFailed = true;

                return null;

            }

            var taskbar = (ITaskbarList3?)Activator.CreateInstance(type);

            if (taskbar != null)

            {

                taskbar.HrInit();

                _taskbarList = taskbar;

            }

            return _taskbarList;

        }

        catch

        {

            _initFailed = true;

            return null;

        }

    }



    public static void SetValue(IntPtr hwnd, int percent)

    {

        var taskbar = GetTaskbarList();

        if (taskbar == null)

        {

            return;

        }

        var value = (ulong)Math.Clamp(percent, 0, 100);

        taskbar.SetProgressState(hwnd, TaskbarProgressState.Normal);

        taskbar.SetProgressValue(hwnd, value, 100);

    }



    public static void Clear(IntPtr hwnd)

    {

        var taskbar = GetTaskbarList();

        if (taskbar == null)

        {

            return;

        }

        taskbar.SetProgressState(hwnd, TaskbarProgressState.NoProgress);

    }



    [ComImport]

    [Guid("56FDF344-FD6D-433a-A558-3AACB9382371")]

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]

    private interface ITaskbarList3

    {

        void HrInit();

        void AddTab(IntPtr hwnd);

        void DeleteTab(IntPtr hwnd);

        void ActivateTab(IntPtr hwnd);

        void SetActiveAlt(IntPtr hwnd);

        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);

        void SetProgressValue(IntPtr hwnd, ulong completed, ulong total);

        void SetProgressState(IntPtr hwnd, TaskbarProgressState state);

    }

}


