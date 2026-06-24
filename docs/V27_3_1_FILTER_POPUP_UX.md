# Taylan.Pano v27.3.1 Filter Popup UX Fix

Bu ara sürüm uzun filtre değerleri için popup kullanımını iyileştirir.

## Eklenen property'ler

- `FilterPopupResizable`
- `FilterPopupRememberSize`
- `FilterPopupShowValueTooltips`
- `FilterPopupAutoWidthForLongValues`
- `FilterPopupDefaultSize`
- `FilterPopupMinimumSize`
- `FilterPopupMaximumSize`

## Davranış

- Filtre popup penceresi sağ alt köşeden/kenardan büyütülebilir.
- Uzun filtre değerlerinde tooltip gösterilir.
- Uzun SAP malzeme adı, program yolu veya BOM açıklaması gibi değerlerde popup ilk açılışta otomatik genişleyebilir.
- Popup boyutu kolon bazlı hatırlanır.
- Example Center içinde `v27.3.1 Filter Popup UX` örneği eklendi.


## v27.3.1 UltraMax Rebuild
- FilterPopupMaximumSize default: 1800 x 1200
- FilterWindowMaximumSize default: 2000 x 1400
- FilterPopupLimitToWorkingArea: true
- Popup ve ayrı pencere maksimum boyutları ekran çalışma alanına göre güvenli şekilde kırpılır.
