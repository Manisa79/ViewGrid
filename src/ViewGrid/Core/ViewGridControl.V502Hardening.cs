using System.ComponentModel;

namespace ViewGrid.Core;

public sealed class ViewGridHardeningCheckResult
{
    public string Area { get; set; } = string.Empty;
    public string Check { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
}

public partial class ViewGridControl
{
    [Category("ViewGrid 5.0 - Hardening")]
    [DefaultValue(true)]
    [Description("Proje entegrasyonunda güvenli varsayılanları kullanır: tema kontrastı, medya cache, playback state, layout ve interaction ayarlarını dengeli hale getirir.")]
    public bool EnableBuildRuntimeHardening { get; set; } = true;

    [Category("ViewGrid 5.0 - Hardening")]
    [DefaultValue(true)]
    [Description("Host uygulama profillerinde koyu/açık tema okunurluğunu daha sıkı korur.")]
    public bool StrictThemeReadabilityGuard { get; set; } = true;

    [Category("ViewGrid 5.0 - Hardening")]
    [DefaultValue(true)]
    [Description("Medya görünümlerinde placeholder, cache, lazy loading ve playback state ayarlarını birlikte güvenli hale getirir.")]
    public bool StrictMediaRuntimeGuard { get; set; } = true;

    public void ApplyViewGrid502HardeningDefaults()
    {
        EnableBuildRuntimeHardening = true;
        StrictThemeReadabilityGuard = true;
        StrictMediaRuntimeGuard = true;

        ApplyViewGrid5FoundationDefaults();

        EnforceThemeAccessibility = true;
        UseUnifiedThemeVisuals = true;
        AutoApplyThemeToColumnHeaders = true;
        AutoApplyThemeToContextMenus = true;
        EnableThemeStudio = true;
        ThemeStudioEnforceAccessibility = true;

        EnableMediaPro = true;
        EnableMediaLazyLoading = true;
        EnableMediaImageCache = true;
        MediaMemoryCacheLimit = Math.Max(MediaMemoryCacheLimit, 256);
        ShowMediaPlaybackState = true;
        ShowMediaNowPlayingBadge = true;
        ShowMediaEqualizerIndicator = true;
        ShowMediaOverlayButton = true;

        EnableEnterpriseLayout = true;
        EnableSearchEverywhere = true;
        EnableCommandPalette = true;
        EnableLayoutStudio = true;

        Invalidate();
    }

    public void ApplyAudix502MediaDefaults()
    {
        ApplyAudixMediaProfile();
        EnableBuildRuntimeHardening = true;
        StrictMediaRuntimeGuard = true;
        EnableMediaLazyLoading = true;
        EnableMediaImageCache = true;
        MediaMemoryCacheLimit = Math.Max(MediaMemoryCacheLimit, 512);
        ShowMediaPlaybackState = true;
        ShowMediaNowPlayingBadge = true;
        ShowMediaEqualizerIndicator = true;
        ShowMediaOverlayButton = true;
        MediaVideoPreviewMode = true;
        MediaImageRoundedCorners = true;
        MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
        SetViewMode(ViewGridMode.Poster);
        Invalidate();
    }

    public IReadOnlyList<ViewGridHardeningCheckResult> RunViewGrid502RuntimeHardeningChecks()
    {
        var checks = new List<ViewGridHardeningCheckResult>();

        checks.Add(new ViewGridHardeningCheckResult
        {
            Area = "Theme",
            Check = "Accessibility Guard",
            Passed = EnforceThemeAccessibility && ThemeStudioEnforceAccessibility && UseUnifiedThemeVisuals,
            Message = EnforceThemeAccessibility && ThemeStudioEnforceAccessibility && UseUnifiedThemeVisuals
                ? "Koyu/açık tema okunurluk koruması aktif."
                : "Tema okunurluk ayarlarından biri kapalı."
        });

        checks.Add(new ViewGridHardeningCheckResult
        {
            Area = "Media",
            Check = "Image Cache + Lazy Loading",
            Passed = EnableMediaLazyLoading && EnableMediaImageCache && MediaMemoryCacheLimit >= 128,
            Message = EnableMediaLazyLoading && EnableMediaImageCache
                ? $"Medya cache aktif. Limit: {MediaMemoryCacheLimit}"
                : "Medya cache/lazy loading kapalı."
        });

        checks.Add(new ViewGridHardeningCheckResult
        {
            Area = "Media",
            Check = "Playback State",
            Passed = ShowMediaPlaybackState && ShowMediaOverlayButton,
            Message = ShowMediaPlaybackState && ShowMediaOverlayButton
                ? "Play/Pause state ve overlay görünür."
                : "Play tuşuna basıldığında kart state'i yeterince görünmeyebilir."
        });

        checks.Add(new ViewGridHardeningCheckResult
        {
            Area = "Interaction",
            Check = "Search + Command",
            Passed = EnableSearchEverywhere && EnableCommandPalette,
            Message = EnableSearchEverywhere && EnableCommandPalette
                ? "Search Everywhere ve Command Palette aktif."
                : "Hızlı arama/komut akışlarından biri kapalı."
        });

        checks.Add(new ViewGridHardeningCheckResult
        {
            Area = "Layout",
            Check = "Enterprise Layout",
            Passed = EnableEnterpriseLayout && EnableLayoutStudio,
            Message = EnableEnterpriseLayout && EnableLayoutStudio
                ? "Layout kaydetme/studio akışı aktif."
                : "Layout profili eksik olabilir."
        });

        return checks;
    }
}
