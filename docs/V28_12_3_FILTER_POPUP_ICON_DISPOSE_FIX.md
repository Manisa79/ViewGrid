# ViewGrid v28.12.3 - Filter Popup Icon/Dispose + Visual Unification Fix

Bu bakım paketi filtre popup tarafındaki iki önemli problemi düzeltir.

## Düzeltmeler

- Filtre ikonundan ve menüden açılan popup aynı floating/resizable render yolunu kullanmaya devam eder.
- Floating popup üst komut satırları menü popup görünümüne daha yakın olacak şekilde ikon + metin düzenine taşındı.
- Komut ikonları tema accent rengine göre çizilir.
- Hover/aktif sıralama vurgusu iki açılış yolunda da tutarlı hale getirildi.
- Sağ-alt resize grip daha temiz çizilir ve iki popup yolunda da görünür kalır.
- `Cannot access a disposed object. Object name: Icon` hatasına sebep olan eski Form.Icon dispose davranışı kaldırıldı. WinForms default/shared icon instance dispose edilmiyor.

## Not

Popup pencereleri kozmetik icon üretim hatalarında artık ViewGrid kullanımını engellemez.
