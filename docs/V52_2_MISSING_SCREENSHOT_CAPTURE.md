# Taylan.Pano v1.0.52.2 - Missing Screenshot Capture Pack

Bu paket, `Pano_Professional_Developer_User_Guide_v1.0.52.2` dokümanında `70. Görsel Durumu ve Eksik Takip` altında kalan bağımsız görselleri üretmek için TestApp içindeki Documentation Capture Mode'u genişletir.

## TestApp değişiklikleri

- `DocumentationMissingScreenshotsForm.cs` eklendi.
- `DocumentationCaptureForm` içine `Missing Documentation` kategorisi altında 31 yeni capture item eklendi.
- `Eksik DOCX Görselleri` butonu eklendi; yalnızca eksik görsel senaryolarını seçer.
- Eksik görsellerin çıktı adı dokümandaki beklenen adla eşleşir; örnek: `kolon-sistemi.png`.
- `pano-docx-insert-map.json` artık `TargetHeading` alanı üretir.

## Üretim adımları

1. `samples/Taylan.Pano.TestApp` projesini çalıştır.
2. `Documentation / Documentation Capture Mode` ekranını aç.
3. Çıktı klasörünü `docs/screenshots` olarak seç.
4. `Eksik DOCX Görselleri` butonuna bas.
5. `Ekranları Üret` ile PNG ve manifest dosyalarını oluştur.
6. DOCX ekleme için:

```bash
python tools/docs/insert_screenshots_into_docx.py \
  --docx docs/Pano_Professional_Developer_User_Guide_v1.0.52.2_WithInlineScreenshots.docx \
  --screenshots docs/screenshots \
  --output docs/Pano_Professional_Developer_User_Guide_v1.0.52.2_Completed.docx
```

## Eksik görsel senaryo listesi

| DOCX bölümü | PNG dosyası |
|---|---|
| 5. Kolon Sistemi | `kolon-sistemi.png` |
| 6. Veri Baglama | `veri-baglama.png` |
| 22. Query Language | `query-language.png` |
| 23. Quick Filter Bar | `quick-filter-bar.png` |
| 25. Sorting | `sorting.png` |
| 26. Grouping | `grouping.png` |
| 27. Conditional Formatting | `conditional-formatting.png` |
| 28. Card Visual Adornments | `card-visual-adornments.png` |
| 29. Card Actions | `card-actions.png` |
| 30. Checkbox Layout | `checkbox-layout.png` |
| 32. Row Height Management | `row-height-management.png` |
| 34. Profile Migration | `profile-migration.png` |
| 36. Expression Engine | `expression-engine.png` |
| 37. Formula Columns | `formula-columns.png` |
| 38. Change Tracking | `change-tracking.png` |
| 39. Event Bus | `event-bus.png` |
| 40. Action Pipeline | `action-pipeline.png` |
| 41. Copy System Pro | `copy-system-pro.png` |
| 43. Virtual Mode | `virtual-mode.png` |
| 45. Stress Test | `stress-test.png` |
| 48. High DPI | `high-dpi.png` |
| 49. Localization | `localization.png` |
| 50. FixedFree Resize | `fixedfree-resize.png` |
| 52. Image Cache | `image-cache.png` |
| 53. Search Everywhere | `search-everywhere.png` |
| 55. Inline Editing | `inline-editing.png` |
| 56. Drag and Drop | `drag-and-drop.png` |
| 57. Multi View Sync | `multi-view-sync.png` |
| 58. State Management | `state-management.png` |
| 59. Plugin System | `plugin-system.png` |
| 61. Smart Suggestions | `smart-suggestions.png` |

## Notlar

Bu formlar gerçek ürün API'lerini ve aynı Pano kontrolünü kullanır; ancak bazı soyut altyapı başlıkları için amaç, görsel dokümantasyon ve yayın kontrolü sağlayan temsilî ekran üretmektir. Örneğin Event Bus, Action Pipeline ve Plugin System başlıklarında akış ve durum matrisi görseli üretilir.
