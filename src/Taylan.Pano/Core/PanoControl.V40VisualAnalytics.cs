using System.ComponentModel;

namespace Taylan.Pano.Core;

public partial class PanoControl
{
    [Category("Pano - V40 Visual Analytics")]
    [DefaultValue(false)]
    [Description("KPI, HeatMap, MiniChart, Timeline ve dashboard builder akışlarını tek analiz profili altında açar.")]
    public bool EnableVisualAnalyticsPro { get; set; }

    [Category("Pano - V40 Visual Analytics")]
    [DefaultValue("Status")]
    public string AnalyticsStatusAspectName { get; set; } = "Status";

    [Category("Pano - V40 Visual Analytics")]
    [DefaultValue("Progress")]
    public string AnalyticsValueAspectName { get; set; } = "Progress";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Category("Pano - V40 Visual Analytics")]
    public List<PanoAnalyticsMetric> AnalyticsMetrics { get; } = new();

    public void ApplyV40AnalyticsProfile(PanoV40AnalyticsPreset preset)
    {
        EnableVisualAnalyticsPro = true;
        EnableDashboardBuilder = true;
        EnsureDefaultDashboardWidgets();
        EnsureDefaultAnalyticsMetrics();

        switch (preset)
        {
            case PanoV40AnalyticsPreset.KpiDashboard:
                SetViewMode(PanoViewMode.KpiDashboard);
                break;
            case PanoV40AnalyticsPreset.HeatMap:
                SetViewMode(PanoViewMode.HeatMap);
                break;
            case PanoV40AnalyticsPreset.Timeline:
                SetViewMode(PanoViewMode.Timeline);
                break;
            case PanoV40AnalyticsPreset.MiniCharts:
                SetViewMode(PanoViewMode.MiniChart);
                break;
            case PanoV40AnalyticsPreset.FactoryOverview:
                EnableFactoryStatusOverlay = true;
                SetViewMode(PanoViewMode.HeatMap);
                break;
        }

        RefreshView();
    }

    public void EnsureDefaultAnalyticsMetrics()
    {
        if (AnalyticsMetrics.Count > 0) return;
        AnalyticsMetrics.Add(new PanoAnalyticsMetric { Key = "open", Title = "Açık İş", Value = "24", Subtitle = "Bugün", Status = "Warning", NumberValue = 24 });
        AnalyticsMetrics.Add(new PanoAnalyticsMetric { Key = "done", Title = "Tamamlandı", Value = "128", Subtitle = "Son 24 saat", Status = "Success", NumberValue = 128 });
        AnalyticsMetrics.Add(new PanoAnalyticsMetric { Key = "risk", Title = "Risk", Value = "7", Subtitle = "İzlenmeli", Status = "Danger", NumberValue = 7 });
        AnalyticsMetrics.Add(new PanoAnalyticsMetric { Key = "perf", Title = "Performans", Value = "%92", Subtitle = "Ortalama", Status = "Info", NumberValue = 92 });
    }

    public string CreateAnalyticsSummaryText()
    {
        EnsureDefaultAnalyticsMetrics();
        return string.Join(Environment.NewLine, AnalyticsMetrics.Select(m => $"{m.Title}: {m.Value} - {m.Subtitle}"));
    }
}
