namespace ViewGrid.Core;

public partial class ViewGridControl
{
    [Category("ViewGrid - V32 Experience Suite")]
    [DefaultValue(false)]
    [Description("Kullanıcının görünüm/kolon/filtre alışkanlıklarını host uygulamanın kaydedebilmesi için ViewGrid tarafında niyet bayrağıdır.")]
    public bool EnableUxIntelligence { get; set; }

    [Category("ViewGrid - V32 Experience Suite")]
    [DefaultValue(true)]
    [Description("Son seçilen görünüm modunun host uygulama tarafından saklanmasını önerir. SaveExperienceSnapshot çıktısına ViewMode dahil edilir.")]
    public bool RememberLastViewMode { get; set; } = true;

    [Category("ViewGrid - V32 Experience Suite")]
    [DefaultValue(true)]
    [Description("Her ActiveScenario için son seçilen ViewMode değerini runtime içinde hatırlar.")]
    public bool RememberViewModePerScenario { get; set; } = true;

    private readonly Dictionary<ViewGridScenario, ViewGridMode> _viewModeMemoryByScenario = new();
    private bool _suppressViewModeMemory;

    [Category("ViewGrid - V32 Experience Suite")]
    [DefaultValue(false)]
    [Description("Makine/hat kartlarında Running/Waiting/Fault/Offline gibi fabrika durumlarının overlay mantığında kullanılacağını belirtir.")]
    public bool EnableFactoryStatusOverlay { get; set; }

    [Category("ViewGrid - V32 Experience Suite")]
    [DefaultValue("Status")]
    [Description("Factory Status Overlay için okunacak property/kolon adı. Örn: Status, MachineStatus, Durum.")]
    public string FactoryStatusAspectName { get; set; } = "Status";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<object, ViewGridMachineStatus>? FactoryStatusGetter { get; set; }

    [Category("ViewGrid - V32 Experience Suite")]
    [DefaultValue(false)]
    [Description("Search Everywhere ve komut paleti kullanımlarında tüm kolonlar üzerinde hızlı arama yapılmasını sağlar.")]
    public bool EnableSearchEverywhere { get; set; }

    [Category("ViewGrid - V32 Experience Suite")]
    [DefaultValue(false)]
    [Description("Kullanıcının kolon/kart/dashboard yerleşimi oluşturup saklayabileceği Layout Studio akışı için hazır ayar.")]
    public bool EnableLayoutStudio { get; set; }

    [Category("ViewGrid - V32 Experience Suite")]
    [DefaultValue(false)]
    [Description("KPI, chart, table, card, heatmap, timeline ve gallery widget tanımlarını tek ViewGrid içinde yönetme akışını açar.")]
    public bool EnableDashboardBuilder { get; set; }

    [Category("ViewGrid - V32 Experience Suite")]
    [DefaultValue(false)]
    [Description("Host uygulamanın ürettiği AI/akıllı öneri kartlarının ViewGrid üstünde/yanında gösterileceğini belirtir.")]
    public bool EnableAiInsights { get; set; }

    [Category("ViewGrid - V32 Experience Suite")]
    [DefaultValue(false)]
    [Description("Çok büyük satır sayılarında kart/poster/tile virtualization davranışlarının host veri sağlayıcısıyla birlikte kullanılacağını belirtir.")]
    public bool EnableVirtualizationPro { get; set; }

    [Category("ViewGrid - V32 Experience Suite")]
    [DefaultValue(100000)]
    [Description("Virtualization Pro senaryolarında hedeflenen büyük veri eşiği.")]
    public int VirtualizationProTargetRows { get; set; } = 100000;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Category("ViewGrid - V32 Experience Suite")]
    [Description("Dashboard Builder için host uygulamanın doldurabileceği widget tanımları.")]
    public List<ViewGridDashboardWidgetDefinition> DashboardWidgets { get; } = new();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<ViewGridAiInsight> AiInsights { get; } = new();

    public void ApplyV32ExperiencePhase(ViewGridExperiencePhase phase)
    {
        switch (phase)
        {
            case ViewGridExperiencePhase.UxIntelligence:
                EnableUxIntelligence = true;
                RememberLastViewMode = true;
                break;
            case ViewGridExperiencePhase.FactoryIntelligence:
                EnableFactoryStatusOverlay = true;
                SetViewMode(ViewGridMode.HeatMap);
                break;
            case ViewGridExperiencePhase.TimelineEngine:
                SetViewMode(ViewGridMode.Timeline);
                break;
            case ViewGridExperiencePhase.DocumentExplorer:
                SetViewMode(ViewGridMode.Gallery);
                EnableMediaLazyLoading = true;
                EnableMediaImageCache = true;
                break;
            case ViewGridExperiencePhase.VirtualizationPro:
                EnableVirtualizationPro = true;
                SetViewMode(ViewGridMode.DenseList);
                break;
            case ViewGridExperiencePhase.SearchEverywhere:
                EnableSearchEverywhere = true;
                EnableModernSearchPanel = true;
                SearchPanelCanFilterResults = true;
                ShowQuickFilterBar = true;
                break;
            case ViewGridExperiencePhase.CommandPalette:
                EnableCommandPalette = true;
                break;
            case ViewGridExperiencePhase.LayoutStudio:
                EnableLayoutStudio = true;
                break;
            case ViewGridExperiencePhase.DashboardBuilder:
                EnableDashboardBuilder = true;
                SetViewMode(ViewGridMode.KpiDashboard);
                EnsureDefaultDashboardWidgets();
                break;
            case ViewGridExperiencePhase.AiLayer:
                EnableAiInsights = true;
                break;
            case ViewGridExperiencePhase.Ecosystem:
                EnableCommandPalette = true;
                EnableLayoutStudio = true;
                EnableDashboardBuilder = true;
                EnableSearchEverywhere = true;
                break;
        }

        RefreshView();
    }

    public void ApplyV32UltimateExperiencePack()
    {
        EnableUxIntelligence = true;
        RememberLastViewMode = true;
        EnableFactoryStatusOverlay = true;
        EnableSearchEverywhere = true;
        EnableCommandPalette = true;
        EnableLayoutStudio = true;
        EnableDashboardBuilder = true;
        EnableAiInsights = true;
        EnableVirtualizationPro = true;
        EnableMediaLazyLoading = true;
        EnableMediaImageCache = true;
        EnableModernSearchPanel = true;
                SearchPanelCanFilterResults = true;
                ShowQuickFilterBar = true;
        EnsureDefaultDashboardWidgets();
        RefreshView();
    }

    public string SaveExperienceSnapshot()
    {
        string view = RememberLastViewMode ? _viewMode.ToString() : string.Empty;
        string widgets = string.Join(",", DashboardWidgets.Select(w => w.Key + ":" + w.Kind));
        return "ViewMode=" + view + Environment.NewLine +
               "UxIntelligence=" + EnableUxIntelligence + Environment.NewLine +
               "FactoryOverlay=" + EnableFactoryStatusOverlay + Environment.NewLine +
               "SearchEverywhere=" + EnableSearchEverywhere + Environment.NewLine +
               "CommandPalette=" + EnableCommandPalette + Environment.NewLine +
               "LayoutStudio=" + EnableLayoutStudio + Environment.NewLine +
               "DashboardBuilder=" + EnableDashboardBuilder + Environment.NewLine +
               "AiInsights=" + EnableAiInsights + Environment.NewLine +
               "VirtualizationPro=" + EnableVirtualizationPro + Environment.NewLine +
               "Widgets=" + widgets;
    }

    public IEnumerable<object> SearchEverywhere(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<object>();
        string needle = query.Trim();
        return GetObjects().Where(row => Columns.VisibleColumns.Any(col =>
        {
            string? value = Convert.ToString(col.GetValue(row));
            return !string.IsNullOrEmpty(value) && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        })).ToArray();
    }

    public void RememberCurrentViewModeForActiveScenario()
    {
        if (!RememberViewModePerScenario || _suppressViewModeMemory) return;
        _viewModeMemoryByScenario[ActiveScenario] = ViewMode;
    }

    internal IDisposable SuspendViewModeMemory()
    {
        _suppressViewModeMemory = true;
        return new ViewGridModeMemoryScope(this);
    }

    private sealed class ViewGridModeMemoryScope : IDisposable
    {
        private ViewGridControl? _owner;
        public ViewGridModeMemoryScope(ViewGridControl owner) => _owner = owner;
        public void Dispose()
        {
            if (_owner != null) _owner._suppressViewModeMemory = false;
            _owner = null;
        }
    }

    public bool TryRestoreRememberedViewMode(ViewGridScenario scenario)
    {
        if (!RememberViewModePerScenario) return false;
        if (!_viewModeMemoryByScenario.TryGetValue(scenario, out var remembered)) return false;
        if (ViewMode == remembered) return true;
        SetViewMode(remembered);
        return true;
    }

    public ViewGridMachineStatus ResolveFactoryStatus(object row)
    {
        if (FactoryStatusGetter != null) return FactoryStatusGetter(row);
        string? value = null;
        var col = Columns.FirstOrDefault(c => string.Equals(c.AspectName, FactoryStatusAspectName, StringComparison.OrdinalIgnoreCase) || string.Equals(c.Header, FactoryStatusAspectName, StringComparison.OrdinalIgnoreCase));
        if (col != null) value = Convert.ToString(col.GetValue(row));
        if (string.IsNullOrWhiteSpace(value))
        {
            var prop = row.GetType().GetProperty(FactoryStatusAspectName);
            if (prop != null) value = Convert.ToString(prop.GetValue(row));
        }
        return value?.Trim().ToLowerInvariant() switch
        {
            "running" or "online" or "çalışıyor" or "calisiyor" or "ok" => ViewGridMachineStatus.Running,
            "waiting" or "bekliyor" or "warning" or "uyarı" => ViewGridMachineStatus.Waiting,
            "fault" or "arıza" or "ariza" or "fail" or "error" => ViewGridMachineStatus.Fault,
            "offline" or "kapalı" or "kapali" => ViewGridMachineStatus.Offline,
            "maintenance" or "bakım" or "bakim" => ViewGridMachineStatus.Maintenance,
            _ => ViewGridMachineStatus.Unknown
        };
    }

    public void AddAiInsight(string title, string message, string severity = "Info", string suggestedAction = "")
    {
        AiInsights.Add(new ViewGridAiInsight { Title = title, Message = message, Severity = severity, SuggestedAction = suggestedAction });
        EnableAiInsights = true;
    }

    public void EnsureDefaultDashboardWidgets()
    {
        if (DashboardWidgets.Count > 0) return;
        DashboardWidgets.Add(new ViewGridDashboardWidgetDefinition { Key = "total", Title = "Toplam", Kind = ViewGridDashboardWidgetKind.Kpi, AspectName = "Code" });
        DashboardWidgets.Add(new ViewGridDashboardWidgetDefinition { Key = "status", Title = "Durum Dağılımı", Kind = ViewGridDashboardWidgetKind.HeatMap, AspectName = "Status" });
        DashboardWidgets.Add(new ViewGridDashboardWidgetDefinition { Key = "timeline", Title = "Zaman Akışı", Kind = ViewGridDashboardWidgetKind.Timeline, AspectName = "Detail" });
    }
    public bool IsDashboardWidgetEnabled(ViewGridDashboardWidgetKind kind)
        => DashboardWidgets.Any(w => w.Kind == kind);

    public void SetDashboardWidgetEnabled(ViewGridDashboardWidgetKind kind, bool enabled)
    {
        EnableDashboardBuilder = true;

        if (!enabled)
        {
            DashboardWidgets.RemoveAll(w => w.Kind == kind);
            RefreshView();
            return;
        }

        if (!IsDashboardWidgetEnabled(kind))
            DashboardWidgets.Add(CreateDefaultDashboardWidget(kind));

        RefreshView();
    }

    private static ViewGridDashboardWidgetDefinition CreateDefaultDashboardWidget(ViewGridDashboardWidgetKind kind)
    {
        return kind switch
        {
            ViewGridDashboardWidgetKind.Kpi => new ViewGridDashboardWidgetDefinition { Key = "kpi", Title = "KPI", Kind = kind, AspectName = "Code" },
            ViewGridDashboardWidgetKind.HeatMap => new ViewGridDashboardWidgetDefinition { Key = "heat", Title = "HeatMap", Kind = kind, AspectName = "Status" },
            ViewGridDashboardWidgetKind.Chart => new ViewGridDashboardWidgetDefinition { Key = "chart", Title = "MiniChart", Kind = kind, AspectName = "Progress" },
            ViewGridDashboardWidgetKind.Timeline => new ViewGridDashboardWidgetDefinition { Key = "timeline", Title = "Timeline", Kind = kind, AspectName = "Detail" },
            ViewGridDashboardWidgetKind.Gallery => new ViewGridDashboardWidgetDefinition { Key = "gallery", Title = "Gallery", Kind = kind, AspectName = "Image" },
            ViewGridDashboardWidgetKind.Table => new ViewGridDashboardWidgetDefinition { Key = "table", Title = "Table", Kind = kind, AspectName = "Name" },
            ViewGridDashboardWidgetKind.Card => new ViewGridDashboardWidgetDefinition { Key = "card", Title = "Card", Kind = kind, AspectName = "Name" },
            _ => new ViewGridDashboardWidgetDefinition { Key = kind.ToString().ToLowerInvariant(), Title = kind.ToString(), Kind = kind, AspectName = "Name" }
        };
    }
}
