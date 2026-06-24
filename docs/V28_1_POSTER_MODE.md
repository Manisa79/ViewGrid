# Taylan.Pano v28.1 Poster Mode

Bu sürümde `PanoViewMode.Poster` eklendi. Amaç `ExtraLargeIcons` gibi teknik isim yerine büyük görsel kart senaryoları için akılda kalıcı ve designer dostu bir görünüm adı sağlamaktır.

## Eklenenler

- `PanoViewMode.Poster`
- `PosterModeAutoLayout`
- `PosterPreferredWidth`
- `PosterPreferredHeight`
- `PosterImageHeight`
- `ApplyPosterModeDefaults()`
- Example Center Pro içinde `v28.1 Poster Mode` senaryosu

## Davranış

Poster modu mevcut tile/poster çizim altyapısını kullanır. Bu nedenle eski kodlar bozulmadan kalır; yeni projelerde ise `PanoViewMode.Poster` kullanımı daha okunaklıdır.

```csharp
grid.SetViewMode(PanoViewMode.Poster);
grid.PosterPreferredWidth = 220;
grid.PosterPreferredHeight = 300;
grid.PosterImageHeight = 176;
grid.ApplyPosterModeDefaults();
```
