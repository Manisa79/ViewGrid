namespace Taylan.Pano.Core;

/// <summary>
/// Kart/Tile/Dashboard görünümlerinde uygulamanın satır bazlı görsel işaretler vermesi için genel model.
/// Ticket, stok, üretim, dosya, sipariş gibi farklı senaryolarda aynı renderer altyapısı kullanılabilir.
/// </summary>
public sealed class PanoCardVisualInfo
{
    public Color? AccentColor { get; set; }
    public PanoCardAccentMode AccentMode { get; set; } = PanoCardAccentMode.Auto;
    public Color? DotColor { get; set; }
    public bool? ShowDot { get; set; }
    public List<PanoCardBadge> Badges { get; } = new();
    public List<PanoCardAction> Actions { get; } = new();
}

public sealed class PanoCardBadge
{
    public string? Text { get; set; }
    public Image? Image { get; set; }
    public PanoCardGlyph Glyph { get; set; } = PanoCardGlyph.None;
    public Color? BackColor { get; set; }
    public Color? ForeColor { get; set; }
    public PanoCardBadgePlacement Placement { get; set; } = PanoCardBadgePlacement.TopRight;
    public string? ToolTipText { get; set; }
}

public enum PanoCardAccentMode
{
    Auto,
    None,
    TopBar,
    LeftBar,
    BottomBar,
    Outline,
    Glow
}

public enum PanoCardBadgePlacement
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public enum PanoCardGlyph
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
