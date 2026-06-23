# ViewGrid v28.12.1 - Float Safe Dialog/Icon Fix

Bu bakım paketi v28.11 ve v28.12 değişikliklerini çalıştırmayı engelleyen derleme hatasını düzeltir.

## Düzeltmeler

- `ViewGridDialogIconFactory.Blend(...)` metodu `float` yerine `double` oran kabul edecek şekilde güncellendi.
- Koyu/açık tema uyumlu ViewGrid dialog başlık ikonlarında `double -> float` dönüşüm hatası giderildi.
- v28.11 dialog title/theme/icon altyapısı ve v28.12 filtre popup resize/visual sync değişiklikleri birlikte korunur.

## Not

Visual Studio tarafında eski kırık build çıktısı kalmışsa `bin` ve `obj` klasörlerini silip temiz build alın.
