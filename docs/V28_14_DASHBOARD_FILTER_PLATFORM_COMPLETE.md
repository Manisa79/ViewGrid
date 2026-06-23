# ViewGrid v28.14 - Dashboard Filter + Platform Completion

Bu sürüm v28.13 platform altyapısını daha çalışır hale getiren polish/fix paketidir.

## Düzeltmeler
- Details/List görünümünde satır seçilince kart/poster yüksekliğinin taşması engellendi.
- Poster/CardView/RowCard/Details geçişlerinde eski kart yüksekliğinin yeni moda sızması düzeltildi.
- Quick Filter Bar artık global düz metin yerine gerçek kolon filtresi uygular.
- C# derleyicilerinde `Geçersiz ifade terimi ','` gibi hatalara yol açabilen modern/target-typed bazı v28.13 kodları daha güvenli klasik söz dizimine çekildi.

## Yeni Card/Dashboard kolon filtre altyapısı
CardView/Dashboard modlarında header olmadığı için `Kolon filtre` butonuna basıldığında hangi kolonun filtreleneceği artık açıkça seçilebilir.

Yeni API:

```csharp
grid.ShowDashboardColumnFilter(btnKolonFiltre);
grid.ShowDashboardColumnFilterAtScreen(Control.MousePosition);
grid.ShowDashboardColumnFilterForTypedText(txtSearch.Text, btnKolonFiltre);

grid.DashboardFilterOptions.ColumnSelectionMode = ViewGridCardFilterColumnSelectionMode.AskUser;
grid.DashboardFilterOptions.ShowColumnChooserBeforeFilter = true;
grid.DashboardFilterOptions.RememberLastColumn = true;
```

Modlar:
- `AskUser`: kullanıcı kolon seçer.
- `FirstVisible`: ilk görünür/filtrelenebilir kolon.
- `LastUsed`: son seçilen kolon.
- `AutoBestMatch`: yazılan değere göre en uygun kolonu tahmin eder.

Öneri: AOI Support Desk gibi dashboardlarda default `AskUser`, hızlı arama kutusundan filtre açılıyorsa `AutoBestMatch` uygundur.
