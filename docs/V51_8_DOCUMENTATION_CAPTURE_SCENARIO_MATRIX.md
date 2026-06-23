# ViewGrid v51.8 - Documentation Capture Scenario Matrix

Bu sürüm, TestApp içindeki Documentation Capture Mode'u sadece ana örnek ekranı yakalayan bir yapı olmaktan çıkarıp özellik/senaryo bazlı ekran görüntüsü üretim merkezine dönüştürür.

## Eklenenler

- Capture listesi kategori + özellik + senaryo formatına geçirildi.
- Aynı örnek formdan birden fazla senaryo yakalanabilir hale getirildi.
- Media Library için Poster, Gallery, MediaTile ve FilmStrip ayrı PNG olarak üretilebilir.
- Audix Pilot için Now Playing ve FilmStrip senaryoları ayrı yakalanır.
- Media Playback için Playing, Paused, Loading/Error senaryoları ayrı listelenir.
- Filter Popup UX için temel popup, uzun değerler ve resize senaryoları ayrıldı.
- Theme Audit için Dark, Light ve High Contrast/Accessibility senaryoları ayrıldı.
- Pro Experience için Layout, Performance ve Analytics senaryoları ayrıldı.
- `viewgrid-screenshots.md` artık kategori başlıklarıyla üretilir.
- `viewgrid-docx-insert-map.json` içine Category ve ScenarioName alanları eklendi.

## Davranış

Capture motoru, form içindeki sekme, combo, buton ve liste öğelerini metin veya isim üzerinden genel olarak arar. Uygun kontrol bulunursa ilgili senaryo seçilir; bulunamazsa ekran yine varsayılan haliyle yakalanır. Böylece örnek formlar değişse bile dokümantasyon capture akışı kırılmadan çalışır.

## Çıktı örnekleri

```text
docs/screenshots/
├─ viewgrid-view-details.png
├─ viewgrid-view-dashboard.png
├─ viewgrid-media-library-mediatile.png
├─ viewgrid-audix-now-playing.png
├─ viewgrid-playback-paused.png
├─ viewgrid-theme-audit-dark.png
└─ viewgrid-filter-popup-long-values.png
```

## Amaç

ViewGrid Professional Developer & User Guide dokümanında her özellik için gerçek ekran görüntüsü kullanılabilmesini sağlamak.
