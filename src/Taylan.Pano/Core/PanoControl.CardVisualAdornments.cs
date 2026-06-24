using System.ComponentModel;
using Taylan.Pano.Columns;

namespace Taylan.Pano.Core;

public partial class PanoControl
{
    private bool _cardVisualAdornments = true;
    private bool _cardAutoBadgesFromBadgeColumns = true;
    private int _cardBadgeSize = 24;
    private int _cardBadgeMaxCount = 4;
    private PanoCardAccentMode _cardDefaultAccentMode = PanoCardAccentMode.Auto;

    [Category("Pano - Card Visuals")]
    [DefaultValue(true)]
    [DisplayName("Kart Görsel Eklentileri")]
    [Description("Card/Tile/Dashboard/Kanban görünümlerinde durum barı, corner badge, ikon rozeti ve benzeri satır bazlı görsel eklentileri çizer.")]
    public bool CardVisualAdornments
    {
        get => _cardVisualAdornments;
        set
        {
            if (_cardVisualAdornments == value) return;
            _cardVisualAdornments = value;
            Invalidate();
        }
    }

    [Category("Pano - Card Visuals")]
    [DefaultValue(true)]
    [DisplayName("Badge Kolonlarından Otomatik Rozet")]
    [Description("Kind=Badge olan görünür kolonları Card/Tile/Dashboard üzerinde otomatik küçük rozet olarak gösterir.")]
    public bool CardAutoBadgesFromBadgeColumns
    {
        get => _cardAutoBadgesFromBadgeColumns;
        set
        {
            if (_cardAutoBadgesFromBadgeColumns == value) return;
            _cardAutoBadgesFromBadgeColumns = value;
            Invalidate();
        }
    }

    [Category("Pano - Card Visuals")]
    [DefaultValue(24)]
    [DisplayName("Kart Badge Boyutu")]
    public int CardBadgeSize
    {
        get => _cardBadgeSize;
        set
        {
            int newValue = Math.Clamp(value, 16, 42);
            if (_cardBadgeSize == newValue) return;
            _cardBadgeSize = newValue;
            Invalidate();
        }
    }

    [Category("Pano - Card Visuals")]
    [DefaultValue(4)]
    [DisplayName("Kart Maksimum Badge")]
    public int CardBadgeMaxCount
    {
        get => _cardBadgeMaxCount;
        set
        {
            int newValue = Math.Clamp(value, 0, 12);
            if (_cardBadgeMaxCount == newValue) return;
            _cardBadgeMaxCount = newValue;
            Invalidate();
        }
    }

    [Category("Pano - Card Visuals")]
    [DefaultValue(PanoCardAccentMode.Auto)]
    [DisplayName("Varsayılan Kart Accent")]
    [Description("Kart görsel bilgisi özel AccentMode vermezse kullanılacak varsayılan accent çizimi.")]
    public PanoCardAccentMode CardDefaultAccentMode
    {
        get => _cardDefaultAccentMode;
        set
        {
            if (_cardDefaultAccentMode == value) return;
            _cardDefaultAccentMode = value;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<object, PanoCardVisualInfo?>? CardVisualInfoGetter { get; set; }

    private PanoCardVisualInfo? ResolveCardVisualInfo(object row, Color? statusColor)
    {
        if (!CardVisualAdornments || row == null) return null;

        PanoCardVisualInfo? info = null;
        try
        {
            info = CardVisualInfoGetter?.Invoke(row);
        }
        catch
        {
            info = null;
        }

        if (info == null)
        {
            info = new PanoCardVisualInfo();
        }

        if (!info.AccentColor.HasValue && statusColor.HasValue)
        {
            info.AccentColor = statusColor.Value;
        }

        if (!info.DotColor.HasValue && statusColor.HasValue)
        {
            info.DotColor = statusColor.Value;
        }

        if (CardAutoBadgesFromBadgeColumns && info.Badges.Count < CardBadgeMaxCount)
        {
            AddAutoBadgesFromColumns(row, info);
        }

        bool hasVisual = info.AccentColor.HasValue || info.DotColor.HasValue || info.Badges.Count > 0 || info.ShowDot.HasValue;
        return hasVisual ? info : null;
    }

    private void AddAutoBadgesFromColumns(object row, PanoCardVisualInfo info)
    {
        if (CardBadgeMaxCount <= 0) return;
        int available = Math.Max(0, CardBadgeMaxCount - info.Badges.Count);
        if (available <= 0) return;

        foreach (PanoColumn col in Columns.VisibleColumns.Where(c => c.Kind == PanoColumnKind.Badge).Take(available))
        {
            object? value = null;
            try { value = col.GetValue(row); } catch { value = null; }
            string text = Convert.ToString(value) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) continue;

            Color badgeColor = ResolveSemanticStatusColor(text);
            info.Badges.Add(new PanoCardBadge
            {
                Text = text,
                BackColor = badgeColor,
                ForeColor = BestTextOn(badgeColor, _theme.ForeColor),
                Placement = PanoCardBadgePlacement.BottomRight
            });
        }
    }

    private bool ShouldDrawCardVisualAccent(PanoCardVisualInfo? info, Color? statusColor)
    {
        if (!CardVisualAdornments) return false;
        if (info != null && info.AccentColor.HasValue) return true;
        return ShouldDrawCardStatusTopBar() && statusColor.HasValue;
    }

    private Color? ResolveCardDotColor(PanoCardVisualInfo? info, Color? statusColor)
    {
        if (info?.ShowDot == false) return null;
        if (info != null && info.DotColor.HasValue) return info.DotColor.Value;
        return statusColor;
    }

    private void DrawCardAccentAdorner(Graphics g, Rectangle r, Color cardBack, bool isSelected, PanoCardVisualInfo? info, Color? statusColor)
    {
        Color? rawColor = info != null && info.AccentColor.HasValue ? info.AccentColor : statusColor;
        if (!rawColor.HasValue || rawColor.Value == Color.Empty) return;

        Color color = EnsureVisibleAccent(rawColor.Value, cardBack);
        PanoCardAccentMode mode = info != null ? info.AccentMode : PanoCardAccentMode.Auto;
        if (mode == PanoCardAccentMode.Auto) mode = CardDefaultAccentMode;
        if (mode == PanoCardAccentMode.Auto)
        {
            mode = ViewMode switch
            {
                PanoViewMode.RowCard or PanoViewMode.Timeline => PanoCardAccentMode.LeftBar,
                PanoViewMode.Poster or PanoViewMode.IconGrid => PanoCardAccentMode.Outline,
                _ => PanoCardAccentMode.TopBar
            };
        }
        if (mode == PanoCardAccentMode.None) return;

        Color paint = isSelected ? color : Blend(color, cardBack, _theme.IsDark ? 0.25 : 0.10);
        switch (mode)
        {
            case PanoCardAccentMode.LeftBar:
                using (var b = new SolidBrush(paint))
                    g.FillRoundedRectangle(b, new Rectangle(r.Left + 1, r.Top + 1, 5, Math.Max(8, r.Height - 2)), Math.Max(4, _theme.CornerRadius - 2));
                break;
            case PanoCardAccentMode.BottomBar:
                using (var b = new SolidBrush(paint))
                    g.FillRoundedRectangle(b, new Rectangle(r.Left + 1, r.Bottom - 6, Math.Max(8, r.Width - 2), 5), Math.Max(4, _theme.CornerRadius - 2));
                break;
            case PanoCardAccentMode.Outline:
                using (var p = new Pen(paint, isSelected ? 2.4f : 1.6f))
                    g.DrawRoundedRectangle(p, Rectangle.Inflate(r, -3, -3), Math.Max(6, _theme.CornerRadius - 1));
                break;
            case PanoCardAccentMode.Glow:
                using (var glow = new SolidBrush(Color.FromArgb(_theme.IsDark ? 45 : 34, color)))
                    g.FillRoundedRectangle(glow, Rectangle.Inflate(r, -3, -3), Math.Max(6, _theme.CornerRadius - 1));
                using (var p = new Pen(Color.FromArgb(_theme.IsDark ? 130 : 105, color), 1.4f))
                    g.DrawRoundedRectangle(p, Rectangle.Inflate(r, -2, -2), Math.Max(6, _theme.CornerRadius - 1));
                break;
            case PanoCardAccentMode.TopBar:
            default:
                using (var b = new SolidBrush(paint))
                    g.FillRoundedRectangle(b, new Rectangle(r.Left + 1, r.Top + 1, Math.Max(8, r.Width - 2), 5), Math.Max(4, _theme.CornerRadius - 2));
                break;
        }
    }

    private void DrawCardBadges(Graphics g, Rectangle cardBounds, PanoCardVisualInfo? info, Color cardBack)
    {
        if (!CardVisualAdornments || info == null || info.Badges.Count == 0 || CardBadgeMaxCount <= 0) return;

        int size = CardBadgeSize;
        int gap = 5;
        int margin = 8;
        Dictionary<PanoCardBadgePlacement, int> offsets = new();

        foreach (PanoCardBadge badge in info.Badges.Take(CardBadgeMaxCount))
        {
            PanoCardBadgePlacement placement = badge.Placement;
            offsets.TryGetValue(placement, out int offset);
            Rectangle br = GetBadgeRect(cardBounds, placement, size, margin, offset);
            offsets[placement] = offset + size + gap;
            DrawCardBadge(g, br, badge, cardBack);
        }
    }

    private static Rectangle GetBadgeRect(Rectangle cardBounds, PanoCardBadgePlacement placement, int size, int margin, int offset)
    {
        return placement switch
        {
            PanoCardBadgePlacement.TopLeft => new Rectangle(cardBounds.Left + margin + offset, cardBounds.Top + margin, size, size),
            PanoCardBadgePlacement.BottomLeft => new Rectangle(cardBounds.Left + margin + offset, cardBounds.Bottom - margin - size, size, size),
            PanoCardBadgePlacement.BottomRight => new Rectangle(cardBounds.Right - margin - size - offset, cardBounds.Bottom - margin - size, size, size),
            _ => new Rectangle(cardBounds.Right - margin - size - offset, cardBounds.Top + margin, size, size)
        };
    }

    private void DrawCardBadge(Graphics g, Rectangle r, PanoCardBadge badge, Color cardBack)
    {
        Color rawBack = badge.BackColor ?? _theme.AccentColor;
        Color back = EnsureVisibleAccent(rawBack, cardBack);
        Color fore = badge.ForeColor ?? BestTextOn(back, _theme.ForeColor);

        using (var shadow = new SolidBrush(Color.FromArgb(_theme.IsDark ? 70 : 42, Color.Black)))
            g.FillEllipse(shadow, new Rectangle(r.X + 1, r.Y + 2, r.Width, r.Height));
        using (var b = new SolidBrush(back))
            g.FillEllipse(b, r);
        using (var p = new Pen(EnsureVisibleStroke(back, cardBack)))
            g.DrawEllipse(p, r);

        Rectangle inner = Rectangle.Inflate(r, -5, -5);
        if (badge.Image != null)
        {
            g.DrawImage(badge.Image, inner);
            return;
        }

        if (badge.Glyph != PanoCardGlyph.None)
        {
            DrawCardGlyph(g, inner, badge.Glyph, fore);
            return;
        }

        string text = badge.Text ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;
        using Font font = new Font(Font.FontFamily, Math.Max(7f, Math.Min(9f, Font.Size - 1f)), FontStyle.Bold);
        TextRenderer.DrawText(g, ShortBadgeText(text), font, r, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static string ShortBadgeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = text.Trim();
        return text.Length <= 3 ? text : text.Substring(0, 3).ToUpperInvariant();
    }

    private void DrawCardGlyph(Graphics g, Rectangle r, PanoCardGlyph glyph, Color fore)
    {
        var old = g.SmoothingMode;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using Pen p = new Pen(fore, 1.7f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round, LineJoin = System.Drawing.Drawing2D.LineJoin.Round };
        using SolidBrush b = new SolidBrush(fore);
        int cx = r.Left + r.Width / 2;
        int cy = r.Top + r.Height / 2;

        switch (glyph)
        {
            case PanoCardGlyph.Warning:
                Point[] tri = { new Point(cx, r.Top), new Point(r.Right, r.Bottom), new Point(r.Left, r.Bottom) };
                g.DrawPolygon(p, tri);
                g.DrawLine(p, cx, r.Top + 4, cx, r.Bottom - 5);
                g.FillEllipse(b, cx - 1, r.Bottom - 3, 2, 2);
                break;
            case PanoCardGlyph.Error:
                g.DrawLine(p, r.Left + 2, r.Top + 2, r.Right - 2, r.Bottom - 2);
                g.DrawLine(p, r.Right - 2, r.Top + 2, r.Left + 2, r.Bottom - 2);
                break;
            case PanoCardGlyph.Success:
            case PanoCardGlyph.Check:
                g.DrawLines(p, new[] { new Point(r.Left + 1, cy), new Point(cx - 2, r.Bottom - 2), new Point(r.Right - 1, r.Top + 2) });
                break;
            case PanoCardGlyph.Message:
                g.DrawRoundedRectangle(p, new Rectangle(r.Left + 1, r.Top + 2, r.Width - 2, r.Height - 5), 3);
                g.DrawLine(p, r.Left + 5, r.Bottom - 3, r.Left + 3, r.Bottom);
                break;
            case PanoCardGlyph.Attachment:
                g.DrawArc(p, r.Left + 2, r.Top + 1, r.Width - 5, r.Height - 3, 90, 290);
                g.DrawArc(p, r.Left + 5, r.Top + 4, r.Width - 10, r.Height - 8, 90, 260);
                break;
            case PanoCardGlyph.Pin:
                g.DrawLine(p, cx, r.Top + 1, cx, r.Bottom - 1);
                g.DrawLine(p, r.Left + 3, r.Top + 5, r.Right - 3, r.Top + 5);
                g.FillEllipse(b, cx - 3, r.Top + 1, 6, 6);
                break;
            case PanoCardGlyph.Lock:
                g.DrawRectangle(p, r.Left + 2, cy - 1, r.Width - 4, r.Height / 2);
                g.DrawArc(p, r.Left + 4, r.Top + 1, r.Width - 8, r.Height - 4, 180, 180);
                break;
            case PanoCardGlyph.Star:
                DrawStar(g, p, new Rectangle(r.Left + 1, r.Top + 1, r.Width - 2, r.Height - 2));
                break;
            case PanoCardGlyph.Clock:
                g.DrawEllipse(p, r);
                g.DrawLine(p, cx, cy, cx, r.Top + 3);
                g.DrawLine(p, cx, cy, r.Right - 3, cy + 2);
                break;
            case PanoCardGlyph.Flag:
                g.DrawLine(p, r.Left + 3, r.Top + 1, r.Left + 3, r.Bottom - 1);
                g.DrawLines(p, new[] { new Point(r.Left + 4, r.Top + 2), new Point(r.Right - 2, r.Top + 4), new Point(r.Left + 4, cy + 1) });
                break;
            case PanoCardGlyph.Bell:
                g.DrawArc(p, r.Left + 2, r.Top + 2, r.Width - 4, r.Height - 3, 200, 140);
                g.DrawLine(p, r.Left + 3, r.Bottom - 4, r.Right - 3, r.Bottom - 4);
                g.FillEllipse(b, cx - 2, r.Bottom - 2, 4, 3);
                break;
            case PanoCardGlyph.Info:
            default:
                g.DrawEllipse(p, r);
                g.DrawLine(p, cx, cy - 1, cx, r.Bottom - 3);
                g.FillEllipse(b, cx - 1, r.Top + 3, 2, 2);
                break;
        }

        g.SmoothingMode = old;
    }

    private static void DrawStar(Graphics g, Pen p, Rectangle r)
    {
        double a = -Math.PI / 2;
        Point[] pts = new Point[10];
        int cx = r.Left + r.Width / 2;
        int cy = r.Top + r.Height / 2;
        double outer = Math.Min(r.Width, r.Height) / 2.0;
        double inner = outer * 0.45;
        for (int i = 0; i < pts.Length; i++)
        {
            double radius = i % 2 == 0 ? outer : inner;
            pts[i] = new Point((int)Math.Round(cx + Math.Cos(a) * radius), (int)Math.Round(cy + Math.Sin(a) * radius));
            a += Math.PI / 5;
        }
        g.DrawPolygon(p, pts);
    }

    private void DrawCardActions(Graphics g, Rectangle cardBounds, PanoCardVisualInfo? info, Color cardBack)
    {
        if (!CardVisualAdornments || info == null || info.Actions.Count == 0) return;

        int size = Math.Max(18, Math.Min(34, CardBadgeSize));
        int gap = 5;
        int margin = 8;
        Dictionary<PanoCardActionPlacement, int> offsets = new();

        foreach (PanoCardAction action in info.Actions.Where(a => a.Visible).Take(6))
        {
            offsets.TryGetValue(action.Placement, out int offset);
            Rectangle ar = GetActionRect(cardBounds, action.Placement, size, margin, offset);
            offsets[action.Placement] = offset + size + gap;
            DrawCardAction(g, ar, action, cardBack);
        }
    }

    private static Rectangle GetActionRect(Rectangle cardBounds, PanoCardActionPlacement placement, int size, int margin, int offset)
    {
        return placement switch
        {
            PanoCardActionPlacement.TopLeft => new Rectangle(cardBounds.Left + margin + offset, cardBounds.Top + margin, size, size),
            PanoCardActionPlacement.BottomLeft => new Rectangle(cardBounds.Left + margin + offset, cardBounds.Bottom - margin - size, size, size),
            PanoCardActionPlacement.BottomRight => new Rectangle(cardBounds.Right - margin - size - offset, cardBounds.Bottom - margin - size, size, size),
            _ => new Rectangle(cardBounds.Right - margin - size - offset, cardBounds.Top + margin, size, size)
        };
    }

    private void DrawCardAction(Graphics g, Rectangle r, PanoCardAction action, Color cardBack)
    {
        Color rawBack = action.BackColor ?? Blend(_theme.AccentColor, cardBack, _theme.IsDark ? 0.35 : 0.20);
        Color back = EnsureVisibleAccent(rawBack, cardBack);
        Color fore = action.ForeColor ?? BestTextOn(back, _theme.ForeColor);

        using (var b = new SolidBrush(Color.FromArgb(_theme.IsDark ? 210 : 235, back)))
            g.FillRoundedRectangle(b, r, Math.Max(5, _theme.CornerRadius - 3));
        using (var p = new Pen(EnsureVisibleStroke(back, cardBack)))
            g.DrawRoundedRectangle(p, r, Math.Max(5, _theme.CornerRadius - 3));

        Rectangle inner = Rectangle.Inflate(r, -5, -5);
        if (action.Image != null)
        {
            g.DrawImage(action.Image, inner);
            return;
        }

        if (action.Glyph != PanoCardGlyph.None)
        {
            DrawCardGlyph(g, inner, action.Glyph, fore);
            return;
        }

        string text = action.Text ?? action.Key;
        if (string.IsNullOrWhiteSpace(text)) return;
        using Font font = new Font(Font.FontFamily, Math.Max(7f, Math.Min(9f, Font.Size - 1f)), FontStyle.Bold);
        TextRenderer.DrawText(g, ShortBadgeText(text), font, r, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private bool TryProcessCardActionClick(Point location, int rowIndex, object row)
    {
        if (!IsTileView || !CardVisualAdornments) return false;
        Color? cardStatusColor = ResolveCardStatusColor(row);
        PanoCardVisualInfo? info = ResolveCardVisualInfo(row, cardStatusColor);
        if (info == null || info.Actions.Count == 0) return false;

        PanoColumn? first = Columns.VisibleColumns.FirstOrDefault();
        if (first == null) return false;
        Rectangle cardBounds = GetCellBounds(rowIndex, first);
        if (cardBounds.IsEmpty) return false;

        int size = Math.Max(18, Math.Min(34, CardBadgeSize));
        int gap = 5;
        int margin = 8;
        Dictionary<PanoCardActionPlacement, int> offsets = new();

        foreach (PanoCardAction action in info.Actions.Where(a => a.Visible).Take(6))
        {
            offsets.TryGetValue(action.Placement, out int offset);
            Rectangle ar = GetActionRect(cardBounds, action.Placement, size, margin, offset);
            offsets[action.Placement] = offset + size + gap;
            if (!ar.Contains(location)) continue;

            action.Click?.Invoke(row);
            CardActionClick?.Invoke(this, new PanoCardActionClickEventArgs(row, action));
            return true;
        }

        return false;
    }

}
