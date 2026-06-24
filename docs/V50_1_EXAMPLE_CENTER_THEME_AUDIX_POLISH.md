# Taylan.Pano v50.1 - Example Center + Theme + Audix Polish

Bu paket Pano 5.0 Foundation üzerine pratik kullanım düzenlemesi getirir.

## Ana hedefler

- Example Center içinde özellikleri bulmayı kolaylaştırmak.
- Kategori + arama destekli örnek merkezi akışı eklemek.
- Audix/Media, Theme/Okunurluk, Stability/Build, Performance, Analytics, Factory/AOI, Layout/Interaction ve Timeline/Kanban başlıklarını ayrı filtrelenebilir hale getirmek.
- Pano 5.0 Foundation/Stability örneklerini Example Center Pro içine görünür şekilde bağlamak.

## Eklenen örnekler

- `31 v50 Foundation / Stability`
- `32 v50.1 Example Center Navigator`

## Audix için kullanım notu

Audix medya ekranlarında önerilen başlangıç:

```csharp
pano.ApplyAudixMediaProfile();
pano.SetViewMode(PanoViewMode.Poster);
pano.MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
pano.ShowMediaPlaybackState = true;
pano.ShowMediaNowPlayingBadge = true;
pano.ShowMediaEqualizerIndicator = true;
```

Video dosyalarında aynı playback state altyapısı kullanılabilir. Video için host uygulama `MediaVideoPreviewMode` ile preview davranışını açabilir.

## Build notu

Bu ortamda `dotnet` bulunmadığı için gerçek build çalıştırılamadı. Statik API guard çalıştırıldı.
