# Taylan.Pano API Reference

Bu dosya Pano'nin ana ürün API yüzeyini tek yerde toplar.

## Veri Yükleme

```csharp
grid.SetObjects(rows);
await grid.SetObjectsAsync(async ct => await LoadRowsAsync(ct));
```

## Görünüm Modları

```csharp
grid.ViewMode = PanoViewMode.Details;
grid.ViewMode = PanoViewMode.Card;
grid.ViewMode = PanoViewMode.Dashboard;
grid.ViewMode = PanoViewMode.Poster;
```

## Profil Sistemi v29

Tek profil modeli `PanoLayoutProfile` kabul edilir. Eski `PanoColumnProfile` kaldırılmıştır.

```csharp
grid.Profiles.Save("Technician");
grid.Profiles.Load("Technician");
grid.Profiles.Export("Technician.panoprofile");
grid.Profiles.Import("Technician.panoprofile");
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

Her release öncesi `build/Build-Pano.ps1 -Configuration Release -Pack` çalıştırılmalıdır.
