namespace WindowsCpuBar;

internal sealed class TitleBarCpuRenderer
{
    public void Draw(
        Graphics graphics,
        Rectangle bounds,
        CpuHistoryBuffer cpuHistory,
        CpuHistoryBuffer gpuHistory,
        Color cpuColor,
        Color gpuColor,
        bool isActive)
    {
        var trackColor = isActive
            ? Color.FromArgb(48, 255, 255, 255)
            : Color.FromArgb(40, 180, 180, 180);

        var halfHeight = bounds.Height / 2;
        if (halfHeight <= 0)
        {
            return;
        }

        var cpuBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, halfHeight);
        var gpuBounds = new Rectangle(bounds.X, bounds.Y + halfHeight, bounds.Width, bounds.Height - halfHeight);

        if (cpuHistory.Count > 0)
        {
            CpuSparklineRenderer.Draw(graphics, cpuBounds, cpuHistory, cpuColor, trackColor, drawCurrentMarker: false);
        }

        if (gpuHistory.Count > 0)
        {
            CpuSparklineRenderer.Draw(graphics, gpuBounds, gpuHistory, gpuColor, trackColor, drawCurrentMarker: false);
        }
    }

    public static string FormatMinimizedTitle(double cpuPercent, double gpuPercent, bool gpuAvailable) =>
        gpuAvailable
            ? $"CPU {(int)Math.Round(cpuPercent)}% GPU {(int)Math.Round(gpuPercent)}%"
            : $"CPU {(int)Math.Round(cpuPercent)}% GPU --%";
}
