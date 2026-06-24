# Taylan.Pano v28.4 - Filter Popup State & Default Popup Experience

Bu sürüm filtre kullanımını Excel benzeri daha hızlı ve tutarlı hale getirir.

## Varsayılan filtre davranışı

- Header filtre butonuna tıklandığında artık varsayılan olarak popup filtre açılır.
- Ayrı pencere istenirse `FilterMenuMode = PanoFilterMenuMode.ModalWindow` seçilebilir.
- İki seçenek birlikte sunulmak istenirse `FilterMenuMode = PanoFilterMenuMode.Both` kullanılabilir.

## Tümünü seç senkronizasyonu

Popup filtre ve ayrı filtre penceresinde listedeki değerler tek tek işaretlenip kaldırıldığında `Tümünü seç` kendini otomatik günceller:

- Tüm değerler seçiliyse: Checked
- Hiç değer seçili değilse: Unchecked
- Bazı değerler seçiliyse: Indeterminate

Bu davranış CardView, Dashboard, Details veya başka görünüm fark etmeksizin aynı filtre altyapısından çalışır.
