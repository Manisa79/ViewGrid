using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed class PanoV50FoundationSampleForm : Form
{
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = PanoDataMode.Object,
        ViewMode = PanoViewMode.GroupCard,
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

    public PanoV50FoundationSampleForm()
    {
        Text = "Pano 5.0 Foundation / Stability";
        Width = 1180;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;

        _grid.ApplyThemePreset(PanoThemePreset.Dark);
        _grid.ApplyPano5FoundationDefaults();
        ConfigureColumns();
        LoadRows();
        ConfigureToolbar();

        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);
        UpdateInfo("Pano 5.0 Foundation: modül profilleri, runtime stability checks, tema erişilebilirliği ve Audix/AOI/Factory hazır profilleri.");
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new PanoColumn("Faz", nameof(FoundationRow.Phase), 90) { Kind = PanoColumnKind.Badge });
        _grid.Columns.Add(new PanoColumn("Alan", nameof(FoundationRow.Area), 180));
        _grid.Columns.Add(new PanoColumn("Özellik", nameof(FoundationRow.Feature), 240) { FillFreeSpace = true });
        _grid.Columns.Add(new PanoColumn("Durum", nameof(FoundationRow.Status), 120) { Kind = PanoColumnKind.Badge });
        _grid.Columns.Add(new PanoColumn("Kullanım", nameof(FoundationRow.Usage), 320));
    }

    private void ConfigureToolbar()
    {
        _tool.Items.Add(new ToolStripButton("5.0 Varsayılan", null, (_, __) => { _grid.ApplyPano5FoundationDefaults(); UpdateInfo("Tüm Pano 5.0 güvenli varsayılanları açıldı."); }));
        _tool.Items.Add(new ToolStripButton("Audix Profili", null, (_, __) => { _grid.ApplyAudixMediaProfile(); _grid.SetViewMode(PanoViewMode.Poster); UpdateInfo("Audix: Poster/Gallery/FilmStrip, cover cache, playback state ve video preview altyapısı aktif."); }));
        _tool.Items.Add(new ToolStripButton("AOI Profili", null, (_, __) => { _grid.ApplyAoiSupportDeskProfile(); _grid.SetViewMode(PanoViewMode.Kanban); UpdateInfo("AOI: Kanban/Timeline/Dashboard/Interaction profili aktif."); }));
        _tool.Items.Add(new ToolStripButton("Factory Profili", null, (_, __) => { _grid.ApplyFactoryIntelligenceProfile(); _grid.SetViewMode(PanoViewMode.HeatMap); UpdateInfo("Factory Intelligence: HeatMap/KPI/Timeline/Analytics profili aktif."); }));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Stability Check", null, (_, __) => ShowChecks()));
        _tool.Items.Add(new ToolStripButton("Dark", null, (_, __) => _grid.ApplyThemePreset(PanoThemePreset.Dark)));
        _tool.Items.Add(new ToolStripButton("Light", null, (_, __) => _grid.ApplyThemePreset(PanoThemePreset.Light)));
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
            new("Sonraki", "Pano 5.x", "Gerçek modül paketlerine ayırma", "Plan", "Taylan.Pano.Core, Pano.Media, Pano.Analytics, Pano.Dashboard gibi ayrı paketlere kademeli geçiş.")
        });
    }

    private void ShowChecks()
    {
        var checks = _grid.RunPano5RuntimeChecks();
        string msg = string.Join(Environment.NewLine, checks.Select(c => (c.Passed ? "✓ " : "! ") + c.CheckName + " - " + c.Message));
        MessageBox.Show(this, msg, "Pano 5.0 Runtime Stability Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateInfo(string text)
    {
        _info.Text = text;
    }

    private sealed record FoundationRow(string Phase, string Area, string Feature, string Status, string Usage);
}

