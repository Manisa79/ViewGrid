using System.ComponentModel;

namespace Taylan.Pano.Core;

public partial class PanoControl
{
    [Category("Pano - Media Smart Presets")]
    [DefaultValue(PanoMediaSmartPreset.Music)]
    [Description("Runtime menüden uygulanan son medya akıllı preset türü.")]
    public PanoMediaSmartPreset MediaSmartPreset { get; set; } = PanoMediaSmartPreset.Music;

    public void ApplyMediaSmartPreset(PanoMediaSmartPreset preset)
    {
        MediaSmartPreset = preset;
        ApplyAudix51MediaPilotDefaults();
        EnableMediaPro = true;
        EnableMediaLazyLoading = true;
        EnableMediaImageCache = true;
        MediaImageRoundedCorners = true;
        ShowMediaOverlayButton = true;
        ShowMediaQualityBadge = true;
        ShowMediaPlaybackState = preset is PanoMediaSmartPreset.Music or PanoMediaSmartPreset.Movie;
        ShowMediaNowPlayingBadge = preset == PanoMediaSmartPreset.Music;
        ShowMediaEqualizerIndicator = preset == PanoMediaSmartPreset.Music;
        ApplyV38PerformanceProfile(PanoV38PerformancePreset.MediaLibrary);

        switch (preset)
        {
            case PanoMediaSmartPreset.Music:
                MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
                MediaQualityBadgeAspectName = string.IsNullOrWhiteSpace(MediaQualityBadgeAspectName) ? "Quality" : MediaQualityBadgeAspectName;
                DetailCardLayout = PanoDetailCardLayout.Media;
                DetailCardMediaImageWidth = Math.Max(150, DetailCardMediaImageWidth);
                DetailCardMediaImageHeight = Math.Max(170, DetailCardMediaImageHeight);
                SetViewMode(PanoViewMode.MediaTile);
                break;

            case PanoMediaSmartPreset.Movie:
                MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
                MediaQualityBadgeAspectName = string.IsNullOrWhiteSpace(MediaQualityBadgeAspectName) ? "Quality" : MediaQualityBadgeAspectName;
                DetailCardLayout = PanoDetailCardLayout.PosterLeft;
                DetailCardMediaImageWidth = Math.Max(190, DetailCardMediaImageWidth);
                DetailCardMediaImageHeight = Math.Max(260, DetailCardMediaImageHeight);
                SetViewMode(PanoViewMode.Poster);
                break;

            case PanoMediaSmartPreset.Photo:
                MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
                DetailCardLayout = PanoDetailCardLayout.Media;
                DetailCardMediaImageWidth = Math.Max(170, DetailCardMediaImageWidth);
                DetailCardMediaImageHeight = Math.Max(150, DetailCardMediaImageHeight);
                SetViewMode(PanoViewMode.Gallery);
                break;

            case PanoMediaSmartPreset.Document:
                MediaImageScaleMode = PanoMediaImageScaleMode.Contain;
                ShowMediaPlaybackState = false;
                ShowMediaNowPlayingBadge = false;
                ShowMediaEqualizerIndicator = false;
                DetailCardLayout = PanoDetailCardLayout.PosterLeft;
                DetailCardMediaImageWidth = Math.Max(160, DetailCardMediaImageWidth);
                DetailCardMediaImageHeight = Math.Max(220, DetailCardMediaImageHeight);
                SetViewMode(PanoViewMode.DetailCard);
                break;
        }

        RefreshView();
    }
}
