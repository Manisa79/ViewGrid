# ViewGrid v33 - Theme Accessibility Engine

Bu sürüm v31/v32 görsel fazlarını koruyarak koyu/açık tema okunurluğunu merkezi hale getirir.

## Eklenenler

- `ViewGridThemeAccessibility` merkezi kontrast yardımcı sınıfı
- `ViewGridControl.EnforceThemeAccessibility = true` varsayılanı
- Tema uygulanırken panel, kontrol, header, border, muted, empty, accent ve selection renklerinin normalize edilmesi
- Kart/poster/dashboard üst filtre barında TextBox, ComboBox, Button ve aktif filtre bilgi metinlerinin koyu temada okunur hale getirilmesi
- Buton border / hover / pressed renklerinin tema paletine göre ayarlanması
- Example Center içinde `v33 Theme Lab / Okunurluk` senaryosu

## Audix için öneri

Albüm kapağı kullanılan `Poster`, `Gallery`, `MediaTile` ve `FilmStrip` modlarında:

```csharp
viewgrid.EnforceThemeAccessibility = true;
viewgrid.AutoEnsureReadableTextColors = true;
viewgrid.ThemePreset = ViewGridThemePreset.System;
viewgrid.SetViewMode(ViewGridMode.Poster);
```

Koyu tema form/panel üzerinde ViewGrid otomatik olarak parent renginden okunabilir palet üretir.

## Hedef

Artık ViewGrid içinde doğrudan `Color.White`, `Color.Black`, soluk gri vb. sabit değerlerle düşük kontrast riski azaltılır; merkezi tema motoru host uygulamanın temasına göre yazı ve yüzeyleri güvenli hale getirir.
