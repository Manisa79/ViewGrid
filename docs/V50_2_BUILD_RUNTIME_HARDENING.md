# Taylan.Pano v50.2 - Build & Runtime Hardening

Bu sürüm yeni görsel mod eklemek yerine Pano 5.0 özelliklerini projelerde daha güvenli kullanmaya odaklanır.

## Ana hedef

- Derleme/API çakışması risklerini daha erken yakalamak
- Koyu/açık tema okunurluğunu güvenli varsayılanlara almak
- Audix medya görünümü için cache + lazy loading + playback state ayarlarını tek profilde toplamak
- Example Center içinde v50.2 hardening ekranını görünür hale getirmek

## Yeni API

```csharp
pano.ApplyPano502HardeningDefaults();
var checks = pano.RunPano502RuntimeHardeningChecks();
```

Audix için:

```csharp
pano.ApplyAudix502MediaDefaults();
pano.SetViewMode(PanoViewMode.Poster);
```

## Runtime check alanları

- Theme / Accessibility Guard
- Media / Image Cache + Lazy Loading
- Media / Playback State
- Interaction / Search + Command
- Layout / Enterprise Layout

## Example Center

Yeni örnek:

- `PanoV502HardeningSampleForm`
- Example Center hızlı erişim: `Pano v50.2 Hardening`

## Build notu

Bu paket, `tools/pano_api_guard.py` statik kontrol akışını destekler. Gerçek C# derleme için Windows/.NET SDK ortamında `build/build-pano.cmd` veya `build/Build-Pano.ps1` çalıştırılmalıdır.
