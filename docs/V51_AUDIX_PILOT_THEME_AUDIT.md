# Taylan.Pano v51 - Audix Pilot / Theme Audit / Example Cleanup

Bu sürümde yeni bir görünüm modu eklemek yerine Pano'nin mevcut medya, tema ve örnek merkezi yetenekleri gerçek kullanım senaryosuna yaklaştırıldı.

## Hedef

- Audix içinde albüm kapağı, play/pause state, video preview ve medya rozetlerini tak-çalıştır kullanmak.
- Koyu/açık/yüksek kontrast temalarda buton, label, combo, badge, kart ve medya overlay okunurluğunu korumak.
- Example Center kalabalığında özellikleri kategori + arama + hızlı erişim ile bulmayı kolaylaştırmak.
- v50.2 hardening ayarlarını koruyarak Pano 5.x API kırılma riskini azaltmak.

## Yeni yardımcılar

```csharp
pano.ApplyPano51RealUsageDefaults();
pano.ApplyAudix51MediaPilotDefaults();
pano.ApplyTheme51AuditDefaults(PanoThemeStudioPreset.AudixDark);
var checks = pano.RunPano51UsageChecks();
```

## Audix için temel kullanım

```csharp
pano.ApplyAudix51MediaPilotDefaults();
pano.MediaQualityBadgeGetter = row => ((TrackRow)row).Quality;
pano.MediaKindGetter = row => ((TrackRow)row).IsVideo ? PanoMediaKind.Video : PanoMediaKind.Audio;
pano.MediaPlaybackStateGetter = row => ((TrackRow)row).PlaybackState;
pano.MediaPlayPauseClicked += (_, e) => TogglePlayback((TrackRow)e.RowObject);
pano.SetViewMode(PanoViewMode.Poster);
```

Önerilen Audix görünümleri:

- `Poster`: büyük albüm kapağı.
- `MediaTile`: Spotify tarzı kompakt kart.
- `Gallery`: kapak galerisi.
- `FilmStrip`: yatay video/kapak şeridi.

## Yeni örnek

Example Center içinde:

- `Pano 5.1 Audix Pilot`
- `PanoV51RealUsagePilotSampleForm`

Bu ekranda ses ve video kayıtları, eksik kapak placeholder'ı, FLAC/MP3/1080p/4K rozetleri, now-playing ve equalizer göstergesi birlikte gösterilir.
