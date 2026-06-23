# ViewGrid v28.12 - Filter Popup Resize + Visual Sync

Bu sürüm filtre ikonundan açılan floating popup ile menüden açılan filtre popup/pencere görünümünü aynı görsel çizgiye yaklaştırır.

## Eklenen / Düzeltilenler

- Filtre ikonundan açılan popup kullanıcı tarafından sağ/alt kenar ve sağ-alt grip üzerinden büyütülüp küçültülebilir.
- Popup boyutu kolon bazlı hatırlanır.
- Sağ-alt resize grip artık bozuk/ok karakterli görünmez; temiz diagonal çizgilerle çizilir.
- Popup arka planı, border ve bağlantı oku tema ile uyumlu çizilir.
- Popup boyutlandırılırken iç liste, arama kutusu, butonlar ve alt komutlar otomatik yeniden yerleşir.
- Menüden açılan popup ile filtre ikonundan açılan popup aynı ikonlu komut ve aktif sıralama vurgusu dilini kullanır.
- Kenardan resize yakalama alanı panel ve iç kontroller üzerinde de çalışacak şekilde güçlendirildi.

## İlgili Ayarlar

```csharp
grid.FilterPopupResizable = true;
grid.FilterPopupEdgeResize = true;
grid.FilterPopupRememberSize = true;
grid.FilterPopupShowActionIcons = true;
grid.FilterPopupHighlightActiveCommands = true;
```
