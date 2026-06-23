using System.ComponentModel;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    [Category("ViewGrid - V40 Visual Analytics")]
    [DefaultValue(false)]
    [Description("KPI, HeatMap, MiniChart, Timeline ve dashboard builder akışlarını tek analiz profili altında açar.")]
    public bool EnableVisualAnalyticsPro { get; set; }

    [Category("ViewGrid - V40 Visual Analytics")]
    [DefaultValue("Status")]
    public string AnalyticsStatusAspectName { get; set; } = "Status";

    [Category("ViewGrid - V40 Visual Analytics")]
    [DefaultValue("Progress")]
    public string AnalyticsValueAspectName { get; set; } = "Progress";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Category("ViewGrid - V40 Visual Analytics")]
    public List<ViewGridAnalyticsMetric> AnalyticsMetrics { get; } = new();

    public void ApplyV40AnalyticsProfile(ViewGridV40AnalyticsPreset preset)
    {
        EnableVisualAnalyticsPro = true;
        EnableDashboardBuilder = true;
        EnsureDefaultDashboardWidgets();
        EnsureDefaultAnalyticsMetrics();

        switch (preset)
        {
            case ViewGridV40AnalyticsPreset.KpiDashboard:
                SetViewMode(ViewGridMode.KpiDashboard);
                break;
            case ViewGridV40AnalyticsPreset.HeatMap:
                SetViewMode(ViewGridMode.HeatMap);
                break;
            case ViewGridV40AnalyticsPreset.Timeline:
                SetViewMode(ViewGridMode.Timeline);
                break;
            case ViewGridV40AnalyticsPreset.MiniCharts:
                SetViewMode(ViewGridMode.MiniChart);
                break;
            case ViewGridV40AnalyticsPreset.FactoryOverview:
                EnableFactoryStatusOverlay = true;
                SetViewMode(ViewGridMode.HeatMap);
                break;
        }

        RefreshView();
    }

    public void EnsureDefaultAnalyticsMetrics()
    {
        if (AnalyticsMetrics.Count > 0) return;
        AnalyticsMetrics.Add(new ViewGridAnalyticsMetric { Key = "open", Title = "Açık İş", Value = "24", Subtitle = "Bugün", Status = "Warning", NumberValue = 24 });
        AnalyticsMetrics.Add(new ViewGridAnalyticsMetric { Key = "done", Title = "Tamamlandı", Value = "128", Subtitle = "Son 24 saat", Status = "Success", NumberValue = 128 });
        AnalyticsMetrics.Add(new ViewGridAnalyticsMetric { Key = "risk", Title = "Risk", Value = "7", Subtitle = "İzlenmeli", Status = "Danger", NumberValue = 7 });
        AnalyticsMetrics.Add(new ViewGridAnalyticsMetric { Key = "perf", Title = "Performans", Value = "%92", Subtitle = "Ortalama", Status = "Info", NumberValue = 92 });
    }

    public string CreateAnalyticsSummaryText()
    {
        EnsureDefaultAnalyticsMetrics();
        return string.Join(Environment.NewLine, AnalyticsMetrics.Select(m => $"{m.Title}: {m.Value} - {m.Subtitle}"));
    }
}
