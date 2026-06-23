# ViewGrid v1.0.52.2 - Documentation Capture Mode

Bu güncelleme GitHub dokümantasyonu için Example Center ekran görüntülerini otomatik üretme altyapısını korur ve v1.0.52.2 dokümanında eksik kalan bağımsız görseller için ek capture senaryoları sağlar.

## Amaç

ViewGrid Professional Developer & User Guide içinde her ViewMode ve showcase bölümünün yalnızca açıklama/kod ile değil, gerçek ekran görüntüleriyle desteklenmesi hedeflenir.

## TestApp içinde yeni ekran

`ViewGrid.TestApp` sol menüsüne şu seçenek eklendi:

```text
Documentation / Documentation Capture Mode
```

Bu ekran seçilen örnek formları sırayla açar, `DrawToBitmap` ile PNG çıktısı üretir ve doküman için ek dosyalar oluşturur.

## Üretilen çıktılar

Varsayılan klasör:

```text
<ViewGrid.TestApp.exe klasörü>/docs/screenshots
```

Üretilen dosyalar:

```text
viewgrid-example-center.png
viewgrid-viewmode-showcase.png
viewgrid-media-library.png
viewgrid-audix-pilot.png
viewgrid-screenshot-manifest.json
viewgrid-screenshots.md
viewgrid-docx-insert-map.json
```

## Word dokümanına ekleme

Otomatik ekleme için yardımcı script:

```bash
python tools/docs/insert_screenshots_into_docx.py \
  --docx docs/ViewGrid_Professional_Developer_User_Guide_v1.0.51_Full.docx \
  --screenshots docs/screenshots \
  --output docs/ViewGrid_Professional_Developer_User_Guide_with_Screenshots.docx
```

Script `viewgrid-docx-insert-map.json` dosyasını okur. v1.0.52.2 itibarıyla `TargetHeading` alanı varsa görselleri ilgili DOCX başlığının altına inline ekler; başlık bulunamazsa yerleştirilemeyen görselleri son ek galerisine alır.

## Eksik DOCX görselleri

`Documentation Capture Mode` ekranında `Eksik DOCX Görselleri` butonu yalnızca 1.0.52.2 kılavuzunda eksik takip listesinde görünen başlıkları seçer.

Üretilen dosya adları dokümandaki beklenen adlarla birebir eşleşir:

```text
docs/screenshots/kolon-sistemi.png
docs/screenshots/veri-baglama.png
docs/screenshots/query-language.png
...
docs/screenshots/smart-suggestions.png
```

`viewgrid-docx-insert-map.json` içindeki her eksik görsel girdisi ayrıca `TargetHeading` taşır. Böylece otomatik ekleme scripti görseli dokümanın ilgili bölüm başlığının altına yerleştirebilir.

## Inline DOCX ekleme

```bash
python tools/docs/insert_screenshots_into_docx.py \
  --docx docs/ViewGrid_Professional_Developer_User_Guide_v1.0.52.2_WithInlineScreenshots.docx \
  --screenshots docs/screenshots \
  --output docs/ViewGrid_Professional_Developer_User_Guide_v1.0.52.2_Completed.docx
```

Legacy son-galeri davranışı gerekiyorsa:

```bash
python tools/docs/insert_screenshots_into_docx.py \
  --docx docs/ViewGrid_Professional_Developer_User_Guide_v1.0.52.2_WithInlineScreenshots.docx \
  --screenshots docs/screenshots \
  --output docs/ViewGrid_Professional_Developer_User_Guide_v1.0.52.2_Gallery.docx \
  --mode gallery
```
