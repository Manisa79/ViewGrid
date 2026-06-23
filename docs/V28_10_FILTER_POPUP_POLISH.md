# ViewGrid v28.10 - Filter Popup Resize, Icons and Active Command Polish

Bu sürüm filtre popup menüsünü daha kullanışlı ve görsel olarak daha okunur hale getirir.

## Eklenenler

- Floating filtre popup için sağ/alt kenardan yeniden boyutlandırma desteği.
- Mevcut sağ alt resize grip daha görünür hale getirildi.
- Popup boyutu kolon bazlı hatırlanmaya devam eder.
- Filtre popup komutlarına ikon eklendi:
  - ↑ Artan sırala
  - ↓ Azalan sırala
  - ↕ Sıralamayı kaldır
  - □ Ayrı filtre penceresi
  - ✕ Tüm filtreleri temizle
- Aktif sıralama komutu accent renkli vurgu ile gösterilir.
- Aktif filtre varsa temizleme satırı daha belirgin görünür.
- Komut satırlarına hover vurgusu eklendi.
- Ayrı filtre penceresindeki sol komut butonlarına da ikon + hover polish eklendi.

## Yeni özellikler

```csharp
grid.FilterPopupShowActionIcons = true;
grid.FilterPopupHighlightActiveCommands = true;
grid.FilterPopupEdgeResize = true;
grid.FilterPopupResizable = true;
grid.FilterPopupRememberSize = true;
```

## Not

Bu özellikler sadece AOI Support Desk için değil, ViewGrid kullanan tüm projelerde geçerli genel filtre popup deneyimi olarak tasarlandı.
