# ViewGrid v41 - Media Playback State Suite

Audix ve video arşivleri için medya kartlarına gerçek playback state desteği eklendi.

## Eklenenler

- `ViewGridMediaPlaybackState`: `None`, `Loading`, `Playing`, `Paused`, `Error`
- `ViewGridMediaKind`: `Audio`, `Video`, `Image`, `Document`
- `ShowMediaPlaybackState`
- `ShowMediaNowPlayingBadge`
- `ShowMediaEqualizerIndicator`
- `MediaPlaybackStateGetter`
- `MediaKindGetter`
- `MediaPlayPauseClicked`
- Video için `MediaVideoPreviewMode`

## Audix örnek kullanım

```csharp
viewgrid.ShowMediaOverlayButton = true;
viewgrid.ShowMediaPlaybackState = true;
viewgrid.ShowMediaNowPlayingBadge = true;
viewgrid.ShowMediaEqualizerIndicator = true;
viewgrid.MediaKindGetter = row => ViewGridMediaKind.Audio;
viewgrid.MediaPlaybackStateGetter = row =>
{
    var track = row as TrackItem;
    if (track == null) return ViewGridMediaPlaybackState.None;
    if (track.IsLoading) return ViewGridMediaPlaybackState.Loading;
    if (track.IsPlaying) return ViewGridMediaPlaybackState.Playing;
    if (track.IsPaused) return ViewGridMediaPlaybackState.Paused;
    return ViewGridMediaPlaybackState.None;
};

viewgrid.MediaPlayPauseClicked += (s, e) =>
{
    var track = e.RowObject as TrackItem;
    if (track == null) return;

    if (e.CurrentState == ViewGridMediaPlaybackState.Playing)
        Pause(track);
    else
        Play(track);

    viewgrid.RefreshMediaPlayback();
};
```

## Video davranışı

Video dosyalarında aynı overlay sistemi çalışır. `MediaKindGetter` video döndürürse host uygulama `MediaPlayPauseClicked` içinde ister gömülü preview paneli, ister harici player açabilir.

```csharp
viewgrid.MediaVideoPreviewMode = true;
viewgrid.MediaKindGetter = row => ((MediaItem)row).IsVideo ? ViewGridMediaKind.Video : ViewGridMediaKind.Audio;
```

ViewGrid player motoru olmaya çalışmaz; doğru davranış, playback UI state ve tıklama olayını sağlamaktır. Gerçek oynatma Audix/video player tarafında yapılır.
