using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Taylan.Pano.Theming;

/// <summary>
/// Theme-aware ToolStrip renderer used by Pano popup menus, merged user menus and nested drop-down menus.
/// The important part is that it also paints nested ToolStripDropDown surfaces; otherwise dark theme
/// parent menus can open light/default Windows submenus.
/// </summary>
public sealed class SmartMenuRenderer : ToolStripProfessionalRenderer
{
    private readonly PanoTheme _theme;

    public SmartMenuRenderer(PanoTheme theme) : base(new SmartMenuColorTable(theme))
    {
        _theme = theme;
    }

    public static void ApplyTo(ToolStrip? strip, PanoTheme theme)
    {
        if (strip == null || strip.IsDisposed) return;

        strip.Renderer = new SmartMenuRenderer(theme);
        strip.BackColor = theme.PanelBackColor;
        strip.ForeColor = theme.ForeColor;

        foreach (ToolStripItem item in strip.Items)
            ApplyToItem(item, theme);
    }

    public static void ApplyToItem(ToolStripItem item, PanoTheme theme)
    {
        item.BackColor = theme.PanelBackColor;
        item.ForeColor = item.Enabled ? theme.ForeColor : theme.EmptyTextColor;

        if (item is ToolStripMenuItem menuItem)
        {
            menuItem.DropDown.Renderer = new SmartMenuRenderer(theme);
            menuItem.DropDown.BackColor = theme.PanelBackColor;
            menuItem.DropDown.ForeColor = theme.ForeColor;
            menuItem.DropDownOpening -= OnDropDownOpening;
            menuItem.DropDownOpening += OnDropDownOpening;
            menuItem.DropDown.Tag = theme;

            foreach (ToolStripItem child in menuItem.DropDownItems)
                ApplyToItem(child, theme);
        }
    }

    private static void OnDropDownOpening(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item) return;
        if (item.DropDown.Tag is not PanoTheme theme) return;
        ApplyTo(item.DropDown, theme);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var b = new SolidBrush(_theme.PanelBackColor);
        e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.ToolStrip.Size));
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var p = new Pen(_theme.BorderColor);
        e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        var rect = e.AffectedBounds;
        using var b = new SolidBrush(_theme.IsDark ? Blend(_theme.PanelBackColor, Color.White, 0.035) : Blend(_theme.PanelBackColor, Color.Black, 0.025));
        e.Graphics.FillRectangle(b, rect);
        using var p = new Pen(_theme.BorderColor);
        e.Graphics.DrawLine(p, rect.Right - 1, rect.Top, rect.Right - 1, rect.Bottom);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var rect = new Rectangle(Point.Empty, e.Item.Size);
        var isPressed = e.Item.Pressed || (e.Item is ToolStripMenuItem mi && mi.DropDown.Visible);
        var back = e.Item.Selected || isPressed ? _theme.HotBackColor : _theme.PanelBackColor;
        using var b = new SolidBrush(back);
        e.Graphics.FillRectangle(b, rect);

        if (e.Item.Selected || isPressed)
        {
            using var p = new Pen(_theme.AccentColor);
            e.Graphics.DrawRectangle(p, 1, 1, Math.Max(0, rect.Width - 3), Math.Max(0, rect.Height - 3));
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var backColor = e.Item.Selected || e.Item.Pressed ? _theme.HotBackColor : _theme.PanelBackColor;
        var textColor = e.Item.Enabled ? _theme.ForeColor : _theme.EmptyTextColor;
        e.TextColor = EnsureReadable(textColor, backColor);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var rect = e.ImageRectangle;
        rect.Inflate(3, 3);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var b = new SolidBrush(_theme.SelectionBackColor))
            e.Graphics.FillRoundedRectangle(b, rect, 4);
        using (var p = new Pen(_theme.SelectionForeColor, 2f))
        {
            int x1 = rect.Left + rect.Width / 4;
            int y1 = rect.Top + rect.Height / 2;
            int x2 = rect.Left + rect.Width / 2 - 1;
            int y2 = rect.Bottom - rect.Height / 4;
            int x3 = rect.Right - rect.Width / 5;
            int y3 = rect.Top + rect.Height / 4;
            e.Graphics.DrawLines(p, new[] { new Point(x1, y1), new Point(x2, y2), new Point(x3, y3) });
        }
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = e.Item.Enabled ? _theme.ForeColor : _theme.EmptyTextColor;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var p = new Pen(_theme.BorderColor);
        int y = e.Item.Height / 2;
        e.Graphics.DrawLine(p, 4, y, e.Item.Width - 4, y);
    }

    private static Color EnsureReadable(Color text, Color back)
    {
        if (back == Color.Empty || back == Color.Transparent) return text;
        double tl = (0.299 * text.R + 0.587 * text.G + 0.114 * text.B) / 255d;
        double bl = (0.299 * back.R + 0.587 * back.G + 0.114 * back.B) / 255d;
        if (Math.Abs(tl - bl) >= 0.38d) return text;
        return bl < 0.50d ? Color.White : Color.FromArgb(32, 36, 42);
    }

    private static Color Blend(Color first, Color second, double amount)
    {
        amount = Math.Max(0, Math.Min(1, amount));
        int r = (int)Math.Round(first.R + (second.R - first.R) * amount);
        int g = (int)Math.Round(first.G + (second.G - first.G) * amount);
        int b = (int)Math.Round(first.B + (second.B - first.B) * amount);
        return Color.FromArgb(r, g, b);
    }
}

internal sealed class SmartMenuColorTable : ProfessionalColorTable
{
    private readonly PanoTheme _theme;
    public SmartMenuColorTable(PanoTheme theme) => _theme = theme;

    public override Color ToolStripDropDownBackground => _theme.PanelBackColor;
    public override Color ImageMarginGradientBegin => _theme.PanelBackColor;
    public override Color ImageMarginGradientMiddle => _theme.PanelBackColor;
    public override Color ImageMarginGradientEnd => _theme.PanelBackColor;
    public override Color MenuBorder => _theme.BorderColor;
    public override Color MenuItemBorder => _theme.AccentColor;
    public override Color MenuItemSelected => _theme.HotBackColor;
    public override Color MenuItemSelectedGradientBegin => _theme.HotBackColor;
    public override Color MenuItemSelectedGradientEnd => _theme.HotBackColor;
    public override Color MenuItemPressedGradientBegin => _theme.SelectionBackColor;
    public override Color MenuItemPressedGradientMiddle => _theme.SelectionBackColor;
    public override Color MenuItemPressedGradientEnd => _theme.SelectionBackColor;
    public override Color CheckBackground => _theme.SelectionBackColor;
    public override Color CheckSelectedBackground => _theme.SelectionBackColor;
    public override Color CheckPressedBackground => _theme.SelectionBackColor;
    public override Color SeparatorDark => _theme.BorderColor;
    public override Color SeparatorLight => _theme.BorderColor;
}

internal static class GraphicsRoundedRectangleExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        if (radius <= 0)
        {
            graphics.FillRectangle(brush, bounds);
            return;
        }

        int diameter = radius * 2;
        using var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
