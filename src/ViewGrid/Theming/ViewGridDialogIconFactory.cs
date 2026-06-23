using System.Drawing.Drawing2D;

namespace ViewGrid.Theming;

/// <summary>
/// Creates small theme-aware icons for ViewGrid owned tool windows. The icons are
/// intentionally generated in code so the library does not need external image
/// resources and stays designer/package friendly.
/// </summary>
public static class ViewGridDialogIconFactory
{
    public static Icon Create(ViewGridDialogIconKind kind, ViewGridTheme theme, int size = 32)
    {
        size = Math.Max(16, Math.Min(64, size));
        using var bitmap = new Bitmap(size, size);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            Color accent = theme.AccentColor == Color.Empty ? Color.FromArgb(0, 120, 215) : theme.AccentColor;
            Color surface = theme.IsDark ? Color.FromArgb(48, 48, 54) : Color.White;
            Color stroke = theme.IsDark ? Color.FromArgb(225, 225, 230) : Color.FromArgb(42, 42, 48);
            Color soft = Blend(accent, surface, theme.IsDark ? 0.35 : 0.62);

            Rectangle outer = new Rectangle(2, 2, size - 5, size - 5);
            using (GraphicsPath path = RoundedRect(outer, Math.Max(4, size / 5)))
            using (SolidBrush fill = new SolidBrush(soft))
            using (Pen border = new Pen(Blend(accent, stroke, 0.25f), 1.4f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            using var pen = new Pen(stroke, Math.Max(1.7f, size / 15f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using var accentPen = new Pen(accent, Math.Max(2f, size / 11f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using var brush = new SolidBrush(accent);
            using var textBrush = new SolidBrush(stroke);

            DrawGlyph(g, kind, outer, pen, accentPen, brush, textBrush, theme);
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static void DrawGlyph(Graphics g, ViewGridDialogIconKind kind, Rectangle r, Pen pen, Pen accentPen, Brush accentBrush, Brush textBrush, ViewGridTheme theme)
    {
        int x = r.X;
        int y = r.Y;
        int w = r.Width;
        int h = r.Height;

        switch (kind)
        {
            case ViewGridDialogIconKind.Filter:
                Point[] funnel =
                {
                    new(x + w / 4, y + h / 4),
                    new(x + 3 * w / 4, y + h / 4),
                    new(x + 58 * w / 100, y + h / 2),
                    new(x + 58 * w / 100, y + 72 * h / 100),
                    new(x + 44 * w / 100, y + 80 * h / 100),
                    new(x + 44 * w / 100, y + h / 2)
                };
                using (GraphicsPath p = new GraphicsPath()) { p.AddPolygon(funnel); g.FillPath(accentBrush, p); g.DrawPath(pen, p); }
                break;
            case ViewGridDialogIconKind.Column:
                for (int i = 0; i < 3; i++)
                    g.DrawLine(i == 1 ? accentPen : pen, x + w / 4 + i * w / 5, y + h / 4, x + w / 4 + i * w / 5, y + 3 * h / 4);
                break;
            case ViewGridDialogIconKind.Search:
                g.DrawEllipse(accentPen, x + w / 4, y + h / 4, w / 3, h / 3);
                g.DrawLine(pen, x + 56 * w / 100, y + 56 * h / 100, x + 73 * w / 100, y + 73 * h / 100);
                break;
            case ViewGridDialogIconKind.Export:
                g.DrawRectangle(pen, x + w / 4, y + 42 * h / 100, w / 2, h / 3);
                g.DrawLine(accentPen, x + w / 2, y + h / 5, x + w / 2, y + 58 * h / 100);
                g.DrawLine(accentPen, x + w / 2, y + h / 5, x + 38 * w / 100, y + 35 * h / 100);
                g.DrawLine(accentPen, x + w / 2, y + h / 5, x + 62 * w / 100, y + 35 * h / 100);
                break;
            case ViewGridDialogIconKind.Designer:
                g.DrawRectangle(pen, x + w / 4, y + w / 4, w / 2, h / 2);
                g.FillEllipse(accentBrush, x + 20 * w / 100, y + 20 * h / 100, w / 7, h / 7);
                g.FillEllipse(accentBrush, x + 67 * w / 100, y + 20 * h / 100, w / 7, h / 7);
                g.FillEllipse(accentBrush, x + 20 * w / 100, y + 67 * h / 100, w / 7, h / 7);
                g.FillEllipse(accentBrush, x + 67 * w / 100, y + 67 * h / 100, w / 7, h / 7);
                break;
            case ViewGridDialogIconKind.Command:
                g.DrawString(">", new Font("Segoe UI", Math.Max(11, w / 2f), FontStyle.Bold), textBrush, x + w * 0.24f, y + h * 0.12f);
                break;
            case ViewGridDialogIconKind.Info:
                DrawInfo(g, r, accentBrush, textBrush);
                break;
            case ViewGridDialogIconKind.Warning:
                DrawWarning(g, r, accentBrush, pen);
                break;
            case ViewGridDialogIconKind.Success:
                g.DrawLine(accentPen, x + w / 4, y + h / 2, x + 43 * w / 100, y + 68 * h / 100);
                g.DrawLine(accentPen, x + 43 * w / 100, y + 68 * h / 100, x + 76 * w / 100, y + 32 * h / 100);
                break;
            case ViewGridDialogIconKind.Error:
                g.DrawLine(accentPen, x + w / 3, y + h / 3, x + 2 * w / 3, y + 2 * h / 3);
                g.DrawLine(accentPen, x + 2 * w / 3, y + h / 3, x + w / 3, y + 2 * h / 3);
                break;
            default:
                DrawGrid(g, r, pen, accentPen);
                break;
        }
    }

    private static void DrawGrid(Graphics g, Rectangle r, Pen pen, Pen accentPen)
    {
        int x = r.X, y = r.Y, w = r.Width, h = r.Height;
        for (int row = 0; row < 3; row++)
            g.DrawLine(row == 0 ? accentPen : pen, x + w / 4, y + h / 4 + row * h / 5, x + 3 * w / 4, y + h / 4 + row * h / 5);
        for (int col = 0; col < 3; col++)
            g.DrawLine(pen, x + w / 4 + col * w / 5, y + h / 4, x + w / 4 + col * w / 5, y + 3 * h / 4);
    }

    private static void DrawInfo(Graphics g, Rectangle r, Brush accent, Brush text)
    {
        int x = r.X, y = r.Y, w = r.Width, h = r.Height;
        g.FillEllipse(accent, x + w / 2 - w / 14, y + h / 4, w / 7, h / 7);
        using var font = new Font("Segoe UI", Math.Max(13, w / 2f), FontStyle.Bold);
        g.DrawString("i", font, text, x + w * 0.42f, y + h * 0.31f);
    }

    private static void DrawWarning(Graphics g, Rectangle r, Brush fill, Pen pen)
    {
        int x = r.X, y = r.Y, w = r.Width, h = r.Height;
        Point[] tri = { new(x + w / 2, y + h / 5), new(x + 78 * w / 100, y + 73 * h / 100), new(x + 22 * w / 100, y + 73 * h / 100) };
        g.FillPolygon(fill, tri);
        g.DrawPolygon(pen, tri);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = Math.Max(1, radius) * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color Blend(Color first, Color second, double amount)
    {
        amount = Math.Max(0d, Math.Min(1d, amount));
        int r = (int)Math.Round(first.R + (second.R - first.R) * amount);
        int g = (int)Math.Round(first.G + (second.G - first.G) * amount);
        int b = (int)Math.Round(first.B + (second.B - first.B) * amount);
        return Color.FromArgb(r, g, b);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
