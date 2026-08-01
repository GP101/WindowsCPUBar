using System.Diagnostics;

namespace WindowsCpuBar;

internal sealed class ProcessInfoForm : Form
{
    private const string UnavailableText = "(Unavailable)";

    private readonly int _pid;
    private readonly TextBox _processNameValue;
    private readonly TextBox _pidValue;
    private readonly TextBox _cpuValue;
    private readonly TextBox _memoryValue;
    private readonly TextBox _executablePathValue;
    private readonly TextBox _workingDirectoryValue;
    private readonly TextBox _startTimeValue;
    private readonly Button _openLocationButton;

    private string? _executablePath;

    public bool ProcessWasKilled { get; private set; }

    public ProcessInfoForm(int pid, string processName, string cpuPercent, string memory)
    {
        _pid = pid;

        Text = "Process Info";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        ClientSize = new Size(480, 320);

        var infoPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(12, 12, 12, 8)
        };
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 7; i++)
        {
            infoPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        _processNameValue = AddInfoRow(infoPanel, 0, "Process:", processName);
        _pidValue = AddInfoRow(infoPanel, 1, "PID:", pid.ToString());
        _cpuValue = AddInfoRow(infoPanel, 2, "CPU:", cpuPercent);
        _memoryValue = AddInfoRow(infoPanel, 3, "Memory:", memory);
        _executablePathValue = AddInfoRow(infoPanel, 4, "Executable:", "...");
        _workingDirectoryValue = AddInfoRow(infoPanel, 5, "Working directory:", "...");
        _startTimeValue = AddInfoRow(infoPanel, 6, "Start time:", "...");

        _executablePathValue.Multiline = true;
        _executablePathValue.Height = 48;
        _workingDirectoryValue.Multiline = true;
        _workingDirectoryValue.Height = 48;

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(12, 8, 12, 12)
        };

        var buttonFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var closeButton = new Button
        {
            Text = "Close",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        closeButton.Click += (_, _) => Close();

        _openLocationButton = new Button
        {
            Text = "Open Location",
            AutoSize = true
        };
        _openLocationButton.Click += OnOpenLocationClick;

        var killButton = new Button
        {
            Text = "Kill",
            AutoSize = true
        };
        killButton.Click += OnKillClick;

        buttonFlow.Controls.Add(closeButton);
        buttonFlow.Controls.Add(_openLocationButton);
        buttonFlow.Controls.Add(killButton);
        buttonPanel.Controls.Add(buttonFlow);

        Controls.Add(infoPanel);
        Controls.Add(buttonPanel);
        CancelButton = closeButton;

        Load += OnFormLoad;
    }

    private static TextBox AddInfoRow(TableLayoutPanel panel, int row, string labelText, string valueText)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 8, 6)
        };

        var valueBox = new TextBox
        {
            Text = valueText,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = SystemColors.Control,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };

        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(valueBox, 1, row);
        return valueBox;
    }

    private void OnFormLoad(object? sender, EventArgs e)
    {
        try
        {
            using var process = Process.GetProcessById(_pid);
            _processNameValue.Text = process.ProcessName;
            _memoryValue.Text = FormatMemory(process.WorkingSet64);

            try
            {
                _executablePath = process.MainModule?.FileName;
                _executablePathValue.Text = string.IsNullOrEmpty(_executablePath)
                    ? UnavailableText
                    : _executablePath;
            }
            catch
            {
                _executablePath = null;
                _executablePathValue.Text = UnavailableText;
            }

            var workingDirectory = ProcessWorkingDirectoryHelper.TryGetCurrentDirectory(_pid);
            _workingDirectoryValue.Text = string.IsNullOrWhiteSpace(workingDirectory)
                ? UnavailableText
                : workingDirectory;

            try
            {
                _startTimeValue.Text = process.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                _startTimeValue.Text = UnavailableText;
            }
        }
        catch
        {
            _executablePathValue.Text = UnavailableText;
            _workingDirectoryValue.Text = UnavailableText;
            _startTimeValue.Text = UnavailableText;
        }

        _openLocationButton.Enabled = !string.IsNullOrEmpty(_executablePath) && File.Exists(_executablePath);
    }

    private void OnKillClick(object? sender, EventArgs e)
    {
        if (!ProcessKillHelper.TryKill(_pid, out var errorMessage))
        {
            MessageBox.Show(
                this,
                errorMessage,
                "Kill Process",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        ProcessWasKilled = true;
        Close();
    }

    private void OnOpenLocationClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_executablePath) || !File.Exists(_executablePath))
        {
            MessageBox.Show(
                this,
                "The executable location is unavailable.",
                "Open Location",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{_executablePath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Open Location",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private static string FormatMemory(long bytes)
    {
        const long mb = 1024 * 1024;
        if (bytes < mb)
        {
            return $"{bytes / 1024} KB";
        }

        return $"{bytes / (double)mb:F0} MB";
    }
}
