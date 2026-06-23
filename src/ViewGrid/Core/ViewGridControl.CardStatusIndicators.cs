using System.ComponentModel;
using ViewGrid.Columns;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    private bool _cardStatusIndicators = true;
    private bool _cardStatusTopBar = true;
    private bool _cardStatusDot = true;
    private string _cardStatusAspectName = string.Empty;
    private int _cardStatusDotSize = 11;

    [Category("ViewGrid - Card Status")]
    [DefaultValue(true)]
    [DisplayName("Kart Durum Göstergeleri")]
    [Description("Card/Tile/Dashboard/Kanban görünümünde durum bilgisini ayrıca renkli nokta ve üst bar olarak çizer. Details hücre renderer'ına bağlı kalmaz.")]
    public bool CardStatusIndicators
    {
        get => _cardStatusIndicators;
        set
        {
            if (_cardStatusIndicators == value) return;
            _cardStatusIndicators = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - Card Status")]
    [DefaultValue(true)]
    [DisplayName("Kart Durum Üst Bar")]
    [Description("Card/Dashboard/Kanban kartlarının üst çizgisini durum rengine bağlar.")]
    public bool CardStatusTopBar
    {
        get => _cardStatusTopBar;
        set
        {
            if (_cardStatusTopBar == value) return;
            _cardStatusTopBar = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - Card Status")]
    [DefaultValue(true)]
    [DisplayName("Kart Durum Noktası")]
    [Description("Kart başlığının solunda durum renginde nokta çizer. Ticket/durum listelerinde Details dışındaki görünümlerde icon kaybını engeller.")]
    public bool CardStatusDot
    {
        get => _cardStatusDot;
        set
        {
            if (_cardStatusDot == value) return;
            _cardStatusDot = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - Card Status")]
    [DefaultValue(11)]
    [DisplayName("Kart Durum Nokta Boyutu")]
    public int CardStatusDotSize
    {
        get => _cardStatusDotSize;
        set
        {
            int newValue = Math.Clamp(value, 6, 24);
            if (_cardStatusDotSize == newValue) return;
            _cardStatusDotSize = newValue;
            Invalidate();
        }
    }

    [Category("ViewGrid - Card Status")]
    [DefaultValue("")]
    [DisplayName("Kart Durum AspectName")]
    [Description("Durum renginin okunacağı kolon AspectName değeri. Boş bırakılırsa Status/Durum/State/Stage gibi kolonlar otomatik bulunur.")]
    public string CardStatusAspectName
    {
        get => _cardStatusAspectName;
        set
        {
            string newValue = value ?? string.Empty;
            if (string.Equals(_cardStatusAspectName, newValue, StringComparison.Ordinal)) return;
            _cardStatusAspectName = newValue;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<object, Color?>? CardStatusColorGetter { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<object, object?>? CardStatusValueGetter { get; set; }

    private bool ShouldDrawCardStatusTopBar()
    {
        if (!CardStatusIndicators || !CardStatusTopBar) return false;
        return ViewMode is ViewGridMode.Tile
            or ViewGridMode.LargeCard
            or ViewGridMode.DashboardCard
            or ViewGridMode.RowCard
            or ViewGridMode.Kanban
            or ViewGridMode.Timeline
            or ViewGridMode.Poster;
    }

    private bool ShouldDrawCardStatusDot(Color? statusColor)
    {
        if (!CardStatusIndicators || !CardStatusDot || !statusColor.HasValue) return false;
        return ViewMode is ViewGridMode.Tile
            or ViewGridMode.LargeCard
            or ViewGridMode.DashboardCard
            or ViewGridMode.RowCard
            or ViewGridMode.Kanban
            or ViewGridMode.Timeline
            or ViewGridMode.Poster;
    }

    private Color? ResolveCardStatusColor(object row)
    {
        if (row == null) return null;

        try
        {
            Color? custom = CardStatusColorGetter?.Invoke(row);
            if (custom.HasValue && custom.Value != Color.Empty) return custom.Value;
        }
        catch
        {
            // Consumer delegates should not break ViewGrid painting.
        }

        object? statusValue = null;
        try
        {
            statusValue = CardStatusValueGetter?.Invoke(row);
        }
        catch
        {
            statusValue = null;
        }

        if (statusValue == null)
        {
            ViewGridColumn? col = FindCardStatusColumn();
            if (col != null)
            {
                try { statusValue = col.GetValue(row); }
                catch { statusValue = null; }
            }
        }

        if (statusValue is Color color && color != Color.Empty) return color;

        string text = Convert.ToString(statusValue) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return null;
        return ResolveCardSemanticStatusColor(text);
    }

    private ViewGridColumn? FindCardStatusColumn()
    {
        if (!string.IsNullOrWhiteSpace(CardStatusAspectName))
        {
            ViewGridColumn? exact = Columns.FirstOrDefault(c => string.Equals(c.AspectName, CardStatusAspectName, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;
        }

        string[] preferredAspects = { "Status", "Durum", "State", "Stage", "TicketStatus", "IssueStatus" };
        foreach (string aspect in preferredAspects)
        {
            ViewGridColumn? visibleExact = Columns.VisibleColumns.FirstOrDefault(c => string.Equals(c.AspectName, aspect, StringComparison.OrdinalIgnoreCase));
            if (visibleExact != null) return visibleExact;
        }

        foreach (string aspect in preferredAspects)
        {
            ViewGridColumn? anyExact = Columns.FirstOrDefault(c => string.Equals(c.AspectName, aspect, StringComparison.OrdinalIgnoreCase));
            if (anyExact != null) return anyExact;
        }

        return Columns.VisibleColumns.FirstOrDefault(c =>
            c.Header.Contains("Durum", StringComparison.OrdinalIgnoreCase) ||
            c.Header.Contains("Status", StringComparison.OrdinalIgnoreCase) ||
            c.Header.Contains("State", StringComparison.OrdinalIgnoreCase));
    }

    private Color ResolveCardSemanticStatusColor(string text)
    {
        string key = (text ?? string.Empty).Trim().ToUpperInvariant();

        if (key.Contains("BAKILIYOR") || key.Contains("BAKILIYO") || key.Contains("IN PROGRESS") || key.Contains("PROGRESS") || key.Contains("PROCESS") || key.Contains("AÇIK") || key.Contains("ACIK") || key.Contains("OPEN"))
            return Color.FromArgb(54, 158, 245);

        if (key.Contains("YENİ") || key.Contains("YENI") || key.Contains("NEW") || key.Contains("BEKLEYEN") || key.Contains("BEKLIYOR") || key.Contains("PENDING") || key.Contains("WAIT"))
            return Color.FromArgb(240, 190, 55);

        if (key.Contains("TAMAMLANDI") || key.Contains("TAMAM") || key.Contains("DONE") || key.Contains("RESOLVED") || key.Contains("ÇÖZ") || key.Contains("COZ") || key.Contains("OK") || key.Contains("PASS"))
            return Color.FromArgb(74, 190, 118);

        if (key.Contains("KAPALI") || key.Contains("CLOSED") || key.Contains("CLOSE") || key.Contains("İPTAL") || key.Contains("IPTAL") || key.Contains("CANCEL"))
            return Color.FromArgb(112, 122, 135);

        return ResolveSemanticStatusColor(text);
    }

    private bool TryDrawCardStatusDot(Graphics g, object row, Color statusColor, Color cardBack, ref int textX, ref int textW, int titleY)
    {
        if (textW <= CardStatusDotSize + 8) return false;

        int size = CardStatusDotSize;
        int dotX = textX;
        int dotY = titleY + Math.Max(3, (20 - size) / 2);
        var dotRect = new Rectangle(dotX, dotY, size, size);
        Color fill = EnsureVisibleAccent(statusColor, cardBack);
        Color ring = Blend(Color.White, fill, _theme.IsDark ? 0.35 : 0.55);

        using (var ringBrush = new SolidBrush(Color.FromArgb(_theme.IsDark ? 85 : 110, ring)))
            g.FillEllipse(ringBrush, Rectangle.Inflate(dotRect, 2, 2));
        using (var b = new SolidBrush(fill))
            g.FillEllipse(b, dotRect);
        using (var p = new Pen(EnsureVisibleStroke(fill, cardBack)))
            g.DrawEllipse(p, dotRect);

        int consume = size + 7;
        textX += consume;
        textW = Math.Max(20, textW - consume);
        return true;
    }
}
