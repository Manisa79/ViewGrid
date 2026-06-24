using System.ComponentModel;
using System.Drawing;
using Taylan.Pano.Columns;
using Taylan.Pano.Theming;

namespace Taylan.Pano.Rendering;

/// <summary>
/// v27 renderer altyapısının hafif çekirdeği. Mevcut çizim motorunu bozmadan kolonlara anlamlı görsel profil bağlamak için kullanılır.
/// Sonraki adımda PanoControl paint akışına tam entegre edilebilir; bugün MasterData/AOI presetlerinde güvenli metadata sağlar.
/// </summary>
public interface IPanoCellRenderer
{
    string Name { get; }
    void Render(Graphics graphics, Rectangle bounds, object? value, PanoColumn column, PanoTheme theme, bool selected, bool hot);
}

public enum PanoCellVisualProfile
{
    Default,
    Badge,
    IconText,
    Progress,
    Tags,
    Hyperlink,
    ActionButton,
    WarningStatus
}

public static class PanoCellVisualProfileExtensions
{
    public static PanoColumn ApplyVisualProfile(this PanoColumn column, PanoCellVisualProfile profile)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));

        column.Tag = profile;
        switch (profile)
        {
            case PanoCellVisualProfile.Badge:
            case PanoCellVisualProfile.WarningStatus:
                column.Kind = PanoColumnKind.Badge;
                break;
            case PanoCellVisualProfile.Progress:
                column.Kind = PanoColumnKind.ProgressBar;
                break;
            case PanoCellVisualProfile.Hyperlink:
                column.Kind = PanoColumnKind.Hyperlink;
                break;
            case PanoCellVisualProfile.ActionButton:
                column.Kind = PanoColumnKind.Button;
                break;
        }

        return column;
    }
}

public sealed class PanoRendererRegistry
{
    private readonly Dictionary<string, IPanoCellRenderer> _renderers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IPanoCellRenderer renderer)
    {
        if (renderer == null) throw new ArgumentNullException(nameof(renderer));
        _renderers[renderer.Name] = renderer;
    }

    public bool TryGet(string name, out IPanoCellRenderer renderer)
        => _renderers.TryGetValue(name ?? string.Empty, out renderer!);
}

public sealed class PanoBadgeRenderer : IPanoCellRenderer
{
    public string Name => "Badge";

    public void Render(Graphics graphics, Rectangle bounds, object? value, PanoColumn column, PanoTheme theme, bool selected, bool hot)
    {
        string text = Convert.ToString(value) ?? string.Empty;
        using var brush = new SolidBrush(selected ? theme.SelectionBackColor : theme.PanelBackColor);
        using var fore = new SolidBrush(selected ? theme.SelectionForeColor : theme.ForeColor);
        using var pen = new Pen(theme.GridColor);
        Rectangle pill = Rectangle.Inflate(bounds, -6, -5);
        graphics.FillRoundedRectangle(brush, pill, 10);
        graphics.DrawRoundedRectangle(pen, pill, 10);
        TextRenderer.DrawText(graphics, text, SystemFonts.MessageBoxFont, pill, fore.Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal static class PanoRendererDrawingExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedPath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedPath(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        int d = Math.Max(1, radius * 2);
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
