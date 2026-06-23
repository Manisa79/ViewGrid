# ViewGrid v29 Profile System

v29 ile profil sistemi tek modele indirildi:

- `ViewGridColumnProfile` kaldırıldı.
- Tek kaynak: `ViewGridLayoutProfile`.
- Dosya uzantısı: `.viewgridprofile`.
- `ProfileVersion = 29`.
- Kullanıcı / makine / rol kapsamı desteklenir.
- Eski `.json` kolon layout profilleri `ViewGridProfileMigrator` ile otomatik dönüştürülebilir.

## Kullanım

```csharp
grid.SaveLayoutProfile("Technician", roleName: "Technician");
grid.LoadLayoutProfile("Technician", roleName: "Technician");
grid.ExportLayoutProfile(@"C:\Temp\Technician.viewgridprofile", "Technician");
grid.ImportLayoutProfile(@"C:\Temp\Technician.viewgridprofile", apply: true);
int migrated = grid.MigrateLegacyProfiles();
```

## Geriye uyumluluk

`SaveProfile`, `LoadProfile`, `ResetProfile` artık v29 layout profile API'sine yönlenir.
`ViewGridColumnProfile` sınıfı bilinçli olarak kaldırıldı; yeni kod `ViewGridLayoutProfile` kullanmalıdır.

## Build fix

Ultimate paketindeki iki ayrı `ViewGridCellChange` modeli ayrıldı:

- Undo sistemi: `ViewGrid.Undo.ViewGridCellChange`
- Change tracking: `ViewGridTrackedCellChange`

Bu sayede `RowObject` / `Column` compile hatası giderildi.
