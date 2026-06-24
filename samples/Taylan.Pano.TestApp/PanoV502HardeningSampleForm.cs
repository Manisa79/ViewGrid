using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed class PanoV502HardeningSampleForm : Form
{
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = PanoDataMode.Object,
        ViewMode = PanoViewMode.RowPreview,
        RowHeight = 96,
        ShowQuickFilterBar = true,
        ShowActiveFilterChips = true
    };

    private readonly ToolStrip _tool = new() { GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Bottom,
        Height = 56,
        Padding = new Padding(12, 6, 12, 6),
        TextAlign = ContentAlignment.MiddleLeft
    };

    public PanoV502HardeningSampleForm()
    {
        Text = "Pano v50.2 Build & Runtime Hardening";
        Width = 1220;
        Height = 740;
        MinimumSize = new Size(980, 620);
        StartPosition = FormStartPosition.CenterScreen;

        _grid.ApplyThemeStudioPreset(PanoThemeStudioPreset.FactoryOsDark);
        _grid.ApplyPano502HardeningDefaults();
        ConfigureColumns();
        ConfigureToolbar();
        LoadRows();

        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);
        UpdateInfo("v50.2: yeni özellik değil; projeye tak-çalıştır güvenliği, tema okunurluğu, medya runtime ayarları ve API guard akışı.");
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new PanoColumn("Alan", nameof(HardeningRow.Area), 140) { Kind = PanoColumnKind.Badge });
        _grid.Columns.Add(new PanoColumn("Kontrol", nameof(HardeningRow.Check), 220));
        _grid.Columns.Add(new PanoColumn("Durum", nameof(HardeningRow.Status), 110) { Kind = PanoColumnKind.Badge });
        _grid.Columns.Add(new PanoColumn("Açıklama", nameof(HardeningRow.Description), 420) { FillFreeSpace = true });
        _grid.Columns.Add(new PanoColumn("Audix/AOI etkisi", nameof(HardeningRow.ProjectImpact), 300));
    }

    private void ConfigureToolbar()
    {
        _tool.Items.Add(new ToolStripButton("v50.2 Varsayılan", null, (_, __) =>
        {
            _grid.ApplyPano502HardeningDefaults();
            LoadRows();
            UpdateInfo("Build/runtime hardening varsayılanları yeniden uygulandı.");
        }));
        _tool.Items.Add(new ToolStripButton("Audix Medya", null, (_, __) =>
        {
            _grid.ApplyAudix502MediaDefaults();
            _grid.SetViewMode(PanoViewMode.Poster);
            UpdateInfo("Audix için Poster + cover cache + playback state + video preview güvenli ayarları uygulandı.");
        }));
        _tool.Items.Add(new ToolStripButton("Runtime Check", null, (_, __) => ShowChecks()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Dark", null, (_, __) => _grid.ApplyThemeStudioPreset(PanoThemeStudioPreset.FactoryOsDark)));
        _tool.Items.Add(new ToolStripButton("Light", null, (_, __) => _grid.ApplyThemeStudioPreset(PanoThemeStudioPreset.FactoryOsLight)));
        _tool.Items.Add(new ToolStripButton("High Contrast", null, (_, __) => _grid.ApplyThemeStudioPreset(PanoThemeStudioPreset.HighContrastDark)));
    }

    private void LoadRows()
    {
        _grid.SetObjects(new List<HardeningRow>
        {
            new("Build", "API Guard", "Aktif", "Duplicate property/event, eksik namespace ve örnek merkez referans risklerini statik tarama ile görünür yapar.", "Yeni faz eklerken PanoColumnKind / EnableCommandPalette benzeri hataları daha erken yakalar."),
            new("Theme", "Readability Guard", "Aktif", "Light/Dark/High Contrast temalarda label, buton, combo ve kart metinlerinin okunurluğunu zorlar.", "Audix koyu temada kapak kartları ve AOI ticket kartları daha net görünür."),
            new("Media", "Cache + Lazy Loading", "Aktif", "Albüm kapağı/video thumbnail gibi görseller için cache ve lazy loading birlikte açılır.", "Audix büyük arşivlerde daha akıcı çalışır."),
            new("Media", "Playback State", "Aktif", "Play tuşuna basıldığında kart üstünde pause, şimdi çalıyor rozeti ve equalizer göstergesi görünür.", "Şarkı/video çalıyor mu sorusu kart üzerinde netleşir."),
            new("Interaction", "Search + Command", "Aktif", "Search Everywhere ve Command Palette temel profil içinde açık gelir.", "Örnek merkez ve gerçek projelerde özellik bulmak kolaylaşır."),
            new("Layout", "Enterprise Layout", "Aktif", "Kolon, filtre, görünüm ve layout profili için güvenli temel ayarlar açık gelir.", "Kullanıcı görünümü kaybolmadan proje bazlı deneyim sağlanır."),
            new("Performance", "Smoke Data Plan", "Hazır", "10k/50k satır ve medya cache senaryoları için test planı dokümana eklendi.", "Audix ve Factory Navigator büyüdükçe darboğazlar daha kolay izlenir.")
        });
    }

    private void ShowChecks()
    {
        var lines = _grid.RunPano502RuntimeHardeningChecks()
            .Select(c => (c.Passed ? "✓ " : "! ") + c.Area + " / " + c.Check + " - " + c.Message);
        MessageBox.Show(this, string.Join(Environment.NewLine, lines), "Pano v50.2 Runtime Hardening Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateInfo(string text) => _info.Text = text;

    private sealed record HardeningRow(string Area, string Check, string Status, string Description, string ProjectImpact);
}

