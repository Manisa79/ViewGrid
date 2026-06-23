# ViewGrid v34 + v35 + v36 Faz Paketi

Bu paket v33 Theme Accessibility Engine üzerine üç fazı birlikte ekler.

## Faz 34 — Stability & Build Quality

Amaç: ViewGrid çekirdeğinde yeni fazlar büyüdükçe API çakışmalarını, riskli ayarları ve örnek merkezi karmaşasını daha kolay kontrol etmek.

Eklenenler:

- `EnableBuildQualityDiagnostics`
- `EnableCompatibilityGuards`
- `AutoRepairRiskyOptions`
- `ApplyV34BuildQualityPack()`
- `RunBuildQualityDiagnostics()`
- `RunBuildQualityDiagnosticsText()`
- `ViewGridQualityReport`
- `ViewGridQualityCheckItem`

Kontrol ettiği alanlar:

- Public API property çakışmaları
- Kolon tanımı / duplicate AspectName
- Medya görünümünde görsel kolonu/placeholder durumu
- Tema kontrast oranı
- Media lazy-load/cache uyumu
- Popup ve poster ölçülerinde riskli küçük değerler

## Faz 35 — Theme Studio

Amaç: Audix, AOI Support Desk, FactoryOS, SmokeWhite ve yüksek kontrast gibi proje bazlı tema presetlerini tek merkezde yönetmek.

Eklenenler:

- `EnableThemeStudio`
- `ThemeStudioPreset`
- `ThemeStudioEnforceAccessibility`
- `ApplyThemeStudioPreset(...)`
- `ApplyV35ThemeStudioPack(...)`
- `GetThemeStudioPalettes()`
- `ExportCurrentThemeStudioPalette(...)`
- `ViewGridThemeStudio`
- `ViewGridThemeStudioPreset`

Hazır presetler:

- `AudixDark`, `AudixLight`
- `AoiSupportDark`, `AoiSupportLight`
- `FactoryOsDark`, `FactoryOsLight`
- `SmokeWhite`
- `HighContrastDark`, `HighContrastLight`
- `MidnightGlass`

## Faz 36 — Media Pro

Amaç: Audix ve medya/katalog ekranları için albüm kapağı deneyimini profesyonel hale getirmek.

Eklenenler:

- `EnableMediaPro`
- `MediaMemoryCacheLimit`
- `MediaDiskCacheFolder`
- `MissingCoverBehavior`
- `MediaGroupAspectName`
- `ShowMediaSelectedGlow`
- `ShowMissingCoverBadge`
- `MediaImagePathGetter`
- `ApplyV36MediaProPack()`
- `ResolveMediaImagePro(...)`
- `LoadMediaImageFromCache(...)`
- `ClearMediaImageCache()`
- `CreateMediaPlaceholderBitmap(...)`

Audix önerilen kullanım:

```csharp
viewgrid.ApplyV36MediaProPack();
viewgrid.SetViewMode(ViewGridMode.Poster);
viewgrid.MediaQualityBadgeAspectName = "Format";
viewgrid.MediaImagePathGetter = row => ((TrackItem)row).CoverPath;

var coverColumn = new ViewGridColumn("Kapak", "Title");
coverColumn.ImageGetter = row => viewgrid.ResolveMediaImagePro(row);
viewgrid.Columns.Add(coverColumn);
```

## Example Center

Example Center Pro içine şu yeni başlıklar eklendi:

- `27 v34 Build Quality / Stability`
- `28 v35 Theme Studio`
- `29 v36 Media Pro`

Ayrıca ana örnek merkezi üstündeki hızlı erişime `V34-36 Faz Merkezi` kısayolu eklendi.
