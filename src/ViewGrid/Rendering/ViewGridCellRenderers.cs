using System.ComponentModel;
using System.Drawing;
using ViewGrid.Columns;
using ViewGrid.Theming;

namespace ViewGrid.Rendering;

/// <summary>
/// v27 renderer altyapısının hafif çekirdeği. Mevcut çizim motorunu bozmadan kolonlara anlamlı görsel profil bağlamak için kullanılır.
/// Sonraki adımda ViewGridControl paint akışına tam entegre edilebilir; bugün MasterData/AOI presetlerinde güvenli metadata sağlar.
/// </summary>
public interface IViewGridCellRenderer
{
    string Name { get; }
    void Render(Graphics graphics, Rectangle bounds, object? value, ViewGridColumn column, ViewGridTheme theme, bool selected, bool hot);
}

public enum ViewGridCellVisualProfile
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

public static class ViewGridCellVisualProfileExtensions
{
    public static ViewGridColumn ApplyVisualProfile(this ViewGridColumn column, ViewGridCellVisualProfile profile)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));

        column.Tag = profile;
        switch (profile)
        {
            case ViewGridCellVisualProfile.Badge:
            case ViewGridCellVisualProfile.WarningStatus:
                column.Kind = ViewGridColumnKind.Badge;
                break;
            case ViewGridCellVisualProfile.Progress:
                column.Kind = ViewGridColumnKind.ProgressBar;
                break;
            case ViewGridCellVisualProfile.Hyperlink:
                column.Kind = ViewGridColumnKind.Hyperlink;
                break;
            case ViewGridCellVisualProfile.ActionButton:
                column.Kind = ViewGridColumnKind.Button;
                break;
        }

        return column;
    }
}

public sealed class ViewGridRendererRegistry
{
    private readonly Dictionary<string, IViewGridCellRenderer> _renderers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IViewGridCellRenderer renderer)
    {
        if (renderer == null) throw new ArgumentNullException(nameof(renderer));
        _renderers[renderer.Name] = renderer;
    }

    public bool TryGet(string name, out IViewGridCellRenderer renderer)
        => _renderers.TryGetValue(name ?? string.Empty, out renderer!);
}

public sealed class ViewGridBadgeRenderer : IViewGridCellRenderer
{
    public string Name => "Badge";

    public void Render(Graphics graphics, Rectangle bounds, object? value, ViewGridColumn column, ViewGridTheme theme, bool selected, bool hot)
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

internal static class ViewGridRendererDrawingExtensions
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
