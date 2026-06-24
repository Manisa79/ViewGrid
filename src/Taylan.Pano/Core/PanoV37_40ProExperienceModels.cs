using System.ComponentModel;

namespace Taylan.Pano.Core;

public enum PanoV37LayoutScope
{
    User,
    Project,
    Machine,
    Scenario
}

public enum PanoV38PerformancePreset
{
    Balanced,
    LargeData,
    MediaLibrary,
    VirtualMillionRows,
    LowMemory
}

public enum PanoV39InteractionPreset
{
    Standard,
    PowerUser,
    TouchFriendly,
    AudixMedia,
    FactoryOperator
}

public enum PanoV40AnalyticsPreset
{
    KpiDashboard,
    HeatMap,
    Timeline,
    MiniCharts,
    FactoryOverview
}

public enum PanoMediaSmartPreset
{
    Music,
    Movie,
    Photo,
    Document
}

public sealed class PanoLayoutPresetInfo
{
    public string Name { get; set; } = string.Empty;
    public string ViewMode { get; set; } = string.Empty;
    public string GroupBy { get; set; } = string.Empty;
    public string GlobalFilter { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public List<PanoColumnLayoutInfo> Columns { get; set; } = new();
}

public sealed class PanoColumnLayoutInfo
{
    public string Name { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public string AspectName { get; set; } = string.Empty;
    public int Width { get; set; }
    public int DisplayIndex { get; set; }
    public bool Visible { get; set; }
}

public sealed class PanoShortcutAction
{
    public string KeyText { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class PanoAnalyticsMetric
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double NumberValue { get; set; }
}
