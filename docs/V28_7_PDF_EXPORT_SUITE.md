# Taylan.Pano v28.7 - PDF Export Suite

Bu sürüm Pano export altyapısına gerçek PDF çıktısı ekler.

## Eklenenler

- Details/Table PDF export
- Card/Dashboard PDF export
- Header / footer / sayfa numarası
- Kayıt sayısı ve tarih bilgisi
- Fit-to-page kolon ölçekleme
- Zebra row desteği
- Grid line aç/kapat
- Print-friendly tema
- Card accent bar, status dot ve badge çıktısı
- Card layout definition ile PDF kart alan sıralaması
- Büyük veri için MaxRows limiti
- Dış paket bağımlılığı olmadan PDF üretimi

## Kullanım

```csharp
grid.ExportVisiblePdf(@"C:\Temp\tickets.pdf", new PanoPdfExportOptions
{
    Title = "AOI Support Desk Tickets",
    Mode = PanoPdfExportMode.Table,
    Orientation = PanoPdfPageOrientation.Landscape,
    FitToPageWidth = true,
    ShowGridLines = true,
    ZebraRows = true
});
```

Card/Dashboard çıktısı:

```csharp
grid.ExportVisiblePdf(@"C:\Temp\dashboard.pdf", new PanoPdfExportOptions
{
    Title = "Dashboard",
    Mode = PanoPdfExportMode.Card,
    CardColumns = 2,
    CardMinHeight = 105,
    CardVisualInfoResolver = row => new PanoCardVisualInfo
    {
        AccentColor = Color.DodgerBlue,
        DotColor = Color.DodgerBlue,
        Badges = { new PanoCardBadge { Text = "SAP", BackColor = Color.SeaGreen } }
    }
});
```

Not: PDF içindeki metin Helvetica tabanlıdır; Türkçe karakterler güvenli ASCII karşılıklarına normalize edilir.
