using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Summary;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed class PanoV279PopularEnterpriseFeaturesForm : Form
{
    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new() { Dock = DockStyle.Top, Height = 74, Padding = new Padding(12), TextAlign = ContentAlignment.MiddleLeft };
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        EmptyListMessage = "v27.9 popüler özellik örneği için kayıt yok",
        HeaderContextMenuBehavior = PanoHeaderContextMenuBehavior.Full,
        FilterMenuMode = Taylan.Pano.Filtering.PanoFilterMenuMode.Both,
        EnablePopularFeaturePack = true,
        PopularFeaturePreset = PanoPopularFeaturePreset.MasterData,
        StateKeyAspectName = nameof(EnterpriseRow.Id)
    };

    private readonly List<EnterpriseRow> _rows = new();
    private bool _dark = true;

    public PanoV279PopularEnterpriseFeaturesForm()
    {
        Text = "Pano v27.9 - Popular Enterprise Features";
        Width = 1380;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;

        BuildToolbar();
        ConfigureColumns();
        LoadRows();
        ApplyDemoEnhancements();
        ApplyTheme(true);

        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);
    }

    private void BuildToolbar()
    {
        _tool.Items.Add(new ToolStripButton("Balanced", null, (_, __) => ApplyPreset(PanoPopularFeaturePreset.Balanced)));
        _tool.Items.Add(new ToolStripButton("MasterData", null, (_, __) => ApplyPreset(PanoPopularFeaturePreset.MasterData)));
        _tool.Items.Add(new ToolStripButton("SupportDesk", null, (_, __) => ApplyPreset(PanoPopularFeaturePreset.SupportDesk)));
        _tool.Items.Add(new ToolStripButton("Large Data", null, (_, __) => ApplyPreset(PanoPopularFeaturePreset.LargeDataReview)));
        _tool.Items.Add(new ToolStripButton("Data Entry", null, (_, __) => ApplyPreset(PanoPopularFeaturePreset.DataEntry)));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Ctrl+F Panel", null, (_, __) => _grid.ShowModernSearchPanel()));
        _tool.Items.Add(new ToolStripButton("Kolon Seçici", null, (_, __) => _grid.ShowColumnChooser()));
        _tool.Items.Add(new ToolStripButton("Gelişmiş Filtre", null, (_, __) => _grid.ShowAdvancedFilterBuilder()));
        _tool.Items.Add(new ToolStripButton("Filtreleri Temizle", null, (_, __) => _grid.ClearFilters()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Duruma göre grupla", null, (_, __) => _grid.SetGroupBy(nameof(EnterpriseRow.Status))));
        _tool.Items.Add(new ToolStripButton("Makineye göre grupla", null, (_, __) => _grid.SetGroupBy(nameof(EnterpriseRow.Machine))));
        _tool.Items.Add(new ToolStripButton("Grupları temizle", null, (_, __) => _grid.ClearGrouping()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Tema", null, (_, __) => ApplyTheme(!_dark)));
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new PanoColumn("ID", nameof(EnterpriseRow.Id), 58));
        _grid.Columns.Add(new PanoColumn("Tip", nameof(EnterpriseRow.Type), 105) { Kind = PanoColumnKind.Badge, AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("Kod", nameof(EnterpriseRow.Code), 130));
        _grid.Columns.Add(new PanoColumn("Başlık / Malzeme", nameof(EnterpriseRow.Title), 280) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 3 });
        _grid.Columns.Add(new PanoColumn("Makine", nameof(EnterpriseRow.Machine), 130) { AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("Durum", nameof(EnterpriseRow.Status), 126) { Kind = PanoColumnKind.Badge, AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("Miktar", nameof(EnterpriseRow.Quantity), 90));
        _grid.Columns.Add(new PanoColumn("İlerleme", nameof(EnterpriseRow.Progress), 96) { Kind = PanoColumnKind.ProgressBar });
        _grid.Columns.Add(new PanoColumn("Etiketler", nameof(EnterpriseRow.Tags), 230) { Kind = PanoColumnKind.Tags });
        _grid.Columns.Add(new PanoColumn("Açıklama / Yol", nameof(EnterpriseRow.Description), 420) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 4 });
    }

    private void ApplyDemoEnhancements()
    {
        _grid.ApplyPopularFeaturePack(PanoPopularFeaturePreset.MasterData);
        _grid.AddSemanticStatusConditionalFormat(nameof(EnterpriseRow.Status));
        _grid.AddCountSummary(nameof(EnterpriseRow.Id));
        _grid.AddNumericSummary(nameof(EnterpriseRow.Quantity), PanoSummaryType.Sum, "Toplam {0}");
        _grid.AddNumericSummary(nameof(EnterpriseRow.Progress), PanoSummaryType.Average, "Ort. %{0:0}");
        _grid.SetObjects(_rows);
        _info.Text = "v27.9: popüler enterprise grid davranışları tek preset altında. Ctrl+F arama paneli, column chooser, advanced filter, summary footer, frozen column, conditional format ve kart filtre UX aynı ekranda denenebilir.";
    }

    private void ApplyPreset(PanoPopularFeaturePreset preset)
    {
        _grid.ApplyPopularFeaturePack(preset);
        _grid.SetObjects(_rows);
        _info.Text = $"Aktif preset: {preset}. Sağ tık menüsü, kolon seçici, filtre popup, özet/footer ve arama davranışını test edebilirsin.";
    }

    private void LoadRows()
    {
        _rows.Clear();
        string[] statuses = { "Open", "Waiting", "Done", "Critical Missing", "OK", "Error" };
        string[] types = { "BOM", "SAP", "Ticket", "Program", "Machine" };
        string[] machines = { "AOI-QX150", "FLEX-01", "LINE-03", "REWORK-02", "AXIAL" };

        for (int i = 1; i <= 160; i++)
        {
            _rows.Add(new EnterpriseRow
            {
                Id = i,
                Type = types[i % types.Length],
                Code = (i % 3 == 0 ? "MAT-" : i % 3 == 1 ? "TCK-" : "PRG-") + i.ToString("000000"),
                Title = i % 4 == 0
                    ? "kritik toleranslı komponent - AOI/MasterData uzun malzeme açıklaması"
                    : i % 4 == 1
                        ? "False call destek talebi ve operatör mesajı"
                        : "SAP BOM pozisyonu / dizgi programı kontrol kaydı",
                Machine = machines[i % machines.Length],
                Status = statuses[i % statuses.Length],
                Quantity = 1 + (i % 28),
                Progress = Math.Min(100, 8 + (i * 7) % 96),
                Tags = i % 2 == 0 ? "BOM;SAP;AOI" : "Program;Makine;Kontrol",
                Description = $@"\\cyberserver\D\Production\MasterData\ProgramArchive\Line-{i % 9}\Machine-{machines[i % machines.Length]}\VeryLongFolderName_For_FilterPopup_And_Tooltip_Test_{i:000}\program-{i:000}.txt"
            });
        }
    }

    private void ApplyTheme(bool dark)
    {
        _dark = dark;
        var theme = dark ? PanoTheme.DarkTheme() : PanoTheme.LightTheme();
        _grid.ApplyTheme(theme);
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        _info.BackColor = theme.PanelBackColor;
        _info.ForeColor = theme.ForeColor;
        _tool.BackColor = theme.HeaderBackColor;
        _tool.ForeColor = theme.HeaderForeColor;
    }

    private sealed class EnterpriseRow
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Machine { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Progress { get; set; }
        public string Tags { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

