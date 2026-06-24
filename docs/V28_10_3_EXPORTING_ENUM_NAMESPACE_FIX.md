# Taylan.Pano v28.10.3 - Exporting Enum Namespace Fix

Bu paket v28.10.2 sonrasında PDF export örneğinde görülen namespace gölgeleme hatasını düzeltir.

## Düzeltilen hata

TestApp içinde bazı formlarda `Pano` isimli instance alanı bulunduğu için şu kullanım yanlış çözülüyordu:

```csharp
Taylan.Pano.Exporting.PanoPdfExportMode.Card
```

Derleyici bunu namespace olarak değil, `PanoControl.Exporting` facade property’si olarak yorumlayabiliyordu.

## Düzeltme

PDF örnekleri artık global namespace ile çağrılır:

```csharp
new global::Taylan.Pano.Exporting.PanoPdfExportOptions
{
    Mode = global::Taylan.Pano.Exporting.PanoPdfExportMode.Card,
    Orientation = global::Taylan.Pano.Exporting.PanoPdfPageOrientation.Portrait
};
```

## Desteklenen kullanım

```csharp
grid.ExportVisiblePdf(path, options);
grid.Exporting.ExportVisiblePdf(path, options);
```

`PanoPdfExportMode`, `PanoPdfPageOrientation` ve `PanoPdfExportOptions` tipleri `Taylan.Pano.Exporting` namespace’i altındadır.
