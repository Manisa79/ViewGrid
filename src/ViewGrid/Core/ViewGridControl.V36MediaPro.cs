using System.Security.Cryptography;
using System.Text;

namespace ViewGrid.Core;

public enum ViewGridMediaMissingCoverBehavior
{
    Placeholder,
    Initials,
    Hidden,
    BadgeOnly
}

public partial class ViewGridControl
{
    private readonly Dictionary<string, Image> _mediaProMemoryCache = new(StringComparer.OrdinalIgnoreCase);

    [Category("ViewGrid - V36 Media Pro")]
    [DefaultValue(false)]
    [Description("Audix ve medya kütüphanesi ekranları için gelişmiş kapak cache, placeholder, overlay ve gruplama davranışlarını aktif eder.")]
    public bool EnableMediaPro { get; set; }

    [Category("ViewGrid - V36 Media Pro")]
    [DefaultValue(256)]
    [Description("Bellek içi medya görsel cache üst sınırı. Çok büyük arşivlerde 128-512 arası önerilir.")]
    public int MediaMemoryCacheLimit { get; set; } = 256;

    [Category("ViewGrid - V36 Media Pro")]
    [DefaultValue("")]
    [Description("Albüm kapakları için opsiyonel disk cache klasörü. Boşsa sadece bellek cache kullanılır.")]
    public string MediaDiskCacheFolder { get; set; } = string.Empty;

    [Category("ViewGrid - V36 Media Pro")]
    [DefaultValue(ViewGridMediaMissingCoverBehavior.Placeholder)]
    [Description("Kapak bulunamadığında placeholder/initials/gizle/rozet-only davranışı.")]
    public ViewGridMediaMissingCoverBehavior MissingCoverBehavior { get; set; } = ViewGridMediaMissingCoverBehavior.Placeholder;

    [Category("ViewGrid - V36 Media Pro")]
    [DefaultValue("Album")]
    [Description("Sanatçı/albüm gruplaması için okunacak property/kolon adı.")]
    public string MediaGroupAspectName { get; set; } = "Album";

    [Category("ViewGrid - V36 Media Pro")]
    [DefaultValue(true)]
    [Description("Seçili medya kartında accent glow/outline kullanır.")]
    public bool ShowMediaSelectedGlow { get; set; } = true;

    [Category("ViewGrid - V36 Media Pro")]
    [DefaultValue(true)]
    [Description("Kapak eksik olduğunda rozet/metinsel uyarı gösterimi için host uygulama tarafına niyet bildirir.")]
    public bool ShowMissingCoverBadge { get; set; } = true;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<object, string?>? MediaImagePathGetter { get; set; }

    public void ApplyV36MediaProPack()
    {
        EnableMediaPro = true;
        EnableMediaLazyLoading = true;
        EnableMediaImageCache = true;
        ShowMediaOverlayButton = true;
        ShowMediaQualityBadge = true;
        ShowMediaSelectedGlow = true;
        ShowMissingCoverBadge = true;
        MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
        MediaOverlayButtonText = "▶";
        if (MediaPlaceholderImage == null)
            MediaPlaceholderImage = CreateMediaPlaceholderBitmap("VIEWGRID", _theme.AccentColor);
        if (ViewMode != ViewGridMode.Poster && ViewMode != ViewGridMode.Gallery && ViewMode != ViewGridMode.MediaTile && ViewMode != ViewGridMode.FilmStrip)
            SetViewMode(ViewGridMode.Poster);
        RefreshView();
    }

    public Image? ResolveMediaImagePro(object row)
    {
        if (row == null) return MediaPlaceholderImage;
        string? path = MediaImagePathGetter?.Invoke(row);
        if (string.IsNullOrWhiteSpace(path))
            return MissingCoverBehavior == ViewGridMediaMissingCoverBehavior.Hidden ? null : MediaPlaceholderImage;
        return LoadMediaImageFromCache(path) ?? MediaPlaceholderImage;
    }

    public Image? LoadMediaImageFromCache(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return MediaPlaceholderImage;
        string key = path.Trim();
        if (_mediaProMemoryCache.TryGetValue(key, out var cached)) return cached;

        string actualPath = key;
        if (!File.Exists(actualPath) && !string.IsNullOrWhiteSpace(MediaDiskCacheFolder))
        {
            string cachePath = Path.Combine(MediaDiskCacheFolder, Sha1(key) + Path.GetExtension(key));
            if (File.Exists(cachePath)) actualPath = cachePath;
        }

        if (!File.Exists(actualPath)) return MediaPlaceholderImage;
        try
        {
            using var src = Image.FromFile(actualPath);
            var clone = new Bitmap(src);
            AddMediaImageToMemoryCache(key, clone);
            return clone;
        }
        catch
        {
            return MediaPlaceholderImage;
        }
    }

    public void AddMediaImageToMemoryCache(string key, Image image)
    {
        if (string.IsNullOrWhiteSpace(key) || image == null) return;
        if (_mediaProMemoryCache.Count >= Math.Max(16, MediaMemoryCacheLimit))
        {
            var first = _mediaProMemoryCache.Keys.FirstOrDefault();
            if (first != null)
            {
                _mediaProMemoryCache[first].Dispose();
                _mediaProMemoryCache.Remove(first);
            }
        }
        _mediaProMemoryCache[key] = image;
    }

    public void ClearMediaImageCache()
    {
        foreach (var image in _mediaProMemoryCache.Values)
            image.Dispose();
        _mediaProMemoryCache.Clear();
        RefreshView();
    }

    public Bitmap CreateMediaPlaceholderBitmap(string label, Color? baseColor = null)
    {
        string safe = string.IsNullOrWhiteSpace(label) ? "MEDIA" : label.Trim();
        if (safe.Length > 10) safe = safe.Substring(0, 10);
        var color = baseColor ?? _theme.AccentColor;
        var bmp = new Bitmap(360, 520);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bg = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, bmp.Width, bmp.Height), ViewGrid.Theming.ViewGridThemeAccessibility.Blend(color, Color.White, 0.15d), ViewGrid.Theming.ViewGridThemeAccessibility.Blend(color, Color.Black, 0.45d), 55f);
        g.FillRectangle(bg, 0, 0, bmp.Width, bmp.Height);
        using var glow = new SolidBrush(Color.FromArgb(70, Color.White));
        g.FillEllipse(glow, -40, 60, 230, 230);
        using var band = new SolidBrush(Color.FromArgb(145, Color.Black));
        g.FillRectangle(band, 0, 350, bmp.Width, 170);
        using var big = new Font("Segoe UI", 34, FontStyle.Bold);
        TextRenderer.DrawText(g, safe, big, new Rectangle(0, 158, bmp.Width, 78), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        using var small = new Font("Segoe UI", 11, FontStyle.Bold);
        TextRenderer.DrawText(g, "NO COVER", small, new Rectangle(0, 394, bmp.Width, 30), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, "ViewGrid Media Pro", SystemFonts.CaptionFont, new Rectangle(0, 426, bmp.Width, 30), Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bmp;
    }

    public string ResolveMediaGroupValue(object row)
    {
        if (row == null || string.IsNullOrWhiteSpace(MediaGroupAspectName)) return string.Empty;
        var col = Columns.FirstOrDefault(c => string.Equals(c.AspectName, MediaGroupAspectName, StringComparison.OrdinalIgnoreCase) || string.Equals(c.Header, MediaGroupAspectName, StringComparison.OrdinalIgnoreCase));
        if (col != null) return Convert.ToString(col.GetValue(row)) ?? string.Empty;
        var prop = row.GetType().GetProperty(MediaGroupAspectName);
        return prop == null ? string.Empty : Convert.ToString(prop.GetValue(row)) ?? string.Empty;
    }

    private static string Sha1(string value)
    {
        using var sha = SHA1.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        return string.Concat(bytes.Select(b => b.ToString("x2")));
    }
}
