using System.ComponentModel;

namespace Taylan.Pano.Core;

[Flags]
public enum PanoFeatureModule
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

public sealed class PanoModuleProfileInfo
{
    public string Name { get; set; } = string.Empty;
    public PanoFeatureModule Modules { get; set; } = PanoFeatureModule.Core;
    public string Description { get; set; } = string.Empty;
}

public sealed class PanoStabilityCheckResult
{
    public string CheckName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
}

public partial class PanoControl
{
    [Category("Pano 5.0 - Modules")]
    [DefaultValue(PanoFeatureModule.All)]
    [Description("Pano 5.0 modüler kullanım profili. Audix, AOI, FactoryOS gibi uygulamalarda sadece gereken deneyim paketlerini aktif etmeyi kolaylaştırır.")]
    public PanoFeatureModule EnabledFeatureModules { get; set; } = PanoFeatureModule.All;

    [Category("Pano 5.0 - Stability")]
    [DefaultValue(true)]
    [Description("Yeni faz özellikleri açıkken erişilebilir tema, güvenli medya durumu ve örnek merkez uyumluluğu gibi koruma ayarlarını otomatik uygular.")]
    public bool EnablePano5SafetyDefaults { get; set; } = true;

    public void ApplyPano5FoundationDefaults()
    {
        EnabledFeatureModules = PanoFeatureModule.All;
        EnablePano5SafetyDefaults = true;
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
        EnabledFeatureModules = PanoFeatureModule.Core | PanoFeatureModule.Media | PanoFeatureModule.Explorer | PanoFeatureModule.ThemeStudio | PanoFeatureModule.Interaction | PanoFeatureModule.LayoutStudio | PanoFeatureModule.Accessibility;
        ApplyV36MediaProPack();
        ShowMediaPlaybackState = true;
        ShowMediaNowPlayingBadge = true;
        ShowMediaEqualizerIndicator = true;
        ShowMediaOverlayButton = true;
        MediaVideoPreviewMode = true;
        MediaImageRoundedCorners = true;
        MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
        EnableThemeStudio = true;
        ThemeStudioEnforceAccessibility = true;
        EnableCommandPalette = true;
        EnableSearchEverywhere = true;
        Invalidate();
    }

    public void ApplyFactoryIntelligenceProfile()
    {
        EnabledFeatureModules = PanoFeatureModule.Core | PanoFeatureModule.Analytics | PanoFeatureModule.Kanban | PanoFeatureModule.Timeline | PanoFeatureModule.Dashboard | PanoFeatureModule.ThemeStudio | PanoFeatureModule.Accessibility;
        EnableDashboardBuilder = true;
        EnableVisualAnalyticsPro = true;
        EnableThemeStudio = true;
        ThemeStudioEnforceAccessibility = true;
        EnforceThemeAccessibility = true;
        Invalidate();
    }

    public void ApplyAoiSupportDeskProfile()
    {
        EnabledFeatureModules = PanoFeatureModule.Core | PanoFeatureModule.Kanban | PanoFeatureModule.Timeline | PanoFeatureModule.Dashboard | PanoFeatureModule.Interaction | PanoFeatureModule.ThemeStudio | PanoFeatureModule.Accessibility;
        EnableSearchEverywhere = true;
        EnableCommandPalette = true;
        EnableThemeStudio = true;
        ThemeStudioEnforceAccessibility = true;
        EnforceThemeAccessibility = true;
        Invalidate();
    }

    public IReadOnlyList<PanoModuleProfileInfo> GetPano5ModuleProfiles()
    {
        return new List<PanoModuleProfileInfo>
        {
            new PanoModuleProfileInfo { Name = "Audix Media", Modules = PanoFeatureModule.Core | PanoFeatureModule.Media | PanoFeatureModule.Explorer | PanoFeatureModule.Interaction | PanoFeatureModule.ThemeStudio | PanoFeatureModule.Accessibility, Description = "Albüm kapağı, video thumbnail, play/pause state, FilmStrip, Poster, Gallery ve medya cache odaklı profil." },
            new PanoModuleProfileInfo { Name = "AOI Support Desk", Modules = PanoFeatureModule.Core | PanoFeatureModule.Kanban | PanoFeatureModule.Timeline | PanoFeatureModule.Dashboard | PanoFeatureModule.Interaction | PanoFeatureModule.Accessibility, Description = "Ticket kartları, Kanban, Timeline, SLA rozetleri, hızlı arama ve koyu/açık tema okunurluğu." },
            new PanoModuleProfileInfo { Name = "Factory Intelligence", Modules = PanoFeatureModule.Core | PanoFeatureModule.Analytics | PanoFeatureModule.Dashboard | PanoFeatureModule.Timeline | PanoFeatureModule.ThemeStudio | PanoFeatureModule.Accessibility, Description = "HeatMap, KPI, mini chart, üretim durumu ve FactoryOS dashboard akışı." },
            new PanoModuleProfileInfo { Name = "MasterData", Modules = PanoFeatureModule.Core | PanoFeatureModule.PropertyGrid | PanoFeatureModule.LayoutStudio | PanoFeatureModule.Interaction | PanoFeatureModule.Accessibility, Description = "Kolon hafızası, property card, detay satırları, filtre presetleri ve stabil grid kullanımı." },
            new PanoModuleProfileInfo { Name = "Bilge Defter", Modules = PanoFeatureModule.Core | PanoFeatureModule.Media | PanoFeatureModule.Dashboard | PanoFeatureModule.LayoutStudio | PanoFeatureModule.Accessibility, Description = "Öğrenci fotoğrafları, Gallery/Card görünümü, ödeme/yoklama dashboard ve çıktı hazırlığı." }
        };
    }

    public IReadOnlyList<PanoStabilityCheckResult> RunPano5RuntimeChecks()
    {
        var result = new List<PanoStabilityCheckResult>();
        result.Add(new PanoStabilityCheckResult { CheckName = "Theme Accessibility", Passed = EnforceThemeAccessibility && ThemeStudioEnforceAccessibility, Message = EnforceThemeAccessibility && ThemeStudioEnforceAccessibility ? "Okunurluk koruması aktif." : "Koyu/açık tema okunurluk koruması kapalı." });
        result.Add(new PanoStabilityCheckResult { CheckName = "Media Playback", Passed = ShowMediaPlaybackState, Message = ShowMediaPlaybackState ? "Audio/video playback state görünür." : "Play tuşu çalışsa bile kart state'i görünmeyebilir." });
        result.Add(new PanoStabilityCheckResult { CheckName = "Command Palette", Passed = EnableCommandPalette, Message = EnableCommandPalette ? "Ctrl+K hızlı komut akışı aktif." : "Command Palette kapalı." });
        result.Add(new PanoStabilityCheckResult { CheckName = "Module Profile", Passed = EnabledFeatureModules != 0, Message = EnabledFeatureModules == 0 ? "Hiç modül seçili değil." : EnabledFeatureModules.ToString() });
        return result;
    }
}
