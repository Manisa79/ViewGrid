using System.ComponentModel;

namespace ViewGrid.Core;

public enum ViewGridV37LayoutScope
{
    User,
    Project,
    Machine,
    Scenario
}

public enum ViewGridV38PerformancePreset
{
    Balanced,
    LargeData,
    MediaLibrary,
    VirtualMillionRows,
    LowMemory
}

public enum ViewGridV39InteractionPreset
{
    Standard,
    PowerUser,
    TouchFriendly,
    AudixMedia,
    FactoryOperator
}

public enum ViewGridV40AnalyticsPreset
{
    KpiDashboard,
    HeatMap,
    Timeline,
    MiniCharts,
    FactoryOverview
}

public enum ViewGridMediaSmartPreset
{
    Music,
    Movie,
    Photo,
    Document
}

public sealed class ViewGridLayoutPresetInfo
{
    public string Name { get; set; } = string.Empty;
    public string ViewMode { get; set; } = string.Empty;
    public string GroupBy { get; set; } = string.Empty;
    public string GlobalFilter { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public List<ViewGridColumnLayoutInfo> Columns { get; set; } = new();
}

public sealed class ViewGridColumnLayoutInfo
{
    public string Name { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public string AspectName { get; set; } = string.Empty;
    public int Width { get; set; }
    public int DisplayIndex { get; set; }
    public bool Visible { get; set; }
}

public sealed class ViewGridShortcutAction
{
    public string KeyText { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class ViewGridAnalyticsMetric
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double NumberValue { get; set; }
}
