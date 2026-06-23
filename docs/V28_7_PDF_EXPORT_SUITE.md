# ViewGrid v28.7 - PDF Export Suite

Bu sürüm ViewGrid export altyapısına gerçek PDF çıktısı ekler.

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
grid.ExportVisiblePdf(@"C:\Temp\tickets.pdf", new ViewGridPdfExportOptions
{
    Title = "AOI Support Desk Tickets",
    Mode = ViewGridPdfExportMode.Table,
    Orientation = ViewGridPdfPageOrientation.Landscape,
    FitToPageWidth = true,
    ShowGridLines = true,
    ZebraRows = true
});
```

Card/Dashboard çıktısı:

```csharp
grid.ExportVisiblePdf(@"C:\Temp\dashboard.pdf", new ViewGridPdfExportOptions
{
    Title = "Dashboard",
    Mode = ViewGridPdfExportMode.Card,
    CardColumns = 2,
    CardMinHeight = 105,
    CardVisualInfoResolver = row => new ViewGridCardVisualInfo
    {
        AccentColor = Color.DodgerBlue,
        DotColor = Color.DodgerBlue,
        Badges = { new ViewGridCardBadge { Text = "SAP", BackColor = Color.SeaGreen } }
    }
});
```

Not: PDF içindeki metin Helvetica tabanlıdır; Türkçe karakterler güvenli ASCII karşılıklarına normalize edilir.
