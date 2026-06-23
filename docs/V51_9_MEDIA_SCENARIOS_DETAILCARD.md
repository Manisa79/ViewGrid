# ViewGrid v51.9 - Media Scenarios + Media DetailCard

Bu sürüm medya odaklı kullanım için görünüm senaryosu menüsünü genişletir ve DetailCard görünümüne kapak/afiş solda, metadata sağda çalışan medya düzeni ekler.

## Eklenen görünüm senaryoları

`Görünüm Senaryosu` menüsünde `Medya / Görsel Senaryolar` ayracı altında şu seçenekler bulunur:

- Medya Kütüphanesi
- Albüm / Müzik Arşivi
- Film / Video Afişleri
- Fotoğraf Galerisi
- MediaTile Kartları
- FilmStrip / Yatay Şerit
- Media DetailCard
- Now Playing / Çalan Medya
- Video Preview
- Doküman Önizleme

## Media DetailCard

Yeni layout:

```csharp
grid.ViewMode = ViewGridMode.DetailCard;
grid.DetailCardLayout = ViewGridDetailCardLayout.Media;
grid.DetailCardMediaImageWidth = 158;
grid.DetailCardMediaImageHeight = 178;
```

Bu modda `ViewGridColumnKind.Image` veya `ViewGridColumnKind.Icon` kolonundaki kapak görseli sol tarafta çizilir. Diğer kolonlar sağ tarafta property/metadata olarak gösterilir.

## Playback API

Bu sürümde medya playback API'si core içinde mevcuttur:

```csharp
grid.ShowMediaPlaybackState = true;
grid.ShowMediaNowPlayingBadge = true;
grid.ShowMediaEqualizerIndicator = true;
grid.MediaPlaybackStateGetter = row => ((TrackRow)row).PlaybackState;
grid.MediaKindGetter = row => ((TrackRow)row).Kind;
grid.MediaPlayPauseClicked += (_, e) => TogglePlayback(e.RowObject, e.CurrentState);
grid.RefreshMediaPlayback();
```

Not: Bu API'ler görünmüyorsa uygulama eski ViewGrid.dll referansını kullanıyor olabilir. `bin/obj` klasörleri temizlenip proje referansı son `src/ViewGrid` projesine yönlendirilmelidir.
