namespace WindowsCpuBar;

internal sealed class CpuHistoryPanel : Panel
{
    public CpuHistoryBuffer? History { get; set; }

    public Color BarColor { get; set; } = Color.FromArgb(0, 120, 215);

    public CpuHistoryPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        UpdateStyles();
        BackColor = SystemColors.ControlLight;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (History == null || History.Count == 0)
        {
            return;
        }

        CpuSparklineRenderer.Draw(
            e.Graphics,
            ClientRectangle,
            History,
            BarColor,
            BackColor,
            drawCurrentMarker: true);
    }
}
