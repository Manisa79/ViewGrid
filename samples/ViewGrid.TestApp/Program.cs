using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Virtualization;
using ViewGrid.Summary;
using ViewGrid.Formatting;
using ViewGrid.Details;
using ViewGrid.Theming;
using ViewGrid.Filtering;
using ViewGrid.Localization;
using ViewGridToastKind = global::ViewGrid.Notifications.ViewGridToastKind;
using System.Data;
using ViewGrid.Tree;
using System.Reflection;
using System.Diagnostics;

namespace ViewGrid.TestApp;

internal static class Program
{
    public static ViewGridTheme AppTheme { get; set; } = WindowsThemeService.CurrentTheme();

    [STAThread]
    static void Main()
    {
        InstallGlobalExceptionGuards();
        ApplicationConfiguration.Initialize();
        ConfigureStartupLanguage();
        Application.Run(new SampleHubForm());
    }


    private static void ConfigureStartupLanguage()
    {
        if (!ShowLanguageSelectionDialog(null, exitOnCancel: true))
            Environment.Exit(0);
    }

    public static bool ShowLanguageSelectionDialog(IWin32Window? owner, bool exitOnCancel = false)
    {
        string settingsFile = Path.Combine(Application.StartupPath, "settings", "viewgrid-testapp-language.json");
        var savedLanguage = StartupLanguageForm.LoadSavedLanguage(settingsFile);

        using var languageForm = new StartupLanguageForm(savedLanguage);
        var result = owner == null ? languageForm.ShowDialog() : languageForm.ShowDialog(owner);
        if (result != DialogResult.OK)
            return false;

        ViewGrid.Localization.ViewGridLocalization.Use(languageForm.SelectedLanguage);
        if (languageForm.RememberSelection)
            StartupLanguageForm.SaveLanguage(settingsFile, languageForm.SelectedLanguage);

        return true;
    }

    private static void InstallGlobalExceptionGuards()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (_, e) =>
        {
            Debug.WriteLine("ViewGrid UI exception: " + e.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Debug.WriteLine("ViewGrid domain exception: " + e.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Debug.WriteLine("ViewGrid task exception: " + e.Exception);
            e.SetObserved();
        };
    }
}

public sealed class MainForm : Form
{
    private readonly ViewGrid.Core.ViewGridControl _viewgrid = new()
    {
        Dock = DockStyle.Fill,
        EmptyListMessage = "Kayıt yok",
        CellEditActivationKey = Keys.F2,
        AllowEditAllCells = true,
        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full,
        CloseHeaderContextMenuBeforeOpeningFilterPopup = true
    };
    private readonly TextBox _search = new() { Dock = DockStyle.Top, PlaceholderText = "Genel arama..." };
    private readonly ToolStrip _tool = new();
    private readonly ToolStripLabel _status = new("Hazır");
    private readonly ToolStripLabel _versionLabel = new();
    private readonly Panel _themePreview = new() { Dock = DockStyle.Top, Height = 46, Padding = new Padding(10, 6, 10, 6) };
    private readonly Label _themeInfo = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private ViewGridTheme _currentTheme = Program.AppTheme;
    private string _themeMode = "Windows";
    private bool _suppressSearchFilter;

    public MainForm()
    {
        Text = $"{ViewGridVersionInfo.SuggestedProjectName} Test App - {GetAppVersionText()}";
        Width = 1240;
        Height = 800;
        MinimumSize = new Size(980, 620);
        ViewGridWindowChrome.ApplyOnHandleCreated(this, () => _currentTheme, true);

        BuildToolbar();
        BuildThemePreview();

        Controls.Add(_viewgrid);
        Controls.Add(_search);
        Controls.Add(_themePreview);
        Controls.Add(_tool);
        _tool.Dock = DockStyle.Top;

        _search.TextChanged += (_,__) =>
        {
            if (_suppressSearchFilter) return;
            _viewgrid.SetGlobalFilter(_search.Text);
            if (!string.IsNullOrWhiteSpace(_search.Text)) _viewgrid.JumpToFirstMatch(_search.Text);
        };
        _viewgrid.ButtonClick += (_, e) => MessageBox.Show(this, $"Buton: {((DemoRow)e.RowObject).Name}", "ViewGridControl");
        _viewgrid.CellValueChanged += (_, e) => _status.Text = $"Güncellendi: {e.Column.Header} = {e.NewValue}";
        _viewgrid.ShowSummaryFooter = true;
        _viewgrid.EnableRowDetails = true;
        _viewgrid.FilterMenuMode = ViewGridFilterMenuMode.Both;
        ConfigureMillionRowFiltering(_viewgrid);
        _viewgrid.SetRowDetailsProvider(CreateDetailsProvider());

        WindowsThemeService.ThemeChanged += (_,__) =>
        {
            if (_themeMode == "Windows")
                ApplyAppTheme(WindowsThemeService.CurrentTheme(), "Windows otomatik");
        };

        ConfigureColumns();
        ApplyAppTheme(_currentTheme, "Windows otomatik");
        Shown += async (_,__) => await LoadStartupRows();
    }

    internal static void ConfigureMillionRowFiltering(ViewGrid.Core.ViewGridControl grid)
    {
        grid.FastFilterMenuForHugeLists = true;
        grid.FastFilterMenuInitialScanRows = 300;
        grid.FastFilterPopupPreviewRows = 300;
        grid.MaxFilterDistinctScanRows = 1_000_000;
        grid.MaxVirtualFilterScanRows = 1_000_000;
        grid.MaxAsyncFilterDistinctScanRows = 1_000_000;
        grid.FastFilterMenuSearchScanRows = 1_000_000;
        grid.AsyncLoadFullFilterValues = true;
        grid.TypedFilterSearchesAllRows = true;
        grid.MaxEmbeddedFilterVisibleValues = 2_000;
    }

    private static string GetAppVersionText()
    {
        var asm = Assembly.GetExecutingAssembly();
        return asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? asm.GetName().Version?.ToString()
            ?? "dev";
    }

    private void ShowVersionInfo()
    {
        MessageBox.Show(this,
            $"Proje adı önerisi: {ViewGridVersionInfo.SuggestedProjectName}\n" +
            $"DLL: {ViewGridVersionInfo.DisplayText}\n" +
            $"Test App: {GetAppVersionText()}\n\n" +
            "Tüm değişiklik geçmişi artık tek README.md altında tutulur.",
            "Sürüm Bilgisi", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BuildToolbar()
    {
        _tool.GripStyle = ToolStripGripStyle.Hidden;
        _tool.Items.Add(new ToolStripButton("150.000.000 Virtual", null, (_,__) => LoadVirtualRows(150_000_000)));
        _tool.Items.Add(new ToolStripButton("999.000.000 Virtual", null, (_,__) => LoadVirtualRows(999_000_000)));
        _tool.Items.Add(new ToolStripButton("Filtreleri Temizle", null, (_,__) => ClearAllFiltersFast()));
        _tool.Items.Add(new ToolStripButton("Header Sağ Tık / Filtre Popup", null, (_,__) => _viewgrid.ShowHeaderContextMenuForAspect(nameof(DemoRow.Name))));
        _tool.Items.Add(new ToolStripButton("Ad filtre penceresi", null, (_,__) => _viewgrid.ShowFilterMenuForAspect(nameof(DemoRow.Name))));
        _tool.Items.Add(new ToolStripButton("Kolon Seç", null, (_,__) => _viewgrid.ShowColumnChooser()));

        var samplesMenu = new ToolStripDropDownButton("ViewGrid");
        BuildProductViewGridMenu(samplesMenu);
        _tool.Items.Add(samplesMenu);

        var viewMenu = new ToolStripDropDownButton("Görünüm");
        viewMenu.DropDownItems.Add("Kompakt", null, (_,__) => { _viewgrid.SetViewMode(ViewGridMode.ExtraLargeIcons); _status.Text = "Görünüm: Kompakt"; });
        viewMenu.DropDownItems.Add("Standart", null, (_,__) => { _viewgrid.SetViewMode(ViewGridMode.LargeIcons); _status.Text = "Görünüm: Standart"; });
        viewMenu.DropDownItems.Add("Geniş", null, (_,__) => { _viewgrid.SetViewMode(ViewGridMode.MediumIcons); _status.Text = "Görünüm: Geniş"; });
        viewMenu.DropDownItems.Add("Liste", null, (_,__) => { _viewgrid.SetViewMode(ViewGridMode.List); _status.Text = "Görünüm: Liste"; });
        viewMenu.DropDownItems.Add("Kart", null, (_,__) => { _viewgrid.SetViewMode(ViewGridMode.Tile); _status.Text = "Görünüm: Kart"; });
        viewMenu.DropDownItems.Add("Geniş Kart", null, (_,__) => { _viewgrid.SetViewMode(ViewGridMode.LargeCard); _status.Text = "Görünüm: Geniş Kart"; });
        viewMenu.DropDownItems.Add("Dashboard Kart", null, (_,__) => { _viewgrid.SetViewMode(ViewGridMode.DashboardCard); _status.Text = "Görünüm: Dashboard Kart"; });
        viewMenu.DropDownItems.Add("Satır Kart", null, (_,__) => { _viewgrid.SetViewMode(ViewGridMode.RowCard); _status.Text = "Görünüm: Satır Kart"; });
        viewMenu.DropDownItems.Add("DetailCard", null, (_,__) => { _viewgrid.SetViewMode(ViewGridMode.DetailCard); _status.Text = "Görünüm: DetailCard"; });
        viewMenu.DropDownItems.Add("Poster", null, (_,__) => { _viewgrid.TilePosterMode = true; _viewgrid.SetViewMode(ViewGridMode.Poster); _status.Text = "Görünüm: Poster"; });
        viewMenu.DropDownItems.Add("MediaTile", null, (_,__) => { _viewgrid.TilePosterMode = true; _viewgrid.SetViewMode(ViewGridMode.MediaTile); _status.Text = "Görünüm: MediaTile"; });
        viewMenu.DropDownItems.Add("Gallery", null, (_,__) => { _viewgrid.TilePosterMode = true; _viewgrid.SetViewMode(ViewGridMode.Gallery); _status.Text = "Görünüm: Gallery"; });
        viewMenu.DropDownItems.Add("FilmStrip", null, (_,__) => { _viewgrid.TilePosterMode = true; _viewgrid.SetViewMode(ViewGridMode.FilmStrip); _status.Text = "Görünüm: FilmStrip"; });
        viewMenu.DropDownItems.Add("GroupCard", null, (_,__) => { _viewgrid.TilePosterMode = false; _viewgrid.SetViewMode(ViewGridMode.GroupCard); _status.Text = "Görünüm: GroupCard"; });
        viewMenu.DropDownItems.Add("PropertyCard", null, (_,__) => { _viewgrid.TilePosterMode = false; _viewgrid.SetViewMode(ViewGridMode.PropertyCard); _status.Text = "Görünüm: PropertyCard"; });
        viewMenu.DropDownItems.Add("KPI Dashboard", null, (_,__) => { _viewgrid.TilePosterMode = false; _viewgrid.SetViewMode(ViewGridMode.KpiDashboard); _status.Text = "Görünüm: KPI Dashboard"; });
        viewMenu.DropDownItems.Add("HeatMap", null, (_,__) => { _viewgrid.TilePosterMode = false; _viewgrid.SetViewMode(ViewGridMode.HeatMap); _status.Text = "Görünüm: HeatMap"; });
        viewMenu.DropDownItems.Add("MiniChart", null, (_,__) => { _viewgrid.TilePosterMode = false; _viewgrid.SetViewMode(ViewGridMode.MiniChart); _status.Text = "Görünüm: MiniChart"; });
        viewMenu.DropDownItems.Add("RowPreview", null, (_,__) => { _viewgrid.TilePosterMode = false; _viewgrid.SetViewMode(ViewGridMode.RowPreview); _status.Text = "Görünüm: RowPreview"; });
        viewMenu.DropDownItems.Add("MasterDetail", null, (_,__) => { _viewgrid.TilePosterMode = false; _viewgrid.SetViewMode(ViewGridMode.MasterDetail); _status.Text = "Görünüm: MasterDetail"; });
        viewMenu.DropDownItems.Add("Kanban", null, (_,__) => { _viewgrid.TilePosterMode = false; _viewgrid.SetViewMode(ViewGridMode.Kanban); _status.Text = "Görünüm: Kanban"; });
        viewMenu.DropDownItems.Add("Zaman Akışı", null, (_,__) => { _viewgrid.SetViewMode(ViewGridMode.Timeline); _status.Text = "Görünüm: Zaman Akışı"; });
        viewMenu.DropDownItems.Add("Detay", null, (_,__) => { _viewgrid.SetViewMode(ViewGridMode.Details); _status.Text = "Görünüm: Detay"; });
        _tool.Items.Add(viewMenu);

        var groupMenu = new ToolStripDropDownButton("Gruplama");
        groupMenu.DropDownItems.Add("Duruma göre grupla", null, (_,__) => { _viewgrid.SetGroupBy(nameof(DemoRow.State)); _status.Text = "Gruplama: Durum"; });
        groupMenu.DropDownItems.Add("Mesleğe göre grupla", null, (_,__) => { _viewgrid.SetGroupBy(nameof(DemoRow.Occupation)); _status.Text = "Gruplama: Meslek"; });
        groupMenu.DropDownItems.Add("Tüm grupları daralt", null, (_,__) => { _viewgrid.CollapseAllGroups(); _status.Text = "Tüm gruplar daraltıldı"; });
        groupMenu.DropDownItems.Add("Tüm grupları aç", null, (_,__) => { _viewgrid.ExpandAllGroups(); _status.Text = "Tüm gruplar açıldı"; });
        groupMenu.DropDownItems.Add("Gruplamayı kaldır", null, (_,__) => { _viewgrid.ClearGrouping(); _status.Text = "Gruplama kapalı"; });
        _tool.Items.Add(groupMenu);

        var filterStyleMenu = new ToolStripDropDownButton("Filtre Stili");
        filterStyleMenu.DropDownItems.Add("Popup menü", null, (_,__) => { _viewgrid.FilterMenuMode = ViewGridFilterMenuMode.PopupMenu; _status.Text = "Filtre stili: Popup menü"; });
        filterStyleMenu.DropDownItems.Add("Ayrı pencere", null, (_,__) => { _viewgrid.FilterMenuMode = ViewGridFilterMenuMode.ModalWindow; _status.Text = "Filtre stili: Ayrı pencere"; });
        filterStyleMenu.DropDownItems.Add("İkisi birlikte", null, (_,__) => { _viewgrid.FilterMenuMode = ViewGridFilterMenuMode.Both; _status.Text = "Filtre stili: İkisi birlikte"; });
        _tool.Items.Add(filterStyleMenu);

        var contextMenuMode = new ToolStripDropDownButton("Başlık Sağ Tık");
        contextMenuMode.DropDownItems.Add("Menü yok", null, (_,__) => { _viewgrid.HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.None; _status.Text = "Başlık sağ tık: menü yok"; });
        contextMenuMode.DropDownItems.Add("Sadece filtre popup", null, (_,__) => { _viewgrid.HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.FilterOnly; _status.Text = "Başlık sağ tık: sadece filtre"; });
        contextMenuMode.DropDownItems.Add("Tam menü", null, (_,__) => { _viewgrid.HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full; _status.Text = "Başlık sağ tık: tam menü"; });
        contextMenuMode.DropDownItems.Add(new ToolStripSeparator());
        contextMenuMode.DropDownItems.Add("Sıralama öğelerini aç/kapat", null, (_,__) => { _viewgrid.ShowHeaderMenuSortItems = !_viewgrid.ShowHeaderMenuSortItems; _status.Text = $"Sıralama menüsü: {_viewgrid.ShowHeaderMenuSortItems}"; });
        contextMenuMode.DropDownItems.Add("Layout öğelerini aç/kapat", null, (_,__) => { _viewgrid.ShowHeaderMenuLayoutItems = !_viewgrid.ShowHeaderMenuLayoutItems; _status.Text = $"Layout menüsü: {_viewgrid.ShowHeaderMenuLayoutItems}"; });
        contextMenuMode.DropDownItems.Add("Gruplama öğelerini aç/kapat", null, (_,__) => { _viewgrid.ShowHeaderMenuGroupingItems = !_viewgrid.ShowHeaderMenuGroupingItems; _status.Text = $"Gruplama menüsü: {_viewgrid.ShowHeaderMenuGroupingItems}"; });
        contextMenuMode.DropDownItems.Add(new ToolStripSeparator());
        contextMenuMode.DropDownItems.Add("Profil: Full", null, (_,__) => { _viewgrid.ApplyMenuProfile(ViewGridMenuProfile.Full); _status.Text = "Menü profili: Full"; });
        contextMenuMode.DropDownItems.Add("Profil: Standard", null, (_,__) => { _viewgrid.ApplyMenuProfile(ViewGridMenuProfile.Standard); _status.Text = "Menü profili: Standard"; });
        contextMenuMode.DropDownItems.Add("Profil: Minimal", null, (_,__) => { _viewgrid.ApplyMenuProfile(ViewGridMenuProfile.Minimal); _status.Text = "Menü profili: Minimal"; });
        contextMenuMode.DropDownItems.Add("Profil: ReadOnly", null, (_,__) => { _viewgrid.ApplyMenuProfile(ViewGridMenuProfile.ReadOnly); _status.Text = "Menü profili: ReadOnly"; });
        contextMenuMode.DropDownItems.Add("Profil: None", null, (_,__) => { _viewgrid.ApplyMenuProfile(ViewGridMenuProfile.None); _status.Text = "Menü profili: None / ViewGrid menüleri kapalı"; });
        contextMenuMode.DropDownItems.Add("Boş liste bilgisini aç/kapat", null, (_,__) => { _viewgrid.ShowEmptyListMessage = !_viewgrid.ShowEmptyListMessage; _viewgrid.Invalidate(); _status.Text = $"Boş liste bilgisi: {_viewgrid.ShowEmptyListMessage}"; });
        _tool.Items.Add(contextMenuMode);

        var colorMenu = new ToolStripDropDownButton("Satır Renkleri");
        void AddRowPreset(string text, ViewGridRowColorPreset preset, double strength = 0.18)
        {
            colorMenu.DropDownItems.Add(text, null, (_,__) =>
            {
                _viewgrid.RowBackColorGetter = null;
                _viewgrid.RowForeColorGetter = null;
                _viewgrid.RowColorAspectName = nameof(DemoRow.State);
                _viewgrid.RowColorStrength = strength;
                _viewgrid.RowColorPreset = preset;
                _viewgrid.CustomGroupBackColor = Color.Empty;
                _viewgrid.Invalidate();
                _status.Text = "Satır renkleri: " + text;
            });
        }
        AddRowPreset("Tema varsayılanı", ViewGridRowColorPreset.ThemeDefault);
        AddRowPreset("Yumuşak zebra", ViewGridRowColorPreset.SubtleZebra, 0.12);
        AddRowPreset("Tema vurgu satırları", ViewGridRowColorPreset.SoftAccent, 0.13);
        AddRowPreset("AOI risk / durum uyumlu", ViewGridRowColorPreset.AOIRisk, 0.22);
        AddRowPreset("Durum bazlı pastel", ViewGridRowColorPreset.StatusPills, 0.18);
        AddRowPreset("Severity bantları", ViewGridRowColorPreset.SeverityBands, 0.24);
        AddRowPreset("Pastel kart görünümü", ViewGridRowColorPreset.PastelCards, 0.16);
        AddRowPreset("Focus glow", ViewGridRowColorPreset.FocusGlow, 0.18);
        colorMenu.DropDownItems.Add(new ToolStripSeparator());
        colorMenu.DropDownItems.Add("Özel: Fail yazısı vurgulu", null, (_,__) =>
        {
            _viewgrid.RowColorPreset = ViewGridRowColorPreset.AOIRisk;
            _viewgrid.RowColorAspectName = nameof(DemoRow.State);
            _viewgrid.RowColorStrength = 0.24;
            _viewgrid.RowForeColorGetter = row => ((DemoRow)row).State == "Fail" ? (_currentTheme.IsDark ? Color.FromArgb(255, 190, 190) : Color.FromArgb(140, 0, 0)) : null;
            _viewgrid.Invalidate();
            _status.Text = "Satır renkleri: Fail yazısı vurgulu";
        });
        _tool.Items.Add(colorMenu);

        _tool.Items.Add(new ToolStripButton("Bul", null, (_,__) =>
        {
            if (!_viewgrid.FindNext(_search.Text))
                _status.Text = "Eşleşme bulunamadı";
            else
                _status.Text = "Sonraki eşleşmeye gidildi";
        }));
        _tool.Items.Add(new ToolStripButton("Önceki", null, (_,__) =>
        {
            if (!_viewgrid.FindPrevious(_search.Text))
                _status.Text = "Eşleşme bulunamadı";
            else
                _status.Text = "Önceki eşleşmeye gidildi";
        }));
        _tool.Items.Add(new ToolStripButton("CSV Export", null, (_,__) =>
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ViewGrid.csv");
            try
            {
                var actualPath = _viewgrid.ExportVisibleCsv(path);
                MessageBox.Show(this, "CSV oluşturuldu:\n" + actualPath, "Export");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "CSV dışa aktarılırken hata oluştu:\n" + ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }));
        _tool.Items.Add(new ToolStripButton("Excel Export", null, (_,__) =>
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ViewGrid.xlsx");
            try
            {
                var actualPath = _viewgrid.ExportVisibleExcel(path, "ViewGridControl");
                MessageBox.Show(this, "Excel çıktısı oluşturuldu:\n" + actualPath, "Export");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Excel dışa aktarılırken hata oluştu:\n" + ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }));
        _tool.Items.Add(new ToolStripButton("Yazdırma Önizleme", null, (_,__) => _viewgrid.ShowPrintPreview("ViewGridControl Ana Örnek")));
        _tool.Items.Add(new ToolStripButton("Yazdır", null, (_,__) => _viewgrid.Print("ViewGridControl Ana Örnek")));
        _tool.Items.Add(new ToolStripButton("Layout Kaydet", null, (_,__) => _viewgrid.SaveLayout(Path.Combine(Application.StartupPath, "viewgrid-layout.json"))));
        _tool.Items.Add(new ToolStripButton("Layout Yükle", null, (_,__) => _viewgrid.LoadLayout(Path.Combine(Application.StartupPath, "viewgrid-layout.json"))));

        var themeMenu = new ToolStripDropDownButton("Tema");
        themeMenu.DropDownItems.Add("Windows otomatik", null, (_,__) => { _themeMode = "Windows"; ApplyAppTheme(WindowsThemeService.CurrentTheme(), "Windows otomatik"); });
        themeMenu.DropDownItems.Add("Açık tema", null, (_,__) => { _themeMode = "Light"; ApplyAppTheme(ViewGridTheme.LightTheme(), "Açık tema"); });
        themeMenu.DropDownItems.Add("Koyu tema", null, (_,__) => { _themeMode = "Dark"; ApplyAppTheme(ViewGridTheme.DarkTheme(), "Koyu tema"); });
        themeMenu.DropDownItems.Add("Fluent açık + acrylic", null, (_,__) => { _themeMode = "FluentLight"; ApplyAppTheme(ViewGridTheme.FluentLightTheme(), "Fluent açık + acrylic"); });
        themeMenu.DropDownItems.Add("Fluent koyu + acrylic", null, (_,__) => { _themeMode = "FluentDark"; ApplyAppTheme(ViewGridTheme.FluentDarkTheme(), "Fluent koyu + acrylic"); });
        themeMenu.DropDownItems.Add(new ToolStripSeparator());
        themeMenu.DropDownItems.Add("Midnight Purple", null, (_,__) => { _themeMode = "MidnightPurple"; _viewgrid.ThemePreset = ViewGridThemePreset.MidnightPurple; ApplyAppTheme(ViewGridTheme.FromPreset(ViewGridThemePreset.MidnightPurple), "Midnight Purple"); });
        themeMenu.DropDownItems.Add("Slate Blue", null, (_,__) => { _themeMode = "SlateBlue"; _viewgrid.ThemePreset = ViewGridThemePreset.SlateBlue; ApplyAppTheme(ViewGridTheme.FromPreset(ViewGridThemePreset.SlateBlue), "Slate Blue"); });
        themeMenu.DropDownItems.Add("Graphite", null, (_,__) => { _themeMode = "Graphite"; _viewgrid.ThemePreset = ViewGridThemePreset.Graphite; ApplyAppTheme(ViewGridTheme.FromPreset(ViewGridThemePreset.Graphite), "Graphite"); });
        themeMenu.DropDownItems.Add("Emerald", null, (_,__) => { _themeMode = "Emerald"; _viewgrid.ThemePreset = ViewGridThemePreset.Emerald; ApplyAppTheme(ViewGridTheme.FromPreset(ViewGridThemePreset.Emerald), "Emerald"); });
        themeMenu.DropDownItems.Add("Warm Sand", null, (_,__) => { _themeMode = "WarmSand"; _viewgrid.ThemePreset = ViewGridThemePreset.WarmSand; ApplyAppTheme(ViewGridTheme.FromPreset(ViewGridThemePreset.WarmSand), "Warm Sand"); });
        themeMenu.DropDownItems.Add("Ocean", null, (_,__) => { _themeMode = "Ocean"; _viewgrid.ThemePreset = ViewGridThemePreset.Ocean; ApplyAppTheme(ViewGridTheme.FromPreset(ViewGridThemePreset.Ocean), "Ocean"); });
        themeMenu.DropDownItems.Add("Rose Quartz", null, (_,__) => { _themeMode = "RoseQuartz"; _viewgrid.ThemePreset = ViewGridThemePreset.RoseQuartz; ApplyAppTheme(ViewGridTheme.FromPreset(ViewGridThemePreset.RoseQuartz), "Rose Quartz"); });
        themeMenu.DropDownItems.Add("Cyber Neon", null, (_,__) => { _themeMode = "CyberNeon"; _viewgrid.ThemePreset = ViewGridThemePreset.CyberNeon; ApplyAppTheme(ViewGridTheme.FromPreset(ViewGridThemePreset.CyberNeon), "Cyber Neon"); });
        themeMenu.DropDownItems.Add("Nord", null, (_,__) => { _themeMode = "Nord"; _viewgrid.ThemePreset = ViewGridThemePreset.Nord; ApplyAppTheme(ViewGridTheme.FromPreset(ViewGridThemePreset.Nord), "Nord"); });
        themeMenu.DropDownItems.Add("Olive", null, (_,__) => { _themeMode = "Olive"; _viewgrid.ThemePreset = ViewGridThemePreset.Olive; ApplyAppTheme(ViewGridTheme.FromPreset(ViewGridThemePreset.Olive), "Olive"); });
        themeMenu.DropDownItems.Add("Steel", null, (_,__) => { _themeMode = "Steel"; _viewgrid.ThemePreset = ViewGridThemePreset.Steel; ApplyAppTheme(ViewGridTheme.FromPreset(ViewGridThemePreset.Steel), "Steel"); });
        themeMenu.DropDownItems.Add(new ToolStripSeparator());
        themeMenu.DropDownItems.Add("AOI mor vurgu", null, (_,__) => { _themeMode = "Aoi"; ApplyAppTheme(CreateAoiTheme(), "AOI mor vurgu"); });
        themeMenu.DropDownItems.Add("Yüksek kontrast", null, (_,__) => { _themeMode = "Contrast"; ApplyAppTheme(CreateHighContrastTheme(), "Yüksek kontrast"); });
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(themeMenu);
        _tool.Items.Add(new ToolStripButton("Sürüm", null, (_,__) => ShowVersionInfo()));
        _tool.Items.Add(new ToolStripSeparator());
        _versionLabel.Text = $"DLL: {ViewGridVersionInfo.InformationalVersion} | App: {GetAppVersionText()}";
        _tool.Items.Add(_versionLabel);
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(_status);
    }

    private static void AddMenuItem(ToolStripItemCollection items, string text, Action action)
    {
        items.Add(text, null, (_, __) => action());
    }

    private static ToolStripMenuItem AddSubMenu(ToolStripItemCollection items, string text)
    {
        var menu = new ToolStripMenuItem(text);
        items.Add(menu);
        return menu;
    }

    private void BuildProductViewGridMenu(ToolStripDropDownButton root)
    {
        // v51.1: gerçek uygulama menüsünü sade tut; tüm teknik örnekleri Developer Center altına taşı.
        AddMenuItem(root.DropDownItems, "Example Center / Arama", () => new SampleHubForm().Show());
        AddMenuItem(root.DropDownItems, "Audix Medya Pilot", () => new ViewGridV51RealUsagePilotSampleForm().Show());
        AddMenuItem(root.DropDownItems, "Theme Audit / Okunurluk", () => new ViewGridV51RealUsagePilotSampleForm().Show());
        AddMenuItem(root.DropDownItems, "Media Playback State", () => new ViewGridV41MediaPlaybackSampleForm().Show());
        root.DropDownItems.Add(new ToolStripSeparator());

        var showcase = AddSubMenu(root.DropDownItems, "Showcase");
        AddMenuItem(showcase.DropDownItems, "Medya Vitrini", () => new MediaLibrarySampleForm().Show());
        AddMenuItem(showcase.DropDownItems, "Tüm Görünüm Modları", () => new ViewModeShowcaseSampleForm().Show());
        AddMenuItem(showcase.DropDownItems, "Poster / Gallery / FilmStrip", () => new PosterGallerySampleForm().Show());
        AddMenuItem(showcase.DropDownItems, "MasterData Senaryoları", () => new MasterDataScenarioSampleForm().Show());

        var developer = AddSubMenu(root.DropDownItems, "Developer Center / Tüm Teknik Örnekler");
        BuildDeveloperViewGridMenu(developer);
    }

    private void BuildDeveloperViewGridMenu(ToolStripMenuItem developer)
    {
        AddMenuItem(developer.DropDownItems, "Tüm formlar / Sample Hub", () => new SampleHubForm().Show());
        AddMenuItem(developer.DropDownItems, "Tüm özellikler ana örneği", () => new MainForm().Show());
        AddMenuItem(developer.DropDownItems, "Özellik test merkezi", () => new FeatureVerificationSuiteForm().Show());
        developer.DropDownItems.Add(new ToolStripSeparator());

        var latest = AddSubMenu(developer.DropDownItems, "5.x Stabilite / Kullanım");
        AddMenuItem(latest.DropDownItems, "ViewGrid 5.1 Audix Pilot / Theme Audit", () => new ViewGridV51RealUsagePilotSampleForm().Show());
        AddMenuItem(latest.DropDownItems, "ViewGrid v50.2 Build & Runtime Hardening", () => new ViewGridV502HardeningSampleForm().Show());
        AddMenuItem(latest.DropDownItems, "ViewGrid 5.0 Foundation / Stability", () => new ViewGridV50FoundationSampleForm().Show());
        AddMenuItem(latest.DropDownItems, "V41 Media Playback State", () => new ViewGridV41MediaPlaybackSampleForm().Show());
        AddMenuItem(latest.DropDownItems, "V37-V40 Pro Experience", () => new ViewGridV37ToV40ProExperienceSampleForm().Show());

        var media = AddSubMenu(developer.DropDownItems, "Media / Audix");
        AddMenuItem(media.DropDownItems, "V30 Visual Experience Suite", () => new MediaLibrarySampleForm().Show());
        AddMenuItem(media.DropDownItems, "V30 Gelişmiş Görünüm Vitrini", () => new ViewModeShowcaseSampleForm().Show());
        AddMenuItem(media.DropDownItems, "V30 Poster / Gallery / FilmStrip", () => new PosterGallerySampleForm().Show());
        AddMenuItem(media.DropDownItems, "Media Library / Albüm-Film-Fotoğraf", () => new MediaLibrarySampleForm().Show());

        var core = AddSubMenu(developer.DropDownItems, "Core Grid");
        AddMenuItem(core.DropDownItems, "Filtreleme detay örneği", () => new FilteringSampleForm().Show());
        AddMenuItem(core.DropDownItems, "Gruplama detay örneği", () => new GroupingSampleForm().Show());
        AddMenuItem(core.DropDownItems, "Modern ProgressBar örneği", () => new ProgressBarSampleForm().Show());
        AddMenuItem(core.DropDownItems, "Yazdırma / Önizleme örneği", () => new PrintPreviewSampleForm().Show());
        AddMenuItem(core.DropDownItems, "Görünüm modları örneği", () => new ViewModesSampleForm().Show());
        AddMenuItem(core.DropDownItems, "Kart görünümü örneği", () => new TileSampleForm().Show());
        AddMenuItem(core.DropDownItems, "Geniş Kart / Ticket görünümü örneği", () => new LargeCardSampleForm().Show());
        AddMenuItem(core.DropDownItems, "Çok satırlı hücre örneği", () => new MultilineCellsSampleForm().Show());
        AddMenuItem(core.DropDownItems, "Highlight arama örneği", () => new HighlightSearchSampleForm().Show());
        AddMenuItem(core.DropDownItems, "Satır renklendirme örneği", () => new ColoringSampleForm().Show());
        AddMenuItem(core.DropDownItems, "Image + Combo hücre örneği", () => new ImageComboSampleForm().Show());

        var designer = AddSubMenu(developer.DropDownItems, "Designer / API / Uyumluluk");
        AddMenuItem(designer.DropDownItems, "Kolon Designer Editor Smoke Test", () => new ViewGridColumnDesignerEditorSmokeForm().Show());
        AddMenuItem(designer.DropDownItems, "Designer önizleme örneği", () => new DesignerPreviewSampleForm().Show());
        AddMenuItem(designer.DropDownItems, "Designer + Menü Merge örneği", () => new DesignerMenuMergeSampleForm().Show());
        AddMenuItem(designer.DropDownItems, "Designer/API özellik örneği", () => new DesignerApiSampleForm().Show());
        AddMenuItem(designer.DropDownItems, "ViewGrid Designer Smart Tag", () => new ViewGridDesignerSmartTagSampleForm().Show());
        AddMenuItem(designer.DropDownItems, "ViewGrid/GLV uyumluluk / SetObjects", () => new ViewGridCompatSampleForm().Show());
        AddMenuItem(designer.DropDownItems, "ViewGrid kolon özellikleri uyumluluk", () => new ViewGridColumnCompatibilitySampleForm().Show());
        AddMenuItem(designer.DropDownItems, "OLV ekstra uyumluluk yardımcıları", () => new OLVExtrasSampleForm().Show());

        var enterprise = AddSubMenu(developer.DropDownItems, "Enterprise / Performance");
        AddMenuItem(enterprise.DropDownItems, "MasterData görünüm senaryoları", () => new MasterDataScenarioSampleForm().Show());
        AddMenuItem(enterprise.DropDownItems, "v28.7 PDF Export Suite", () => new PdfExportSuiteSampleForm().Show());
        AddMenuItem(enterprise.DropDownItems, "v28.8 Hücre İçi Scroll", () => new CellOverflowScrollSampleForm().Show());
        AddMenuItem(enterprise.DropDownItems, "999M sanal liste örneği", () => new MillionRowsSampleForm().Show());
        AddMenuItem(enterprise.DropDownItems, "Klavye erişimi / shortcut testi", () => new KeyboardAccessibilitySampleForm().Show());
        AddMenuItem(enterprise.DropDownItems, "Column Manager / kolon düzeni", () => new ColumnManagerSampleForm().Show());
        AddMenuItem(enterprise.DropDownItems, "Menu Visibility Manager", () => new MenuVisibilityManagerSampleForm().Show());
        AddMenuItem(enterprise.DropDownItems, "Dil / Localization test", () => new LocalizationSampleForm().Show());
    }

    private void BuildThemePreview()
    {
        var accentDot = new Panel { Width = 22, Dock = DockStyle.Left, Margin = new Padding(0, 0, 10, 0) };
        accentDot.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var b = new SolidBrush(_currentTheme.AccentColor);
            e.Graphics.FillEllipse(b, 2, 6, 18, 18);
        };
        _themePreview.Controls.Add(_themeInfo);
        _themePreview.Controls.Add(accentDot);
    }

    private ViewGridRowDetailsProvider CreateDetailsProvider() => new()
    {
        PreferredHeight = 82,
        CreateDetailsControl = row =>
        {
            var r = (DemoRow)row;
            return new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                BackColor = Blend(_currentTheme.BackColor, _currentTheme.AccentColor, _currentTheme.IsDark ? 0.14 : 0.08),
                ForeColor = _currentTheme.ForeColor,
                Text = $"Detay paneli: Id={r.Id}, Ad={r.Name}, Durum={r.State}, Puan={r.Rating}  |  Tema rengi ve okunabilirlik burada da uygulanır.",
                TextAlign = ContentAlignment.MiddleLeft
            };
        }
    };

    private void ConfigureColumns()
    {
        _viewgrid.Columns.Add(new ViewGridColumn("✓", nameof(DemoRow.Checked), 42)
        {
            Kind = ViewGridColumnKind.CheckBox,
            HeaderCheckBox = true,
            HeaderCheckBoxThreeState = true,
            AspectPutter = (row, value) => ((DemoRow)row).SetChecked(Convert.ToBoolean(value))
        });
        _viewgrid.Columns.Add(new ViewGridColumn("İşaret", nameof(DemoRow.NeedsReview), 58)
        {
            Kind = ViewGridColumnKind.CheckBox,
            AspectPutter = (row, value) => ((DemoRow)row).SetNeedsReview(Convert.ToBoolean(value))
        });
        _viewgrid.Columns.Add(new ViewGridColumn("Id", nameof(DemoRow.Id), 70));
        _viewgrid.Columns.Add(new ViewGridColumn("İkon", nameof(DemoRow.State), 70)
        {
            Kind = ViewGridColumnKind.Icon,
            ImageGetter = row => CreateStateIcon(((DemoRow)row).State),
            AspectGetter = row => ((DemoRow)row).State
        });
        _viewgrid.Columns.Add(new ViewGridColumn("Ad", nameof(DemoRow.Name), 220) { Editable = true, FillFreeSpace = true });
        _viewgrid.Columns.Add(new ViewGridColumn("Meslek", nameof(DemoRow.Occupation), 145));
        _viewgrid.Columns.Add(new ViewGridColumn("Durum", nameof(DemoRow.State), 120) { Kind = ViewGridColumnKind.Badge });
        _viewgrid.Columns.Add(new ViewGridColumn("İlerleme", nameof(DemoRow.Progress), 150) { Kind = ViewGridColumnKind.ProgressBar });
        _viewgrid.Columns.Add(new ViewGridColumn("Puan", nameof(DemoRow.Rating), 120) { Kind = ViewGridColumnKind.Rating, MaxRating = 5 });
        _viewgrid.Columns.Add(new ViewGridColumn("İşlem", nameof(DemoRow.ActionText), 100) { Kind = ViewGridColumnKind.Button });
        _viewgrid.Summaries.Add(new ViewGridSummaryItem { Column = _viewgrid.Columns[nameof(DemoRow.Id)]!, Type = ViewGridSummaryType.Count, Format = "Adet: {0}" });
        _viewgrid.Summaries.Add(new ViewGridSummaryItem { Column = _viewgrid.Columns[nameof(DemoRow.Progress)]!, Type = ViewGridSummaryType.Average, Format = "Ort: {0:0.0}" });
        _viewgrid.ConditionalFormats.Add(new ViewGridConditionalFormat { Column = _viewgrid.Columns[nameof(DemoRow.State)], BackColor = Color.FromArgb(60, 220, 60, 60), ForeColor = Color.DarkRed, Predicate = (_,_,v) => string.Equals(Convert.ToString(v), "Fail", StringComparison.OrdinalIgnoreCase) });
    }


    private Image CreateStateIcon(string state)
    {
        var bmp = new Bitmap(22, 22);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Color c = state == "Fail" ? Color.FromArgb(220, 60, 70) : state == "Review" ? _currentTheme.AccentColor : Color.FromArgb(36, 155, 90);
        using var b = new SolidBrush(c);
        using var p = new Pen(Blend(c, Color.White, _currentTheme.IsDark ? 0.25 : 0.05), 1.2f);
        g.FillEllipse(b, 3, 3, 16, 16);
        g.DrawEllipse(p, 3, 3, 16, 16);
        string text = state == "Fail" ? "!" : state == "Review" ? "?" : "✓";
        using var f = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var tb = new SolidBrush(Color.White);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, f, tb, new RectangleF(3, 2, 16, 17), sf);
        return bmp;
    }

    private void ApplyAppTheme(ViewGridTheme theme, string caption)
    {
        _currentTheme = EnsureReadable(theme);
        Program.AppTheme = _currentTheme;
        _viewgrid.ApplyTheme(_currentTheme);
        _viewgrid.SetRowDetailsProvider(CreateDetailsProvider());

        BackColor = _currentTheme.BackColor;
        ForeColor = _currentTheme.ForeColor;
        _themePreview.BackColor = Blend(_currentTheme.BackColor, _currentTheme.AccentColor, _currentTheme.IsDark ? 0.16 : 0.07);
        _themePreview.ForeColor = _currentTheme.ForeColor;
        _themeInfo.ForeColor = _currentTheme.ForeColor;
        _themeInfo.Text = $"Tema: {caption}  |  DLL={ViewGridVersionInfo.InformationalVersion}  |  Accent={ColorTranslator.ToHtml(_currentTheme.AccentColor)}  |  Fluent={_currentTheme.UseFluentBackdrop}  |  Acrylic={_currentTheme.UseAcrylicEffect}  |  Animasyonlu seçim aktif";

        _search.BackColor = _currentTheme.IsDark ? Color.FromArgb(38, 38, 42) : Color.White;
        _search.ForeColor = _currentTheme.ForeColor;
        _search.BorderStyle = BorderStyle.FixedSingle;

        _tool.BackColor = _currentTheme.HeaderBackColor;
        _tool.ForeColor = _currentTheme.HeaderForeColor;
        foreach (ToolStripItem item in _tool.Items)
        {
            item.ForeColor = _currentTheme.HeaderForeColor;
            item.BackColor = _currentTheme.HeaderBackColor;
        }
        SmartMenuRenderer.ApplyTo(_tool, _currentTheme);

        ViewGridWindowChrome.Apply(this, _currentTheme, true);
        _themePreview.Invalidate(true);
        Invalidate(true);
    }

    private static ViewGridTheme CreateAoiTheme()
    {
        var t = ViewGridTheme.FluentDarkTheme();
        t.AccentColor = Color.FromArgb(128, 104, 176);
        t.SelectionBackColor = Color.FromArgb(112, 88, 168);
        t.HotBackColor = Color.FromArgb(50, 43, 64);
        t.UseFluentBackdrop = true;
        t.UseAcrylicEffect = true;
        t.UseAnimatedSelection = true;
        return t;
    }

    private static ViewGridTheme CreateHighContrastTheme() => new()
    {
        BackColor = Color.Black,
        ForeColor = Color.White,
        HeaderBackColor = Color.FromArgb(20, 20, 20),
        HeaderForeColor = Color.White,
        GridColor = Color.FromArgb(90, 90, 90),
        AlternateBackColor = Color.FromArgb(12, 12, 12),
        SelectionBackColor = Color.FromArgb(255, 204, 0),
        SelectionForeColor = Color.Black,
        HotBackColor = Color.FromArgb(45, 45, 45),
        AccentColor = Color.FromArgb(255, 204, 0),
        BorderColor = Color.White,
        EmptyTextColor = Color.FromArgb(230, 230, 230),
        PanelBackColor = Color.Black,
        ControlBackColor = Color.FromArgb(10, 10, 10),
        SelectionGlowColor = Color.FromArgb(180, 255, 204, 0),
        UseAnimatedSelection = true,
        IsDark = true
    };

    private static ViewGridTheme EnsureReadable(ViewGridTheme t)
    {
        if (ContrastRatio(t.BackColor, t.ForeColor) < 4.5)
            t.ForeColor = t.IsDark ? Color.White : Color.FromArgb(25, 25, 25);
        if (ContrastRatio(t.HeaderBackColor, t.HeaderForeColor) < 4.5)
            t.HeaderForeColor = t.IsDark ? Color.White : Color.FromArgb(20, 20, 20);
        if (ContrastRatio(t.SelectionBackColor, t.SelectionForeColor) < 3.0)
            t.SelectionForeColor = GetLuminance(t.SelectionBackColor) < 0.5 ? Color.White : Color.Black;
        return t;
    }

    private static Color Blend(Color a, Color b, double amount)
    {
        amount = Math.Max(0, Math.Min(1, amount));
        return Color.FromArgb(
            (int)(a.R + (b.R - a.R) * amount),
            (int)(a.G + (b.G - a.G) * amount),
            (int)(a.B + (b.B - a.B) * amount));
    }

    private static double ContrastRatio(Color a, Color b)
    {
        var l1 = GetLuminance(a) + 0.05;
        var l2 = GetLuminance(b) + 0.05;
        return Math.Max(l1, l2) / Math.Min(l1, l2);
    }

    private static double GetLuminance(Color c)
    {
        static double Channel(byte v)
        {
            var x = v / 255.0;
            return x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    private const int StartupRowCount = 150_000_000;
    private const int StartupStressRowCount = 150_000_000;

    private Task LoadStartupRows()
    {
        LoadVirtualRows(StartupRowCount);
        return Task.CompletedTask;
    }

    private Task LoadStartupStressRows()
    {
        LoadVirtualRows(StartupStressRowCount);
        return Task.CompletedTask;
    }

    private async Task LoadRowsAsync(int rowCount, string caption)
    {
        var sw = Stopwatch.StartNew();
        _status.Text = $"{caption} yükleniyor... {rowCount:N0} kayıt";
        UseWaitCursor = true;
        try
        {
            var rows = Enumerable.Range(1, rowCount).Select(i => new DemoRow
                {
                    Id = i,
                    Name = "AOI kayıt " + i,
                    State = i % 7 == 0 ? "Fail" : i % 5 == 0 ? "Review" : "OK",
                    Occupation = DemoData.Occupations[i % DemoData.Occupations.Length],
                    Progress = i % 101,
                    Rating = i % 6,
                    Checked = i % 9 == 0,
                    NeedsReview = i % 18 == 0,
                    ActionText = "Aç",
                    Tags = i % 3 == 0 ? "AOI,Fail,Review" : i % 2 == 0 ? "ViewGrid,Filter,Tile" : "Fast,Virtual,Theme",
                    Spark = $"{(i * 3) % 20},{(i * 5) % 35},{(i * 7) % 55},{(i * 11) % 80},{(i * 13) % 100}",
                    Link = "Aç / Detay",
                    RowColor = i % 7 == 0 ? "#E53935" : i % 5 == 0 ? "#7E57C2" : "#43A047",
                    Year = 2020 + (i % 7),
                    PosterIndex = i % 12
                }).ToList();

            _viewgrid.ClearFilters();
            _search.Clear();
            if (rowCount <= 10_000)
                _viewgrid.SetObjects(rows);
            else
                await _viewgrid.SetObjectsAsync(_ => Task.FromResult<IEnumerable<DemoRow>>(rows));
            sw.Stop();
            _status.Text = $"{rowCount:N0} async kayıt yüklendi | Süre: {sw.Elapsed.TotalSeconds:0.0} sn | Standart veri için Virtual provider kullan";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }


    private void ClearAllFiltersFast()
    {
        _suppressSearchFilter = true;
        try
        {
            if (_search.TextLength > 0)
                _search.Clear();
        }
        finally
        {
            _suppressSearchFilter = false;
        }

        _viewgrid.ClearFilters();
        _status.Text = "Filtreler temizlendi";
    }

    private void LoadVirtualRows(int rowCount = 150_000_000)
    {
        _suppressSearchFilter = true;
        try
        {
            if (_search.TextLength > 0)
                _search.Clear();
        }
        finally
        {
            _suppressSearchFilter = false;
        }

        _viewgrid.ClearFilters();
        _viewgrid.SetVirtualProvider(new VirtualDemoRowProvider(rowCount));
        _status.Text = $"{rowCount:N0} sanal satır aktif | anında yükleme | RAM'e liste basılmaz | filtre/sort provider tarafında";
    }
}

internal static class DemoData
{
    public static readonly string[] Occupations = { "Programmer", "Economist", "Nurse", "School Teacher", "Gymnast", "Operator", "Technician" };
}

public sealed class DemoRow
{
    private Action<int, bool>? _checkedSetter;
    private Action<int, bool>? _needsReviewSetter;

    public bool Checked { get; set; }
    public bool NeedsReview { get; set; }
    public bool Approved { get; set; }
    public bool Locked { get; set; }
    public int Id { get; set; }
    public int RealIndex { get; set; }
    public string Name { get; set; } = "";
    public string State { get; set; } = "";
    public string Occupation { get; set; } = "";
    public string Machine { get; set; } = "LINE01YASREW";
    public string Description { get; set; } = "ViewGrid örnek açıklama metni";
    public string Notes { get; set; } = "ViewGrid örnek not metni";
    public DateTime Date { get; set; } = DateTime.Now;
    public int Progress { get; set; }
    public int Rating { get; set; }
    public string ActionText { get; set; } = "Aç";
    public string Tags { get; set; } = "AOI,ViewGrid";
    public string Spark { get; set; } = "10,20,12,35,48,40,70";
    public string Link { get; set; } = "Detay";
    public string RowColor { get; set; } = "#1280D8";
    public int Year { get; set; }
    public int PosterIndex { get; set; }
    public List<DemoRow>? Children { get; set; }

    public void BindVirtualSetters(Action<int, bool> checkedSetter, Action<int, bool> needsReviewSetter)
    {
        _checkedSetter = checkedSetter;
        _needsReviewSetter = needsReviewSetter;
    }

    public void SetChecked(bool value)
    {
        Checked = value;
        _checkedSetter?.Invoke(RealIndex, value);
    }

    public void SetNeedsReview(bool value)
    {
        NeedsReview = value;
        _needsReviewSetter?.Invoke(RealIndex, value);
    }
}

public sealed class VirtualDemoRowProvider : IQueryRowProvider, IAsyncPreloadRowProvider, IProviderChangeNotifier, IBulkCheckStateProvider
{
    private const int DefaultHugeCount = 999_000_000;
    private readonly int _totalRows;
    private readonly HashSet<int> _checked = new();
    private readonly HashSet<int> _needsReview = new();
    private bool _allChecked;
    private bool _allNeedsReview;
    private ViewGridFilterSet _filters = new();
    private ViewGridColumn[] _columns = Array.Empty<ViewGridColumn>();
    private ViewGridColumn? _sortColumn;
    private bool _sortDescending;
    private int _filteredCount;
    private bool _hasNameExact;
    private int _nameExactRealIndex = -1;
    private const int PageSize = 256;
    private readonly object _pageSync = new();
    private readonly Dictionary<int, DemoRow[]> _pageCache = new();
    private readonly HashSet<int> _loadingPages = new();

    public event EventHandler? RowsChanged;

    public VirtualDemoRowProvider(int count = DefaultHugeCount)
    {
        _totalRows = Math.Max(0, count);
        _filteredCount = _totalRows;
    }

    public long TotalCount64 => _filteredCount;
    public int Count => _filteredCount;

    public void ApplyView(ViewGridFilterSet filters, ViewGridColumn[] columns, ViewGridColumn? sortColumn, bool sortDescending)
    {
        _filters = filters ?? new ViewGridFilterSet();
        _columns = columns ?? Array.Empty<ViewGridColumn>();
        _sortColumn = sortColumn;
        _sortDescending = sortDescending;
        _hasNameExact = TryGetNameExactIndex(_filters, out _nameExactRealIndex);
        _filteredCount = CalculateFilteredCount();
        lock (_pageSync)
        {
            _pageCache.Clear();
            _loadingPages.Clear();
        }
    }

    public bool TryGetDistinctValues(ViewGridColumn column, ViewGridFilterSet filters, ViewGridColumn[] columns, int maxValues, string? searchText, out IReadOnlyList<string> values)
    {
        values = Array.Empty<string>();
        if (column == null) return false;
        var aspect = column.AspectName;
        if (aspect == nameof(DemoRow.State)) { values = new[] { "Fail", "OK", "Review" }; return true; }
        if (aspect == nameof(DemoRow.Occupation)) { values = DemoData.Occupations; return true; }
        if (aspect == nameof(DemoRow.Rating)) { values = Enumerable.Range(0, 6).Select(x => x.ToString()).ToArray(); return true; }
        if (aspect == nameof(DemoRow.Progress)) { values = Enumerable.Range(0, 101).Select(x => x.ToString()).ToArray(); return true; }
        if (aspect == nameof(DemoRow.Year)) { values = Enumerable.Range(2020, 7).Select(x => x.ToString()).ToArray(); return true; }

        // v24.69: Name column is virtually unbounded. The popup still shows only a small
        // preview when empty, but typed searches are resolved against the complete 999M range.
        // Example: searching "Virtual satır 999000000" immediately appears and filters correctly.
        if (aspect == nameof(DemoRow.Name))
        {
            values = BuildVirtualNameDistinctValues(searchText, maxValues);
            return true;
        }
        return false;
    }


    private IReadOnlyList<string> BuildVirtualNameDistinctValues(string? searchText, int maxValues)
    {
        int limit = Math.Clamp(maxValues, 1, 10_000);
        string q = (searchText ?? string.Empty).Trim();
        var results = new List<string>(Math.Min(limit, 256));
        var seen = new HashSet<int>();

        void AddId(int id)
        {
            if (id < 1 || id > _totalRows || !seen.Add(id)) return;
            results.Add("Virtual satır " + id);
        }

        if (string.IsNullOrWhiteSpace(q))
        {
            int take = Math.Min(_totalRows, Math.Min(limit, 1500));
            for (int id = 1; id <= take; id++) AddId(id);
            return results;
        }

        if (TryParseVirtualName(q, out int exactIndex))
            AddId(exactIndex + 1);

        string digits = new string(q.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out int numericId))
        {
            AddId(numericId);
            for (int delta = 1; results.Count < Math.Min(limit, 80) && delta <= 25; delta++)
            {
                AddId(numericId - delta);
                AddId(numericId + delta);
            }
        }

        // For contains searches such as "999", generate matching virtual names without asking
        // ViewGridControl to scan hundreds of millions of rows on the UI thread.
        int scanCap = Math.Min(_totalRows, 2_000_000);
        for (int id = 1; results.Count < limit && id <= scanCap; id++)
        {
            string name = "Virtual satır " + id;
            if (name.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0)
                AddId(id);
        }

        return results;
    }

    public object? GetRow(int index)
    {
        if (index < 0 || index >= _filteredCount) return null;

        int pageIndex = index / PageSize;
        int offset = index % PageSize;
        lock (_pageSync)
        {
            if (_pageCache.TryGetValue(pageIndex, out var page) && offset >= 0 && offset < page.Length)
                return page[offset];
        }

        EnsurePageLoading(pageIndex);
        return CreateLoadingRow(index);
    }

    public void RequestPreload(int startIndex, int count)
    {
        if (startIndex < 0 || count <= 0) return;
        int firstPage = startIndex / PageSize;
        int lastPage = Math.Min((_filteredCount - 1) / PageSize, (startIndex + count - 1) / PageSize);
        for (int page = firstPage; page <= lastPage; page++)
            EnsurePageLoading(page);
    }

    private void EnsurePageLoading(int pageIndex)
    {
        if (pageIndex < 0) return;
        lock (_pageSync)
        {
            if (_pageCache.ContainsKey(pageIndex) || _loadingPages.Contains(pageIndex)) return;
            _loadingPages.Add(pageIndex);
        }

        _ = Task.Run(() =>
        {
            DemoRow[] page;
            try
            {
                page = BuildPage(pageIndex);
            }
            catch
            {
                page = Array.Empty<DemoRow>();
            }

            lock (_pageSync)
            {
                _pageCache[pageIndex] = page;
                _loadingPages.Remove(pageIndex);

                // Keep memory bounded for huge data; only nearby/recent pages stay in RAM.
                if (_pageCache.Count > 48)
                {
                    foreach (int key in _pageCache.Keys.OrderBy(k => Math.Abs(k - pageIndex)).Skip(48).ToArray())
                        _pageCache.Remove(key);
                }
            }
            RowsChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private DemoRow[] BuildPage(int pageIndex)
    {
        int start = pageIndex * PageSize;
        int take = Math.Min(PageSize, Math.Max(0, _filteredCount - start));
        var rows = new DemoRow[take];
        for (int i = 0; i < take; i++)
        {
            int viewIndex = start + i;
            int realIndex = MapViewIndexToRealIndex(viewIndex);
            rows[i] = realIndex >= 0 && realIndex < _totalRows ? CreateRowByRealIndex(realIndex) : CreateLoadingRow(viewIndex);
            rows[i].BindVirtualSetters(SetChecked, SetNeedsReview);
        }
        return rows;
    }

    private DemoRow CreateLoadingRow(int viewIndex)
    {
        return new DemoRow
        {
            Id = viewIndex + 1,
            RealIndex = -1,
            Name = "Yükleniyor...",
            State = "Loading",
            Occupation = "Query page",
            Progress = 0,
            Rating = 0,
            ActionText = "...",
            Tags = "Async,Virtual,Query",
            Spark = "0,0,0,0,0",
            Link = string.Empty,
            RowColor = "#777777",
            Year = 0,
            PosterIndex = 0
        };
    }

    private int MapViewIndexToRealIndex(int viewIndex)
    {
        if (_hasNameExact) return viewIndex == 0 ? _nameExactRealIndex : -1;

        // No active filter: O(1) direct mapping. This is the critical path for 999M smooth scrolling.
        var sortAspect = _sortColumn?.AspectName;
        if (!HasAnyFilter() && (string.IsNullOrWhiteSpace(sortAspect) || sortAspect == nameof(DemoRow.Id) || sortAspect == nameof(DemoRow.RealIndex) || sortAspect == nameof(DemoRow.Name)))
            return _sortDescending ? _totalRows - 1 - viewIndex : viewIndex;

        // Sıralama da provider tarafında: ViewGridControl dev index listesi oluşturmaz.
        if (string.IsNullOrWhiteSpace(sortAspect) || sortAspect == nameof(DemoRow.Id) || sortAspect == nameof(DemoRow.RealIndex) || sortAspect == nameof(DemoRow.Name))
        {
            return _sortDescending
                ? FindNthMatchReverse(viewIndex)
                : FindNthMatchForward(viewIndex);
        }
        if (sortAspect == nameof(DemoRow.Progress)) return FindNthMatchByModulo(viewIndex, 101, _sortDescending);
        if (sortAspect == nameof(DemoRow.Rating)) return FindNthMatchByModulo(viewIndex, 6, _sortDescending);
        if (sortAspect == nameof(DemoRow.Year)) return FindNthMatchByYear(viewIndex, _sortDescending);
        if (sortAspect == nameof(DemoRow.Occupation)) return FindNthMatchByOrderedKeys(viewIndex, DemoData.Occupations.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToArray(), i => DemoData.Occupations[i % DemoData.Occupations.Length], _sortDescending);
        if (sortAspect == nameof(DemoRow.State)) return FindNthMatchByOrderedKeys(viewIndex, new[] { "Fail", "OK", "Review" }, GetState, _sortDescending);

        return FindNthMatchForward(viewIndex);
    }

    private int FindNthMatchForward(int viewIndex)
    {
        if (TryMapNthPeriodic(viewIndex, Enumerable.Range(0, GetCycleLimit()).Where(PassesVirtualFilters).ToArray(), false, out int real)) return real;
        int seen = -1;
        for (int i = 0; i < _totalRows; i++)
            if (PassesVirtualFilters(i) && ++seen == viewIndex) return i;
        return -1;
    }

    private int FindNthMatchReverse(int viewIndex)
    {
        if (TryMapNthPeriodic(viewIndex, Enumerable.Range(0, GetCycleLimit()).Where(PassesVirtualFilters).ToArray(), true, out int real)) return real;
        int seen = -1;
        for (int i = _totalRows - 1; i >= 0; i--)
            if (PassesVirtualFilters(i) && ++seen == viewIndex) return i;
        return -1;
    }

    private int FindNthMatchByModulo(int viewIndex, int modulo, bool desc)
    {
        var keys = desc ? Enumerable.Range(0, modulo).Reverse() : Enumerable.Range(0, modulo);
        return MapNthByVirtualKey(viewIndex, keys.Select(x => x.ToString()).ToArray(), i => (i % modulo).ToString());
    }

    private int FindNthMatchByYear(int viewIndex, bool desc)
    {
        var years = Enumerable.Range(2020, 7).Select(x => x.ToString()).ToArray();
        if (desc) Array.Reverse(years);
        return MapNthByVirtualKey(viewIndex, years, i => (2020 + (i % 7)).ToString());
    }

    private int FindNthMatchByOrderedKeys(int viewIndex, string[] orderedKeys, Func<int, string> keyGetter, bool desc)
    {
        if (desc) Array.Reverse(orderedKeys);
        return MapNthByVirtualKey(viewIndex, orderedKeys, keyGetter);
    }

    private int MapNthByVirtualKey(int viewIndex, string[] orderedKeys, Func<int, string> keyGetter)
    {
        int cycleLimit = GetCycleLimit();
        foreach (string key in orderedKeys)
        {
            var offsets = Enumerable.Range(0, cycleLimit)
                .Where(i => string.Equals(keyGetter(i), key, StringComparison.CurrentCultureIgnoreCase) && PassesVirtualFilters(i))
                .ToArray();
            long count = CountPeriodicOffsets(offsets);
            if (viewIndex >= count) { viewIndex -= (int)Math.Min(int.MaxValue, count); continue; }
            if (TryMapNthPeriodic(viewIndex, offsets, false, out int real)) return real;
        }
        return -1;
    }

    private int GetCycleLimit() => Math.Min(60_606, _totalRows);

    private long CountPeriodicOffsets(int[] offsets)
    {
        if (offsets.Length == 0) return 0;
        const int cycle = 60_606;
        long fullCycles = _totalRows / cycle;
        int remainder = _totalRows % cycle;
        long total = fullCycles * offsets.Length;
        if (remainder > 0) total += offsets.Count(o => o < remainder);
        return total;
    }

    private bool TryMapNthPeriodic(int viewIndex, int[] offsets, bool reverse, out int realIndex)
    {
        realIndex = -1;
        if (offsets.Length == 0 || !IsPeriodicFilterSafe()) return false;
        Array.Sort(offsets);
        if (reverse) Array.Reverse(offsets);
        const int cycle = 60_606;

        if (!reverse)
        {
            long cycleIndex = viewIndex / offsets.Length;
            int offsetIndex = viewIndex % offsets.Length;
            long candidate = cycleIndex * cycle + offsets[offsetIndex];
            if (candidate < _totalRows) { realIndex = (int)candidate; return true; }
        }
        else
        {
            long lastIndex = _totalRows - 1L;
            long lastCycle = lastIndex / cycle;
            int remainderMax = (int)(lastIndex % cycle);
            var tailOffsets = offsets.Where(o => o <= remainderMax).ToArray();
            if (tailOffsets.Length > 0)
            {
                if (viewIndex < tailOffsets.Length)
                {
                    realIndex = (int)(lastCycle * cycle + tailOffsets[viewIndex]);
                    return true;
                }
                viewIndex -= tailOffsets.Length;
                lastCycle--;
            }
            long cycleBack = viewIndex / offsets.Length;
            int offsetIndex = viewIndex % offsets.Length;
            long candidateCycle = lastCycle - cycleBack;
            if (candidateCycle >= 0)
            {
                realIndex = (int)(candidateCycle * cycle + offsets[offsetIndex]);
                return true;
            }
        }
        return false;
    }

    private bool IsPeriodicFilterSafe()
    {
        if (!string.IsNullOrWhiteSpace(_filters.GlobalText)) return false;
        foreach (var f in _filters.Filters)
        {
            if (string.Equals(f.AspectName, nameof(DemoRow.Name), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(f.AspectName, nameof(DemoRow.Id), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(f.AspectName, nameof(DemoRow.RealIndex), StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private int CalculateFilteredCount()
    {
        if (_hasNameExact) return _nameExactRealIndex >= 0 && _nameExactRealIndex < _totalRows && PassesVirtualFilters(_nameExactRealIndex) ? 1 : 0;
        if (!HasAnyFilter()) return _totalRows;

        // Tüm kayıtlar üzerinde filtre mantığı: demo verisi periyodik üretildiği için 60.606'lık döngü tam sayılabilir.
        const int cycle = 60_606; // lcm(11,13,7,101,6)
        int cycleMatches = 0;
        int cycleLimit = Math.Min(cycle, _totalRows);
        for (int i = 0; i < cycleLimit; i++)
            if (PassesVirtualFilters(i)) cycleMatches++;

        int fullCycles = _totalRows / cycle;
        int remainder = _totalRows % cycle;
        long total = (long)fullCycles * cycleMatches;
        for (int i = 0; i < remainder; i++)
            if (PassesVirtualFilters(i)) total++;
        return (int)Math.Min(int.MaxValue, total);
    }

    private bool HasAnyFilter() => _filters.Filters.Count > 0 || !string.IsNullOrWhiteSpace(_filters.GlobalText);

    private bool PassesVirtualFilters(int realIndex)
    {
        if (!string.IsNullOrWhiteSpace(_filters.GlobalText))
        {
            string g = _filters.GlobalText.Trim();
            if (TryParseVirtualName(g, out int exact)) return exact == realIndex;
            if (CreateSearchText(realIndex).IndexOf(g, StringComparison.CurrentCultureIgnoreCase) < 0) return false;
        }

        foreach (var f in _filters.Filters)
        {
            // v26.58: Name/Id filters in the 150M demo can arrive as either
            // "Virtual satır 150000000" or just "150000000" depending on how
            // quickly the user presses Apply before async distinct values return.
            // Resolve both forms to the real index so the provider never scans the
            // whole range and the row is visible immediately.
            if (TryResolveExactIdentityFilter(f, out int exactRealIndex))
            {
                if (exactRealIndex != realIndex) return false;
                continue;
            }

            string value = GetAspectText(realIndex, f.AspectName);
            if (!PassesOne(value, f)) return false;
        }
        return true;
    }

    private static bool TryResolveExactIdentityFilter(ViewGridColumnFilter f, out int realIndex)
    {
        realIndex = -1;
        if (f == null) return false;
        bool identityAspect =
            string.Equals(f.AspectName, nameof(DemoRow.Name), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(f.AspectName, nameof(DemoRow.Id), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(f.AspectName, nameof(DemoRow.RealIndex), StringComparison.OrdinalIgnoreCase);
        if (!identityAspect) return false;

        if (f.Mode == ViewGridFilterMode.ValueList && f.SelectedValues is { Count: 1 })
            return TryParseVirtualName(f.SelectedValues.First(), out realIndex);

        if ((f.Mode == ViewGridFilterMode.Equals || f.Mode == ViewGridFilterMode.Contains || f.Mode == ViewGridFilterMode.StartsWith) &&
            !string.IsNullOrWhiteSpace(f.Text))
            return TryParseVirtualName(f.Text.Trim(), out realIndex);

        return false;
    }

    private string CreateSearchText(int realIndex)
        => string.Join(' ', realIndex + 1, "Virtual satır " + (realIndex + 1), GetState(realIndex), DemoData.Occupations[realIndex % DemoData.Occupations.Length], realIndex % 101, realIndex % 6, 2020 + (realIndex % 7));

    private static bool PassesOne(string value, ViewGridColumnFilter f)
    {
        var t = f.Text ?? string.Empty;
        if (f.Mode == ViewGridFilterMode.ValueList)
            return f.SelectedValues == null || f.SelectedValues.Contains(value);
        return f.Mode switch
        {
            ViewGridFilterMode.Contains => value.IndexOf(t, StringComparison.CurrentCultureIgnoreCase) >= 0,
            ViewGridFilterMode.Equals => string.Equals(value, t, StringComparison.CurrentCultureIgnoreCase),
            ViewGridFilterMode.StartsWith => value.StartsWith(t, StringComparison.CurrentCultureIgnoreCase),
            ViewGridFilterMode.EndsWith => value.EndsWith(t, StringComparison.CurrentCultureIgnoreCase),
            ViewGridFilterMode.IsEmpty => string.IsNullOrWhiteSpace(value),
            ViewGridFilterMode.IsNotEmpty => !string.IsNullOrWhiteSpace(value),
            ViewGridFilterMode.GreaterThan => Compare(value, t) > 0,
            ViewGridFilterMode.LessThan => Compare(value, t) < 0,
            ViewGridFilterMode.Between => Compare(value, t) >= 0 && Compare(value, f.Text2 ?? t) <= 0,
            _ => true
        };
    }

    private static int Compare(string a, string b)
    {
        if (decimal.TryParse(a, out var da) && decimal.TryParse(b, out var db)) return da.CompareTo(db);
        return string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
    }

    private string GetAspectText(int realIndex, string aspect)
    {
        if (aspect == nameof(DemoRow.Id)) return (realIndex + 1).ToString();
        if (aspect == nameof(DemoRow.RealIndex)) return realIndex.ToString();
        if (aspect == nameof(DemoRow.Name)) return "Virtual satır " + (realIndex + 1);
        if (aspect == nameof(DemoRow.State)) return GetState(realIndex);
        if (aspect == nameof(DemoRow.Occupation)) return DemoData.Occupations[realIndex % DemoData.Occupations.Length];
        if (aspect == nameof(DemoRow.Progress)) return (realIndex % 101).ToString();
        if (aspect == nameof(DemoRow.Rating)) return (realIndex % 6).ToString();
        if (aspect == nameof(DemoRow.Year)) return (2020 + (realIndex % 7)).ToString();
        return string.Empty;
    }

    private DemoRow CreateRowByRealIndex(int realIndex)
    {
        return new DemoRow
        {
            Id = realIndex + 1,
            RealIndex = realIndex,
            Name = "Virtual satır " + (realIndex + 1),
            State = GetState(realIndex),
            Occupation = DemoData.Occupations[realIndex % DemoData.Occupations.Length],
            Progress = realIndex % 101,
            Rating = realIndex % 6,
            Checked = GetStoredChecked(_checked, _allChecked, realIndex),
            NeedsReview = GetStoredChecked(_needsReview, _allNeedsReview, realIndex),
            ActionText = "Git",
            Tags = realIndex % 3 == 0 ? "AOI,Fail,Review" : realIndex % 2 == 0 ? "ViewGrid,Filter,Tile" : "Fast,Virtual,Theme",
            Spark = $"{(realIndex*3)%20},{(realIndex*5)%35},{(realIndex*7)%55},{(realIndex*11)%80},{(realIndex*13)%100}",
            Link = "Aç / Detay",
            RowColor = realIndex % 7 == 0 ? "#E53935" : realIndex % 5 == 0 ? "#7E57C2" : "#43A047",
            Year = 2020 + (realIndex % 7),
            PosterIndex = realIndex % 12
        };
    }

    private static string GetState(int index) => index % 11 == 0 ? "Fail" : index % 13 == 0 ? "Review" : "OK";

    public void SetChecked(int realIndex, bool value) => SetState(_checked, _allChecked, realIndex, value);
    public void SetNeedsReview(int realIndex, bool value) => SetState(_needsReview, _allNeedsReview, realIndex, value);

    private static bool GetStoredChecked(HashSet<int> exceptions, bool allChecked, int realIndex)
        => allChecked ? !exceptions.Contains(realIndex) : exceptions.Contains(realIndex);

    private void SetState(HashSet<int> exceptions, bool allChecked, int realIndex, bool value)
    {
        if (realIndex < 0 || realIndex >= _totalRows) return;
        if (allChecked)
        {
            if (value) exceptions.Remove(realIndex); else exceptions.Add(realIndex);
        }
        else
        {
            if (value) exceptions.Add(realIndex); else exceptions.Remove(realIndex);
        }
    }

    public bool TrySetAllCheckStates(ViewGridColumn column, CheckState state)
    {
        if (column == null || state == CheckState.Indeterminate) return false;
        bool value = state == CheckState.Checked;
        if (string.Equals(column.AspectName, nameof(DemoRow.Checked), StringComparison.OrdinalIgnoreCase))
        {
            _allChecked = value;
            _checked.Clear();
            ClearCachedPages();
            RowsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        if (string.Equals(column.AspectName, nameof(DemoRow.NeedsReview), StringComparison.OrdinalIgnoreCase))
        {
            _allNeedsReview = value;
            _needsReview.Clear();
            ClearCachedPages();
            RowsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }

    public bool TryGetCheckStateSummary(ViewGridColumn column, out int checkedCount, out int uncheckedCount)
    {
        checkedCount = 0;
        uncheckedCount = 0;
        if (column == null) return false;

        if (string.Equals(column.AspectName, nameof(DemoRow.Checked), StringComparison.OrdinalIgnoreCase))
            return GetSummary(_checked, _allChecked, out checkedCount, out uncheckedCount);

        if (string.Equals(column.AspectName, nameof(DemoRow.NeedsReview), StringComparison.OrdinalIgnoreCase))
            return GetSummary(_needsReview, _allNeedsReview, out checkedCount, out uncheckedCount);

        return false;
    }

    private bool GetSummary(HashSet<int> exceptions, bool allChecked, out int checkedCount, out int uncheckedCount)
    {
        int total = Math.Max(0, _filteredCount);
        if (total == 0)
        {
            checkedCount = 0;
            uncheckedCount = 0;
            return true;
        }

        if (!HasAnyFilter())
        {
            int exceptionCount = exceptions.Count;
            checkedCount = allChecked ? Math.Max(0, total - exceptionCount) : exceptionCount;
            uncheckedCount = allChecked ? exceptionCount : Math.Max(0, total - exceptionCount);
            return true;
        }

        int scan = Math.Min(total, 20000);
        int checkedLocal = 0;
        int uncheckedLocal = 0;
        for (int i = 0; i < scan; i++)
        {
            int real = MapViewIndexToRealIndex(i);
            bool value = real >= 0 && GetStoredChecked(exceptions, allChecked, real);
            if (value) checkedLocal++; else uncheckedLocal++;
            if (checkedLocal > 0 && uncheckedLocal > 0) break;
        }
        checkedCount = checkedLocal;
        uncheckedCount = uncheckedLocal;
        return true;
    }

    private void ClearCachedPages()
    {
        lock (_pageSync)
        {
            _pageCache.Clear();
            _loadingPages.Clear();
        }
    }

    private static bool TryGetNameExactIndex(ViewGridFilterSet filters, out int index)
    {
        index = -1;

        foreach (var filter in filters.Filters)
        {
            if (TryResolveExactIdentityFilter(filter, out index))
                return true;
        }

        if (!string.IsNullOrWhiteSpace(filters.GlobalText) && TryParseVirtualName(filters.GlobalText.Trim(), out index))
            return true;

        return false;
    }

    private static bool TryParseVirtualName(string text, out int index)
    {
        index = -1;
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0) return false;

        const string prefix = "Virtual satır ";
        string numberPart = text;
        if (text.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
            numberPart = text[prefix.Length..].Trim();
        else if (text.StartsWith("satır ", StringComparison.CurrentCultureIgnoreCase))
            numberPart = text[6..].Trim();

        // v26.58: The huge-data demo must resolve searches like "150000000"
        // without scanning 150M rows. Treat a plain positive id as the generated
        // virtual name/id for the row.
        if (!IsPlainPositiveInt(numberPart, out int id)) return false;
        index = id - 1;
        return true;
    }

    private static bool IsPlainPositiveInt(string text, out int value)
    {
        value = 0;
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0) return false;
        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsDigit(text[i])) return false;
        }
        return int.TryParse(text, out value) && value > 0;
    }
}

public sealed class SampleHubForm : Form
{
    private readonly ViewGridTheme _theme;
    private readonly Panel _host = new() { Dock = DockStyle.Fill, Padding = new Padding(12) };
    private readonly FlowLayoutPanel _nav = new()
    {
        Dock = DockStyle.Left,
        Width = 330,
        AutoScroll = true,
        Padding = new Padding(10),
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false
    };
    private readonly Label _header = new()
    {
        Dock = DockStyle.Top,
        Height = 58,
        Padding = new Padding(16, 10, 16, 6),
        Font = new Font("Segoe UI", 14F, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Top,
        Height = 44,
        Padding = new Padding(16, 0, 16, 8),
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly TextBox _searchBox = new()
    {
        Width = 296,
        Height = 30,
        Margin = new Padding(0, 0, 0, 8),
        PlaceholderText = "Örnek ara..."
    };
    private Button? _selectedNavButton;

    private Color NavNormalBack => _theme.BackColor;
    private Color NavNormalFore => _theme.ForeColor;
    private Color NavSelectedBack => Blend(_theme.AccentColor, _theme.BackColor, 0.72);
    private Color NavSelectedFore => GetReadableTextColor(NavSelectedBack);
    private Color NavSelectedBorder => _theme.AccentColor;

    public SampleHubForm()
    {
        _theme = Program.AppTheme;
        Text = "ViewGrid Example Center Pro - Tek Merkez";
        Width = 1280;
        Height = 760;
        MinimumSize = new Size(980, 620);
        StartPosition = FormStartPosition.CenterScreen;

        ViewGridWindowChrome.ApplyOnHandleCreated(this, () => _theme, true);
        ApplyThemeToShell();

        ApplyLocalizedShellTexts();

        Controls.Add(_host);
        Controls.Add(_nav);
        Controls.Add(_info);
        Controls.Add(_header);

        BuildNavigation();
        ShowWelcome();
    }


    private void ApplyLocalizedShellTexts()
    {
        bool tr = ViewGridLocalization.EffectiveLanguage == ViewGridLanguage.Turkish;
        Text = tr ? "ViewGrid Örnek Merkezi Pro - Tek Merkez" : "ViewGrid Example Center Pro - Single Hub";
        _header.Text = tr ? "ViewGrid Örnek Merkezi Pro" : "ViewGrid Example Center Pro";
        _info.Text = tr
            ? "Tüm örnekler tek merkezden yönetilir. Sol menüde arayıp açıklamasıyla birlikte aynı panelde açabilirsin."
            : "All samples are managed from one hub. Search from the left menu and open each sample in the same panel.";
        _searchBox.PlaceholderText = tr ? "Örnek ara..." : "Search samples...";
    }

    private void ApplyThemeToShell()
    {
        BackColor = _theme.BackColor;
        ForeColor = _theme.ForeColor;
        _host.BackColor = _theme.BackColor;
        _nav.BackColor = _theme.PanelBackColor;
        _searchBox.BackColor = _theme.BackColor;
        _searchBox.ForeColor = _theme.ForeColor;
        _header.BackColor = _theme.HeaderBackColor;
        _header.ForeColor = _theme.HeaderForeColor;
        _info.BackColor = _theme.PanelBackColor;
        _info.ForeColor = _theme.ForeColor;
    }

    private void BuildNavigation()
    {
        _searchBox.TextChanged += (_, __) => ApplySampleSearchFilter();
        _nav.Controls.Add(_searchBox);

        AddSection("🌐 Dil / Language");
        AddNav("Dil Değiştir", "TestApp açıkken ViewGrid yerleşik menüleri, dialogları ve yeni açılan örnek ekranları için dili değiştirir.", () =>
        {
            if (Program.ShowLanguageSelectionDialog(this))
            {
                ApplyLocalizedShellTexts();
                bool tr = ViewGridLocalization.EffectiveLanguage == ViewGridLanguage.Turkish;
                MessageBox.Show(this,
                    tr ? "Dil değiştirildi. Yeni açılan ViewGrid örnekleri seçilen dili kullanacak." : "Language changed. Newly opened ViewGrid samples will use the selected language.",
                    tr ? "Dil" : "Language",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            return new LocalizationSampleForm();
        });

        AddSection("📸 Dokümantasyon / Documentation");
        AddNav("Documentation Capture Mode", "Example Center ekran görüntülerini otomatik PNG olarak üretir ve Word/PDF dokümanına eklenecek manifest dosyasını hazırlar.", () => new DocumentationCaptureForm());

        AddSection("⭐ Hızlı Erişim / Nerede Bulurum?");
        AddNav("ViewGrid 5.1 Audix Pilot", "Audix gerçek kullanım pilotu: albüm kapağı, play/pause state, video preview, tema audit ve Example Center cleanup akışı.", () => new ViewGridV51RealUsagePilotSampleForm());
        AddNav("ViewGrid v50.2 Hardening", "Build/API guard, runtime check, tema okunurluğu, Audix medya güvenli ayarları ve tak-çalıştır stabilizasyon ekranı.", () => new ViewGridV502HardeningSampleForm());
        AddNav("ViewGrid 5.0 Foundation", "Stability, Theme Accessibility, Media Playback, modül profilleri ve hangi uygulamada hangi paket açılmalı sorusunu tek ekranda gösterir.", () => new ViewGridV50FoundationSampleForm());
        AddNav("V41 Media Playback State", "Audix ve video arşivleri için play/pause, şimdi çalıyor rozeti, equalizer ve video preview davranışını dene.", () => new ViewGridV41MediaPlaybackSampleForm());
        AddNav("V37-40 Pro Experience", "Enterprise Layout, Performance Pro, Interaction Pro ve Visual Analytics fazlarını tek ekranda dene.", () => new ViewGridV37ToV40ProExperienceSampleForm());
        AddNav("V34-50 Faz Merkezi", "Build Quality, Theme Studio, Media Pro, Foundation ve Example Center Navigator akışlarını tek yerden dene.", () => new ExampleCenterProForm());
        AddNav("V31 Faz Merkezi", "Faz 31-37: Media Experience, Smart Views, Grouping, MasterDetail, Kanban Pro, Designer ve Export tek ekranda.", () => new ViewGridV31AllPhasesSampleForm());
        AddNav("Audix Albüm Kapakları", "Albüm kapağı, placeholder, FLAC/MP3 rozeti, hover play overlay ve Poster/Gallery/FilmStrip geçişleri.", () => new ViewGridV31AllPhasesSampleForm());
        AddNav("Medya Vitrini", "Poster, Gallery, MediaTile, FilmStrip ve DetailCard için görsel medya örneği.", () => new MediaLibrarySampleForm());
        AddNav("Tüm Görünüm Modları", "ViewGridMode listesindeki tüm yeni modları tek ekrandan dene.", () => new ViewModeShowcaseSampleForm());

        AddSection("Örnekler");
        AddNav("Layout Manager", "Kolon düzeni kaydet / yükle / reset", () => new LayoutManagerVerificationForm());
        AddNav("Incremental Search + Highlight", "Yazarken bulma ve satır vurgulama", () => new IncrementalSearchHighlightVerificationForm());
        AddNav("Advanced Copy / Export", "Hücre, satır, JSON, CSV, Excel, JSON export", () => new AdvancedCopyExportVerificationForm());
        AddNav("Grouping", "Grup başlığı, daralt / aç, grup rengi", () => new GroupingVerificationForm());
        AddNav("Mini Analytics", "Distinct, boş değer, top values raporu", () => new MiniAnalyticsVerificationForm());
        AddNav("ViewGridControl Mode", "Object / DataTable / Virtual / Tree / Tile modlarını tek ekranda test et", () => new ViewGridDataModeVerificationForm());
        AddNav("Hepsi Bir Arada Smoke Test", "GLV geçiş API duman testi", () => new GLVFeaturePackSampleForm());
        AddNav("Özellik test merkezi", "Layout, highlight, export, grouping, analytics ve mode testlerini kartlı merkezde aç", () => new FeatureVerificationSuiteForm());

        AddSection("v31 Media + Smart Views Suite");
        AddNav("V31 Faz Merkezi", "Faz 31-37 tamamı: Audix medya, kullanıcı preset, gelişmiş gruplama, master detail, kanban, designer ve export akışı.", () => new ViewGridV31AllPhasesSampleForm());

        AddSection("v30 Visual Experience Suite");
        AddNav("V30 Ana Vitrin", "Poster, Gallery, MediaTile, FilmStrip, KPI, HeatMap, MiniChart, PropertyCard, DetailCard ve gruplama örnekleri tek ekranda.", () => new MediaLibrarySampleForm());
        AddNav("Görsel Medya Kütüphanesi", "Audix/Plex/Spotify tarzı albüm kapağı, film afişi, fotoğraf ve makine görseliyle medya görünümleri.", () => new MediaLibrarySampleForm());
        AddNav("Gelişmiş Görünüm Vitrini", "Details, Poster, Gallery, Kanban, Timeline, RowPreview, GroupCard, PropertyCard, KPI, HeatMap, MiniChart ve MasterDetail modları.", () => new ViewModeShowcaseSampleForm());
        AddNav("Poster / Resimli İçerik", "Kapak/afiş/fotoğraf tabanlı katalog görünümü için yalın poster örneği.", () => new PosterGallerySampleForm());
        AddNav("Master Detail", "Üst kayıt + detay kayıtları kullanımını gösteren klasik master-detail örneği.", () => new MasterDetailSampleForm());
        AddNav("Frozen + Command + Row Details", "Sabit kolon, satır butonu ve açılır detay paneli ile enterprise grid davranışı.", () => new FrozenCommandDetailsSampleForm());

        AddSection("Klasik ViewGrid Örnekleri");
        AddNav("Filtreleme", "Popup filtre, metin/değer listesi, temizleme", () => new FilteringSampleForm());
        AddNav("Gruplama detay", "ViewGrid/GLV tarzı grup davranışları", () => new GroupingSampleForm());
        AddNav("Modern ProgressBar", "Tema uyumlu progressbar ve text", () => new ProgressBarSampleForm());
        AddNav("Yazdırma / Önizleme", "PrintPreview, Print, PageSetup", () => new PrintPreviewSampleForm());
        AddNav("Görünüm modları", "Kompakt, Standart, Geniş, Liste, Kart, Geniş Kart, Detay", () => new ViewModesSampleForm());
        AddNav("MasterData görünüm senaryoları", "SAP, BOM, ürün ağacı, program dosyası, makine seçimi ve log ekranları için hazır ViewGrid senaryoları", () => new MasterDataScenarioSampleForm());
        AddNav("ViewGrid v27 Product Core", "State Engine, renderer profilleri, range virtual provider ve MasterData hazır kullanım örneği", () => new ViewGridV27ProductSampleForm());
        AddNav("Example Center Pro / Kategori Bulucu", "Tüm fazları kategori + arama ile bulabileceğin yeni ViewGrid 5.0 örnek merkezi", () => new ExampleCenterProForm());
        AddNav("v27.3 Renderer Showcase", "Badge, progress, icon+text, tag, button, hyperlink ve status renklerini tek tabloda gösterir", () => new ViewGridV273RendererShowcaseForm());
        AddNav("v27.3.1 Filter Popup UX", "Uzun SAP malzeme adı ve program yolu filtrelerinde resize, tooltip, auto-width ve boyut hatırlama davranışını gösterir", () => new ViewGridV2731FilterPopupUxForm());
        AddNav("v27.6 Design-Time Theme Sync", "Visual Studio designer açık tema önizlemesi, runtime temadan bağımsız design-time sync ve menü tema uyumu", () => new ViewGridV276DesignTimeThemeSampleForm());
        AddNav("v27.7 / v27.8 Card + Tree UX", "Kart filtre alanı içerik kesmesini düzeltir; TreeGrid sağ tık, çift tık, arama ve dal genişletme UX özelliklerini gösterir", () => new TreeViewSampleForm());
        AddNav("v27.9 Popular Enterprise Features", "Arama paneli, özet/footer, conditional format, column chooser, frozen column, advanced filter ve presetleri tek ekranda toplar", () => new ViewGridV279PopularEnterpriseFeaturesForm());
        AddNav("v28 UX Polish", "Kart/poster/dashboard görünümlerinde global filtre butonunu üst bara taşır; best-practice UX presetlerini gösterir", () => new ViewGridV28UxPolishForm());
        AddNav("v28.1 Poster Mode", "ViewGridMode.Poster, otomatik poster ölçüleri, image scaling ve üst filtre UX davranışını gösterir", () => new ViewGridV28UxPolishForm());
        AddNav("ViewGridControl Mode", "Object / DataTable / Virtual / Tree / Tile ana çalışma modları", () => new ViewGridDataModeVerificationForm());
        AddNav("Kart", "Responsive kart görünümü; TileMaxTextLines designer/kod testi", () => new TileSampleForm());
        AddNav("Geniş Kart", "Ticket/mesaj için geniş kart; LargeCardMaxTextLines designer/kod testi", () => new LargeCardSampleForm());
        AddNav("Details Çok Satırlı Hücre", "Kolon genişliğine göre 4-5 satıra sarılan hücre metni", () => new MultilineCellsSampleForm());
        AddNav("Poster / Resimli içerik", "Standart görselli kart/poster senaryosu", () => new PosterGallerySampleForm());
        AddNav("Media Library / Albüm-Film-Fotoğraf", "Albüm kapağı, film afişi, fotoğraf ve makine görseliyle Poster, MediaTile, FilmStrip ve DetailCard geçişleri", () => new MediaLibrarySampleForm());
        AddNav("Highlight arama", "Eşleşme vurgusu ve ileri/geri bulma", () => new HighlightSearchSampleForm());
        AddNav("Satır renklendirme", "RowBackColorGetter ve conditional format", () => new ColoringSampleForm());
        AddNav("Image + Combo hücre", "İkon/görsel kolon ve combobox editör", () => new ImageComboSampleForm());
        AddNav("Kolon Designer Editor Smoke Test", "Son kolon editor düzeltmesini test eder: gerçek ViewGridColumnCollectionEditor, Tamam/İptal, ekle/sil ve Audix property korunması", () => new ViewGridColumnDesignerEditorSmokeForm());
        AddNav("Designer önizleme", "Design-time davranış vitrini", () => new DesignerPreviewSampleForm());
        AddNav("Designer + Menü Merge", "User ContextMenuStrip içinde ViewGrid liste özellikleri alt menüsü", () => new DesignerMenuMergeSampleForm());
        AddNav("Export", "CSV ve Excel .xlsx export", () => new ExportSampleForm());
        AddNav("v28.7 PDF Export Suite", "Details/Table ve Card/Dashboard PDF export; masaüstüne örnek PDF üretir", () => new PdfExportSuiteSampleForm());
        AddNav("v28.8 Hücre İçi Scroll", "Uzun açıklama/not hücrelerinde satır büyümeden wheel scroll ve çift tık okuyucu popup", () => new CellOverflowScrollSampleForm());
        AddNav("999M sanal liste", "Virtual provider ve büyük veri testi", () => new MillionRowsSampleForm());
        AddNav("Designer / API", "Designer property + kod API kullanımı", () => new DesignerApiSampleForm());
        AddNav("GLV geçiş / Feature Pack", "Hyperlink, toggle, tags, sparkline, seçim", () => new GLVFeaturePackSampleForm());
        AddNav("OLV ekstra uyumluluk yardımcıları", "Seçim snapshot/restore, aspect ile bulma, kolon göster/gizle ve checked yardımcıları", () => new OLVExtrasSampleForm());
        AddNav("ViewGrid/GLV uyumluluk / SetObjects", "SetObjects, AddObject, RemoveObject, seçim ve checked nesne uyumluluğu", () => new ViewGridCompatSampleForm());
        AddNav("ViewGrid kolon özellikleri uyumluluk", "ViewGrid kolon propertyleri, header checkbox, button sizing ve hyperlink davranışı", () => new ViewGridColumnCompatibilitySampleForm());
        AddNav("Her Kolonda Checkbox", "Person, Operasyon, Onay ve Kilit kolonlarında checkbox + metin, header checkbox ve Space toggle testi", () => new MultiColumnCheckBoxSampleForm());
        AddNav("Kart / Geniş Kart Checkbox", "Tile, Kart, Geniş Kart ve Poster modlarında overlay checkbox, header senkronu ve farklı checkbox konumları", () => new TileCardCheckBoxSampleForm());
        AddNav("ViewGrid Designer Smart Tag", "Visual Studio Designer hızlı görev menüsü ve ViewGrid görevleri", () => new ViewGridDesignerSmartTagSampleForm());
        AddNav("Klavye erişimi / Shortcut testi", "Tüm ViewGrid işlemlerine klavye ile erişim ve düzenleme kısayolları", () => new KeyboardAccessibilitySampleForm());
        AddNav("Menu Visibility Manager", "Örnek menüleri ve ViewGrid yerleşik menülerini göster/gizle", () => new MenuVisibilityManagerSampleForm());
        AddNav("Database örnekleri", "DataTable, repository, SQL template, async refresh", () => new DatabaseSampleForm());
        AddNav("Database CRUD + hücre editörleri", "DataSource, AutoGenerateColumns, Add/Delete/SaveChanges ve Text/Check/Combo/Numeric/Date editörleri", () => new DatabaseCrudEditorsSampleForm());
        AddNav("Excel filtre satırı", "Başlık altı hızlı filtre kutuları: string, bool, numeric, date", () => new ExcelFilterRowSampleForm());
        AddNav("Master-detail", "Üst kayıt seçilince alt ViewGrid detaylarını yükler", () => new MasterDetailSampleForm());
        AddNav("Frozen + command + row details", "Sabit kolon, satır butonları ve açılır detay paneli", () => new FrozenCommandDetailsSampleForm());
        AddNav("Conditional formatting", "Kural bazlı satır/hücre renklendirme ve ikon mantığı", () => new ConditionalFormattingSampleForm());
        AddNav("TreeView + TreeGrid", "TreeView master/detail ve hiyerarşik ViewGrid", () => new TreeViewSampleForm());
        AddNav("Column Manager", "Kolon sürükle-bırak, autosave, preset", () => new ColumnManagerSampleForm());
        AddNav("Dil / Localization", "TR/EN/DE/FR/ES/IT/RU/AR/CN/JP", () => new LocalizationSampleForm());
        AddSection("Premium UX / v26.03");
        AddNav("Dialog Theme / Resize Showcase", "Yardımcı pencerelerde tema, resize ve sistem menüsü standardını test eder", () => new DialogThemeResizeShowcaseForm());
        AddNav("Örnek merkezi arama testi", "Sol menüde canlı arama, açıklama içinde arama ve seçim vurgusu", () => new SampleCenterSearchDemoForm());
        AddNav("Toast Notification Demo", "ViewGridToast info/success/warning/error bildirimi", () => new ToastNotificationSampleForm());
        AddNav("Search Panel Demo", "Ctrl+F modern arama paneli, ileri/geri bulma ve filtreleme", () => new SearchPanelDemoForm());
        AddNav("Async Filter Demo", "Standart listede hızlı popup filtre ve async değer yükleme ayarları", () => new AsyncFilterDemoForm());
        AddNav("Theme Preview Demo", "Light/Dark/Fluent tema geçişleri ve accent önizleme", () => new ThemePreviewDemoForm());
        AddNav("Dialog Showcase", "Tema uyumlu, boyutlandırılabilir dialog ve sistem menüsü standardı", () => new DialogShowcaseSampleForm());
        AddNav("Window Chrome Demo", "Dark caption, resize, minimize/maximize kapatma ve shadow testi", () => new WindowChromeDemoForm());
        AddNav("Tile Advanced Demo", "Responsive tile/kart görünümü, görsel alan ve seçili kart davranışı", () => new TileAdvancedDemoForm());
        AddNav("Virtualization Stress Test", "1M+ sanal veri yaklaşımı, hızlı filtre ve scroll senaryosu", () => new VirtualizationStressDemoForm());
        AddNav("Layout Profiles Demo", "Kullanıcı layout profili kaydet/yükle, export/import yaklaşımı", () => new LayoutProfilesDemoForm());
        AddNav("Keyboard Navigation Demo", "Space checkbox, Enter, F2 ve klavye erişilebilirlik akışı", () => new KeyboardNavigationDemoForm());
        AddNav("Excel Filter Demo", "Popup filtre, değer listesi, hızlı temizleme ve pencere filtre davranışı", () => new ExcelFilterDemoForm());
        AddNav("Designer Preview Demo", "Design-time sample data, property preview ve güvenli designer yaklaşımı", () => new DesignerPreviewHubDemoForm());
        RefreshSectionVisibility();
    }

    private void SortSampleButtonsAlphabetically()
    {
        var buttons = _nav.Controls.OfType<Button>()
            .OrderBy(b => b.Text?.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0], StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        int index = 2; // 0: arama kutusu, 1: bölüm başlığı
        foreach (var button in buttons)
            _nav.Controls.SetChildIndex(button, index++);
    }

    private void ApplySampleSearchFilter()
    {
        string text = _searchBox.Text.Trim();
        foreach (Control control in _nav.Controls)
        {
            if (control is Button button)
            {
                string searchable = (button.Text ?? string.Empty) + " " + (button.Tag?.ToString() ?? string.Empty);
                button.Visible = string.IsNullOrWhiteSpace(text) ||
                    searchable.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0;
            }
        }

        RefreshSectionVisibility();
    }

    private void RefreshSectionVisibility()
    {
        bool searching = !string.IsNullOrWhiteSpace(_searchBox.Text);
        for (int i = 0; i < _nav.Controls.Count; i++)
        {
            if (_nav.Controls[i] is Label sectionLabel)
            {
                if (!searching)
                {
                    sectionLabel.Visible = true;
                    continue;
                }

                bool hasVisibleButton = false;
                for (int j = i + 1; j < _nav.Controls.Count; j++)
                {
                    if (_nav.Controls[j] is Label)
                        break;

                    if (_nav.Controls[j] is Button nextButton && nextButton.Visible)
                    {
                        hasVisibleButton = true;
                        break;
                    }
                }

                sectionLabel.Visible = hasVisibleButton;
            }
        }
    }

    private void AddSection(string text)
    {
        var label = new Label
        {
            Width = _nav.Width - 34,
            Height = 32,
            Margin = new Padding(0, 10, 0, 4),
            Padding = new Padding(4, 7, 4, 0),
            Text = text,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = _theme.PanelBackColor,
            ForeColor = _theme.AccentColor
        };
        _nav.Controls.Add(label);
    }

    private void AddNav(string title, string description, Func<Form> createForm)
    {
        var button = new Button
        {
            Width = _nav.Width - 34,
            Height = 58,
            Margin = new Padding(0, 0, 0, 7),
            Padding = new Padding(10, 4, 10, 4),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = title + Environment.NewLine + description,
            Tag = title + " " + description,
            FlatStyle = FlatStyle.Flat,
            BackColor = _theme.BackColor,
            ForeColor = _theme.ForeColor,
            Font = new Font("Segoe UI", 8.75F)
        };
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.FlatAppearance.MouseOverBackColor = Blend(_theme.AccentColor, _theme.BackColor, 0.86);
        button.FlatAppearance.MouseDownBackColor = Blend(_theme.AccentColor, _theme.BackColor, 0.68);
        button.Click += (_,__) =>
        {
            SelectNavButton(button);
            ShowEmbeddedForm(title, description, createForm());
        };
        _nav.Controls.Add(button);
    }

    private void SelectNavButton(Button button)
    {
        if (_selectedNavButton != null && !_selectedNavButton.IsDisposed)
        {
            _selectedNavButton.BackColor = NavNormalBack;
            _selectedNavButton.ForeColor = NavNormalFore;
            _selectedNavButton.FlatAppearance.BorderColor = _theme.BorderColor;
            _selectedNavButton.FlatAppearance.BorderSize = 1;
            _selectedNavButton.Font = new Font("Segoe UI", 8.75F, FontStyle.Regular);
        }

        _selectedNavButton = button;
        _selectedNavButton.BackColor = NavSelectedBack;
        _selectedNavButton.ForeColor = NavSelectedFore;
        _selectedNavButton.FlatAppearance.BorderColor = NavSelectedBorder;
        _selectedNavButton.FlatAppearance.BorderSize = 2;
        _selectedNavButton.Font = new Font("Segoe UI", 8.75F, FontStyle.Bold);
    }

    private static Color Blend(Color accent, Color baseColor, double baseWeight)
    {
        baseWeight = Math.Max(0, Math.Min(1, baseWeight));
        double accentWeight = 1d - baseWeight;
        return Color.FromArgb(
            (int)(accent.R * accentWeight + baseColor.R * baseWeight),
            (int)(accent.G * accentWeight + baseColor.G * baseWeight),
            (int)(accent.B * accentWeight + baseColor.B * baseWeight));
    }

    private static Color GetReadableTextColor(Color backColor)
    {
        double luminance = (0.299 * backColor.R + 0.587 * backColor.G + 0.114 * backColor.B) / 255d;
        return luminance > 0.58 ? Color.FromArgb(32, 32, 32) : Color.White;
    }

    private void ShowEmbeddedForm(string title, string description, Form form)
    {
        _host.SuspendLayout();
        foreach (Control c in _host.Controls) c.Dispose();
        _host.Controls.Clear();

        _header.Text = "Örnekler - " + title;
        _info.Text = description;

        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        form.StartPosition = FormStartPosition.Manual;
        form.BackColor = _theme.BackColor;
        form.ForeColor = _theme.ForeColor;

        _host.Controls.Add(form);
        form.Show();
        _host.ResumeLayout(true);
    }

    private void ShowWelcome()
    {
        _selectedNavButton = null;
        var welcome = new Panel { Dock = DockStyle.Fill, BackColor = _theme.BackColor, Padding = new Padding(28) };
        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 52,
            Text = "Hoş geldin 👋",
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = _theme.ForeColor,
            BackColor = _theme.BackColor
        };
        var body = new Label
        {
            Dock = DockStyle.Top,
            Height = 170,
            Text = "Sol menüden istediğin örneği seç.\n\n" +
                   "• Ayrı pencere açmadan tek panelde test edebilirsin.\n" +
                   "• Arama kutusuna Audix, Poster, Kanban, Export, Designer veya Faz 31-37 yazabilirsin.\n" +
                   "• En üstteki Hızlı Erişim bölümü en çok kullanılan yeni özellikleri toplar.",
            Font = new Font("Segoe UI", 11F),
            ForeColor = _theme.ForeColor,
            BackColor = _theme.BackColor
        };
        welcome.Controls.Add(body);
        welcome.Controls.Add(title);
        _host.Controls.Add(welcome);
    }
}

public abstract class ViewGridSampleFormBase : Form
{
    protected readonly ViewGrid.Core.ViewGridControl ViewGrid = new()
    {
        Dock = DockStyle.Fill,
        EmptyListMessage = "Kayıt yok",
        CellEditActivationKey = Keys.F2,
        AllowEditAllCells = true,
        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full,
        CloseHeaderContextMenuBeforeOpeningFilterPopup = true
    };
    protected readonly ToolStrip Tool = new() { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top };
    protected readonly Label Info = new() { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(10), TextAlign = ContentAlignment.MiddleLeft };

    protected ViewGridSampleFormBase(string title, string info)
    {
        Text = title;
        Width = 980;
        Height = 620;
        MinimumSize = new Size(760, 460);
        var sampleTheme = Program.AppTheme;
        ViewGridWindowChrome.ApplyOnHandleCreated(this, () => sampleTheme, true);
        BackColor = sampleTheme.BackColor;
        ForeColor = sampleTheme.ForeColor;
        Info.BackColor = sampleTheme.PanelBackColor;
        Info.ForeColor = sampleTheme.ForeColor;
        Tool.BackColor = sampleTheme.HeaderBackColor;
        Tool.ForeColor = sampleTheme.HeaderForeColor;
        ViewGrid.ApplyTheme(sampleTheme);
        MainForm.ConfigureMillionRowFiltering(ViewGrid);
        Info.Text = info;
        Controls.Add(ViewGrid);
        Controls.Add(Info);
        Controls.Add(Tool);
        ApplySampleChrome(sampleTheme);
        AddSampleNavigation();
        ConfigureCommonColumns();
        ViewGrid.SetObjects(CreateRows(250));
    }

    protected void ApplySampleChrome(ViewGridTheme theme)
    {
        Program.AppTheme = theme;
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        Info.BackColor = theme.PanelBackColor;
        Info.ForeColor = theme.ForeColor;
        Tool.BackColor = theme.HeaderBackColor;
        Tool.ForeColor = theme.HeaderForeColor;
        Tool.Renderer = new ViewGrid.Theming.SmartMenuRenderer(theme);
        foreach (ToolStripItem item in Tool.Items)
            ApplyToolItemTheme(item, theme);
        ViewGrid.ApplyTheme(theme);
        ViewGridWindowChrome.Apply(this, theme, true);
    }

    private static void ApplyToolItemTheme(ToolStripItem item, ViewGridTheme theme)
    {
        item.BackColor = theme.HeaderBackColor;
        item.ForeColor = theme.HeaderForeColor;
        if (item is ToolStripDropDownItem dd)
        {
            dd.DropDown.BackColor = theme.PanelBackColor;
            dd.DropDown.ForeColor = theme.ForeColor;
            foreach (ToolStripItem child in dd.DropDownItems)
                ApplyToolItemTheme(child, theme);
        }
    }

    private void AddSampleNavigation()
    {
        Tool.Items.Add(new ToolStripButton("Örnek merkezi", null, (_,__) => new SampleHubForm().Show()));
        var jump = new ToolStripDropDownButton("Diğer örnekler");
        jump.DropDownItems.Add("Demo Hub", null, (_,__) => new SampleHubForm().Show());
        jump.DropDownItems.Add("Filtreleme", null, (_,__) => new FilteringSampleForm().Show());
        jump.DropDownItems.Add("Gruplama", null, (_,__) => new GroupingSampleForm().Show());
        jump.DropDownItems.Add("Modern ProgressBar", null, (_,__) => new ProgressBarSampleForm().Show());
        jump.DropDownItems.Add("Yazdırma / Önizleme", null, (_,__) => new PrintPreviewSampleForm().Show());
        jump.DropDownItems.Add("Görünüm modları", null, (_,__) => new ViewModesSampleForm().Show());
        jump.DropDownItems.Add("Kart", null, (_,__) => new TileSampleForm().Show());
        jump.DropDownItems.Add("Geniş Kart", null, (_,__) => new LargeCardSampleForm().Show());
        jump.DropDownItems.Add("Details Çok Satırlı Hücre", null, (_,__) => new MultilineCellsSampleForm().Show());
        jump.DropDownItems.Add("Poster / Resimli içerik", null, (_,__) => new PosterGallerySampleForm().Show());
        jump.DropDownItems.Add("Media Library / Albüm-Film-Fotoğraf", null, (_,__) => new MediaLibrarySampleForm().Show());
        jump.DropDownItems.Add("V31 Faz Merkezi", null, (_,__) => new ViewGridV31AllPhasesSampleForm().Show());
        jump.DropDownItems.Add("V30 Visual Experience Suite", null, (_,__) => new MediaLibrarySampleForm().Show());
        jump.DropDownItems.Add("V30 Gelişmiş Görünüm Vitrini", null, (_,__) => new ViewModeShowcaseSampleForm().Show());
        jump.DropDownItems.Add("Highlight arama", null, (_,__) => new HighlightSearchSampleForm().Show());
        jump.DropDownItems.Add("Satır renklendirme", null, (_,__) => new ColoringSampleForm().Show());
        jump.DropDownItems.Add("Image + Combo hücre", null, (_,__) => new ImageComboSampleForm().Show());
        jump.DropDownItems.Add("Designer önizleme", null, (_,__) => new DesignerPreviewSampleForm().Show());
        jump.DropDownItems.Add("Export", null, (_,__) => new ExportSampleForm().Show());
        jump.DropDownItems.Add("v28.7 PDF Export", null, (_,__) => new PdfExportSuiteSampleForm().Show());
        jump.DropDownItems.Add("v28.8 Hücre İçi Scroll", null, (_,__) => new CellOverflowScrollSampleForm().Show());
        jump.DropDownItems.Add("999M sanal liste", null, (_,__) => new MillionRowsSampleForm().Show());
        jump.DropDownItems.Add("Designer/API", null, (_,__) => new DesignerApiSampleForm().Show());
        jump.DropDownItems.Add("GLV geçiş / feature pack", null, (_,__) => new GLVFeaturePackSampleForm().Show());
        jump.DropDownItems.Add("Column Manager", null, (_,__) => new ColumnManagerSampleForm().Show());
        jump.DropDownItems.Add("ViewGrid kolon özellikleri", null, (_,__) => new ViewGridColumnCompatibilitySampleForm().Show());
        jump.DropDownItems.Add("Her kolonda checkbox", null, (_,__) => new MultiColumnCheckBoxSampleForm().Show());
        jump.DropDownItems.Add("Kart / Geniş Kart Checkbox", null, (_,__) => new TileCardCheckBoxSampleForm().Show());
        jump.DropDownItems.Add("ViewGrid Designer Smart Tag", null, (_,__) => new ViewGridDesignerSmartTagSampleForm().Show());
        jump.DropDownItems.Add("Menu Visibility Manager", null, (_,__) => new MenuVisibilityManagerSampleForm().Show());
        jump.DropDownItems.Add("Dil / Localization", null, (_,__) => new LocalizationSampleForm().Show());
        Tool.Items.Add(jump);
        var themeMenu = new ToolStripDropDownButton("Tema");
        themeMenu.DropDownItems.Add("Windows otomatik", null, (_,__) => ApplySampleChrome(WindowsThemeService.CurrentTheme()));
        themeMenu.DropDownItems.Add("Açık tema", null, (_,__) => ApplySampleChrome(ViewGridTheme.LightTheme()));
        themeMenu.DropDownItems.Add("Koyu tema", null, (_,__) => ApplySampleChrome(ViewGridTheme.DarkTheme()));
        themeMenu.DropDownItems.Add("Fluent açık", null, (_,__) => ApplySampleChrome(ViewGridTheme.FluentLightTheme()));
        themeMenu.DropDownItems.Add("Fluent koyu", null, (_,__) => ApplySampleChrome(ViewGridTheme.FluentDarkTheme()));
        Tool.Items.Add(themeMenu);
        Tool.Items.Add(new ToolStripSeparator());
        ApplySampleChrome(Program.AppTheme);
    }

    protected void ConfigureCommonColumns()
    {
        ViewGrid.Columns.Add(new ViewGridColumn("✓", nameof(DemoRow.Checked), 42) { Kind = ViewGridColumnKind.CheckBox, AspectPutter = (row, value) => ((DemoRow)row).SetChecked(Convert.ToBoolean(value)) });
        ViewGrid.Columns.Add(new ViewGridColumn("Id", nameof(DemoRow.Id), 70));
        ViewGrid.Columns.Add(new ViewGridColumn("Ad", nameof(DemoRow.Name), 240) { Editable = true, FillFreeSpace = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Meslek", nameof(DemoRow.Occupation), 140));
        ViewGrid.Columns.Add(new ViewGridColumn("Durum", nameof(DemoRow.State), 110) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("İlerleme", nameof(DemoRow.Progress), 140) { Kind = ViewGridColumnKind.ProgressBar });
        ViewGrid.Columns.Add(new ViewGridColumn("Puan", nameof(DemoRow.Rating), 110) { Kind = ViewGridColumnKind.Rating, MaxRating = 5 });
    }

    protected static List<DemoRow> CreateRows(int count)
    {
        return Enumerable.Range(1, count).Select(i => new DemoRow
        {
            Id = i,
            Name = "AOI kayıt " + i,
            State = i % 7 == 0 ? "Fail" : i % 5 == 0 ? "Review" : "OK",
            Occupation = DemoData.Occupations[i % DemoData.Occupations.Length],
            Progress = (i * 7) % 101,
            Rating = i % 6,
            Checked = i % 8 == 0,
            NeedsReview = i % 12 == 0,
            Approved = i % 5 == 0,
            Locked = i % 9 == 0,
            ActionText = "Aç",
            Tags = i % 3 == 0 ? "AOI,Fail,Review" : i % 2 == 0 ? "ViewGrid,Filter,Tile" : "Fast,Virtual,Theme",
            Spark = $"{(i*3)%20},{(i*5)%35},{(i*7)%55},{(i*11)%80},{(i*13)%100}",
            Link = "Aç / Detay",
            RowColor = i % 7 == 0 ? "#E53935" : i % 5 == 0 ? "#7E57C2" : "#43A047",
            Year = 2020 + (i % 7),
            PosterIndex = i % 12
        }).ToList();
    }
}

public sealed class MultiColumnCheckBoxSampleForm : ViewGridSampleFormBase
{
    public MultiColumnCheckBoxSampleForm() : base("Her Kolonda Checkbox", "Test: Checkbox sadece ilk seçim kolonu değil; Person, Operasyon, Onay ve Kilit gibi normal veri kolonlarında metinle birlikte ayrı ayrı kullanılabilir.")
    {
        ViewGrid.Columns.Clear();
        ViewGrid.FullRowSelect = true;
        ViewGrid.MultiSelect = true;
        ViewGrid.ShowGridLines = true;
        ViewGrid.KeyboardSpaceTogglesCheckBoxes = true;

        ViewGrid.Columns.Add(new ViewGridColumn("Person", nameof(DemoRow.Name), 190)
        {
            CellCheckBox = true,
            HeaderCheckBox = true,
            HeaderCheckBoxThreeState = true,
            CheckBoxAspectName = nameof(DemoRow.Checked),
            FillFreeSpace = false
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Operasyon", nameof(DemoRow.Occupation), 150)
        {
            CellCheckBox = true,
            HeaderCheckBox = true,
            HeaderCheckBoxThreeState = true,
            CheckBoxAspectName = nameof(DemoRow.NeedsReview)
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Onay", nameof(DemoRow.State), 120)
        {
            CellCheckBox = true,
            HeaderCheckBox = true,
            HeaderCheckBoxThreeState = true,
            CheckBoxAspectName = nameof(DemoRow.Approved)
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Kilit", nameof(DemoRow.Tags), 180)
        {
            CellCheckBox = true,
            HeaderCheckBox = true,
            HeaderCheckBoxThreeState = true,
            CheckBoxAspectName = nameof(DemoRow.Locked)
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Action", nameof(DemoRow.ActionText), 110) { Kind = ViewGridColumnKind.Button, ButtonText = "Aç" });
        ViewGrid.Columns.Add(new ViewGridColumn("Link", nameof(DemoRow.Link), 120) { Kind = ViewGridColumnKind.Hyperlink });
        ViewGrid.Columns.Add(new ViewGridColumn("Notes", nameof(DemoRow.Tags), 420) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 2 });

        ViewGrid.ButtonClick += (_, e) => Info.Text = $"Action tıklandı: {((DemoRow)e.RowObject).Name}";
        ViewGrid.CellValueChanged += (_, e) =>
        {
            if (e.RowObject is DemoRow row)
            {
                Info.Text = $"Checkbox değişti: {row.Name} / {e.Column.Header}";
            }
            else
            {
                Info.Text = $"Header checkbox değişti: {e.Column.Header} = {e.NewValue}";
            }
        };
        ViewGrid.HyperlinkClick += (_, e) => Info.Text = $"Link tıklandı: {((DemoRow)e.RowObject).Link}";

        Tool.Items.Add(new ToolStripButton("Person header toggle", null, (_,__) => ToggleHeader("Person")));
        Tool.Items.Add(new ToolStripButton("Operasyon header toggle", null, (_,__) => ToggleHeader("Operasyon")));
        Tool.Items.Add(new ToolStripButton("Onay header toggle", null, (_,__) => ToggleHeader("Onay")));
        Tool.Items.Add(new ToolStripButton("Kilit header toggle", null, (_,__) => ToggleHeader("Kilit")));
        Tool.Items.Add(new ToolStripButton("Kolon seçici", null, (_,__) => ViewGrid.ShowColumnChooser()));

        var rows = CreateRows(80);
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].Name = i < 5 ? new[] { "Wilhelm Frat", "Alana Roderick", "Frank Price", "Eric", "Nicola Scotts" }[i] : "Person " + (i + 1);
            rows[i].Tags = "ViewGrid kolon checkbox uyumluluğu test satırı " + (i + 1);
            rows[i].Approved = i % 3 == 0;
            rows[i].Locked = i % 7 == 0;
        }
        ViewGrid.SetObjects(rows);
        Info.Text = "Örnek merkezindeki bu demo; birden fazla normal veri kolonunda checkbox + metin, header checkbox, Space toggle, button ve hyperlink davranışını birlikte doğrular.";
    }

    private void ToggleHeader(string columnName)
    {
        var c = ViewGrid.Columns[columnName];
        if (c == null) return;

        c.HeaderCheckState = c.HeaderCheckState == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
        ViewGrid.Invalidate();
        Info.Text = columnName + " header checkbox durumu değiştirildi.";
    }
}


public sealed class TileCardCheckBoxSampleForm : ViewGridSampleFormBase
{
    public TileCardCheckBoxSampleForm() : base("Kart / Geniş Kart Checkbox", "Tile, Kart, Geniş Kart ve Poster görünümünde kart üzerinde checkbox overlay desteği; header checkbox ile senkron, designer propertyleri ve konum ayarı test edilir.")
    {
        ViewGrid.Columns.Clear();
        ViewGrid.FullRowSelect = true;
        ViewGrid.MultiSelect = true;
        ViewGrid.TileCheckBoxes = true;
        ViewGrid.TileCheckBoxAspectName = nameof(DemoRow.Checked);
        ViewGrid.TileCheckBoxPosition = ViewGridTileCheckBoxPosition.TopRight;
        ViewGrid.TileCheckBoxSize = 20;
        ViewGrid.TileCheckBoxMargin = 10;
        ViewGrid.TilePreferredWidth = 280;
        ViewGrid.TilePreferredHeight = 112;
        ViewGrid.TileMaxTextLines = 4;
        ViewGrid.LargeCardPreferredWidth = 560;
        ViewGrid.LargeCardPreferredHeight = 176;
        ViewGrid.LargeCardMaxTextLines = 8;
        ViewGrid.TileShowAllVisibleTextColumns = true;
        ViewGrid.KeyboardSpaceTogglesCheckBoxes = true;

        ViewGrid.Columns.Add(new ViewGridColumn("Seç", nameof(DemoRow.Checked), 70)
        {
            Kind = ViewGridColumnKind.CheckBox,
            HeaderCheckBox = true,
            HeaderCheckBoxThreeState = true,
            AspectPutter = (row, value) => ((DemoRow)row).SetChecked(Convert.ToBoolean(value))
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Ad", nameof(DemoRow.Name), 220) { Editable = true, FillFreeSpace = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Durum", nameof(DemoRow.State), 110) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("Meslek", nameof(DemoRow.Occupation), 150));
        ViewGrid.Columns.Add(new ViewGridColumn("Not", nameof(DemoRow.Tags), 330) { WordWrap = true, MaxTextLines = 3 });
        ViewGrid.Columns.Add(new ViewGridColumn("İlerleme", nameof(DemoRow.Progress), 120) { Kind = ViewGridColumnKind.ProgressBar });

        Tool.Items.Add(new ToolStripButton("Detay", null, (_,__) => SetMode(ViewGridMode.Details)));
        Tool.Items.Add(new ToolStripButton("Kart", null, (_,__) => SetMode(ViewGridMode.Tile)));
        Tool.Items.Add(new ToolStripButton("Geniş Kart", null, (_,__) => SetMode(ViewGridMode.LargeCard)));
        Tool.Items.Add(new ToolStripButton("Poster", null, (_,__) =>
        {
            ViewGrid.TilePosterMode = !ViewGrid.TilePosterMode;
            ViewGrid.TilePosterImageHeight = 120;
            SetMode(ViewGridMode.Tile);
            Info.Text = "Poster modu = " + ViewGrid.TilePosterMode;
        }));
        Tool.Items.Add(new ToolStripButton("Checkbox Aç/Kapat", null, (_,__) =>
        {
            ViewGrid.TileCheckBoxes = !ViewGrid.TileCheckBoxes;
            Info.Text = "TileCheckBoxes = " + ViewGrid.TileCheckBoxes;
        }));
        Tool.Items.Add(new ToolStripButton("Sol Üst", null, (_,__) => SetCheckBoxPosition(ViewGridTileCheckBoxPosition.TopLeft)));
        Tool.Items.Add(new ToolStripButton("Sağ Üst", null, (_,__) => SetCheckBoxPosition(ViewGridTileCheckBoxPosition.TopRight)));
        Tool.Items.Add(new ToolStripButton("Sol Alt", null, (_,__) => SetCheckBoxPosition(ViewGridTileCheckBoxPosition.BottomLeft)));
        Tool.Items.Add(new ToolStripButton("Sağ Alt", null, (_,__) => SetCheckBoxPosition(ViewGridTileCheckBoxPosition.BottomRight)));
        Tool.Items.Add(new ToolStripButton("Kenar +", null, (_,__) => ChangeMargin(2)));
        Tool.Items.Add(new ToolStripButton("Kenar -", null, (_,__) => ChangeMargin(-2)));
        Tool.Items.Add(new ToolStripButton("Header Toggle", null, (_,__) => ToggleHeaderCheckBox()));

        var rows = CreateRows(90);
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].Name = "Kart checkbox kayıt " + (i + 1);
            rows[i].Tags = "Kart modunda checkbox, header senkronu ve seçim durumu test satırı " + (i + 1);
            rows[i].Checked = i % 4 == 0;
        }

        ViewGrid.SetObjects(rows);
        ViewGrid.SetViewMode(ViewGridMode.Tile);
        Info.Text = "Kart checkbox köşesini Sol Üst/Sağ Üst/Sol Alt/Sağ Alt butonlarıyla runtime değiştirin; aynı ayar designer PropertyGrid üzerinden de yapılabilir.";
    }

    private void SetMode(ViewGridMode mode)
    {
        ViewGrid.SetViewMode(mode);
        Info.Text = "Görünüm = " + mode + " / TileCheckBoxes = " + ViewGrid.TileCheckBoxes + " / Konum = " + ViewGrid.TileCheckBoxPosition;
    }

    private void SetCheckBoxPosition(ViewGridTileCheckBoxPosition position)
    {
        ViewGrid.TileCheckBoxPosition = position;
        ViewGrid.Invalidate();
        Info.Text = "TileCheckBoxPosition = " + ViewGrid.TileCheckBoxPosition + " / Margin = " + ViewGrid.TileCheckBoxMargin;
    }

    private void ChangeMargin(int delta)
    {
        ViewGrid.TileCheckBoxMargin = Math.Max(0, Math.Min(48, ViewGrid.TileCheckBoxMargin + delta));
        ViewGrid.Invalidate();
        Info.Text = "TileCheckBoxPosition = " + ViewGrid.TileCheckBoxPosition + " / Margin = " + ViewGrid.TileCheckBoxMargin;
    }

    private void ToggleHeaderCheckBox()
    {
        var c = ViewGrid.Columns["Seç"];
        if (c == null) return;
        c.HeaderCheckState = c.HeaderCheckState == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
        ViewGrid.Invalidate();
        Info.Text = "Header checkbox görsel durumu değiştirildi. Asıl toplu toggle için header checkbox'a tıklayın.";
    }
}


public sealed class SampleCenterSearchDemoForm : ViewGridSampleFormBase
{
    public SampleCenterSearchDemoForm() : base("Örnek Merkezi Arama Testi", "Örnek merkezi içinde arama devam ediyor. Başlık ve açıklama metinleri birlikte aranır; bölüm başlıkları arama sırasında gizlenir.")
    {
        ConfigureCommonColumns();
        ViewGrid.SetObjects(CreateRows(80));
        Info.Text = "SampleHubForm sol menüsündeki arama kutusu artık hem örnek adında hem açıklamasında arama yapar. Örn: toast, tema, filter, keyboard, designer.";
    }
}

public sealed class ToastNotificationSampleForm : ViewGridSampleFormBase
{
    public ToastNotificationSampleForm() : base("Toast Notification Demo", "ViewGridToast bildirimleri host formun sağ altında tema uyumlu şekilde gösterir.")
    {
        ConfigureCommonColumns();
        ViewGrid.SetObjects(CreateRows(45));
        Tool.Items.Add(new ToolStripButton("Info", null, (_,__) => ViewGrid.ShowToast("Bilgilendirme bildirimi gösterildi.", ViewGridToastKind.Info)));
        Tool.Items.Add(new ToolStripButton("Success", null, (_,__) => ViewGrid.ShowToast("İşlem başarıyla tamamlandı.", ViewGridToastKind.Success)));
        Tool.Items.Add(new ToolStripButton("Warning", null, (_,__) => ViewGrid.ShowToast("Kontrol edilmesi gereken kayıtlar var.", ViewGridToastKind.Warning)));
        Tool.Items.Add(new ToolStripButton("Error", null, (_,__) => ViewGrid.ShowToast("Örnek hata bildirimi.", ViewGridToastKind.Error)));
    }
}

public sealed class SearchPanelDemoForm : ViewGridSampleFormBase
{
    public SearchPanelDemoForm() : base("Search Panel Demo", "Ctrl+F veya toolbar ile modern arama panelini açar. İleri/geri arama ve isteğe bağlı filtreleme test edilir.")
    {
        ConfigureCommonColumns();
        ViewGrid.EnableModernSearchPanel = true;
        ViewGrid.SearchPanelCanFilterResults = false;
        ViewGrid.SetObjects(CreateRows(500));
        Tool.Items.Add(new ToolStripButton("Modern Arama Paneli", null, (_,__) => ViewGrid.ShowModernSearchPanel()));
        Tool.Items.Add(new ToolStripButton("Panel filtre modunu aç/kapat", null, (_,__) =>
        {
            ViewGrid.SearchPanelCanFilterResults = !ViewGrid.SearchPanelCanFilterResults;
            Info.Text = "SearchPanelCanFilterResults = " + ViewGrid.SearchPanelCanFilterResults;
        }));
    }
}

public sealed class AsyncFilterDemoForm : ViewGridSampleFormBase
{
    public AsyncFilterDemoForm() : base("Async Filter Demo", "Standart listelerde popup filtre değerleri daha hızlı açılsın diye async/full scan ayarları tek örnekte gösterilir.")
    {
        ConfigureCommonColumns();
        MainForm.ConfigureMillionRowFiltering(ViewGrid);
        ViewGrid.SetObjects(CreateRows(15000));
        Tool.Items.Add(new ToolStripButton("Ad filtresini aç", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.Name))));
        Tool.Items.Add(new ToolStripButton("Durum filtresini aç", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.State))));
        Info.Text = "FastFilterMenuForHugeLists, AsyncLoadFullFilterValues ve typed filter scan ayarları aktif. Daha büyük veri için Virtualization Stress Test örneğine bak.";
    }
}

public sealed class ThemePreviewDemoForm : ViewGridSampleFormBase
{
    public ThemePreviewDemoForm() : base("Theme Preview Demo", "Light, Dark ve Fluent temalar ViewGrid + host kontrollerinde birlikte test edilir.")
    {
        ConfigureCommonColumns();
        ViewGrid.SetObjects(CreateRows(120));
        Tool.Items.Add(new ToolStripButton("Light", null, (_,__) => ApplySampleChrome(ViewGridTheme.LightTheme())));
        Tool.Items.Add(new ToolStripButton("Dark", null, (_,__) => ApplySampleChrome(ViewGridTheme.DarkTheme())));
        Tool.Items.Add(new ToolStripButton("Fluent Light", null, (_,__) => ApplySampleChrome(ViewGridTheme.FluentLightTheme())));
        Tool.Items.Add(new ToolStripButton("Fluent Dark", null, (_,__) => ApplySampleChrome(ViewGridTheme.FluentDarkTheme())));
    }
}

public sealed class DialogShowcaseSampleForm : ViewGridSampleFormBase
{
    public DialogShowcaseSampleForm() : base("Dialog Showcase", "Filtre, kolon seçici, komut paleti ve arama paneli gibi yardımcı pencerelerde tema/resize/sistem menüsü standardı kontrol edilir.")
    {
        ConfigureCommonColumns();
        ViewGrid.SetObjects(CreateRows(180));
        Tool.Items.Add(new ToolStripButton("Filtre Penceresi", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.Name))));
        Tool.Items.Add(new ToolStripButton("Kolon Seçici", null, (_,__) => ViewGrid.ShowColumnChooser()));
        Tool.Items.Add(new ToolStripButton("Arama Paneli", null, (_,__) => ViewGrid.ShowModernSearchPanel()));
        Tool.Items.Add(new ToolStripButton("Toast", null, (_,__) => ViewGrid.ShowToast("Dialog standardı örneği", ViewGridToastKind.Info)));
    }
}

public sealed class WindowChromeDemoForm : ViewGridSampleFormBase
{
    public WindowChromeDemoForm() : base("Window Chrome Demo", "Dark caption, resize, shadow ve sistem menüsündeki kullanılmayan öğelerin temizlenmesi için bağımsız pencere açar.")
    {
        ConfigureCommonColumns();
        ViewGrid.SetObjects(CreateRows(60));
        Tool.Items.Add(new ToolStripButton("Sizable Dialog Aç", null, (_,__) => ShowChromeDialog(true)));
        Tool.Items.Add(new ToolStripButton("Fixed Dialog Aç", null, (_,__) => ShowChromeDialog(false)));
    }

    private void ShowChromeDialog(bool sizable)
    {
        using var form = new Form
        {
            Text = sizable ? "ViewGrid sizeable themed dialog" : "ViewGrid fixed themed dialog",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(520, 340),
            MinimumSize = new Size(420, 260),
            Font = Font
        };
        ViewGridDialogChrome.ConfigureStandardDialog(form, Program.AppTheme, new Size(520, 340), sizable);
        form.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "Başlık çubuğu tema uyumu, resize davranışı, shadow ve sistem menüsü bu pencerede kontrol edilir."
        });
        ViewGridDialogThemeApplier.Apply(form, Program.AppTheme);
        form.ShowDialog(this);
    }
}

public sealed class TileAdvancedDemoForm : ViewGridSampleFormBase
{
    public TileAdvancedDemoForm() : base("Tile Advanced Demo", "Responsive tile/kart görünümü; yatay/dikey boyut değişiminde taşma ve seçim davranışı test edilir.")
    {
        ConfigureCommonColumns();
        ViewGrid.SetObjects(CreateRows(240));
        ViewGrid.SetViewMode(ViewGridMode.Tile);
        Tool.Items.Add(new ToolStripButton("Tile", null, (_,__) => ViewGrid.SetViewMode(ViewGridMode.Tile)));
        Tool.Items.Add(new ToolStripButton("Details", null, (_,__) => ViewGrid.SetViewMode(ViewGridMode.Details)));
        Tool.Items.Add(new ToolStripButton("Large Icons", null, (_,__) => ViewGrid.SetViewMode(ViewGridMode.LargeIcons)));
    }
}

public sealed class VirtualizationStressDemoForm : ViewGridSampleFormBase
{
    public VirtualizationStressDemoForm() : base("Virtualization Stress Test", "1M+ satır senaryosunda provider tabanlı liste, hızlı filtre ve scroll davranışı test edilir.")
    {
        ConfigureCommonColumns();
        MainForm.ConfigureMillionRowFiltering(ViewGrid);
        ViewGrid.SetVirtualProvider(new VirtualDemoRowProvider(1_500_000));
        Tool.Items.Add(new ToolStripButton("Ad filtresi", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.Name))));
        Tool.Items.Add(new ToolStripButton("Durum filtresi", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.State))));
        Tool.Items.Add(new ToolStripButton("999M provider", null, (_,__) => ViewGrid.SetVirtualProvider(new VirtualDemoRowProvider(999_000_000))));
    }
}

public sealed class LayoutProfilesDemoForm : ViewGridSampleFormBase
{
    public LayoutProfilesDemoForm() : base("Layout Profiles Demo", "Kolon görünürlük/sıra/genişlik ayarlarını dosyaya kaydetme ve geri yükleme senaryosu.")
    {
        ConfigureCommonColumns();
        ViewGrid.SetObjects(CreateRows(100));
        Tool.Items.Add(new ToolStripButton("Layout Kaydet", null, (_,__) => ViewGrid.SaveLayout(Path.Combine(Application.StartupPath, "viewgrid-sample-layout.json"))));
        Tool.Items.Add(new ToolStripButton("Layout Yükle", null, (_,__) => ViewGrid.LoadLayout(Path.Combine(Application.StartupPath, "viewgrid-sample-layout.json"))));
        Tool.Items.Add(new ToolStripButton("Kolon Seçici", null, (_,__) => ViewGrid.ShowColumnChooser()));
    }
}

public sealed class KeyboardNavigationDemoForm : ViewGridSampleFormBase
{
    public KeyboardNavigationDemoForm() : base("Keyboard Navigation Demo", "Space checkbox toggle, Enter/F2 edit akışı ve klavye ile menü erişimi için hızlı test ekranı.")
    {
        ConfigureCommonColumns();
        ViewGrid.CellEditActivationKey = Keys.F2;
        ViewGrid.AllowEditAllCells = true;
        ViewGrid.SetObjects(CreateRows(70));
        Info.Text = "İlk checkbox kolonundayken Space check/uncheck yapmalı. F2 edit, Ctrl+F arama paneli, header menü sağ tık/klavye ile test edilebilir.";
        Tool.Items.Add(new ToolStripButton("Ctrl+F Panel", null, (_,__) => ViewGrid.ShowModernSearchPanel()));
    }
}

public sealed class ExcelFilterDemoForm : ViewGridSampleFormBase
{
    public ExcelFilterDemoForm() : base("Excel Filter Demo", "Popup filtre ve ayrı filtre penceresi davranışını Excel benzeri akışta test eder.")
    {
        ConfigureCommonColumns();
        ViewGrid.FilterMenuMode = ViewGridFilterMenuMode.Both;
        ViewGrid.SetObjects(CreateRows(320));
        Tool.Items.Add(new ToolStripButton("Ad filtre", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.Name))));
        Tool.Items.Add(new ToolStripButton("Meslek filtre", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.Occupation))));
        Tool.Items.Add(new ToolStripButton("Temizle", null, (_,__) => ViewGrid.ClearFilters()));
    }
}

public sealed class DesignerPreviewHubDemoForm : ViewGridSampleFormBase
{
    public DesignerPreviewHubDemoForm() : base("Designer Preview Demo", "Design-time sample data runtime'a sızmasın; özellikler designer-safe şekilde preview edilsin diye kontrol ekranı.")
    {
        ConfigureCommonColumns();
        ViewGrid.DesignTimeSampleData = false;
        ViewGrid.SetObjects(CreateRows(36));
        Tool.Items.Add(new ToolStripButton("Kolon düzenleyici", null, (_,__) => ViewGrid.ShowColumnChooser()));
        Info.Text = "Designer.cs tarafında gerçek kontrol/layout korunur; runtime sample data istemeden gelmemeli. Bu örnek designer-safe property davranışlarını temsil eder.";
    }
}

public sealed class ProgressBarSampleForm : ViewGridSampleFormBase
{
    public ProgressBarSampleForm() : base("ViewGrid Modern ProgressBar Örneği", "ProgressBar animasyonu varsayılan kapalıdır. Görünüm tema, satır rengi ve accent renkle uyumludur; istenirse kod/designer ile açılır.")
    {
        ViewGrid.ProgressBarAnimated = false;
        ViewGrid.ProgressBarUseGradient = true;
        ViewGrid.ProgressBarShowText = true;
        ViewGrid.ProgressBarUseAccentColor = true;
        ViewGrid.RowColorPreset = ViewGridRowColorPreset.StatusPills;
        ViewGrid.RowColorAspectName = nameof(DemoRow.State);
        ViewGrid.RowColorStrength = 0.16;
        Tool.Items.Add(new ToolStripButton("Animasyon Aç/Kapat", null, (_,__) => { ViewGrid.ProgressBarAnimated = !ViewGrid.ProgressBarAnimated; ViewGrid.Invalidate(); Info.Text = "ProgressBarAnimated = " + ViewGrid.ProgressBarAnimated + "  (büyük listelerde kapalı önerilir)"; }));
        Tool.Items.Add(new ToolStripButton("Accent renk", null, (_,__) => { ViewGrid.ProgressBarUseAccentColor = true; ViewGrid.Invalidate(); }));
        Tool.Items.Add(new ToolStripButton("Düşük/orta/yüksek renk", null, (_,__) => { ViewGrid.ProgressBarUseAccentColor = false; ViewGrid.Invalidate(); }));
        Tool.Items.Add(new ToolStripButton("Gradient Aç/Kapat", null, (_,__) => { ViewGrid.ProgressBarUseGradient = !ViewGrid.ProgressBarUseGradient; ViewGrid.Invalidate(); }));
    }
}

public sealed class PrintPreviewSampleForm : ViewGridSampleFormBase
{
    public PrintPreviewSampleForm() : base("ViewGrid Yazdırma / Önizleme Örneği", "ViewGrid/GLV tarzı önizleme desteği: tüm görünür satırlar, seçili satırlar, PageSetup, PrintPreview ve Print.")
    {
        ViewGrid.PrintTitle = "ViewGridControl Yazdırma Önizleme Örneği";
        ViewGrid.PrintMaxRows = 5000;
        Tool.Items.Add(new ToolStripButton("Önizleme", null, (_,__) => ViewGrid.ShowPrintPreview("ViewGridControl Yazdırma Önizleme Örneği")));
        Tool.Items.Add(new ToolStripButton("Yazdır", null, (_,__) => ViewGrid.Print("ViewGridControl Yazdırma Önizleme Örneği")));
        Tool.Items.Add(new ToolStripButton("Sayfa Ayarı", null, (_,__) => ViewGrid.ShowPageSetup("ViewGridControl Yazdırma Önizleme Örneği")));
        Tool.Items.Add(new ToolStripButton("Sadece Seçili Satırlar", null, (_,__) => { ViewGrid.PrintSelectedRowsOnly = !ViewGrid.PrintSelectedRowsOnly; Info.Text = "PrintSelectedRowsOnly = " + ViewGrid.PrintSelectedRowsOnly + " | Çoklu seçim yapıp önizleme açabilirsin."; }));
        Tool.Items.Add(new ToolStripButton("Tema Rengi Aç/Kapat", null, (_,__) => { ViewGrid.PrintUseThemeColors = !ViewGrid.PrintUseThemeColors; Info.Text = "PrintUseThemeColors = " + ViewGrid.PrintUseThemeColors + " | Profesyonel çıktı için kapalı önerilir."; }));
        Tool.Items.Add(new ToolStripButton("Fit to page width", null, (_,__) => { ViewGrid.PrintFitToPageWidth = !ViewGrid.PrintFitToPageWidth; Info.Text = "PrintFitToPageWidth = " + ViewGrid.PrintFitToPageWidth; }));
        Tool.Items.Add(new ToolStripButton("Grid Aç/Kapat", null, (_,__) => { ViewGrid.PrintShowGrid = !ViewGrid.PrintShowGrid; Info.Text = "PrintShowGrid = " + ViewGrid.PrintShowGrid; }));
        Tool.Items.Add(new ToolStripButton("Zebra Aç/Kapat", null, (_,__) => { ViewGrid.PrintZebraRows = !ViewGrid.PrintZebraRows; Info.Text = "PrintZebraRows = " + ViewGrid.PrintZebraRows; }));
    }
}

public sealed class FilteringSampleForm : ViewGridSampleFormBase
{
    public FilteringSampleForm() : base("ViewGrid Filtreleme Detay Örneği", "Popup filtre, ayrı filtre penceresi, hızlı metin filtreleme ve tüm filtreleri temizleme örneği.")
    {
        ViewGrid.FilterMenuMode = ViewGridFilterMenuMode.Both;
        var search = new ToolStripTextBox { Width = 180 };
        search.TextChanged += (_,__) => ViewGrid.SetGlobalFilter(search.Text ?? "");
        Tool.Items.Add(new ToolStripLabel("Ara:"));
        Tool.Items.Add(search);
        Tool.Items.Add(new ToolStripButton("Ad popup filtre", null, (_,__) => ViewGrid.ShowHeaderContextMenuForAspect(nameof(DemoRow.Name))));
        Tool.Items.Add(new ToolStripButton("Popup stil", null, (_,__) => ViewGrid.FilterMenuMode = ViewGridFilterMenuMode.PopupMenu));
        Tool.Items.Add(new ToolStripButton("İkisi birlikte", null, (_,__) => ViewGrid.FilterMenuMode = ViewGridFilterMenuMode.Both));
        Tool.Items.Add(new ToolStripButton("Ad filtre penceresi", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.Name))));
        Tool.Items.Add(new ToolStripButton("Filtreleri temizle", null, (_,__) => { search.Text = ""; ViewGrid.ClearFilters(); }));
    }
}

public sealed class GroupingSampleForm : ViewGridSampleFormBase
{
    public GroupingSampleForm() : base("ViewGrid Gruplama Detay Örneği", "ViewGrid/GLV tarzı gruplama: başlığa tıkla aç/kapat, tüm grupları toplu daralt/aç.")
    {
        Tool.Items.Add(new ToolStripButton("Duruma göre grupla", null, (_,__) => ViewGrid.SetGroupBy(nameof(DemoRow.State))));
        Tool.Items.Add(new ToolStripButton("Mesleğe göre grupla", null, (_,__) => ViewGrid.SetGroupBy(nameof(DemoRow.Occupation))));
        Tool.Items.Add(new ToolStripButton("Tüm grupları daralt", null, (_,__) => ViewGrid.CollapseAllGroups()));
        Tool.Items.Add(new ToolStripButton("Tüm grupları aç", null, (_,__) => ViewGrid.ExpandAllGroups()));
        Tool.Items.Add(new ToolStripButton("Gruplamayı kaldır", null, (_,__) => ViewGrid.ClearGrouping()));
        Tool.Items.Add(new ToolStripButton("Grup başlığı rengi", null, (_,__) => { ViewGrid.CustomGroupBackColor = Color.FromArgb(235, 229, 250); ViewGrid.Invalidate(); }));
        ViewGrid.SetGroupBy(nameof(DemoRow.State));
    }
}

public sealed class ViewModesSampleForm : ViewGridSampleFormBase
{
    public ViewModesSampleForm() : base("ViewGrid Görünüm Modları Detay Örneği", "Kompakt, Standart, Geniş, Liste, Kart, Geniş Kart ve Detay görünümleri aynı veriyle denenebilir.")
    {
        AddMode("Kompakt", ViewGridMode.ExtraLargeIcons);
        AddMode("Standart", ViewGridMode.LargeIcons);
        AddMode("Geniş", ViewGridMode.MediumIcons);
        AddMode("Liste", ViewGridMode.List);
        AddMode("Kart", ViewGridMode.Tile);
        AddMode("Geniş Kart", ViewGridMode.LargeCard);
        AddMode("Detay", ViewGridMode.Details);
    }
    private void AddMode(string text, ViewGridMode mode) => Tool.Items.Add(new ToolStripButton(text, null, (_,__) => ViewGrid.SetViewMode(mode)));
}

public sealed class TileSampleForm : ViewGridSampleFormBase
{
    public TileSampleForm() : base("ViewGrid Kart Detay Örneği", "ViewGrid tile görünümüne benzer kart dizilim örneği. Kartlarda ad, meslek, tarih/durum gibi alanlar kompakt gösterilir.")
    {
        ViewGrid.TilePreferredHeight = 96;
        ViewGrid.TileMaxTextLines = 4;
        ViewGrid.SetViewMode(ViewGridMode.Tile);
        Tool.Items.Add(new ToolStripButton("Kart görünümü", null, (_,__) => ViewGrid.SetViewMode(ViewGridMode.Tile)));
        Tool.Items.Add(new ToolStripButton("Detay görünümü", null, (_,__) => ViewGrid.SetViewMode(ViewGridMode.Details)));
        Tool.Items.Add(new ToolStripButton("Mesleğe göre grupla", null, (_,__) => ViewGrid.SetGroupBy(nameof(DemoRow.Occupation))));
        Tool.Items.Add(new ToolStripButton("Kart yüksekliği +", null, (_,__) => { ViewGrid.TilePreferredHeight += 12; ViewGrid.SetViewMode(ViewGridMode.Tile); }));
        Tool.Items.Add(new ToolStripButton("Kart yüksekliği -", null, (_,__) => { ViewGrid.TilePreferredHeight = Math.Max(72, ViewGrid.TilePreferredHeight - 12); ViewGrid.SetViewMode(ViewGridMode.Tile); }));
        Tool.Items.Add(new ToolStripButton("Satır +", null, (_,__) => { ViewGrid.TileMaxTextLines = Math.Min(12, ViewGrid.TileMaxTextLines + 1); ViewGrid.Invalidate(); }));
        Tool.Items.Add(new ToolStripButton("Satır -", null, (_,__) => { ViewGrid.TileMaxTextLines = Math.Max(1, ViewGrid.TileMaxTextLines - 1); ViewGrid.Invalidate(); }));
    }
}

public sealed class PosterGallerySampleForm : ViewGridSampleFormBase
{
    private readonly Image[] _posters;
    private readonly ToolStripTextBox _search = new() { Width = 180 };
    private readonly ToolStripComboBox _genre = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ToolStripLabel _status = new("0 kayıt");
    private List<DemoRow> _rows = new();

    public PosterGallerySampleForm() : base("ViewGrid Poster / Resimli İçerik Örneği", "Plex/Netflix tarzı katalog: büyük görsel, başlık, yıl, tür, rating ve responsive poster kartları.")
    {
        _posters = CreatePosterImages();

        ViewGrid.Columns.Add(new ViewGridColumn("Poster", nameof(DemoRow.PosterIndex), 92)
        {
            Kind = ViewGridColumnKind.Image,
            ImageGetter = row => _posters[((DemoRow)row).PosterIndex % _posters.Length]
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Başlık", nameof(DemoRow.Name), 230) { FillFreeSpace = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Tür", nameof(DemoRow.Occupation), 115) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("Yıl", nameof(DemoRow.Year), 80));
        ViewGrid.Columns.Add(new ViewGridColumn("Rating", nameof(DemoRow.Rating), 100) { Kind = ViewGridColumnKind.Rating });
        ViewGrid.Columns.Add(new ViewGridColumn("İzleme", nameof(DemoRow.Progress), 110) { Kind = ViewGridColumnKind.ProgressBar });
        ViewGrid.SetViewMode(ViewGridMode.Poster);
        ViewGrid.TilePosterMode = true;
        ViewGrid.TilePreferredWidth = 214;
        ViewGrid.TilePreferredHeight = 292;
        ViewGrid.TilePosterImageHeight = 168;
        ViewGrid.EnableModernEmptyState = true;
        ViewGrid.EmptyListMessage = "Bu filtreye uygun poster bulunamadı";
        ViewGrid.AutoSizeTileWidthToContent = false;
        ViewGrid.MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;

        _rows = CreatePosterRows();
        ViewGrid.SetObjects(_rows);

        _genre.Items.Add("Tüm türler");
        foreach (var g in _rows.Select(x => x.Occupation).Distinct().OrderBy(x => x)) _genre.Items.Add(g);
        _genre.SelectedIndex = 0;

        _search.TextChanged += (_,__) => ApplyPosterFilter();
        _genre.SelectedIndexChanged += (_,__) => ApplyPosterFilter();

        Tool.Items.Add(new ToolStripLabel("Ara:"));
        Tool.Items.Add(_search);
        Tool.Items.Add(new ToolStripLabel("Tür:"));
        Tool.Items.Add(_genre);
        Tool.Items.Add(new ToolStripSeparator());
        Tool.Items.Add(new ToolStripButton("Poster", null, (_,__) => { ViewGrid.TilePosterMode = true; ViewGrid.SetViewMode(ViewGridMode.Poster); }));
        Tool.Items.Add(new ToolStripButton("MediaTile", null, (_,__) => { ViewGrid.TilePosterMode = true; ViewGrid.SetViewMode(ViewGridMode.MediaTile); }));
        Tool.Items.Add(new ToolStripButton("FilmStrip", null, (_,__) => { ViewGrid.TilePosterMode = true; ViewGrid.SetViewMode(ViewGridMode.FilmStrip); }));
        Tool.Items.Add(new ToolStripButton("Detay", null, (_,__) => { ViewGrid.TilePosterMode = false; ViewGrid.SetViewMode(ViewGridMode.Details); }));
        Tool.Items.Add(new ToolStripButton("Görsel +", null, (_,__) => { ViewGrid.TilePosterImageHeight += 12; ViewGrid.TilePreferredHeight += 12; ViewGrid.RefreshView(); }));
        Tool.Items.Add(new ToolStripButton("Görsel -", null, (_,__) => { ViewGrid.TilePosterImageHeight = Math.Max(90, ViewGrid.TilePosterImageHeight - 12); ViewGrid.TilePreferredHeight = Math.Max(190, ViewGrid.TilePreferredHeight - 12); ViewGrid.RefreshView(); }));
        Tool.Items.Add(new ToolStripButton("Yıla göre grupla", null, (_,__) => ViewGrid.SetGroupBy(nameof(DemoRow.Year))));
        Tool.Items.Add(new ToolStripButton("Grupları temizle", null, (_,__) => ViewGrid.ClearGrouping()));
        Tool.Items.Add(new ToolStripButton("Filtre menüsü", null, (_,__) => ViewGrid.ShowHeaderContextMenuForAspect(nameof(DemoRow.Name))));
        Tool.Items.Add(new ToolStripSeparator());
        Tool.Items.Add(_status);
        UpdatePosterStatus(_rows.Count);
    }

    private void ApplyPosterFilter()
    {
        string search = _search.Text?.Trim() ?? string.Empty;
        string genre = _genre.SelectedIndex > 0 ? _genre.Text : string.Empty;

        var filtered = _rows.Where(row =>
            (string.IsNullOrWhiteSpace(search) || row.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) || row.Occupation.Contains(search, StringComparison.CurrentCultureIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(genre) || string.Equals(row.Occupation, genre, StringComparison.CurrentCultureIgnoreCase))).ToList();

        ViewGrid.SetObjects(filtered);
        UpdatePosterStatus(filtered.Count);
    }

    private void UpdatePosterStatus(int count)
    {
        _status.Text = $"{count} / {_rows.Count} poster";
    }

    private static List<DemoRow> CreatePosterRows()
    {
        string[] titles = { "Ölümcül Zarafet", "Frankenstein", "A House of Dynamite", "The Lost Bus", "Neon Circuit", "Silent Orbit", "Green Valley", "Red Signal", "Last Protocol", "Deep Archive", "Night Runner", "Quantum Lake", "Bright Failure", "AOI Nights", "Signal Trace", "Board Runner" };
        string[] types = { "Drama", "Sci-Fi", "Thriller", "Action", "Mystery", "Adventure", "AOI", "Production" };
        return Enumerable.Range(0, titles.Length).Select(i => new DemoRow
        {
            Id = i + 1,
            Name = titles[i],
            Occupation = types[i % types.Length],
            State = i % 4 == 0 ? "New" : i % 3 == 0 ? "Watch" : "OK",
            Year = 2020 + (i % 7),
            PosterIndex = i,
            Progress = (i * 17 + 8) % 100,
            Rating = 2 + (i % 4)
        }).ToList();
    }

    private static Image[] CreatePosterImages()
    {
        Color[] colors =
        {
            Color.FromArgb(90, 24, 154), Color.FromArgb(20, 83, 45), Color.FromArgb(166, 20, 20),
            Color.FromArgb(134, 74, 19), Color.FromArgb(18, 74, 120), Color.FromArgb(85, 85, 95),
            Color.FromArgb(35, 112, 82), Color.FromArgb(180, 45, 58), Color.FromArgb(60, 42, 130),
            Color.FromArgb(32, 92, 110), Color.FromArgb(100, 52, 80), Color.FromArgb(86, 106, 34),
            Color.FromArgb(30, 95, 150), Color.FromArgb(130, 50, 110), Color.FromArgb(60, 120, 125), Color.FromArgb(150, 90, 35)
        };
        return colors.Select((c, i) => CreatePosterBitmap(c, i + 1)).ToArray();
    }

    private static Bitmap CreatePosterBitmap(Color baseColor, int number)
    {
        var bmp = new Bitmap(220, 320);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bg = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, bmp.Width, bmp.Height), ControlPaint.Light(baseColor), ControlPaint.DarkDark(baseColor), 55f);
        g.FillRectangle(bg, 0, 0, bmp.Width, bmp.Height);
        using var glow = new SolidBrush(Color.FromArgb(72, Color.White));
        g.FillEllipse(glow, -40 + number * 11 % 90, 22 + number * 17 % 120, 170, 170);
        using var accent = new Pen(Color.FromArgb(130, Color.White), 3f);
        g.DrawRectangle(accent, 10, 10, bmp.Width - 20, bmp.Height - 20);
        using var shade = new SolidBrush(Color.FromArgb(132, Color.Black));
        g.FillRectangle(shade, 0, 220, bmp.Width, 100);
        using var font = new Font("Segoe UI", 34, FontStyle.Bold);
        TextRenderer.DrawText(g, number.ToString("00"), font, new Rectangle(0, 100, bmp.Width, 70), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        using var small = new Font("Segoe UI", 10, FontStyle.Bold);
        TextRenderer.DrawText(g, "VIEWGRID POSTER", small, new Rectangle(0, 248, bmp.Width, 26), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, "responsive tile", SystemFonts.CaptionFont, new Rectangle(0, 274, bmp.Width, 22), Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bmp;
    }
}


public sealed class MediaLibrarySampleForm : ViewGridSampleFormBase
{
    private readonly Image[] _covers;
    private readonly List<MediaItem> _items;
    private readonly ToolStripComboBox _type = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ToolStripComboBox _scale = new() { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ToolStripTextBox _search = new() { Width = 180 };
    private readonly ToolStripLabel _status = new("0 kayıt");

    public MediaLibrarySampleForm() : base("ViewGrid Media Library Showcase", "Audix, film arşivi, fotoğraf galerisi, makine kataloğu ve AOI hata görseli gibi senaryolarda aynı ViewGrid verisini Poster / MediaTile / FilmStrip / DetailCard olarak gösterir.")
    {
        _covers = CreateMediaImages();
        _items = CreateMediaItems();

        ViewGrid.Columns.Add(new ViewGridColumn("Kapak", nameof(MediaItem.ImageIndex), 120)
        {
            Kind = ViewGridColumnKind.Image,
            ImageGetter = row => _covers[((MediaItem)row).ImageIndex % _covers.Length]
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Başlık", nameof(MediaItem.Title), 240) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 2 });
        ViewGrid.Columns.Add(new ViewGridColumn("Alt Bilgi", nameof(MediaItem.Subtitle), 210) { FillFreeSpace = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Tür", nameof(MediaItem.MediaType), 95) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("Albüm / Koleksiyon", nameof(MediaItem.Collection), 170));
        ViewGrid.Columns.Add(new ViewGridColumn("Süre", nameof(MediaItem.Duration), 70));
        ViewGrid.Columns.Add(new ViewGridColumn("Durum", nameof(MediaItem.State), 90) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("Açıklama", nameof(MediaItem.Description), 360) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 4 });

        ViewGrid.SetObjects(_items);
        ViewGrid.TilePosterMode = true;
        ViewGrid.TilePreferredWidth = 218;
        ViewGrid.TilePreferredHeight = 292;
        ViewGrid.TilePosterImageHeight = 166;
        ViewGrid.TileMaxTextLines = 5;
        ViewGrid.PosterPreferredWidth = 220;
        ViewGrid.PosterPreferredHeight = 306;
        ViewGrid.PosterImageHeight = 182;
        ViewGrid.MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
        ViewGrid.MediaImageRoundedCorners = true;
        ViewGrid.EnableModernEmptyState = true;
        ViewGrid.EmptyListMessage = "Bu filtreye uygun medya kaydı yok";
        ViewGrid.SetViewMode(ViewGridMode.Poster);

        _type.Items.Add("Tümü");
        foreach (var t in _items.Select(x => x.MediaType).Distinct().OrderBy(x => x)) _type.Items.Add(t);
        _type.SelectedIndex = 0;

        _scale.Items.Add(ViewGridMediaImageScaleMode.Cover);
        _scale.Items.Add(ViewGridMediaImageScaleMode.Contain);
        _scale.Items.Add(ViewGridMediaImageScaleMode.Stretch);
        _scale.SelectedItem = ViewGridMediaImageScaleMode.Cover;

        _search.TextChanged += (_, __) => ApplyFilter();
        _type.SelectedIndexChanged += (_, __) => ApplyFilter();
        _scale.SelectedIndexChanged += (_, __) =>
        {
            if (_scale.SelectedItem is ViewGridMediaImageScaleMode mode)
            {
                ViewGrid.MediaImageScaleMode = mode;
                ViewGrid.RefreshView();
            }
        };

        Tool.Items.Add(new ToolStripLabel("Ara:"));
        Tool.Items.Add(_search);
        Tool.Items.Add(new ToolStripLabel("Tip:"));
        Tool.Items.Add(_type);
        Tool.Items.Add(new ToolStripLabel("Görsel:"));
        Tool.Items.Add(_scale);
        Tool.Items.Add(new ToolStripSeparator());
        Tool.Items.Add(new ToolStripButton("Poster", null, (_, __) => ApplyPoster()));
        Tool.Items.Add(new ToolStripButton("Gallery", null, (_, __) => ApplyGallery()));
        Tool.Items.Add(new ToolStripButton("MediaTile", null, (_, __) => ApplyMediaTile()));
        Tool.Items.Add(new ToolStripButton("FilmStrip", null, (_, __) => ApplyFilmStrip()));
        Tool.Items.Add(new ToolStripButton("KPI", null, (_, __) => { ViewGrid.TilePosterMode = false; ViewGrid.TilePreferredWidth = 240; ViewGrid.TilePreferredHeight = 140; ViewGrid.SetViewMode(ViewGridMode.KpiDashboard); }));
        Tool.Items.Add(new ToolStripButton("HeatMap", null, (_, __) => { ViewGrid.TilePosterMode = false; ViewGrid.TilePreferredWidth = 190; ViewGrid.TilePreferredHeight = 126; ViewGrid.SetViewMode(ViewGridMode.HeatMap); }));
        Tool.Items.Add(new ToolStripButton("MiniChart", null, (_, __) => { ViewGrid.TilePosterMode = false; ViewGrid.TilePreferredWidth = 320; ViewGrid.TilePreferredHeight = 118; ViewGrid.SetViewMode(ViewGridMode.MiniChart); }));
        Tool.Items.Add(new ToolStripButton("Property", null, (_, __) => { ViewGrid.TilePosterMode = false; ViewGrid.SetViewMode(ViewGridMode.PropertyCard); }));
        Tool.Items.Add(new ToolStripButton("DetailCard", null, (_, __) => { ViewGrid.TilePosterMode = false; ViewGrid.SetViewMode(ViewGridMode.DetailCard); }));
        Tool.Items.Add(new ToolStripButton("Detay Liste", null, (_, __) => { ViewGrid.TilePosterMode = false; ViewGrid.SetViewMode(ViewGridMode.Details); }));
        Tool.Items.Add(new ToolStripButton("Koleksiyona göre grupla", null, (_, __) => ViewGrid.SetGroupBy(nameof(MediaItem.MediaType))));
        Tool.Items.Add(new ToolStripButton("Grupları temizle", null, (_, __) => ViewGrid.ClearGrouping()));
        Tool.Items.Add(new ToolStripSeparator());
        Tool.Items.Add(_status);
        UpdateStatus(_items.Count);
    }

    private void ApplyPoster()
    {
        ViewGrid.TilePosterMode = true;
        ViewGrid.PosterPreferredWidth = 220;
        ViewGrid.PosterPreferredHeight = 306;
        ViewGrid.PosterImageHeight = 182;
        ViewGrid.TileMaxTextLines = 5;
        ViewGrid.SetViewMode(ViewGridMode.Poster);
    }

    private void ApplyGallery()
    {
        ViewGrid.TilePosterMode = true;
        ViewGrid.TilePreferredWidth = 210;
        ViewGrid.TilePreferredHeight = 250;
        ViewGrid.TilePosterImageHeight = 148;
        ViewGrid.TileMaxTextLines = 5;
        ViewGrid.SetViewMode(ViewGridMode.Gallery);
    }

    private void ApplyMediaTile()
    {
        ViewGrid.TilePosterMode = true;
        ViewGrid.TilePreferredWidth = 190;
        ViewGrid.TilePreferredHeight = 236;
        ViewGrid.TilePosterImageHeight = 128;
        ViewGrid.TileMaxTextLines = 4;
        ViewGrid.SetViewMode(ViewGridMode.MediaTile);
    }

    private void ApplyFilmStrip()
    {
        ViewGrid.TilePosterMode = true;
        ViewGrid.TilePreferredWidth = 920;
        ViewGrid.TilePreferredHeight = 164;
        ViewGrid.TilePosterImageHeight = 116;
        ViewGrid.TileMaxTextLines = 7;
        ViewGrid.SetViewMode(ViewGridMode.FilmStrip);
    }

    private void ApplyFilter()
    {
        string search = _search.Text?.Trim() ?? string.Empty;
        string mediaType = _type.SelectedIndex > 0 ? _type.Text : string.Empty;
        var filtered = _items.Where(item =>
            (string.IsNullOrWhiteSpace(search)
             || item.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase)
             || item.Subtitle.Contains(search, StringComparison.CurrentCultureIgnoreCase)
             || item.Collection.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            && (string.IsNullOrWhiteSpace(mediaType) || string.Equals(item.MediaType, mediaType, StringComparison.CurrentCultureIgnoreCase))).ToList();
        ViewGrid.SetObjects(filtered);
        UpdateStatus(filtered.Count);
    }

    private void UpdateStatus(int count) => _status.Text = $"{count} / {_items.Count} medya";

    private static List<MediaItem> CreateMediaItems()
    {
        return new List<MediaItem>
        {
            new("Şımarık", "Tarkan", "Müzik", "Ölürüm Sana", "03:55", "OK", "Albüm kapağıyla Audix benzeri şarkı listesi görünümü.", 0),
            new("This Is Greece 1", "Anna Meliti", "Müzik", "Mediterranean Collection", "03:28", "Tam", "Şarkı adı, sanatçı, albüm ve süre kart üzerinde okunaklı durur.", 1),
            new("Neon Circuit", "Director's Cut", "Film", "Sci-Fi Posters", "01:52", "Yeni", "Film afişi / poster katalog görünümü.", 2),
            new("Silent Orbit", "Episode 04", "Film", "Space Archive", "42:10", "İzlendi", "Plex/Netflix benzeri büyük görsel kart senaryosu.", 3),
            new("AOI Error Frame", "LINE10 AOI", "AOI", "Board Visuals", "00:18", "Fail", "AOI hata karesi veya komponent görüntüsü kart üzerinde gösterilebilir.", 4),
            new("ASMPT SX1", "Placement Machine", "Makine", "Factory Navigator", "Aktif", "OK", "Makine fotoğrafı, hat, durum ve açıklama için görsel katalog.", 5),
            new("Student Portrait", "Bilge Defter", "Fotoğraf", "Yüzme 1", "Profil", "Aktif", "Öğrenci fotoğrafı + sınıf + durum gibi kayıtlar için kullanılabilir.", 6),
            new("Green Valley", "Travel Gallery", "Fotoğraf", "Summer 2026", "Foto", "Arşiv", "Fotoğraf galerisi veya doküman küçük önizleme kullanımı.", 7),
            new("Remote Session", "Technician Desk", "Ticket", "AOI Support", "12 dk", "Bekliyor", "Ticket eki, ekran görüntüsü veya mesaj görseli FilmStrip görünümünde rahat okunur.", 8),
            new("PCB Intelligence", "17MB140R5", "PCB", "CAD Compare", "Panel", "Review", "PCB görseli, varyant, refdes ve kontrol sonucu birlikte gösterilebilir.", 9)
        };
    }

    private static Image[] CreateMediaImages()
    {
        Color[] colors =
        {
            Color.FromArgb(32, 88, 180), Color.FromArgb(30, 145, 120), Color.FromArgb(110, 40, 170), Color.FromArgb(20, 42, 92), Color.FromArgb(180, 55, 48),
            Color.FromArgb(80, 96, 112), Color.FromArgb(190, 120, 40), Color.FromArgb(44, 140, 72), Color.FromArgb(130, 60, 120), Color.FromArgb(40, 120, 150)
        };
        string[] labels = { "MUSIC", "ALBUM", "MOVIE", "SCENE", "AOI", "MACHINE", "PHOTO", "GALLERY", "TICKET", "PCB" };
        return colors.Select((c, i) => CreateMediaBitmap(c, labels[i], i)).ToArray();
    }

    private static Bitmap CreateMediaBitmap(Color baseColor, string label, int seed)
    {
        var bmp = new Bitmap(360, 520);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bg = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, bmp.Width, bmp.Height), ControlPaint.Light(baseColor), ControlPaint.DarkDark(baseColor), 50f + seed * 6);
        g.FillRectangle(bg, 0, 0, bmp.Width, bmp.Height);
        using var glow1 = new SolidBrush(Color.FromArgb(70, Color.White));
        using var glow2 = new SolidBrush(Color.FromArgb(90, Color.Black));
        g.FillEllipse(glow1, -40 + seed * 23 % 160, 36 + seed * 31 % 190, 230, 230);
        g.FillEllipse(glow2, 170 - seed * 11 % 120, 250, 260, 260);
        using var line = new Pen(Color.FromArgb(130, Color.White), 4f);
        DrawRoundedMediaFrame(g, line, new Rectangle(18, 18, bmp.Width - 36, bmp.Height - 36), 24);
        using var band = new SolidBrush(Color.FromArgb(138, Color.Black));
        g.FillRectangle(band, 0, 352, bmp.Width, 168);
        using var big = new Font("Segoe UI", 36, FontStyle.Bold);
        TextRenderer.DrawText(g, label, big, new Rectangle(0, 155, bmp.Width, 72), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        using var small = new Font("Segoe UI", 11, FontStyle.Bold);
        TextRenderer.DrawText(g, "VIEWGRID MEDIA", small, new Rectangle(0, 392, bmp.Width, 30), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, "Poster • MediaTile • FilmStrip", SystemFonts.CaptionFont, new Rectangle(0, 424, bmp.Width, 30), Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bmp;
    }

    private static void DrawRoundedMediaFrame(Graphics g, Pen pen, Rectangle bounds, int radius)
    {
        int d = Math.Max(1, radius * 2);
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.DrawPath(pen, path);
    }

    private sealed record MediaItem(string Title, string Subtitle, string MediaType, string Collection, string Duration, string State, string Description, int ImageIndex);
}

public sealed class ViewGridV31AllPhasesSampleForm : ViewGridSampleFormBase
{
    private readonly Image[] _images;
    private readonly List<V31FeatureItem> _items;
    private readonly ToolStripTextBox _search = new() { Width = 190 };
    private readonly ToolStripComboBox _phase = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ToolStripComboBox _project = new() { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ToolStripLabel _status = new("0 özellik");

    public ViewGridV31AllPhasesSampleForm() : base("ViewGrid v31 Faz Merkezi", "Faz 31-37 tek ekranda: Audix medya deneyimi, smart view presetleri, gelişmiş grouping, master-detail, kanban, designer ve export akışı.")
    {
        _images = CreatePhaseImages();
        _items = CreateFeatureItems();

        ViewGrid.Columns.Clear();
        ViewGrid.Columns.Add(new ViewGridColumn("Görsel", nameof(V31FeatureItem.ImageIndex), 96)
        {
            Kind = ViewGridColumnKind.Image,
            ImageGetter = row => _images[((V31FeatureItem)row).ImageIndex % _images.Length]
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Özellik", nameof(V31FeatureItem.Title), 220) { Editable = true, FillFreeSpace = true, WordWrap = true, MaxTextLines = 2 });
        ViewGrid.Columns.Add(new ViewGridColumn("Faz", nameof(V31FeatureItem.Phase), 90) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("Proje", nameof(V31FeatureItem.Project), 120) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("ViewGrid Modu", nameof(V31FeatureItem.ViewMode), 140));
        ViewGrid.Columns.Add(new ViewGridColumn("Kullanım", nameof(V31FeatureItem.Usage), 360) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 4 });
        ViewGrid.Columns.Add(new ViewGridColumn("Rozet", nameof(V31FeatureItem.Badge), 85) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("Konum", nameof(V31FeatureItem.WhereToFind), 260) { WordWrap = true, MaxTextLines = 3 });

        ViewGrid.SetObjects(_items);
        ViewGrid.TilePosterMode = true;
        ViewGrid.TilePreferredWidth = 245;
        ViewGrid.TilePreferredHeight = 314;
        ViewGrid.TilePosterImageHeight = 150;
        ViewGrid.TileMaxTextLines = 6;
        ViewGrid.MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
        ViewGrid.MediaImageRoundedCorners = true;
        ViewGrid.MediaPlaceholderImage = _images[0];
        ViewGrid.ShowMediaOverlayButton = true;
        ViewGrid.MediaOverlayButtonText = "▶";
        ViewGrid.MediaQualityBadgeAspectName = nameof(V31FeatureItem.Badge);
        ViewGrid.MediaQualityBadgeGetter = row => ((V31FeatureItem)row).Badge;
        ViewGrid.EnableModernEmptyState = true;
        ViewGrid.EmptyListMessage = "Bu arama/filtre için özellik bulunamadı";
        ViewGrid.SetViewMode(ViewGridMode.Poster);

        _phase.Items.Add("Tüm Fazlar");
        foreach (var p in _items.Select(x => x.Phase).Distinct().OrderBy(x => x)) _phase.Items.Add(p);
        _phase.SelectedIndex = 0;

        _project.Items.Add("Tüm Projeler");
        foreach (var p in _items.Select(x => x.Project).Distinct().OrderBy(x => x)) _project.Items.Add(p);
        _project.SelectedIndex = 0;

        _search.TextChanged += (_, __) => ApplyFilter();
        _phase.SelectedIndexChanged += (_, __) => ApplyFilter();
        _project.SelectedIndexChanged += (_, __) => ApplyFilter();

        Tool.Items.Add(new ToolStripLabel("Ara:"));
        Tool.Items.Add(_search);
        Tool.Items.Add(new ToolStripLabel("Faz:"));
        Tool.Items.Add(_phase);
        Tool.Items.Add(new ToolStripLabel("Proje:"));
        Tool.Items.Add(_project);
        Tool.Items.Add(new ToolStripSeparator());
        Tool.Items.Add(new ToolStripButton("Audix Poster", null, (_, __) => ApplyMedia(ViewGridMode.Poster, 245, 314, 150)));
        Tool.Items.Add(new ToolStripButton("Gallery", null, (_, __) => ApplyMedia(ViewGridMode.Gallery, 218, 265, 132)));
        Tool.Items.Add(new ToolStripButton("MediaTile", null, (_, __) => ApplyMedia(ViewGridMode.MediaTile, 198, 238, 112)));
        Tool.Items.Add(new ToolStripButton("FilmStrip", null, (_, __) => ApplyMedia(ViewGridMode.FilmStrip, 900, 168, 110)));
        Tool.Items.Add(new ToolStripButton("Feature Finder", null, (_, __) => { ViewGrid.TilePosterMode = false; ViewGrid.SetViewMode(ViewGridMode.RowPreview); }));
        Tool.Items.Add(new ToolStripButton("Kanban Pro", null, (_, __) => { ViewGrid.TilePosterMode = false; ViewGrid.TilePreferredWidth = 330; ViewGrid.TilePreferredHeight = 188; ViewGrid.SetViewMode(ViewGridMode.Kanban); ViewGrid.SetGroupBy(nameof(V31FeatureItem.Project)); }));
        Tool.Items.Add(new ToolStripButton("MasterDetail", null, (_, __) => { ViewGrid.TilePosterMode = false; ViewGrid.SetViewMode(ViewGridMode.MasterDetail); }));
        Tool.Items.Add(new ToolStripButton("KPI", null, (_, __) => { ViewGrid.TilePosterMode = false; ViewGrid.TilePreferredWidth = 240; ViewGrid.TilePreferredHeight = 140; ViewGrid.SetViewMode(ViewGridMode.KpiDashboard); }));
        Tool.Items.Add(new ToolStripButton("Export/Print", null, (_, __) => { ViewGrid.TilePosterMode = false; ViewGrid.SetViewMode(ViewGridMode.DetailCard); }));
        Tool.Items.Add(new ToolStripButton("Grupları temizle", null, (_, __) => ViewGrid.ClearGrouping()));
        Tool.Items.Add(new ToolStripSeparator());
        Tool.Items.Add(_status);
        UpdateStatus(_items.Count);
    }

    private void ApplyMedia(ViewGridMode mode, int width, int height, int imageHeight)
    {
        ViewGrid.ClearGrouping();
        ViewGrid.TilePosterMode = true;
        ViewGrid.TilePreferredWidth = width;
        ViewGrid.TilePreferredHeight = height;
        ViewGrid.TilePosterImageHeight = imageHeight;
        ViewGrid.PosterPreferredWidth = width;
        ViewGrid.PosterPreferredHeight = height;
        ViewGrid.PosterImageHeight = imageHeight;
        ViewGrid.SetViewMode(mode);
    }

    private void ApplyFilter()
    {
        string text = _search.Text?.Trim() ?? string.Empty;
        string phase = _phase.SelectedIndex > 0 ? _phase.Text : string.Empty;
        string project = _project.SelectedIndex > 0 ? _project.Text : string.Empty;

        var filtered = _items.Where(item =>
            (string.IsNullOrWhiteSpace(text)
             || item.Title.Contains(text, StringComparison.CurrentCultureIgnoreCase)
             || item.Usage.Contains(text, StringComparison.CurrentCultureIgnoreCase)
             || item.WhereToFind.Contains(text, StringComparison.CurrentCultureIgnoreCase)
             || item.ViewMode.Contains(text, StringComparison.CurrentCultureIgnoreCase)
             || item.Project.Contains(text, StringComparison.CurrentCultureIgnoreCase))
            && (string.IsNullOrWhiteSpace(phase) || string.Equals(item.Phase, phase, StringComparison.CurrentCultureIgnoreCase))
            && (string.IsNullOrWhiteSpace(project) || string.Equals(item.Project, project, StringComparison.CurrentCultureIgnoreCase))).ToList();

        ViewGrid.SetObjects(filtered);
        UpdateStatus(filtered.Count);
    }

    private void UpdateStatus(int count) => _status.Text = $"{count} / {_items.Count} özellik";

    private static List<V31FeatureItem> CreateFeatureItems()
    {
        return new List<V31FeatureItem>
        {
            new("31", "Audix", "Albüm kapağı lazy-load + disk cache", "Poster / Gallery / MediaTile", "ImageGetter içinde dosyadan kapak oku, disk cache ile sakla, ViewGrid MediaPlaceholderImage ile eksik kapakları düzgün göster.", "Example Center > V31 Faz Merkezi / Media Library", "CACHE", 0),
            new("31", "Audix", "Kapak üstü play overlay", "Poster / FilmStrip", "ShowMediaOverlayButton=true yapıldığında seçili/hover medya kartında play aksiyonu görünür; Audix player kontrolüne bağlanabilir.", "Example Center > V31 Faz Merkezi > Audix Poster", "PLAY", 1),
            new("31", "Audix", "FLAC / MP3 / 320kbps rozeti", "Poster / Gallery", "MediaQualityBadgeAspectName veya MediaQualityBadgeGetter ile kalite rozeti kapak üzerinde gösterilir.", "ViewGrid - Media Experience property grubu", "FLAC", 2),
            new("31", "Genel", "Eksik görsel placeholder", "Poster / MediaTile", "MediaPlaceholderImage ile albüm kapağı, öğrenci fotoğrafı, makine görseli veya AOI karesi yoksa boş alan yerine kurumsal placeholder gösterilir.", "ViewGrid - Media Experience > MediaPlaceholderImage", "SAFE", 3),

            new("32", "Genel", "Smart View Preset", "Details / Poster / Kanban", "Kullanıcının kolon, filtre, sıralama, grup ve görünüm modunu kaydedip tekrar yüklemesi için layout profile akışıyla birlikte kullanılır.", "Example Center > Layout Profiles Demo", "PRESET", 4),
            new("32", "MasterData", "Son kullanılan görünüm", "Details / RowPreview", "Makine, BOM, program listelerinde kullanıcı son kullandığı grid görünümüne geri dönebilir.", "ProfileV29 + UserLayoutProfile örnekleri", "UX", 5),
            new("32", "Genel", "Layout export/import", "Details", "ViewGrid layout profilini dışarı aktarma ve başka kullanıcıya/PC’ye taşıma senaryosu.", "Example Center > Layout Profiles Demo", "JSON", 6),

            new("33", "Factory", "Gelişmiş grup başlıkları", "GroupCard / GroupedList", "Hat, makine tipi, durum veya medya türüne göre gruplu görünüm; sayaç ve renkli başlıklarla okunabilirlik artar.", "Example Center > Gruplama / V31 Faz Merkezi", "GROUP", 7),
            new("33", "Audix", "Sanatçı / Albüm gruplama", "GroupCard", "Audix listesinde sanatçı veya albüme göre kart gruplama; kapaklı listede arşiv daha düzenli görünür.", "V31 Faz Merkezi > Koleksiyon/Gruplama", "ALBUM", 8),

            new("34", "AOI", "Master Detail satır detayı", "MasterDetail / DetailCard", "Ticket veya AOI kayıtlarında satır açıldığında mesaj geçmişi, ek görsel veya alt tablo gösterme akışı.", "Example Center > Master Detail", "DETAIL", 9),
            new("34", "MasterData", "Ürün ağacı alt detay", "MasterDetail", "Sipariş satırı altında BOM, CAD, program ve makine uyum detayı gösterilebilir.", "Example Center > Master Detail", "BOM", 10),

            new("35", "AOI", "Kanban Pro ticket akışı", "Kanban", "Yeni / Bekliyor / Bakılıyor / Tamamlandı kolon mantığı için kart görünümü; SLA ve teknisyen rozetiyle desteklenebilir.", "Example Center > V31 Faz Merkezi > Kanban Pro", "SLA", 11),
            new("35", "LineWorkspace", "Duruma göre kart taşıma hazırlığı", "Kanban", "Sürükle-bırak iş akışına hazır görünüm; şimdilik grup + kart mantığıyla pratik takip sağlar.", "ViewGridMode.Kanban", "FLOW", 12),

            new("36", "Genel", "Designer friendly property grupları", "Tüm modlar", "Medya ayarları ViewGrid - Media Experience kategorisinde görünür; WinForms Designer’da ayarlanabilir property yaklaşımı korundu.", "PropertyGrid > ViewGrid - Media Experience", "DESIGN", 13),
            new("36", "Genel", "Example Center hızlı erişim", "SampleHub", "Hangi özellik nerede sorunu için en üstte hızlı erişim bölümü, canlı arama ve faz merkezi eklendi.", "Example Center > Hızlı Erişim", "FIND", 14),

            new("37", "Genel", "Card/Poster print-export akışı", "DetailCard / Poster", "PDF/Excel/PNG export senaryoları için görsel görünümden rapor üretme akışı Example Center’da ayrıştırıldı.", "Example Center > Yazdırma / PDF Export", "PDF", 15),
            new("37", "Audix", "Kapaklı albüm katalog çıktısı", "Gallery / Poster", "Audix’te seçili albüm listesini kapaklı katalog gibi yazdırma veya PDF’e aktarma için kullanılabilir.", "V31 Faz Merkezi > Export/Print", "PRINT", 16)
        };
    }

    private static Image[] CreatePhaseImages()
    {
        Color[] colors =
        {
            Color.FromArgb(37, 105, 180), Color.FromArgb(32, 150, 125), Color.FromArgb(110, 65, 185), Color.FromArgb(190, 95, 42),
            Color.FromArgb(185, 54, 72), Color.FromArgb(70, 110, 135), Color.FromArgb(55, 140, 90), Color.FromArgb(130, 84, 160),
            Color.FromArgb(28, 78, 150), Color.FromArgb(36, 120, 160), Color.FromArgb(165, 130, 48), Color.FromArgb(125, 68, 130),
            Color.FromArgb(80, 92, 115), Color.FromArgb(35, 135, 98), Color.FromArgb(95, 120, 180), Color.FromArgb(180, 88, 40),
            Color.FromArgb(90, 90, 105)
        };
        string[] labels = { "MEDIA", "PLAY", "FLAC", "IMG", "PRESET", "VIEW", "JSON", "GROUP", "ALBUM", "DETAIL", "BOM", "KANBAN", "FLOW", "DESIGN", "FIND", "PDF", "PRINT" };
        return colors.Select((c, i) => CreatePhaseBitmap(c, labels[i], i)).ToArray();
    }

    private static Bitmap CreatePhaseBitmap(Color baseColor, string label, int seed)
    {
        var bmp = new Bitmap(360, 520);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bg = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, bmp.Width, bmp.Height), ControlPaint.Light(baseColor), ControlPaint.DarkDark(baseColor), 38f + seed * 5);
        g.FillRectangle(bg, 0, 0, bmp.Width, bmp.Height);
        using var glow = new SolidBrush(Color.FromArgb(62, Color.White));
        g.FillEllipse(glow, -30 + (seed * 19) % 150, 38 + (seed * 29) % 150, 230, 230);
        using var shade = new SolidBrush(Color.FromArgb(128, Color.Black));
        g.FillRectangle(shade, 0, 350, bmp.Width, 170);
        using var pen = new Pen(Color.FromArgb(125, Color.White), 4f);
        DrawRoundedPhaseFrame(g, pen, new Rectangle(18, 18, bmp.Width - 36, bmp.Height - 36), 24);
        using var big = new Font("Segoe UI", 33, FontStyle.Bold);
        TextRenderer.DrawText(g, label, big, new Rectangle(0, 155, bmp.Width, 74), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        using var small = new Font("Segoe UI", 11, FontStyle.Bold);
        TextRenderer.DrawText(g, "VIEWGRID V31", small, new Rectangle(0, 392, bmp.Width, 28), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, "Media • Smart • Export", SystemFonts.CaptionFont, new Rectangle(0, 424, bmp.Width, 28), Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bmp;
    }

    private static void DrawRoundedPhaseFrame(Graphics g, Pen pen, Rectangle bounds, int radius)
    {
        int d = Math.Max(1, radius * 2);
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.DrawPath(pen, path);
    }

    private sealed record V31FeatureItem(string Phase, string Project, string Title, string ViewMode, string Usage, string WhereToFind, string Badge, int ImageIndex);
}

public sealed class HighlightSearchSampleForm : ViewGridSampleFormBase
{
    private readonly ToolStripTextBox _search = new() { Width = 180 };
    public HighlightSearchSampleForm() : base("ViewGrid Highlight Search Detay Örneği", "Arama metni satır içinde sarı highlight ile gösterilir, sonraki/önceki eşleşmeye atlanabilir.")
    {
        _search.Text = "kayıt 25";
        _search.TextChanged += (_,__) => { ViewGrid.SetGlobalFilter(_search.Text ?? ""); ViewGrid.JumpToFirstMatch(_search.Text ?? ""); };
        Tool.Items.Add(new ToolStripLabel("Highlight ara:"));
        Tool.Items.Add(_search);
        Tool.Items.Add(new ToolStripButton("Sonraki", null, (_,__) => ViewGrid.FindNext(_search.Text ?? "")));
        Tool.Items.Add(new ToolStripButton("Önceki", null, (_,__) => ViewGrid.FindPrevious(_search.Text ?? "")));
        Tool.Items.Add(new ToolStripButton("Temizle", null, (_,__) => { _search.Text = ""; ViewGrid.ClearFilters(); }));
        Shown += (_,__) => { ViewGrid.SetGlobalFilter(_search.Text ?? ""); ViewGrid.JumpToFirstMatch(_search.Text ?? ""); };
    }
}

public sealed class ColoringSampleForm : ViewGridSampleFormBase
{
    private readonly ToolStripComboBox _themeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly ToolStripComboBox _rowColorBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };

    public ColoringSampleForm() : base("ViewGrid Satır Renklendirme Detay Örneği", "Satır renkleri seçilen temaya göre otomatik uyarlanır. Fail/Review/OK gibi durumlar açık ve koyu temada okunabilir kalır.")
    {
        foreach (ViewGridThemePreset preset in Enum.GetValues(typeof(ViewGridThemePreset)))
            _themeBox.Items.Add(preset);
        foreach (ViewGridRowColorPreset preset in Enum.GetValues(typeof(ViewGridRowColorPreset)))
            _rowColorBox.Items.Add(preset);

        _themeBox.SelectedItem = ViewGridThemePreset.MidnightPurple;
        _rowColorBox.SelectedItem = ViewGridRowColorPreset.AOIRisk;

        _themeBox.SelectedIndexChanged += (_,__) => ApplyPreview();
        _rowColorBox.SelectedIndexChanged += (_,__) => ApplyPreview();

        Tool.Items.Add(new ToolStripLabel("Tema:"));
        Tool.Items.Add(_themeBox);
        Tool.Items.Add(new ToolStripLabel("Satır:"));
        Tool.Items.Add(_rowColorBox);
        Tool.Items.Add(new ToolStripButton("Fail yazısını vurgula", null, (_,__) =>
        {
            ViewGrid.RowForeColorGetter = row => ((DemoRow)row).State == "Fail" ? (ViewGridTheme.FromPreset((ViewGridThemePreset)_themeBox.SelectedItem!).IsDark ? Color.FromArgb(255, 190, 190) : Color.FromArgb(140, 0, 0)) : null;
            ViewGrid.Invalidate();
        }));
        Tool.Items.Add(new ToolStripButton("Koşullu format ekle", null, (_,__) =>
        {
            ViewGrid.ConditionalFormats.Clear();
            ViewGrid.ConditionalFormats.Add(new ViewGridConditionalFormat { Column = ViewGrid.Columns[nameof(DemoRow.State)], BackColor = Color.FromArgb(80, 220, 60, 60), ForeColor = Color.DarkRed, Predicate = (_,_,v) => Convert.ToString(v) == "Fail" });
            ViewGrid.Invalidate();
        }));
        Tool.Items.Add(new ToolStripButton("Sıfırla", null, (_,__) =>
        {
            ViewGrid.RowBackColorGetter = null;
            ViewGrid.RowForeColorGetter = null;
            ViewGrid.ConditionalFormats.Clear();
            _rowColorBox.SelectedItem = ViewGridRowColorPreset.ThemeDefault;
            ApplyPreview();
        }));

        Shown += (_,__) => ApplyPreview();
    }

    private void ApplyPreview()
    {
        var themePreset = _themeBox.SelectedItem is ViewGridThemePreset tp ? tp : ViewGridThemePreset.System;
        var rowPreset = _rowColorBox.SelectedItem is ViewGridRowColorPreset rp ? rp : ViewGridRowColorPreset.ThemeDefault;
        ViewGrid.ThemePreset = themePreset;
        ViewGrid.RowColorPreset = rowPreset;
        ViewGrid.RowColorAspectName = nameof(DemoRow.State);
        ViewGrid.RowColorStrength = rowPreset == ViewGridRowColorPreset.AOIRisk || rowPreset == ViewGridRowColorPreset.SeverityBands ? 0.23 : 0.16;
        ViewGrid.RowBackColorGetter = null;
        ViewGrid.RowForeColorGetter = null;
        ViewGrid.Invalidate();
        Info.Text = $"Tema: {themePreset} | Satır renkleri: {rowPreset} | Aspect: {ViewGrid.RowColorAspectName}";
    }
}

public sealed class ImageComboSampleForm : ViewGridSampleFormBase
{
    private static readonly string[] StatusValues = { "OK", "Review", "Fail" };

    public ImageComboSampleForm() : base("ViewGrid Image + Combo Hücre Örneği", "İkon/görsel kolon + ikonlu ComboBox hücre düzenleme. Combo hücreye tek tıklayınca liste direkt açılır; F2 de desteklenir.")
    {
        ViewGrid.Columns.Clear();
        ViewGrid.RowHeight = 34;
        ViewGrid.CellEditActivationKey = Keys.F2;
        ViewGrid.AllowEditAllCells = true;
        ViewGrid.EnableCellEditing = true;

        ViewGrid.Columns.Add(new ViewGridColumn("Durum", nameof(DemoRow.State), 76)
        {
            Kind = ViewGridColumnKind.Icon,
            ImageGetter = row => CreateStatusIcon(((DemoRow)row).State),
            AspectGetter = row => ((DemoRow)row).State
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Ad", nameof(DemoRow.Name), 240) { Editable = true, FillFreeSpace = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Kategori", nameof(DemoRow.Occupation), 170)
        {
            Kind = ViewGridColumnKind.ComboBox,
            Editable = true,
            ComboBoxItems = DemoData.Occupations.ToList(),
            ComboBoxImageGetter = CreateOccupationIcon,
            Editor = new ViewGrid.Editing.ComboBoxCellEditor(DemoData.Occupations)
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Karar", nameof(DemoRow.State), 130)
        {
            Kind = ViewGridColumnKind.ComboBox,
            Editable = true,
            ComboBoxItems = StatusValues.ToList(),
            ComboBoxImageGetter = CreateStatusIcon,
            Editor = new ViewGrid.Editing.ComboBoxCellEditor(StatusValues)
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Puan", nameof(DemoRow.Rating), 110) { Kind = ViewGridColumnKind.Rating, MaxRating = 5 });
        ViewGrid.Columns.Add(new ViewGridColumn("İlerleme", nameof(DemoRow.Progress), 140) { Kind = ViewGridColumnKind.ProgressBar });

        Tool.Items.Add(new ToolStripButton("Karar filtresi", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.State))));
        Tool.Items.Add(new ToolStripButton("Kategori filtresi", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.Occupation))));
        Tool.Items.Add(new ToolStripButton("Yeni satır seti", null, (_,__) => LoadImageComboRows(120)));
        LoadImageComboRows(120);
    }

    private void LoadImageComboRows(int count)
    {
        var rows = CreateRows(count);
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].Name = "Görsel/Combo kayıt " + (i + 1);
            rows[i].State = i % 9 == 0 ? "Fail" : i % 4 == 0 ? "Review" : "OK";
        }
        ViewGrid.SetObjects(rows);
    }

    private Image CreateOccupationIcon(string value)
    {
        var bmp = new Bitmap(24, 24);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int hash = (value ?? string.Empty).GetHashCode() & 0x7fffffff;
        Color c = Color.FromArgb(255, 80 + hash % 120, 90 + (hash / 7) % 120, 110 + (hash / 13) % 110);
        using var fill = new SolidBrush(c);
        using var pen = new Pen(Color.FromArgb(230, Color.White), 1.1f);
        using (var path = CreateRoundRectPath(new Rectangle(3, 3, 18, 18), 5))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }
        string text = string.IsNullOrWhiteSpace(value) ? "?" : value.Trim()[0].ToString().ToUpperInvariant();
        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, font, brush, new RectangleF(3, 3, 18, 18), sf);
        return bmp;
    }


    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundRectPath(Rectangle r, int radius)
    {
        int d = Math.Max(1, radius * 2);
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private Image CreateStatusIcon(string state)
    {
        var bmp = new Bitmap(24, 24);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Color c = state == "Fail" ? Color.FromArgb(230, 70, 80) : state == "Review" ? Color.FromArgb(245, 170, 50) : Color.FromArgb(48, 170, 96);
        using var glow = new SolidBrush(Color.FromArgb(55, c));
        using var fill = new SolidBrush(c);
        using var pen = new Pen(Color.FromArgb(230, Color.White), 1.1f);
        g.FillEllipse(glow, 1, 1, 22, 22);
        g.FillEllipse(fill, 4, 4, 16, 16);
        g.DrawEllipse(pen, 4, 4, 16, 16);
        string text = state == "Fail" ? "!" : state == "Review" ? "?" : "✓";
        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, font, brush, new RectangleF(4, 3, 16, 17), sf);
        return bmp;
    }
}

public sealed class ExportSampleForm : ViewGridSampleFormBase
{
    public ExportSampleForm() : base("ViewGrid Export Detay Örneği", "Görünen satırları CSV veya Excel .xlsx olarak dışarı aktarır.")
    {
        Tool.Items.Add(new ToolStripButton("CSV Export", null, (_,__) => ExportCsv()));
        Tool.Items.Add(new ToolStripButton("Excel Export", null, (_,__) => ExportExcel()));
    }
    private void ExportCsv()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ViewGrid.Sample.csv");
        try
        {
            var actualPath = ViewGrid.ExportVisibleCsv(path);
            MessageBox.Show(this, actualPath, "CSV Export");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "CSV dışa aktarılırken hata oluştu:\n" + ex.Message, "CSV Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    private void ExportExcel()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ViewGrid.Sample.xlsx");
        try
        {
            var actualPath = ViewGrid.ExportVisibleExcel(path, "ViewGridControlSample");
            MessageBox.Show(this, actualPath, "Excel Export");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Excel dışa aktarılırken hata oluştu:\n" + ex.Message, "Excel Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

public sealed class MillionRowsSampleForm : ViewGridSampleFormBase
{
    public MillionRowsSampleForm() : base("ViewGrid 1M Sanal Liste Detay Örneği", "999.000.000 satır virtual/query provider ile simüle edilir; büyük veri RAM'e alınmaz, filtre/sort provider tarafında çalışır.")
    {
        Tool.Items.Add(new ToolStripButton("999M Virtual yükle", null, (_,__) => ViewGrid.SetVirtualProvider(new VirtualDemoRowProvider(999_000_000))));
        Tool.Items.Add(new ToolStripButton("1.500.000 normal/async yükle", null, async (_,__) => await ViewGrid.SetObjectsAsync<DemoRow>(async ct => { await Task.Delay(25, ct); return CreateRows(1_500_000); })));
        Tool.Items.Add(new ToolStripButton("Ad filtre popup", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.Name))));
        Tool.Items.Add(new ToolStripButton("Hızlı filtre modu", null, (_,__) => { ViewGrid.FastFilterMenuForHugeLists = true; MainForm.ConfigureMillionRowFiltering(ViewGrid); Info.Text = "999M filtre menüsü hızlı açılır; yazılan metin provider tarafında tüm sanal veri üzerinde filtrelenir."; }));
        Tool.Items.Add(new ToolStripButton("Tam tarama modu", null, (_,__) => { ViewGrid.FastFilterMenuForHugeLists = false; Info.Text = "Dev veride tüm distinct değerleri listelemek yerine arama/provider filtresi kullanılmalıdır."; }));
        Shown += (_,__) => ViewGrid.SetVirtualProvider(new VirtualDemoRowProvider(999_000_000));
    }
}

public sealed class DesignerApiSampleForm : ViewGridSampleFormBase
{
    public DesignerApiSampleForm() : base("ViewGrid Designer / API Detay Örneği", "DLL projeye eklendiğinde designer tarafında değiştirilebilen özelliklerin ve kodla ayarlanabilen API'lerin kısa vitrini.")
    {
        Tool.Items.Add(new ToolStripButton("Designer özelliklerini göster", null, (_,__) => MessageBox.Show(this,
            "Örnek designer/API özellikleri:\n" +
            "• ViewMode / FilterMenuMode\n" +
            "• ShowFilterMenu / ShowGridLines / EnableGrouping\n" +
            "• HeaderHeight / RowHeight\n" +
            "• HighlightBackColor / HighlightForeColor\n" +
            "• CustomGroupBackColor\n" +
            "• RowBackColorGetter / ConditionalFormats kod tarafı\n" +
            "• Columns koleksiyonu: CheckBox, Badge, ProgressBar, Rating, Button", "ViewGrid Designer/API")));
        Tool.Items.Add(new ToolStripButton("Header filtre menüsü", null, (_,__) => ViewGrid.ShowHeaderContextMenuForAspect(nameof(DemoRow.Name))));
        Tool.Items.Add(new ToolStripButton("Kolon seçici", null, (_,__) => ViewGrid.ShowColumnChooser()));
        Tool.Items.Add(new ToolStripButton("Layout kaydet", null, (_,__) => ViewGrid.SaveLayout(Path.Combine(Application.StartupPath, "sample-layout.json"))));
        Tool.Items.Add(new ToolStripButton("Layout yükle", null, (_,__) => ViewGrid.LoadLayout(Path.Combine(Application.StartupPath, "sample-layout.json"))));
    }
}



public sealed class ColumnManagerSampleForm : ViewGridSampleFormBase
{
    private readonly string _layoutPath = Path.Combine(Application.StartupPath, "column-manager-sample-layout.json");

    public ColumnManagerSampleForm() : base("ViewGrid Column Manager Detay Örneği", "Kolon başlığını tutup sağa/sola sürükleyerek yer değiştir. Genişlik değişiklikleri ve kolon görünürlüğü layout olarak kaydedilebilir.")
    {
        ViewGrid.Name = "ColumnManagerSampleViewGrid";
        ViewGrid.AllowColumnReorder = true;
        ViewGrid.ShowColumnReorderPreview = true;
        ViewGrid.AutoSaveColumnLayout = false;
        ViewGrid.ColumnLayoutStorageKey = "ColumnManagerSample";

        Tool.Items.Add(new ToolStripButton("Sürükle-bırak aktif", null, (_,__) => { ViewGrid.AllowColumnReorder = true; Info.Text = "Kolon başlığını tutup sağa/sola sürükle."; }));
        Tool.Items.Add(new ToolStripButton("Otomatik kaydet Aç/Kapat", null, (_,__) => { ViewGrid.AutoSaveColumnLayout = !ViewGrid.AutoSaveColumnLayout; Info.Text = "AutoSaveColumnLayout = " + ViewGrid.AutoSaveColumnLayout; }));
        Tool.Items.Add(new ToolStripButton("Layout kaydet", null, (_,__) => { ViewGrid.SaveLayout(_layoutPath); Info.Text = "Kaydedildi: " + _layoutPath; }));
        Tool.Items.Add(new ToolStripButton("Layout yükle", null, (_,__) => { ViewGrid.LoadLayout(_layoutPath); Info.Text = "Yüklendi: " + _layoutPath; }));
        Tool.Items.Add(new ToolStripButton("Varsayılana dön", null, (_,__) => { ViewGrid.ResetColumnLayout(); Info.Text = "Kolon düzeni varsayılana döndü."; }));
        Tool.Items.Add(new ToolStripButton("Kolon seçici", null, (_,__) => ViewGrid.ShowColumnChooser()));
        Tool.Items.Add(new ToolStripButton("İçeriğe sığdır", null, (_,__) => ViewGrid.AutoResizeColumnsToContent()));

        Info.Text += "  |  Sağ tık menüsünde de Kolon düzenini kaydet/yükle/sıfırla seçenekleri var.";
    }
}


public sealed class LocalizationSampleForm : ViewGridSampleFormBase
{
    private readonly ToolStripComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };

    public LocalizationSampleForm() : base("ViewGrid Dil / Localization Test Formu", "Menü, filtre popup, kolon seçimi ve boş liste metinlerinin Windows dili veya seçilen dile göre değişmesini test eder.")
    {
        foreach (ViewGridLanguage lang in Enum.GetValues(typeof(ViewGridLanguage)))
            _language.Items.Add(lang);
        _language.SelectedItem = ViewGridLanguage.Auto;
        _language.SelectedIndexChanged += (_,__) => ApplySelectedLanguage();

        Tool.Items.Add(new ToolStripLabel("Dil:"));
        Tool.Items.Add(_language);
        Tool.Items.Add(new ToolStripButton("Header filtre", null, (_,__) => ViewGrid.ShowHeaderContextMenuForAspect(nameof(DemoRow.Name))));
        Tool.Items.Add(new ToolStripButton("Filtre penceresi", null, (_,__) => ViewGrid.ShowFilterMenuForAspect(nameof(DemoRow.Name))));
        Tool.Items.Add(new ToolStripButton("Kolon seçici", null, (_,__) => ViewGrid.ShowColumnChooser()));
        Tool.Items.Add(new ToolStripButton("Boş liste metni", null, (_,__) => ViewGrid.SetObjects(Array.Empty<DemoRow>())));
        Tool.Items.Add(new ToolStripButton("Veriyi geri yükle", null, (_,__) => ViewGrid.SetObjects(CreateRows(250))));

        ViewGrid.Language = ViewGridLanguage.Auto;
        ViewGrid.FilterMenuMode = ViewGridFilterMenuMode.Both;
        Info.Text += "  |  Auto seçeneği CurrentUICulture ile Windows dilini algılar, eksik metinde English fallback kullanır.";
    }

    private void ApplySelectedLanguage()
    {
        if (_language.SelectedItem is not ViewGridLanguage lang) return;
        ViewGrid.Language = lang;
        ViewGrid.EmptyListMessage = ViewGridText.EmptyList;
        Info.Text = $"Aktif dil: {lang} / Effective: {ViewGridText.EffectiveLanguage}. Sağ tık menüsünü veya filtre popup'ını açarak metinleri kontrol et.";
        ViewGrid.Invalidate();
    }
}

public sealed class GLVFeaturePackSampleForm : ViewGridSampleFormBase
{
    public GLVFeaturePackSampleForm() : base("ViewGrid GLV Feature Pack", "GLV'de sık kullanılan liste özelliklerinin ViewGrid karşılıkları: hyperlink, toggle, tags, sparkline, color swatch, seçim/kolon/layout işlemleri.")
    {
        ViewGrid.Columns.Clear();
        ViewGrid.Columns.Add(new ViewGridColumn("✓", nameof(DemoRow.Checked), 42) { Kind = ViewGridColumnKind.CheckBox, AspectPutter = (row, value) => ((DemoRow)row).SetChecked(Convert.ToBoolean(value)) });
        ViewGrid.Columns.Add(new ViewGridColumn("Toggle", nameof(DemoRow.NeedsReview), 76) { Kind = ViewGridColumnKind.ToggleSwitch, AspectPutter = (row, value) => ((DemoRow)row).SetNeedsReview(Convert.ToBoolean(value)) });
        ViewGrid.Columns.Add(new ViewGridColumn("Id", nameof(DemoRow.Id), 70));
        ViewGrid.Columns.Add(new ViewGridColumn("Ad", nameof(DemoRow.Name), 210) { Editable = true, FillFreeSpace = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Link", nameof(DemoRow.Link), 90) { Kind = ViewGridColumnKind.Hyperlink });
        ViewGrid.Columns.Add(new ViewGridColumn("Etiketler", nameof(DemoRow.Tags), 180) { Kind = ViewGridColumnKind.Tags });
        ViewGrid.Columns.Add(new ViewGridColumn("Mini grafik", nameof(DemoRow.Spark), 130) { Kind = ViewGridColumnKind.Sparkline });
        ViewGrid.Columns.Add(new ViewGridColumn("Renk", nameof(DemoRow.RowColor), 105) { Kind = ViewGridColumnKind.ColorSwatch });
        ViewGrid.Columns.Add(new ViewGridColumn("Durum", nameof(DemoRow.State), 110) { Kind = ViewGridColumnKind.Badge });
        ViewGrid.Columns.Add(new ViewGridColumn("İlerleme", nameof(DemoRow.Progress), 140) { Kind = ViewGridColumnKind.ProgressBar });
        ViewGrid.Columns.Add(new ViewGridColumn("Puan", nameof(DemoRow.Rating), 110) { Kind = ViewGridColumnKind.Rating });
        ViewGrid.SetObjects(CreateRows(500));
        ViewGrid.HyperlinkClick += (_, e) => MessageBox.Show(this, $"Hyperlink tıklandı: {((DemoRow)e.RowObject).Name}", "ViewGrid Hyperlink");
        ViewGrid.ItemActivate += (_, e) => Info.Text = $"Aktif satır: {((DemoRow)e.RowObject).Name}";

        Tool.Items.Add(new ToolStripButton("Tümünü seç", null, (_,__) => ViewGrid.SelectAllRows()));
        Tool.Items.Add(new ToolStripButton("Seçimi temizle", null, (_,__) => ViewGrid.ClearSelection()));
        Tool.Items.Add(new ToolStripButton("Seçimi ters çevir", null, (_,__) => ViewGrid.InvertSelection()));
        Tool.Items.Add(new ToolStripButton("Kolonları içeriğe göre ayarla", null, (_,__) => ViewGrid.AutoResizeColumnsToContent()));
        Tool.Items.Add(new ToolStripButton("Üste git", null, (_,__) => ViewGrid.ScrollToTop()));
        Tool.Items.Add(new ToolStripButton("Alta git", null, (_,__) => ViewGrid.ScrollToBottom()));
        Tool.Items.Add(new ToolStripButton("Layout kaydet", null, (_,__) => ViewGrid.SaveLayout(Path.Combine(Application.StartupPath, "olv-feature-pack-layout.json"))));
        Tool.Items.Add(new ToolStripButton("Layout yükle", null, (_,__) => ViewGrid.LoadLayout(Path.Combine(Application.StartupPath, "olv-feature-pack-layout.json"))));
        Tool.Items.Add(new ToolStripButton("Kart görünümü", null, (_,__) => ViewGrid.SetViewMode(ViewGridMode.Tile)));
        Tool.Items.Add(new ToolStripButton("Detay", null, (_,__) => ViewGrid.SetViewMode(ViewGridMode.Details)));
    }
}



public sealed class DatabaseSampleForm : Form
{
    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        FullRowSelect = true,
        MultiSelect = true,
        ShowGridLines = true,
        EmptyListMessage = "Database örneği için kayıt yok",
        EnableModernEmptyState = true,
        EnableClipboard = true,
        EnableHighlightEngine = true,
        FastFilterMenuForHugeLists = true,
        AsyncLoadFullFilterValues = true
    };

    private readonly TextBox _search = new() { Dock = DockStyle.Top, PlaceholderText = "Barkod, makine, assembly veya sonuç içinde ara..." };
    private readonly Label _info = new() { Dock = DockStyle.Top, Height = 42, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 12, 0) };

    public DatabaseSampleForm()
    {
        Text = "ViewGrid Database Örnekleri";
        Width = 1180;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Program.AppTheme.BackColor;
        ForeColor = Program.AppTheme.ForeColor;
        ViewGridWindowChrome.ApplyOnHandleCreated(this, () => Program.AppTheme, true);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var liveTab = new TabPage("DataTable / Repository") { BackColor = Program.AppTheme.BackColor, ForeColor = Program.AppTheme.ForeColor };
        var sqlTab = new TabPage("SQL Server şablonu") { BackColor = Program.AppTheme.BackColor, ForeColor = Program.AppTheme.ForeColor };

        ConfigureGridColumns();
        _grid.SetObjects(CreateAoiDataTable(5000).Rows.Cast<DataRow>());
        _info.Text = "Bu örnek gerçek veritabanına bağlanmadan DataTable/DataRow ile çalışır. Aynı kolon yapısı SQL Server repository sonucuna da uygulanır.";
        _search.TextChanged += (_, _) => _grid.SetGlobalFilter(_search.Text);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(8), WrapContents = false };
        AddButton(buttons, "5.000 kayıt yükle", () => _grid.SetObjects(CreateAoiDataTable(5000).Rows.Cast<DataRow>()));
        AddButton(buttons, "50.000 kayıt yükle", () => _grid.SetObjects(CreateAoiDataTable(50000).Rows.Cast<DataRow>()));
        AddButton(buttons, "Sadece FAIL", () => _grid.SetObjects(CreateAoiDataTable(50000).Rows.Cast<DataRow>().Where(r => Convert.ToString(r["AoiResult"]) == "FAIL")));
        AddButton(buttons, "Temizle", () => _grid.ClearObjects());
        AddButton(buttons, "Kopyala", () => _grid.CopySelectionToClipboard());

        liveTab.Controls.Add(_grid);
        liveTab.Controls.Add(buttons);
        liveTab.Controls.Add(_search);
        liveTab.Controls.Add(_info);

        var sqlText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 10F),
            Text = GetSqlRepositoryTemplate()
        };
        sqlTab.Controls.Add(sqlText);
        tabs.TabPages.Add(liveTab);
        tabs.TabPages.Add(sqlTab);
        Controls.Add(tabs);
    }

    private void ConfigureGridColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new ViewGridColumn("Id", "", 70) { AspectGetter = row => ((DataRow)row)["Id"] });
        _grid.Columns.Add(new ViewGridColumn("Barkod", "", 160) { AspectGetter = row => ((DataRow)row)["Barcode"] });
        _grid.Columns.Add(new ViewGridColumn("Makine", "", 130) { AspectGetter = row => ((DataRow)row)["MachineName"] });
        _grid.Columns.Add(new ViewGridColumn("Assembly", "", 220) { AspectGetter = row => ((DataRow)row)["AssemblyName"] });
        _grid.Columns.Add(new ViewGridColumn("AOI", "", 90) { AspectGetter = row => ((DataRow)row)["AoiResult"] });
        _grid.Columns.Add(new ViewGridColumn("AI", "", 90) { AspectGetter = row => ((DataRow)row)["AIResult"] });
        _grid.Columns.Add(new ViewGridColumn("Bekleyen", "", 90) { AspectGetter = row => ((DataRow)row)["WaitingDecision"] });
        _grid.Columns.Add(new ViewGridColumn("Tarih", "", 160) { AspectGetter = row => ((DateTime)((DataRow)row)["AoiTestTime"]).ToString("yyyy-MM-dd HH:mm") });
    }

    private static DataTable CreateAoiDataTable(int count)
    {
        var table = new DataTable("AoiFailList");
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Barcode", typeof(string));
        table.Columns.Add("MachineName", typeof(string));
        table.Columns.Add("AssemblyName", typeof(string));
        table.Columns.Add("AoiResult", typeof(string));
        table.Columns.Add("AIResult", typeof(string));
        table.Columns.Add("WaitingDecision", typeof(int));
        table.Columns.Add("AoiTestTime", typeof(DateTime));
        string[] machines = { "QX250i", "QX500", "Flex", "Flex Ultra" };
        for (int i = 1; i <= count; i++)
        {
            table.Rows.Add(i, "BC" + i.ToString("00000000"), machines[i % machines.Length], "20CMB20R4B-ALT+" + (23640000 + i), i % 7 == 0 ? "FAIL" : "PASS", i % 11 == 0 ? "WAIT" : "OK", i % 5, DateTime.Today.AddMinutes(-i));
        }
        return table;
    }

    private static void AddButton(FlowLayoutPanel panel, string text, Action action)
    {
        var b = new Button { Text = text, AutoSize = true, Height = 30, Margin = new Padding(4) };
        b.Click += (_, _) => action();
        panel.Controls.Add(b);
    }

    private static string GetSqlRepositoryTemplate() => """"
// SQL Server repository örneği
// NuGet: Microsoft.Data.SqlClient
// Not: UI thread'i kilitlememek için await + SetObjects kullanılır.

public sealed class AoiFailRow
{
    public int TestResultId { get; set; }
    public string Barcode { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string AssemblyName { get; set; } = "";
    public string AoiResult { get; set; } = "";
    public string AIResult { get; set; } = "";
    public int WaitingDecision { get; set; }
    public DateTime AoiTestTime { get; set; }
}

public async Task<List<AoiFailRow>> LoadAoiFailsAsync(string connectionString, int machineId, CancellationToken ct)
{
    const string sql = @"
SELECT TOP (50000)
    tr.Id AS TestResultId,
    tr.Barcode,
    m.Name AS MachineName,
    tr.AssemblyName,
    CASE WHEN tr.AoiResult = 0 THEN 'FAIL' ELSE 'PASS' END AS AoiResult,
    CASE WHEN tr.AIResult = 0 THEN 'FAIL' WHEN tr.AIResult = 1 THEN 'OK' ELSE 'WAIT' END AS AIResult,
    SUM(CASE WHEN d.Id IS NULL OR d.Decision IS NULL THEN 1 ELSE 0 END) AS WaitingDecision,
    tr.AoiTestTime
FROM dbo.DO_TESTRESULT tr
LEFT JOIN dbo.DO_AOIFAIL f ON f.TestResultId = tr.Id
LEFT JOIN dbo.DO_AIDECISION d ON d.AoiFailId = f.Id
LEFT JOIN dbo.Machine m ON m.Id = tr.MachineId
WHERE tr.MachineId = @MachineId
GROUP BY tr.Id, tr.Barcode, m.Name, tr.AssemblyName, tr.AoiResult, tr.AIResult, tr.AoiTestTime
ORDER BY tr.AoiTestTime DESC";

    var result = new List<AoiFailRow>();
    await using var con = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
    await con.OpenAsync(ct);
    await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, con);
    cmd.Parameters.AddWithValue("@MachineId", machineId);
    await using var rd = await cmd.ExecuteReaderAsync(ct);
    while (await rd.ReadAsync(ct))
    {
        result.Add(new AoiFailRow
        {
            TestResultId = rd.GetInt32(0),
            Barcode = rd.GetString(1),
            MachineName = rd.IsDBNull(2) ? "" : rd.GetString(2),
            AssemblyName = rd.IsDBNull(3) ? "" : rd.GetString(3),
            AoiResult = rd.GetString(4),
            AIResult = rd.GetString(5),
            WaitingDecision = rd.GetInt32(6),
            AoiTestTime = rd.GetDateTime(7)
        });
    }
    return result;
}

// ViewGrid kullanım:
// var rows = await repository.LoadAoiFailsAsync(conn, machineId, cancellationToken);
// viewgridView1.SetObjects(rows);
// viewgridView1.FastFilterMenuForHugeLists = true;
// viewgridView1.AsyncLoadFullFilterValues = true;
"""";
}

public sealed class TreeViewSampleForm : Form
{
    private readonly TreeView _tree = new() { Dock = DockStyle.Left, Width = 320, HideSelection = false, BorderStyle = BorderStyle.FixedSingle };
    private readonly ViewGridControl _detailGrid = new()
    {
        Dock = DockStyle.Fill,
        FullRowSelect = true,
        ShowGridLines = true,
        EmptyListMessage = "Ağaçtan bir düğüm seçin",
        EnableModernEmptyState = true
    };
    private readonly TreeViewGridControl _treeGrid = new()
    {
        Dock = DockStyle.Fill,
        FullRowSelect = true,
        ShowGridLines = true,
        EmptyListMessage = "TreeGrid kaydı yok",
        EnableModernEmptyState = true,
        LazyLoadChildren = false,
        EnableTreeContextMenu = true,
        TreeDoubleClickTogglesNode = true,
        TreeSearchBehavior = TreeViewGridSearchBehavior.ExpandAncestorsAndDescendants,
        TreeDefaultExpandLevel = 2
    };
    private readonly TextBox _search = new() { Width = 220, PlaceholderText = "Tree içinde ara..." };
    private readonly ComboBox _stateFilter = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0) };
    private readonly List<TreeDemoNode> _roots;

    public TreeViewSampleForm()
    {
        Text = "ViewGrid TreeView + TreeGrid Örnekleri";
        Width = 1260;
        Height = 760;
        MinimumSize = new Size(980, 620);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Program.AppTheme.BackColor;
        ForeColor = Program.AppTheme.ForeColor;
        ViewGridDialogChrome.ConfigureStandardDialog(this, Program.AppTheme, new Size(980, 620), sizeable: true);
        ViewGridDialogThemeApplier.Apply(this, Program.AppTheme);

        _roots = CreateTreeData();
        ConfigureDetailGrid();
        ConfigureTreeGrid();
        FillTreeView();

        _stateFilter.Items.AddRange(new object[] { "Tüm durumlar", "OK", "FAIL", "WAIT", "Kontrol", "Aktif" });
        _stateFilter.SelectedIndex = 0;
        _search.TextChanged += (_,__) => ApplyTreeSearchAndFilter();
        _stateFilter.SelectedIndexChanged += (_,__) => ApplyTreeSearchAndFilter();

        var tool = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
        tool.Items.Add(new ToolStripControlHost(_search) { Width = 230 });
        tool.Items.Add(new ToolStripControlHost(_stateFilter) { Width = 140 });
        tool.Items.Add(new ToolStripSeparator());
        tool.Items.Add(new ToolStripButton("Tümünü aç", null, (_,__) => { _tree.ExpandAll(); _treeGrid.ExpandAll(); UpdateTreeStatus(); }));
        tool.Items.Add(new ToolStripButton("Tümünü kapat", null, (_,__) => { _tree.CollapseAll(); _treeGrid.CollapseAll(); UpdateTreeStatus(); }));
        tool.Items.Add(new ToolStripButton("2. seviyeye aç", null, (_,__) => { _tree.ExpandAll(); _treeGrid.ExpandToLevel(2); UpdateTreeStatus(); }));
        tool.Items.Add(new ToolStripButton("Kolon seçici", null, (_,__) => _treeGrid.ShowColumnChooser()));
        tool.Items.Add(new ToolStripButton("İlk eşleşmeye git", null, (_,__) => SelectFirstTreeMatch()));
        tool.Items.Add(new ToolStripButton("Seçili dalı aç", null, (_,__) => { if (_treeGrid.SelectedObject != null) _treeGrid.ExpandDescendants(_treeGrid.SelectedObject, 3); UpdateTreeStatus(); }));
        tool.Items.Add(new ToolStripButton("Seçili dalı kapat", null, (_,__) => { if (_treeGrid.SelectedObject != null) _treeGrid.CollapseDescendants(_treeGrid.SelectedObject); UpdateTreeStatus(); }));
        tool.Items.Add(new ToolStripButton("Düğüm yolunu kopyala", null, (_,__) => { if (_treeGrid.SelectedObject != null) Clipboard.SetText(_treeGrid.GetNodePath(_treeGrid.SelectedObject)); }));

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var masterDetail = new TabPage("TreeView + ViewGrid detay") { BackColor = Program.AppTheme.BackColor };
        var treeGridTab = new TabPage("TreeViewGridControl") { BackColor = Program.AppTheme.BackColor };

        masterDetail.Controls.Add(_detailGrid);
        masterDetail.Controls.Add(_tree);
        treeGridTab.Controls.Add(_treeGrid);
        tabs.TabPages.Add(masterDetail);
        tabs.TabPages.Add(treeGridTab);
        Controls.Add(tabs);
        Controls.Add(_status);
        Controls.Add(tool);
        ViewGridDialogThemeApplier.Apply(this, Program.AppTheme);

        _tree.AfterSelect += (_, e) =>
        {
            if (e.Node?.Tag is TreeDemoNode node)
                _detailGrid.SetObjects(Flatten(node).Where(MatchesCurrentFilter).ToList());
            UpdateTreeStatus();
        };
        _treeGrid.NodeExpanded += (_,__) => UpdateTreeStatus();
        _treeGrid.NodeCollapsed += (_,__) => UpdateTreeStatus();
        if (_tree.Nodes.Count > 0) _tree.SelectedNode = _tree.Nodes[0];
        UpdateTreeStatus();
    }

    private void ConfigureDetailGrid()
    {
        _detailGrid.ApplyTheme(Program.AppTheme);
        _detailGrid.Columns.Add(new ViewGridColumn("Seviye", "Level", 70));
        _detailGrid.Columns.Add(new ViewGridColumn("Tip", "Kind", 100));
        _detailGrid.Columns.Add(new ViewGridColumn("Kod", "Code", 150));
        _detailGrid.Columns.Add(new ViewGridColumn("Ad", "Name", 260) { FillFreeSpace = true });
        _detailGrid.Columns.Add(new ViewGridColumn("Durum", "State", 100) { Kind = ViewGridColumnKind.Badge });
        _detailGrid.Columns.Add(new ViewGridColumn("Adet", "Count", 80));
    }

    private void ConfigureTreeGrid()
    {
        _treeGrid.ApplyTheme(Program.AppTheme);
        _treeGrid.Columns.Add(new ViewGridColumn("Ağaç", "Name", 360) { FillFreeSpace = true, AspectGetter = row => new string(' ', ((TreeDemoNode)row).Level * 4) + (((TreeDemoNode)row).Children.Count > 0 ? (_treeGrid.IsExpanded(row) ? "▾ " : "▸ ") : "  ") + ((TreeDemoNode)row).Name });
        _treeGrid.Columns.Add(new ViewGridColumn("Tip", "Kind", 110));
        _treeGrid.Columns.Add(new ViewGridColumn("Kod", "Code", 150));
        _treeGrid.Columns.Add(new ViewGridColumn("Durum", "State", 100) { Kind = ViewGridColumnKind.Badge });
        _treeGrid.Columns.Add(new ViewGridColumn("Adet", "Count", 80));
        _treeGrid.SetChildrenGetter(row => ((TreeDemoNode)row).Children);
        _treeGrid.SetTreeObjects(_roots);
        _treeGrid.CellClick += (_, e) =>
        {
            if (e.Column.AspectName == "Name" && e.RowObject is TreeDemoNode node && node.Children.Count > 0)
                _treeGrid.ToggleNode(node);
        };
    }

    private void FillTreeView()
    {
        _tree.Nodes.Clear();
        foreach (var root in _roots) _tree.Nodes.Add(CreateWinTreeNode(root));
        _tree.ExpandAll();
    }

    private void ApplyTreeSearchAndFilter()
    {
        FillTreeView();
        PruneTreeNodes(_tree.Nodes);
        if (_tree.Nodes.Count > 0)
        {
            _tree.ExpandAll();
            _tree.SelectedNode = _tree.Nodes[0];
        }
        _treeGrid.SetTreeObjects(FilterTreeForGrid(_roots).Cast<object>().ToList());
        _treeGrid.ExpandToLevel(2);
        UpdateTreeStatus();
    }

    private bool MatchesCurrentFilter(TreeDemoNode node)
    {
        string search = _search.Text.Trim();
        string state = _stateFilter.SelectedIndex > 0 ? _stateFilter.Text : string.Empty;
        bool searchOk = string.IsNullOrWhiteSpace(search) || node.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) || node.Code.Contains(search, StringComparison.CurrentCultureIgnoreCase) || node.Kind.Contains(search, StringComparison.CurrentCultureIgnoreCase);
        bool stateOk = string.IsNullOrWhiteSpace(state) || string.Equals(node.State, state, StringComparison.CurrentCultureIgnoreCase);
        return searchOk && stateOk;
    }

    private bool PruneTreeNodes(TreeNodeCollection nodes)
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            var tn = nodes[i];
            bool childMatch = PruneTreeNodes(tn.Nodes);
            bool selfMatch = tn.Tag is TreeDemoNode node && MatchesCurrentFilter(node);
            if (!selfMatch && !childMatch) nodes.RemoveAt(i);
        }
        return nodes.Count > 0;
    }

    private IEnumerable<TreeDemoNode> FilterTreeForGrid(IEnumerable<TreeDemoNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (MatchesCurrentFilter(node) || node.Children.Any(HasMatchingDescendant)) yield return node;
        }
    }

    private bool HasMatchingDescendant(TreeDemoNode node)
    {
        return MatchesCurrentFilter(node) || node.Children.Any(HasMatchingDescendant);
    }

    private void SelectFirstTreeMatch()
    {
        string text = _search.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        _treeGrid.ApplyTreeSearch(text);
    }

    private void UpdateTreeStatus()
    {
        int total = _roots.Sum(x => Flatten(x).Count());
        _status.Text = $"TreeView düğüm: {_tree.GetNodeCount(true)} | TreeGrid görünür: {_treeGrid.ViewCount} | Toplam: {total} | Space/Enter/çift tık ile aç-kapat | Sağ tık ağaç menüsü | Aramada üst dallar otomatik açılır";
        _status.BackColor = Program.AppTheme.PanelBackColor;
        _status.ForeColor = Program.AppTheme.MutedForeColor;
    }

    private static TreeNode CreateWinTreeNode(TreeDemoNode node)
    {
        var tn = new TreeNode($"{node.Name} ({node.Count})") { Tag = node };
        foreach (var child in node.Children) tn.Nodes.Add(CreateWinTreeNode(child));
        return tn;
    }

    private static IEnumerable<TreeDemoNode> Flatten(TreeDemoNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var item in Flatten(child))
                yield return item;
    }

    private static List<TreeDemoNode> CreateTreeData()
    {
        var top = new TreeDemoNode("Top Layer", "Layer", "TOP", "Aktif", 0, 1);
        var bottom = new TreeDemoNode("Bottom Layer", "Layer", "BOTTOM", "Aktif", 0, 1);
        for (int b = 1; b <= 6; b++)
        {
            var board = new TreeDemoNode("Board " + b, "Board", "B" + b, b % 2 == 0 ? "Kontrol" : "OK", 0, 2);
            for (int c = 1; c <= 10; c++)
            {
                var comp = new TreeDemoNode("U" + b + c.ToString("00"), "Component", "REF" + b + c.ToString("00"), c % 5 == 0 ? "FAIL" : c % 7 == 0 ? "WAIT" : "OK", c * 3, 3);
                board.Children.Add(comp);
            }
            top.Children.Add(board);
        }
        for (int b = 7; b <= 12; b++)
        {
            var board = new TreeDemoNode("Board " + b, "Board", "B" + b, b % 3 == 0 ? "Kontrol" : "OK", 0, 2);
            for (int c = 1; c <= 8; c++) board.Children.Add(new TreeDemoNode("R" + b + c.ToString("00"), "Component", "REF" + b + c.ToString("00"), c % 4 == 0 ? "WAIT" : "OK", c, 3));
            bottom.Children.Add(board);
        }
        return new List<TreeDemoNode> { top, bottom };
    }

    private sealed class TreeDemoNode
    {
        public TreeDemoNode(string name, string kind, string code, string state, int count, int level)
        {
            Name = name; Kind = kind; Code = code; State = state; Count = count; Level = level;
        }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Code { get; set; }
        public string State { get; set; }
        public int Count { get; set; }
        public int Level { get; set; }
        public List<TreeDemoNode> Children { get; } = new();
        public override string ToString() => $"{Name} {Code} {State}";
    }
}

public sealed class DialogThemeResizeShowcaseForm : Form
{
    private readonly ViewGridTheme _theme = Program.AppTheme;

    public DialogThemeResizeShowcaseForm()
    {
        Text = "Dialog Theme / Resize Showcase";
        Width = 760;
        Height = 520;
        MinimumSize = new Size(640, 420);
        ViewGridDialogChrome.ConfigureStandardDialog(this, _theme, new Size(640, 420), sizeable: true);

        var info = new Label
        {
            Dock = DockStyle.Top,
            Height = 78,
            Padding = new Padding(16),
            Text = "Bu test ekranı yardımcı pencerelerin ortak standardını kontrol eder: dark/light caption, resize, minimum size, kullanılmayan sistem menüsü öğeleri ve kontrol tema uyumu.",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(14), FlowDirection = FlowDirection.LeftToRight };
        var btnFilter = new Button { Text = "Filtre penceresi", Width = 150, Height = 34 };
        var btnChooser = new Button { Text = "Kolon seçici", Width = 150, Height = 34 };
        var btnSearch = new Button { Text = "Arama paneli", Width = 150, Height = 34 };
        panel.Controls.Add(btnFilter);
        panel.Controls.Add(btnChooser);
        panel.Controls.Add(btnSearch);

        var grid = new ViewGridControl { Dock = DockStyle.Fill, FullRowSelect = true, ShowGridLines = true };
        grid.ApplyTheme(_theme);
        grid.Columns.Add(new ViewGridColumn("Ad", nameof(DemoRow.Name), 180) { FillFreeSpace = true });
        grid.Columns.Add(new ViewGridColumn("Durum", nameof(DemoRow.State), 100));
        grid.Columns.Add(new ViewGridColumn("Yıl", nameof(DemoRow.Year), 80));
        grid.SetObjects(Enumerable.Range(1, 50).Select(i => new DemoRow { Id = i, Name = "Dialog test " + i, State = i % 3 == 0 ? "FAIL" : "OK", Year = 2020 + i % 5 }).ToList());

        btnFilter.Click += (_,__) => grid.ShowHeaderContextMenuForAspect(nameof(DemoRow.State));
        btnChooser.Click += (_,__) => grid.ShowColumnChooser();
        btnSearch.Click += (_,__) => grid.ShowModernSearchPanel();

        Controls.Add(grid);
        Controls.Add(panel);
        Controls.Add(info);
        ViewGridDialogThemeApplier.Apply(this, _theme);
    }
}

public sealed class ViewGridDataModeVerificationForm : Form
{
    public ViewGridDataModeVerificationForm()
    {
        Text = "ViewGridControl Mode Doğrulama - Object / DataTable / Virtual / Tree / Tile";
        Width = 1180;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Program.AppTheme.BackColor;
        ForeColor = Program.AppTheme.ForeColor;
        ViewGridWindowChrome.ApplyOnHandleCreated(this, () => Program.AppTheme, true);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateObjectTab());
        tabs.TabPages.Add(CreateDataTableTab());
        tabs.TabPages.Add(CreateVirtualTab());
        tabs.TabPages.Add(CreateTreeTab());
        tabs.TabPages.Add(CreateTileTab());
        Controls.Add(tabs);
    }

    private static TabPage CreateObjectTab()
    {
        var grid = CreateDemoGrid(ViewGridDataMode.Object);
        grid.SetObjects(CreateRows(300));
        return Wrap("Mode = Object", "POCO / model listesi ile klasik ViewGrid/GLV benzeri kullanım.", grid);
    }

    private static TabPage CreateDataTableTab()
    {
        var grid = CreateDemoGrid(ViewGridDataMode.DataTable);
        grid.BindDataTable(CreateModeDataTable(300), autoGenerateColumns: true, primaryKey: "Id");
        return Wrap("Mode = DataTable", "DataTable, DataView veya BindingSource tabanlı çalışma. AutoGenerateColumns ve editör altyapısı birlikte test edilir.", grid);
    }

    private static TabPage CreateVirtualTab()
    {
        var grid = CreateDemoGrid(ViewGridDataMode.Virtual);
        grid.SetUltraVirtualProvider(new VirtualDemoRowProvider(1_000_000));
        return Wrap("Mode = Virtual", "IRowProvider / IQueryRowProvider ile sanal veri. Filtre/sıralama provider tarafına taşınabilir.", grid);
    }

    private static TabPage CreateTreeTab()
    {
        var grid = new TreeViewGridControl
        {
            Dock = DockStyle.Fill,
            Mode = ViewGridDataMode.Tree,
            FullRowSelect = true,
            ShowGridLines = true,
            EnableModernEmptyState = true,
            EmptyListMessage = "Tree kayıt yok"
        };
        grid.Columns.Add(new ViewGridColumn("Ad", "Name", 260));
        grid.Columns.Add(new ViewGridColumn("Tür", "State", 130));
        grid.Columns.Add(new ViewGridColumn("Yıl", "Year", 80));
        grid.SetChildrenGetter(row => ((DemoRow)row).Children ?? Enumerable.Empty<DemoRow>());
        var roots = CreateRows(8).ToList();
        for (int i = 0; i < roots.Count; i++)
        {
            roots[i].Children = CreateRows(3).Select(x => { x.Name = roots[i].Name + " / Alt " + x.Id; return x; }).ToList();
        }
        grid.SetTreeObjects(roots);
        grid.Mode = ViewGridDataMode.Tree;
        return Wrap("Mode = Tree", "TreeViewGridControl ile hiyerarşik veri senaryosu. Aynı ViewGrid kolon/render altyapısını kullanır.", grid);
    }

    private static TabPage CreateTileTab()
    {
        var grid = CreateDemoGrid(ViewGridDataMode.Tile);
        grid.TileMinWidth = 260;
        grid.TilePreferredHeight = 110;
        grid.SetObjects(CreateRows(150));
        grid.Mode = ViewGridDataMode.Tile;
        return Wrap("Mode = Tile", "Kart/tile görünüm odaklı kullanım. Mode=Tile otomatik Tile ViewMode uygular.", grid);
    }

    private static ViewGridControl CreateDemoGrid(ViewGridDataMode mode)
    {
        var grid = new ViewGridControl
        {
            Dock = DockStyle.Fill,
            Mode = mode,
            FullRowSelect = true,
            MultiSelect = true,
            ShowGridLines = true,
            EnableModernEmptyState = true,
            EnableHighlightEngine = true,
            HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full,
            EmptyListMessage = "Kayıt yok"
        };
        grid.Columns.Add(new ViewGridColumn("Id", nameof(DemoRow.Id), 70));
        grid.Columns.Add(new ViewGridColumn("Ad", nameof(DemoRow.Name), 240));
        grid.Columns.Add(new ViewGridColumn("Durum", nameof(DemoRow.State), 110));
        grid.Columns.Add(new ViewGridColumn("Meslek", nameof(DemoRow.Occupation), 150));
        grid.Columns.Add(new ViewGridColumn("Yıl", nameof(DemoRow.Year), 80));
        grid.Columns.Add(new ViewGridColumn("Progress", nameof(DemoRow.Progress), 110) { Kind = ViewGridColumnKind.ProgressBar });
        return grid;
    }

    private static TabPage Wrap(string title, string info, Control content)
    {
        var page = new TabPage(title) { BackColor = Program.AppTheme.BackColor, ForeColor = Program.AppTheme.ForeColor };
        var label = new Label
        {
            Dock = DockStyle.Top,
            Height = 46,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 12, 0),
            Text = info,
            BackColor = Program.AppTheme.PanelBackColor,
            ForeColor = Program.AppTheme.ForeColor
        };
        page.Controls.Add(content);
        page.Controls.Add(label);
        return page;
    }

    private static List<DemoRow> CreateRows(int count)
    {
        var list = new List<DemoRow>(count);
        for (int i = 1; i <= count; i++)
        {
            list.Add(new DemoRow
            {
                Id = i,
                RealIndex = i,
                Name = "ViewGrid Mode satır " + i.ToString("0000"),
                State = i % 7 == 0 ? "Fail" : i % 5 == 0 ? "Review" : "OK",
                Occupation = DemoData.Occupations[i % DemoData.Occupations.Length],
                Year = 2020 + (i % 7),
                Progress = (i * 7) % 101,
                Rating = i % 6
            });
        }
        return list;
    }

    private static DataTable CreateModeDataTable(int count)
    {
        var t = new DataTable("ViewGridDataModeDataTable");
        t.Columns.Add("Id", typeof(int));
        t.Columns.Add("Name", typeof(string));
        t.Columns.Add("State", typeof(string));
        t.Columns.Add("Occupation", typeof(string));
        t.Columns.Add("Year", typeof(int));
        t.Columns.Add("Progress", typeof(int));
        foreach (var r in CreateRows(count))
            t.Rows.Add(r.Id, r.Name, r.State, r.Occupation, r.Year, r.Progress);
        t.AcceptChanges();
        return t;
    }
}

public sealed class FeatureVerificationSuiteForm : Form
{
    public FeatureVerificationSuiteForm()
    {
        var theme = Program.AppTheme;
        Text = "ViewGrid Özellik Test Merkezi";
        Width = 920;
        Height = 620;
        MinimumSize = new Size(760, 480);
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        ViewGridWindowChrome.ApplyOnHandleCreated(this, () => theme, true);

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(16, 10, 16, 4),
            Text = "ViewGrid özellik doğrulama örnekleri",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            BackColor = theme.HeaderBackColor,
            ForeColor = theme.HeaderForeColor
        };
        var info = new Label
        {
            Dock = DockStyle.Top,
            Height = 54,
            Padding = new Padding(16, 4, 16, 8),
            Text = "Her kart tek bir özelliği izole eder. Butonlara basıp davranışı ayrı ayrı kontrol edebilirsin; sonuç bilgisi form altındaki açıklama alanında güncellenir.",
            BackColor = theme.PanelBackColor,
            ForeColor = theme.ForeColor
        };
        var scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(16),
            BackColor = theme.BackColor,
            ForeColor = theme.ForeColor
        };
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 0,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            BackColor = theme.BackColor,
            ForeColor = theme.ForeColor
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        AddSuiteCard(grid, theme, "1) Her Kolonda Checkbox", "Person, Operasyon, Onay ve Kilit gibi normal metin kolonlarında checkbox + metin birlikte çizilir; header checkbox ve Space desteği test edilir.", () => new MultiColumnCheckBoxSampleForm().Show());
        AddSuiteCard(grid, theme, "2) Kart / Geniş Kart Checkbox", "Tile/Kart/Geniş Kart/Poster görünümünde overlay checkbox, header toggle ve konum ayarları test edilir.", () => new TileCardCheckBoxSampleForm().Show());
        AddSuiteCard(grid, theme, "3) Layout Manager", "Kolon sırası, genişlik, görünürlük, sort/group bilgisi kaydet-yükle-reset.", () => new LayoutManagerVerificationForm().Show());
        AddSuiteCard(grid, theme, "4) Incremental Search + Highlight", "Yazınca satır bulma, sonraki/önceki eşleşme ve geçici satır highlight testi.", () => new IncrementalSearchHighlightVerificationForm().Show());
        AddSuiteCard(grid, theme, "5) Advanced Copy / Export", "Hücre, satır, JSON kopyalama; seçili/görünen CSV, Excel XML ve JSON export testi.", () => new AdvancedCopyExportVerificationForm().Show());
        AddSuiteCard(grid, theme, "6) Grouping", "Durum/meslek/yıl gruplama, expand/collapse ve grup rengini ayrı test eder.", () => new GroupingVerificationForm().Show());
        AddSuiteCard(grid, theme, "7) Mini Analytics", "Kolon bazlı boş/değer sayısı, distinct ve top values raporunu canlı gösterir.", () => new MiniAnalyticsVerificationForm().Show());
        AddSuiteCard(grid, theme, "8) ViewGridControl Mode", "Object/DataTable/Virtual/Tree/Tile modlarını ayrı sekmelerde doğrular.", () => new ViewGridDataModeVerificationForm().Show());
        AddSuiteCard(grid, theme, "9) Hepsi Bir Arada Smoke Test", "Tüm yeni API'leri aynı grid üzerinde hızlı duman testiyle çalıştırır.", () => new GLVFeaturePackSampleForm().Show());
        AddSuiteCard(grid, theme, "10) Geniş Kart / Ticket Görünümü", "LargeCard modunu, 4-5 satırlı geniş ticket kartlarını ve seçim sonrası yükseklik korumasını test eder.", () => new LargeCardSampleForm().Show());
        AddSuiteCard(grid, theme, "11) OLV Ekstra Uyumluluk", "Selection snapshot, aspect lookup, kolon göster/gizle ve checked helper API'lerini test eder.", () => new OLVExtrasSampleForm().Show());
        AddSuiteCard(grid, theme, "12) Details Çok Satırlı Hücre", "Detaylı görünümde uzun hücre metninin kolon genişliğine göre 4-5 satıra sarılmasını test eder.", () => new MultilineCellsSampleForm().Show());

        scrollHost.Controls.Add(grid);
        Controls.Add(scrollHost);
        Controls.Add(info);
        Controls.Add(title);
    }

    private static void AddSuiteCard(TableLayoutPanel grid, ViewGridTheme theme, string header, string description, Action open)
    {
        int index = grid.Controls.Count;
        int row = index / 2;
        int col = index % 2;
        if (col == 0)
        {
            grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        }
        var panel = new Panel
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
            Height = 96,
            Margin = new Padding(6),
            Padding = new Padding(10),
            BackColor = theme.PanelBackColor,
            ForeColor = theme.ForeColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        var btn = new Button
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(96, 72),
            Text = "Test Et",
            BackColor = theme.AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btn.Left = panel.ClientSize.Width - btn.Width - 10;
        btn.Top = 10;
        btn.Click += (_,__) => open();
        panel.Resize += (_,__) => btn.Left = panel.ClientSize.Width - btn.Width - 10;
        var label = new Label
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
            AutoEllipsis = true,
            Text = header + Environment.NewLine + description,
            Padding = new Padding(0, 4, 8, 0),
            ForeColor = theme.ForeColor
        };
        label.SetBounds(0, 0, Math.Max(10, panel.ClientSize.Width - btn.Width - 28), panel.ClientSize.Height - 2);
        panel.Resize += (_,__) => label.SetBounds(0, 0, Math.Max(10, panel.ClientSize.Width - btn.Width - 28), panel.ClientSize.Height - 2);
        panel.Controls.Add(label);
        panel.Controls.Add(btn);
        grid.Controls.Add(panel, col, row);
    }
}

public sealed class LayoutManagerVerificationForm : ViewGridSampleFormBase
{
    private readonly string _layoutPath = Path.Combine(Application.StartupPath, "viewgrid-v2487-layout-test.json");

    public LayoutManagerVerificationForm() : base("Layout Manager", "Test: kolonları değiştir, kaydet, boz, yükle ve reset ile varsayılana dön. Sort/group bilgisi de layout içinde taşınır.")
    {
        ViewGrid.ColumnLayoutStorageKey = "ViewGridV2487LayoutTest";
        ViewGrid.AutoSaveColumnLayout = false;
        Tool.Items.Add(new ToolStripButton("1 Kaydet", null, (_,__) => { ViewGrid.SaveLayout(_layoutPath); Info.Text = "Layout kaydedildi: " + _layoutPath; }));
        Tool.Items.Add(new ToolStripButton("2 Kolonları boz", null, (_,__) => { var progressCol = ViewGrid.Columns[nameof(DemoRow.Progress)]; if (progressCol != null) progressCol.Visible = false; var nameCol = ViewGrid.Columns[nameof(DemoRow.Name)]; if (nameCol != null) nameCol.Width = 420; ViewGrid.SetGroupBy(nameof(DemoRow.State)); ViewGrid.RefreshView(); Info.Text = "Kolon görünürlüğü/genişliği ve grup değiştirildi. Şimdi Yükle ile geri al."; }));
        Tool.Items.Add(new ToolStripButton("3 Yükle", null, (_,__) => { ViewGrid.LoadLayout(_layoutPath); Info.Text = "Layout yüklendi."; }));
        Tool.Items.Add(new ToolStripButton("4 Reset", null, (_,__) => { ViewGrid.ResetColumnLayout(); Info.Text = "Layout varsayılana döndü."; }));
        Tool.Items.Add(new ToolStripButton("Kolon seçici", null, (_,__) => ViewGrid.ShowColumnChooser()));
        Tool.Items.Add(new ToolStripButton("Duruma göre grupla", null, (_,__) => ViewGrid.SetGroupBy(nameof(DemoRow.State))));
        Tool.Items.Add(new ToolStripButton("Grubu kaldır", null, (_,__) => ViewGrid.ClearGrouping()));
    }
}

public sealed class IncrementalSearchHighlightVerificationForm : ViewGridSampleFormBase
{
    private readonly ToolStripTextBox _find = new() { Width = 160 };

    public IncrementalSearchHighlightVerificationForm() : base("Incremental Search + Highlight", "Test: kutuya 'AOI kayıt 199' veya 'Fail' yaz; Bul/Önceki ile gez. Satır highlight butonları geçici vurgu üretir.")
    {
        ViewGrid.HighlightSearchText = true;
        ViewGrid.HighlightGlobalFilterText = true;
        ViewGrid.EnableIncrementalSearch = true;
        ViewGrid.DefaultHighlightDurationMs = 4500;
        ViewGrid.SetObjects(CreateRows(1000));
        Tool.Items.Add(new ToolStripLabel("Ara:"));
        Tool.Items.Add(_find);
        Tool.Items.Add(new ToolStripButton("Bul", null, (_,__) => DoFindNext()));
        Tool.Items.Add(new ToolStripButton("Önceki", null, (_,__) => DoFindPrevious()));
        Tool.Items.Add(new ToolStripButton("Fail satırlarını vurgula", null, (_,__) => HighlightFailRows()));
        Tool.Items.Add(new ToolStripButton("Seçiliyi vurgula", null, (_,__) => { if (ViewGrid.SelectedObject != null) ViewGrid.HighlightObject(ViewGrid.SelectedObject, Color.Orange, 6000, "manual"); }));
        Tool.Items.Add(new ToolStripButton("Vurguları temizle", null, (_,__) => ViewGrid.ClearRowHighlights()));
        _find.TextChanged += (_,__) => { if (!string.IsNullOrWhiteSpace(_find.Text)) ViewGrid.JumpToFirstMatch(_find.Text); else ViewGrid.ClearSearchHighlight(); };
    }

    private void DoFindNext()
    {
        bool ok = ViewGrid.FindNext(_find.Text);
        Info.Text = ok ? "Sonraki eşleşmeye gidildi." : "Eşleşme bulunamadı.";
    }

    private void DoFindPrevious()
    {
        bool ok = ViewGrid.FindPrevious(_find.Text);
        Info.Text = ok ? "Önceki eşleşmeye gidildi." : "Eşleşme bulunamadı.";
    }

    private void HighlightFailRows()
    {
        int highlighted = 0;
        for (int i = 0; i < ViewGrid.ViewCount && highlighted < 25; i++)
        {
            if (ViewGrid.GetModelObject(i) is DemoRow row && row.State == "Fail")
            {
                ViewGrid.HighlightRow(i, Color.FromArgb(255, 193, 7), 7000, "Fail");
                highlighted++;
            }
        }
        Info.Text = $"İlk {highlighted} Fail satırı geçici vurgulandı.";
    }
}

public sealed class AdvancedCopyExportVerificationForm : ViewGridSampleFormBase
{
    private readonly string _folder = Path.Combine(Application.StartupPath, "ViewGridExportTests");

    public AdvancedCopyExportVerificationForm() : base("Advanced Copy / Export", "Test: birkaç satır seç; hücre/satır/JSON kopyala. CSV, Excel XML ve JSON dosyaları test klasörüne yazılır.")
    {
        Directory.CreateDirectory(_folder);
        ViewGrid.MultiSelect = true;
        ViewGrid.SelectObject(ViewGrid.GetModelObject(4));
        ViewGrid.SelectObject(ViewGrid.GetModelObject(9), addToSelection: true);
        Tool.Items.Add(new ToolStripButton("Seçimi kopyala", null, (_,__) => { ViewGrid.CopySelectionToClipboard(); Info.Text = "Seçim TSV olarak panoya kopyalandı."; }));
        Tool.Items.Add(new ToolStripButton("Hücreyi kopyala", null, (_,__) => { ViewGrid.CopySelectedCellToClipboard(); Info.Text = "Aktif hücre panoya kopyalandı."; }));
        Tool.Items.Add(new ToolStripButton("JSON kopyala", null, (_,__) => { ViewGrid.CopySelectionAsJsonToClipboard(); Info.Text = "Seçim JSON olarak panoya kopyalandı."; }));
        Tool.Items.Add(new ToolStripButton("CSV export", null, (_,__) => Export(() => ViewGrid.ExportVisibleCsv(Path.Combine(_folder, "visible.csv")))));
        Tool.Items.Add(new ToolStripButton("Excel export", null, (_,__) => Export(() => ViewGrid.ExportVisibleExcel(Path.Combine(_folder, "visible.xlsx"), "ViewGridTest"))));
        Tool.Items.Add(new ToolStripButton("JSON export", null, (_,__) => Export(() => ViewGrid.ExportVisibleJson(Path.Combine(_folder, "visible.json")))));
        Tool.Items.Add(new ToolStripButton("Seçili JSON export", null, (_,__) => Export(() => ViewGrid.ExportSelectedJson(Path.Combine(_folder, "selected.json")))));
    }

    private void Export(Func<string> action)
    {
        try
        {
            var path = action();
            Info.Text = "Dosya oluşturuldu: " + path;
            MessageBox.Show(this, path, "Export Test");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Export Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

public sealed class GroupingVerificationForm : ViewGridSampleFormBase
{
    public GroupingVerificationForm() : base("Grouping", "Test: gruplama kolonunu değiştir; tüm grupları daralt/aç; grup rengi ve group clear davranışını kontrol et.")
    {
        ViewGrid.EnableGrouping = true;
        ViewGrid.SetObjects(CreateRows(800));
        Tool.Items.Add(new ToolStripButton("Durum", null, (_,__) => { ViewGrid.SetGroupBy(nameof(DemoRow.State)); Info.Text = "Duruma göre gruplandı."; }));
        Tool.Items.Add(new ToolStripButton("Meslek", null, (_,__) => { ViewGrid.SetGroupBy(nameof(DemoRow.Occupation)); Info.Text = "Mesleğe göre gruplandı."; }));
        Tool.Items.Add(new ToolStripButton("Yıl", null, (_,__) => { ViewGrid.SetGroupBy(nameof(DemoRow.Year)); Info.Text = "Yıla göre gruplandı."; }));
        Tool.Items.Add(new ToolStripButton("Daralt", null, (_,__) => { ViewGrid.CollapseAllGroups(); Info.Text = "Tüm gruplar daraltıldı."; }));
        Tool.Items.Add(new ToolStripButton("Aç", null, (_,__) => { ViewGrid.ExpandAllGroups(); Info.Text = "Tüm gruplar açıldı."; }));
        Tool.Items.Add(new ToolStripButton("Renk ver", null, (_,__) => { ViewGrid.CustomGroupBackColor = Color.FromArgb(235, 229, 250); ViewGrid.Invalidate(); Info.Text = "Grup başlığı rengi değiştirildi."; }));
        Tool.Items.Add(new ToolStripButton("Temizle", null, (_,__) => { ViewGrid.ClearGrouping(); Info.Text = "Gruplama kaldırıldı."; }));
        ViewGrid.SetGroupBy(nameof(DemoRow.State));
    }
}

public sealed class MiniAnalyticsVerificationForm : ViewGridSampleFormBase
{
    private readonly TextBox _analytics = new() { Dock = DockStyle.Right, Width = 360, Multiline = true, ScrollBars = ScrollBars.Both, ReadOnly = true, Font = new Font("Consolas", 9F) };

    public MiniAnalyticsVerificationForm() : base("Mini Analytics", "Test: kolon bazlı Row/Blank/Distinct/Top Values raporunu üret. Filtre uygula ve raporu tekrar yenileyerek görünür veri analitiğini kontrol et.")
    {
        Controls.Add(_analytics);
        _analytics.BringToFront();
        ViewGrid.SetObjects(CreateRows(1200));
        Tool.Items.Add(new ToolStripButton("Analytics yenile", null, (_,__) => RefreshAnalytics()));
        Tool.Items.Add(new ToolStripButton("Durum analizi", null, (_,__) => ShowColumnAnalytics(nameof(DemoRow.State))));
        Tool.Items.Add(new ToolStripButton("Meslek analizi", null, (_,__) => ShowColumnAnalytics(nameof(DemoRow.Occupation))));
        Tool.Items.Add(new ToolStripButton("Panoya kopyala", null, (_,__) => { ViewGrid.CopyMiniAnalyticsToClipboard(); Info.Text = "Mini analytics panoya kopyalandı."; }));
        Tool.Items.Add(new ToolStripButton("Fail filtresi", null, (_,__) => { ViewGrid.SetGlobalFilter("Fail"); RefreshAnalytics(); }));
        Tool.Items.Add(new ToolStripButton("Filtre temizle", null, (_,__) => { ViewGrid.ClearFilters(); RefreshAnalytics(); }));
        RefreshAnalytics();
    }

    private void RefreshAnalytics()
    {
        var lines = new List<string> { "Column | Rows | Blank | Distinct | Top values", new string('-', 76) };
        foreach (var item in ViewGrid.GetVisibleAnalytics(maxDistinctPerColumn: 5, maxRows: 50000))
        {
            var top = string.Join(", ", item.TopValues.Select(v => $"{v.Value}:{v.Count}"));
            lines.Add($"{item.ColumnText} | {item.RowCount} | {item.BlankCount} | {item.DistinctCount} | {top}");
        }
        _analytics.Text = string.Join(Environment.NewLine, lines);
        Info.Text = "Analytics yenilendi. Filtre varsa sadece görünür satırlar üzerinden hesaplandı.";
    }

    private void ShowColumnAnalytics(string aspectName)
    {
        var item = ViewGrid.GetColumnAnalytics(aspectName, maxDistinct: 10, maxRows: 50000);
        if (item == null) return;
        _analytics.Text = item.ColumnText + Environment.NewLine +
            $"Rows: {item.RowCount}" + Environment.NewLine +
            $"Blank: {item.BlankCount}" + Environment.NewLine +
            $"Distinct: {item.DistinctCount}" + Environment.NewLine +
            "Top Values:" + Environment.NewLine +
            string.Join(Environment.NewLine, item.TopValues.Select(v => "  " + v.Value + " = " + v.Count));
        Info.Text = item.ColumnText + " kolonu analiz edildi.";
    }
}


public sealed class DesignerPreviewSampleForm : ViewGridSampleFormBase
{
    private readonly ToolStripComboBox _preview = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };

    public DesignerPreviewSampleForm() : base("ViewGrid Designer Önizleme", "DesignTimeThemePreview, AutoThemeFromParent ve örnek veri davranışını runtime'da da test eder.")
    {
        foreach (ViewGridDesignTimeThemePreview value in Enum.GetValues(typeof(ViewGridDesignTimeThemePreview)))
            _preview.Items.Add(value);
        _preview.SelectedItem = ViewGridDesignTimeThemePreview.Auto;
        _preview.SelectedIndexChanged += (_,__) => ApplyPreviewMode();

        Tool.Items.Add(new ToolStripLabel("Designer tema:"));
        Tool.Items.Add(_preview);
        Tool.Items.Add(new ToolStripButton("Form koyu", null, (_,__) => { BackColor = Color.FromArgb(32, 32, 38); ForeColor = Color.White; ApplyParentPreview(); }));
        Tool.Items.Add(new ToolStripButton("Form açık", null, (_,__) => { BackColor = Color.FromArgb(245, 247, 250); ForeColor = Color.FromArgb(25, 25, 25); ApplyParentPreview(); }));
        Tool.Items.Add(new ToolStripButton("Örnek veri yenile", null, (_,__) => { ViewGrid.DesignTimeSampleData = true; ViewGrid.RefreshDesignTimePreview(); }));

        ViewGrid.AutoThemeFromParent = true;
        ViewGrid.DesignTimeSampleData = true;
        ApplyPreviewMode();
    }

    private void ApplyPreviewMode()
    {
        if (_preview.SelectedItem is not ViewGridDesignTimeThemePreview mode) return;
        ViewGrid.DesignTimeThemePreview = mode;
        ApplyParentPreview();
    }

    private void ApplyParentPreview()
    {
        var mode = ViewGrid.DesignTimeThemePreview;
        var theme = mode switch
        {
            ViewGridDesignTimeThemePreview.Light => ViewGridTheme.LightTheme(),
            ViewGridDesignTimeThemePreview.Dark => ViewGridTheme.DarkTheme(),
            ViewGridDesignTimeThemePreview.Fluent => BackColor.GetBrightness() < 0.45f ? ViewGridTheme.FluentDarkTheme() : ViewGridTheme.FluentLightTheme(),
            _ => ViewGridTheme.FromParentColor(BackColor, ForeColor)
        };
        ViewGrid.ApplyTheme(theme);
        Info.BackColor = theme.PanelBackColor;
        Info.ForeColor = theme.ForeColor;
        Tool.BackColor = theme.HeaderBackColor;
        Tool.ForeColor = theme.HeaderForeColor;
        Info.Text = $"Designer önizleme modu: {mode} | AutoThemeFromParent={ViewGrid.AutoThemeFromParent} | Form rengi={ColorTranslator.ToHtml(BackColor)}";
    }
}

public sealed class DatabaseCrudEditorsSampleForm : Form
{
    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        FullRowSelect = true,
        MultiSelect = true,
        ShowGridLines = true,
        EnableCellEditing = true,
        AllowEditAllCells = true,
        EmptyListMessage = "CRUD örneği için kayıt yok",
        EnableModernEmptyState = true,
        FastFilterMenuForHugeLists = true,
        AsyncLoadFullFilterValues = true
    };

    private readonly DataTable _table;
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 38, Padding = new Padding(12, 0, 12, 0), TextAlign = ContentAlignment.MiddleLeft };

    public DatabaseCrudEditorsSampleForm()
    {
        Text = "Database CRUD + Hücre Editörleri";
        Width = 1180;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Program.AppTheme.BackColor;
        ForeColor = Program.AppTheme.ForeColor;
        ViewGridWindowChrome.ApplyOnHandleCreated(this, () => Program.AppTheme, true);
        _grid.ApplyTheme(Program.AppTheme);

        _table = CreateEditableTable();
        _grid.PrimaryKey = "Id";
        _grid.AutoGenerateColumns = true;
        _grid.DataSource = _table;
        ConfigureGeneratedColumns();

        var tool = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
        tool.Items.Add(new ToolStripButton("Yeni satır", null, (_,__) => AddRow()));
        tool.Items.Add(new ToolStripButton("Seçiliyi sil", null, (_,__) => DeleteRows()));
        tool.Items.Add(new ToolStripButton("SaveChanges", null, (_,__) => SaveChangesDemo()));
        tool.Items.Add(new ToolStripButton("RejectChanges", null, (_,__) => { _grid.RejectChanges(); RefreshStatus(); }));
        tool.Items.Add(new ToolStripButton("Fail filtresi", null, (_,__) => _grid.SetGlobalFilter("Fail")));
        tool.Items.Add(new ToolStripButton("Filtre temizle", null, (_,__) => _grid.ClearFilters()));

        _grid.CellValueChanged += (_,__) => RefreshStatus();
        Controls.Add(_grid);
        Controls.Add(_status);
        Controls.Add(tool);
        RefreshStatus();
    }

    private void ConfigureGeneratedColumns()
    {
        var id = _grid.GetColumn("Id");
        if (id != null) { id.ReadOnly = true; id.Editable = false; id.Width = 70; }

        var barcode = _grid.GetColumn("Barcode");
        if (barcode != null) { barcode.EditorType = ViewGrid.Editing.ViewGridCellEditorKind.TextBox; barcode.Required = true; barcode.MaxLength = 30; barcode.Width = 160; }

        var active = _grid.GetColumn("IsActive");
        if (active != null) { active.Header = "Aktif"; active.Kind = ViewGridColumnKind.CheckBox; active.EditorType = ViewGrid.Editing.ViewGridCellEditorKind.CheckBox; active.Width = 80; }

        var result = _grid.GetColumn("Result");
        if (result != null) { result.Header = "Sonuç"; result.Kind = ViewGridColumnKind.ComboBox; result.EditorType = ViewGrid.Editing.ViewGridCellEditorKind.ComboBox; result.ComboBoxItems = new List<string> { "PASS", "FAIL", "WAIT", "REPAIR" }; result.Width = 110; }

        var qty = _grid.GetColumn("Quantity");
        if (qty != null) { qty.Header = "Adet"; qty.Kind = ViewGridColumnKind.Numeric; qty.EditorType = ViewGrid.Editing.ViewGridCellEditorKind.Numeric; qty.NumericMinimum = 0; qty.NumericMaximum = 999999; qty.Width = 90; }

        var score = _grid.GetColumn("Score");
        if (score != null) { score.Header = "Puan"; score.Kind = ViewGridColumnKind.Numeric; score.EditorType = ViewGrid.Editing.ViewGridCellEditorKind.Numeric; score.NumericMinimum = 0; score.NumericMaximum = 100; score.Width = 90; }

        var date = _grid.GetColumn("TestDate");
        if (date != null) { date.Header = "Test Tarihi"; date.Kind = ViewGridColumnKind.Date; date.EditorType = ViewGrid.Editing.ViewGridCellEditorKind.DateTime; date.DateTimeFormat = ViewGrid.Editing.ViewGridDateTimeEditorFormat.Custom; date.DateTimeCustomFormat = "dd.MM.yyyy HH:mm"; date.Width = 150; }

        _grid.RebuildColumns();
    }

    private void AddRow()
    {
        int nextId = _table.Rows.Cast<DataRow>().Where(r => r.RowState != DataRowState.Deleted).Select(r => Convert.ToInt32(r["Id"])).DefaultIfEmpty(0).Max() + 1;
        _grid.AddNewRow(("Id", nextId), ("Barcode", "BC" + nextId.ToString("000000")), ("IsActive", true), ("Result", "WAIT"), ("Quantity", 1), ("Score", 50), ("TestDate", DateTime.Now));
        RefreshStatus();
    }

    private void DeleteRows()
    {
        int deleted = _grid.DeleteSelectedRows();
        MessageBox.Show(this, deleted + " satır silinmek üzere işaretlendi.", "ViewGrid CRUD");
        RefreshStatus();
    }

    private void SaveChangesDemo()
    {
        int affected = _grid.SaveChanges();
        MessageBox.Show(this, "Demo DataTable modunda SaveChanges AcceptChanges yapar. SQL adapter bağlıysa INSERT/UPDATE/DELETE komutları çalışır. Affected: " + affected, "ViewGrid CRUD");
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        _status.Text = $"Rows: {_grid.ObjectCount:N0}   Changed: {_grid.ChangedRowCount:N0}   HasChanges: {_grid.HasChanges}   Editörler: TextBox / CheckBox / ComboBox / Numeric / DateTime";
    }

    private static DataTable CreateEditableTable()
    {
        var t = new DataTable("DO_TESTRESULT_DEMO");
        t.Columns.Add("Id", typeof(int));
        t.Columns.Add("Barcode", typeof(string)).MaxLength = 30;
        t.Columns.Add("IsActive", typeof(bool));
        t.Columns.Add("Result", typeof(string));
        t.Columns.Add("Quantity", typeof(int));
        t.Columns.Add("Score", typeof(long));
        t.Columns.Add("TestDate", typeof(DateTime));
        t.PrimaryKey = new[] { t.Columns["Id"]! };
        for (int i = 1; i <= 250; i++)
            t.Rows.Add(i, "BC" + i.ToString("000000"), i % 3 != 0, i % 8 == 0 ? "FAIL" : i % 5 == 0 ? "WAIT" : "PASS", i % 100, (long)(i % 101), DateTime.Today.AddMinutes(-i));
        t.AcceptChanges();
        return t;
    }
}

public sealed class DesignerMenuMergeSampleForm : Form
{
    private readonly ViewGrid.Core.ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = ViewGridDataMode.Object,
        ViewMode = ViewGridMode.Details,
        DesignTimeThemePreview = ViewGridDesignTimeThemePreview.Auto,
        AutoThemeFromParent = true,
        MergeBuiltInMenuWithUserContextMenu = true,
        BuiltInMenuMergeText = "Liste özellikleri"
    };

    private readonly Label _info = new()
    {
        Dock = DockStyle.Top,
        Height = 58,
        Padding = new Padding(12),
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "ViewGrid üzerinde sağ tıkla: kullanıcı ContextMenuStrip menüsünün altında 'Liste özellikleri' olarak ViewGrid'nin kendi hızlı menüsü birleşir."
    };

    private ToolStrip? _top;
    private ContextMenuStrip? _cms;

    public DesignerMenuMergeSampleForm()
    {
        Text = "ViewGrid Designer + ContextMenu Merge Örneği";
        Width = 980;
        Height = 640;
        StartPosition = FormStartPosition.CenterScreen;

        _cms = new ContextMenuStrip();
        _cms.Items.Add("Kullanıcı menüsü: Barkodu kopyala", null, (_,__) => Clipboard.SetText("DEMO-0001"));
        _cms.Items.Add("Kullanıcı menüsü: Özel işlem", null, (_,__) => MessageBox.Show(this, "Bu öğe uygulamanın kendi menüsünden geldi.", "User menu"));
        _cms.Opening += (_,__) => _grid.AttachBuiltInMenuTo(_cms);
        _grid.ContextMenuStrip = _cms;

        _grid.Columns.Add(new ViewGridColumn("Id", nameof(Row.Id), 70));
        _grid.Columns.Add(new ViewGridColumn("Ad", nameof(Row.Name), 180) { FillFreeSpace = true });
        _grid.Columns.Add(new ViewGridColumn("Durum", nameof(Row.State), 110));
        _grid.Columns.Add(new ViewGridColumn("Oran", nameof(Row.Rate), 90) { TextAlign = ContentAlignment.MiddleRight });
        _grid.SetObjects(Enumerable.Range(1, 80).Select(i => new Row(i, "Kart " + i, i % 5 == 0 ? "Fail" : i % 3 == 0 ? "Review" : "OK", i * 1.23m)));

        _top = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top };
        _top.Items.Add(new ToolStripButton("Parent açık tema", null, (_,__) => ApplyShell(false)));
        _top.Items.Add(new ToolStripButton("Parent koyu tema", null, (_,__) => ApplyShell(true)));
        _top.Items.Add(new ToolStripButton("Header menüsünü kullanıcı menüsüne ekle", null, (_,__) => _grid.AttachBuiltInHeaderMenuTo(_cms!, nameof(Row.Name))));
        _top.Items.Add(new ToolStripButton("Mode=Object", null, (_,__) => _grid.Mode = ViewGridDataMode.Object));
        _top.Items.Add(new ToolStripButton("Mode=Virtual", null, (_,__) => _grid.Mode = ViewGridDataMode.Virtual));
        _top.Items.Add(new ToolStripButton("Mode=Tree", null, (_,__) => _grid.Mode = ViewGridDataMode.Tree));
        _top.Items.Add(new ToolStripButton("Mode=Tile", null, (_,__) => _grid.Mode = ViewGridDataMode.Tile));

        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_top);
        ApplyShell(Program.AppTheme.IsDark);
    }

    private void ApplyShell(bool dark)
    {
        BackColor = dark ? Color.FromArgb(32, 32, 36) : Color.FromArgb(248, 249, 252);
        ForeColor = dark ? Color.White : Color.FromArgb(25, 25, 25);
        _info.BackColor = BackColor;
        _info.ForeColor = ForeColor;
        var theme = ViewGridTheme.FromParentColor(BackColor, ForeColor);
        _grid.ApplyTheme(theme);
        _grid.RefreshDesignTimePreview();
        if (_top != null)
        {
            _top.BackColor = theme.HeaderBackColor;
            _top.ForeColor = theme.HeaderForeColor;
            SmartMenuRenderer.ApplyTo(_top, theme);
        }
        if (_cms != null)
            SmartMenuRenderer.ApplyTo(_cms, theme);
    }

    private sealed record Row(int Id, string Name, string State, decimal Rate);
}

// v28.7+ visible smoke samples. Kept in TestApp so the examples are immediately reachable,
// not only as detached code snippets under samples/ViewGrid.FeatureSamples.
public sealed class PdfExportSuiteSampleForm : ViewGridSampleFormBase
{
    public PdfExportSuiteSampleForm() : base("v28.7 PDF Export Suite", "Details/Table ve Card/Dashboard PDF export örneği. Masaüstüne ViewGrid_Table_Export.pdf ve ViewGrid_Card_Export.pdf üretir.")
    {
        ViewGrid.Columns.Clear();
        ViewGrid.Columns.Add(new ViewGridColumn("Durum", nameof(DemoRow.State), 120) { CardRole = "Title", CardOrder = 0 });
        ViewGrid.Columns.Add(new ViewGridColumn("Makine", nameof(DemoRow.Machine), 150) { CardOrder = 1 });
        ViewGrid.Columns.Add(new ViewGridColumn("Meslek", nameof(DemoRow.Occupation), 140) { CardOrder = 2 });
        ViewGrid.Columns.Add(new ViewGridColumn("Açıklama", nameof(DemoRow.Description), 260) { CardOrder = 3, CardShowCaption = true });
        ViewGrid.Columns.Add(new ViewGridColumn("Puan", nameof(DemoRow.Rating), 80) { CardOrder = 4 });
        ViewGrid.SetObjects(CreateRows(60));
        ViewGrid.CardVisualInfoGetter = row =>
        {
            var r = (DemoRow)row;
            Color statusColor = r.State.Contains("Fail", StringComparison.OrdinalIgnoreCase) ? Color.Goldenrod : Color.MediumSeaGreen;
            return new ViewGridCardVisualInfo
            {
                AccentColor = statusColor,
                DotColor = statusColor,
                Badges =
                {
                    new ViewGridCardBadge { Text = r.State.Contains("Fail", StringComparison.OrdinalIgnoreCase) ? "FAIL" : "OK", BackColor = statusColor, ForeColor = Color.White }
                }
            };
        };
        Tool.Items.Add(new ToolStripSeparator());
        Tool.Items.Add(new ToolStripButton("Table PDF", null, (_,__) => ExportPdf(false)));
        Tool.Items.Add(new ToolStripButton("Card PDF", null, (_,__) => ExportPdf(true)));
        Tool.Items.Add(new ToolStripButton("Card görünümü", null, (_,__) => ViewGrid.SetViewMode(ViewGridMode.DashboardCard)));
    }

    private void ExportPdf(bool card)
    {
        string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), card ? "ViewGrid_Card_Export.pdf" : "ViewGrid_Table_Export.pdf");
        string actual = ViewGrid.ExportVisiblePdf(file, new global::ViewGrid.Exporting.ViewGridPdfExportOptions
        {
            Title = card ? "ViewGrid Card PDF Export" : "ViewGrid Table PDF Export",
            Subtitle = "v28.7 export smoke sample",
            Mode = card ? global::ViewGrid.Exporting.ViewGridPdfExportMode.Card : global::ViewGrid.Exporting.ViewGridPdfExportMode.Table,
            Orientation = card ? global::ViewGrid.Exporting.ViewGridPdfPageOrientation.Portrait : global::ViewGrid.Exporting.ViewGridPdfPageOrientation.Landscape,
            FitToPageWidth = true,
            ShowGridLines = true,
            ZebraRows = true,
            CardColumns = 2,
            CardMinHeight = 105
        });
        MessageBox.Show(this, "PDF oluşturuldu:\n" + actual, "ViewGrid PDF Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

public sealed class CellOverflowScrollSampleForm : ViewGridSampleFormBase
{
    public CellOverflowScrollSampleForm() : base("v28.8 Hücre İçi Scroll", "Uzun açıklama/not kolonlarında satır yüksekliği büyümeden hücre içinde mouse wheel ile kaydırma ve çift tık okuyucu popup test edilir.")
    {
        ViewGrid.Columns.Clear();
        ViewGrid.RowHeight = 62;
        ViewGrid.DetailsRowHeight = 62;
        ViewGrid.AllowMultilineCells = true;
        ViewGrid.AutoRowHeightForMultilineCells = false;
        ViewGrid.EnableCellOverflowScroll = true;
        ViewGrid.ShowCellOverflowScrollBars = true;
        ViewGrid.EnableCellOverflowDetailsPopup = true;
        ViewGrid.Columns.Add(new ViewGridColumn("Id", nameof(DemoRow.Id), 70));
        ViewGrid.Columns.Add(new ViewGridColumn("Makine", nameof(DemoRow.Machine), 150));
        ViewGrid.Columns.Add(new ViewGridColumn("Kısa Durum", nameof(DemoRow.State), 120));
        ViewGrid.Columns.Add(new ViewGridColumn("Mesaj / Açıklama", nameof(DemoRow.Description), 300)
        {
            WordWrap = true,
            AllowCellScroll = true,
            CellScrollMaxVisibleLines = 3,
            CellOverflowDetailsOnDoubleClick = true
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Teknisyen Notu", nameof(DemoRow.Notes), 280)
        {
            WordWrap = true,
            AllowCellScroll = true,
            CellScrollMaxVisibleLines = 3,
            CellOverflowDetailsOnDoubleClick = true
        });
        ViewGrid.Columns.Add(new ViewGridColumn("Zaman", nameof(DemoRow.Date), 140));
        var rows = CreateRows(80);
        foreach (var r in rows)
        {
            r.Description = "Bu hücredeki uzun içerik kolon genişliğine göre otomatik sarılır. Kullanıcı pencereyi büyütüp küçülttüğünde metin yeniden ölçülür; sığmayan kısım hücre içinde mouse wheel ile kaydırılır. Detay okumak için çift tık kullanılabilir.";
            r.Notes = "Program kontrol ediliyor, SAP/PCB bilgisi karşılaştırılıyor. Gerekirse operatör tekrar denemeden önce teknisyen sonucu bu not alanından takip eder. Uzun not satır yüksekliğini büyütmez.";
        }
        ViewGrid.SetObjects(rows);
    }
}
