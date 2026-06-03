using System.Drawing.Drawing2D;

namespace CustomTaskManager.Controls;

public sealed class SystemPieChart : Control
{
    private readonly List<PieChartSlice> _slices = [];

    public SystemPieChart()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(30, 36, 44);
        ForeColor = Color.FromArgb(242, 245, 248);
        MutedColor = Color.FromArgb(154, 164, 177);
        BorderColor = Color.FromArgb(55, 66, 80);
        CenterText = "0%";
        SubText = "System pressure";
    }

    public string CenterText { get; set; }

    public string SubText { get; set; }

    public Color MutedColor { get; set; }

    public Color BorderColor { get; set; }

    public void SetSlices(IEnumerable<PieChartSlice> slices)
    {
        _slices.Clear();
        _slices.AddRange(slices.Where(slice => slice.Value > 0));
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var background = new SolidBrush(BackColor);
        graphics.FillRectangle(background, ClientRectangle);

        using var borderPen = new Pen(BorderColor);
        graphics.DrawRectangle(borderPen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));

        if (Width < 120 || Height < 120)
        {
            return;
        }

        var total = _slices.Sum(slice => slice.Value);
        if (total <= 0)
        {
            DrawEmptyState(graphics);
            return;
        }

        var chartSize = Math.Min(Height - 58, Math.Max(130, (int)(Width * 0.48)));
        chartSize = Math.Min(chartSize, Math.Min(Width - 32, Height - 44));
        var chartBounds = new Rectangle(18, (Height - chartSize) / 2, chartSize, chartSize);

        var startAngle = -90F;
        foreach (var slice in _slices)
        {
            var sweepAngle = (float)(slice.Value / total * 360D);
            using var brush = new SolidBrush(slice.Color);
            graphics.FillPie(brush, chartBounds, startAngle, sweepAngle);
            startAngle += sweepAngle;
        }

        using var ringPen = new Pen(BorderColor, 2F);
        graphics.DrawEllipse(ringPen, chartBounds);

        var innerInset = Math.Max(24, chartSize / 4);
        var innerBounds = Rectangle.Inflate(chartBounds, -innerInset, -innerInset);
        using var innerBrush = new SolidBrush(BackColor);
        graphics.FillEllipse(innerBrush, innerBounds);
        graphics.DrawEllipse(ringPen, innerBounds);

        DrawCenterText(graphics, innerBounds);
        DrawLegend(graphics, new Rectangle(chartBounds.Right + 22, 24, Width - chartBounds.Right - 40, Height - 48), total);
    }

    private void DrawCenterText(Graphics graphics, Rectangle bounds)
    {
        using var centerFont = new Font(Font.FontFamily, Math.Max(14F, bounds.Width / 6F), FontStyle.Bold);
        using var subFont = new Font(Font.FontFamily, 9F, FontStyle.Regular);
        using var textBrush = new SolidBrush(ForeColor);
        using var mutedBrush = new SolidBrush(MutedColor);

        var centerSize = graphics.MeasureString(CenterText, centerFont);
        var subSize = graphics.MeasureString(SubText, subFont);
        var totalHeight = centerSize.Height + subSize.Height - 4;
        var startY = bounds.Top + (bounds.Height - totalHeight) / 2F;

        graphics.DrawString(CenterText, centerFont, textBrush, bounds.Left + (bounds.Width - centerSize.Width) / 2F, startY);
        graphics.DrawString(SubText, subFont, mutedBrush, bounds.Left + (bounds.Width - subSize.Width) / 2F, startY + centerSize.Height - 4);
    }

    private void DrawLegend(Graphics graphics, Rectangle bounds, double total)
    {
        if (bounds.Width < 120)
        {
            return;
        }

        using var titleFont = new Font(Font.FontFamily, 10F, FontStyle.Bold);
        using var itemFont = new Font(Font.FontFamily, 9F, FontStyle.Bold);
        using var detailFont = new Font(Font.FontFamily, 8.5F);
        using var textBrush = new SolidBrush(ForeColor);
        using var mutedBrush = new SolidBrush(MutedColor);

        graphics.DrawString("Overall Analysis", titleFont, textBrush, bounds.Left, bounds.Top);

        var y = bounds.Top + 30;
        foreach (var slice in _slices)
        {
            if (y + 42 > bounds.Bottom)
            {
                break;
            }

            using var dotBrush = new SolidBrush(slice.Color);
            graphics.FillEllipse(dotBrush, bounds.Left, y + 3, 12, 12);

            var percent = slice.Value / total * 100D;
            var label = $"{slice.Label}  {percent:N0}% share";
            graphics.DrawString(label, itemFont, textBrush, bounds.Left + 20, y);
            graphics.DrawString(slice.Detail, detailFont, mutedBrush, bounds.Left + 20, y + 18);
            y += 42;
        }
    }

    private void DrawEmptyState(Graphics graphics)
    {
        using var textBrush = new SolidBrush(MutedColor);
        using var font = new Font(Font.FontFamily, 10F, FontStyle.Bold);
        const string message = "Waiting for visualization data...";
        var size = graphics.MeasureString(message, font);
        graphics.DrawString(message, font, textBrush, (Width - size.Width) / 2F, (Height - size.Height) / 2F);
    }
}
