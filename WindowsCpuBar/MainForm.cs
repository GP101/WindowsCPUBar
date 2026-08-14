using System.ComponentModel;

namespace WindowsCpuBar;

internal sealed class MainForm : Form
{
    private const string DefaultTitle = "Windows CPU Bar";
    private const int TitleBarDrawWidth = 140;

    private readonly CpuMonitor _cpuMonitor = new();
    private readonly GpuMonitor _gpuMonitor = new();
    private readonly ProcessCpuSampler _processSampler = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly AppSettings _settings;
    private readonly TitleBarCpuRenderer _titleBarRenderer = new();
    private CpuHistoryBuffer _history;
    private CpuHistoryBuffer _gpuHistory;

    private readonly CpuHistoryPanel _historyPanel;
    private readonly Label _overlayLabel;
    private readonly Label _cpuValueLabel;
    private readonly CpuHistoryPanel _gpuHistoryPanel;
    private readonly Label _gpuOverlayLabel;
    private readonly Label _gpuValueLabel;
    private readonly NumericUpDown _intervalNumeric;
    private readonly NumericUpDown _historySecondsNumeric;
    private readonly CheckBox _showTextCheckBox;
    private readonly Button _colorButton;
    private readonly Panel _previewPanel;
    private readonly Button _gpuColorButton;
    private readonly Panel _gpuPreviewPanel;
    private readonly NumericUpDown _topProcessNumeric;
    private readonly Button _refreshProcessButton;
    private readonly ListView _processListView;

    private string _restoredTitle = DefaultTitle;
    private bool _isUpdatingUi;
    private bool _isActiveCaption = true;
    private DateTime _lastTaskbarIconUpdate = DateTime.MinValue;
    private static readonly TimeSpan TaskbarIconUpdateInterval = TimeSpan.FromSeconds(2);

    public MainForm()
    {
        _settings = AppSettings.Load();
        var historyCapacity = _settings.GetHistoryCapacity();
        _history = new CpuHistoryBuffer(historyCapacity);
        _gpuHistory = new CpuHistoryBuffer(historyCapacity);

        Text = DefaultTitle;
        MinimumSize = new Size(1072, 459);
        ClientSize = new Size(1056, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;

        var description = new Label
        {
            AutoSize = false,
            Location = new Point(16, 16),
            Size = new Size(420, 64),
            Text =
                "Displays CPU/GPU usage with history graphs and taskbar progress.\r\n" +
                "When minimized, the taskbar button icon shows the CPU graph,\r\n" +
                "and the caption displays CPU%/GPU%."
        };

        _historyPanel = new CpuHistoryPanel
        {
            Location = new Point(16, 76),
            Size = new Size(420, 28),
            BarColor = _settings.BarColor
        };

        _overlayLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };

        _cpuValueLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 108),
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
            Text = "CPU: --%"
        };

        _gpuHistoryPanel = new CpuHistoryPanel
        {
            Location = new Point(16, 136),
            Size = new Size(420, 28),
            BarColor = _settings.GpuBarColor
        };

        _gpuOverlayLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };

        _gpuValueLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 168),
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
            Text = "GPU: --%"
        };

        var intervalLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 204),
            Text = "Update interval (ms):"
        };

        _intervalNumeric = new NumericUpDown
        {
            Location = new Point(155, 201),
            Width = 90,
            Minimum = 200,
            Maximum = 10000,
            Increment = 100,
            Value = Math.Clamp(_settings.UpdateIntervalMs, 200, 10000)
        };
        _intervalNumeric.ValueChanged += (_, _) => ApplySettingsFromControls();

        var historyLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 240),
            Text = "History (seconds):"
        };

        _historySecondsNumeric = new NumericUpDown
        {
            Location = new Point(155, 237),
            Width = 90,
            Minimum = 10,
            Maximum = 600,
            Increment = 10,
            Value = Math.Clamp(_settings.HistorySeconds, 10, 600)
        };
        _historySecondsNumeric.ValueChanged += (_, _) => ApplySettingsFromControls();

        _showTextCheckBox = new CheckBox
        {
            AutoSize = true,
            Location = new Point(16, 276),
            Text = "Show current value on graph",
            Checked = _settings.ShowText
        };
        _showTextCheckBox.CheckedChanged += (_, _) => ApplySettingsFromControls();

        _colorButton = new Button
        {
            Location = new Point(16, 308),
            Size = new Size(140, 32),
            Text = "CPU graph color..."
        };
        _colorButton.Click += OnColorButtonClick;

        _previewPanel = new Panel
        {
            Location = new Point(300, 308),
            Size = new Size(136, 32),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = _settings.BarColor
        };

        var previewLabel = new Label
        {
            AutoSize = true,
            Location = new Point(240, 314),
            Text = "CPU:"
        };

        _gpuColorButton = new Button
        {
            Location = new Point(16, 348),
            Size = new Size(140, 32),
            Text = "GPU graph color..."
        };
        _gpuColorButton.Click += OnGpuColorButtonClick;

        _gpuPreviewPanel = new Panel
        {
            Location = new Point(300, 348),
            Size = new Size(136, 32),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = _settings.GpuBarColor
        };

        var gpuPreviewLabel = new Label
        {
            AutoSize = true,
            Location = new Point(240, 354),
            Text = "GPU:"
        };

        var topProcessLabel = new Label
        {
            AutoSize = true,
            Location = new Point(460, 20),
            Text = "Top process count:"
        };

        _topProcessNumeric = new NumericUpDown
        {
            Location = new Point(600, 17),
            Width = 90,
            Minimum = 5,
            Maximum = 30,
            Value = Math.Clamp(_settings.TopProcessCount, 5, 30)
        };
        _topProcessNumeric.ValueChanged += (_, _) => ApplySettingsFromControls();

        var processListLabel = new Label
        {
            AutoSize = true,
            Location = new Point(460, 48),
            Text = "Top CPU processes:"
        };

        _refreshProcessButton = new Button
        {
            Location = new Point(904, 43),
            Size = new Size(136, 28),
            Text = "Refresh list"
        };
        _refreshProcessButton.Click += OnRefreshProcessListClick;

        _processListView = new ListView
        {
            Location = new Point(460, 76),
            Size = new Size(580, 304),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            MultiSelect = false
        };
        _processListView.Columns.Add("Process", 180);
        _processListView.Columns.Add("PID", 60);
        _processListView.Columns[0].Width = 130;
        _processListView.Columns.Add("Type", 82);
        _processListView.Columns.Add("CPU %", 70);
        _processListView.Columns.Add("Services", 150);
        _processListView.Columns.Add("Memory", 80);

        var processInfoMenuItem = new ToolStripMenuItem("Process Info");
        processInfoMenuItem.Click += OnProcessInfoClick;

        var killProcessMenuItem = new ToolStripMenuItem("Kill Process");
        killProcessMenuItem.Click += OnKillProcessClick;

        var processContextMenu = new ContextMenuStrip();
        processContextMenu.Opening += OnProcessContextMenuOpening;
        processContextMenu.Items.Add(processInfoMenuItem);
        processContextMenu.Items.Add(killProcessMenuItem);
        _processListView.ContextMenuStrip = processContextMenu;

        var copyrightLabel = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(758, 397),
            ForeColor = SystemColors.GrayText,
            Text = "Copyright (c) 2026 jintaeks@gmail.com"
        };

        Controls.Add(description);
        _historyPanel.Controls.Add(_overlayLabel);
        _gpuHistoryPanel.Controls.Add(_gpuOverlayLabel);
        Controls.Add(_historyPanel);
        Controls.Add(_cpuValueLabel);
        Controls.Add(_gpuHistoryPanel);
        Controls.Add(_gpuValueLabel);
        Controls.Add(intervalLabel);
        Controls.Add(_intervalNumeric);
        Controls.Add(historyLabel);
        Controls.Add(_historySecondsNumeric);
        Controls.Add(_showTextCheckBox);
        Controls.Add(_colorButton);
        Controls.Add(previewLabel);
        Controls.Add(_previewPanel);
        Controls.Add(_gpuColorButton);
        Controls.Add(gpuPreviewLabel);
        Controls.Add(_gpuPreviewPanel);
        Controls.Add(topProcessLabel);
        Controls.Add(_topProcessNumeric);
        Controls.Add(processListLabel);
        Controls.Add(_refreshProcessButton);
        Controls.Add(_processListView);
        Controls.Add(copyrightLabel);

        Load += OnFormLoad;
        Resize += OnFormResize;
        FormClosing += OnFormClosing;

        _timer.Tick += OnTimerTick;
        ApplySettingsFromControls(silent: true);
    }

    private void OnFormLoad(object? sender, EventArgs e)
    {
        _restoredTitle = Text;
        _historyPanel.History = _history;
        _gpuHistoryPanel.History = _gpuHistory;
        TaskbarHistoryIcon.Initialize(Icon);
        UpdateOverlayVisibility();
        _cpuMonitor.Sample();
        _gpuMonitor.Sample();
        _timer.Start();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _timer.Stop();
        TaskbarProgress.Clear(Handle);
        TaskbarHistoryIcon.Restore(Handle);
        TaskbarHistoryIcon.DisposeAll();
        _settings.Save();
        _cpuMonitor.Dispose();
        _gpuMonitor.Dispose();
        _processSampler.Dispose();
    }

    private void OnFormResize(object? sender, EventArgs e)
    {
        UpdateTitleForWindowState();
        UpdateTaskbarHistory();
        InvalidateTitleBar();
        _historyPanel.Invalidate();
        _gpuHistoryPanel.Invalidate();
    }

    private void OnColorButtonClick(object? sender, EventArgs e)
    {
        using var dialog = new ColorDialog
        {
            Color = _settings.BarColor,
            FullOpen = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _settings.BarColorArgb = dialog.Color.ToArgb();
        _historyPanel.BarColor = _settings.BarColor;
        _previewPanel.BackColor = _settings.BarColor;
        _settings.Save();
        _historyPanel.Invalidate();
        InvalidateTitleBar();
        UpdateTaskbarHistory();
    }

    private void OnGpuColorButtonClick(object? sender, EventArgs e)
    {
        using var dialog = new ColorDialog
        {
            Color = _settings.GpuBarColor,
            FullOpen = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _settings.GpuBarColorArgb = dialog.Color.ToArgb();
        _gpuHistoryPanel.BarColor = _settings.GpuBarColor;
        _gpuPreviewPanel.BackColor = _settings.GpuBarColor;
        _settings.Save();
        _gpuHistoryPanel.Invalidate();
        InvalidateTitleBar();
    }

    private void ApplySettingsFromControls(bool silent = false)
    {
        if (_isUpdatingUi)
        {
            return;
        }

        _settings.UpdateIntervalMs = (int)_intervalNumeric.Value;
        _settings.HistorySeconds = (int)_historySecondsNumeric.Value;
        _settings.ShowText = _showTextCheckBox.Checked;
        _settings.TopProcessCount = (int)_topProcessNumeric.Value;

        _timer.Interval = _settings.UpdateIntervalMs;
        EnsureHistoryCapacity();
        UpdateOverlayVisibility();

        if (!silent)
        {
            _settings.Save();
        }
    }

    private void EnsureHistoryCapacity()
    {
        var capacity = _settings.GetHistoryCapacity();
        if (_history.Capacity == capacity && _gpuHistory.Capacity == capacity)
        {
            return;
        }

        _history = new CpuHistoryBuffer(capacity);
        _gpuHistory = new CpuHistoryBuffer(capacity);
        _historyPanel.History = _history;
        _gpuHistoryPanel.History = _gpuHistory;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _cpuMonitor.Sample();
        _gpuMonitor.Sample();
        _history.Add(_cpuMonitor.UsagePercent);
        _gpuHistory.Add(_gpuMonitor.UsagePercent);
        var cpuPercent = (int)Math.Round(_cpuMonitor.UsagePercent);
        var gpuPercent = _gpuMonitor.IsAvailable
            ? (int)Math.Round(_gpuMonitor.UsagePercent)
            : (int?)null;

        _isUpdatingUi = true;
        try
        {
            _cpuValueLabel.Text = $"CPU: {cpuPercent}%";
            _gpuValueLabel.Text = gpuPercent.HasValue ? $"GPU: {gpuPercent.Value}%" : "GPU: --%";
            UpdateOverlayText(cpuPercent, gpuPercent);
            TaskbarProgress.SetValue(Handle, cpuPercent);
            UpdateTitleForWindowState();
            _historyPanel.Invalidate();
            _gpuHistoryPanel.Invalidate();
            InvalidateTitleBar();
        }
        finally
        {
            _isUpdatingUi = false;
        }
    }

    private async void OnRefreshProcessListClick(object? sender, EventArgs e)
    {
        _refreshProcessButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            await Task.Run(() => _processSampler.Sample());

            if (!_processSampler.HasBaseline)
            {
                await Task.Delay(_settings.UpdateIntervalMs);
                await Task.Run(() => _processSampler.Sample());
            }

            UpdateProcessList();
        }
        finally
        {
            UseWaitCursor = false;
            _refreshProcessButton.Enabled = true;
        }
    }

    private void UpdateProcessList()
    {
        if (!_processSampler.HasBaseline)
        {
            return;
        }

        var topProcesses = _processSampler.GetTopProcesses(_settings.TopProcessCount);

        _processListView.BeginUpdate();
        try
        {
            _processListView.Items.Clear();
            foreach (var entry in topProcesses)
            {
                var item = new ListViewItem(entry.Name);
                item.Tag = entry.Pid;
                item.SubItems.Add(entry.Pid.ToString());
                item.SubItems.Add(entry.ProcessType);
                item.SubItems.Add($"{entry.CpuPercent:F1}%");
                item.SubItems.Add(entry.ServiceNames);
                item.SubItems.Add(FormatMemory(entry.WorkingSetBytes));
                _processListView.Items.Add(item);
            }
        }
        finally
        {
            _processListView.EndUpdate();
        }
    }

    private void OnProcessContextMenuOpening(object? sender, CancelEventArgs e)
    {
        var hasSelection = _processListView.SelectedItems.Count > 0;
        if (_processListView.ContextMenuStrip == null)
        {
            return;
        }

        foreach (ToolStripItem menuItem in _processListView.ContextMenuStrip.Items)
        {
            menuItem.Enabled = hasSelection;
        }
    }

    private void OnProcessInfoClick(object? sender, EventArgs e)
    {
        if (_processListView.SelectedItems.Count == 0)
        {
            return;
        }

        var item = _processListView.SelectedItems[0];
        var pid = (int)item.Tag!;
        var processName = item.Text;
        var cpuPercent = item.SubItems.Count > 3 ? item.SubItems[3].Text : "--";
        var memory = item.SubItems.Count > 5 ? item.SubItems[5].Text : "--";

        using var dialog = new ProcessInfoForm(pid, processName, cpuPercent, memory);
        dialog.ShowDialog(this);
        if (dialog.ProcessWasKilled)
        {
            _processListView.Items.Remove(item);
        }
    }

    private void OnKillProcessClick(object? sender, EventArgs e)
    {
        if (_processListView.SelectedItems.Count == 0)
        {
            return;
        }

        var item = _processListView.SelectedItems[0];
        var pid = (int)item.Tag!;

        if (!ProcessKillHelper.TryKill(pid, out var errorMessage))
        {
            MessageBox.Show(
                this,
                errorMessage,
                "Kill Process",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _processListView.Items.Remove(item);
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

    private void UpdateOverlayText(int cpuPercent, int? gpuPercent)
    {
        if (!_settings.ShowText)
        {
            return;
        }

        _overlayLabel.Text = $"{cpuPercent}%";
        _gpuOverlayLabel.Text = gpuPercent.HasValue ? $"{gpuPercent.Value}%" : "--%";
    }

    private void UpdateOverlayVisibility()
    {
        _overlayLabel.Visible = _settings.ShowText;
        _gpuOverlayLabel.Visible = _settings.ShowText;
        if (!_settings.ShowText)
        {
            _overlayLabel.Text = string.Empty;
            _gpuOverlayLabel.Text = string.Empty;
        }
    }

    private void UpdateTaskbarHistory()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        if (WindowState == FormWindowState.Minimized)
        {
            var now = DateTime.UtcNow;
            if (now - _lastTaskbarIconUpdate >= TaskbarIconUpdateInterval)
            {
                TaskbarHistoryIcon.Apply(Handle, _history, _settings.BarColor);
                _lastTaskbarIconUpdate = now;
            }
        }
        else
        {
            TaskbarHistoryIcon.Restore(Handle);
            _lastTaskbarIconUpdate = DateTime.MinValue;
        }
    }

    private void UpdateTitleForWindowState()
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Text = TitleBarCpuRenderer.FormatMinimizedTitle(
                _cpuMonitor.UsagePercent,
                _gpuMonitor.UsagePercent,
                _gpuMonitor.IsAvailable);
        }
        else
        {
            Text = _restoredTitle;
        }

        UpdateTaskbarHistory();
    }

    private void InvalidateTitleBar()
    {
        if (!IsHandleCreated || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        NativeMethods.RedrawWindow(
            Handle,
            IntPtr.Zero,
            IntPtr.Zero,
            NativeMethods.RdwInvalidate | NativeMethods.RdwFrame);
    }

    private void TryDrawTitleBarCpu()
    {
        if (!IsHandleCreated || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        var bounds = GetTitleBarDrawBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var deviceContext = NativeMethods.GetWindowDC(Handle);
        if (deviceContext == IntPtr.Zero)
        {
            return;
        }

        try
        {
            using var graphics = Graphics.FromHdc(deviceContext);
            _titleBarRenderer.Draw(
                graphics,
                bounds,
                _history,
                _gpuHistory,
                _settings.BarColor,
                _settings.GpuBarColor,
                _isActiveCaption);
        }
        finally
        {
            NativeMethods.ReleaseDC(Handle, deviceContext);
        }
    }

    private Rectangle GetTitleBarDrawBounds()
    {
        var frameBorder = SystemInformation.FrameBorderSize;
        var captionHeight = SystemInformation.CaptionHeight;
        var buttonWidth = SystemInformation.CaptionButtonSize.Width;
        var buttonsWidth = buttonWidth * 3 + frameBorder.Width;
        var left = frameBorder.Width + SystemInformation.SmallIconSize.Width + 8;
        var x = Math.Max(left + 140, Width - buttonsWidth - TitleBarDrawWidth);
        var y = frameBorder.Height + 2;
        var height = Math.Max(8, captionHeight - 4);

        return new Rectangle(x, y, TitleBarDrawWidth, height);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == NativeMethods.WmNcActivate)
        {
            _isActiveCaption = m.WParam != IntPtr.Zero;
        }

        if (m.Msg is NativeMethods.WmNcPaint or NativeMethods.WmNcActivate)
        {
            TryDrawTitleBarCpu();
        }
    }
}
