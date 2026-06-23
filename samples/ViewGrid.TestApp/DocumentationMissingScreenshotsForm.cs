using ViewGrid.Localization;

namespace ViewGrid.TestApp;

public sealed class DocumentationMissingScreenshotsForm : ViewGridSampleFormBase
{
    private readonly DocumentationMissingScreenshotScenario _scenario;

    public DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenario scenario)
        : base($"ViewGrid DOCX Missing Screenshot - {scenario.Title}", scenario.Description)
    {
        _scenario = scenario;
        Text = $"ViewGrid Documentation Capture - {scenario.TargetHeading}";
        Width = 1280;
        Height = 800;
        MinimumSize = new Size(1100, 680);

        Tool.Items.Add(new ToolStripSeparator());
        Tool.Items.Add(new ToolStripLabel($"DOCX: {scenario.TargetHeading}"));
        Tool.Items.Add(new ToolStripLabel($"PNG: {scenario.OutputFileName}.png"));

        Info.Text = $"{scenario.Description}  |  Target: {scenario.TargetHeading}  |  Output: docs/screenshots/{scenario.OutputFileName}.png";
        ApplyScenario();
    }

    private void ApplyScenario()
    {
        ViewGrid.Columns.Clear();
        ViewGrid.ClearFilters();
        ViewGrid.ClearGrouping();
        ViewGrid.ClearSort();
        ViewGrid.ShowHeader = true;
        ViewGrid.ShowGridLines = true;
        ViewGrid.FullRowSelect = true;
        ViewGrid.MultiSelect = true;
        ViewGrid.AllowEditAllCells = false;
        ViewGrid.AllowColumnReorder = false;
        ViewGrid.AllowRowDragDrop = false;
        ViewGrid.ShowQuickFilterBar = false;
        ViewGrid.ShowActiveFilterChips = false;
        ViewGrid.CardVisualAdornments = false;
        ViewGrid.AllowMultilineCells = false;
        ViewGrid.MaxCellTextLines = 1;
        ViewGrid.DetailsRowHeight = 30;
        ViewGrid.EnableSearchEverywhere = false;
        ViewGrid.EnableCommandPalette = false;

        switch (_scenario.Key)
        {
            case "kolon-sistemi":
                ConfigureMixedColumnSystem();
                break;
            case "veri-baglama":
                ConfigureDataBinding();
                break;
            case "query-language":
                ConfigureQueryLanguage();
                break;
            case "quick-filter-bar":
                ConfigureQuickFilter();
                break;
            case "sorting":
                ConfigureSorting();
                break;
            case "grouping":
                ConfigureGrouping();
                break;
            case "conditional-formatting":
                ConfigureConditionalFormatting();
                break;
            case "card-visual-adornments":
                ConfigureCardVisuals();
                break;
            case "card-actions":
                ConfigureCardActions();
                break;
            case "checkbox-layout":
                ConfigureCheckboxLayout();
                break;
            case "row-height-management":
                ConfigureRowHeight();
                break;
            case "profile-migration":
                ConfigureProfileMigration();
                break;
            case "expression-engine":
                ConfigureExpressionEngine();
                break;
            case "formula-columns":
                ConfigureFormulaColumns();
                break;
            case "change-tracking":
                ConfigureChangeTracking();
                break;
            case "event-bus":
                ConfigureEventBus();
                break;
            case "action-pipeline":
                ConfigureActionPipeline();
                break;
            case "copy-system-pro":
                ConfigureCopySystem();
                break;
            case "virtual-mode":
                ConfigureVirtualMode(1_500_000, "Virtual Mode provider: 1.500.000 satır");
                break;
            case "stress-test":
                ConfigureVirtualMode(9_990_000, "Stress Test provider: 9.990.000 satır / hızlı scroll + filtre");
                break;
            case "high-dpi":
                ConfigureHighDpi();
                break;
            case "localization":
                ConfigureLocalization();
                break;
            case "fixedfree-resize":
                ConfigureFixedFreeResize();
                break;
            case "image-cache":
                ConfigureImageCache();
                break;
            case "search-everywhere":
                ConfigureSearchEverywhere();
                break;
            case "inline-editing":
                ConfigureInlineEditing();
                break;
            case "drag-and-drop":
                ConfigureDragAndDrop();
                break;
            case "multi-view-sync":
                ConfigureMultiViewSync();
                break;
            case "state-management":
                ConfigureStateManagement();
                break;
            case "plugin-system":
                ConfigurePluginSystem();
                break;
            case "smart-suggestions":
                ConfigureSmartSuggestions();
                break;
            default:
                ConfigureCapabilityMatrix("Documentation", _scenario.Title, _scenario.Description);
                break;
        }

        ViewGrid.Refresh();
        Application.DoEvents();
    }

    private void AddStandardColumns(bool includeAction = false, bool includeSparkline = false)
    {
        ViewGrid.Columns.Add(new ViewGridColumn("✓", nameof(DocumentationFeatureRow.Checked), 44) { Kind = ViewGridColumnKind.CheckBox, HeaderCheckBox = true, HeaderCheckBoxThreeState = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Id", nameof(DocumentationFeatureRow.Id), 70));
        ViewGrid.Columns.Add(new ViewGridColumn("Ad", nameof(DocumentationFeatureRow.Name), 230) { FillFreeSpace = true, Editable = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Kategori", nameof(DocumentationFeatureRow.Category), 150));
        ViewGrid.Columns.Add(new ViewGridColumn("Durum", nameof(DocumentationFeatureRow.Status), 110) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("İlerleme", nameof(DocumentationFeatureRow.Progress), 130) { Kind = ViewGridColumnKind.ProgressBar });
        ViewGrid.Columns.Add(new ViewGridColumn("Skor", nameof(DocumentationFeatureRow.Score), 90) { Kind = ViewGridColumnKind.Rating, MaxRating = 5 });
        if (includeSparkline)
            ViewGrid.Columns.Add(new ViewGridColumn("Trend", nameof(DocumentationFeatureRow.Spark), 140) { Kind = ViewGridColumnKind.Sparkline });
        if (includeAction)
        {
            ViewGrid.Columns.Add(new ViewGridColumn("Aksiyon", nameof(DocumentationFeatureRow.Action), 110) { Kind = ViewGridColumnKind.Button, ButtonText = "Aç" });
            ViewGrid.Columns.Add(new ViewGridColumn("Link", nameof(DocumentationFeatureRow.Link), 120) { Kind = ViewGridColumnKind.Hyperlink });
        }
        ViewGrid.Columns.Add(new ViewGridColumn("Notlar", nameof(DocumentationFeatureRow.Notes), 340) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 2 });
    }

    private void AddCapabilityColumns()
    {
        ViewGrid.Columns.Add(new ViewGridColumn("Sıra", nameof(DocumentationFeatureRow.Id), 70));
        ViewGrid.Columns.Add(new ViewGridColumn("Yetkinlik", nameof(DocumentationFeatureRow.Name), 260) { FillFreeSpace = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Kategori", nameof(DocumentationFeatureRow.Category), 160));
        ViewGrid.Columns.Add(new ViewGridColumn("Durum", nameof(DocumentationFeatureRow.Status), 120) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("Kapsam", nameof(DocumentationFeatureRow.Progress), 120) { Kind = ViewGridColumnKind.ProgressBar });
        ViewGrid.Columns.Add(new ViewGridColumn("Çıktı", nameof(DocumentationFeatureRow.Action), 130));
        ViewGrid.Columns.Add(new ViewGridColumn("Açıklama", nameof(DocumentationFeatureRow.Notes), 420) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 3 });
    }

    private void ConfigureMixedColumnSystem()
    {
        AddStandardColumns(includeAction: true, includeSparkline: true);
        ViewGrid.Columns.Add(new ViewGridColumn("Etiketler", nameof(DocumentationFeatureRow.Tags), 160) { Kind = ViewGridColumnKind.Tags });
        ViewGrid.SetObjects(CreateRows("Column", 42));
        ViewGrid.SortBy(ViewGrid.Columns[nameof(DocumentationFeatureRow.Status)], false);
    }

    private void ConfigureDataBinding()
    {
        AddCapabilityColumns();
        ViewGrid.SetObjects(new[]
        {
            Row(1, "List<T> binding", "Binding", "Ready", 100, "SetObjects", "POCO nesne listesi doğrudan ViewGrid içine bağlanır."),
            Row(2, "BindingSource", "Binding", "Ready", 95, "BindingSource.List", "WinForms veri bağlama zinciri korunur."),
            Row(3, "DataTable rows", "Binding", "Ready", 90, "DataRow list", "Legacy DataTable ekranları kontrollü geçiş yapabilir."),
            Row(4, "Virtual provider", "Binding", "Preview", 85, "IQueryRowProvider", "Büyük veri provider üzerinden sayfalı okunur."),
            Row(5, "RefreshObject", "Binding", "Ready", 80, "Update row", "Tek satır güncelleme ile ekran yeniden çizilir.")
        });
    }

    private void ConfigureQueryLanguage()
    {
        AddCapabilityColumns();
        ViewGrid.ShowQuickFilterBar = true;
        ViewGrid.ShowActiveFilterChips = true;
        ViewGrid.SetObjects(new[]
        {
            Row(1, "status:open", "Alias", "Match", 100, "Durum=Open", "Durum alanını alias ile filtreler."),
            Row(2, "date >= 2026-06-01", "Date", "Match", 90, "Tarih aralığı", "Tarih karşılaştırma operatörleri desteklenir."),
            Row(3, "machine~AOI", "Contains", "Match", 82, "Metin arama", "Yaklaşık/metin içerir araması için kullanılır."),
            Row(4, "status:open AND sla:late", "Boolean", "Match", 75, "AND/OR", "Power-user filtrelerinde birleşik koşullar."),
            Row(5, "owner:operator", "Alias", "Ready", 70, "Rol filtresi", "Kısaltılmış alan adları okunabilir sorguya çevrilir.")
        });
        ViewGrid.SetGlobalFilter("status open");
    }

    private void ConfigureQuickFilter()
    {
        AddStandardColumns();
        ViewGrid.ShowQuickFilterBar = true;
        ViewGrid.ShowActiveFilterChips = true;
        ViewGrid.SetObjects(CreateRows("Quick Filter", 80));
        ViewGrid.SetGlobalFilter("Review");
        Tool.Items.Add(new ToolStripLabel("Aktif hızlı filtre: Review"));
    }

    private void ConfigureSorting()
    {
        AddStandardColumns(includeSparkline: true);
        ViewGrid.SetObjects(CreateRows("Sorting", 120));
        ViewGrid.SortBy(ViewGrid.Columns[nameof(DocumentationFeatureRow.Progress)], descending: true);
        Tool.Items.Add(new ToolStripLabel("Sıralama: İlerleme ▼"));
    }

    private void ConfigureGrouping()
    {
        AddStandardColumns();
        ViewGrid.SetObjects(CreateRows("Grouping", 120));
        ViewGrid.SetGroupBy(nameof(DocumentationFeatureRow.Status));
        Tool.Items.Add(new ToolStripLabel("Gruplama: Durum"));
    }

    private void ConfigureConditionalFormatting()
    {
        AddStandardColumns();
        ViewGrid.SetObjects(CreateRows("Quality", 80));
        ViewGrid.RowBackColorGetter = row => row is DocumentationFeatureRow r && r.Status == "Fail" ? (Color?)Color.FromArgb(255, 235, 238) : null;
        ViewGrid.RowForeColorGetter = row => row is DocumentationFeatureRow r && r.Status == "Fail" ? (Color?)Color.FromArgb(140, 0, 0) : null;
        ViewGrid.ConditionalFormats.Clear();
        ViewGrid.ConditionalFormats.Add(new global::ViewGrid.Formatting.ViewGridConditionalFormat
        {
            Column = ViewGrid.Columns[nameof(DocumentationFeatureRow.Status)],
            BackColor = Color.FromArgb(90, 229, 57, 53),
            ForeColor = Color.DarkRed,
            Predicate = (_, _, value) => Convert.ToString(value) == "Fail"
        });
    }

    private void ConfigureCardVisuals()
    {
        AddStandardColumns(includeAction: true);
        ViewGrid.CardVisualAdornments = true;
        ViewGrid.SetViewMode(ViewGridMode.DashboardCard);
        ViewGrid.SetObjects(CreateRows("Card Visual", 28));
    }

    private void ConfigureCardActions()
    {
        AddStandardColumns(includeAction: true);
        ViewGrid.SetViewMode(ViewGridMode.LargeCard);
        ViewGrid.SetObjects(CreateRows("Card Action", 30));
        ViewGrid.ButtonClick += (_, e) => Info.Text = $"Kart aksiyonu: {((DocumentationFeatureRow)e.RowObject).Name}";
        ViewGrid.HyperlinkClick += (_, e) => Info.Text = $"Kart linki: {((DocumentationFeatureRow)e.RowObject).Link}";
    }

    private void ConfigureCheckboxLayout()
    {
        ViewGrid.Columns.Add(new ViewGridColumn("Person", nameof(DocumentationFeatureRow.Name), 210) { CellCheckBox = true, HeaderCheckBox = true, HeaderCheckBoxThreeState = true, CheckBoxAspectName = nameof(DocumentationFeatureRow.Checked) });
        ViewGrid.Columns.Add(new ViewGridColumn("Operasyon", nameof(DocumentationFeatureRow.Category), 160) { CellCheckBox = true, HeaderCheckBox = true, HeaderCheckBoxThreeState = true, CheckBoxAspectName = nameof(DocumentationFeatureRow.Approved) });
        ViewGrid.Columns.Add(new ViewGridColumn("Onay", nameof(DocumentationFeatureRow.Status), 120) { CellCheckBox = true, HeaderCheckBox = true, HeaderCheckBoxThreeState = true, CheckBoxAspectName = nameof(DocumentationFeatureRow.Locked) });
        ViewGrid.Columns.Add(new ViewGridColumn("İlerleme", nameof(DocumentationFeatureRow.Progress), 130) { Kind = ViewGridColumnKind.ProgressBar });
        ViewGrid.Columns.Add(new ViewGridColumn("Notlar", nameof(DocumentationFeatureRow.Notes), 420) { FillFreeSpace = true });
        ViewGrid.KeyboardSpaceTogglesCheckBoxes = true;
        ViewGrid.SetObjects(CreateRows("Checkbox", 40));
    }

    private void ConfigureRowHeight()
    {
        AddStandardColumns();
        ViewGrid.AllowMultilineCells = true;
        ViewGrid.MaxCellTextLines = 3;
        ViewGrid.DetailsRowHeight = 44;
        ViewGrid.SetObjects(CreateRows("Row Height", 45, longNotes: true));
        Tool.Items.Add(new ToolStripLabel("DetailsRowHeight = 44 / MaxCellTextLines = 3"));
    }

    private void ConfigureProfileMigration() => ConfigureCapabilityMatrix("Profile", "Profile Migration", "Legacy JSON layout, role scope and version migration states.");
    private void ConfigureExpressionEngine() => ConfigureCapabilityMatrix("Expression", "Expression Engine", "Computed rules such as status == 'Fail' && progress < 60.");
    private void ConfigureFormulaColumns() => ConfigureCapabilityMatrix("Formula", "Formula Columns", "Computed columns: SLA score, risk index, formatted output.");
    private void ConfigureChangeTracking() => ConfigureCapabilityMatrix("Tracking", "Change Tracking", "Inserted, modified, deleted and accepted row states.");
    private void ConfigureEventBus() => ConfigureCapabilityMatrix("Event", "Event Bus", "RefreshRequested, RowChanged and ExportCompleted event stream.");
    private void ConfigureActionPipeline() => ConfigureCapabilityMatrix("Action", "Action Pipeline", "Validate -> execute -> audit -> notify command flow.");
    private void ConfigureCopySystem() => ConfigureCapabilityMatrix("Copy", "Copy System Pro", "Visible rows, selected cells, Markdown/CSV and formatted clipboard output.");

    private void ConfigureCapabilityMatrix(string category, string title, string description)
    {
        AddCapabilityColumns();
        ViewGrid.SetObjects(new[]
        {
            Row(1, $"{title} - hazırlık", category, "Ready", 100, "OK", description),
            Row(2, $"{title} - doğrulama", category, "Preview", 84, "Validate", "TestApp üzerinden kontrollü senaryo ve hata mesajı gösterilir."),
            Row(3, $"{title} - kayıt", category, "Ready", 76, "Persist", "Profil, state veya audit çıktısı uygulama klasörüne yazılır."),
            Row(4, $"{title} - tema/dil", category, "Ready", 70, "Theme", "Light/Dark/System ve localization davranışı aynı ekranda kontrol edilir."),
            Row(5, $"{title} - export", category, "Ready", 65, "Docs", "Dokümantasyon ekran görüntüsü ve manifest haritası üretilir.")
        });
    }

    private void ConfigureVirtualMode(int count, string label)
    {
        ViewGrid.Columns.Clear();
        ConfigureCommonColumns();
        MainForm.ConfigureMillionRowFiltering(ViewGrid);
        ViewGrid.SetVirtualProvider(new VirtualDemoRowProvider(count));
        Tool.Items.Add(new ToolStripLabel(label));
        ViewGrid.ShowQuickFilterBar = true;
    }

    private void ConfigureHighDpi()
    {
        AddStandardColumns();
        ViewGrid.DetailsRowHeight = 38;
        ViewGrid.AllowMultilineCells = true;
        ViewGrid.MaxCellTextLines = 2;
        ViewGrid.SetObjects(CreateRows("High DPI", 60));
        Tool.Items.Add(new ToolStripLabel("AutoScale + readable toolbar + 38px rows"));
    }

    private void ConfigureLocalization()
    {
        AddCapabilityColumns();
        ViewGrid.Language = ViewGridLanguage.Auto;
        ViewGrid.SetObjects(new[]
        {
            Row(1, "Auto", "Language", "Ready", 100, "CurrentUICulture", "Windows dili algılanır, eksik metin English fallback ile tamamlanır."),
            Row(2, "Türkçe", "Language", "Ready", 95, "tr", "Filtre, kolon seçici ve menü metinleri Türkçe görüntülenir."),
            Row(3, "English", "Language", "Ready", 95, "en", "GitHub kullanıcıları için İngilizce fallback hazırdır."),
            Row(4, "Runtime switch", "Language", "Preview", 80, "Use(...)", "Uygulama yeniden başlamadan dil değişimi doğrulanır.")
        });
    }

    private void ConfigureFixedFreeResize()
    {
        ViewGrid.Columns.Add(new ViewGridColumn("Sabit Id", nameof(DocumentationFeatureRow.Id), 88));
        ViewGrid.Columns.Add(new ViewGridColumn("Sabit Durum", nameof(DocumentationFeatureRow.Status), 120) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("Serbest Ad", nameof(DocumentationFeatureRow.Name), 220) { FillFreeSpace = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Serbest Notlar", nameof(DocumentationFeatureRow.Notes), 320) { FillFreeSpace = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Progress", nameof(DocumentationFeatureRow.Progress), 130) { Kind = ViewGridColumnKind.ProgressBar });
        ViewGrid.SetObjects(CreateRows("FixedFree", 55));
        Tool.Items.Add(new ToolStripLabel("Fixed columns + FillFreeSpace columns"));
    }

    private void ConfigureImageCache()
    {
        AddStandardColumns(includeSparkline: true);
        ViewGrid.SetViewMode(ViewGridMode.MediaTile);
        ViewGrid.SetObjects(new[]
        {
            Row(1, "Album cover 01", "Image Cache", "Cached", 100, "Memory", "Thumbnail bellek cache içinde hazır."),
            Row(2, "Film poster 14", "Image Cache", "Loading", 62, "Lazy", "İlk görünümde lazy loading ile yükleniyor."),
            Row(3, "AOI defect photo", "Image Cache", "Cached", 98, "Disk", "Disk cache üzerinden hızlı okunur."),
            Row(4, "Student photo", "Image Cache", "Placeholder", 35, "Fallback", "Görsel yoksa tema uyumlu placeholder gösterilir.")
        });
    }

    private void ConfigureSearchEverywhere()
    {
        AddCapabilityColumns();
        ViewGrid.EnableSearchEverywhere = true;
        ViewGrid.EnableCommandPalette = true;
        ViewGrid.ShowQuickFilterBar = true;
        ViewGrid.SetObjects(new[]
        {
            Row(1, "Ctrl+K Command Palette", "Search", "Ready", 100, "Command", "Komut, görünüm ve aksiyonlar tek arama alanından bulunur."),
            Row(2, "Global row search", "Search", "Ready", 95, "Rows", "Tüm görünür kolonlarda canlı arama yapılır."),
            Row(3, "Filter aliases", "Search", "Ready", 88, "Aliases", "status/open gibi kısa komutlar filtreye çevrilir."),
            Row(4, "Navigation result", "Search", "Preview", 78, "Jump", "Bulunan satıra atla ve highlight davranışı.")
        });
        ViewGrid.SetGlobalFilter("search");
    }

    private void ConfigureInlineEditing()
    {
        AddStandardColumns(includeAction: true);
        ViewGrid.AllowEditAllCells = true;
        ViewGrid.CellEditActivationKey = Keys.F2;
        foreach (var column in ViewGrid.Columns)
            column.Editable = column.AspectName is nameof(DocumentationFeatureRow.Name) or nameof(DocumentationFeatureRow.Category) or nameof(DocumentationFeatureRow.Notes);
        ViewGrid.SetObjects(CreateRows("Inline Edit", 50));
        Tool.Items.Add(new ToolStripLabel("F2 / double-click editing enabled"));
    }

    private void ConfigureDragAndDrop()
    {
        AddStandardColumns();
        ViewGrid.AllowColumnReorder = true;
        ViewGrid.ShowColumnReorderPreview = true;
        ViewGrid.AllowRowDragDrop = true;
        ViewGrid.ShowDropIndicator = true;
        ViewGrid.SetObjects(CreateRows("Drag Drop", 50));
        Tool.Items.Add(new ToolStripLabel("Column reorder + row drag/drop indicators"));
    }

    private void ConfigureMultiViewSync()
    {
        AddStandardColumns(includeSparkline: true);
        ViewGrid.SetViewMode(ViewGridMode.MasterDetail);
        ViewGrid.SetObjects(CreateRows("Sync", 42));
        Tool.Items.Add(new ToolStripLabel("Details/Card/Dashboard shared data source"));
    }

    private void ConfigureStateManagement()
    {
        AddCapabilityColumns();
        ViewGrid.SetObjects(new[]
        {
            Row(1, "ViewMode", "State", "Saved", 100, "Details", "Seçili görünüm modu profil içinde saklanır."),
            Row(2, "Filters", "State", "Saved", 95, "JSON", "Aktif filtreler kullanıcı/rol bazında korunur."),
            Row(3, "Columns", "State", "Saved", 90, "Layout", "Sıra, genişlik ve görünürlük import/export edilir."),
            Row(4, "Theme", "State", "Ready", 85, "System", "Light/Dark/System tema seçimi kalıcı hale gelir.")
        });
    }

    private void ConfigurePluginSystem()
    {
        AddCapabilityColumns();
        ViewGrid.SetObjects(new[]
        {
            Row(1, "TicketHeatmapPlugin", "Plugin", "Loaded", 100, "Visual", "Satır risk değerinden heatmap görseli üretir."),
            Row(2, "ExportAuditPlugin", "Plugin", "Loaded", 90, "Audit", "Export işlemi öncesi/sonrası kayıt alır."),
            Row(3, "ThemeGuardPlugin", "Plugin", "Loaded", 82, "Theme", "Kontrast ve okunurluk uyarıları üretir."),
            Row(4, "SmartFilterPlugin", "Plugin", "Preview", 75, "Filter", "Sık kullanılan filtreleri öneri olarak sunar.")
        });
    }

    private void ConfigureSmartSuggestions()
    {
        AddCapabilityColumns();
        ViewGrid.SetObjects(new[]
        {
            Row(1, "Bu ekranda status filtresi öner", "Suggestion", "New", 100, "Filter", "Fail/Review satır yoğunluğu arttığında filtre önerisi göster."),
            Row(2, "Progress kolonunu sağa sabitle", "Suggestion", "New", 86, "Layout", "Operatör ekranında kritik metrik kolonlarını öne çıkar."),
            Row(3, "Dark tema kontrastını yükselt", "Suggestion", "Ready", 78, "Theme", "Koyu temada düşük kontrastlı badge uyarısı."),
            Row(4, "Virtual mode kullan", "Suggestion", "Ready", 72, "Performance", "Satır sayısı eşiği aşıldığında provider önerisi.")
        });
        ViewGrid.ShowQuickFilterBar = true;
    }

    private static DocumentationFeatureRow Row(int id, string name, string category, string status, int progress, string action, string notes)
        => new()
        {
            Id = id,
            Name = name,
            Category = category,
            Status = status,
            Progress = Math.Max(0, Math.Min(100, progress)),
            Score = Math.Max(1, Math.Min(5, (progress / 20) + 1)),
            Action = action,
            Link = "Detay",
            Notes = notes,
            Tags = category + "," + status,
            Spark = $"{progress % 20},{(progress + 12) % 40},{(progress + 24) % 60},{(progress + 36) % 80},{progress}",
            Checked = id % 2 == 0,
            Approved = id % 3 == 0,
            Locked = id % 4 == 0,
            CreatedAt = DateTime.Today.AddDays(-id)
        };

    private static List<DocumentationFeatureRow> CreateRows(string prefix, int count, bool longNotes = false)
    {
        string[] statuses = { "OK", "Review", "Fail", "Open", "Done", "Cached", "Ready" };
        string[] categories = { "AOI", "Support", "Line", "Media", "Profile", "Export", "Theme" };
        return Enumerable.Range(1, count).Select(i =>
        {
            string status = statuses[i % statuses.Length];
            string category = categories[i % categories.Length];
            int progress = (i * 7) % 101;
            string notes = longNotes
                ? $"{prefix} satırı {i}: çok satırlı açıklama, wrap, overflow ve okunabilir satır yüksekliği davranışını göstermek için hazırlanmış uzun dokümantasyon metni."
                : $"{prefix} senaryosu için ViewGrid dokümantasyon satırı {i}.";
            return Row(i, $"{prefix} kayıt {i:000}", category, status, progress, status == "Fail" ? "İncele" : "Aç", notes);
        }).ToList();
    }
}

public sealed record DocumentationMissingScreenshotScenario(
    string Key,
    string Title,
    string TargetHeading,
    string OutputFileName,
    string Description);

public static class DocumentationMissingScreenshotScenarios
{
    private static readonly DocumentationMissingScreenshotScenario[] Items =
    {
        new("kolon-sistemi", "Kolon Sistemi", "5. Kolon Sistemi", "kolon-sistemi", "ViewGridColumn türleri, header/cell checkbox, badge, progress, rating, button ve hyperlink kolonlarını gösterir."),
        new("veri-baglama", "Veri Baglama", "6. Veri Baglama", "veri-baglama", "List<T>, BindingSource, DataTable ve virtual provider veri bağlama yollarını tek ekranda gösterir."),
        new("query-language", "Query Language", "22. Query Language", "query-language", "Power-user sorgu dili, alias, tarih ve boolean operator örneklerini gösterir."),
        new("quick-filter-bar", "Quick Filter Bar", "23. Quick Filter Bar", "quick-filter-bar", "Hızlı filtre çubuğu ve aktif filtre chip davranışını gösterir."),
        new("sorting", "Sorting", "25. Sorting", "sorting", "Sıralama glyph, ascending/descending ve aktif sort durumunu gösterir."),
        new("grouping", "Grouping", "26. Grouping", "grouping", "Durum kolonuna göre gruplama ve grup başlığı davranışını gösterir."),
        new("conditional-formatting", "Conditional Formatting", "27. Conditional Formatting", "conditional-formatting", "Koşullu format, risk rengi ve durum badge vurgusunu gösterir."),
        new("card-visual-adornments", "Card Visual Adornments", "28. Card Visual Adornments", "card-visual-adornments", "Kart görünümünde accent, status ve badge görsel zenginleştirmelerini gösterir."),
        new("card-actions", "Card Actions", "29. Card Actions", "card-actions", "Kart üstü button/hyperlink aksiyonlarını gösterir."),
        new("checkbox-layout", "Checkbox Layout", "30. Checkbox Layout", "checkbox-layout", "Header checkbox ve veri kolonlarında çoklu checkbox layout davranışını gösterir."),
        new("row-height-management", "Row Height Management", "32. Row Height Management", "row-height-management", "Satır yüksekliği, word wrap ve çok satırlı hücre davranışını gösterir."),
        new("profile-migration", "Profile Migration", "34. Profile Migration", "profile-migration", "Profil migration, layout versiyonu ve rol kapsamı durumlarını gösterir."),
        new("expression-engine", "Expression Engine", "36. Expression Engine", "expression-engine", "Expression rule ve hesaplanmış sonuç durumlarını gösterir."),
        new("formula-columns", "Formula Columns", "37. Formula Columns", "formula-columns", "Formula kolonları, hesaplanmış skorlar ve doğrulama durumlarını gösterir."),
        new("change-tracking", "Change Tracking", "38. Change Tracking", "change-tracking", "Yeni/değişti/silindi/senkronize satır durumlarını gösterir."),
        new("event-bus", "Event Bus", "39. Event Bus", "event-bus", "Publish/subscribe event stream ve audit mesajlarını gösterir."),
        new("action-pipeline", "Action Pipeline", "40. Action Pipeline", "action-pipeline", "Validate/execute/audit/notify aksiyon pipeline akışını gösterir."),
        new("copy-system-pro", "Copy System Pro", "41. Copy System Pro", "copy-system-pro", "Seçili hücre, görünür satır ve formatlı kopyalama modlarını gösterir."),
        new("virtual-mode", "Virtual Mode", "43. Virtual Mode", "virtual-mode", "Büyük veri virtual provider ve hızlı scroll hazırlığını gösterir."),
        new("stress-test", "Stress Test", "45. Stress Test", "stress-test", "Stres testi, büyük satır sayısı ve provider metriklerini gösterir."),
        new("high-dpi", "High DPI", "48. High DPI", "high-dpi", "High DPI okunurluk, toolbar ve satır ölçekleme davranışını gösterir."),
        new("localization", "Localization", "49. Localization", "localization", "Runtime dil değişimi ve fallback lokalizasyon durumlarını gösterir."),
        new("fixedfree-resize", "FixedFree Resize", "50. FixedFree Resize", "fixedfree-resize", "Sabit ve FillFreeSpace kolonların birlikte resize davranışını gösterir."),
        new("image-cache", "Image Cache", "52. Image Cache", "image-cache", "Medya thumbnail cache, lazy loading ve placeholder durumlarını gösterir."),
        new("search-everywhere", "Search Everywhere", "53. Search Everywhere", "search-everywhere", "Global arama, command palette ve search result davranışını gösterir."),
        new("inline-editing", "Inline Editing", "55. Inline Editing", "inline-editing", "F2/double-click inline editing ve editable kolonları gösterir."),
        new("drag-and-drop", "Drag and Drop", "56. Drag and Drop", "drag-and-drop", "Column reorder, row drag-drop ve drop indicator davranışını gösterir."),
        new("multi-view-sync", "Multi View Sync", "57. Multi View Sync", "multi-view-sync", "Aynı veri kaynağını paylaşan birden çok görünüm senkronizasyonunu gösterir."),
        new("state-management", "State Management", "58. State Management", "state-management", "View, filter, column layout ve theme state saklama durumlarını gösterir."),
        new("plugin-system", "Plugin System", "59. Plugin System", "plugin-system", "Plugin registry ve extension point örneklerini gösterir."),
        new("smart-suggestions", "Smart Suggestions", "61. Smart Suggestions", "smart-suggestions", "Smart filter/layout/action önerilerini gösterir.")
    };

    public static IReadOnlyList<DocumentationMissingScreenshotScenario> All => Items;

    public static DocumentationMissingScreenshotScenario Get(string key)
        => Items.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
           ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown documentation screenshot scenario.");
}

public sealed class DocumentationFeatureRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int Score { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string Spark { get; set; } = string.Empty;
    public bool Checked { get; set; }
    public bool Approved { get; set; }
    public bool Locked { get; set; }
    public DateTime CreatedAt { get; set; }
}
