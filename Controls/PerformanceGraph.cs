namespace CustomTaskManager.Controls;

public sealed class PerformanceGraph : Control
{
    private readonly Queue<double> _values = new();

    public PerformanceGraph()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(28, 29, 32);
        ForeColor = Color.FromArgb(238, 239, 241);
        LineColor = Color.FromArgb(42, 157, 143);
        GridColor = Color.FromArgb(55, 57, 62);
        Caption = "Usage";
    }

    public string Caption { get; set; }

    public Color LineColor { get; set; }

    public Color GridColor { get; set; }

    public int MaxSamples { get; set; } = 60;

    public double LatestValue => _values.Count == 0 ? 0 : _values.Last();

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
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var backBrush = new SolidBrush(BackColor);
        graphics.FillRectangle(backBrush, ClientRectangle);

        var chartBounds = new Rectangle(14, 38, Math.Max(20, Width - 28), Math.Max(20, Height - 54));

        using var borderPen = new Pen(GridColor);
        using var gridPen = new Pen(Color.FromArgb(42, GridColor), 1);
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

        graphics.DrawString(Caption, captionFont, captionBrush, 12, 10);
        var valueText = $"{LatestValue:N1}%";
        var valueSize = graphics.MeasureString(valueText, valueFont);
        graphics.DrawString(valueText, valueFont, valueBrush, Width - valueSize.Width - 12, 6);

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

        using var linePen = new Pen(LineColor, 2.3F);
        graphics.DrawLines(linePen, points);
    }
}
