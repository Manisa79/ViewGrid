# ViewGrid v28.15 Ultimate Platform

Bu sürüm ViewGrid'yi sadece liste/kart kontrolü değil, genişletilebilir bir WinForms veri platformu haline getiren üst seviye altyapıları ekler.

## Eklenenler

- **Query Language**: `status:open AND machine:LINE1`, `qty > 10`, `date >= 2026-01-01`, `name~motor` gibi power-user filtreleri.
- **Expression Engine**: `IF(Status='Error','❌','✔')`, `CONCAT(Line,' / ',Machine)`, `UPPER(Name)`, `LEN(Text)`, sayısal formüller.
- **Action Pipeline**: Row/cell/query/change gibi tetikleyicilere bağlanabilen zincirlenebilir aksiyonlar.
- **Event Bus**: Plugin ve host uygulamalar için `QueryChanged`, `ChangesDetected`, `LayoutPackageApplied`, `ActionsExecuted` gibi olaylar.
- **Change Tracking**: Snapshot alıp sonradan değişen satır/hücreleri bulma ve istenirse flash efektle gösterme.
- **Layout Package Import/Export**: Görünüm modu, satır yüksekliği, kolon genişlikleri, görünürlük, pin durumu, query ve preset bilgilerini taşınabilir paket olarak kaydetme.
- **Smart Suggestions**: Query, komut paleti ve kolon adları için öneri altyapısı.
- **Data Profiling**: Kolon bazlı unique/null/blank/top values/min/max/average/samples analizi.
- **Row Height hardening**: Details/List görünümünde seçim sonrası satır yüksekliğinin kart/poster yüksekliğine sıçramasını engelleyen seçim sonrası stabilizasyon güçlendirildi.

## Örnek Kullanım

```csharp
grid.EnableUltimateDefaults();
grid.Query = "Status:Open AND Priority >= 2";

object? label = grid.EvaluateExpression(row, "IF(Status='Error','Kritik','Normal')");

grid.CaptureChangeSnapshot();
// data değişti...
IReadOnlyList<ViewGridRowChangeSet> changes = grid.DetectChanges();

grid.ExportLayoutPackage("operator.layout", "Operator");
grid.ImportLayoutPackage("operator.layout");

IReadOnlyList<ViewGridDataColumnProfile> profile = grid.GetDataProfile();
```

## Not

Bu katmanlar genel ViewGrid API'sidir; AOI Support Desk'e özel değildir. Stok, üretim, sipariş, dosya, log, destek ticket, dashboard ve yönetim ekranlarında aynı altyapı kullanılabilir.
