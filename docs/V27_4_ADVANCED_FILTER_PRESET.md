# Taylan.Pano v27.4 - Advanced Filter & Preset Engine

Bu sürüm Pano filtre sistemini sadece tek kolon popup filtresi olmaktan çıkarıp kullanıcı tarafında yönetilebilir bir filtre motoruna taşır.

## Eklenenler

- Çok kolonlu gelişmiş filtre oluşturucu
- AND / OR filtre mantığı
- Filtre preset kaydet / yükle
- Header ve gövde sağ tık menüsünden gelişmiş filtre erişimi
- Hazır hızlı filtreler
  - Open Tickets
  - Waiting
  - Eksik BOM
  - SAP Satırları
  - BOM Satırları
- Example Center Pro içinde `Advanced Filter & Preset` senaryosu

## Public API

```csharp
grid.ShowAdvancedFilterBuilder();
grid.SaveCurrentFilterPreset("MasterData Eksik BOM");
grid.LoadFilterPreset("MasterData Eksik BOM");
grid.ApplyBuiltInFilterPreset("Eksik BOM");
grid.AdvancedFilterLogic = PanoFilterLogic.Or;
```

## Önerilen kullanım

MasterData tarafında BOM / SAP / program yolu ekranlarında kullanıcıların sürekli tekrar ettiği filtreler preset olarak saklanabilir.
AOI Support Desk tarafında açık ticket, bekleyen ticket, makine bazlı kayıtlar ve eksik karar gibi listeler hızlı filtre olarak kullanılabilir.
