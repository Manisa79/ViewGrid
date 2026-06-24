using System.ComponentModel;
using System.Text;
using Taylan.Pano.Columns;

namespace Taylan.Pano.Core;

public sealed class PanoDiagnosticsCenterItem
{
    public PanoQualitySeverity Severity { get; set; } = PanoQualitySeverity.Info;
    public string Area { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Severity} | {Area} | {Code} | {Message}";
    }
}

public sealed class PanoDiagnosticsCenterReport
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<PanoDiagnosticsCenterItem> Items { get; } = new();
    public int ErrorCount => Items.Count(x => x.Severity == PanoQualitySeverity.Error);
    public int WarningCount => Items.Count(x => x.Severity == PanoQualitySeverity.Warning);
    public bool IsClean => ErrorCount == 0 && WarningCount == 0;

    public void Add(PanoQualitySeverity severity, string area, string code, string message, string suggestion = "")
    {
        Items.Add(new PanoDiagnosticsCenterItem
        {
            Severity = severity,
            Area = area,
            Code = code,
            Message = message,
            Suggestion = suggestion
        });
    }
}

public partial class PanoControl
{
    [Category("Pano - Diagnostics Center")]
    [DefaultValue(true)]
    [Description("Build quality, runtime hardening, real usage, performance, media ve dashboard kontrollerini tek raporda toplar.")]
    public bool EnablePanoDiagnosticsCenter { get; set; } = true;

    public PanoDiagnosticsCenterReport RunPanoDiagnosticsCenter()
    {
        var report = new PanoDiagnosticsCenterReport();
        if (!EnablePanoDiagnosticsCenter)
        {
            report.Add(PanoQualitySeverity.Info, "Diagnostics", "CENTER_DISABLED", "Pano Diagnostics Center kapali.");
            return report;
        }

        AppendBuildQualityDiagnostics(report);
        AppendRuntimeDiagnostics(report);
        AppendRealUsageDiagnostics(report);
        AppendViewModeDiagnostics(report);
        AppendMediaDiagnostics(report);
        AppendDashboardDiagnostics(report);
        AppendPerformanceDiagnostics(report);
        return report;
    }

    public string RunPanoDiagnosticsCenterText()
    {
        var report = RunPanoDiagnosticsCenter();
        var sb = new StringBuilder();
        sb.AppendLine("Pano Diagnostics Center");
        sb.AppendLine("Created: " + report.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("Errors: " + report.ErrorCount + "  Warnings: " + report.WarningCount);
        sb.AppendLine();

        foreach (var item in report.Items)
        {
            sb.AppendLine(item.ToString());
            if (!string.IsNullOrWhiteSpace(item.Suggestion))
                sb.AppendLine("  -> " + item.Suggestion);
        }

        sb.AppendLine();
        sb.AppendLine(GetViewModeDecisionGuideText());
        return sb.ToString();
    }

    public void ShowPanoDiagnosticsCenter()
    {
        string text = RunPanoDiagnosticsCenterText();
        var form = new Form
        {
            Text = "Pano Diagnostics Center",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(920, 680),
            MinimizeBox = false
        };

        var editor = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Consolas", 9.5f),
            Text = text
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8)
        };

        var close = new Button { Text = "Kapat", Width = 96 };
        close.Click += (_, __) => form.Close();
        var copy = new Button { Text = "Kopyala", Width = 96 };
        copy.Click += (_, __) => Clipboard.SetText(editor.Text);
        var refresh = new Button { Text = "Yenile", Width = 96 };
        refresh.Click += (_, __) => editor.Text = RunPanoDiagnosticsCenterText();

        buttons.Controls.Add(close);
        buttons.Controls.Add(copy);
        buttons.Controls.Add(refresh);
        form.Controls.Add(editor);
        form.Controls.Add(buttons);

        var owner = FindForm();
        if (owner == null)
            form.Show();
        else
            form.Show(owner);
    }

    public string GetViewModeDecisionGuideText()
    {
        return "View Mode Decision Guide" + Environment.NewLine +
               "- Details/DenseList: buyuk veri, klasik tablo, hizli filtre ve satir tarama." + Environment.NewLine +
               "- RowCard: satiri kart gibi gosterir; aksiyon, rozet ve kisa metin icin iyi." + Environment.NewLine +
               "- RowPreview: RowCard'a benzer ama daha kompakt onizleme gibi davranir; uzun listede daha az yer kaplar." + Environment.NewLine +
               "- DetailCard: secili/veri satirini okunabilir detay karti gibi acar; media varsa kapak/poster paneli ekler." + Environment.NewLine +
               "- PropertyCard: DetailCard'in daha form/property okuma odakli hali; etiket-deger duzeni belirgindir." + Environment.NewLine +
               "- MediaTile/Poster/Gallery/FilmStrip: muzik, film, fotograf ve dokuman kapak/thumbnail senaryolari." + Environment.NewLine +
               "- KpiDashboard/HeatMap/MiniChart: ozet metrik, fabrika durumu ve trend odakli dashboard yuzeyi.";
    }

    private void AppendBuildQualityDiagnostics(PanoDiagnosticsCenterReport report)
    {
        foreach (var item in RunBuildQualityDiagnostics().Items)
            report.Add(item.Severity, item.Area, item.Code, item.Message, item.Suggestion);
    }

    private void AppendRuntimeDiagnostics(PanoDiagnosticsCenterReport report)
    {
        foreach (var item in RunPano5RuntimeChecks())
            report.Add(item.Passed ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, "Runtime", item.CheckName, item.Message);

        foreach (var item in RunPano502RuntimeHardeningChecks())
            report.Add(item.Passed ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, item.Area, item.Check, item.Message);
    }

    private void AppendRealUsageDiagnostics(PanoDiagnosticsCenterReport report)
    {
        foreach (var item in RunPano51UsageChecks())
            report.Add(item.Passed ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, item.Profile, item.Check, item.Message);
    }

    private void AppendViewModeDiagnostics(PanoDiagnosticsCenterReport report)
    {
        report.Add(PanoQualitySeverity.Info, "ViewMode", "CURRENT", "Aktif gorunum: " + ViewMode, GetViewModeDecisionGuideText().Replace(Environment.NewLine, " "));
        report.Add(RememberViewModePerScenario ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, "ViewMode", "MEMORY", RememberViewModePerScenario ? "Senaryo bazli gorunum hafizasi aktif." : "Senaryo bazli gorunum hafizasi kapali.", "Runtime menusu > Gorunum > Senaryoya gore gorunumu hatirla.");

        bool hasImageSource = Columns.VisibleColumns.Any(c =>
            c.Kind == PanoColumnKind.Image ||
            c.Kind == PanoColumnKind.Icon ||
            c.ImageGetter != null ||
            c.StateImageGetter != null ||
            c.Image != null ||
            !string.IsNullOrWhiteSpace(c.ImageAspectName));

        if ((ViewMode == PanoViewMode.DetailCard || ViewMode == PanoViewMode.PropertyCard || ViewMode == PanoViewMode.MediaTile || ViewMode == PanoViewMode.Poster) && !hasImageSource)
            report.Add(PanoQualitySeverity.Warning, "ViewMode", "IMAGE_SOURCE", "Gorsel agirlikli gorunum acik ama otomatik image kolonu bulunamadi.", "Kind=Image/Icon olan bir kolon, ImageGetter veya bitmap donen AspectName ekleyin.");
    }

    private void AppendMediaDiagnostics(PanoDiagnosticsCenterReport report)
    {
        report.Add(PanoQualitySeverity.Info, "Media", "SMART_PRESET", "Aktif media smart preset: " + MediaSmartPreset);
        report.Add(EnableMediaImageCache ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, "Media", "IMAGE_CACHE", EnableMediaImageCache ? "Image cache aktif. Limit: " + MediaMemoryCacheLimit : "Image cache kapali.", "Buyuk medya listelerinde ApplyMediaSmartPreset veya ApplyV38PerformanceProfile(MediaLibrary) kullanin.");
        report.Add(EnableMediaLazyLoading ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, "Media", "LAZY_IMAGE", EnableMediaLazyLoading ? "Lazy image resolve aktif." : "Lazy image resolve kapali.", "Audix/film/fotograf kataloglarinda lazy resolve acik olmali.");
    }

    private void AppendDashboardDiagnostics(PanoDiagnosticsCenterReport report)
    {
        report.Add(EnableDashboardBuilder ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, "Dashboard", "BUILDER", EnableDashboardBuilder ? "Dashboard builder aktif." : "Dashboard builder kapali.", "Sag tik > Gorunum > Dashboard preset editor ile KPI/HeatMap/MiniChart acilabilir.");
        report.Add(DashboardWidgets.Count > 0 ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, "Dashboard", "WIDGETS", "Dashboard widget sayisi: " + DashboardWidgets.Count, "EnsureDefaultDashboardWidgets veya SetDashboardWidgetEnabled kullanin.");
        report.Add(IsDashboardWidgetEnabled(PanoDashboardWidgetKind.Kpi) ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, "Dashboard", "KPI", IsDashboardWidgetEnabled(PanoDashboardWidgetKind.Kpi) ? "KPI widget aktif." : "KPI widget kapali.");
        report.Add(IsDashboardWidgetEnabled(PanoDashboardWidgetKind.HeatMap) ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, "Dashboard", "HEATMAP", IsDashboardWidgetEnabled(PanoDashboardWidgetKind.HeatMap) ? "HeatMap widget aktif." : "HeatMap widget kapali.");
        report.Add(IsDashboardWidgetEnabled(PanoDashboardWidgetKind.Chart) ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, "Dashboard", "MINICHART", IsDashboardWidgetEnabled(PanoDashboardWidgetKind.Chart) ? "MiniChart widget aktif." : "MiniChart widget kapali.");
    }

    private void AppendPerformanceDiagnostics(PanoDiagnosticsCenterReport report)
    {
        report.Add(EnablePaintPerformanceMetrics ? PanoQualitySeverity.Info : PanoQualitySeverity.Warning, "Performance", "PAINT_METRICS", EnablePaintPerformanceMetrics ? GetPerformanceSummary() : "Paint olcumu kapali.", "Sag tik > Gorunum > Akilli presetler > Performance: medya cache.");

        if (ViewCount >= 1000 && !EnableMediaImageCache && (ViewMode == PanoViewMode.MediaTile || ViewMode == PanoViewMode.Poster || ViewMode == PanoViewMode.Gallery || ViewMode == PanoViewMode.FilmStrip))
            report.Add(PanoQualitySeverity.Warning, "Performance", "MEDIA_BIG_LIST", "Buyuk medya listesinde cache kapali.", "Image cache + lazy resolve birlikte acilmali.");
    }
}
