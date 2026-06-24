# Taylan.Pano v29 Profile System

v29 ile profil sistemi tek modele indirildi:

- `PanoColumnProfile` kaldırıldı.
- Tek kaynak: `PanoLayoutProfile`.
- Dosya uzantısı: `.panoprofile`.
- `ProfileVersion = 29`.
- Kullanıcı / makine / rol kapsamı desteklenir.
- Eski `.json` kolon layout profilleri `PanoProfileMigrator` ile otomatik dönüştürülebilir.

## Kullanım

```csharp
grid.SaveLayoutProfile("Technician", roleName: "Technician");
grid.LoadLayoutProfile("Technician", roleName: "Technician");
grid.ExportLayoutProfile(@"C:\Temp\Technician.panoprofile", "Technician");
grid.ImportLayoutProfile(@"C:\Temp\Technician.panoprofile", apply: true);
int migrated = grid.MigrateLegacyProfiles();
```

## Geriye uyumluluk

`SaveProfile`, `LoadProfile`, `ResetProfile` artık v29 layout profile API'sine yönlenir.
`PanoColumnProfile` sınıfı bilinçli olarak kaldırıldı; yeni kod `PanoLayoutProfile` kullanmalıdır.

## Build fix

Ultimate paketindeki iki ayrı `PanoCellChange` modeli ayrıldı:

- Undo sistemi: `Taylan.Pano.Undo.PanoCellChange`
- Change tracking: `PanoTrackedCellChange`

Bu sayede `RowObject` / `Column` compile hatası giderildi.
