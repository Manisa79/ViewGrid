# ViewGrid API Reference

Bu dosya ViewGrid'nin ana ürün API yüzeyini tek yerde toplar.

## Veri Yükleme

```csharp
grid.SetObjects(rows);
await grid.SetObjectsAsync(async ct => await LoadRowsAsync(ct));
```

## Görünüm Modları

```csharp
grid.ViewMode = ViewGridMode.Details;
grid.ViewMode = ViewGridMode.Card;
grid.ViewMode = ViewGridMode.Dashboard;
grid.ViewMode = ViewGridMode.Poster;
```

## Profil Sistemi v29

Tek profil modeli `ViewGridLayoutProfile` kabul edilir. Eski `ViewGridColumnProfile` kaldırılmıştır.

```csharp
grid.Profiles.Save("Technician");
grid.Profiles.Load("Technician");
grid.Profiles.Export("Technician.viewgridprofile");
grid.Profiles.Import("Technician.viewgridprofile");
grid.Profiles.MigrateLegacyProfiles();
```

## Filtreleme

- Header popup filtre
- Card/Dashboard kolon seçerek filtreleme
- Advanced filter builder
- Preset filtreler
- Quick filter bar

## Export

```csharp
grid.ExportVisiblePdf(path, options);
grid.Exporting.ExportVisiblePdf(path, options);
```

## Ürünleşme Notu

Her release öncesi `build/Build-ViewGrid.ps1 -Configuration Release -Pack` çalıştırılmalıdır.
