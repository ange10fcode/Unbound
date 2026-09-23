using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Unbound.UI;

public sealed class RoundedPanel : Panel
{
    public int Radius { get; set; } = 12;
    public Color BorderColor { get; set; } = Color.Transparent;

    public RoundedPanel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    protected override void OnResize(System.EventArgs eventargs)
    {
        base.OnResize(eventargs);
        UpdateRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = DrawingTools.RoundRect(rect, Radius);
        using var background = new SolidBrush(BackColor);

        e.Graphics.FillPath(background, path);

        if (BorderColor != Color.Transparent)
        {
            using var pen = new Pen(BorderColor);
            e.Graphics.DrawPath(pen, path);
        }
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
            return;

        using GraphicsPath path = DrawingTools.RoundRect(
            new Rectangle(0, 0, Width, Height),
            Radius);

        Region?.Dispose();
        Region = new Region(path);
    }
}

internal static class DrawingTools
{
    public static GraphicsPath RoundRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        diameter = System.Math.Min(diameter, System.Math.Min(bounds.Width, bounds.Height));

        var path = new GraphicsPath();
        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
