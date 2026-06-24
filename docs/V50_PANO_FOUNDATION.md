# Taylan.Pano 5.0 Foundation / Stability

Bu paket yeni görünüm eklemekten çok Pano'nin büyüyen mimarisini sağlamlaştırmak için hazırlandı.

## Amaç

Pano artık sadece ListView/Grid alternatifi değil; medya, dashboard, timeline, kanban, tema, command palette, layout ve analytics özellikleri olan büyük bir görsel veri platformu haline geldi. Bu yüzden v5.0 Foundation paketi şu hedefleri toplar:

- Fazlar arası API/property çakışmalarını azaltmak.
- Audix, AOI Support Desk, FactoryOS, MasterData ve Bilge Defter için profil bazlı kullanım sağlamak.
- Koyu/açık tema okunurluğunu varsayılan olarak korumak.
- Audio/video playback state görünürlüğünü güvenli varsayılan haline getirmek.
- Example Center içinde yeni özellikleri daha kolay buldurmak.

## Yeni ana API'ler

```csharp
pano.ApplyPano5FoundationDefaults();
pano.ApplyAudixMediaProfile();
pano.ApplyAoiSupportDeskProfile();
pano.ApplyFactoryIntelligenceProfile();
```

## Modül profilleri

```csharp
var profiles = pano.GetPano5ModuleProfiles();
```

Profiller:

- Audix Media
- AOI Support Desk
- Factory Intelligence
- MasterData
- Bilge Defter

## Runtime stability check

```csharp
var checks = pano.RunPano5RuntimeChecks();
```

Kontrol edilenler:

- Theme Accessibility
- Media Playback state
- Command Palette
- Aktif modül profili

## Audix için önerilen kullanım

```csharp
pano.ApplyAudixMediaProfile();
pano.SetViewMode(PanoViewMode.Poster);
pano.MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
pano.ShowMediaPlaybackState = true;
pano.ShowMediaNowPlayingBadge = true;
pano.ShowMediaEqualizerIndicator = true;
```

Video dosyalarında `MediaVideoPreviewMode = true` bırakılır. Gerçek video player/preview açma işi host uygulamada `MediaPlayPauseClicked` event'i içinde yapılmalıdır.

## Example Center

Yeni örnek:

- `Pano 5.0 Foundation / Stability`

Bu ekranda profil butonları, runtime check ve koyu/açık tema hızlı testi bulunur.
