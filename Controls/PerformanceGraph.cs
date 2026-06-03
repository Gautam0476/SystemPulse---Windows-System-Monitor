using System.Drawing.Drawing2D;

namespace CustomTaskManager.Controls;

public sealed class PerformanceGraph : Control
{
    private readonly Queue<double> _values = new();
    private string? _valueTextOverride;

    public PerformanceGraph()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(30, 36, 44);
        ForeColor = Color.FromArgb(242, 245, 248);
        LineColor = Color.FromArgb(0, 179, 167);
        GridColor = Color.FromArgb(55, 66, 80);
        Caption = "Usage";
    }

    public string Caption { get; set; }

    public Color LineColor { get; set; }

    public Color GridColor { get; set; }

    public int MaxSamples { get; set; } = 60;

    public double LatestValue => _values.Count == 0 ? 0 : _values.Last();

    public string? ValueTextOverride
    {
        get => _valueTextOverride;
        set
        {
            if (_valueTextOverride == value)
            {
                return;
            }

            _valueTextOverride = value;
            Invalidate();
        }
    }

    public void AddValue(double value)
    {
        _values.Enqueue(Math.Clamp(value, 0, 100));

        while (_values.Count > MaxSamples)
        {
            _values.Dequeue();
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var backBrush = new SolidBrush(BackColor);
        graphics.FillRectangle(backBrush, ClientRectangle);
        using var outerBorderPen = new Pen(GridColor);
        graphics.DrawRectangle(outerBorderPen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));

        var chartBounds = new Rectangle(16, 42, Math.Max(20, Width - 32), Math.Max(20, Height - 60));

        using var borderPen = new Pen(Color.FromArgb(150, GridColor));
        using var gridPen = new Pen(Color.FromArgb(34, GridColor), 1);
        for (var row = 0; row <= 4; row++)
        {
            var y = chartBounds.Top + row * chartBounds.Height / 4;
            graphics.DrawLine(gridPen, chartBounds.Left, y, chartBounds.Right, y);
        }

        for (var column = 0; column <= 6; column++)
        {
            var x = chartBounds.Left + column * chartBounds.Width / 6;
            graphics.DrawLine(gridPen, x, chartBounds.Top, x, chartBounds.Bottom);
        }

        graphics.DrawRectangle(borderPen, chartBounds);

        using var captionBrush = new SolidBrush(ForeColor);
        using var valueBrush = new SolidBrush(LineColor);
        using var captionFont = new Font(Font.FontFamily, 9F, FontStyle.Bold);
        using var valueFont = new Font(Font.FontFamily, 16F, FontStyle.Bold);

        graphics.DrawString(Caption, captionFont, captionBrush, 14, 12);
        var valueText = ValueTextOverride ?? $"{LatestValue:N1}%";
        var valueSize = graphics.MeasureString(valueText, valueFont);
        graphics.DrawString(valueText, valueFont, valueBrush, Width - valueSize.Width - 14, 8);

        if (_values.Count < 2)
        {
            return;
        }

        var values = _values.ToArray();
        var points = new PointF[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            var x = chartBounds.Left + index * chartBounds.Width / Math.Max(1, MaxSamples - 1);
            var y = chartBounds.Bottom - (float)(values[index] / 100d * chartBounds.Height);
            points[index] = new PointF(x, y);
        }

        using var areaPath = new GraphicsPath();
        areaPath.AddLines(points);
        areaPath.AddLine(points[^1], new PointF(points[^1].X, chartBounds.Bottom));
        areaPath.AddLine(new PointF(points[0].X, chartBounds.Bottom), new PointF(points[0].X, points[0].Y));
        areaPath.CloseFigure();
        using var areaBrush = new LinearGradientBrush(
            chartBounds,
            Color.FromArgb(74, LineColor),
            Color.FromArgb(8, LineColor),
            LinearGradientMode.Vertical);
        graphics.FillPath(areaBrush, areaPath);

        using var glowPen = new Pen(Color.FromArgb(70, LineColor), 5F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var linePen = new Pen(LineColor, 2.3F);
        linePen.StartCap = LineCap.Round;
        linePen.EndCap = LineCap.Round;
        linePen.LineJoin = LineJoin.Round;
        graphics.DrawLines(glowPen, points);
        graphics.DrawLines(linePen, points);
    }
}
