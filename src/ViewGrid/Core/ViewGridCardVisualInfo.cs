namespace ViewGrid.Core;

/// <summary>
/// Kart/Tile/Dashboard görünümlerinde uygulamanın satır bazlı görsel işaretler vermesi için genel model.
/// Ticket, stok, üretim, dosya, sipariş gibi farklı senaryolarda aynı renderer altyapısı kullanılabilir.
/// </summary>
public sealed class ViewGridCardVisualInfo
{
    public Color? AccentColor { get; set; }
    public ViewGridCardAccentMode AccentMode { get; set; } = ViewGridCardAccentMode.Auto;
    public Color? DotColor { get; set; }
    public bool? ShowDot { get; set; }
    public List<ViewGridCardBadge> Badges { get; } = new();
    public List<ViewGridCardAction> Actions { get; } = new();
}

public sealed class ViewGridCardBadge
{
    public string? Text { get; set; }
    public Image? Image { get; set; }
    public ViewGridCardGlyph Glyph { get; set; } = ViewGridCardGlyph.None;
    public Color? BackColor { get; set; }
    public Color? ForeColor { get; set; }
    public ViewGridCardBadgePlacement Placement { get; set; } = ViewGridCardBadgePlacement.TopRight;
    public string? ToolTipText { get; set; }
}

public enum ViewGridCardAccentMode
{
    Auto,
    None,
    TopBar,
    LeftBar,
    BottomBar,
    Outline,
    Glow
}

public enum ViewGridCardBadgePlacement
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public enum ViewGridCardGlyph
{
    None,
    Info,
    Warning,
    Error,
    Success,
    Message,
    Attachment,
    Pin,
    Lock,
    Star,
    Check,
    Clock,
    Flag,
    Bell
}
