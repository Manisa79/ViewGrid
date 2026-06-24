using System.Reflection;
using System.Text;

namespace Taylan.Pano.Core;

public partial class PanoControl
{
    [Category("Pano - V34 Build Quality")]
    [DefaultValue(true)]
    [Description("Geriye uyumluluk için eski property ve görünüm adlarını koruyan güvenli API yüzeyini aktif tutar.")]
    public bool EnableCompatibilityGuards { get; set; } = true;

    [Category("Pano - V34 Build Quality")]
    [DefaultValue(true)]
    [Description("Host uygulamalarda temel kolon/view/theme ayarları için hızlı kalite kontrol helperlarını aktif eder.")]
    public bool EnableBuildQualityDiagnostics { get; set; } = true;

    [Category("Pano - V34 Build Quality")]
    [DefaultValue(true)]
    [Description("Faz paketleri uygulanırken riskli kombinasyonları güvenli varsayılanlara çeker.")]
    public bool AutoRepairRiskyOptions { get; set; } = true;

    public PanoQualityReport RunBuildQualityDiagnostics()
    {
        var report = new PanoQualityReport();
        if (!EnableBuildQualityDiagnostics)
        {
            report.Add(PanoQualitySeverity.Info, "Diagnostics", "DISABLED", "Build Quality Diagnostics kapalı.");
            return report;
        }

        CheckPublicApi(report);
        CheckColumns(report);
        CheckViewMode(report);
        CheckTheme(report);
        CheckMediaOptions(report);
        return report;
    }

    public string RunBuildQualityDiagnosticsText()
    {
        var report = RunBuildQualityDiagnostics();
        var sb = new StringBuilder();
        sb.AppendLine("Pano v34 Build Quality Report");
        sb.AppendLine("Created: " + report.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("Errors: " + report.ErrorCount + "  Warnings: " + report.WarningCount);
        foreach (var item in report.Items)
        {
            sb.AppendLine(item.ToString());
            if (!string.IsNullOrWhiteSpace(item.Suggestion))
                sb.AppendLine("  -> " + item.Suggestion);
        }
        return sb.ToString();
    }

    public void ApplyV34BuildQualityPack()
    {
        EnableCompatibilityGuards = true;
        EnableBuildQualityDiagnostics = true;
        AutoRepairRiskyOptions = true;
        EnforceThemeAccessibility = true;
        AutoEnsureReadableTextColors = true;
        if (AutoRepairRiskyOptions)
            RepairRiskyOptions();
        RefreshView();
    }

    private void RepairRiskyOptions()
    {
        if (TilePreferredWidth < 120) TilePreferredWidth = 160;
        if (TilePreferredHeight < 120) TilePreferredHeight = 180;
        if (PosterPreferredWidth < 140) PosterPreferredWidth = 180;
        if (PosterPreferredHeight < 180) PosterPreferredHeight = 240;
        if (TilePosterImageHeight < 64) TilePosterImageHeight = 120;
        if (PosterImageHeight < 64) PosterImageHeight = 140;
        if (FilterPopupMinimumSize.Width < 260 || FilterPopupMinimumSize.Height < 220)
            FilterPopupMinimumSize = new Size(360, 320);
        if (FilterPopupDefaultSize.Width < FilterPopupMinimumSize.Width || FilterPopupDefaultSize.Height < FilterPopupMinimumSize.Height)
            FilterPopupDefaultSize = new Size(Math.Max(520, FilterPopupMinimumSize.Width), Math.Max(520, FilterPopupMinimumSize.Height));
    }

    private void CheckPublicApi(PanoQualityReport report)
    {
        var props = typeof(PanoControl).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var duplicates = props.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).ToList();
        if (duplicates.Count == 0)
            report.Add(PanoQualitySeverity.Info, "Public API", "API001", "Runtime public property çakışması görünmüyor.");
        else
            foreach (var d in duplicates)
                report.Add(PanoQualitySeverity.Error, "Public API", "API_DUP", "Duplicate public property: " + d.Key, "Aynı property sadece tek partial dosyada tanımlanmalı.");
    }

    private void CheckColumns(PanoQualityReport report)
    {
        if (Columns.Count == 0)
        {
            report.Add(PanoQualitySeverity.Warning, "Columns", "COL_EMPTY", "Kolon tanımı yok.", "Designer.cs içinde en az bir PanoColumn tanımlayın.");
            return;
        }

        var duplicateAspects = Columns.Where(c => !string.IsNullOrWhiteSpace(c.AspectName))
            .GroupBy(c => c.AspectName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();
        foreach (var d in duplicateAspects)
            report.Add(PanoQualitySeverity.Warning, "Columns", "COL_DUP_ASPECT", "Aynı AspectName birden fazla kolonda kullanılıyor: " + d.Key);

        report.Add(PanoQualitySeverity.Info, "Columns", "COL_COUNT", "Kolon sayısı: " + Columns.Count);
    }

    private void CheckViewMode(PanoQualityReport report)
    {
        report.Add(PanoQualitySeverity.Info, "ViewMode", "VIEW_CURRENT", "Aktif görünüm: " + ViewMode);
        if ((ViewMode == PanoViewMode.Poster || ViewMode == PanoViewMode.Gallery || ViewMode == PanoViewMode.MediaTile || ViewMode == PanoViewMode.FilmStrip) && Columns.Count == 0)
            report.Add(PanoQualitySeverity.Warning, "ViewMode", "VIEW_MEDIA_NO_COLUMN", "Medya görünümü açık ama görsel alabilecek kolon yok.", "ImageGetter olan bir kolon ekleyin veya MediaPlaceholderImage kullanın.");
    }

    private void CheckTheme(PanoQualityReport report)
    {
        var ratio = Taylan.Pano.Theming.PanoThemeAccessibility.ContrastRatio(_theme.BackColor, _theme.ForeColor);
        if (ratio < 4.5d)
            report.Add(PanoQualitySeverity.Warning, "Theme", "THEME_CONTRAST", "Ana yazı kontrastı düşük: " + ratio.ToString("0.00"), "EnforceThemeAccessibility=true kullanın.");
        else
            report.Add(PanoQualitySeverity.Info, "Theme", "THEME_OK", "Ana yazı kontrastı uygun: " + ratio.ToString("0.00"));
    }

    private void CheckMediaOptions(PanoQualityReport report)
    {
        if (EnableMediaLazyLoading && !EnableMediaImageCache)
            report.Add(PanoQualitySeverity.Warning, "Media", "MEDIA_CACHE_OFF", "Lazy loading açık ama media image cache kapalı.", "Audix gibi arşivlerde iki ayarı birlikte kullanın.");
        if (ShowMediaQualityBadge && string.IsNullOrWhiteSpace(MediaQualityBadgeAspectName) && MediaQualityBadgeGetter == null)
            report.Add(PanoQualitySeverity.Warning, "Media", "MEDIA_BADGE_NO_SOURCE", "Medya rozeti açık ama badge kaynağı yok.", "MediaQualityBadgeAspectName veya MediaQualityBadgeGetter tanımlayın.");
    }
}
