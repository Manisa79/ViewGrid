# Taylan.Pano v28.12.2 - Unified Filter Popup Anchor & Visual Sync

Bu sürüm filtre popup davranışını toparlar.

## Düzeltmeler

- Filtre ikonundan açılan popup ile sağ tık menüsü / alt menü içinden açılan popup aynı floating/resizable altyapıyı kullanır.
- CardView / Tile / Dashboard gibi başlıksız görünümlerde popup artık kolon header koordinatına göre değil, mouse/buton yakınına göre açılır.
- Popup ekran dışına taşıyorsa çalışma alanı içine otomatik alınır.
- Sağ-alt resize grip görünümü temizlendi; çift çizim ve bozuk köşe görüntüsü kaldırıldı.
- Popup boyutu yine kolon bazlı hatırlanır.

## Yeni API

```csharp
grid.ShowFilterMenuForAspect("LastMessage");
grid.ShowFilterMenuForAspect("LastMessage", buttonFilter);
grid.ShowFilterMenuForAspect("LastMessage", new Point(x, y));
grid.ShowFilterMenuForAspectAtScreen("LastMessage", screenPoint);
```

## Yeni ayarlar

```csharp
grid.UseUnifiedFloatingFilterPopup = true;
grid.FilterPopupAnchorToMouseWhenHeaderUnavailable = true;
```
