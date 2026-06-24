# Taylan.Pano v29.6 - Media Library Views

Bu güncelleme Pano kart/poster altyapısını medya listeleri için güçlendirir.

## Eklenenler

- `PanoViewMode.MediaTile`: Albüm kapağı, film afişi, öğrenci/makine fotoğrafı gibi kompakt medya kutuları.
- `PanoViewMode.FilmStrip`: Büyük görsel + sağ tarafta metin alanı olan yatay medya şeridi.
- `PanoMediaImageScaleMode`: `Contain`, `Cover`, `Stretch` görsel yerleşim seçenekleri.
- `MediaImageScaleMode` ve `MediaImageRoundedCorners` propertyleri.
- Poster çiziminde yuvarlatılmış görsel kırpma ve sınır çizgisi.
- Example Center içine `Media Library / Albüm-Film-Fotoğraf` örneği.

## Kullanım

```csharp
pano.Columns.Add(new PanoColumn("Kapak", nameof(Row.Cover))
{
    Kind = PanoColumnKind.Image,
    ImageGetter = row => ((Row)row).CoverImage
});

pano.MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
pano.SetViewMode(PanoViewMode.Poster);
// veya
pano.SetViewMode(PanoViewMode.MediaTile);
pano.SetViewMode(PanoViewMode.FilmStrip);
```

## Örnek senaryolar

- Audix: albüm kapağı + şarkı/sanatçı/albüm/süre.
- Film arşivi: poster + başlık/tür/yıl/rating.
- AOI Workspace: hata fotoğrafı veya komponent karesi + ticket bilgisi.
- Factory Navigator: makine fotoğrafı + hat/durum.
- Bilge Defter: öğrenci fotoğrafı + sınıf/durum.
