# ViewGrid v51 - Audix Pilot / Theme Audit / Example Cleanup

Bu sürümde yeni bir görünüm modu eklemek yerine ViewGrid'nin mevcut medya, tema ve örnek merkezi yetenekleri gerçek kullanım senaryosuna yaklaştırıldı.

## Hedef

- Audix içinde albüm kapağı, play/pause state, video preview ve medya rozetlerini tak-çalıştır kullanmak.
- Koyu/açık/yüksek kontrast temalarda buton, label, combo, badge, kart ve medya overlay okunurluğunu korumak.
- Example Center kalabalığında özellikleri kategori + arama + hızlı erişim ile bulmayı kolaylaştırmak.
- v50.2 hardening ayarlarını koruyarak ViewGrid 5.x API kırılma riskini azaltmak.

## Yeni yardımcılar

```csharp
viewgrid.ApplyViewGrid51RealUsageDefaults();
viewgrid.ApplyAudix51MediaPilotDefaults();
viewgrid.ApplyTheme51AuditDefaults(ViewGridThemeStudioPreset.AudixDark);
var checks = viewgrid.RunViewGrid51UsageChecks();
```

## Audix için temel kullanım

```csharp
viewgrid.ApplyAudix51MediaPilotDefaults();
viewgrid.MediaQualityBadgeGetter = row => ((TrackRow)row).Quality;
viewgrid.MediaKindGetter = row => ((TrackRow)row).IsVideo ? ViewGridMediaKind.Video : ViewGridMediaKind.Audio;
viewgrid.MediaPlaybackStateGetter = row => ((TrackRow)row).PlaybackState;
viewgrid.MediaPlayPauseClicked += (_, e) => TogglePlayback((TrackRow)e.RowObject);
viewgrid.SetViewMode(ViewGridMode.Poster);
```

Önerilen Audix görünümleri:

- `Poster`: büyük albüm kapağı.
- `MediaTile`: Spotify tarzı kompakt kart.
- `Gallery`: kapak galerisi.
- `FilmStrip`: yatay video/kapak şeridi.

## Yeni örnek

Example Center içinde:

- `ViewGrid 5.1 Audix Pilot`
- `ViewGridV51RealUsagePilotSampleForm`

Bu ekranda ses ve video kayıtları, eksik kapak placeholder'ı, FLAC/MP3/1080p/4K rozetleri, now-playing ve equalizer göstergesi birlikte gösterilir.
