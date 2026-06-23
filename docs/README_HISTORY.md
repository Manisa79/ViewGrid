## ViewGrid v51.0 - Audix Pilot / Theme Audit / Example Cleanup

Bu güncelleme yeni özellik eklemekten çok mevcut ViewGrid 5.x yeteneklerini gerçek projeye tak-çalıştır hale getirmeye odaklanır.

- `ApplyViewGrid51RealUsageDefaults()` eklendi.
- `ApplyAudix51MediaPilotDefaults()` eklendi.
- `ApplyTheme51AuditDefaults(...)` eklendi.
- `RunViewGrid51UsageChecks()` ile Audix, tema, Example Center ve stability kontrolleri tek raporda görünür.
- Example Center içine `ViewGrid 5.1 Audix Pilot` eklendi.
- Audix için Poster / MediaTile / Gallery / FilmStrip, albüm kapağı, play/pause state, now-playing rozeti, equalizer ve video preview davranışı tek örnekte gösterildi.

# ViewGrid

## ViewGrid v50.2 - Build & Runtime Hardening

Bu güncelleme yeni özellik eklemekten çok mevcut ViewGrid 5.0 özelliklerini projeye tak-çalıştır hale getirmeye odaklanır.

- `ApplyViewGrid502HardeningDefaults()` eklendi.
- `ApplyAudix502MediaDefaults()` ile Audix için Poster/Gallery/FilmStrip, medya cache, lazy loading, playback state ve video preview güvenli varsayılanları birlikte uygulanır.
- `RunViewGrid502RuntimeHardeningChecks()` ile tema, medya, interaction ve layout ayarları runtime'da kontrol edilebilir.
- Example Center'a `ViewGridV502HardeningSampleForm` eklendi.
- Detay: `docs/V50_2_BUILD_RUNTIME_HARDENING.md`.

 v50.1 Update

Bu sürüm ViewGrid 5.0 Foundation üzerine Example Center bulunabilirliği, tema okunurluğu ve Audix medya kullanım akışını toparlar. Example Center Pro içinde artık kategori + arama ile özellik bulunabilir.

Öne çıkanlar:

- Example Center Pro: kategori + arama destekli bulucu
- v50 Foundation/Stability senaryosu
- v50.1 Example Center Navigator senaryosu
- Audix medya profili için hazır kullanım notları
- Theme Accessibility ve Media Playback güvenli varsayılanları

---


## v37-v40 Pro Experience Suite

- Faz 37 Enterprise Layout: layout snapshot JSON, kolon/görünüm kaydı.
- Faz 38 Performance Pro: large data, media library, virtual million rows ve low memory profilleri.
- Faz 39 Interaction Pro: command palette, search everywhere, power user shortcuts.
- Faz 40 Visual Analytics: KPI dashboard, heatmap, timeline, mini chart ve factory overview presetleri.
- Example Center ve SampleHub içine `ViewGridV37ToV40ProExperienceSampleForm` eklendi.

## 1.0.33.0 - Theme Accessibility Engine

- v31/v32 görsel ve medya fazları korundu; koyu/açık tema okunurluğu için merkezi kontrast motoru eklendi.
- `ViewGridThemeAccessibility` ve `ViewGridControl.EnforceThemeAccessibility` eklendi.
- Card/Poster/Dashboard hızlı filtre barında TextBox, ComboBox, Button, chip ve “Aktif filtre yok” bilgi metni koyu temada okunur hale getirildi.
- Tema uygulanırken Panel/Control/Header/Border/Muted/Empty/Accent/Selection renkleri normalize edilir.
- Example Center içine `v33 Theme Lab / Okunurluk` eklendi.
- Detaylar: `docs/V33_THEME_ACCESSIBILITY_ENGINE.md`

## 1.0.28.4 - Filter Popup State & Default Popup Experience

- Header filtre ikonuna tıklanınca varsayılan deneyim artık hızlı popup filtre menüsü.
- `FilterMenuMode` tasarım zamanında `PopupMenu`, `ModalWindow` veya `Both` olarak değiştirilebilir.
- Popup/Card filter listelerinde tek tek seçim yapılınca `Tümünü seç` otomatik olarak Checked / Unchecked / Indeterminate durumuna geçer.
- Ayrı filtre penceresi ve popup filtre aynı seçim davranışını paylaşacak şekilde hizalandı.
- Detaylar: `docs/V28_4_FILTER_POPUP_STATE.md`

## 1.0.28.3 - Card Visual Adornments

- Card/Tile/Dashboard/Kanban/Timeline renderer içine genel amaçlı görsel eklenti katmanı eklendi.
- `ViewGridCardVisualInfo`, `ViewGridCardBadge`, `ViewGridCardGlyph`, `ViewGridCardAccentMode`, `ViewGridCardBadgePlacement` eklendi.
- `CardVisualInfoGetter` ile uygulama satır bazlı accent bar, status dot, corner badge, ikon/glyph ve sayı rozeti verebilir.
- `CardAutoBadgesFromBadgeColumns` ile `ViewGridColumnKind.Badge` kolonları kart üstünde otomatik rozet olarak çizilebilir.
- Eski `CardStatusIndicators` davranışı korunarak yeni görsel altyapıya bağlandı; AOI Support Desk gibi ticket ekranları dışında stok, üretim, dosya, sipariş ekranlarında da kullanılabilir.
- Detaylar: `docs/V28_3_CARD_VISUAL_ADORNMENTS.md`

## 1.0.27.7 - Card / Large View Filter UX

- Kart, geniş kart, dashboard, kanban, poster ve ikon grid gibi header olmayan büyük görünümler için hızlı filtre barı eklendi.
- `ShowQuickFilterBar`, `ShowFloatingFilterButton`, `ShowActiveFilterChips`, `CardFilterUxPlacement` ve ilgili designer/runtime propertyleri eklendi.
- Floating filtre butonu kolon filtre popup/pencere akışına doğrudan bağlandı.
- Aktif filtre chipleri ile global/kolon filtreleri görünür ve tek tıkla kaldırılabilir hale geldi.
- Example Center Pro ve ayrı `ViewGridV277CardFilterUxForm` örneği eklendi.
- Detaylar: `docs/V27_7_CARD_FILTER_UX.md`

## 1.0.27.2 - User Facing Features

- State / Preset menüsü başlık ve gövde sağ tık menülerine açıldı.
- Görünüm Senaryosu menüsü eklendi: MasterData, BOM, Ticket Dashboard, Timeline, Yoğun Veri vb.
- Designer SmartTag içine ActiveScenario, State menüsü ve otomatik state ayarları eklendi.
- `SaveStateToDefaultPath`, `LoadStateFromDefaultPath`, `SaveStatePreset`, `LoadStatePreset`, `ResetRuntimeState` public API olarak eklendi.
- Example Center Pro v27.2 toolbar'ı state/preset/reset akışını gösterir.
- MasterData/AOI için `UseMasterDataDefaults`, `UseBomPositionDefaults`, `UseTicketBoardDefaults` hızlı başlangıç yardımcıları eklendi.
- Detaylar: `docs/V27_USER_FACING_FEATURES.md`

## 1.0.27.0 - Product Core / MasterData ready

- v27 State Engine eklendi: kolon layout, filtre, sıralama, görünüm modu, seçim ve checked satırlar tek JSON state olarak kaydedilebilir/yüklenebilir.
- `ViewGridState`, `CaptureState`, `ApplyState`, `SaveState`, `LoadState` eklendi.
- `StateKeyAspectName` ile MasterData/SAP/BOM/Ticket ekranlarında stabil seçim geri yükleme desteği eklendi.
- Hafif renderer profil altyapısı eklendi: Badge, Progress, Hyperlink, ActionButton, WarningStatus gibi cell visual profile metadata desteği.
- Range/page tabanlı `ViewGridRangeVirtualProvider` eklendi; SQL/SAP/API gibi büyük veri kaynakları için sayfa cache başlangıcı hazırlandı.
- Örnek merkezine `ViewGrid v27 Product Core` örneği eklendi.
- Versiyon 1.0.27.0 yapıldı.

## 1.0.27.1 - Example Center Pro

- Örnek merkezine `ExampleCenterProForm` eklendi.
- Ticket Dashboard, Kanban, MasterData BOM, Program Dosyaları, Makine/Hat seçimi, Timeline, Master-Detail ve Yoğun Veri senaryoları tek vitrin ekranında toplandı.
- V27 state save/load, gruplama, tema geçişi ve senaryo bazlı görünüm ayarları örnek merkezinde görünür hale getirildi.

## 1.0.26.90 - MasterData view scenarios

- `ViewGridScenario` ve `grid.ApplyScenario(...)` eklendi.
- MasterData/SAP/BOM kullanımına hazır senaryolar: Standart Tablo, Yoğun Veri Tablosu, Ürün Ağacı, BOM/Pozisyon Listesi, Program Dosyaları, Makine/Hat Seçimi, Ticket Dashboard, İşlem Geçmişi, MasterData Detay.
- Örnek merkezine "MasterData görünüm senaryoları" vitrini eklendi.
- AOI Support Desk, MasterData ve üretim programı ekranlarında aynı görünüm dilini kullanmak için hazır preset yaklaşımı eklendi.
- Versiyon 1.0.26.90 yapıldı.

﻿## 1.0.26.88 - Filter persistence designer/runtime option

- `ViewGridControl.PersistColumnFilters` public designer/runtime özelliği netleştirildi.
- Varsayılan `false`: kolon sırası/genişliği/görünürlük/sort layout ile korunur, global arama ve kolon filtreleri kaydedilmez/yüklenmez.
- Runtime kullanım için `PersistFiltersInLayout` alias eklendi.
- Eski layout dosyalarında filtre kayıtlı olsa bile `PersistColumnFilters = false` iken yüklemede filtreler sanitize edilir.
- Versiyon 1.0.26.88 yapıldı.

## 1.0.26.87 - Tile checkbox position designer/runtime

- Tile/Kart/Geniş Kart/Poster checkbox konumu designer ve runtime için netleştirildi.
- `TileCheckBoxPosition` dört köşe desteği: TopLeft, TopRight, BottomLeft, BottomRight.
- `TileCheckBoxMargin` eklendi; checkbox seçilen köşeden istenen mesafeye alınabilir.
- Örnek merkezindeki Kart / Geniş Kart Checkbox demosuna Sol Üst, Sağ Üst, Sol Alt, Sağ Alt ve margin test butonları eklendi.
- Versiyon 1.0.26.87 yapıldı.

## 1.0.26.87 - Tile / Card checkbox support

- Tile/Kart/Geniş Kart/Poster görünümlerinde overlay checkbox desteği eklendi.
- Designer propertyleri: TileCheckBoxes, TileCheckBoxAspectName, TileCheckBoxPosition, TileCheckBoxSize.
- Kart checkbox hit-test, paint ve header checkbox senkronu eklendi.
- Örnek merkezine "Kart / Geniş Kart Checkbox" demosu eklendi.
- Versiyon 1.0.26.87 yapıldı.



## v27.3.1 UltraMax Rebuild
- FilterPopupMaximumSize default: 1800 x 1200
- FilterWindowMaximumSize default: 2000 x 1400
- FilterPopupLimitToWorkingArea: true
- Popup ve ayrı pencere maksimum boyutları ekran çalışma alanına göre güvenli şekilde kırpılır.


## v27.4 Advanced Filter & Preset Engine

- Çok kolonlu gelişmiş filtre oluşturucu
- AND / OR filtre mantığı
- Filtre preset kaydet/yükle
- Header/gövde menülerinden gelişmiş filtre erişimi
- Example Center Pro içinde Advanced Filter & Preset senaryosu


## 1.0.27.6 - Design-Time Theme Sync

- ViewGridControl artık Visual Studio tasarım zamanında varsayılan olarak temiz açık tema ile çizilir.
- Runtime tema davranışı korunur; design-time sync yalnızca designer yüzeyinde çalışır.
- `EnableDesignTimeThemeSync`, `DesignTimeFollowParentTheme` ve `DesignTimeThemeSyncMenus` eklendi.
- SmartTag içine designer tema senkronizasyon ayarları eklendi.
- Example Center içine `v27.6 Design-Time Theme Sync` örneği eklendi.

## 1.0.27.5 - Menu & Icon Customization

- Tüm built-in ViewGrid menüleri designer/runtime tarafında yönetilebilir hale getirildi.
- Header/body/merged menü grupları ayrı ayrı açılıp kapatılabilir.
- Menü item bazlı gizleme/gösterme eklendi.
- Kullanıcı ContextMenuStrip merge davranışı geliştirildi.
- Built-in ikon + kullanıcı ikon klasörü + ImageList + dark/light ImageList desteği eklendi.
- Example Center Pro içine Menu & Icon Customization senaryosu eklendi.

## v27.8 Card Layout + Tree UX

- Card/Poster/Dashboard görünümlerinde hızlı filtre barı artık içerik alanını rezerve eder; kartlar filtre barının altında kesilmeden başlar.
- `CardViewReserveFilterArea`, `CardFilterContentSpacing`, `ReservedCardFilterAreaHeight` eklendi.
- TreeViewGridControl için sağ tık ağaç menüsü, çift tıkla aç/kapat, aramada üst dalları açma, seçili dalı genişlet/daralt ve düğüm yolu kopyalama eklendi.
- Example Center içinde v27.8 davranışı TreeView + TreeGrid örneğine bağlandı.

## v27.9 Popular Enterprise Features

Bu sürümde diğer popüler grid bileşenlerinde sık kullanılan arama paneli, özet/footer, conditional formatting, frozen column, column chooser, advanced filter ve preset davranışları ViewGrid tarafında tek komutla açılabilir hale getirildi.

```csharp
grid.ApplyPopularFeaturePack(ViewGridPopularFeaturePreset.MasterData);
grid.AddSemanticStatusConditionalFormat("Status");
grid.AddNumericSummary("Quantity", ViewGridSummaryType.Sum, "Toplam {0}");
```

Örnek merkezinde `v27.9 Popular Enterprise Features` ekranı eklendi.


## v28.1 Poster Mode

`ViewGridMode.Poster` eklendi. Büyük görsel kart/poster senaryoları artık `ExtraLargeIcons` yerine daha anlaşılır bir enum adıyla kullanılabilir. Poster ölçüleri `PosterPreferredWidth`, `PosterPreferredHeight`, `PosterImageHeight` ve `PosterModeAutoLayout` ile designer/runtime tarafından ayarlanabilir.


## v28.2 Card Status Indicators

CardView/Dashboard/Kanban/Poster görünümünde Details hücre renderer'ına bağlı kalmadan durum noktası ve üst status bar çizimi eklendi. `CardStatusAspectName`, `CardStatusColorGetter` ve `CardStatusValueGetter` ile AOI Support Desk gibi ticket ekranlarında durum rengi merkezi şekilde yönetilebilir.


## v28.5 Enterprise Feature Suite

- Smart filter metadata, kolon bazlı arama aliasları ve fuzzy/qualified smart search yardımcıları eklendi.
- Card/Tile/Dashboard görünümlerine inline action butonları eklendi.
- Conditional rule engine, column profile aliasları, aggregate metadata, live refresh/row changed hazırlığı ve plugin koleksiyonu eklendi.
- Örnekler: `EnterpriseFeatureSuiteSamples.cs`.


## v28.6 highlights

- Card Layout Designer foundation for Card/Tile/Dashboard.
- Excel-style advanced filter shortcuts: Select Only, Exclude, Invert, Top 10, Above Average.
- Details/List row-height stability: row selection no longer expands row height unless multiline auto-height is explicitly enabled.


## v29.1 Productization Pack

Bu paket ViewGrid'yi ürünleşmeye hazırlayan build, CI, release ve dokümantasyon katmanlarını içerir.

### Hızlı build

```powershell
./build/Build-ViewGrid.ps1 -Configuration Release -Pack
```

### Release zip

```powershell
./release/Make-ReleaseZip.ps1 -Version 1.0.29.1
```

### Dokümanlar

- `docs/API_REFERENCE.md`
- `docs/PRODUCTIZATION_GUIDE.md`
- `docs/STRESS_TEST_PLAN.md`
- `RELEASE_CHECKLIST.md`
- `CHANGELOG.md`

### Stress test yaklaşımı

10K/100K normal liste, 1M+ virtual provider, 1B/1T ise fake virtual provider senaryosu olarak ölçülmelidir. Bu yaklaşım RAM'i şişirmeden gerçek ViewGrid render/scroll pipeline'ını test eder.
## v31 Media + Smart Experience Suite

ViewGrid v31, görsel medya ve örnek merkezi tarafını toparlar:

- `ViewGridMode.Poster`, `Gallery`, `MediaTile`, `FilmStrip` için Audix/Plex/Spotify tarzı kapaklı kullanım.
- `MediaPlaceholderImage`, `ShowMediaOverlayButton`, `MediaQualityBadgeAspectName`, `MediaQualityBadgeGetter` ile albüm kapağı, play overlay ve FLAC/MP3 rozeti.
- Example Center içinde en üstte **Hızlı Erişim / Nerede Bulurum?** bölümü.
- Yeni **V31 Faz Merkezi**: Faz 31-37 özellikleri, proje bazlı kullanım ve nerede bulunur bilgisi aranabilir/filtrelenebilir şekilde.

Audix hızlı örnek:

```csharp
viewgrid.TilePosterMode = true;
viewgrid.MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
viewgrid.MediaPlaceholderImage = placeholderAlbumCover;
viewgrid.ShowMediaOverlayButton = true;
viewgrid.MediaQualityBadgeAspectName = "Quality";
viewgrid.SetViewMode(ViewGridMode.Poster);
```


## v36 Build Quality + Theme Studio + Media Pro

Bu pakette v34 Build Quality, v35 Theme Studio ve v36 Media Pro fazları birleştirildi. Detaylar için `docs/V34_35_36_BUILD_THEME_MEDIA_PRO.md` dosyasına bakın.


## v41 Media Playback State Suite

Audix ve video arşivleri için medya kartlarında play/pause/loading/error state desteği eklendi. `MediaPlayPauseClicked` olayıyla host uygulama gerçek player akışına bağlanabilir; `MediaKindGetter` ile audio/video özel davranış seçilebilir.


## ViewGrid 5.0 Foundation / Stability

Bu sürümde yeni özellik ekleme hızından çok mevcut platformun stabil mimariye oturması hedeflendi.

- `ApplyViewGrid5FoundationDefaults()`
- `ApplyAudixMediaProfile()`
- `ApplyAoiSupportDeskProfile()`
- `ApplyFactoryIntelligenceProfile()`
- `RunViewGrid5RuntimeChecks()`
- Example Center: **ViewGrid 5.0 Foundation / Stability**

Detay: `docs/V50_VIEWGRID_FOUNDATION.md`

## v51.1 Product / Developer Menu Separation

ViewGrid test uygulamasındaki örnek menüsü sadeleştirildi. Kullanıcıya dönük kısa menü korunurken tüm teknik örnekler Developer Center altında gruplandı. Gerçek uygulamalarda ViewGrid örnek menülerinin gösterilmemesi, örneklerin ayrı Example Center/Test App içinde tutulması önerilir. Ayrıntılar: `docs/V51_1_PRODUCT_DEVELOPER_MENU_SEPARATION.md`.

## v51.2 Core / Examples Separation

For GitHub/community preview readiness, ViewGrid now keeps the production control package and sample application clearly separated:

- `src/ViewGrid` contains the reusable `ViewGrid.dll` control/API surface.
- `samples/ViewGrid.TestApp` contains Example Center, showcase forms, demo data and developer samples.
- Legacy detached feature snippets were moved under `samples/ViewGrid.TestApp/Snippets/FeatureSamples` and excluded from compilation.

Host applications should reference only the ViewGrid project/DLL and should not include sample menus unless they intentionally add their own developer/demo screen.


## v1.0.51.3 - TestApp Language Selector

`ViewGrid.TestApp` açılışına dil seçim ekranı eklendi. Seçilen dil `ViewGridLocalization.Use(...)` ile tüm ViewGrid built-in menü/dialog metinlerine uygulanır ve `settings/viewgrid-testapp-language.json` içinde saklanabilir.

```csharp
using ViewGrid.Localization;

ViewGridLocalization.Use(ViewGridLanguage.Turkish);
```

Detay: `docs/V51_3_TESTAPP_LANGUAGE_SELECTOR.md`


## v1.0.51.4 - TestApp Language Form Polish

- TestApp dil seçimi penceresindeki checkbox kırpılma problemi düzeltildi.
- `StartupLanguageForm` kontrolleri Designer.cs içine taşındı.
- TestApp içinde sol menüye **Dil Değiştir** seçeneği eklendi.
- Kullanıcı test ekranına girdikten sonra dili tekrar değiştirip yeni açılan örneklerde seçili dili kullanabilir.
