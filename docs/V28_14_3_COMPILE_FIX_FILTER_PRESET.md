# Taylan.Pano v28.14.3 - Filter Preset Compile Fix

## Düzeltilen hata

- `PanoControl` içinde aynı imzaya sahip iki ayrı `LoadFilterPreset(string)` metodu bulunuyordu.
- v27.4 dosya tabanlı preset sistemi ile v28.13 platform preset sistemi tek public API altında birleştirildi.

## Yeni davranış

`grid.LoadFilterPreset("PresetName")` çağrısı artık önce v28.13 platform/bellek presetlerine bakar, bulamazsa v27.4 dosya tabanlı preset klasöründen yükler.

Bu sayede mevcut kullanım bozulmadan duplicate member compile hatası giderildi.
