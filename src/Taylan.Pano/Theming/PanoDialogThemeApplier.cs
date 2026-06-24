using System.Drawing.Drawing2D;

namespace Taylan.Pano.Theming;

/// <summary>
/// Shared visual polish for Pano popup/dialog windows. Keeps helper windows consistent
/// with the active Pano theme without forcing every form to duplicate button/textbox styling.
/// </summary>
public static class PanoDialogThemeApplier
{
    public static void Apply(Control root, PanoTheme theme)
    {
        if (root is null) return;
        root.BackColor = NormalizePanelBack(theme);
        root.ForeColor = NormalizePanelFore(theme);
        root.Font = root.Font ?? new Font("Segoe UI", 9F);

        foreach (Control control in Enumerate(root))
            ApplyToControl(control, theme);
    }

    public static void ApplyToControl(Control control, PanoTheme theme)
    {
        control.Font = control.Font ?? new Font("Segoe UI", 9F);

        if (control is Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = theme.BorderColor;
            button.FlatAppearance.MouseOverBackColor = Blend(theme.ControlBackColor, theme.AccentColor, theme.IsDark ? 0.22 : 0.12);
            button.FlatAppearance.MouseDownBackColor = Blend(theme.ControlBackColor, theme.AccentColor, theme.IsDark ? 0.34 : 0.20);
            button.BackColor = NormalizeControlBack(theme);
            button.ForeColor = NormalizeControlFore(theme);
            button.UseVisualStyleBackColor = false;
            return;
        }

        if (control is TextBoxBase || control is ListBox || control is CheckedListBox || control is ComboBox)
        {
            control.BackColor = NormalizeControlBack(theme);
            control.ForeColor = NormalizeControlFore(theme);
            return;
        }

        if (control is ToolStrip toolStrip)
        {
            toolStrip.Renderer = new SmartMenuRenderer(theme);
            toolStrip.BackColor = NormalizePanelBack(theme);
            toolStrip.ForeColor = NormalizePanelFore(theme);
            return;
        }

        if (control is StatusStrip statusStrip)
        {
            statusStrip.Renderer = new SmartMenuRenderer(theme);
            statusStrip.BackColor = NormalizePanelBack(theme);
            statusStrip.ForeColor = EnsureReadableTextColor(theme.MutedForeColor, statusStrip.BackColor);
            return;
        }

        if (control is TreeView treeView)
        {
            treeView.BackColor = NormalizeControlBack(theme);
            treeView.ForeColor = NormalizeControlFore(theme);
            treeView.LineColor = theme.BorderColor;
            treeView.BorderStyle = BorderStyle.FixedSingle;
            return;
        }

        if (control is TabControl tabControl)
        {
            tabControl.BackColor = NormalizePanelBack(theme);
            tabControl.ForeColor = NormalizePanelFore(theme);
            return;
        }

        if (control is TabPage tabPage)
        {
            tabPage.BackColor = NormalizePanelBack(theme);
            tabPage.ForeColor = NormalizePanelFore(theme);
            return;
        }

        if (control is Panel || control is TableLayoutPanel || control is FlowLayoutPanel || control is GroupBox)
        {
            control.BackColor = NormalizePanelBack(theme);
            control.ForeColor = NormalizePanelFore(theme);
            return;
        }

        control.BackColor = NormalizePanelBack(theme);
        control.ForeColor = NormalizePanelFore(theme);
    }


    public static Color EnsureReadableTextColor(Color preferred, Color back)
    {
        if (preferred == Color.Empty || preferred == Color.Transparent)
            return IsDark(back) ? Color.WhiteSmoke : Color.FromArgb(32, 32, 32);

        double contrast = ContrastRatio(preferred, back);
        if (contrast >= 4.5d) return preferred;
        return IsDark(back) ? Color.WhiteSmoke : Color.FromArgb(32, 32, 32);
    }

    public static Color NormalizePanelBack(PanoTheme theme)
    {
        var back = theme.PanelBackColor == Color.Empty ? theme.BackColor : theme.PanelBackColor;
        if (back == Color.Empty || back == Color.Transparent)
            back = theme.IsDark ? Color.FromArgb(36, 36, 40) : Color.FromArgb(250, 251, 253);
        return back;
    }

    public static Color NormalizeControlBack(PanoTheme theme)
    {
        var back = theme.ControlBackColor == Color.Empty ? theme.TextBackColor : theme.ControlBackColor;
        if (back == Color.Empty || back == Color.Transparent)
            back = theme.IsDark ? Color.FromArgb(42, 42, 46) : Color.White;
        return back;
    }

    public static Color NormalizePanelFore(PanoTheme theme)
        => EnsureReadableTextColor(theme.ForeColor, NormalizePanelBack(theme));

    public static Color NormalizeControlFore(PanoTheme theme)
        => EnsureReadableTextColor(theme.ForeColor, NormalizeControlBack(theme));

    private static bool IsDark(Color color)
    {
        if (color == Color.Empty || color == Color.Transparent) return false;
        return (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) < 140;
    }

    private static double ContrastRatio(Color a, Color b)
    {
        static double Channel(double c)
        {
            c /= 255d;
            return c <= 0.03928d ? c / 12.92d : Math.Pow((c + 0.055d) / 1.055d, 2.4d);
        }

        double la = 0.2126d * Channel(a.R) + 0.7152d * Channel(a.G) + 0.0722d * Channel(a.B);
        double lb = 0.2126d * Channel(b.R) + 0.7152d * Channel(b.G) + 0.0722d * Channel(b.B);
        double lighter = Math.Max(la, lb);
        double darker = Math.Min(la, lb);
        return (lighter + 0.05d) / (darker + 0.05d);
    }

    public static void PaintRoundedPanel(Graphics g, Rectangle bounds, PanoTheme theme, int radius = 8)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var path = RoundedRect(bounds, radius);
        using var fill = new SolidBrush(theme.PanelBackColor);
        using var border = new Pen(theme.BorderColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.FillPath(fill, path);
        g.DrawPath(border, path);
    }

    public static Color Blend(Color first, Color second, double amount)
    {
        amount = Math.Max(0, Math.Min(1, amount));
        int r = (int)Math.Round(first.R + (second.R - first.R) * amount);
        int g = (int)Math.Round(first.G + (second.G - first.G) * amount);
        int b = (int)Math.Round(first.B + (second.B - first.B) * amount);
        return Color.FromArgb(r, g, b);
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

    private static IEnumerable<Control> Enumerate(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Enumerate(child))
                yield return nested;
        }
    }
}
