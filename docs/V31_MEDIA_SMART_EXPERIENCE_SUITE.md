# ViewGrid v31 - Media + Smart Experience Suite

Bu sürüm v31 fazlarını tek pakette toplar.

## Faz 31 - Media Experience
- Poster, Gallery, MediaTile ve FilmStrip için medya placeholder desteği.
- Kapak üstünde FLAC/MP3/320kbps gibi kalite rozeti.
- Hover/selected durumda play/action overlay düğmesi.
- Audix gibi büyük arşivlerde ImageGetter + disk cache + lazy-load kullanımına uygun property altyapısı.

Yeni property grubu: `ViewGrid - Media Experience`

```csharp
viewgrid.MediaPlaceholderImage = placeholder;
viewgrid.ShowMediaOverlayButton = true;
viewgrid.MediaOverlayButtonText = "▶";
viewgrid.ShowMediaQualityBadge = true;
viewgrid.MediaQualityBadgeAspectName = "Quality";
viewgrid.MediaQualityBadgeGetter = row => ((TrackItem)row).Quality;
```

## Faz 32 - Smart Views
- Layout profile / preset kullanım akışı Example Center içinde daha görünür hale getirildi.
- Kullanıcı görünüm kaydetme, kolon/filtre/sıralama/grup/görünüm modu kombinasyonları için örnek yönlendirmeler eklendi.

## Faz 33 - Advanced Grouping
- GroupCard ve GroupedList kullanım senaryoları V31 Faz Merkezi içinde toplandı.
- Audix için Sanatçı/Albüm, Factory için Hat/Makine tipi, AOI için Durum/Teknisyen gruplama rehberi eklendi.

## Faz 34 - Master Detail
- MasterDetail / DetailCard yönlendirmeleri V31 Faz Merkezi içinde netleştirildi.

## Faz 35 - Kanban Pro
- Kanban görünümü için ticket/durum/akış örnekleri V31 Faz Merkezi içinde hızlı erişime alındı.

## Faz 36 - Designer Friendly
- Yeni medya property'leri designer-friendly kategoriyle eklendi.
- Example Center en üstüne `Hızlı Erişim / Nerede Bulurum?` bölümü eklendi.
- `V31 Faz Merkezi` ekranı eklendi: faz, proje, kullanım ve nerede bulunur bilgileri aranabilir/filtrelenebilir hale geldi.

## Faz 37 - Print / Export
- Card/Poster/Gallery export-print akışları V31 Faz Merkezi içinde PDF/Print yönlendirmesiyle toparlandı.

## Audix hızlı kullanım

```csharp
viewgrid.Columns.Add(new ViewGridColumn("Kapak", nameof(TrackItem.CoverPath), 96)
{
    Kind = ViewGridColumnKind.Image,
    ImageGetter = row => albumCoverCache.GetOrLoad(((TrackItem)row).CoverPath)
});
viewgrid.Columns.Add(new ViewGridColumn("Şarkı", nameof(TrackItem.Title), 220) { FillFreeSpace = true });
viewgrid.Columns.Add(new ViewGridColumn("Sanatçı", nameof(TrackItem.Artist), 160));
viewgrid.Columns.Add(new ViewGridColumn("Albüm", nameof(TrackItem.Album), 180));
viewgrid.Columns.Add(new ViewGridColumn("Kalite", nameof(TrackItem.Quality), 80) { Kind = ViewGridColumnKind.Badge });

viewgrid.TilePosterMode = true;
viewgrid.MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
viewgrid.MediaImageRoundedCorners = true;
viewgrid.MediaPlaceholderImage = CreateDefaultAlbumCover();
viewgrid.ShowMediaOverlayButton = true;
viewgrid.MediaOverlayButtonText = "▶";
viewgrid.MediaQualityBadgeAspectName = nameof(TrackItem.Quality);
viewgrid.SetViewMode(ViewGridMode.Poster);
```
