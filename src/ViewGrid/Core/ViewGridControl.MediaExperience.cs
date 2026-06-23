namespace ViewGrid.Core;

public partial class ViewGridControl
{
    [Category("ViewGrid - Media Experience")]
    [DefaultValue(true)]
    [Description("Poster/Gallery/MediaTile/FilmStrip görünümlerinde görsellerin cache/lazy-load senaryolarına hazır şekilde yönetileceğini belirtir. Mevcut ImageGetter çıktısı bozulmadan çalışır.")]
    public bool EnableMediaImageCache { get; set; } = true;

    [Category("ViewGrid - Media Experience")]
    [DefaultValue(true)]
    [Description("Medya görsellerinin ihtiyaç oldukça yüklenmesi için host uygulamaya niyet bildirir. Audix gibi büyük arşivlerde ImageGetter içinde disk cache ile birlikte kullanılabilir.")]
    public bool EnableMediaLazyLoading { get; set; } = true;

    [Category("ViewGrid - Media Experience")]
    [DefaultValue(null)]
    [Description("ImageGetter null döndürürse Poster/Gallery/MediaTile/FilmStrip alanında gösterilecek varsayılan kapak/placeholder görseli.")]
    public Image? MediaPlaceholderImage { get; set; }

    [Category("ViewGrid - Media Experience")]
    [DefaultValue(false)]
    [Description("Medya kartı hover/selected olduğunda görselin üzerinde küçük play/action düğmesi gösterir.")]
    public bool ShowMediaOverlayButton { get; set; }

    [Category("ViewGrid - Media Experience")]
    [DefaultValue("▶")]
    [Description("Medya kartı overlay düğmesinde gösterilecek kısa metin veya ikon karakteri.")]
    public string MediaOverlayButtonText { get; set; } = "▶";

    [Category("ViewGrid - Media Experience")]
    [DefaultValue(true)]
    [Description("Medya görselinin üzerinde FLAC, MP3, 320kbps, FAIL, OK gibi kalite/durum rozeti gösterir.")]
    public bool ShowMediaQualityBadge { get; set; } = true;

    [Category("ViewGrid - Media Experience")]
    [DefaultValue("Quality")]
    [Description("Medya rozeti için okunacak property/kolon adı. Örn: Quality, Format, Bitrate, State.")]
    public string MediaQualityBadgeAspectName { get; set; } = "Quality";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<object, string?>? MediaQualityBadgeGetter { get; set; }

    private string ResolveMediaQualityBadge(object row)
    {
        if (!ShowMediaQualityBadge || row == null) return string.Empty;
        string? value = MediaQualityBadgeGetter?.Invoke(row);
        if (!string.IsNullOrWhiteSpace(value)) return value.Trim();

        if (!string.IsNullOrWhiteSpace(MediaQualityBadgeAspectName))
        {
            var col = Columns.FirstOrDefault(c =>
                string.Equals(c.AspectName, MediaQualityBadgeAspectName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Header, MediaQualityBadgeAspectName, StringComparison.OrdinalIgnoreCase));
            if (col != null)
            {
                value = Convert.ToString(col.GetValue(row));
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            }

            var prop = row.GetType().GetProperty(MediaQualityBadgeAspectName);
            if (prop != null)
            {
                value = Convert.ToString(prop.GetValue(row));
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            }
        }

        return string.Empty;
    }

    private void DrawMediaQualityBadge(Graphics g, Rectangle imageBounds, object row, Color cardBack)
    {
        string badge = ResolveMediaQualityBadge(row);
        if (string.IsNullOrWhiteSpace(badge)) return;

        Size textSize = TextRenderer.MeasureText(badge, Font, Size.Empty, TextFormatFlags.NoPadding);
        int w = Math.Min(imageBounds.Width - 12, Math.Max(38, textSize.Width + 18));
        if (w <= 12) return;

        var rect = new Rectangle(imageBounds.Right - w - 8, imageBounds.Top + 8, w, 23);
        Color badgeBack = Color.FromArgb(_theme.IsDark ? 220 : 235, _theme.IsDark ? Color.FromArgb(20, 24, 30) : Color.White);
        Color badgeFore = EnsureReadableTextOn(badgeBack, _theme.AccentColor);
        using var brush = new SolidBrush(badgeBack);
        g.FillRoundedRectangle(brush, rect, 10);
        using var pen = new Pen(Color.FromArgb(_theme.IsDark ? 120 : 90, _theme.BorderColor));
        g.DrawRoundedRectangle(pen, rect, 10);
        TextRenderer.DrawText(g, badge, Font, rect, badgeFore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private void DrawMediaOverlayButton(Graphics g, Rectangle imageBounds, Color cardBack)
    {
        if (!ShowMediaOverlayButton || imageBounds.Width < 48 || imageBounds.Height < 48) return;
        string text = string.IsNullOrWhiteSpace(MediaOverlayButtonText) ? "▶" : MediaOverlayButtonText.Trim();
        int size = Math.Min(54, Math.Max(38, Math.Min(imageBounds.Width, imageBounds.Height) / 4));
        var rect = new Rectangle(imageBounds.Left + (imageBounds.Width - size) / 2, imageBounds.Top + (imageBounds.Height - size) / 2, size, size);
        Color back = Color.FromArgb(_theme.IsDark ? 210 : 225, _theme.IsDark ? Color.Black : Color.White);
        Color fore = EnsureReadableTextOn(back, _theme.AccentColor);
        using var brush = new SolidBrush(back);
        g.FillEllipse(brush, rect);
        using var pen = new Pen(Color.FromArgb(_theme.IsDark ? 150 : 120, _theme.AccentColor), 1.5f);
        g.DrawEllipse(pen, rect);
        using var font = new Font(Font.FontFamily, Math.Max(11f, Font.Size + 3f), FontStyle.Bold);
        TextRenderer.DrawText(g, text, font, rect, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
