using System.ComponentModel;
using System.Text;
using ViewGrid.Columns;

namespace ViewGrid.Core;

public sealed class ViewGridDiagnosticsCenterItem
{
    public ViewGridQualitySeverity Severity { get; set; } = ViewGridQualitySeverity.Info;
    public string Area { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Severity} | {Area} | {Code} | {Message}";
    }
}

public sealed class ViewGridDiagnosticsCenterReport
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<ViewGridDiagnosticsCenterItem> Items { get; } = new();
    public int ErrorCount => Items.Count(x => x.Severity == ViewGridQualitySeverity.Error);
    public int WarningCount => Items.Count(x => x.Severity == ViewGridQualitySeverity.Warning);
    public bool IsClean => ErrorCount == 0 && WarningCount == 0;

    public void Add(ViewGridQualitySeverity severity, string area, string code, string message, string suggestion = "")
    {
        Items.Add(new ViewGridDiagnosticsCenterItem
        {
            Severity = severity,
            Area = area,
            Code = code,
            Message = message,
            Suggestion = suggestion
        });
    }
}

public partial class ViewGridControl
{
    [Category("ViewGrid - Diagnostics Center")]
    [DefaultValue(true)]
    [Description("Build quality, runtime hardening, real usage, performance, media ve dashboard kontrollerini tek raporda toplar.")]
    public bool EnableViewGridDiagnosticsCenter { get; set; } = true;

    public ViewGridDiagnosticsCenterReport RunViewGridDiagnosticsCenter()
    {
        var report = new ViewGridDiagnosticsCenterReport();
        if (!EnableViewGridDiagnosticsCenter)
        {
            report.Add(ViewGridQualitySeverity.Info, "Diagnostics", "CENTER_DISABLED", "ViewGrid Diagnostics Center kapali.");
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

    public string RunViewGridDiagnosticsCenterText()
    {
        var report = RunViewGridDiagnosticsCenter();
        var sb = new StringBuilder();
        sb.AppendLine("ViewGrid Diagnostics Center");
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

    public void ShowViewGridDiagnosticsCenter()
    {
        string text = RunViewGridDiagnosticsCenterText();
        var form = new Form
        {
            Text = "ViewGrid Diagnostics Center",
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
        refresh.Click += (_, __) => editor.Text = RunViewGridDiagnosticsCenterText();

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

    private void AppendBuildQualityDiagnostics(ViewGridDiagnosticsCenterReport report)
    {
        foreach (var item in RunBuildQualityDiagnostics().Items)
            report.Add(item.Severity, item.Area, item.Code, item.Message, item.Suggestion);
    }

    private void AppendRuntimeDiagnostics(ViewGridDiagnosticsCenterReport report)
    {
        foreach (var item in RunViewGrid5RuntimeChecks())
            report.Add(item.Passed ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, "Runtime", item.CheckName, item.Message);

        foreach (var item in RunViewGrid502RuntimeHardeningChecks())
            report.Add(item.Passed ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, item.Area, item.Check, item.Message);
    }

    private void AppendRealUsageDiagnostics(ViewGridDiagnosticsCenterReport report)
    {
        foreach (var item in RunViewGrid51UsageChecks())
            report.Add(item.Passed ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, item.Profile, item.Check, item.Message);
    }

    private void AppendViewModeDiagnostics(ViewGridDiagnosticsCenterReport report)
    {
        report.Add(ViewGridQualitySeverity.Info, "ViewMode", "CURRENT", "Aktif gorunum: " + ViewMode, GetViewModeDecisionGuideText().Replace(Environment.NewLine, " "));
        report.Add(RememberViewModePerScenario ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, "ViewMode", "MEMORY", RememberViewModePerScenario ? "Senaryo bazli gorunum hafizasi aktif." : "Senaryo bazli gorunum hafizasi kapali.", "Runtime menusu > Gorunum > Senaryoya gore gorunumu hatirla.");

        bool hasImageSource = Columns.VisibleColumns.Any(c =>
            c.Kind == ViewGridColumnKind.Image ||
            c.Kind == ViewGridColumnKind.Icon ||
            c.ImageGetter != null ||
            c.StateImageGetter != null ||
            c.Image != null ||
            !string.IsNullOrWhiteSpace(c.ImageAspectName));

        if ((ViewMode == ViewGridMode.DetailCard || ViewMode == ViewGridMode.PropertyCard || ViewMode == ViewGridMode.MediaTile || ViewMode == ViewGridMode.Poster) && !hasImageSource)
            report.Add(ViewGridQualitySeverity.Warning, "ViewMode", "IMAGE_SOURCE", "Gorsel agirlikli gorunum acik ama otomatik image kolonu bulunamadi.", "Kind=Image/Icon olan bir kolon, ImageGetter veya bitmap donen AspectName ekleyin.");
    }

    private void AppendMediaDiagnostics(ViewGridDiagnosticsCenterReport report)
    {
        report.Add(ViewGridQualitySeverity.Info, "Media", "SMART_PRESET", "Aktif media smart preset: " + MediaSmartPreset);
        report.Add(EnableMediaImageCache ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, "Media", "IMAGE_CACHE", EnableMediaImageCache ? "Image cache aktif. Limit: " + MediaMemoryCacheLimit : "Image cache kapali.", "Buyuk medya listelerinde ApplyMediaSmartPreset veya ApplyV38PerformanceProfile(MediaLibrary) kullanin.");
        report.Add(EnableMediaLazyLoading ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, "Media", "LAZY_IMAGE", EnableMediaLazyLoading ? "Lazy image resolve aktif." : "Lazy image resolve kapali.", "Audix/film/fotograf kataloglarinda lazy resolve acik olmali.");
    }

    private void AppendDashboardDiagnostics(ViewGridDiagnosticsCenterReport report)
    {
        report.Add(EnableDashboardBuilder ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, "Dashboard", "BUILDER", EnableDashboardBuilder ? "Dashboard builder aktif." : "Dashboard builder kapali.", "Sag tik > Gorunum > Dashboard preset editor ile KPI/HeatMap/MiniChart acilabilir.");
        report.Add(DashboardWidgets.Count > 0 ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, "Dashboard", "WIDGETS", "Dashboard widget sayisi: " + DashboardWidgets.Count, "EnsureDefaultDashboardWidgets veya SetDashboardWidgetEnabled kullanin.");
        report.Add(IsDashboardWidgetEnabled(ViewGridDashboardWidgetKind.Kpi) ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, "Dashboard", "KPI", IsDashboardWidgetEnabled(ViewGridDashboardWidgetKind.Kpi) ? "KPI widget aktif." : "KPI widget kapali.");
        report.Add(IsDashboardWidgetEnabled(ViewGridDashboardWidgetKind.HeatMap) ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, "Dashboard", "HEATMAP", IsDashboardWidgetEnabled(ViewGridDashboardWidgetKind.HeatMap) ? "HeatMap widget aktif." : "HeatMap widget kapali.");
        report.Add(IsDashboardWidgetEnabled(ViewGridDashboardWidgetKind.Chart) ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, "Dashboard", "MINICHART", IsDashboardWidgetEnabled(ViewGridDashboardWidgetKind.Chart) ? "MiniChart widget aktif." : "MiniChart widget kapali.");
    }

    private void AppendPerformanceDiagnostics(ViewGridDiagnosticsCenterReport report)
    {
        report.Add(EnablePaintPerformanceMetrics ? ViewGridQualitySeverity.Info : ViewGridQualitySeverity.Warning, "Performance", "PAINT_METRICS", EnablePaintPerformanceMetrics ? GetPerformanceSummary() : "Paint olcumu kapali.", "Sag tik > Gorunum > Akilli presetler > Performance: medya cache.");

        if (ViewCount >= 1000 && !EnableMediaImageCache && (ViewMode == ViewGridMode.MediaTile || ViewMode == ViewGridMode.Poster || ViewMode == ViewGridMode.Gallery || ViewMode == ViewGridMode.FilmStrip))
            report.Add(ViewGridQualitySeverity.Warning, "Performance", "MEDIA_BIG_LIST", "Buyuk medya listesinde cache kapali.", "Image cache + lazy resolve birlikte acilmali.");
    }
}
