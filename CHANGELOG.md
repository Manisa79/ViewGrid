# Taylan.Pano v1.0.52.2

- GitHub kaynak yayın paketi temizlendi.
- README, publishing guide, issue template ve sürüm yardımcıları `1.0.52.2` ile hizalandı.
- Generated `.git`, `.vs`, `bin`, `obj`, build DLL/EXE/PDB ve kullanıcıya özel proje dosyaları release kaynak paketinden çıkarıldı.
- NuGet metadata için MIT lisans ifadesi ve opsiyonel `PanoRepositoryUrl` MSBuild property desteği eklendi.

# Taylan.Pano v51.0

- Audix gerçek kullanım pilot ekranı eklendi.
- Pano 5.1 helper metodları eklendi.
- Theme Audit varsayılanları ve kullanım kontrol raporu eklendi.
- Example Center hızlı erişimine v51 senaryosu bağlandı.
- README ve v51 dokümanı güncellendi.


## v50.2 - Build & Runtime Hardening

- Build/runtime hardening API eklendi.
- Audix medya varsayılan profili güçlendirildi.
- Runtime check sonuç modeli eklendi.
- Example Center'a v50.2 hardening ekranı ve hızlı erişim bağlantısı eklendi.
- Tema okunurluğu, medya cache/playback ve interaction/layout güvenli varsayılanları tek metotta toplandı.

## 1.0.50.1 - Example Center + Theme + Audix Polish

- Example Center Pro içine kategori + arama destekli özellik bulucu eklendi.
- Sol örnek listesi Media/Audix, Theme/Okunurluk, Stability/Build, Performance, Analytics/Dashboard, Factory/AOI, Layout/Interaction ve Timeline/Kanban kategorilerine ayrıldı.
- v50 Foundation/Stability ve v50.1 Example Center Navigator senaryoları eklendi.
- Audix medya kullanım notları ve Pano 5 güvenli varsayılanları örnek merkezde görünür hale getirildi.
- Statik API guard tekrar çalıştırıldı.



## 1.0.50.0 - Pano 5.0 Foundation

- Pano 5.0 modül profili altyapısı eklendi.
- Audix, AOI Support Desk ve Factory Intelligence hazır profil metodları eklendi.
- Runtime stability check API'si eklendi.
- Theme Accessibility + Media Playback state güvenli varsayılanları tek profile bağlandı.
- Example Center'a Pano 5.0 Foundation / Stability örneği eklendi.
- API Guard dokümanı eklendi.

## v37-v40 Pro Experience Suite

- Faz 37 Enterprise Layout: layout snapshot JSON, kolon/görünüm kaydı.
- Faz 38 Performance Pro: large data, media library, virtual million rows ve low memory profilleri.
- Faz 39 Interaction Pro: command palette, search everywhere, power user shortcuts.
- Faz 40 Visual Analytics: KPI dashboard, heatmap, timeline, mini chart ve factory overview presetleri.
- Example Center ve SampleHub içine `PanoV37ToV40ProExperienceSampleForm` eklendi.

# Taylan.Pano v36.0 - Build Quality + Theme Studio + Media Pro

- Faz 34 Stability & Build Quality eklendi.
- Faz 35 Theme Studio eklendi.
- Faz 36 Media Pro eklendi.
- Example Center Pro içine v34/v35/v36 senaryoları eklendi.
- Ana örnek merkezine V34-36 hızlı erişim kısayolu eklendi.

# v33.0 Theme Accessibility Engine

- v31/v32 fazları korundu ve tema erişilebilirlik fazı eklendi.
- Koyu/açık tema için merkezi kontrast normalizasyonu eklendi.
- Kart/poster/dashboard hızlı filtre barında TextBox, ComboBox, Button ve bilgi metinleri okunur hale getirildi.
- Example Center içine `v33 Theme Lab / Okunurluk` senaryosu eklendi.

# v32.0 Ultimate Experience Roadmap Suite

- Faz 38-48 tek çatı altında toplandı.
- `PanoExperiencePhase`, `PanoMachineStatus`, `PanoDocumentPreviewKind`, `PanoDashboardWidgetKind` eklendi.
- `ApplyV32ExperiencePhase`, `ApplyV32UltimateExperiencePack`, `SaveExperienceSnapshot`, `SearchEverywhere`, `ResolveFactoryStatus`, `AddAiInsight` helperları eklendi.
- Example Center içine v31/v32 hızlı erişim ve yeni örnek senaryolar eklendi.
- Audix medya deneyimi, Factory Intelligence, Timeline, Document Explorer, Virtualization Pro, Search/Command, Layout Studio, Dashboard Builder ve AI Layer örnekleri eklendi.

﻿# v31.0 - Media + Smart Experience Suite

- Faz 31-37 tek pakette toparlandı.
- `Pano - Media Experience` property grubu eklendi.
- Medya placeholder, kalite rozeti ve hover/play overlay desteği eklendi.
- Example Center en üstüne `Hızlı Erişim / Nerede Bulurum?` bölümü eklendi.
- `V31 Faz Merkezi` örneği eklendi; Audix, AOI, Factory, MasterData ve genel kullanım senaryoları aranabilir/filtrelenebilir hale getirildi.
- Audix için Poster/Gallery/MediaTile/FilmStrip albüm kapağı örnek akışı güçlendirildi.

# v29.5 - FixedFree Column Resize

- Details görünümünde kolon elle genişletilirken sağdaki kullanılabilir boşluk önce FillFreeSpace kolonlarından karşılanır.
- `AbsorbColumnResizeOverflowFromFreeSpace` özelliği eklendi. Varsayılan açık.
- Yatay scrollbar yalnızca gerçekten boş alan kalmadığında açılır.

# Changelog

## 1.0.29.1 - Productization Pack

- Build/CI scriptleri eklendi.
- GitHub Actions workflow eklendi.
- API reference dokümanı eklendi.
- Productization guide eklendi.
- Stress test planı eklendi.
- Release zip/NuGet paketleme scripti eklendi.
- QualityChecks duplicate public type ve bilinen syntax hatalarını yakalayacak şekilde eklendi.
- Versiyon 1.0.29.1 olarak güncellendi.

## 1.0.29.0 - Profile System

- `PanoColumnProfile` kaldırıldı.
- Tek profil modeli `PanoLayoutProfile` oldu.
- `.panoprofile` import/export ve migration altyapısı eklendi.

## v51.1 - Product / Developer Menu Separation

- Pano ana örnek menüsü sadeleştirildi.
- Teknik örnekler `Developer Center / Tüm Teknik Örnekler` altında kategorilere ayrıldı.
- Showcase menüsü eklendi: medya vitrini, görünüm modları, poster/gallery ve MasterData senaryoları.
- Gerçek uygulamalar için Pano örnek menülerini ürün menüsünden ayırma standardı dokümante edildi.

## 1.0.51.2 - Core / Examples Separation

- Moved detached feature snippets into `Taylan.Pano.TestApp`.
- Removed Example Center specific cleanup switches from core runtime API.
- Added TestApp README and v51.2 documentation.
- Added compile exclusion for legacy snippets to avoid duplicate demo model names.

## 1.0.51.3 - TestApp Language Selector

- Added startup language selector to `samples/Taylan.Pano.TestApp`.
- Added `PanoLocalization` facade for application-wide Pano language selection.
- Persisted TestApp language selection under `settings/pano-testapp-language.json`.
- Documented multi-language usage for consumer projects.


## 1.0.51.4 - TestApp Language Form Polish

- Fixed clipped remember checkbox in startup language selector.
- Moved `StartupLanguageForm` UI controls into `StartupLanguageForm.Designer.cs`.
- Added runtime language change entry in TestApp navigation.

## 1.0.51.6 - Documentation Capture Mode

- Added `DocumentationCaptureForm` to `Taylan.Pano.TestApp`.
- Added automatic Example Center screenshot generation for documentation.
- Added screenshot manifest, markdown gallery and DOCX insert map outputs.
- Added `tools/docs/insert_screenshots_into_docx.py` helper script.
- Updated version metadata to `1.0.51.6-community-preview-doc-capture`.
