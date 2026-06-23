using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Filtering;
using ViewGrid.Rendering;
using ViewGrid.State;
using ViewGrid.Theming;
using ViewGrid.Virtualization;

namespace ViewGrid.TestApp;

public sealed class ViewGridV27ProductSampleForm : Form
{
    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full,
        FilterMenuMode = ViewGridFilterMenuMode.Both,
        EmptyListMessage = "v27 ürün seviyesi örneği için kayıt yok",
        StateKeyAspectName = nameof(V27Row.Id),
        PersistColumnFilters = true,
        PersistVisualPreferences = true
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Top,
        Height = 76,
        Padding = new Padding(12),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly List<V27Row> _rows = new();
    private string StateFile => Path.Combine(Application.StartupPath, "viewgrid-v27-product-state.json");

    public ViewGridV27ProductSampleForm()
    {
        Text = "ViewGrid v27 - Product Core / MasterData Hazırlığı";
        Width = 1320;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;

        ConfigureColumns();
        ConfigureToolbar();

        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);

        ApplyTheme(Program.AppTheme.IsDark);
        LoadMasterDataRows();
        _grid.ApplyScenario(ViewGridScenario.BomPositions);
        _grid.SetObjects(_rows);
        _info.Text = "v27: State Engine + View Scenario + Cell Visual Profile + Range Virtual Provider. MasterData/BOM/SAP ekranlarında kullanıcı layout, filtre, görünüm ve seçimlerini tek state dosyasında saklamak için hazırlandı.";
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Add(new ViewGridColumn("Seç", nameof(V27Row.Selected), 52) { Kind = ViewGridColumnKind.CheckBox, CellCheckBox = true, HeaderCheckBox = true });
        _grid.Columns.Add(new ViewGridColumn("Id", nameof(V27Row.Id), 70));
        _grid.Columns.Add(new ViewGridColumn("Tip", nameof(V27Row.Type), 105).ApplyVisualProfile(ViewGridCellVisualProfile.Badge));
        _grid.Columns.Add(new ViewGridColumn("Kod", nameof(V27Row.Code), 150));
        _grid.Columns.Add(new ViewGridColumn("Açıklama", nameof(V27Row.Name), 280) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 3 });
        _grid.Columns.Add(new ViewGridColumn("Makine", nameof(V27Row.Machine), 110));
        _grid.Columns.Add(new ViewGridColumn("RefDes", nameof(V27Row.RefDes), 130));
        _grid.Columns.Add(new ViewGridColumn("İlerleme", nameof(V27Row.Progress), 95).ApplyVisualProfile(ViewGridCellVisualProfile.Progress));
        _grid.Columns.Add(new ViewGridColumn("Durum", nameof(V27Row.Status), 115).ApplyVisualProfile(ViewGridCellVisualProfile.WarningStatus));
        _grid.Columns.Add(new ViewGridColumn("Not", nameof(V27Row.Note), 360) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 4 });
    }

    private void ConfigureToolbar()
    {
        _tool.Items.Add(new ToolStripButton("MasterData BOM", null, (_, __) => ApplyScenario(ViewGridScenario.BomPositions)));
        _tool.Items.Add(new ToolStripButton("Ticket Dashboard", null, (_, __) => ApplyScenario(ViewGridScenario.TicketBoard)));
        _tool.Items.Add(new ToolStripButton("Timeline", null, (_, __) => ApplyScenario(ViewGridScenario.Timeline)));
        _tool.Items.Add(new ToolStripButton("Range Virtual", null, (_, __) => LoadRangeVirtualDemo()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("State Kaydet", null, (_, __) => SaveState()));
        _tool.Items.Add(new ToolStripButton("State Yükle", null, (_, __) => LoadState()));
        _tool.Items.Add(new ToolStripButton("Filtre Temizle", null, (_, __) => _grid.ClearFilters()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Koyu", null, (_, __) => ApplyTheme(true)));
        _tool.Items.Add(new ToolStripButton("Açık", null, (_, __) => ApplyTheme(false)));
    }

    private void ApplyScenario(ViewGridScenario scenario)
    {
        _grid.SetObjects(_rows);
        _grid.ClearGrouping();
        _grid.ApplyScenario(scenario);

        if (scenario == ViewGridScenario.TicketBoard)
            _grid.SetGroupBy(nameof(V27Row.Status));
        else if (scenario == ViewGridScenario.Timeline)
            _grid.SetGroupBy(nameof(V27Row.Type));

        _info.Text = $"Aktif senaryo: {scenario.GetDisplayName()} | StateKeyAspectName={_grid.StateKeyAspectName}. Filtre, sıralama, kolon, görünüm ve seçim bilgisi v27 state ile tek dosyada saklanabilir.";
    }

    private void LoadMasterDataRows()
    {
        _rows.Clear();
        string[] status = { "OK", "Eksik", "Kontrol", "Uyarı", "Hazır" };
        string[] machines = { "SIPLACE", "AXIAL", "RADIAL", "OIB", "KnS", "QX250i" };
        for (int i = 1; i <= 180; i++)
        {
            string type = i % 11 == 0 ? "Yarı Mamul" : i % 7 == 0 ? "Program" : "BOM";
            _rows.Add(new V27Row
            {
                Id = i,
                Selected = i % 13 == 0,
                Type = type,
                Code = $"17MB{i % 88 + 100:000}FRR{i % 9}",
                Name = i % 8 == 0 ? "Stoklaşmış yarı mamul / alt ürün" : "SMD komponent ve üretim malzemesi",
                Machine = machines[i % machines.Length],
                RefDes = $"R{i},C{i + 2},U{i % 24 + 1}",
                Progress = i % 101,
                Status = status[i % status.Length],
                Note = "MasterData için BOM, SAP ürün ağacı, program dosyası ve pozisyon kontrolü aynı ViewGrid üzerinde denenir."
            });
        }
    }

    private void LoadRangeVirtualDemo()
    {
        const int total = 1_000_000;
        var provider = new ViewGridRangeVirtualProvider(
            () => total,
            async (start, count, token) =>
            {
                await Task.Delay(20, token).ConfigureAwait(false);
                return Enumerable.Range(start, count)
                    .Select(i => (object?)new V27Row
                    {
                        Id = i + 1,
                        Type = i % 9 == 0 ? "SAP" : "Virtual",
                        Code = $"VR-{i + 1:0000000}",
                        Name = "Range provider satırı",
                        Machine = i % 2 == 0 ? "Flex" : "QX250i",
                        RefDes = $"P{i % 1000}",
                        Progress = i % 100,
                        Status = i % 17 == 0 ? "Uyarı" : "OK",
                        Note = "Bu kayıt sayfa bazlı sanal provider tarafından gerektiğinde üretilir."
                    })
                    .ToList();
            },
            pageSize: 300);

        _grid.SetRangeVirtualProvider(provider);
        _grid.ApplyScenario(ViewGridScenario.DenseData);
        _info.Text = "Range Virtual Provider aktif: 1.000.000 satır belleğe alınmadan sayfa bazlı cache ile gösterilir. SQL/SAP/API için başlangıç altyapısıdır.";
    }

    private void SaveState()
    {
        _grid.SaveState(StateFile, "v27-product-core");
        _info.Text = "State kaydedildi: " + StateFile;
    }

    private void LoadState()
    {
        bool ok = _grid.LoadState(StateFile);
        _info.Text = ok ? "State yüklendi: filtre/görünüm/kolon/seçim geri alındı." : "Henüz kayıtlı state yok.";
    }

    private void ApplyTheme(bool dark)
    {
        BackColor = dark ? Color.FromArgb(28, 29, 33) : Color.FromArgb(246, 248, 252);
        ForeColor = dark ? Color.White : Color.FromArgb(28, 31, 36);
        _info.BackColor = BackColor;
        _info.ForeColor = ForeColor;
        var theme = ViewGridTheme.FromParentColor(BackColor, ForeColor);
        _grid.ApplyTheme(theme);
        SmartMenuRenderer.ApplyTo(_tool, theme);
    }

    private sealed class V27Row
    {
        public bool Selected { get; set; }
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Machine { get; set; } = string.Empty;
        public string RefDes { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }
}
