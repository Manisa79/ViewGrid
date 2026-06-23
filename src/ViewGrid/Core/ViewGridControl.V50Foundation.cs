using System.ComponentModel;

namespace ViewGrid.Core;

[Flags]
public enum ViewGridFeatureModule
{
    Core = 1,
    Media = 2,
    Analytics = 4,
    Kanban = 8,
    Timeline = 16,
    Dashboard = 32,
    PropertyGrid = 64,
    Explorer = 128,
    ThemeStudio = 256,
    Interaction = 512,
    LayoutStudio = 1024,
    Accessibility = 2048,
    All = Core | Media | Analytics | Kanban | Timeline | Dashboard | PropertyGrid | Explorer | ThemeStudio | Interaction | LayoutStudio | Accessibility
}

public sealed class ViewGridModuleProfileInfo
{
    public string Name { get; set; } = string.Empty;
    public ViewGridFeatureModule Modules { get; set; } = ViewGridFeatureModule.Core;
    public string Description { get; set; } = string.Empty;
}

public sealed class ViewGridStabilityCheckResult
{
    public string CheckName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
}

public partial class ViewGridControl
{
    [Category("ViewGrid 5.0 - Modules")]
    [DefaultValue(ViewGridFeatureModule.All)]
    [Description("ViewGrid 5.0 modüler kullanım profili. Audix, AOI, FactoryOS gibi uygulamalarda sadece gereken deneyim paketlerini aktif etmeyi kolaylaştırır.")]
    public ViewGridFeatureModule EnabledFeatureModules { get; set; } = ViewGridFeatureModule.All;

    [Category("ViewGrid 5.0 - Stability")]
    [DefaultValue(true)]
    [Description("Yeni faz özellikleri açıkken erişilebilir tema, güvenli medya durumu ve örnek merkez uyumluluğu gibi koruma ayarlarını otomatik uygular.")]
    public bool EnableViewGrid5SafetyDefaults { get; set; } = true;

    public void ApplyViewGrid5FoundationDefaults()
    {
        EnabledFeatureModules = ViewGridFeatureModule.All;
        EnableViewGrid5SafetyDefaults = true;
        EnforceThemeAccessibility = true;
        UseUnifiedThemeVisuals = true;
        AutoApplyThemeToColumnHeaders = true;
        AutoApplyThemeToContextMenus = true;
        ThemeStudioEnforceAccessibility = true;
        ShowMediaPlaybackState = true;
        ShowMediaNowPlayingBadge = true;
        ShowMediaEqualizerIndicator = true;
        ShowMediaOverlayButton = true;
        EnableMediaPro = true;
        EnableEnterpriseLayout = true;
        EnableSearchEverywhere = true;
        EnableLayoutStudio = true;
        EnableDashboardBuilder = true;
        EnableThemeStudio = true;
        EnableCommandPalette = true;
        Invalidate();
    }

    public void ApplyAudixMediaProfile()
    {
        EnabledFeatureModules = ViewGridFeatureModule.Core | ViewGridFeatureModule.Media | ViewGridFeatureModule.Explorer | ViewGridFeatureModule.ThemeStudio | ViewGridFeatureModule.Interaction | ViewGridFeatureModule.LayoutStudio | ViewGridFeatureModule.Accessibility;
        ApplyV36MediaProPack();
        ShowMediaPlaybackState = true;
        ShowMediaNowPlayingBadge = true;
        ShowMediaEqualizerIndicator = true;
        ShowMediaOverlayButton = true;
        MediaVideoPreviewMode = true;
        MediaImageRoundedCorners = true;
        MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
        EnableThemeStudio = true;
        ThemeStudioEnforceAccessibility = true;
        EnableCommandPalette = true;
        EnableSearchEverywhere = true;
        Invalidate();
    }

    public void ApplyFactoryIntelligenceProfile()
    {
        EnabledFeatureModules = ViewGridFeatureModule.Core | ViewGridFeatureModule.Analytics | ViewGridFeatureModule.Kanban | ViewGridFeatureModule.Timeline | ViewGridFeatureModule.Dashboard | ViewGridFeatureModule.ThemeStudio | ViewGridFeatureModule.Accessibility;
        EnableDashboardBuilder = true;
        EnableVisualAnalyticsPro = true;
        EnableThemeStudio = true;
        ThemeStudioEnforceAccessibility = true;
        EnforceThemeAccessibility = true;
        Invalidate();
    }

    public void ApplyAoiSupportDeskProfile()
    {
        EnabledFeatureModules = ViewGridFeatureModule.Core | ViewGridFeatureModule.Kanban | ViewGridFeatureModule.Timeline | ViewGridFeatureModule.Dashboard | ViewGridFeatureModule.Interaction | ViewGridFeatureModule.ThemeStudio | ViewGridFeatureModule.Accessibility;
        EnableSearchEverywhere = true;
        EnableCommandPalette = true;
        EnableThemeStudio = true;
        ThemeStudioEnforceAccessibility = true;
        EnforceThemeAccessibility = true;
        Invalidate();
    }

    public IReadOnlyList<ViewGridModuleProfileInfo> GetViewGrid5ModuleProfiles()
    {
        return new List<ViewGridModuleProfileInfo>
        {
            new ViewGridModuleProfileInfo { Name = "Audix Media", Modules = ViewGridFeatureModule.Core | ViewGridFeatureModule.Media | ViewGridFeatureModule.Explorer | ViewGridFeatureModule.Interaction | ViewGridFeatureModule.ThemeStudio | ViewGridFeatureModule.Accessibility, Description = "Albüm kapağı, video thumbnail, play/pause state, FilmStrip, Poster, Gallery ve medya cache odaklı profil." },
            new ViewGridModuleProfileInfo { Name = "AOI Support Desk", Modules = ViewGridFeatureModule.Core | ViewGridFeatureModule.Kanban | ViewGridFeatureModule.Timeline | ViewGridFeatureModule.Dashboard | ViewGridFeatureModule.Interaction | ViewGridFeatureModule.Accessibility, Description = "Ticket kartları, Kanban, Timeline, SLA rozetleri, hızlı arama ve koyu/açık tema okunurluğu." },
            new ViewGridModuleProfileInfo { Name = "Factory Intelligence", Modules = ViewGridFeatureModule.Core | ViewGridFeatureModule.Analytics | ViewGridFeatureModule.Dashboard | ViewGridFeatureModule.Timeline | ViewGridFeatureModule.ThemeStudio | ViewGridFeatureModule.Accessibility, Description = "HeatMap, KPI, mini chart, üretim durumu ve FactoryOS dashboard akışı." },
            new ViewGridModuleProfileInfo { Name = "MasterData", Modules = ViewGridFeatureModule.Core | ViewGridFeatureModule.PropertyGrid | ViewGridFeatureModule.LayoutStudio | ViewGridFeatureModule.Interaction | ViewGridFeatureModule.Accessibility, Description = "Kolon hafızası, property card, detay satırları, filtre presetleri ve stabil grid kullanımı." },
            new ViewGridModuleProfileInfo { Name = "Bilge Defter", Modules = ViewGridFeatureModule.Core | ViewGridFeatureModule.Media | ViewGridFeatureModule.Dashboard | ViewGridFeatureModule.LayoutStudio | ViewGridFeatureModule.Accessibility, Description = "Öğrenci fotoğrafları, Gallery/Card görünümü, ödeme/yoklama dashboard ve çıktı hazırlığı." }
        };
    }

    public IReadOnlyList<ViewGridStabilityCheckResult> RunViewGrid5RuntimeChecks()
    {
        var result = new List<ViewGridStabilityCheckResult>();
        result.Add(new ViewGridStabilityCheckResult { CheckName = "Theme Accessibility", Passed = EnforceThemeAccessibility && ThemeStudioEnforceAccessibility, Message = EnforceThemeAccessibility && ThemeStudioEnforceAccessibility ? "Okunurluk koruması aktif." : "Koyu/açık tema okunurluk koruması kapalı." });
        result.Add(new ViewGridStabilityCheckResult { CheckName = "Media Playback", Passed = ShowMediaPlaybackState, Message = ShowMediaPlaybackState ? "Audio/video playback state görünür." : "Play tuşu çalışsa bile kart state'i görünmeyebilir." });
        result.Add(new ViewGridStabilityCheckResult { CheckName = "Command Palette", Passed = EnableCommandPalette, Message = EnableCommandPalette ? "Ctrl+K hızlı komut akışı aktif." : "Command Palette kapalı." });
        result.Add(new ViewGridStabilityCheckResult { CheckName = "Module Profile", Passed = EnabledFeatureModules != 0, Message = EnabledFeatureModules == 0 ? "Hiç modül seçili değil." : EnabledFeatureModules.ToString() });
        return result;
    }
}
