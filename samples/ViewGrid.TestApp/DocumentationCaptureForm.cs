using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ViewGrid.TestApp;

public sealed partial class DocumentationCaptureForm : Form
{
    private readonly List<DocumentationCaptureItem> _items;
    private readonly ViewGridTheme _theme;

    public DocumentationCaptureForm()
    {
        InitializeComponent();
        _theme = Program.AppTheme;
        _items = CreateDefaultItems();
        ConfigureEvents();
        ApplyTheme();
        LoadItems();
    }

    private void ConfigureEvents()
    {
        btnSelectAll.Click += (_, __) => SetAllChecked(true);
        btnSelectMissing.Click += (_, __) => SetMissingDocumentationChecked();
        btnClear.Click += (_, __) => SetAllChecked(false);
        btnChooseFolder.Click += (_, __) => ChooseOutputFolder();
        btnOpenFolder.Click += (_, __) => OpenOutputFolder();
        btnCapture.Click += async (_, __) => await CaptureSelectedAsync();
    }

    private void ApplyTheme()
    {
        BackColor = _theme.BackColor;
        ForeColor = _theme.ForeColor;

        headerPanel.BackColor = _theme.HeaderBackColor;
        titleLabel.ForeColor = _theme.HeaderForeColor;
        descriptionLabel.ForeColor = _theme.HeaderForeColor;

        commandPanel.BackColor = _theme.PanelBackColor;
        outputLabel.ForeColor = _theme.ForeColor;
        outputHintLabel.ForeColor = _theme.MutedForeColor == Color.Empty ? _theme.ForeColor : _theme.MutedForeColor;
        selectionHintLabel.ForeColor = _theme.MutedForeColor == Color.Empty ? _theme.ForeColor : _theme.MutedForeColor;

        txtOutputFolder.BackColor = _theme.ControlBackColor == Color.Empty ? _theme.BackColor : _theme.ControlBackColor;
        txtOutputFolder.ForeColor = _theme.ForeColor;
        txtOutputFolder.BorderStyle = BorderStyle.FixedSingle;

        samplePanel.BackColor = _theme.BackColor;
        logPanel.BackColor = _theme.BackColor;
        sampleTitleLabel.BackColor = _theme.HeaderBackColor;
        sampleTitleLabel.ForeColor = _theme.HeaderForeColor;
        logTitleLabel.BackColor = _theme.HeaderBackColor;
        logTitleLabel.ForeColor = _theme.HeaderForeColor;

        checkedSamples.BackColor = _theme.BackColor;
        checkedSamples.ForeColor = _theme.ForeColor;
        checkedSamples.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        txtLog.BackColor = _theme.ControlBackColor == Color.Empty ? _theme.BackColor : _theme.ControlBackColor;
        txtLog.ForeColor = _theme.ForeColor;
        txtLog.BorderStyle = BorderStyle.FixedSingle;

        progressBar.ForeColor = _theme.AccentColor;
        statusLabel.ForeColor = _theme.ForeColor;
        statusLabel.BackColor = _theme.PanelBackColor;

        StyleCommandButton(btnSelectAll, primary: false);
        StyleCommandButton(btnSelectMissing, primary: false);
        StyleCommandButton(btnClear, primary: false);
        StyleCommandButton(btnChooseFolder, primary: false);
        StyleCommandButton(btnCapture, primary: true);
        StyleCommandButton(btnOpenFolder, primary: false);
    }

    private void StyleCommandButton(Button button, bool primary)
    {
        var controlBack = _theme.ControlBackColor == Color.Empty ? _theme.PanelBackColor : _theme.ControlBackColor;
        var back = primary ? _theme.AccentColor : controlBack;
        var fore = primary ? GetReadableTextColor(back) : _theme.ForeColor;

        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = back;
        button.ForeColor = fore;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? _theme.AccentColor : _theme.BorderColor;
        button.FlatAppearance.MouseOverBackColor = Blend(back, Color.White, _theme.IsDark ? 0.12 : 0.08);
        button.FlatAppearance.MouseDownBackColor = Blend(back, Color.Black, _theme.IsDark ? 0.18 : 0.10);
    }

    private static Color GetReadableTextColor(Color background)
    {
        double luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255d;
        return luminance < 0.55d ? Color.White : Color.FromArgb(25, 25, 25);
    }

    private static Color Blend(Color first, Color second, double amount)
    {
        amount = Math.Max(0, Math.Min(1, amount));
        return Color.FromArgb(
            first.A,
            (int)(first.R + (second.R - first.R) * amount),
            (int)(first.G + (second.G - first.G) * amount),
            (int)(first.B + (second.B - first.B) * amount));
    }

    private void LoadItems()
    {
        txtOutputFolder.Text = Path.Combine(Application.StartupPath, "docs", "screenshots");
        checkedSamples.Items.Clear();
        foreach (var item in _items)
            checkedSamples.Items.Add(item, true);
        progressBar.Minimum = 0;
        progressBar.Maximum = Math.Max(1, checkedSamples.Items.Count);
        AppendLog("Documentation Capture Mode hazır.");
        AppendLog("Kullanım: çıktı klasörünü seç, üretilecek ekranları işaretle ve Ekranları Üret butonuna bas.");
        AppendLog("Üretilecek dosyalar:");
        AppendLog("  • Her ekran için PNG görüntüsü");
        AppendLog("  • viewgrid-screenshot-manifest.json");
        AppendLog("  • viewgrid-screenshots.md");
        AppendLog("  • viewgrid-docx-insert-map.json");
        AppendLog("Eksik görsel üretimi için: Eksik DOCX Görselleri butonunu kullan.");
    }

    private void SetAllChecked(bool isChecked)
    {
        for (int i = 0; i < checkedSamples.Items.Count; i++)
            checkedSamples.SetItemChecked(i, isChecked);
    }

    private void SetMissingDocumentationChecked()
    {
        for (int i = 0; i < checkedSamples.Items.Count; i++)
        {
            bool isMissingShot = checkedSamples.Items[i] is DocumentationCaptureItem item
                && string.Equals(item.Category, "Missing Documentation", StringComparison.OrdinalIgnoreCase);
            checkedSamples.SetItemChecked(i, isMissingShot);
        }

        AppendLog("Sadece eksik DOCX görselleri seçildi.");
    }

    private void ChooseOutputFolder()
    {
        folderBrowserDialog.Description = "Dokümantasyon ekran görüntülerinin üretileceği klasörü seçin.";
        folderBrowserDialog.SelectedPath = Directory.Exists(txtOutputFolder.Text) ? txtOutputFolder.Text : Application.StartupPath;
        if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
            txtOutputFolder.Text = folderBrowserDialog.SelectedPath;
    }

    private void OpenOutputFolder()
    {
        Directory.CreateDirectory(txtOutputFolder.Text);
        Process.Start(new ProcessStartInfo
        {
            FileName = txtOutputFolder.Text,
            UseShellExecute = true
        });
    }

    private async Task CaptureSelectedAsync()
    {
        var selected = checkedSamples.CheckedItems.Cast<DocumentationCaptureItem>().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "En az bir örnek seçmelisin.", "ViewGrid Documentation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string outputFolder = txtOutputFolder.Text.Trim();
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            MessageBox.Show(this, "Çıktı klasörü boş olamaz.", "ViewGrid Documentation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Directory.CreateDirectory(outputFolder);
        ToggleUi(false);
        progressBar.Value = 0;
        txtLog.Clear();
        AppendLog($"Başladı: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        AppendLog($"Çıktı klasörü: {outputFolder}");

        var results = new List<DocumentationCaptureResult>();
        int index = 0;
        foreach (var item in selected)
        {
            index++;
            statusLabel.Text = $"Yakalanıyor: {item.Category} / {item.Title}";
            AppendLog($"[{index}/{selected.Count}] {item.Category} / {item.Title} / {item.ScenarioName}");
            try
            {
                var result = await CaptureOneAsync(item, outputFolder);
                results.Add(result);
                AppendLog($"  OK  -> {Path.GetFileName(result.FilePath)}");
            }
            catch (Exception ex)
            {
                var failed = DocumentationCaptureResult.Failed(item, ex.Message);
                results.Add(failed);
                AppendLog($"  HATA -> {ex.Message}");
            }
            progressBar.Value = Math.Min(progressBar.Maximum, index);
            Application.DoEvents();
        }

        WriteManifest(outputFolder, results);
        statusLabel.Text = $"Tamamlandı: {results.Count(x => x.Success)}/{results.Count} ekran görüntüsü üretildi.";
        AppendLog("Manifest ve dokümantasyon haritası üretildi.");
        ToggleUi(true);
    }

    private async Task<DocumentationCaptureResult> CaptureOneAsync(DocumentationCaptureItem item, string outputFolder)
    {
        using var form = item.CreateForm();
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(80, 80);
        form.Size = item.Size;
        form.ShowInTaskbar = false;
        form.Text = item.Title;

        form.Show(this);
        form.BringToFront();
        await WaitForUiAsync(item.WarmupMs);
        item.Prepare?.Invoke(form);
        await WaitForUiAsync(item.ScenarioWarmupMs);

        string safeName = MakeSafeFileName(item.FileName);
        string file = Path.Combine(outputFolder, safeName + ".png");
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(file, System.Drawing.Imaging.ImageFormat.Png);
        form.Close();

        return DocumentationCaptureResult.Successful(item, file);
    }

    private static async Task WaitForUiAsync(int ms)
    {
        int remaining = Math.Max(ms, 100);
        while (remaining > 0)
        {
            Application.DoEvents();
            int slice = Math.Min(remaining, 100);
            await Task.Delay(slice);
            remaining -= slice;
        }
        Application.DoEvents();
    }

    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(value.Length);
        foreach (char c in value)
            sb.Append(invalid.Contains(c) ? '_' : c);
        return sb.ToString().Trim('_');
    }

    private void WriteManifest(string outputFolder, List<DocumentationCaptureResult> results)
    {
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(outputFolder, "viewgrid-screenshot-manifest.json"), JsonSerializer.Serialize(results, jsonOptions), Encoding.UTF8);

        var md = new StringBuilder();
        md.AppendLine("# ViewGrid Documentation Screenshots");
        md.AppendLine();
        md.AppendLine("Bu dosya TestApp > Documentation Capture Mode tarafından otomatik üretilir.");
        md.AppendLine();
        foreach (var group in results.GroupBy(x => x.Category).OrderBy(x => x.Key))
        {
            md.AppendLine($"## {group.Key}");
            md.AppendLine();
            foreach (var result in group)
            {
                md.AppendLine($"### {result.Title} — {result.ScenarioName}");
                md.AppendLine();
                md.AppendLine(result.Description);
                md.AppendLine();
                if (result.Success)
                    md.AppendLine($"![{result.Title}]({Path.GetFileName(result.FilePath)})");
                else
                    md.AppendLine($"> Capture failed: {result.ErrorMessage}");
                md.AppendLine();
            }
        }
        File.WriteAllText(Path.Combine(outputFolder, "viewgrid-screenshots.md"), md.ToString(), Encoding.UTF8);

        var insertMap = results.Where(x => x.Success).Select(x => new
        {
            x.SectionKey,
            x.Category,
            x.Title,
            x.ScenarioName,
            ImagePath = Path.GetFileName(x.FilePath),
            Caption = $"Şekil - {x.Category} / {x.Title} / {x.ScenarioName}",
            TargetHeading = x.TargetHeading,
            SuggestedWidthInches = 6.4
        }).ToList();
        File.WriteAllText(Path.Combine(outputFolder, "viewgrid-docx-insert-map.json"), JsonSerializer.Serialize(insertMap, jsonOptions), Encoding.UTF8);
    }

    private void ToggleUi(bool enabled)
    {
        btnSelectAll.Enabled = enabled;
        btnSelectMissing.Enabled = enabled;
        btnClear.Enabled = enabled;
        btnChooseFolder.Enabled = enabled;
        btnCapture.Enabled = enabled;
        btnOpenFolder.Enabled = enabled;
        checkedSamples.Enabled = enabled;
    }

    private void AppendLog(string text)
    {
        txtLog.AppendText(text + Environment.NewLine);
    }

    private static List<DocumentationCaptureItem> CreateDefaultItems() => new()
    {
        // Overview / navigation
        Item("overview-example-center", "Overview", "Example Center Pro", "Ana ekran", "Tüm ViewGrid örneklerini kategori ve arama ile bulma ekranı.", () => new ExampleCenterProForm()),
        Item("overview-all-features", "Overview", "Example Center Pro", "Tüm özellikler", "Example Center içindeki özellik ağacını ve hızlı erişimi gösterir.", () => new ExampleCenterProForm(), f => ClickAny(f, "Tüm", "Features", "All")),

        // View modes
        Item("view-details", "View Modes", "Details View", "Klasik tablo", "Details görünümünde kolon, satır, filtre ve seçim davranışı.", () => new ViewModeShowcaseSampleForm(), f => SelectScenario(f, "Details")),
        Item("view-card", "View Modes", "Card View", "Kart görünümü", "CardView ile kayıtların kartlar halinde gösterimi.", () => new ViewModeShowcaseSampleForm(), f => SelectScenario(f, "Card")),
        Item("view-dashboard", "View Modes", "Dashboard View", "Dashboard", "Dashboard görünümünde özet, accent bar ve durum göstergeleri.", () => new ViewModeShowcaseSampleForm(), f => SelectScenario(f, "Dashboard")),
        Item("view-detailcard", "View Modes", "DetailCard View", "Satır satır detay", "DetailCard ile tüm kolonların satır satır okunabilir sunumu.", () => new ViewModeShowcaseSampleForm(), f => SelectScenario(f, "DetailCard", "Detail Card")),
        Item("view-poster", "View Modes", "Poster View", "Poster", "Albüm, film ve ürün posterleri için görsel ağırlıklı görünüm.", () => new ViewModeShowcaseSampleForm(), f => SelectScenario(f, "Poster")),
        Item("view-gallery", "View Modes", "Gallery View", "Galeri", "Fotoğraf, kapak ve katalog galerisi görünümü.", () => new ViewModeShowcaseSampleForm(), f => SelectScenario(f, "Gallery")),
        Item("view-mediatile", "View Modes", "MediaTile View", "MediaTile", "Spotify/Plex benzeri kompakt medya kartları.", () => new ViewModeShowcaseSampleForm(), f => SelectScenario(f, "MediaTile", "Media Tile")),
        Item("view-filmstrip", "View Modes", "FilmStrip View", "FilmStrip", "Netflix benzeri yatay medya şeridi görünümü.", () => new ViewModeShowcaseSampleForm(), f => SelectScenario(f, "FilmStrip", "Film Strip")),
        Item("view-propertycard", "View Modes", "PropertyCard", "Özellik kartı", "Nesne özelliklerinin okunabilir kart biçiminde sunumu.", () => new ViewGridV37ToV40ProExperienceSampleForm(), f => SelectScenario(f, "Property")),
        Item("view-groupcard", "View Modes", "GroupCard", "Gruplu kart", "Gruplu kart ve kategori bazlı sunum senaryosu.", () => new ViewGridV37ToV40ProExperienceSampleForm(), f => SelectScenario(f, "Group")),
        Item("view-kanban", "View Modes", "Kanban", "Workflow", "Durum kolonları ve iş akışı kartları.", () => new ViewGridV37ToV40ProExperienceSampleForm(), f => SelectScenario(f, "Kanban", "Workflow")),
        Item("view-heatmap", "View Modes", "HeatMap", "Yoğunluk", "Alarm, üretim veya metrik yoğunluğunu renklerle gösterme.", () => new ViewGridV37ToV40ProExperienceSampleForm(), f => SelectScenario(f, "Heat", "HeatMap")),
        Item("view-kpi", "View Modes", "KPI Dashboard", "KPI", "Özet metrik kartları ve dashboard göstergeleri.", () => new ViewGridV37ToV40ProExperienceSampleForm(), f => SelectScenario(f, "KPI")),
        Item("view-minichart", "View Modes", "MiniChart", "Satır içi grafik", "Satır içinde mini grafik ve trend gösterimi.", () => new ViewGridV37ToV40ProExperienceSampleForm(), f => SelectScenario(f, "Chart", "Mini")),

        // Media / Audix
        Item("media-library-overview", "Media", "Media Library", "Genel medya", "Audix/Plex benzeri albüm kapağı, film afişi ve fotoğraf odaklı medya görünümü.", () => new MediaLibrarySampleForm()),
        Item("media-library-poster", "Media", "Media Library", "Poster", "Media Library içinde poster senaryosu.", () => new MediaLibrarySampleForm(), f => SelectScenario(f, "Poster")),
        Item("media-library-gallery", "Media", "Media Library", "Gallery", "Media Library içinde galeri senaryosu.", () => new MediaLibrarySampleForm(), f => SelectScenario(f, "Gallery")),
        Item("media-library-mediatile", "Media", "Media Library", "MediaTile", "Media Library içinde MediaTile senaryosu.", () => new MediaLibrarySampleForm(), f => SelectScenario(f, "MediaTile", "Media Tile")),
        Item("media-library-filmstrip", "Media", "Media Library", "FilmStrip", "Media Library içinde FilmStrip senaryosu.", () => new MediaLibrarySampleForm(), f => SelectScenario(f, "FilmStrip", "Film Strip")),
        Item("audix-pilot-overview", "Audix", "Audix Pilot", "Albüm görünümü", "Albüm kapağı, play/pause state, now playing rozeti ve medya overlay davranışı.", () => new ViewGridV51RealUsagePilotSampleForm()),
        Item("audix-now-playing", "Audix", "Audix Pilot", "Now Playing", "Çalan kartın ayrı görünmesi, pause overlay ve now playing rozeti.", () => new ViewGridV51RealUsagePilotSampleForm(), f => SelectScenario(f, "Now", "Playing", "Play")),
        Item("audix-filmstrip", "Audix", "Audix Pilot", "FilmStrip", "Audix için yatay albüm/şarkı şeridi.", () => new ViewGridV51RealUsagePilotSampleForm(), f => SelectScenario(f, "FilmStrip", "Film Strip")),
        Item("playback-playing", "Media Playback", "Media Playback State", "Playing", "Audio/video için Playing görsel durumu.", () => new ViewGridV41MediaPlaybackSampleForm(), f => SelectScenario(f, "Playing", "Play")),
        Item("playback-paused", "Media Playback", "Media Playback State", "Paused", "Audio/video için Paused görsel durumu.", () => new ViewGridV41MediaPlaybackSampleForm(), f => SelectScenario(f, "Paused", "Pause")),
        Item("playback-loading-error", "Media Playback", "Media Playback State", "Loading ve Error", "Loading, Error ve özel medya durumlarının dokümantasyon görüntüsü.", () => new ViewGridV41MediaPlaybackSampleForm(), f => SelectScenario(f, "Loading", "Error")),

        // Filtering and interaction
        Item("filter-popup-basic", "Filtering", "Filter Popup UX", "Temel popup", "Kolon filtre popup davranışı.", () => new ViewGridV2731FilterPopupUxForm()),
        Item("filter-popup-long-values", "Filtering", "Filter Popup UX", "Uzun değerler", "Uzun değerler, tooltip ve genişletilebilir popup davranışı.", () => new ViewGridV2731FilterPopupUxForm(), f => SelectScenario(f, "Long", "Uzun", "Tooltip")),
        Item("filter-popup-resize", "Filtering", "Filter Popup UX", "Resize", "Popup boyutlandırma ve hatırlama davranışı.", () => new ViewGridV2731FilterPopupUxForm(), f => SelectScenario(f, "Resize", "Boyut")),
        Item("popular-enterprise-overview", "Enterprise", "Popular Enterprise Features", "Presetler", "Column chooser, summary, conditional format ve advanced filter presetleri.", () => new ViewGridV279PopularEnterpriseFeaturesForm()),
        Item("popular-enterprise-filter", "Enterprise", "Popular Enterprise Features", "Advanced Filter", "Advanced filter presetleri ve hızlı filtre komutları.", () => new ViewGridV279PopularEnterpriseFeaturesForm(), f => SelectScenario(f, "Filter", "Advanced")),
        Item("popular-enterprise-summary", "Enterprise", "Popular Enterprise Features", "Summary", "Footer summary, aggregation ve sayısal özetler.", () => new ViewGridV279PopularEnterpriseFeaturesForm(), f => SelectScenario(f, "Summary", "Footer", "Aggregate")),
        Item("keyboard-accessibility", "Interaction", "Keyboard Accessibility", "Klavye", "Klavye odaklı kullanım, düzenleme ve kısayollar.", () => new KeyboardAccessibilitySampleForm()),
        Item("command-frozen", "Interaction", "Frozen Columns + Commands", "Komutlar", "Sabit kolon, satır butonları ve açılır detay paneli.", () => new FrozenCommandDetailsSampleForm()),
        Item("master-detail", "Interaction", "Master Detail", "Alt detay", "Üst kayıt ve alt detay ViewGrid senaryosu.", () => new MasterDetailSampleForm()),
        Item("cell-overflow-scroll", "Interaction", "Cell Overflow Scroll", "Hücre içi scroll", "Uzun açıklama hücrelerinde satır büyütmeden hücre içi scroll.", () => new CellOverflowScrollSampleForm()),


        // Missing DOCX screenshots from section 70
        Item("missing-kolon-sistemi", "Missing Documentation", "Kolon Sistemi", "Mixed column types", "Text, checkbox, badge, progress, rating, button and hyperlink columns for section 5.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("kolon-sistemi")), outputFileName: "kolon-sistemi", targetHeading: "5. Kolon Sistemi"),
        Item("missing-veri-baglama", "Missing Documentation", "Veri Baglama", "Binding sources", "List<T>, BindingSource, DataTable and provider binding states for section 6.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("veri-baglama")), outputFileName: "veri-baglama", targetHeading: "6. Veri Baglama"),
        Item("missing-query-language", "Missing Documentation", "Query Language", "Power query", "Power-user query syntax, aliases and active result preview for section 22.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("query-language")), outputFileName: "query-language", targetHeading: "22. Query Language"),
        Item("missing-quick-filter-bar", "Missing Documentation", "Quick Filter Bar", "Live search", "Quick filter bar with active text search for section 23.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("quick-filter-bar")), outputFileName: "quick-filter-bar", targetHeading: "23. Quick Filter Bar"),
        Item("missing-sorting", "Missing Documentation", "Sorting", "Sort glyphs", "Ascending/descending sort and header glyph state for section 25.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("sorting")), outputFileName: "sorting", targetHeading: "25. Sorting"),
        Item("missing-grouping", "Missing Documentation", "Grouping", "Grouped rows", "Status-based grouping and group summary rows for section 26.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("grouping")), outputFileName: "grouping", targetHeading: "26. Grouping"),
        Item("missing-conditional-formatting", "Missing Documentation", "Conditional Formatting", "Risk coloring", "Rule-based row and cell coloring for section 27.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("conditional-formatting")), outputFileName: "conditional-formatting", targetHeading: "27. Conditional Formatting"),
        Item("missing-card-visual-adornments", "Missing Documentation", "Card Visual Adornments", "Accent and badges", "Card accent bar, status dot and badge visuals for section 28.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("card-visual-adornments")), outputFileName: "card-visual-adornments", targetHeading: "28. Card Visual Adornments"),
        Item("missing-card-actions", "Missing Documentation", "Card Actions", "Buttons and links", "Card action button and hyperlink states for section 29.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("card-actions")), outputFileName: "card-actions", targetHeading: "29. Card Actions"),
        Item("missing-checkbox-layout", "Missing Documentation", "Checkbox Layout", "Multi checkbox", "Header and cell checkbox layouts for section 30.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("checkbox-layout")), outputFileName: "checkbox-layout", targetHeading: "30. Checkbox Layout"),
        Item("missing-row-height-management", "Missing Documentation", "Row Height Management", "Compact and wrapped", "Details row height, multiline text and overflow behavior for section 32.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("row-height-management")), outputFileName: "row-height-management", targetHeading: "32. Row Height Management"),
        Item("missing-profile-migration", "Missing Documentation", "Profile Migration", "Layout versions", "Legacy profile migration, role scopes and layout versioning for section 34.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("profile-migration")), outputFileName: "profile-migration", targetHeading: "34. Profile Migration"),
        Item("missing-expression-engine", "Missing Documentation", "Expression Engine", "Calculated rules", "Expression rules and computed values for section 36.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("expression-engine")), outputFileName: "expression-engine", targetHeading: "36. Expression Engine"),
        Item("missing-formula-columns", "Missing Documentation", "Formula Columns", "Calculated columns", "Formula column results and validation states for section 37.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("formula-columns")), outputFileName: "formula-columns", targetHeading: "37. Formula Columns"),
        Item("missing-change-tracking", "Missing Documentation", "Change Tracking", "Dirty rows", "Changed/new/deleted row status preview for section 38.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("change-tracking")), outputFileName: "change-tracking", targetHeading: "38. Change Tracking"),
        Item("missing-event-bus", "Missing Documentation", "Event Bus", "Event stream", "Publish/subscribe event flow and audit messages for section 39.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("event-bus")), outputFileName: "event-bus", targetHeading: "39. Event Bus"),
        Item("missing-action-pipeline", "Missing Documentation", "Action Pipeline", "Safe actions", "Validation, command execution and audit pipeline for section 40.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("action-pipeline")), outputFileName: "action-pipeline", targetHeading: "40. Action Pipeline"),
        Item("missing-copy-system-pro", "Missing Documentation", "Copy System Pro", "Clipboard matrix", "Visible rows, selected cells and formatted copy modes for section 41.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("copy-system-pro")), outputFileName: "copy-system-pro", targetHeading: "41. Copy System Pro"),
        Item("missing-virtual-mode", "Missing Documentation", "Virtual Mode", "Large provider", "Provider-backed virtual rows and fast scrolling for section 43.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("virtual-mode")), outputFileName: "virtual-mode", targetHeading: "43. Virtual Mode"),
        Item("missing-stress-test", "Missing Documentation", "Stress Test", "Performance metrics", "Large dataset stress metrics and refresh status for section 45.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("stress-test")), outputFileName: "stress-test", targetHeading: "45. Stress Test"),
        Item("missing-high-dpi", "Missing Documentation", "High DPI", "Scaling", "High DPI sizing, readable row heights and toolbar scaling for section 48.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("high-dpi")), outputFileName: "high-dpi", targetHeading: "48. High DPI"),
        Item("missing-localization", "Missing Documentation", "Localization", "Runtime language", "Language selector and localized command labels for section 49.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("localization")), outputFileName: "localization", targetHeading: "49. Localization"),
        Item("missing-fixedfree-resize", "Missing Documentation", "FixedFree Resize", "Fill-free columns", "Fixed and fill-free column resize behavior for section 50.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("fixedfree-resize")), outputFileName: "fixedfree-resize", targetHeading: "50. FixedFree Resize"),
        Item("missing-image-cache", "Missing Documentation", "Image Cache", "Thumbnail cache", "Media thumbnail cache states and lazy loading for section 52.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("image-cache")), outputFileName: "image-cache", targetHeading: "52. Image Cache"),
        Item("missing-search-everywhere", "Missing Documentation", "Search Everywhere", "Global search", "Cross-view search and command shortcuts for section 53.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("search-everywhere")), outputFileName: "search-everywhere", targetHeading: "53. Search Everywhere"),
        Item("missing-inline-editing", "Missing Documentation", "Inline Editing", "Editable cells", "F2/double-click editing and validation feedback for section 55.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("inline-editing")), outputFileName: "inline-editing", targetHeading: "55. Inline Editing"),
        Item("missing-drag-and-drop", "Missing Documentation", "Drag and Drop", "Drop indicator", "Column and row drag-drop indicators for section 56.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("drag-and-drop")), outputFileName: "drag-and-drop", targetHeading: "56. Drag and Drop"),
        Item("missing-multi-view-sync", "Missing Documentation", "Multi View Sync", "Synchronized views", "Details/card/dashboard sync behavior for section 57.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("multi-view-sync")), outputFileName: "multi-view-sync", targetHeading: "57. Multi View Sync"),
        Item("missing-state-management", "Missing Documentation", "State Management", "View state", "Filter/layout/theme state persistence for section 58.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("state-management")), outputFileName: "state-management", targetHeading: "58. State Management"),
        Item("missing-plugin-system", "Missing Documentation", "Plugin System", "Plugin registry", "Plugin registration and extension points for section 59.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("plugin-system")), outputFileName: "plugin-system", targetHeading: "59. Plugin System"),
        Item("missing-smart-suggestions", "Missing Documentation", "Smart Suggestions", "Suggestions panel", "Smart filter/layout/action suggestions for section 61.", () => new DocumentationMissingScreenshotsForm(DocumentationMissingScreenshotScenarios.Get("smart-suggestions")), outputFileName: "smart-suggestions", targetHeading: "61. Smart Suggestions"),

        // Theme / foundation / export / hardening
        Item("theme-audit-dark", "Theme", "Theme / Accessibility Audit", "Dark", "Dark tema okunurluğu ve güvenli varsayılanlar.", () => new ViewGridV502HardeningSampleForm(), f => SelectScenario(f, "Dark", "Koyu")),
        Item("theme-audit-light", "Theme", "Theme / Accessibility Audit", "Light", "Light tema okunurluğu ve güvenli varsayılanlar.", () => new ViewGridV502HardeningSampleForm(), f => SelectScenario(f, "Light", "Açık")),
        Item("theme-audit-contrast", "Theme", "Theme / Accessibility Audit", "High Contrast", "Kontrast, button, combo ve bilgi metni okunurluk testi.", () => new ViewGridV502HardeningSampleForm(), f => SelectScenario(f, "Contrast", "Accessibility", "Okunurluk")),
        Item("foundation-stability", "Foundation", "ViewGrid Foundation", "Stability", "ViewGrid 5.0 foundation, profile ve runtime stability akışı.", () => new ViewGridV50FoundationSampleForm()),
        Item("hardening-checks", "Foundation", "Build & Runtime Hardening", "Runtime check", "Tema, medya, layout ve interaction güvenli varsayılanları.", () => new ViewGridV502HardeningSampleForm(), f => SelectScenario(f, "Check", "Hardening")),
        Item("pdf-export-suite", "Export", "PDF Export Suite", "PDF", "Details ve Card/Dashboard PDF export seçenekleri.", () => new PdfExportSuiteSampleForm()),
        Item("pdf-export-card", "Export", "PDF Export Suite", "Card PDF", "Kart ve dashboard görünümünün PDF çıktısı.", () => new PdfExportSuiteSampleForm(), f => SelectScenario(f, "Card", "Dashboard")),
        Item("pro-experience-layout", "Pro Experience", "Pro Experience Suite", "Layout", "Layout, preset ve görünüm hafızası.", () => new ViewGridV37ToV40ProExperienceSampleForm(), f => SelectScenario(f, "Layout")),
        Item("pro-experience-performance", "Pro Experience", "Pro Experience Suite", "Performance", "Performance Pro ve büyük veri hazırlıkları.", () => new ViewGridV37ToV40ProExperienceSampleForm(), f => SelectScenario(f, "Performance", "Virtual")),
        Item("pro-experience-analytics", "Pro Experience", "Pro Experience Suite", "Analytics", "Visual analytics, heatmap, KPI ve mini chart bölümleri.", () => new ViewGridV37ToV40ProExperienceSampleForm(), f => SelectScenario(f, "Analytics", "Visual")),
    };

    private static DocumentationCaptureItem Item(
        string sectionKey,
        string category,
        string title,
        string scenarioName,
        string description,
        Func<Form> factory,
        Action<Form>? prepare = null,
        int warmupMs = 700,
        int scenarioWarmupMs = 450,
        string? outputFileName = null,
        string? targetHeading = null)
        => new(sectionKey, category, title, scenarioName, description, factory, prepare, warmupMs, scenarioWarmupMs, outputFileName, targetHeading);

    private static void SelectScenario(Form form, params string[] keywords)
    {
        // Many TestApp screens contain tabs, combo boxes or buttons for sub-scenarios.
        // This helper intentionally stays generic: if the requested control exists it is selected/clicked,
        // otherwise the form is still captured as-is. This keeps documentation capture robust across samples.
        TrySelectTab(form, keywords);
        TrySelectCombo(form, keywords);
        ClickAny(form, keywords);
        form.Refresh();
        Application.DoEvents();
    }

    private static void TrySelectTab(Control root, params string[] keywords)
    {
        foreach (var tab in EnumerateControls(root).OfType<TabControl>())
        {
            foreach (TabPage page in tab.TabPages)
            {
                if (Matches(page.Text, page.Name, keywords))
                {
                    tab.SelectedTab = page;
                    return;
                }
            }
        }
    }

    private static void TrySelectCombo(Control root, params string[] keywords)
    {
        foreach (var combo in EnumerateControls(root).OfType<ComboBox>())
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                string text = combo.Items[i]?.ToString() ?? string.Empty;
                if (Matches(text, string.Empty, keywords))
                {
                    combo.SelectedIndex = i;
                    combo.Refresh();
                    return;
                }
            }
        }
    }

    private static void ClickAny(Control root, params string[] keywords)
    {
        foreach (var button in EnumerateControls(root).OfType<Button>())
        {
            if (Matches(button.Text, button.Name, keywords))
            {
                button.PerformClick();
                Application.DoEvents();
                return;
            }
        }

        foreach (var item in EnumerateControls(root).OfType<ListBox>())
        {
            for (int i = 0; i < item.Items.Count; i++)
            {
                string text = item.Items[i]?.ToString() ?? string.Empty;
                if (Matches(text, string.Empty, keywords))
                {
                    item.SelectedIndex = i;
                    return;
                }
            }
        }
    }

    private static bool Matches(string text, string name, params string[] keywords)
    {
        if (keywords == null || keywords.Length == 0)
            return false;
        string combined = ((text ?? string.Empty) + " " + (name ?? string.Empty)).ToLowerInvariant();
        return keywords.Any(k => !string.IsNullOrWhiteSpace(k) && combined.Contains(k.ToLowerInvariant()));
    }

    private static IEnumerable<Control> EnumerateControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in EnumerateControls(child))
                yield return nested;
        }
    }

}

public sealed record DocumentationCaptureItem(
    string SectionKey,
    string Category,
    string Title,
    string ScenarioName,
    string Description,
    Func<Form> Factory,
    Action<Form>? Prepare = null,
    int WarmupMs = 700,
    int ScenarioWarmupMs = 450,
    string? OutputFileName = null,
    string? TargetHeading = null)
{
    public Size Size { get; init; } = new(1280, 800);
    public string FileName => string.IsNullOrWhiteSpace(OutputFileName) ? $"viewgrid-{SectionKey}" : OutputFileName;
    public Form CreateForm() => Factory();
    public override string ToString() => $"[{Category}] {Title} / {ScenarioName}  —  {Description}";
}

public sealed record DocumentationCaptureResult(
    string SectionKey,
    string Category,
    string Title,
    string ScenarioName,
    string Description,
    string FilePath,
    bool Success,
    string? ErrorMessage,
    string? TargetHeading)
{
    public static DocumentationCaptureResult Successful(DocumentationCaptureItem item, string filePath)
        => new(item.SectionKey, item.Category, item.Title, item.ScenarioName, item.Description, filePath, true, null, item.TargetHeading);

    public static DocumentationCaptureResult Failed(DocumentationCaptureItem item, string errorMessage)
        => new(item.SectionKey, item.Category, item.Title, item.ScenarioName, item.Description, string.Empty, false, errorMessage, item.TargetHeading);
}
