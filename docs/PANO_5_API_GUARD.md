# Taylan.Pano 5 API Guard Notları

Pano fazları hızlı büyüdüğü için yeni partial class dosyaları eklenirken aşağıdaki kurallar uygulanmalı:

1. Aynı `PanoControl` property adı ikinci kez eklenmemeli.
2. Yeni property eklenmeden önce `src/Taylan.Pano/Core/PanoControl*.cs` içinde isim aranmalı.
3. Example Center kodu çekirdekte olmayan enum/property kullanmamalı.
4. Yeni medya/tema örnekleri önce `ApplyPano5FoundationDefaults()` ile güvenli varsayılanları açmalı.
5. Eski API korunacaksa yeni property yerine wrapper metod tercih edilmeli.

Örnek kontrol komutları:

```bash
grep -R "public .* EnableCommandPalette" src/Taylan.Pano/Core/PanoControl*.cs
grep -R "PanoColumnKind" src/Taylan.Pano samples
```

Build ortamı yoksa en azından namespace, enum ve property isimleri bu şekilde taranmalıdır.
