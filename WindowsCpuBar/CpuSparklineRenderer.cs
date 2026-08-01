namespace WindowsCpuBar;

internal static class CpuSparklineRenderer
{
    public static void Draw(
        Graphics graphics,
        Rectangle bounds,
        CpuHistoryBuffer history,
        Color barColor,
        Color trackColor,
        bool drawCurrentMarker = true)
    {
        if (bounds.Width <= 2 || bounds.Height <= 2)
        {
            return;
        }

        using (var trackBrush = new SolidBrush(trackColor))
        {
            graphics.FillRectangle(trackBrush, bounds);
        }

        const int padding = 2;
        var inner = Rectangle.Inflate(bounds, -padding, -padding);
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        var sampleCount = history.Count;
        if (sampleCount <= 0)
        {
            return;
        }

        using var fillBrush = new SolidBrush(barColor);
        var slotCount = Math.Min(sampleCount, history.Capacity);
        var barWidth = Math.Max(1, inner.Width / history.Capacity);
        var gap = barWidth > 2 ? 1 : 0;
        var drawWidth = Math.Max(1, barWidth - gap);
        var startIndex = history.Capacity - slotCount;

        for (var i = 0; i < slotCount; i++)
        {
            var percent = history.GetOrdered(i);
            var barHeight = (int)Math.Round(inner.Height * percent / 100.0);
            if (barHeight <= 0)
            {
                continue;
            }

            var x = inner.X + (startIndex + i) * barWidth;
            var y = inner.Bottom - barHeight + 1;
            graphics.FillRectangle(fillBrush, x, y, drawWidth, barHeight);
        }

        if (!drawCurrentMarker || sampleCount <= 0)
        {
            return;
        }

        var current = history.GetOrdered(sampleCount - 1);
        var markerX = inner.Right - Math.Max(1, drawWidth);
        var markerHeight = (int)Math.Round(inner.Height * current / 100.0);
        if (markerHeight <= 0)
        {
            return;
        }

        using var markerPen = new Pen(Color.FromArgb(180, Color.White), 1);
        var markerY = inner.Bottom - markerHeight + 1;
        graphics.DrawRectangle(markerPen, markerX, markerY, drawWidth, markerHeight);
    }
}
