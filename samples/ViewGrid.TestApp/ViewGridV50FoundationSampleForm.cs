using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Theming;

namespace ViewGrid.TestApp;

public sealed class ViewGridV50FoundationSampleForm : Form
{
    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = ViewGridDataMode.Object,
        ViewMode = ViewGridMode.GroupCard,
        TilePosterMode = true,
        RowHeight = 172,
        TilePreferredWidth = 360,
        TilePreferredHeight = 172,
        ShowQuickFilterBar = true,
        ShowActiveFilterChips = true
    };

    private readonly ToolStrip _tool = new() { GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Bottom,
        Height = 54,
        Padding = new Padding(12, 6, 12, 6),
        TextAlign = ContentAlignment.MiddleLeft
    };

    public ViewGridV50FoundationSampleForm()
    {
        Text = "ViewGrid 5.0 Foundation / Stability";
        Width = 1180;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;

        _grid.ApplyThemePreset(ViewGridThemePreset.Dark);
        _grid.ApplyViewGrid5FoundationDefaults();
        ConfigureColumns();
        LoadRows();
        ConfigureToolbar();

        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);
        UpdateInfo("ViewGrid 5.0 Foundation: modül profilleri, runtime stability checks, tema erişilebilirliği ve Audix/AOI/Factory hazır profilleri.");
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new ViewGridColumn("Faz", nameof(FoundationRow.Phase), 90) { Kind = ViewGridColumnKind.Badge });
        _grid.Columns.Add(new ViewGridColumn("Alan", nameof(FoundationRow.Area), 180));
        _grid.Columns.Add(new ViewGridColumn("Özellik", nameof(FoundationRow.Feature), 240) { FillFreeSpace = true });
        _grid.Columns.Add(new ViewGridColumn("Durum", nameof(FoundationRow.Status), 120) { Kind = ViewGridColumnKind.Badge });
        _grid.Columns.Add(new ViewGridColumn("Kullanım", nameof(FoundationRow.Usage), 320));
    }

    private void ConfigureToolbar()
    {
        _tool.Items.Add(new ToolStripButton("5.0 Varsayılan", null, (_, __) => { _grid.ApplyViewGrid5FoundationDefaults(); UpdateInfo("Tüm ViewGrid 5.0 güvenli varsayılanları açıldı."); }));
        _tool.Items.Add(new ToolStripButton("Audix Profili", null, (_, __) => { _grid.ApplyAudixMediaProfile(); _grid.SetViewMode(ViewGridMode.Poster); UpdateInfo("Audix: Poster/Gallery/FilmStrip, cover cache, playback state ve video preview altyapısı aktif."); }));
        _tool.Items.Add(new ToolStripButton("AOI Profili", null, (_, __) => { _grid.ApplyAoiSupportDeskProfile(); _grid.SetViewMode(ViewGridMode.Kanban); UpdateInfo("AOI: Kanban/Timeline/Dashboard/Interaction profili aktif."); }));
        _tool.Items.Add(new ToolStripButton("Factory Profili", null, (_, __) => { _grid.ApplyFactoryIntelligenceProfile(); _grid.SetViewMode(ViewGridMode.HeatMap); UpdateInfo("Factory Intelligence: HeatMap/KPI/Timeline/Analytics profili aktif."); }));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Stability Check", null, (_, __) => ShowChecks()));
        _tool.Items.Add(new ToolStripButton("Dark", null, (_, __) => _grid.ApplyThemePreset(ViewGridThemePreset.Dark)));
        _tool.Items.Add(new ToolStripButton("Light", null, (_, __) => _grid.ApplyThemePreset(ViewGridThemePreset.Light)));
    }

    private void LoadRows()
    {
        _grid.SetObjects(new List<FoundationRow>
        {
            new("5.0", "Stability", "Duplicate/API guard yaklaşımı", "Hazır", "Yeni fazlardan sonra çakışma riskini azaltmak için guard dokümanı ve runtime checks."),
            new("5.0", "Modules", "Audix / AOI / Factory / MasterData profilleri", "Hazır", "Tek tek property aramak yerine ilgili uygulama profilini tek metodla uygula."),
            new("5.0", "Theme", "Accessibility-first tema güvenliği", "Hazır", "Koyu/açık tema okunurluğu için EnforceThemeAccessibility + ThemeStudio guard."),
            new("5.0", "Media", "Audio/video playback state güvenli varsayılan", "Hazır", "Play tuşu çalışırken kart üzerinde Pause, Şimdi çalıyor ve equalizer state'i görünür."),
            new("5.0", "Layout", "Enterprise layout + command/search", "Hazır", "Kullanıcı görünümü, command palette ve search everywhere varsayılan profilde aktif."),
            new("5.0", "Example Center", "Nerede bulurum akışına 5.0 ekleme", "Hazır", "Kalabalık örnek merkezinde yeni ana mimariyi tek ekranda bulma."),
            new("Sonraki", "ViewGrid 5.x", "Gerçek modül paketlerine ayırma", "Plan", "ViewGrid.Core, ViewGrid.Media, ViewGrid.Analytics, ViewGrid.Dashboard gibi ayrı paketlere kademeli geçiş.")
        });
    }

    private void ShowChecks()
    {
        var checks = _grid.RunViewGrid5RuntimeChecks();
        string msg = string.Join(Environment.NewLine, checks.Select(c => (c.Passed ? "✓ " : "! ") + c.CheckName + " - " + c.Message));
        MessageBox.Show(this, msg, "ViewGrid 5.0 Runtime Stability Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateInfo(string text)
    {
        _info.Text = text;
    }

    private sealed record FoundationRow(string Phase, string Area, string Feature, string Status, string Usage);
}
