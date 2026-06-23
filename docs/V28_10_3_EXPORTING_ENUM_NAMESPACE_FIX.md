# ViewGrid v28.10.3 - Exporting Enum Namespace Fix

Bu paket v28.10.2 sonrasında PDF export örneğinde görülen namespace gölgeleme hatasını düzeltir.

## Düzeltilen hata

TestApp içinde bazı formlarda `ViewGrid` isimli instance alanı bulunduğu için şu kullanım yanlış çözülüyordu:

```csharp
ViewGrid.Exporting.ViewGridPdfExportMode.Card
```

Derleyici bunu namespace olarak değil, `ViewGridControl.Exporting` facade property’si olarak yorumlayabiliyordu.

## Düzeltme

PDF örnekleri artık global namespace ile çağrılır:

```csharp
new global::ViewGrid.Exporting.ViewGridPdfExportOptions
{
    Mode = global::ViewGrid.Exporting.ViewGridPdfExportMode.Card,
    Orientation = global::ViewGrid.Exporting.ViewGridPdfPageOrientation.Portrait
};
```

## Desteklenen kullanım

```csharp
grid.ExportVisiblePdf(path, options);
grid.Exporting.ExportVisiblePdf(path, options);
```

`ViewGridPdfExportMode`, `ViewGridPdfPageOrientation` ve `ViewGridPdfExportOptions` tipleri `ViewGrid.Exporting` namespace’i altındadır.
