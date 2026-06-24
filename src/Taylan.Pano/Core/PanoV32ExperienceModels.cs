namespace Taylan.Pano.Core;

/// <summary>
/// v32+ yol haritasındaki gelişmiş deneyim paketleri. Bu enum host uygulama ayarları,
/// komut paleti ve entegrasyon profillerinde aynı isimlendirmeyi kullanmak için tasarlanmıştır.
/// </summary>
public enum PanoExperiencePhase
{
    UxIntelligence = 38,
    FactoryIntelligence = 39,
    TimelineEngine = 40,
    DocumentExplorer = 41,
    VirtualizationPro = 42,
    SearchEverywhere = 43,
    CommandPalette = 44,
    LayoutStudio = 45,
    DashboardBuilder = 46,
    AiLayer = 47,
    Ecosystem = 48
}

public enum PanoMachineStatus
{
    Unknown,
    Running,
    Waiting,
    Fault,
    Offline,
    Maintenance
}

public enum PanoDocumentPreviewKind
{
    Auto,
    Image,
    Pdf,
    Cad,
    Video,
    Audio,
    Folder,
    Unknown
}

public enum PanoDashboardWidgetKind
{
    Kpi,
    Chart,
    Table,
    Card,
    HeatMap,
    Timeline,
    Gallery,
    Kanban
}

public sealed class PanoTimelineEvent
{
    public DateTime Time { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
}

public sealed class PanoDashboardWidgetDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public PanoDashboardWidgetKind Kind { get; set; }
    public string AspectName { get; set; } = string.Empty;
    public int PreferredWidth { get; set; } = 240;
    public int PreferredHeight { get; set; } = 160;
}

public sealed class PanoAiInsight
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string SuggestedAction { get; set; } = string.Empty;
}
