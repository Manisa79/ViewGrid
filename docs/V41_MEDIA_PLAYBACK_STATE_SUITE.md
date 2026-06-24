# Taylan.Pano v41 - Media Playback State Suite

Audix ve video arşivleri için medya kartlarına gerçek playback state desteği eklendi.

## Eklenenler

- `PanoMediaPlaybackState`: `None`, `Loading`, `Playing`, `Paused`, `Error`
- `PanoMediaKind`: `Audio`, `Video`, `Image`, `Document`
- `ShowMediaPlaybackState`
- `ShowMediaNowPlayingBadge`
- `ShowMediaEqualizerIndicator`
- `MediaPlaybackStateGetter`
- `MediaKindGetter`
- `MediaPlayPauseClicked`
- Video için `MediaVideoPreviewMode`

## Audix örnek kullanım

```csharp
pano.ShowMediaOverlayButton = true;
pano.ShowMediaPlaybackState = true;
pano.ShowMediaNowPlayingBadge = true;
pano.ShowMediaEqualizerIndicator = true;
pano.MediaKindGetter = row => PanoMediaKind.Audio;
pano.MediaPlaybackStateGetter = row =>
{
    var track = row as TrackItem;
    if (track == null) return PanoMediaPlaybackState.None;
    if (track.IsLoading) return PanoMediaPlaybackState.Loading;
    if (track.IsPlaying) return PanoMediaPlaybackState.Playing;
    if (track.IsPaused) return PanoMediaPlaybackState.Paused;
    return PanoMediaPlaybackState.None;
};

pano.MediaPlayPauseClicked += (s, e) =>
{
    var track = e.RowObject as TrackItem;
    if (track == null) return;

    if (e.CurrentState == PanoMediaPlaybackState.Playing)
        Pause(track);
    else
        Play(track);

    pano.RefreshMediaPlayback();
};
```

## Video davranışı

Video dosyalarında aynı overlay sistemi çalışır. `MediaKindGetter` video döndürürse host uygulama `MediaPlayPauseClicked` içinde ister gömülü preview paneli, ister harici player açabilir.

```csharp
pano.MediaVideoPreviewMode = true;
pano.MediaKindGetter = row => ((MediaItem)row).IsVideo ? PanoMediaKind.Video : PanoMediaKind.Audio;
```

Pano player motoru olmaya çalışmaz; doğru davranış, playback UI state ve tıklama olayını sağlamaktır. Gerçek oynatma Audix/video player tarafında yapılır.
