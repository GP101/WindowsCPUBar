# Windows CPU Bar

Windows CPU Bar is a Windows desktop monitor that displays current CPU and GPU usage, short-term history graphs, taskbar progress, and the processes currently using the most CPU.

## Features

- Monitor overall CPU usage and supported GPU 3D-engine usage.
- Display separate CPU and GPU history graphs with optional percentage overlays.
- Draw CPU/GPU history in the title bar while the window is open.
- When minimized, show CPU history in the taskbar button icon and current CPU/GPU percentages in the window caption.
- Show a taskbar progress indicator for current CPU usage.
- List the highest-CPU processes with PID, type, associated service names, CPU usage, and working-set memory.
- Open a selected process's details, executable location, and working directory when accessible.
- Terminate a selected process and its child-process tree.
- Persist display settings between runs.

## Requirements

- Windows
- .NET 8 Desktop Runtime to run the included framework-dependent build
- .NET 8 SDK to build from source

GPU monitoring requires Windows to expose the `GPU Engine` performance-counter category. If it is unavailable, GPU usage is shown as `--%`; CPU monitoring and the remaining features continue to work.

## Run

Run `Release-public\WindowsCpuBar.exe` from this repository's included build output.

## Build from Source

```powershell
dotnet build .\WindowsCpuBar.sln -c Release
```

The Release executable is produced at:

```text
WindowsCpuBar\bin\Release\net8.0-windows\WindowsCpuBar.exe
```

## User Guide

### Monitoring and Display

The left side of the main window shows CPU and GPU history graphs and their current percentage values. Enable **Show current value on graph** to draw the current percentage over each graph.

When the application is minimized:

- The taskbar button icon displays CPU history.
- The window caption displays the latest CPU and GPU percentages.
- The taskbar progress indicator represents current CPU usage.

### Settings

The following controls take effect immediately and are saved automatically.

| Setting | Range | Default | Description |
| --- | --- | --- | --- |
| Update interval | 200 to 10,000 ms | 1,000 ms | How often CPU/GPU values and graphs update. |
| History | 10 to 600 seconds | 60 seconds | Duration retained for each history graph. |
| Show current value on graph | On/Off | On | Displays the current percentage over each graph. |
| CPU/GPU graph color | Any color | Blue / green | Opens a color picker for the respective graph. |
| Top process count | 5 to 30 | 14 | Maximum number of CPU-intensive processes shown after refresh. |

Settings are stored in:

```text
%LocalAppData%\WindowsCpuBar\settings.json
```

### Top CPU Processes

Select **Refresh list** to sample process CPU usage and populate the table. The first refresh may wait for one configured update interval so the application can calculate CPU-time deltas.

The **Type** column uses these values:

- **Normal**: a process with a visible top-level window.
- **Background**: a process without a visible top-level window.
- **Service**: a process associated with one or more Windows services. Associated service names appear in the **Services** column.

Right-click a selected process to choose:

- **Process Info**: view PID, CPU, memory, executable path, working directory, and start time. Use **Open Location** to reveal the executable in File Explorer when the path is available.
- **Kill Process**: terminate the selected process and its complete child-process tree.

> **Warning:** Killing a process can discard unsaved work and stop dependent applications or services. Some protected or elevated processes cannot be inspected or terminated without sufficient permission.

## Technical Implementation

The application is a .NET 8 Windows Forms program.

- **CPU sampling:** `GetSystemTimes` provides idle, kernel, and user time. CPU usage is calculated from consecutive system-time samples and clamped to 0 to 100%.
- **GPU sampling:** `System.Diagnostics.PerformanceCounter` reads `Utilization Percentage` counters for `GPU Engine` instances whose names identify 3D engines. Counters are refreshed periodically, usage is summed per GPU adapter, and the most utilized adapter is shown.
- **History rendering:** fixed-capacity in-memory buffers retain CPU and GPU samples. Custom renderers draw bar-style sparklines in the form, title bar, and minimized taskbar icon.
- **Taskbar integration:** the application uses the Windows taskbar API (`ITaskbarList3`) for CPU progress and Win32 window messages to replace and restore taskbar icons.
- **Process sampling:** per-process `TotalProcessorTime` deltas between refresh samples are normalized by elapsed system time. The process list is sorted by CPU use, and inaccessible processes are skipped.
- **Process metadata:** visible top-level windows and Windows service enumeration are mapped by PID to classify normal, background, and service processes. Metadata lookup failures do not prevent CPU sampling.
- **Process details:** process information uses .NET process APIs; working-directory lookup uses Windows process-memory APIs and may be unavailable for protected or cross-architecture processes.

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

You may run, study, modify, and redistribute the software under the GPL-3.0 terms. Redistributions of modified versions must preserve the GPL-3.0 licensing conditions and include the corresponding license notice and source-code obligations. See [LICENSE](LICENSE) for the complete terms.

Copyright (c) 2026 jintaeks@gmail.com
