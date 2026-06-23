# ViewGrid v28.10.1 - Build Fixes / Sample Visibility

Bu paket v28.8, v28.9 ve v28.10 zincirindeki build engelini düzeltir.

## Düzeltmeler

- `_activeCellPaintViewIndex` alanı ViewGridControl içine eklendi.
- Hücre içi scroll çiziminde kullanılan paint row index artık sınıf alanı üzerinden güvenli çalışır.
- `obj/.../ref/ViewGrid.dll bulunamadı` hatasına sebep olan kaynak compile hatası giderildi.
- v28.7 PDF export örneği TestApp içine görünür form olarak eklendi.
- v28.8 Hücre içi scroll örneği TestApp içine görünür form olarak eklendi.
- Örnek merkezi ve üst toolbar menülerine PDF/Cell Overflow örnekleri bağlandı.

## Not

Visual Studio tarafında önce `bin` ve `obj` klasörlerini temizleyip yeniden build almak iyi olur.

## v28.10.2 export facade hotfix

Some sample/project code used the fluent `grid.Exporting.ExportVisiblePdf(...)` style, while the core API exposed direct methods such as `grid.ExportVisiblePdf(...)`. ViewGridControl now includes a non-serialized `Exporting` facade so both styles compile:

```csharp
grid.ExportVisiblePdf(path, options);
grid.Exporting.ExportVisiblePdf(path, options);
```
