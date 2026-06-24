# Taylan.Pano v31 - Media + Smart Experience Suite

Bu sürüm v31 fazlarını tek pakette toplar.

## Faz 31 - Media Experience
- Poster, Gallery, MediaTile ve FilmStrip için medya placeholder desteği.
- Kapak üstünde FLAC/MP3/320kbps gibi kalite rozeti.
- Hover/selected durumda play/action overlay düğmesi.
- Audix gibi büyük arşivlerde ImageGetter + disk cache + lazy-load kullanımına uygun property altyapısı.

Yeni property grubu: `Pano - Media Experience`

```csharp
pano.MediaPlaceholderImage = placeholder;
pano.ShowMediaOverlayButton = true;
pano.MediaOverlayButtonText = "▶";
pano.ShowMediaQualityBadge = true;
pano.MediaQualityBadgeAspectName = "Quality";
pano.MediaQualityBadgeGetter = row => ((TrackItem)row).Quality;
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
pano.Columns.Add(new PanoColumn("Kapak", nameof(TrackItem.CoverPath), 96)
{
    Kind = PanoColumnKind.Image,
    ImageGetter = row => albumCoverCache.GetOrLoad(((TrackItem)row).CoverPath)
});
pano.Columns.Add(new PanoColumn("Şarkı", nameof(TrackItem.Title), 220) { FillFreeSpace = true });
pano.Columns.Add(new PanoColumn("Sanatçı", nameof(TrackItem.Artist), 160));
pano.Columns.Add(new PanoColumn("Albüm", nameof(TrackItem.Album), 180));
pano.Columns.Add(new PanoColumn("Kalite", nameof(TrackItem.Quality), 80) { Kind = PanoColumnKind.Badge });

pano.TilePosterMode = true;
pano.MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
pano.MediaImageRoundedCorners = true;
pano.MediaPlaceholderImage = CreateDefaultAlbumCover();
pano.ShowMediaOverlayButton = true;
pano.MediaOverlayButtonText = "▶";
pano.MediaQualityBadgeAspectName = nameof(TrackItem.Quality);
pano.SetViewMode(PanoViewMode.Poster);
```
