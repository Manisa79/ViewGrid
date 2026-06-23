# ViewGrid v28.1 Poster Mode

Bu sürümde `ViewGridMode.Poster` eklendi. Amaç `ExtraLargeIcons` gibi teknik isim yerine büyük görsel kart senaryoları için akılda kalıcı ve designer dostu bir görünüm adı sağlamaktır.

## Eklenenler

- `ViewGridMode.Poster`
- `PosterModeAutoLayout`
- `PosterPreferredWidth`
- `PosterPreferredHeight`
- `PosterImageHeight`
- `ApplyPosterModeDefaults()`
- Example Center Pro içinde `v28.1 Poster Mode` senaryosu

## Davranış

Poster modu mevcut tile/poster çizim altyapısını kullanır. Bu nedenle eski kodlar bozulmadan kalır; yeni projelerde ise `ViewGridMode.Poster` kullanımı daha okunaklıdır.

```csharp
grid.SetViewMode(ViewGridMode.Poster);
grid.PosterPreferredWidth = 220;
grid.PosterPreferredHeight = 300;
grid.PosterImageHeight = 176;
grid.ApplyPosterModeDefaults();
```
