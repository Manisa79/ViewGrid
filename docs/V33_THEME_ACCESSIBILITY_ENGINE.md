# Taylan.Pano v33 - Theme Accessibility Engine

Bu sürüm v31/v32 görsel fazlarını koruyarak koyu/açık tema okunurluğunu merkezi hale getirir.

## Eklenenler

- `PanoThemeAccessibility` merkezi kontrast yardımcı sınıfı
- `PanoControl.EnforceThemeAccessibility = true` varsayılanı
- Tema uygulanırken panel, kontrol, header, border, muted, empty, accent ve selection renklerinin normalize edilmesi
- Kart/poster/dashboard üst filtre barında TextBox, ComboBox, Button ve aktif filtre bilgi metinlerinin koyu temada okunur hale getirilmesi
- Buton border / hover / pressed renklerinin tema paletine göre ayarlanması
- Example Center içinde `v33 Theme Lab / Okunurluk` senaryosu

## Audix için öneri

Albüm kapağı kullanılan `Poster`, `Gallery`, `MediaTile` ve `FilmStrip` modlarında:

```csharp
pano.EnforceThemeAccessibility = true;
pano.AutoEnsureReadableTextColors = true;
pano.ThemePreset = PanoThemePreset.System;
pano.SetViewMode(PanoViewMode.Poster);
```

Koyu tema form/panel üzerinde Pano otomatik olarak parent renginden okunabilir palet üretir.

## Hedef

Artık Pano içinde doğrudan `Color.White`, `Color.Black`, soluk gri vb. sabit değerlerle düşük kontrast riski azaltılır; merkezi tema motoru host uygulamanın temasına göre yazı ve yüzeyleri güvenli hale getirir.
