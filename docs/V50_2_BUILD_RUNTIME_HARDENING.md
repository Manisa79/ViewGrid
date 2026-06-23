# ViewGrid v50.2 - Build & Runtime Hardening

Bu sürüm yeni görsel mod eklemek yerine ViewGrid 5.0 özelliklerini projelerde daha güvenli kullanmaya odaklanır.

## Ana hedef

- Derleme/API çakışması risklerini daha erken yakalamak
- Koyu/açık tema okunurluğunu güvenli varsayılanlara almak
- Audix medya görünümü için cache + lazy loading + playback state ayarlarını tek profilde toplamak
- Example Center içinde v50.2 hardening ekranını görünür hale getirmek

## Yeni API

```csharp
viewgrid.ApplyViewGrid502HardeningDefaults();
var checks = viewgrid.RunViewGrid502RuntimeHardeningChecks();
```

Audix için:

```csharp
viewgrid.ApplyAudix502MediaDefaults();
viewgrid.SetViewMode(ViewGridMode.Poster);
```

## Runtime check alanları

- Theme / Accessibility Guard
- Media / Image Cache + Lazy Loading
- Media / Playback State
- Interaction / Search + Command
- Layout / Enterprise Layout

## Example Center

Yeni örnek:

- `ViewGridV502HardeningSampleForm`
- Example Center hızlı erişim: `ViewGrid v50.2 Hardening`

## Build notu

Bu paket, `tools/viewgrid_api_guard.py` statik kontrol akışını destekler. Gerçek C# derleme için Windows/.NET SDK ortamında `build/build-viewgrid.cmd` veya `build/Build-ViewGrid.ps1` çalıştırılmalıdır.
