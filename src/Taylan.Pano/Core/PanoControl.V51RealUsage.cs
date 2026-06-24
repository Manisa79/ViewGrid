using System.ComponentModel;
using Taylan.Pano.Theming;

namespace Taylan.Pano.Core;

public enum Pano51UsageProfile
{
    AudixMedia,
    ThemeAudit,
    StabilityPilot
}

public sealed class Pano51UsageCheckResult
{
    public string Profile { get; set; } = string.Empty;
    public string Check { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
}

public partial class PanoControl
{
    [Category("Pano 5.1 - Real Usage")]
    [DefaultValue(true)]
    [Description("Pano 5.1 gerçek kullanım pilot profilini etkinleştirir: Audix medya, tema audit ve stabilite kontrolleri.")]
    public bool EnablePano51RealUsagePilot { get; set; } = true;

    [Category("Pano 5.1 - Real Usage")]
    [DefaultValue(true)]
    [Description("Audix/Plex/Spotify tarzı medya görünümlerinde albüm kapağı, playback state, placeholder, kalite rozeti ve video preview ayarlarını birlikte uygular.")]
    public bool EnableAudix51MediaPilot { get; set; } = true;

    [Category("Pano 5.1 - Real Usage")]
    [DefaultValue(true)]
    [Description("Koyu/açık/yüksek kontrast temalarda buton, combo, label, badge, kart ve medya overlay okunurluğunu daha sıkı doğrular.")]
    public bool EnableTheme51Audit { get; set; } = true;

    public void ApplyPano51RealUsageDefaults()
    {
        EnablePano51RealUsagePilot = true;
        EnableAudix51MediaPilot = true;
        EnableTheme51Audit = true;

        ApplyPano502HardeningDefaults();

        EnforceThemeAccessibility = true;
        AutoEnsureReadableTextColors = true;
        UseUnifiedThemeVisuals = true;
        ThemeStudioEnforceAccessibility = true;
        AutoApplyThemeToContextMenus = true;
        AutoApplyThemeToColumnHeaders = true;

        EnableSearchEverywhere = true;
        EnableCommandPalette = true;
        EnableEnterpriseLayout = true;
        EnableLayoutStudio = true;

        Invalidate();
    }

    public void ApplyAudix51MediaPilotDefaults()
    {
        ApplyAudix502MediaDefaults();
        EnablePano51RealUsagePilot = true;
        EnableAudix51MediaPilot = true;

        SetViewMode(PanoViewMode.Poster);
        TilePosterMode = true;
        TilePreferredWidth = Math.Max(TilePreferredWidth, 235);
        TilePreferredHeight = Math.Max(TilePreferredHeight, 318);
        TilePosterImageHeight = Math.Max(TilePosterImageHeight, 172);
        PosterPreferredWidth = Math.Max(PosterPreferredWidth, 235);
        PosterPreferredHeight = Math.Max(PosterPreferredHeight, 318);
        PosterImageHeight = Math.Max(PosterImageHeight, 184);

        ShowMediaOverlayButton = true;
        ShowMediaPlaybackState = true;
        ShowMediaNowPlayingBadge = true;
        ShowMediaEqualizerIndicator = true;
        ShowMediaQualityBadge = true;
        MediaVideoPreviewMode = true;
        MediaImageRoundedCorners = true;
        MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
        EnableMediaLazyLoading = true;
        EnableMediaImageCache = true;
        MediaMemoryCacheLimit = Math.Max(MediaMemoryCacheLimit, 512);

        ShowQuickFilterBar = true;
        ShowActiveFilterChips = true;
        Invalidate();
    }

    public void ApplyTheme51AuditDefaults(PanoThemeStudioPreset preset = PanoThemeStudioPreset.AudixDark)
    {
        EnableTheme51Audit = true;
        ApplyV35ThemeStudioPack(preset);
        EnforceThemeAccessibility = true;
        AutoEnsureReadableTextColors = true;
        UseUnifiedThemeVisuals = true;
        ThemeStudioEnforceAccessibility = true;
        AutoApplyThemeToColumnHeaders = true;
        AutoApplyThemeToContextMenus = true;
        ShowQuickFilterBar = true;
        ShowActiveFilterChips = true;
        Invalidate();
    }

    public IReadOnlyList<Pano51UsageCheckResult> RunPano51UsageChecks()
    {
        var result = new List<Pano51UsageCheckResult>();

        result.Add(new Pano51UsageCheckResult
        {
            Profile = "Audix",
            Check = "Media visual state",
            Passed = ShowMediaPlaybackState && ShowMediaOverlayButton && ShowMediaNowPlayingBadge && EnableMediaImageCache,
            Message = ShowMediaPlaybackState && ShowMediaOverlayButton && ShowMediaNowPlayingBadge && EnableMediaImageCache
                ? "Poster/Gallery/MediaTile/FilmStrip için play-pause state, now-playing rozeti ve cache aktif."
                : "Audix medya kartlarında çalma durumu veya cache görünürlüğü eksik olabilir."
        });

        result.Add(new Pano51UsageCheckResult
        {
            Profile = "Theme",
            Check = "Readability",
            Passed = EnforceThemeAccessibility && AutoEnsureReadableTextColors && ThemeStudioEnforceAccessibility,
            Message = EnforceThemeAccessibility && AutoEnsureReadableTextColors && ThemeStudioEnforceAccessibility
                ? "Light/Dark/High Contrast okunurluk guard aktif."
                : "Koyu/açık tema geçişlerinde bazı metinler düşük kontrast kalabilir."
        });

        result.Add(new Pano51UsageCheckResult
        {
            Profile = "Stability",
            Check = "Hardening defaults",
            Passed = EnableBuildRuntimeHardening && StrictThemeReadabilityGuard && StrictMediaRuntimeGuard,
            Message = EnableBuildRuntimeHardening && StrictThemeReadabilityGuard && StrictMediaRuntimeGuard
                ? "v50.2 hardening varsayılanları korunuyor."
                : "Hardening guard ayarlarından biri kapalı."
        });

        return result;
    }
}
