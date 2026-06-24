using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Filtering;
using Taylan.Pano.Theming;
using System.Text;

namespace Taylan.Pano.TestApp;

public sealed class ExampleCenterProForm : Form
{
    private readonly SplitContainer _mainSplit = new() { Dock = DockStyle.Fill, SplitterDistance = 300, FixedPanel = FixedPanel.Panel1 };
    private readonly ListBox _scenarioList = new() { Dock = DockStyle.Fill, IntegralHeight = false, BorderStyle = BorderStyle.None };
    private readonly Panel _navigator = new() { Dock = DockStyle.Top, Height = 98, Padding = new Padding(10, 8, 10, 8) };
    private readonly TextBox _scenarioSearch = new() { Dock = DockStyle.Top, Height = 28, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Ara: audix, kolon, dashboard, diagnostics, filtre..." };
    private readonly ComboBox _scenarioCategory = new() { Dock = DockStyle.Top, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _navigatorInfo = new() { Dock = DockStyle.Top, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _hero = new() { Dock = DockStyle.Top, Height = 96, Padding = new Padding(16), TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _description = new() { Dock = DockStyle.Top, Height = 84, Padding = new Padding(16, 10, 16, 10), TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly SplitContainer _contentSplit = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 520 };
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = PanoDataMode.Object,
        HeaderContextMenuBehavior = PanoHeaderContextMenuBehavior.Full,
        FilterMenuMode = PanoFilterMenuMode.Both,
        EmptyListMessage = "Example Center Pro için kayıt yok",
        StateKeyAspectName = nameof(ProRow.Id),
        PersistColumnFilters = true,
        PersistVisualPreferences = true,
        EnableGrouping = true,
        ShowStateMenuItems = true,
        ShowScenarioMenuItems = true,
        AutoLoadStateOnCreate = false,
        AutoSaveStateOnDispose = false,
        FilterPopupResizable = true,
        FilterPopupRememberSize = true,
        FilterPopupShowValueTooltips = true,
        FilterPopupAutoWidthForLongValues = true,
        ShowAdvancedFilterMenuItems = true,
        EnableFilterPresets = true
    };
    private readonly TextBox _details = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 10F) };
    private readonly List<ScenarioInfo> _scenarios = new();
    private readonly List<ProRow> _rows = new();
    private bool _dark = true;

    private string StateFile => Path.Combine(Application.StartupPath, "pano-example-center-pro-state.json");

    public ExampleCenterProForm()
    {
        Text = "Pano Example Center Pro - Özellik Rehberi";
        Width = 1440;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;

        BuildScenarios();
        ConfigureColumns();
        ConfigureToolbar();
        ConfigureNavigator();
        BuildLayout();
        ApplyTheme(true);

        _scenarioList.DisplayMember = nameof(ScenarioInfo.DisplayTitle);
        _scenarioList.SelectedIndexChanged += (_, __) => ApplySelectedScenario();
        RefreshScenarioFilter();
    }

    private void BuildLayout()
    {
        _mainSplit.Panel1.Controls.Add(_scenarioList);
        _mainSplit.Panel1.Controls.Add(_navigator);
        _mainSplit.Panel1.Controls.Add(_hero);

        _contentSplit.Panel1.Controls.Add(_grid);
        _contentSplit.Panel2.Controls.Add(_details);

        _mainSplit.Panel2.Controls.Add(_contentSplit);
        _mainSplit.Panel2.Controls.Add(_description);
        _mainSplit.Panel2.Controls.Add(_tool);
        Controls.Add(_mainSplit);
    }


    private void ConfigureNavigator()
    {
        _scenarioCategory.Items.AddRange(new object[]
        {
            "Tümü",
            "Başlangıç / Temel Görünümler",
            "Filtre / Menü / State",
            "Medya / Audix / Doküman",
            "Tema / Okunurluk",
            "Dashboard / Analytics",
            "Performans / Büyük Veri",
            "Stabilite / Diagnostics",
            "Designer / Kolon",
            "Workflow / Kanban / Timeline"
        });
        _scenarioCategory.SelectedIndex = 0;
        _scenarioCategory.SelectedIndexChanged += (_, __) => RefreshScenarioFilter();
        _scenarioSearch.TextChanged += (_, __) => RefreshScenarioFilter();
        _navigatorInfo.Text = "Kategori seç veya özellik/API adıyla ara.";
        _navigator.Controls.Add(_scenarioSearch);
        _navigator.Controls.Add(_scenarioCategory);
        _navigator.Controls.Add(_navigatorInfo);
    }

    private void RefreshScenarioFilter()
    {
        string query = (_scenarioSearch.Text ?? string.Empty).Trim();
        string category = Convert.ToString(_scenarioCategory.SelectedItem) ?? "Tümü";
        var filtered = _scenarios.Where(s => MatchesScenarioFilter(s, query, category)).ToList();

        _scenarioList.DataSource = null;
        _scenarioList.DisplayMember = nameof(ScenarioInfo.DisplayTitle);
        _scenarioList.DataSource = filtered;
        _navigatorInfo.Text = filtered.Count == _scenarios.Count
            ? $"Tüm örnekler gösteriliyor ({filtered.Count})"
            : $"Filtreli örnekler: {filtered.Count} / {_scenarios.Count}";

        if (filtered.Count > 0)
            _scenarioList.SelectedIndex = 0;
        else
        {
            _grid.SetObjects(Array.Empty<object>());
            _description.Text = "Arama/kategori ile eşleşen örnek bulunamadı.";
            _details.Text = "Örnek bulmak için daha genel bir kelime deneyin: media, audix, kolon, dashboard, diagnostics, filtre, theme, performance.";
        }
    }

    private static bool MatchesScenarioFilter(ScenarioInfo scenario, string query, string category)
    {
        string text = (scenario.Title + " " + scenario.Category + " " + scenario.Description + " " + scenario.LongDescription).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(query) && !text.Contains(query.ToLowerInvariant()))
            return false;

        if (string.Equals(category, "Tümü", StringComparison.OrdinalIgnoreCase)) return true;
        if (category.StartsWith("Başlangıç", StringComparison.OrdinalIgnoreCase)) return ContainsAny(text, "başlangıç", "temel", "ticket dashboard", "masterdata", "program dosyaları", "makine");
        if (category.StartsWith("Filtre", StringComparison.OrdinalIgnoreCase)) return ContainsAny(text, "filtre", "filter", "menü", "menu", "state", "preset", "enterprise", "ux polish");
        if (category.StartsWith("Medya", StringComparison.OrdinalIgnoreCase)) return ContainsAny(text, "media", "audix", "poster", "gallery", "filmstrip", "kapak", "video", "playback", "document", "doküman");
        if (category.StartsWith("Tema", StringComparison.OrdinalIgnoreCase)) return ContainsAny(text, "theme", "tema", "okunurluk", "contrast", "factoryos", "dark", "koyu");
        if (category.StartsWith("Dashboard", StringComparison.OrdinalIgnoreCase)) return ContainsAny(text, "analytics", "dashboard", "kpi", "heatmap", "chart", "factory");
        if (category.StartsWith("Performans", StringComparison.OrdinalIgnoreCase)) return ContainsAny(text, "performance", "virtual", "yoğun", "1m", "100k", "dense", "paint");
        if (category.StartsWith("Stabilite", StringComparison.OrdinalIgnoreCase)) return ContainsAny(text, "build", "stability", "quality", "api", "foundation", "guard", "compile", "diagnostics", "hardening");
        if (category.StartsWith("Designer", StringComparison.OrdinalIgnoreCase)) return ContainsAny(text, "designer", "kolon", "column", "editor");
        if (category.StartsWith("Workflow", StringComparison.OrdinalIgnoreCase)) return ContainsAny(text, "timeline", "kanban", "geçmiş", "ticket", "workflow");
        return true;
    }

    private static bool ContainsAny(string text, params string[] words)
    {
        foreach (string word in words)
            if (text.Contains(word, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new PanoColumn("Seç", nameof(ProRow.Selected), 48) { Kind = PanoColumnKind.CheckBox });
        _grid.Columns.Add(new PanoColumn("Tip", nameof(ProRow.Type), 110) { Kind = PanoColumnKind.Badge, AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("Kod / Ticket", nameof(ProRow.Code), 150));
        _grid.Columns.Add(new PanoColumn("Başlık", nameof(ProRow.Title), 260) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 3 });
        _grid.Columns.Add(new PanoColumn("Hat / Makine", nameof(ProRow.Machine), 130) { AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("Ref / Katman", nameof(ProRow.RefDes), 120));
        _grid.Columns.Add(new PanoColumn("İlerleme", nameof(ProRow.Progress), 90) { Kind = PanoColumnKind.ProgressBar });
        _grid.Columns.Add(new PanoColumn("Durum", nameof(ProRow.Status), 120) { Kind = PanoColumnKind.Badge, AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("Açıklama", nameof(ProRow.Detail), 420) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 5 });
    }

    private void ConfigureToolbar()
    {
        _tool.Items.Add(new ToolStripButton("State Kaydet", null, (_, __) => { _grid.AutoStateFilePath = StateFile; _grid.SaveStateToDefaultPath("Example Center Pro"); MessageBox.Show(this, "Grid state kaydedildi.", "Pano", MessageBoxButtons.OK, MessageBoxIcon.Information); }));
        _tool.Items.Add(new ToolStripButton("State Yükle", null, (_, __) => { _grid.AutoStateFilePath = StateFile; _grid.LoadStateFromDefaultPath(); }));
        _tool.Items.Add(new ToolStripButton("Preset Kaydet", null, (_, __) => _grid.SaveStatePreset("ExampleCenterPro")));
        _tool.Items.Add(new ToolStripButton("Preset Yükle", null, (_, __) => _grid.LoadStatePreset("ExampleCenterPro")));
        _tool.Items.Add(new ToolStripButton("State Sıfırla", null, (_, __) => _grid.ResetRuntimeState(clearFilters: true, clearGrouping: true, resetColumns: true)));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Gelişmiş Filtre", null, (_, __) => _grid.ShowAdvancedFilterBuilder()));
        _tool.Items.Add(new ToolStripButton("Filtre Preset Kaydet", null, (_, __) => _grid.SaveCurrentFilterPreset("ExampleCenterFilter")));
        _tool.Items.Add(new ToolStripButton("Filtre Preset Yükle", null, (_, __) => _grid.LoadFilterPreset("ExampleCenterFilter")));
        _tool.Items.Add(new ToolStripButton("Açık Ticket", null, (_, __) => _grid.ApplyBuiltInFilterPreset("Open Tickets")));
        _tool.Items.Add(new ToolStripButton("Eksik BOM", null, (_, __) => _grid.ApplyBuiltInFilterPreset("Eksik BOM")));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Grupla: Durum", null, (_, __) => _grid.SetGroupBy(nameof(ProRow.Status))));
        _tool.Items.Add(new ToolStripButton("Grupla: Makine", null, (_, __) => _grid.SetGroupBy(nameof(ProRow.Machine))));
        _tool.Items.Add(new ToolStripButton("Gruplamayı Temizle", null, (_, __) => _grid.ClearGrouping()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Açık Tema", null, (_, __) => ApplyTheme(false)));
        _tool.Items.Add(new ToolStripButton("Koyu Tema", null, (_, __) => ApplyTheme(true)));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Menü: Minimal", null, (_, __) => { _grid.MenuOptions.ApplyMinimalProfile(); _grid.RefreshView(); }));
        _tool.Items.Add(new ToolStripButton("Menü: Full", null, (_, __) => { _grid.MenuOptions.ApplyFullProfile(); _grid.RefreshView(); }));
        _tool.Items.Add(new ToolStripButton("Advanced Gizle/Göster", null, (_, __) => { bool hide = (_grid.HiddenMenuItems & PanoMenuItemKeys.AdvancedFilter) == 0; _grid.SetMenuItemVisible(PanoMenuItemKeys.AdvancedFilter, !hide); _grid.RefreshView(); }));
    }

    private void BuildScenarios()
    {
        _scenarios.Add(new ScenarioInfo("01  Ticket Dashboard", PanoScenario.TicketBoard, "AOI Support Desk ticketleri için durum, önem ve mesaj özeti odaklı kart vitrini.", "Açık/Bekliyor/Çözüldü ticket akışında teknisyenin en hızlı karar vereceği görünüm. Dashboard Kart, badge kolonları ve state engine birlikte gösterilir."));
        _scenarios.Add(new ScenarioInfo("02  Kanban Takip", PanoScenario.TicketBoard, "Ticket veya görevleri Kanban mantığında takip etmek için kart ağırlıklı görünüm.", "Open / In Progress / Waiting / Done benzeri süreçlerde kullanılabilir. Pano burada gerçek kanban kolon motoruna geçmeden önce kart + grup yaklaşımı ile pratik bir kullanım sunar."));
        _scenarios.Add(new ScenarioInfo("03  MasterData BOM", PanoScenario.BomPositions, "BOM, SAP pozisyonları ve ürün ağacı detayları için yoğun ama okunabilir tablo.", "MasterData tarafında RefDes, malzeme kodu, katman, miktar ve durum gibi kolonlar yoğun listede hızlı taranır. Excel benzeri kullanım hedeflenir."));
        _scenarios.Add(new ScenarioInfo("04  Program Dosyaları", PanoScenario.ProgramFiles, "Dizgi programı, makine programı ve çıktı dosyaları için kart görünümü.", "Flex, QX, Axial, Radial gibi makine klasörleri ve program çıktılarında dosya odaklı seçim ekranı için uygundur."));
        _scenarios.Add(new ScenarioInfo("05  Makine / Hat Seçimi", PanoScenario.MachineOrLinePicker, "Hat, makine, feeder veya istasyon seçimi için ikon/kart ağırlıklı görünüm.", "AOI Support Desk, MasterData ve üretim araçlarında operatör veya teknisyen için hızlı seçim ekranı olarak kullanılabilir."));
        _scenarios.Add(new ScenarioInfo("06  İşlem Geçmişi", PanoScenario.Timeline, "Log, karar akışı, ticket mesajları ve operasyon geçmişi için zaman akışı.", "AOI false-call kararları, SAP sorgu adımları, server bağlantı logları veya kullanıcı aksiyonları için daha okunaklı bir geçmiş görünümü sağlar."));
        _scenarios.Add(new ScenarioInfo("07  Master-Detail", PanoScenario.MasterDetail, "Üst kayıt + alt detay mantığındaki bakım ekranları için profesyonel görünüm.", "MasterData ürün ağacı, Support Desk ticket mesajları veya makine bakım ekranlarında tek kayıt seçilip alt panelde uzun açıklama gösterilebilir."));
        _scenarios.Add(new ScenarioInfo("08  Yoğun Veri", PanoScenario.DenseData, "1M+ satır hedefli SQL/SAP/API listeleri için yoğun grid görünümü.", "Çok satır, az boşluk, hızlı filtre/sıralama ve kaydedilebilir kullanıcı layoutu hedeflenir."));
        _scenarios.Add(new ScenarioInfo("09  Filter Popup UX", PanoScenario.DenseData, "Uzun SAP malzeme adı ve program yolu filtrelerinde resize, tooltip, auto-width ve boyut hatırlama davranışı.", "MasterData, AOI Support Desk ve program dosyası seçim ekranlarında satıra sığmayan filtre değerlerini görmek için popup kenardan büyütülebilir; uzun değerlerde tooltip ve otomatik genişlik kullanılabilir."));
        _scenarios.Add(new ScenarioInfo("10  Advanced Filter & Preset", PanoScenario.DenseData, "Çok kolonlu AND/OR filtre oluşturucu, hızlı filtreler ve kaydedilebilir filtre presetleri.", "MasterData BOM, SAP pozisyonları, program yolu arama ve AOI ticket listelerinde kullanıcıların sık kullandığı filtreleri preset olarak saklaması için hazırlanmıştır. Sağ tık menüsünden Gelişmiş Filtre veya toolbar butonlarından hızlı preset akışı denenebilir."));
        _scenarios.Add(new ScenarioInfo("11  Menu & Icon Customization", PanoScenario.DenseData, "v27.5 menü grupları, item bazlı görünürlük, built-in menü kapatma/merge ve özel ikon kullanımını tek yerde gösterir.", "Designer tarafında MenuOptions ve MenuIcons genişletilebilir property olarak görünür. Runtime tarafında SetMenuGroupVisible, SetMenuItemVisible, SetCustomMenuIconFolder ve SetCustomMenuIconImageList ile proje bazlı menü/ikon kimliği verilebilir."));
        _scenarios.Add(new ScenarioInfo("12  Card Filter UX", PanoScenario.TicketBoard, "v27.7 kart, dashboard, kanban ve poster görünümlerinde header olmadan filtreye hızlı erişim.", "Üst hızlı filtre barı, floating filtre butonu ve aktif filtre chipleri büyük görünümlerde kullanıcıyı header menüsüne bağımlı bırakmaz. AOI ticket kartları, MasterData program kartları ve makine seçim ekranları için önerilir."));
        _scenarios.Add(new ScenarioInfo("13  Popular Enterprise Features", PanoScenario.DenseData, "v27.9 popüler enterprise grid davranışları: arama paneli, özet/footer, frozen column, conditional format, column chooser ve gelişmiş filtre presetleri.", "DevExpress/Telerik/Syncfusion tarzı gridlerde kullanıcıların en çok beklediği davranışları Pano'de tek preset altında toplar. MasterData, Support Desk, büyük veri inceleme ve veri giriş ekranları için hazır ayarlar denenebilir."));
        _scenarios.Add(new ScenarioInfo("14  v28 UX Polish", PanoScenario.TicketBoard, "Kart/poster/dashboard görünümlerinde global filtreyi üst bara taşıyan ve popüler grid davranışlarını tek preset altında toplayan final UX vitrini.", "DevExpress, Telerik ve Syncfusion gibi güçlü gridlerdeki arama/filtre/preset/kolon seçici/özet davranışları Pano'de hafif ve designer dostu property'ler olarak toparlanır. Bu senaryoda filtre butonu kart alanında değil, üst hızlı filtre barındadır."));
        _scenarios.Add(new ScenarioInfo("15  v28.1 Poster Mode", PanoScenario.TicketBoard, "ExtraLargeIcons yerine akılda kalıcı Poster görünüm modu, otomatik poster ölçüleri ve üst filtre UX'i.", "Poster görünümü büyük görsel kart senaryoları için tasarlandı. TilePosterMode altyapısını güvenli şekilde kullanır, ama kullanıcı tarafında PanoViewMode.Poster ismiyle seçilir. PosterPreferredWidth, PosterPreferredHeight, PosterImageHeight ve PosterModeAutoLayout property'leri designer/runtime tarafında ayarlanabilir."));
        _scenarios.Add(new ScenarioInfo("16  v31 Audix Media Temel Örnek", PanoScenario.ProgramFiles, "Albüm kapağı, medya rozeti, hover play overlay ve poster/gallery/filmstrip akışı.", "Audix için albüm kapağı gösterimi, FLAC/MP3 rozeti, placeholder ve media tile ayarları tek örnekte toplanır. Poster, Gallery, MediaTile ve FilmStrip modları bu senaryoda denenebilir."));
        _scenarios.Add(new ScenarioInfo("17  v32 Faz Merkezi / Nerede Bulurum", PanoScenario.DenseData, "Faz 38-48 özelliklerinin kısa açıklama, hedef proje ve görünüm modu ile tek listede bulunması.", "Example Center kalabalıklaştığı için bu merkez hangi özelliğin nerede kullanılacağını gösterir: UX Intelligence, Factory Intelligence, Timeline Engine, Document Explorer, Virtualization Pro, Search Everywhere, Command Palette, Layout Studio, Dashboard Builder, AI Layer ve Ecosystem."));
        _scenarios.Add(new ScenarioInfo("18  v33 Theme Lab / Okunurluk", PanoScenario.DenseData, "Koyu/açık tema, buton, combobox, bilgi metni, badge ve kart metinlerinin kontrast test merkezi.", "Theme Accessibility Engine; Fore/Back, Panel, Control, Border, Muted, Empty, Accent ve Selection renklerini normalize eder. Audix, AOI Support Desk, MasterData ve Line Workspace koyu/açık temalarında yazı kaybolmasını engellemek için hazırlanmıştır."));
        _scenarios.Add(new ScenarioInfo("19  v32 Factory Intelligence", PanoScenario.MachineOrLinePicker, "Makine durum overlay, üretim heatmap ve canlı dashboard hazırlığı.", "Line Workspace ve Factory Navigator için Running/Waiting/Fault/Offline durumları HeatMap/KPI kartlarına taşınır. SQL/SignalR/API canlı veri akışı için host tarafına hazır property ve helperlar eklendi."));
        _scenarios.Add(new ScenarioInfo("20  v32 Timeline Engine", PanoScenario.Timeline, "Ticket, makine ve işlem geçmişleri için daha güçlü zaman akışı.", "AOI Support Desk ticket geçmişi, makine alarm/start/stop kayıtları ve operatör-teknisyen mesajları Timeline modunda okunabilir hale getirilir."));
        _scenarios.Add(new ScenarioInfo("21  v32 Document Explorer", PanoScenario.ProgramFiles, "PDF, görsel, CAD, video ve klasör önizleme mantığı için Explorer tarzı galeri.", "Windows Explorer / medya arşivi mantığında dosya kartları, thumbnail, tür rozeti ve lazy-load senaryoları denenir. Audix ve teknik doküman arşivi için uygundur."));
        _scenarios.Add(new ScenarioInfo("22  v32 Virtualization Pro", PanoScenario.DenseData, "100K+ ve 1M+ satır hedefli yoğun veri ve tile virtualization hazırlığı.", "Çok büyük veri kaynaklarında DenseList, sanal provider ve hızlı arama davranışlarının nasıl konumlanacağı gösterilir."));
        _scenarios.Add(new ScenarioInfo("23  v32 Search Everywhere + Command Palette", PanoScenario.DenseData, "Ctrl+K / Ctrl+Shift+P tarzı arama ve komut merkezine hazır akış.", "Satır, kolon, filtre, görünüm ve export komutlarını tek merkezden bulmaya yönelik API ve örnek kullanım notları içerir."));
        _scenarios.Add(new ScenarioInfo("24  v32 Layout Studio", PanoScenario.MasterDetail, "Kullanıcı görünüm tasarlama, kaydetme ve yeniden yükleme akışı.", "Kolon, kart, dashboard ve filtre düzeninin host uygulama tarafından saklanabilmesi için SaveExperienceSnapshot ve ilgili propertyler gösterilir."));
        _scenarios.Add(new ScenarioInfo("25  v32 Dashboard Builder", PanoScenario.TicketBoard, "KPI, chart, table, card, heatmap, timeline ve gallery widget fikrini tek Pano içinde toplar.", "Power BI benzeri hafif dashboard mantığı için widget tanımları, KPI kartları ve heatmap görünümü örneklenir."));
        _scenarios.Add(new ScenarioInfo("26  v32 AI Layer + Ecosystem", PanoScenario.DenseData, "Host uygulamanın ürettiği akıllı öneri/uyarı kartlarını Pano ile birlikte gösterme hazırlığı.", "Eksik kapak, artan AOI hata oranı, PCB varyant benzerliği veya riskli makine durumları gibi öneriler PanoAiInsight listesinde taşınır."));
        _scenarios.Add(new ScenarioInfo("27  v34 Build Quality / Stability", PanoScenario.DenseData, "API çakışma kontrolü, compile-safe örnek merkezi ve geriye uyumluluk kalite taraması.", "Duplicate property/class risklerini azaltmak, eski projeleri kırmadan yeni fazları açmak ve Example Center içindeki senaryoları kalite raporu ile denetlemek için hazırlanmıştır."));
        _scenarios.Add(new ScenarioInfo("28  v35 Theme Studio", PanoScenario.DenseData, "Audix, AOI Support Desk, FactoryOS, SmokeWhite ve yüksek kontrast tema presetleri.", "Koyu/açık temada canlı önizleme, okunurluk normalizasyonu ve proje bazlı hazır tema paletleri tek merkezde denenir."));
        _scenarios.Add(new ScenarioInfo("29  v36 Media Pro / Cache & Overlay", PanoScenario.ProgramFiles, "Audix için albüm kapağı cache, placeholder, overlay, kalite rozeti ve media gruplama akışı.", "Poster, Gallery, MediaTile ve FilmStrip görünümlerinde albüm kapaklarını daha profesyonel göstermek için disk/bellek cache, eksik kapak davranışı ve seçili kart glow ayarları eklenmiştir."));
        _scenarios.Add(new ScenarioInfo("30  v37-v40 Pro Experience", PanoScenario.DenseData, "Enterprise Layout, Performance Pro, Interaction Pro ve Visual Analytics fazları tek merkezde.", "Layout kaydet/yükle, medya ve büyük veri performans profilleri, Command Palette/Search Everywhere, kısayollar, KPI/HeatMap/Timeline analiz akışları PanoV37ToV40ProExperienceSampleForm içinde canlı örneklenir."));
        _scenarios.Add(new ScenarioInfo("31  v50 Foundation / Modül Profilleri", PanoScenario.DenseData, "Pano 5.0 modül profilleri, runtime stability check ve güvenli varsayılanlar.", "Audix, AOI Support Desk, Factory Intelligence, MasterData ve Bilge Defter için hazır modül profilleri; tema erişilebilirliği, medya playback ve komut paleti kontrolleri tek merkezden doğrulanır."));
        _scenarios.Add(new ScenarioInfo("32  v50.1 Example Center Navigator", PanoScenario.DenseData, "Örnek merkezinde kaybolmamak için kategori + arama destekli bulucu ekranı.", "Example Center artık Media/Audix, Theme/Okunurluk, Stability/Build, Performance, Analytics/Dashboard, Factory/AOI, Layout/Interaction ve Timeline/Kanban kategorileriyle filtrelenebilir."));
        _scenarios.Add(new ScenarioInfo("33  v50.2 Runtime Hardening Kontrolleri", PanoScenario.DenseData, "Derleme/API guard, runtime checks, tema okunurluğu ve Audix medya güvenli varsayılanları.", "Yeni faz eklemek yerine mevcut fazları projelerde daha güvenilir kullanmak için hazırlanmıştır. Duplicate property/event riskleri, medya cache/playback ayarları, tema erişilebilirliği ve Example Center bulucu akışı tek merkezde doğrulanır."));
        _scenarios.Add(new ScenarioInfo("34  Pano 5.1 Audix Pilot / Theme Audit", PanoScenario.ProgramFiles, "Audix gerçek kullanım pilotu: albüm kapağı, playback state, video preview, tema audit ve Example Center cleanup.", "Pano 5.1 artık yeni özellik ekleme yerine gerçek projeye tak-çalıştır kalitesine odaklanır. Audix tarafında Poster/MediaTile/Gallery/FilmStrip görünümleri, kapak cache, placeholder, play/pause state, now-playing rozeti ve video preview davranışı birlikte denenir. Theme Audit koyu/açık/yüksek kontrast okunurluğunu korur; Example Center cleanup ise özellikleri kategori ve arama ile bulunabilir hale getirir."));
        _scenarios.Add(new ScenarioInfo("35  Kolon Designer Editor Smoke Test", PanoScenario.DenseData, "Son kolon editor düzeltmesini test eder: gerçek PanoColumnCollectionEditor, Tamam/İptal, ekle/sil ve property korunması.", "Sample Hub içindeki 'Kolon Designer Editor Smoke Test' formu gerçek Pano kolon editor penceresini açar. Audix benzeri Image/Card/Tooltip/SearchAlias propertyleri, PrivateColumn, Visible, Width ve Name/AspectName değerleri editor turundan sonra raporda karşılaştırılabilir. Arama: designer, kolon, editor, Audix column."));
        _scenarios.Add(new ScenarioInfo("36  Runtime View Menu Modern", PanoScenario.ProgramFiles, "Sağ tık menüsündeki modern görünüm grupları, medya presetleri, dashboard editor ve performance özetini test eder.", "Pano runtime menüsü artık Akıllı presetler, Media Smart Preset, Dashboard preset editor, View Mode Memory ve Performance summary içerir. Sağ tık > Görünüm içinden Müzik/Film/Fotoğraf/Doküman presetleri, KPI/HeatMap/MiniChart toggle, medya cache/lazy image profili ve aktif görünüm bilgisi denenebilir. Arama: menü, view mode, dashboard, audix, media, filmstrip, performance."));
        _scenarios.Add(new ScenarioInfo("37  Pano Diagnostics Center", PanoScenario.DenseData, "Build quality, runtime hardening, media, dashboard, view mode memory ve paint performansını tek raporda toplar.", "Pano Diagnostics Center; v34 Build Quality, v50 Foundation, v50.2 Hardening, v51 Real Usage, Media Smart Preset, Dashboard Preset Editor, View Mode Memory ve Performance pass kontrollerini tek metin raporda birleştirir. Sağ tık > Görünüm > Dashboard preset editor > Diagnostics Center ile pencere açılabilir; rapor kopyalanabilir ve yenilenebilir. Arama: diagnostics, kalite, performance, dashboard, media, view mode."));
    }

    private void ApplySelectedScenario()
    {
        if (_scenarioList.SelectedItem is not ScenarioInfo scenario) return;
        _grid.ClearGrouping();
        LoadRows(scenario);
        _grid.ApplyScenario(scenario.Scenario, updateActiveScenario: true);

        if (scenario.Title.Contains("v28 UX Polish", StringComparison.OrdinalIgnoreCase) || scenario.Title.Contains("v28.1 Poster Mode", StringComparison.OrdinalIgnoreCase))
        {
            _grid.SetViewMode(PanoViewMode.Poster);
            _grid.PosterPreferredWidth = 220;
            _grid.PosterPreferredHeight = 300;
            _grid.PosterImageHeight = 176;
            _grid.ApplyV28UxPolish(PanoUxPolishPreset.PosterGallery);
        }
        else if (scenario.Title.Contains("Popular Enterprise", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyPopularFeaturePack(PanoPopularFeaturePreset.MasterData);
            _grid.AddSemanticStatusConditionalFormat(nameof(ProRow.Status));
            _grid.AddCountSummary(nameof(ProRow.Code));
            _grid.AddNumericSummary(nameof(ProRow.Progress), Taylan.Pano.Summary.PanoSummaryType.Average, "Ort. %{0:0}");
        }
        else if (scenario.Title.Contains("Card Filter UX", StringComparison.OrdinalIgnoreCase))
        {
            _grid.SetViewMode(PanoViewMode.DashboardCard);
            _grid.ShowQuickFilterBar = true;
            _grid.ShowFloatingFilterButton = true;
            _grid.ShowActiveFilterChips = true;
            _grid.CardFilterUxOnlyInCardViews = true;
        }
        else if (scenario.Title.Contains("Kanban", StringComparison.OrdinalIgnoreCase))
        {
            _grid.SetViewMode(PanoViewMode.Kanban);
            _grid.SetGroupBy(nameof(ProRow.Status));
        }
        else if (scenario.Scenario == PanoScenario.MachineOrLinePicker)
        {
            _grid.SetGroupBy(nameof(ProRow.Type));
        }
        else if (scenario.Scenario == PanoScenario.Timeline)
        {
            _grid.SetGroupBy(nameof(ProRow.Machine));
        }

        if (scenario.Title.Contains("v31 Audix", StringComparison.OrdinalIgnoreCase))
        {
            _grid.SetViewMode(PanoViewMode.Poster);
            _grid.TilePosterMode = true;
            _grid.ShowMediaOverlayButton = true;
            _grid.ShowMediaQualityBadge = true;
            _grid.MediaQualityBadgeAspectName = nameof(ProRow.Status);
            _grid.TilePreferredWidth = 220;
            _grid.TilePreferredHeight = 300;
            _grid.TilePosterImageHeight = 172;
        }
        else if (scenario.Title.Contains("Faz Merkezi", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV32UltimateExperiencePack();
            _grid.SetViewMode(PanoViewMode.RowPreview);
        }
        else if (scenario.Title.Contains("Theme Lab", StringComparison.OrdinalIgnoreCase))
        {
            _grid.EnforceThemeAccessibility = true;
            _grid.AutoEnsureReadableTextColors = true;
            _grid.SetViewMode(PanoViewMode.DashboardCard);
            _grid.ShowQuickFilterBar = true;
            _grid.ShowActiveFilterChips = true;
            _grid.CardFilterUxOnlyInCardViews = false;
            _grid.ApplyTheme(PanoThemeAccessibility.Normalize(PanoTheme.FromParentColor(BackColor, ForeColor)));
        }
        else if (scenario.Title.Contains("Factory Intelligence", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV32ExperiencePhase(PanoExperiencePhase.FactoryIntelligence);
            _grid.FactoryStatusAspectName = nameof(ProRow.Status);
        }
        else if (scenario.Title.Contains("Timeline Engine", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV32ExperiencePhase(PanoExperiencePhase.TimelineEngine);
        }
        else if (scenario.Title.Contains("Document Explorer", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV32ExperiencePhase(PanoExperiencePhase.DocumentExplorer);
            _grid.ShowMediaQualityBadge = true;
            _grid.MediaQualityBadgeAspectName = nameof(ProRow.Type);
        }
        else if (scenario.Title.Contains("Virtualization Pro", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV32ExperiencePhase(PanoExperiencePhase.VirtualizationPro);
        }
        else if (scenario.Title.Contains("Search Everywhere", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV32ExperiencePhase(PanoExperiencePhase.SearchEverywhere);
            _grid.ApplyV32ExperiencePhase(PanoExperiencePhase.CommandPalette);
        }
        else if (scenario.Title.Contains("Layout Studio", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV32ExperiencePhase(PanoExperiencePhase.LayoutStudio);
            _grid.SetViewMode(PanoViewMode.MasterDetail);
        }
        else if (scenario.Title.Contains("Dashboard Builder", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV32ExperiencePhase(PanoExperiencePhase.DashboardBuilder);
        }
        else if (scenario.Title.Contains("AI Layer", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV32ExperiencePhase(PanoExperiencePhase.AiLayer);
            _grid.AddAiInsight("Kapak eksikleri", "Audix arşivinde kapak görseli olmayan albümler rozetlenebilir.", "Warning", "Kapak bulma modülünü çalıştır");
            _grid.AddAiInsight("AOI hata artışı", "Son vardiyada aynı makinede benzer hata artışı tespit edilebilir.", "Info", "Timeline ve HeatMap görünümünü aç");
        }
        else if (scenario.Title.Contains("Build Quality", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV34BuildQualityPack();
            _grid.SetViewMode(PanoViewMode.RowPreview);
        }
        else if (scenario.Title.Contains("Theme Studio", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV35ThemeStudioPack(PanoThemeStudioPreset.FactoryOsDark);
            _grid.SetViewMode(PanoViewMode.KpiDashboard);
        }
        else if (scenario.Title.Contains("v37-v40 Pro Experience", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV37EnterpriseLayoutPack();
            _grid.ApplyV38PerformanceProfile(PanoV38PerformancePreset.Balanced);
            _grid.ApplyV39InteractionProfile(PanoV39InteractionPreset.PowerUser);
            _grid.ApplyV40AnalyticsProfile(PanoV40AnalyticsPreset.KpiDashboard);
            _grid.SetViewMode(PanoViewMode.KpiDashboard);
        }
        else if (scenario.Title.Contains("Pano 5.1 Audix", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyAudix51MediaPilotDefaults();
            _grid.MediaQualityBadgeAspectName = nameof(ProRow.Status);
            _grid.SetViewMode(PanoViewMode.Poster);
            _grid.TilePreferredWidth = 235;
            _grid.TilePreferredHeight = 318;
            _grid.TilePosterImageHeight = 184;
        }
        else if (scenario.Title.Contains("v50.2", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyPano502HardeningDefaults();
            _grid.SetViewMode(PanoViewMode.RowPreview);
        }
        else if (scenario.Title.Contains("v50 Foundation", StringComparison.OrdinalIgnoreCase) || scenario.Title.Contains("v50.1 Example Center", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyPano5FoundationDefaults();
            _grid.SetViewMode(PanoViewMode.RowPreview);
        }
        else if (scenario.Title.Contains("Media Pro", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyV36MediaProPack();
            _grid.SetViewMode(PanoViewMode.Poster);
            _grid.MediaQualityBadgeAspectName = nameof(ProRow.Status);
            _grid.TilePreferredWidth = 220;
            _grid.TilePreferredHeight = 300;
            _grid.TilePosterImageHeight = 172;
        }
        else if (scenario.Title.Contains("Runtime View Menu Modern", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyMediaSmartPreset(PanoMediaSmartPreset.Music);
            _grid.MediaQualityBadgeAspectName = nameof(ProRow.Type);
            _grid.SetViewMode(PanoViewMode.MediaTile);
            _grid.ShowHeaderMenuViewModeItems = true;
            _grid.ShowScenarioMenuItems = true;
            _grid.ShowStateMenuItems = true;
            _grid.RememberViewModePerScenario = true;
            _grid.EnablePaintPerformanceMetrics = true;
            _grid.EnsureDefaultDashboardWidgets();
            _grid.HeaderContextMenuBehavior = PanoHeaderContextMenuBehavior.Full;
        }
        else if (scenario.Title.Contains("Diagnostics Center", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyPano51RealUsageDefaults();
            _grid.ApplyMediaSmartPreset(PanoMediaSmartPreset.Music);
            _grid.MediaQualityBadgeAspectName = nameof(ProRow.Status);
            _grid.EnablePanoDiagnosticsCenter = true;
            _grid.EnablePaintPerformanceMetrics = true;
            _grid.RememberViewModePerScenario = true;
            _grid.EnsureDefaultDashboardWidgets();
            _grid.SetDashboardWidgetEnabled(PanoDashboardWidgetKind.Kpi, true);
            _grid.SetDashboardWidgetEnabled(PanoDashboardWidgetKind.HeatMap, true);
            _grid.SetDashboardWidgetEnabled(PanoDashboardWidgetKind.Chart, true);
            _grid.SetViewMode(PanoViewMode.RowPreview);
            _grid.HeaderContextMenuBehavior = PanoHeaderContextMenuBehavior.Full;
        }

        _grid.SetObjects(_rows);
        _description.Text = $"{scenario.Category}  |  {scenario.Description}";
        _details.Text = BuildUsageNotes(scenario);
    }

    private string BuildUsageNotes(ScenarioInfo scenario)
    {
        var doc = BuildScenarioDoc(scenario);
        var sb = new StringBuilder();
        sb.AppendLine(scenario.Title);
        sb.AppendLine(new string('=', scenario.Title.Length));
        sb.AppendLine();
        sb.AppendLine("Kategori");
        sb.AppendLine(doc.Category);
        sb.AppendLine();
        sb.AppendLine("Kapsam");
        sb.AppendLine(scenario.Scope);
        sb.AppendLine();
        sb.AppendLine("Amaç");
        sb.AppendLine(scenario.LongDescription);
        sb.AppendLine();
        sb.AppendLine("Ne gösterir?");
        sb.AppendLine(doc.WhatItShows);
        sb.AppendLine();
        sb.AppendLine("Nasıl test edilir?");
        sb.AppendLine(doc.HowToTest);
        sb.AppendLine();
        sb.AppendLine("Beklenen sonuç");
        sb.AppendLine(doc.ExpectedResult);
        sb.AppendLine();
        sb.AppendLine("İlgili API / Property");
        sb.AppendLine(doc.Api);
        sb.AppendLine();
        sb.AppendLine("Arama kelimeleri");
        sb.AppendLine(doc.Tags);
        return sb.ToString();
    }

    private static ScenarioDoc BuildScenarioDoc(ScenarioInfo scenario)
    {
        string title = scenario.Title;

        if (title.Contains("Ticket Dashboard", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Ticket durumlarını kart/dashboard dilinde hızlı okumayı anlatır.", "Satırları durum veya makineye göre grupla; sağ tık menüsünden kart ve dashboard görünümleri arasında geç.", "Ticket tipi, durum rozeti ve ilerleme bilgisi aynı kartta okunur.", "ApplyScenario(TicketBoard), PanoViewMode.DashboardCard, Badge, ProgressBar", "ticket, dashboard, card, badge, support desk");

        if (title.Contains("Kanban", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "İş akışını durum gruplarıyla kanban mantığına yaklaştırır.", "Duruma göre gruplamayı ve Kanban görünümünü dene.", "Open/In Progress/Waiting/Done akışı görsel olarak ayrılır.", "PanoViewMode.Kanban, SetGroupBy(Status)", "kanban, workflow, ticket, status");

        if (title.Contains("MasterData BOM", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Yoğun teknik veride klasik tablo verimliliğini gösterir.", "Filtre, sıralama, kolon genişliği ve State Kaydet/Yükle akışını dene.", "BOM/SAP satırları dar alanda okunur ve kullanıcı düzeni korunur.", "PanoScenario.BomPositions, DenseData, FilterMenuMode", "bom, sap, dense, table, masterdata");

        if (title.Contains("Program Dosyaları", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Dosya/program seçimi için kart odaklı görünümü anlatır.", "Kart, liste ve sağ tık görünüm modları arasında geç.", "Program klasörleri tablo yerine daha seçilebilir kartlar halinde görünür.", "PanoScenario.ProgramFiles, Tile, RowCard", "program, file, folder, machine");

        if (title.Contains("Makine / Hat", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Operatörün makine/hat/istasyon seçmesini kolaylaştıran picker deneyimi.", "Tip veya durum grubunu aç; kart ve ikon görünümlerini dene.", "Makine, hat ve istasyonlar hızlı seçilecek şekilde ayrışır.", "MachineOrLinePicker, GroupBy(Type), IconGrid", "machine, line, picker, factory");

        if (title.Contains("İşlem Geçmişi", StringComparison.OrdinalIgnoreCase) || title.Contains("Timeline Engine", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Log, ticket mesajı ve makine olaylarını zaman akışı gibi okumayı anlatır.", "Timeline görünümüne geç ve makineye göre gruplamayı dene.", "Olay kayıtları klasik tablodan daha kronolojik ve taranabilir görünür.", "PanoViewMode.Timeline, PanoExperiencePhase.TimelineEngine", "timeline, log, history, event");

        if (title.Contains("Master-Detail", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Üst kayıt ve detay açıklama düzenini gösterir.", "Bir satır seç; MasterDetail ve DetailCard görünümlerini karşılaştır.", "Seçilen kayıt ana bilgileriyle birlikte uzun detayını kaybetmeden okunur.", "PanoViewMode.MasterDetail, DetailCard", "master detail, property, detail");

        if (title.Contains("Yoğun Veri", StringComparison.OrdinalIgnoreCase) || title.Contains("Virtualization", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Büyük listelerde yoğun görünüm, filtre ve performans yaklaşımını anlatır.", "DenseList/Details arasında geç; arama ve filtreyi yoğun satırda dene.", "Çok satırlı veri az boşlukla, hızlı taranabilir şekilde görünür.", "PanoViewMode.DenseList, V38PerformancePreset, ViewCount", "performance, dense, virtual, big data");

        if (title.Contains("Filter Popup", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Uzun filtre değerlerinde popup boyutu, tooltip ve okunurluğu anlatır.", "Kolon başlığındaki filtre ikonunu aç; popup kenarından büyüt ve uzun değerlere bak.", "Uzun SAP/program yolu değerleri kırpılmadan incelenebilir.", "FilterPopupResizable, FilterPopupAutoWidthForLongValues", "filter popup, tooltip, resize");

        if (title.Contains("Advanced Filter", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "AND/OR koşullu gelişmiş filtre ve filtre preset akışını gösterir.", "Toolbar Gelişmiş Filtre, Filtre Preset Kaydet/Yükle butonlarını dene.", "Karmaşık filtreler tekrar kullanılabilir preset olarak saklanır.", "ShowAdvancedFilterBuilder, SaveCurrentFilterPreset, LoadFilterPreset", "advanced filter, preset, query");

        if (title.Contains("Menu", StringComparison.OrdinalIgnoreCase) || title.Contains("Runtime View Menu", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Runtime sağ tık menüsünü, ikonları, görünüm gruplarını ve hazır presetleri anlatır.", "Sağ tık > Görünüm menüsünden Media Smart Preset, Dashboard preset editor ve View Mode Memory seçeneklerini dene.", "Menü daha modern, gruplu ve aktif görünümü açıklayan bir kontrol merkezine dönüşür.", "CreateViewModeMenu, MediaSmartPreset, SetDashboardWidgetEnabled, RememberViewModePerScenario", "menu, context, view mode, preset");

        if (title.Contains("Card Filter", StringComparison.OrdinalIgnoreCase) || title.Contains("UX Polish", StringComparison.OrdinalIgnoreCase) || title.Contains("Popular Enterprise", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Kart/poster/dashboard gibi header dışı görünümlerde filtre ve enterprise grid alışkanlıklarını toparlar.", "Üst hızlı filtre barı, aktif chipler, kolon seçici, özet/footer ve görünüm modlarını dene.", "Kart ekranında bile filtreleme görünür ve kullanıcı header menüsüne mahkum kalmaz.", "ShowQuickFilterBar, ShowActiveFilterChips, ApplyPopularFeaturePack", "ux, filter, enterprise, quick filter");

        if (title.Contains("Poster Mode", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Görsel ağırlıklı poster kart ölçülerini ve medya kart davranışını gösterir.", "Poster görünümüne geç; poster genişlik/yükseklik ve image height etkisini izle.", "ExtraLargeIcons yerine daha anlaşılır Poster modu kullanılır.", "PanoViewMode.Poster, PosterPreferredWidth, PosterImageHeight", "poster, image, card");

        if (title.Contains("Audix", StringComparison.OrdinalIgnoreCase) || title.Contains("Media Pro", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Müzik/film/fotoğraf/doküman kapakları, playback state ve medya cache yaklaşımını anlatır.", "Poster, Gallery, MediaTile ve FilmStrip arasında geç; kalite rozeti ve overlay davranışını izle.", "Kapak görseli, play/pause state, kalite rozeti ve lazy/cache ayarları birlikte çalışır.", "ApplyAudix51MediaPilotDefaults, ApplyMediaSmartPreset, EnableMediaImageCache, EnableMediaLazyLoading", "audix, media, cover, playback, cache");

        if (title.Contains("Faz Merkezi", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Pano fazlarının hangi projede ne işe yaradığını bir indeks gibi açıklar.", "Satırları faz, hedef proje ve durum alanlarına göre filtrele.", "Özelliklerin isimleri değil, kullanım alanları anlaşılır hale gelir.", "ApplyV32UltimateExperiencePack, PanoExperiencePhase", "roadmap, phase, feature index");

        if (title.Contains("Theme", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Koyu/açık/yüksek kontrast tema okunurluğunu ve hazır paletleri gösterir.", "Açık Tema/Koyu Tema butonlarını dene; yazı, kart, badge ve buton kontrastını gözle.", "Metinler koyu ve açık temada kaybolmadan okunur.", "PanoThemeAccessibility, ApplyV35ThemeStudioPack, EnforceThemeAccessibility", "theme, contrast, readability");

        if (title.Contains("Factory Intelligence", StringComparison.OrdinalIgnoreCase) || title.Contains("Dashboard Builder", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "KPI, HeatMap, MiniChart ve fabrika durum görünümlerinin temelini anlatır.", "KPI/HeatMap/MiniChart görünümlerine geç; sağ tık Dashboard preset editor seçeneklerini dene.", "Üretim/makine durumu dashboard bileşenlerine ayrılır.", "EnableDashboardBuilder, DashboardWidgets, PanoViewMode.KpiDashboard, HeatMap, MiniChart", "dashboard, kpi, heatmap, factory");

        if (title.Contains("Document Explorer", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "PDF, görsel, video, klasör ve teknik doküman önizleme fikrini anlatır.", "Gallery/Poster görünümlerine geç; tür rozetleri ve thumbnail davranışını incele.", "Teknik dokümanlar dosya listesi yerine görsel katalog gibi sunulur.", "PanoExperiencePhase.DocumentExplorer, PanoViewMode.Gallery", "document, explorer, pdf, preview");

        if (title.Contains("Search Everywhere", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Satır, kolon, filtre ve komutları tek arama/komut yüzeyinde toplama fikrini anlatır.", "Arama kutusu, sağ tık menüsü ve command palette notlarını birlikte incele.", "Kullanıcı istediği komut veya veriye menü ezberlemeden ulaşır.", "EnableSearchEverywhere, EnableCommandPalette", "search, command palette, ctrl k");

        if (title.Contains("Layout Studio", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Kullanıcı görünüm düzenini saklama ve geri yükleme yaklaşımını anlatır.", "State/Preset Kaydet-Yükle butonlarıyla görünüm düzenini değiştirip geri çağır.", "Kolon, filtre, görünüm ve layout tercihleri host uygulamaya taşınır.", "EnableLayoutStudio, SaveStateToDefaultPath, SaveStatePreset", "layout, state, preset");

        if (title.Contains("AI Layer", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Host uygulamanın ürettiği öneri/uyarıları Pano içinde göstermeye hazırlık yapar.", "AI insight satırlarını filtrele; dashboard/timeline ile nasıl birleşebileceğini incele.", "Pano karar vermez, host uygulamanın ürettiği sinyalleri okunur hale getirir.", "AddAiInsight, PanoAiInsight", "ai, insight, suggestion");

        if (title.Contains("Build Quality", StringComparison.OrdinalIgnoreCase) || title.Contains("Hardening", StringComparison.OrdinalIgnoreCase) || title.Contains("Foundation", StringComparison.OrdinalIgnoreCase) || title.Contains("Diagnostics", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "API, runtime, tema, medya ve dashboard stabilitesini kontrol eden güvenlik/kalite yüzeyini anlatır.", "Sağ tık > Görünüm > Dashboard preset editor > Diagnostics Center aç; raporu yenile ve kopyala.", "Riskli ayarlar uyarı olarak görünür, aktif guard ve presetler doğrulanır.", "RunBuildQualityDiagnostics, RunPano502RuntimeHardeningChecks, RunPanoDiagnosticsCenterText", "diagnostics, quality, stability, hardening");

        if (title.Contains("Kolon Designer", StringComparison.OrdinalIgnoreCase))
            return ScenarioDoc.Create(scenario, "Visual Studio designer tarafındaki Pano kolon editörünün stabil çalışmasını test eder.", "Sample Hub içindeki Kolon Designer Editor Smoke Test formunu aç; ekle/sil/tamam/iptal davranışını dene.", "Pano kendi kolon editörünü açar, property değerleri korunur ve gereksiz field uyarıları azalır.", "PanoColumnCollectionEditor, PanoControlDesigner, GLVColumn", "designer, column, editor, visual studio");

        return ScenarioDoc.Create(scenario, "Bu örnek seçilen Pano senaryosunun temel kullanımını açıklar.", "Sağ tık menüsü, toolbar butonları ve görünüm modlarını deneyerek davranışı karşılaştır.", "Seçilen senaryonun görünüm, filtre, state ve data sunumu netleşir.", "ApplyScenario, SetViewMode, PanoScenario", "pano, example, scenario");
    }

    private void LoadRows(ScenarioInfo scenario)
    {
        _rows.Clear();
        if (scenario.Title.Contains("v28 UX Polish", StringComparison.OrdinalIgnoreCase) || scenario.Title.Contains("v28.1 Poster Mode", StringComparison.OrdinalIgnoreCase))
        {
            AddFilterPopupUxRows();
            _grid.SetViewMode(PanoViewMode.Poster);
            _grid.PosterPreferredWidth = 220;
            _grid.PosterPreferredHeight = 300;
            _grid.PosterImageHeight = 176;
            _grid.ApplyV28UxPolish(PanoUxPolishPreset.PosterGallery);
        }
        else if (scenario.Title.Contains("Popular Enterprise", StringComparison.OrdinalIgnoreCase))
        {
            _grid.ApplyPopularFeaturePack(PanoPopularFeaturePreset.MasterData);
            _grid.AddSemanticStatusConditionalFormat(nameof(ProRow.Status));
            _grid.AddCountSummary(nameof(ProRow.Code));
            _grid.AddNumericSummary(nameof(ProRow.Progress), Taylan.Pano.Summary.PanoSummaryType.Average, "Ort. %{0:0}");
        }
        else if (scenario.Title.Contains("Card Filter UX", StringComparison.OrdinalIgnoreCase))
        {
            AddTicketRows();
            _grid.SetViewMode(PanoViewMode.DashboardCard);
            _grid.ShowQuickFilterBar = true;
            _grid.ShowFloatingFilterButton = true;
            _grid.ShowActiveFilterChips = true;
            _grid.CardFilterUxOnlyInCardViews = true;
        }
        else if (scenario.Title.Contains("Menu", StringComparison.OrdinalIgnoreCase))
        {
            AddFilterPopupUxRows();
            ApplyMenuCustomizationDemo();
        }
        else if (scenario.Title.Contains("v31 Audix", StringComparison.OrdinalIgnoreCase))
        {
            AddAudixMediaRows();
        }
        else if (scenario.Title.Contains("Faz Merkezi", StringComparison.OrdinalIgnoreCase))
        {
            AddV32PhaseRows();
        }
        else if (scenario.Title.Contains("Theme Lab", StringComparison.OrdinalIgnoreCase))
        {
            AddThemeAccessibilityRows();
        }
        else if (scenario.Title.Contains("Factory Intelligence", StringComparison.OrdinalIgnoreCase))
        {
            AddFactoryIntelligenceRows();
        }
        else if (scenario.Title.Contains("Timeline Engine", StringComparison.OrdinalIgnoreCase))
        {
            AddTimelineRows();
        }
        else if (scenario.Title.Contains("Document Explorer", StringComparison.OrdinalIgnoreCase))
        {
            AddDocumentExplorerRows();
        }
        else if (scenario.Title.Contains("Virtualization Pro", StringComparison.OrdinalIgnoreCase))
        {
            AddVirtualizationRows();
        }
        else if (scenario.Title.Contains("Search Everywhere", StringComparison.OrdinalIgnoreCase) || scenario.Title.Contains("Layout Studio", StringComparison.OrdinalIgnoreCase) || scenario.Title.Contains("AI Layer", StringComparison.OrdinalIgnoreCase))
        {
            AddV32PhaseRows();
        }
        else if (scenario.Title.Contains("Dashboard Builder", StringComparison.OrdinalIgnoreCase))
        {
            AddDashboardBuilderRows();
        }
        else if (scenario.Title.Contains("Build Quality", StringComparison.OrdinalIgnoreCase))
        {
            AddV34QualityRows();
        }
        else if (scenario.Title.Contains("Theme Studio", StringComparison.OrdinalIgnoreCase))
        {
            AddV35ThemeStudioRows();
        }
        else if (scenario.Title.Contains("Media Pro", StringComparison.OrdinalIgnoreCase))
        {
            AddV36MediaProRows();
        }
        else if (scenario.Title.Contains("Diagnostics Center", StringComparison.OrdinalIgnoreCase))
        {
            AddDiagnosticsCenterRows();
        }
        else if (scenario.Title.Contains("v50.2", StringComparison.OrdinalIgnoreCase))
        {
            AddV502HardeningRows();
        }
        else if (scenario.Title.Contains("v50 Foundation", StringComparison.OrdinalIgnoreCase) || scenario.Title.Contains("v50.1 Example Center", StringComparison.OrdinalIgnoreCase))
        {
            AddV50FoundationRows();
        }
        else if (scenario.Title.Contains("Ticket", StringComparison.OrdinalIgnoreCase) || scenario.Title.Contains("Kanban", StringComparison.OrdinalIgnoreCase))
        {
            AddTicketRows();
        }
        else if (scenario.Title.Contains("Filter Popup", StringComparison.OrdinalIgnoreCase) || scenario.Title.Contains("Advanced Filter", StringComparison.OrdinalIgnoreCase))
        {
            AddFilterPopupUxRows();
        }
        else if (scenario.Scenario == PanoScenario.BomPositions || scenario.Scenario == PanoScenario.MasterDetail || scenario.Scenario == PanoScenario.DenseData)
        {
            AddBomRows();
        }
        else if (scenario.Scenario == PanoScenario.ProgramFiles)
        {
            AddProgramRows();
        }
        else if (scenario.Scenario == PanoScenario.MachineOrLinePicker)
        {
            AddMachineRows();
        }
        else if (scenario.Scenario == PanoScenario.Timeline)
        {
            AddTimelineRows();
        }
    }

    private void ApplyMenuCustomizationDemo()
    {
        _grid.MenuIconMode = PanoMenuIconMode.BuiltInThenCustom;
        _grid.MenuIconSize = PanoMenuIconSize.Medium20;
        _grid.MenuProfile = PanoMenuProfile.Custom;
        _grid.MenuOptions.HeaderGroups = PanoMenuGroups.Filter | PanoMenuGroups.Sort | PanoMenuGroups.ColumnChooser | PanoMenuGroups.ViewMode | PanoMenuGroups.State | PanoMenuGroups.Scenario;
        _grid.MenuOptions.BodyGroups = PanoMenuGroups.Clipboard | PanoMenuGroups.Filter | PanoMenuGroups.ViewMode | PanoMenuGroups.Theme;
        _grid.MenuOptions.MergedGroups = PanoMenuGroups.Filter | PanoMenuGroups.Sort | PanoMenuGroups.ColumnChooser | PanoMenuGroups.ViewMode;
        _grid.VisibleMenuItems = PanoMenuItemKeys.All;
        _grid.HiddenMenuItems = PanoMenuItemKeys.None;
    }

    private void AddTicketRows()
    {
        string[] statuses = { "Open", "In Progress", "Waiting", "Done" };
        string[] machines = { "AOI-QX150", "AOI-FLEX", "LINE-03", "REWORK-01" };
        for (int i = 1; i <= 36; i++)
        {
            _rows.Add(new ProRow
            {
                Id = i,
                Selected = i % 5 == 0,
                Type = i % 3 == 0 ? "AOI" : "Ticket",
                Code = "TCK-" + i.ToString("0000"),
                Title = i % 4 == 0 ? "AOI durdu, program ve pozisyon kontrolü gerekiyor" : "False call / operatör destek talebi",
                Machine = machines[i % machines.Length],
                RefDes = i % 2 == 0 ? "TOP" : "BOT",
                Progress = Math.Min(100, 12 + i * 3),
                Status = statuses[i % statuses.Length],
                Detail = "Operatör mesajı, oto kod, makine ve son karar bilgileri aynı kartta özetlenir. Yeni mesaj geldiğinde satır/kart vurgusu yapılabilir."
            });
        }
    }

    private void AddBomRows()
    {
        string[] types = { "PCB", "BOM", "SAP", "Pozisyon" };
        string[] statuses = { "OK", "Eksik", "Kontrol", "Alternatif" };
        for (int i = 1; i <= 80; i++)
        {
            _rows.Add(new ProRow
            {
                Id = 1000 + i,
                Selected = i % 13 == 0,
                Type = types[i % types.Length],
                Code = i % 7 == 0 ? "17MB190FRR1_24022836" : "MAT-" + (240000 + i),
                Title = i % 5 == 0 ? "Stoklaşmış yarımamül / SAP BOM kırılımı" : "Komponent pozisyon ve ürün ağacı satırı",
                Machine = i % 2 == 0 ? "Flex" : "QX150i",
                RefDes = "R" + i + ", C" + (i + 12),
                Progress = 40 + (i % 60),
                Status = statuses[i % statuses.Length],
                Detail = "MasterData ekranlarında SAP, ürün ağacı, BOM, RefDes, katman ve program eşleşmesi aynı Pano senaryosu ile gösterilebilir."
            });
        }
    }


    private void AddFilterPopupUxRows()
    {
        string[] machines = { "Flex", "FlexUltra", "QX150i", "QX250i", "Axial", "Radial", "THT" };
        string[] statuses = { "OK", "Warning", "Eksik", "Hazır", "Waiting" };
        for (int i = 1; i <= 80; i++)
        {
            string machine = machines[i % machines.Length];
            _rows.Add(new ProRow
            {
                Id = i,
                Type = "Filter",
                Code = "17MB" + (190 + i % 40) + "FRR1-" + (24000000 + i).ToString(),
                Title = "Uzun SAP malzeme adı / program yolu filtre popup testi " + i.ToString("000"),
                Machine = machine,
                RefDes = (i % 2 == 0 ? "R" : "C") + i.ToString("000"),
                Progress = Math.Min(100, 15 + (i * 8) % 95),
                Status = statuses[i % statuses.Length],
                Detail = @"\\cyberserver\D\" + machine + @"\CIMBWork\1902\17MB190FRR1_24022836\Top\M-AX5-20CPR-Aser-" + (i % 12).ToString() + " / çok uzun filtre değeri örneği - MasterData SAP BOM pozisyon açıklaması ve program klasörü"
            });
        }
    }

    private void AddProgramRows()
    {
        string[] machines = { "Flex", "FlexUltra", "QX100", "QX150i", "Axial", "Radial" };
        for (int i = 1; i <= 30; i++)
        {
            _rows.Add(new ProRow
            {
                Id = 2000 + i,
                Type = "Program",
                Code = "PRG-" + i.ToString("000"),
                Title = machines[i % machines.Length] + " dizgi programı / klasör eşleşmesi",
                Machine = machines[i % machines.Length],
                RefDes = i % 2 == 0 ? "Top" : "Bottom",
                Progress = 20 + i % 80,
                Status = i % 4 == 0 ? "Eksik" : "Hazır",
                Detail = "Program dosyaları kart görünümünde daha rahat seçilir; uzun path tooltip ve geniş kart ile okunabilir."
            });
        }
    }

    private void AddMachineRows()
    {
        string[] lines = { "LINE-01", "LINE-02", "LINE-03", "REWORK", "AOI" };
        for (int i = 1; i <= 25; i++)
        {
            _rows.Add(new ProRow
            {
                Id = 3000 + i,
                Type = i % 3 == 0 ? "AOI" : "Makine",
                Code = "HOST-" + i,
                Title = "Makine / hat seçim kartı",
                Machine = lines[i % lines.Length],
                RefDes = "Sort " + i,
                Progress = i % 2 == 0 ? 100 : 65,
                Status = i % 5 == 0 ? "Offline" : "Online",
                Detail = "Support Desk makine yönetimi, teknisyen seçimi ve üretim hattı filtreleri için ikon grid kullanımı."
            });
        }
    }

    private void AddTimelineRows()
    {
        string[] statuses = { "Bilgi", "Uyarı", "Karar", "Çözüm" };
        for (int i = 1; i <= 28; i++)
        {
            _rows.Add(new ProRow
            {
                Id = 4000 + i,
                Type = "Log",
                Code = DateTime.Today.AddMinutes(i * 11).ToString("HH:mm:ss"),
                Title = "İşlem geçmişi adımı " + i,
                Machine = i % 2 == 0 ? "Server" : "Client",
                RefDes = "Step " + i,
                Progress = Math.Min(100, i * 4),
                Status = statuses[i % statuses.Length],
                Detail = "Ticket mesajları, SAP sorgu sonucu, kullanıcı aksiyonu veya server bağlantı olayı zaman akışında okunabilir."
            });
        }
    }

    private void AddAudixMediaRows()
    {
        string[] formats = { "FLAC", "MP3", "320", "Hi-Res", "Eksik Kapak" };
        string[] artists = { "Tarkan", "Sezen Aksu", "Daft Punk", "Pink Floyd", "Metallica", "Queen" };
        for (int i = 1; i <= 42; i++)
        {
            _rows.Add(new ProRow
            {
                Id = 5000 + i,
                Type = "Album",
                Code = "AUDIX-" + i.ToString("000"),
                Title = artists[i % artists.Length] + " - Albüm " + i.ToString("00"),
                Machine = "Audix Library",
                RefDes = (1990 + i % 34).ToString(),
                Progress = 55 + i % 45,
                Status = formats[i % formats.Length],
                Detail = "Albüm kapağı ImageGetter ile bağlanır; Pano Poster/Gallery/MediaTile/FilmStrip modlarında aynı data farklı sunulur."
            });
        }
    }

    private void AddV32PhaseRows()
    {
        AddPhaseRow(38, "UX Intelligence", "Kullanıcının kolon, filtre ve görünüm alışkanlıklarını hatırlama", "Audix / MasterData / Support Desk");
        AddPhaseRow(39, "Factory Intelligence", "Makine durum overlay, canlı dashboard ve heatmap hazırlığı", "Line Workspace / Factory Navigator");
        AddPhaseRow(40, "Timeline Engine", "Ticket, makine ve mesaj geçmişlerini zaman akışında gösterme", "AOI Support Desk");
        AddPhaseRow(41, "Document Explorer", "PDF, CAD, video, görsel ve klasör önizlemeli galeri", "Audix / Teknik Doküman");
        AddPhaseRow(42, "Virtualization Pro", "100K+ satır ve poster/tile sanallaştırma hazırlığı", "MasterData / SQL büyük veri");
        AddPhaseRow(43, "Search Everywhere", "Satır, kolon, filtre ve komutlarda tek merkezden arama", "Tüm projeler");
        AddPhaseRow(44, "Command Palette", "Ctrl+Shift+P tarzı komut merkezi", "Tüm projeler");
        AddPhaseRow(45, "Layout Studio", "Kullanıcının görünümü tasarlayıp kaydetmesi", "Rapor / Dashboard ekranları");
        AddPhaseRow(46, "Dashboard Builder", "KPI, heatmap, timeline, gallery ve chart widget mantığı", "Line Workspace / AOI");
        AddPhaseRow(47, "AI Layer", "Host uygulamanın ürettiği akıllı önerileri Pano ile gösterme", "Audix / AOI / MasterData");
        AddPhaseRow(48, "Pano Ecosystem", "PanoKanban, PanoTimeline, PanoDashboard gibi ortak çekirdek yaklaşımı", "Uzun vadeli platform");
        AddPhaseRow(49, "Theme Accessibility Engine", "Koyu/açık temada otomatik kontrast, buton/combo/kart okunurluğu ve merkezi palet normalizasyonu", "Tüm projeler");
    }

    private void AddPhaseRow(int id, string title, string detail, string target)
    {
        _rows.Add(new ProRow
        {
            Id = id,
            Type = "Faz " + id,
            Code = "V32-" + id,
            Title = title,
            Machine = target,
            RefDes = "PanoExperiencePhase." + title.Replace(" ", string.Empty),
            Progress = Math.Min(100, 45 + (id - 38) * 5),
            Status = id <= 42 ? "Çekirdek" : "Hazır API",
            Detail = detail
        });
    }

    private void AddThemeAccessibilityRows()
    {
        string[] types = { "Text", "Button", "ComboBox", "Badge", "Card", "Info", "Selection", "Border" };
        string[] statuses = { "Primary", "Muted", "Success", "Warning", "Danger", "Accent", "Disabled", "Selected" };
        for (int i = 0; i < 32; i++)
        {
            _rows.Add(new ProRow
            {
                Id = 9000 + i,
                Type = types[i % types.Length],
                Code = "THEME-" + (i + 1).ToString("000"),
                Title = types[i % types.Length] + " kontrast testi",
                Machine = i % 2 == 0 ? "Dark / FactoryOS" : "Light / SmokeWhite",
                RefDes = "Ratio >= 4.5",
                Progress = 45 + (i * 11) % 55,
                Status = statuses[i % statuses.Length],
                Detail = "Bu satır Theme Accessibility Engine ile koyu/açık temada yazı, bilgi metni, buton kenarı, seçim ve kart yüzeyi okunurluğunu test etmek için eklendi."
            });
        }
    }


    private void AddV34QualityRows()
    {
        AddPhaseRow(34, "Build Quality", "Duplicate property/class riskleri, public API taraması, kolon/view/theme/media kalite kontrol raporu", "Pano çekirdeği / Example Center");
        AddPhaseRow(341, "Compatibility Guards", "Eski projeleri kırmadan yeni property ve görünüm modlarını güvenli varsayılanlarla taşır", "MasterData / AOI / Audix");
        AddPhaseRow(342, "Risky Option Repair", "Küçük popup, aşırı küçük poster ölçüsü ve düşük kontrast gibi riskleri onarır", "Tüm projeler");
        AddPhaseRow(343, "Diagnostics Text", "RunBuildQualityDiagnosticsText ile host uygulama içinde hızlı rapor alınabilir", "Geliştirici ekranları");
    }

    private void AddV35ThemeStudioRows()
    {
        foreach (var palette in _grid.GetThemeStudioPalettes())
        {
            _rows.Add(new ProRow
            {
                Id = 35000 + (int)palette.Preset,
                Type = "Theme",
                Code = "V35-" + palette.Preset,
                Title = palette.Name,
                Machine = palette.Theme.IsDark ? "Dark" : "Light",
                RefDes = "Contrast OK",
                Progress = 100,
                Status = palette.Preset.ToString().Contains("Audix") ? "Audix" : palette.Preset.ToString().Contains("Factory") ? "FactoryOS" : "Preset",
                Detail = palette.Notes
            });
        }
    }

    private void AddV36MediaProRows()
    {
        string[] formats = { "FLAC", "MP3", "320kbps", "Hi-Res", "Eksik Kapak", "WAV" };
        string[] artists = { "Tarkan", "Sezen Aksu", "Daft Punk", "Pink Floyd", "Metallica", "Queen", "Anna Meliti", "Archive" };
        for (int i = 1; i <= 64; i++)
        {
            _rows.Add(new ProRow
            {
                Id = 36000 + i,
                Type = "Media Pro",
                Code = "AUDIX-PRO-" + i.ToString("000"),
                Title = artists[i % artists.Length] + " - Albüm Pro " + i.ToString("00"),
                Machine = i % 3 == 0 ? "Disk Cache" : i % 3 == 1 ? "Memory Cache" : "Lazy Load",
                RefDes = (1985 + i % 40).ToString(),
                Progress = 60 + (i * 7) % 40,
                Status = formats[i % formats.Length],
                Detail = "V36 Media Pro: MediaImagePathGetter, MediaPlaceholderImage, MediaQualityBadgeAspectName, ShowMediaOverlayButton ve MediaImageScaleMode.Cover ile Audix kapak deneyimi."
            });
        }
    }

    private void AddDiagnosticsCenterRows()
    {
        _rows.Add(new ProRow { Id = 37001, Type = "Diagnostics", Code = "CENTER", Title = "Pano Diagnostics Center", Machine = "Runtime Menu", RefDes = "ShowPanoDiagnosticsCenter", Progress = 100, Status = "Ready", Detail = "Sağ tık > Görünüm > Dashboard preset editor > Diagnostics Center penceresi build quality, runtime hardening, media, dashboard ve performance kontrollerini tek raporda toplar." });
        _rows.Add(new ProRow { Id = 37002, Type = "Quality", Code = "V34", Title = "Build Quality Aggregation", Machine = "Taylan.Pano.Core", RefDes = "RunBuildQualityDiagnostics", Progress = 100, Status = "OK", Detail = "Public API, kolon, aktif görünüm, tema kontrastı ve medya kaynak uyarıları tek rapora aktarılır." });
        _rows.Add(new ProRow { Id = 37003, Type = "Runtime", Code = "V50", Title = "Foundation Runtime Checks", Machine = "Taylan.Pano.Core", RefDes = "RunPano5RuntimeChecks", Progress = 100, Status = "OK", Detail = "Tema erişilebilirliği, media playback state, command palette ve modül profili hızlı kontrol edilir." });
        _rows.Add(new ProRow { Id = 37004, Type = "Hardening", Code = "V50.2", Title = "Runtime Hardening Checks", Machine = "Taylan.Pano.Core", RefDes = "RunPano502RuntimeHardeningChecks", Progress = 100, Status = "OK", Detail = "Tema, medya cache/lazy loading, playback overlay, search/command ve layout studio guard değerleri doğrulanır." });
        _rows.Add(new ProRow { Id = 37005, Type = "Media", Code = "PRESET", Title = "Media Smart Preset", Machine = "Audix / Media", RefDes = "ApplyMediaSmartPreset", Progress = 96, Status = "Active", Detail = "Müzik, film, fotoğraf ve doküman için ayrı hazır profil; image cache, lazy resolve, kapak/poster ve playback davranışlarını birlikte ayarlar." });
        _rows.Add(new ProRow { Id = 37006, Type = "Dashboard", Code = "WIDGET", Title = "Dashboard Preset Editor", Machine = "KPI / HeatMap / MiniChart", RefDes = "SetDashboardWidgetEnabled", Progress = 92, Status = "Active", Detail = "Sağ tık menüsünden KPI, HeatMap ve MiniChart bileşenleri açılıp kapatılır; rapor widget sayısını ve aktif bileşenleri gösterir." });
        _rows.Add(new ProRow { Id = 37007, Type = "Memory", Code = "VIEW", Title = "View Mode Memory", Machine = "Scenario UX", RefDes = "RememberViewModePerScenario", Progress = 90, Status = "Active", Detail = "Kullanıcı senaryo bazında son görünümü hatırlatabilir; senaryo defaultları hafızayı ezmeden uygulanır." });
        _rows.Add(new ProRow { Id = 37008, Type = "Performance", Code = "PAINT", Title = "Paint Performance Pass", Machine = "Large Lists", RefDes = "GetPerformanceSummary", Progress = 88, Status = "Watch", Detail = "Last/average paint süresi, paint sayısı ve satır sayısı rapora eklenir; büyük medya listesinde cache/lazy ayarları için uyarı üretir." });
        _rows.Add(new ProRow { Id = 37009, Type = "Guide", Code = "VIEW-MODE", Title = "View Mode Decision Guide", Machine = "UX Guide", RefDes = "GetViewModeDecisionGuideText", Progress = 100, Status = "New", Detail = "RowCard/RowPreview, DetailCard/PropertyCard, medya ve dashboard görünümlerinin ne zaman seçileceği raporun sonunda kısa rehber olarak verilir." });
    }

    private void AddFactoryIntelligenceRows()
    {
        string[] lines = { "LINE-01", "LINE-02", "LINE-03", "LINE-04", "REWORK", "AOI" };
        string[] states = { "Running", "Waiting", "Fault", "Offline", "Maintenance", "OK" };
        for (int i = 1; i <= 36; i++)
        {
            _rows.Add(new ProRow
            {
                Id = 6000 + i,
                Type = "Machine",
                Code = "MC-" + i.ToString("000"),
                Title = "Makine durum kartı / üretim heatmap",
                Machine = lines[i % lines.Length],
                RefDes = "Slot " + (i % 12 + 1),
                Progress = 20 + (i * 7) % 80,
                Status = states[i % states.Length],
                Detail = "FactoryStatusAspectName ile durum okunur; HeatMap/KPI/Dashboard görünümüne taşınır."
            });
        }
    }

    private void AddDocumentExplorerRows()
    {
        string[] kinds = { "PDF", "Image", "CAD", "Video", "Audio", "Folder" };
        for (int i = 1; i <= 48; i++)
        {
            _rows.Add(new ProRow
            {
                Id = 7000 + i,
                Type = kinds[i % kinds.Length],
                Code = "DOC-" + i.ToString("000"),
                Title = kinds[i % kinds.Length] + " önizleme kartı",
                Machine = "Document Explorer",
                RefDes = "Preview",
                Progress = 100,
                Status = i % 5 == 0 ? "Eksik Thumbnail" : "Hazır",
                Detail = "PanoDocumentPreviewKind ile host uygulama thumbnail üretimini yönetebilir. Gallery görünümü Explorer hissi verir."
            });
        }
    }

    private void AddVirtualizationRows()
    {
        for (int i = 1; i <= 300; i++)
        {
            _rows.Add(new ProRow
            {
                Id = 8000 + i,
                Type = i % 3 == 0 ? "SQL" : "Virtual",
                Code = "ROW-" + i.ToString("000000"),
                Title = "Virtualization Pro büyük veri satırı " + i.ToString("000"),
                Machine = i % 2 == 0 ? "PCBADB01" : "PCBADB02",
                RefDes = "Provider",
                Progress = i % 100,
                Status = i % 7 == 0 ? "Lazy" : "Ready",
                Detail = "Örnek merkezinde 300 satır gösterilir; gerçek kullanımda IRowProvider/SQL/API ile 100K+ hedeflenir."
            });
        }
    }

    private void AddDashboardBuilderRows()
    {
        string[] widgets = { "KPI", "Chart", "Table", "Card", "HeatMap", "Timeline", "Gallery", "Kanban" };
        for (int i = 0; i < widgets.Length; i++)
        {
            _rows.Add(new ProRow
            {
                Id = 9000 + i,
                Type = "Widget",
                Code = "WGT-" + widgets[i].ToUpperInvariant(),
                Title = widgets[i] + " widget tanımı",
                Machine = "Dashboard Builder",
                RefDes = "PanoDashboardWidgetKind." + widgets[i],
                Progress = 70 + i * 3,
                Status = i % 2 == 0 ? "Aktif" : "Hazır",
                Detail = "DashboardWidgets koleksiyonu ile host uygulama Pano içinde hafif dashboard düzeni hazırlayabilir."
            });
        }
    }


    private void AddV502HardeningRows()
    {
        _rows.Add(new ProRow { Id = 5021, Type = "Build", Code = "API-GUARD", Title = "Statik API Guard", Machine = "tools/pano_api_guard.py", RefDes = "Duplicate/Namespace", Progress = 94, Status = "OK", Detail = "Duplicate property/event, eksik namespace ve örnek merkez referans riskleri için guard akışı güçlendirildi." });
        _rows.Add(new ProRow { Id = 5022, Type = "Runtime", Code = "CHECKS", Title = "RunPano502RuntimeHardeningChecks", Machine = "Taylan.Pano.Core", RefDes = "Theme/Media/Layout", Progress = 100, Status = "OK", Detail = "Host uygulama açılışında tema, medya, interaction ve layout güvenliğini tek listede doğrulayabilir." });
        _rows.Add(new ProRow { Id = 5023, Type = "Audix", Code = "MEDIA-DEFAULTS", Title = "ApplyAudix502MediaDefaults", Machine = "Poster/Gallery/FilmStrip", RefDes = "Cover/Playback", Progress = 100, Status = "Ready", Detail = "Albüm kapağı cache, lazy loading, play/pause state, şimdi çalıyor rozeti, equalizer ve video preview ayarlarını birlikte uygular." });
        _rows.Add(new ProRow { Id = 5024, Type = "Theme", Code = "READABILITY", Title = "Strict Theme Readability Guard", Machine = "Theme Studio", RefDes = "Dark/Light", Progress = 100, Status = "Ready", Detail = "Koyu/açık tema geçişlerinde buton, bilgi metni, badge, combo ve kart içi metin kontrastı güvenli varsayılanlara alınır." });
        _rows.Add(new ProRow { Id = 5025, Type = "Performance", Code = "CACHE", Title = "Media Cache Limit Guard", Machine = "Pano.Media", RefDes = "512 Audix", Progress = 90, Status = "Watch", Detail = "Audix profili medya memory cache limitini yükseltir; büyük arşivlerde lazy loading ile birlikte kullanılmalıdır." });
        _rows.Add(new ProRow { Id = 5026, Type = "Example", Code = "NAV", Title = "Hardening Sample", Machine = "Example Center", RefDes = "v50.2", Progress = 100, Status = "New", Detail = "Örnek merkezinde v50.2 Build & Runtime Hardening ayrı ekran ve hızlı erişim olarak eklendi." });
    }

    private void AddV50FoundationRows()
    {
        _rows.Add(new ProRow { Id = 5001, Type = "Stability", Code = "CHECK-THEME", Title = "Theme Accessibility Guard", Machine = "Taylan.Pano.Core", RefDes = "Dark/Light", Progress = 100, Status = "OK", Detail = "Koyu/açık tema geçişlerinde metin, buton, combo, kart ve boş mesaj kontrastı merkezi olarak normalize edilir." });
        _rows.Add(new ProRow { Id = 5002, Type = "Stability", Code = "CHECK-MEDIA", Title = "Media Playback State Guard", Machine = "Pano.Media", RefDes = "Audio/Video", Progress = 100, Status = "OK", Detail = "Audix gibi uygulamalarda play tuşuna basıldığında kart playing/paused/loading/error durumunu görsel olarak gösterir." });
        _rows.Add(new ProRow { Id = 5003, Type = "Profile", Code = "AUDIX", Title = "Audix Media Profile", Machine = "Poster/Gallery", RefDes = "Cover", Progress = 96, Status = "Ready", Detail = "Albüm kapağı, FilmStrip, Video Preview, Play/Pause state, overlay, kalite rozeti ve medya cache ayarlarını birlikte açar." });
        _rows.Add(new ProRow { Id = 5004, Type = "Profile", Code = "AOI", Title = "AOI Support Desk Profile", Machine = "Kanban/Timeline", RefDes = "Ticket", Progress = 92, Status = "Ready", Detail = "Ticket kartları, timeline, hızlı arama, command palette ve erişilebilir tema ayarları için hazır profil." });
        _rows.Add(new ProRow { Id = 5005, Type = "Navigator", Code = "EXAMPLE-CENTER", Title = "Kategori + Arama Destekli Örnek Merkezi", Machine = "Example Center", RefDes = "UX", Progress = 100, Status = "New", Detail = "Örnek merkezinde özellikleri bulmak için sol menüye kategori filtresi ve arama kutusu eklendi." });
        _rows.Add(new ProRow { Id = 5006, Type = "Guard", Code = "API-GUARD", Title = "API Guard / Duplicate Risk Control", Machine = "Build", RefDes = "Property", Progress = 88, Status = "Watch", Detail = "PanoColumnKind, EnableCommandPalette, Size overload gibi önceki hataları yakalamak için kalite kontrol akışı dokümante edildi." });
    }

    private void ApplyTheme(bool dark)
    {
        _dark = dark;
        Color back = dark ? Color.FromArgb(24, 26, 31) : Color.FromArgb(246, 248, 252);
        Color panel = dark ? Color.FromArgb(32, 35, 42) : Color.White;
        Color text = dark ? Color.White : Color.FromArgb(27, 31, 38);
        Color muted = dark ? Color.FromArgb(178, 186, 199) : Color.FromArgb(85, 94, 108);

        BackColor = back;
        ForeColor = text;
        _mainSplit.Panel1.BackColor = panel;
        _mainSplit.Panel2.BackColor = back;
        _hero.BackColor = panel;
        _hero.ForeColor = text;
        _hero.Text = "Pano Example Center Pro\nÖzellik rehberi, kategori bulucu ve canlı test merkezi";
        _description.BackColor = back;
        _description.ForeColor = muted;
        _scenarioList.BackColor = panel;
        _scenarioList.ForeColor = text;
        _navigator.BackColor = panel;
        _navigatorInfo.BackColor = panel;
        _navigatorInfo.ForeColor = muted;
        _scenarioSearch.BackColor = dark ? Color.FromArgb(41, 45, 54) : Color.White;
        _scenarioSearch.ForeColor = text;
        _scenarioCategory.BackColor = dark ? Color.FromArgb(41, 45, 54) : Color.White;
        _scenarioCategory.ForeColor = text;
        _details.BackColor = panel;
        _details.ForeColor = text;
        _contentSplit.Panel1.BackColor = back;
        _contentSplit.Panel2.BackColor = panel;

        PanoTheme theme = PanoTheme.FromParentColor(back, text);
        _grid.ApplyTheme(theme);
        SmartMenuRenderer.ApplyTo(_tool, theme);
    }

    private sealed class ScenarioDoc
    {
        public string Category { get; init; } = string.Empty;
        public string WhatItShows { get; init; } = string.Empty;
        public string HowToTest { get; init; } = string.Empty;
        public string ExpectedResult { get; init; } = string.Empty;
        public string Api { get; init; } = string.Empty;
        public string Tags { get; init; } = string.Empty;

        public static ScenarioDoc Create(ScenarioInfo scenario, string whatItShows, string howToTest, string expectedResult, string api, string tags)
        {
            return new ScenarioDoc
            {
                Category = scenario.Category,
                WhatItShows = whatItShows,
                HowToTest = howToTest,
                ExpectedResult = expectedResult,
                Api = api,
                Tags = tags
            };
        }
    }

    private sealed class ScenarioInfo
    {
        public ScenarioInfo(string title, PanoScenario scenario, string description, string longDescription)
        {
            Title = title;
            Scenario = scenario;
            Description = description;
            LongDescription = longDescription;
            Category = ResolveCategory(title, scenario);
            Scope = ResolveScope(title);
            DisplayTitle = $"{Title}  [{Scope}]";
        }

        public string Title { get; }
        public string DisplayTitle { get; }
        public string Category { get; }
        public string Scope { get; }
        public PanoScenario Scenario { get; }
        public string Description { get; }
        public string LongDescription { get; }

        private static string ResolveScope(string title)
        {
            if (title.Contains("Diagnostics", StringComparison.OrdinalIgnoreCase)) return "Diagnostics merkezi";
            if (title.Contains("Kolon Designer", StringComparison.OrdinalIgnoreCase)) return "Designer testi";
            if (title.Contains("Runtime View Menu", StringComparison.OrdinalIgnoreCase)) return "Runtime menü";
            if (title.Contains("Faz Merkezi", StringComparison.OrdinalIgnoreCase)) return "Rehber / indeks";
            if (title.Contains("v37-v40", StringComparison.OrdinalIgnoreCase)) return "Toplu paket";
            if (title.Contains("v50.1", StringComparison.OrdinalIgnoreCase)) return "Navigasyon";
            if (title.Contains("v50.2", StringComparison.OrdinalIgnoreCase)) return "Hardening";
            if (title.Contains("Foundation", StringComparison.OrdinalIgnoreCase)) return "Foundation";
            if (title.Contains("Pano 5.1", StringComparison.OrdinalIgnoreCase)) return "Pilot";
            if (title.Contains("Build Quality", StringComparison.OrdinalIgnoreCase)) return "Kalite kontrol";
            if (title.Contains("Theme Studio", StringComparison.OrdinalIgnoreCase)) return "Tema preset";
            if (title.Contains("Theme Lab", StringComparison.OrdinalIgnoreCase)) return "Tema testi";
            if (title.Contains("Media Pro", StringComparison.OrdinalIgnoreCase)) return "Medya pro";
            if (title.Contains("Audix Media", StringComparison.OrdinalIgnoreCase)) return "Medya temel";
            if (title.Contains("v32", StringComparison.OrdinalIgnoreCase)) return "Faz detayı";
            if (title.Contains("v28", StringComparison.OrdinalIgnoreCase) || title.Contains("v27", StringComparison.OrdinalIgnoreCase)) return "UX fazı";
            return "Ana örnek";
        }

        private static string ResolveCategory(string title, PanoScenario scenario)
        {
            if (title.Contains("Kolon Designer", StringComparison.OrdinalIgnoreCase)) return "Designer / Kolon";
            if (title.Contains("Diagnostics", StringComparison.OrdinalIgnoreCase) || title.Contains("Build Quality", StringComparison.OrdinalIgnoreCase) || title.Contains("Hardening", StringComparison.OrdinalIgnoreCase) || title.Contains("Foundation", StringComparison.OrdinalIgnoreCase)) return "Stabilite / Diagnostics";
            if (title.Contains("Audix", StringComparison.OrdinalIgnoreCase) || title.Contains("Media", StringComparison.OrdinalIgnoreCase) || title.Contains("Poster", StringComparison.OrdinalIgnoreCase) || title.Contains("Document", StringComparison.OrdinalIgnoreCase)) return "Medya / Audix / Doküman";
            if (title.Contains("Theme", StringComparison.OrdinalIgnoreCase)) return "Tema / Okunurluk";
            if (title.Contains("Dashboard", StringComparison.OrdinalIgnoreCase) || title.Contains("Analytics", StringComparison.OrdinalIgnoreCase) || title.Contains("Factory Intelligence", StringComparison.OrdinalIgnoreCase)) return "Dashboard / Analytics";
            if (title.Contains("Performance", StringComparison.OrdinalIgnoreCase) || title.Contains("Virtualization", StringComparison.OrdinalIgnoreCase) || title.Contains("Yoğun", StringComparison.OrdinalIgnoreCase)) return "Performans / Büyük Veri";
            if (title.Contains("Filter", StringComparison.OrdinalIgnoreCase) || title.Contains("Menu", StringComparison.OrdinalIgnoreCase) || title.Contains("State", StringComparison.OrdinalIgnoreCase) || title.Contains("Enterprise", StringComparison.OrdinalIgnoreCase) || title.Contains("UX Polish", StringComparison.OrdinalIgnoreCase)) return "Filtre / Menü / State";
            if (title.Contains("Kanban", StringComparison.OrdinalIgnoreCase) || title.Contains("Timeline", StringComparison.OrdinalIgnoreCase) || title.Contains("İşlem Geçmişi", StringComparison.OrdinalIgnoreCase)) return "Workflow / Kanban / Timeline";
            if (title.Contains("Search", StringComparison.OrdinalIgnoreCase) || title.Contains("Layout", StringComparison.OrdinalIgnoreCase) || title.Contains("AI Layer", StringComparison.OrdinalIgnoreCase) || title.Contains("Faz Merkezi", StringComparison.OrdinalIgnoreCase)) return "Filtre / Menü / State";
            if (scenario == PanoScenario.Timeline) return "Workflow / Kanban / Timeline";
            return "Başlangıç / Temel Görünümler";
        }
    }

    private sealed class ProRow
    {
        public bool Selected { get; set; }
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Machine { get; set; } = string.Empty;
        public string RefDes { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }
}

