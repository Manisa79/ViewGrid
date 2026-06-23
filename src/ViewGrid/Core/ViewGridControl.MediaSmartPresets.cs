using System.ComponentModel;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    [Category("ViewGrid - Media Smart Presets")]
    [DefaultValue(ViewGridMediaSmartPreset.Music)]
    [Description("Runtime menüden uygulanan son medya akıllı preset türü.")]
    public ViewGridMediaSmartPreset MediaSmartPreset { get; set; } = ViewGridMediaSmartPreset.Music;

    public void ApplyMediaSmartPreset(ViewGridMediaSmartPreset preset)
    {
        MediaSmartPreset = preset;
        ApplyAudix51MediaPilotDefaults();
        EnableMediaPro = true;
        EnableMediaLazyLoading = true;
        EnableMediaImageCache = true;
        MediaImageRoundedCorners = true;
        ShowMediaOverlayButton = true;
        ShowMediaQualityBadge = true;
        ShowMediaPlaybackState = preset is ViewGridMediaSmartPreset.Music or ViewGridMediaSmartPreset.Movie;
        ShowMediaNowPlayingBadge = preset == ViewGridMediaSmartPreset.Music;
        ShowMediaEqualizerIndicator = preset == ViewGridMediaSmartPreset.Music;
        ApplyV38PerformanceProfile(ViewGridV38PerformancePreset.MediaLibrary);

        switch (preset)
        {
            case ViewGridMediaSmartPreset.Music:
                MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
                MediaQualityBadgeAspectName = string.IsNullOrWhiteSpace(MediaQualityBadgeAspectName) ? "Quality" : MediaQualityBadgeAspectName;
                DetailCardLayout = ViewGridDetailCardLayout.Media;
                DetailCardMediaImageWidth = Math.Max(150, DetailCardMediaImageWidth);
                DetailCardMediaImageHeight = Math.Max(170, DetailCardMediaImageHeight);
                SetViewMode(ViewGridMode.MediaTile);
                break;

            case ViewGridMediaSmartPreset.Movie:
                MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
                MediaQualityBadgeAspectName = string.IsNullOrWhiteSpace(MediaQualityBadgeAspectName) ? "Quality" : MediaQualityBadgeAspectName;
                DetailCardLayout = ViewGridDetailCardLayout.PosterLeft;
                DetailCardMediaImageWidth = Math.Max(190, DetailCardMediaImageWidth);
                DetailCardMediaImageHeight = Math.Max(260, DetailCardMediaImageHeight);
                SetViewMode(ViewGridMode.Poster);
                break;

            case ViewGridMediaSmartPreset.Photo:
                MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
                DetailCardLayout = ViewGridDetailCardLayout.Media;
                DetailCardMediaImageWidth = Math.Max(170, DetailCardMediaImageWidth);
                DetailCardMediaImageHeight = Math.Max(150, DetailCardMediaImageHeight);
                SetViewMode(ViewGridMode.Gallery);
                break;

            case ViewGridMediaSmartPreset.Document:
                MediaImageScaleMode = ViewGridMediaImageScaleMode.Contain;
                ShowMediaPlaybackState = false;
                ShowMediaNowPlayingBadge = false;
                ShowMediaEqualizerIndicator = false;
                DetailCardLayout = ViewGridDetailCardLayout.PosterLeft;
                DetailCardMediaImageWidth = Math.Max(160, DetailCardMediaImageWidth);
                DetailCardMediaImageHeight = Math.Max(220, DetailCardMediaImageHeight);
                SetViewMode(ViewGridMode.DetailCard);
                break;
        }

        RefreshView();
    }
}
