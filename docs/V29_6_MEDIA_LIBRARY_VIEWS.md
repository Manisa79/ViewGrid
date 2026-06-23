# ViewGrid v29.6 - Media Library Views

Bu güncelleme ViewGrid kart/poster altyapısını medya listeleri için güçlendirir.

## Eklenenler

- `ViewGridMode.MediaTile`: Albüm kapağı, film afişi, öğrenci/makine fotoğrafı gibi kompakt medya kutuları.
- `ViewGridMode.FilmStrip`: Büyük görsel + sağ tarafta metin alanı olan yatay medya şeridi.
- `ViewGridMediaImageScaleMode`: `Contain`, `Cover`, `Stretch` görsel yerleşim seçenekleri.
- `MediaImageScaleMode` ve `MediaImageRoundedCorners` propertyleri.
- Poster çiziminde yuvarlatılmış görsel kırpma ve sınır çizgisi.
- Example Center içine `Media Library / Albüm-Film-Fotoğraf` örneği.

## Kullanım

```csharp
viewgrid.Columns.Add(new ViewGridColumn("Kapak", nameof(Row.Cover))
{
    Kind = ViewGridColumnKind.Image,
    ImageGetter = row => ((Row)row).CoverImage
});

viewgrid.MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
viewgrid.SetViewMode(ViewGridMode.Poster);
// veya
viewgrid.SetViewMode(ViewGridMode.MediaTile);
viewgrid.SetViewMode(ViewGridMode.FilmStrip);
```

## Örnek senaryolar

- Audix: albüm kapağı + şarkı/sanatçı/albüm/süre.
- Film arşivi: poster + başlık/tür/yıl/rating.
- AOI Workspace: hata fotoğrafı veya komponent karesi + ticket bilgisi.
- Factory Navigator: makine fotoğrafı + hat/durum.
- Bilge Defter: öğrenci fotoğrafı + sınıf/durum.
