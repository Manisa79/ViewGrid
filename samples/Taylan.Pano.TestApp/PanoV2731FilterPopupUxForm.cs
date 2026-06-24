using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Filtering;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed class PanoV2731FilterPopupUxForm : Form
{
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = PanoDataMode.Object,
        ViewMode = PanoViewMode.Details,
        HeaderContextMenuBehavior = PanoHeaderContextMenuBehavior.Full,
        FilterMenuMode = PanoFilterMenuMode.Both,
        EmptyListMessage = "v27.3.1 filtre popup UX örneği için kayıt yok",
        ShowGridLines = true,
        AlternateRows = true,
        RowHeight = 34,
        FilterPopupResizable = true,
        FilterPopupRememberSize = true,
        FilterPopupShowValueTooltips = true,
        FilterPopupAutoWidthForLongValues = true,
        FilterPopupDefaultSize = new Size(560, 540),
        FilterPopupMinimumSize = new Size(340, 380),
        FilterPopupMaximumSize = new Size(980, 760)
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Top,
        Height = 86,
        Padding = new Padding(14, 8, 14, 8),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private bool _dark = true;

    public PanoV2731FilterPopupUxForm()
    {
        Text = "Pano v27.3.1 Filter Popup UX";
        Width = 1280;
        Height = 760;
        MinimumSize = new Size(980, 620);
        StartPosition = FormStartPosition.CenterParent;

        BuildToolbar();
        ConfigureColumns();
        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);

        ApplyTheme(Program.AppTheme.IsDark);
        LoadRows();
    }

    private void BuildToolbar()
    {
        _tool.Items.Add(new ToolStripButton("Malzeme filtresini aç", null, (_, __) => _grid.ShowFilterMenuForAspect(nameof(FilterPopupRow.MaterialName))));
        _tool.Items.Add(new ToolStripButton("Program yolu filtresini aç", null, (_, __) => _grid.ShowFilterMenuForAspect(nameof(FilterPopupRow.ProgramPath))));
        _tool.Items.Add(new ToolStripButton("Tooltip aç/kapat", null, (_, __) =>
        {
            _grid.FilterPopupShowValueTooltips = !_grid.FilterPopupShowValueTooltips;
            UpdateInfo();
        }));
        _tool.Items.Add(new ToolStripButton("Auto width aç/kapat", null, (_, __) =>
        {
            _grid.FilterPopupAutoWidthForLongValues = !_grid.FilterPopupAutoWidthForLongValues;
            UpdateInfo();
        }));
        _tool.Items.Add(new ToolStripButton("Resize aç/kapat", null, (_, __) =>
        {
            _grid.FilterPopupResizable = !_grid.FilterPopupResizable;
            UpdateInfo();
        }));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Açık/Koyu", null, (_, __) => ApplyTheme(!_dark)));
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new PanoColumn("Id", nameof(FilterPopupRow.Id), 64) { TextAlign = ContentAlignment.MiddleRight });
        _grid.Columns.Add(new PanoColumn("RefDes", nameof(FilterPopupRow.RefDes), 90));
        _grid.Columns.Add(new PanoColumn("Malzeme Kodu", nameof(FilterPopupRow.MaterialCode), 150));
        _grid.Columns.Add(new PanoColumn("Uzun Malzeme Adı", nameof(FilterPopupRow.MaterialName), 420) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 2 });
        _grid.Columns.Add(new PanoColumn("Makine", nameof(FilterPopupRow.Machine), 110) { AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("Program / Klasör Yolu", nameof(FilterPopupRow.ProgramPath), 520) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 2 });
        _grid.Columns.Add(new PanoColumn("Durum", nameof(FilterPopupRow.Status), 110) { Kind = PanoColumnKind.Badge, AllowGroup = true });
    }

    private void LoadRows()
    {
        string[] machines = { "Flex", "FlexUltra", "QX150i", "QX250i", "Axial", "Radial", "THT" };
        string[] statuses = { "OK", "Warning", "Eksik", "Hazır", "Waiting" };
        var rows = new List<FilterPopupRow>();
        for (int i = 1; i <= 160; i++)
        {
            string family = i % 4 == 0 ? "stoklaşmış yarımamül / semi finished" : i % 3 == 0 ? "kritik toleranslı komponent" : "standart komponent";
            rows.Add(new FilterPopupRow
            {
                Id = i,
                RefDes = (i % 2 == 0 ? "R" : "C") + i.ToString("000"),
                MaterialCode = "17MB" + (190 + i % 40) + "FRR1-" + (24000000 + i).ToString(),
                MaterialName = $"{family} - AOI/MasterData SAP BOM pozisyon açıklaması çok uzun değer örneği {i:000} / üretim hattında filtre popup içinde tam okunması gereken açıklama",
                Machine = machines[i % machines.Length],
                ProgramPath = $@"\\cyberserver\D\{machines[i % machines.Length]}\CIMBWork\1902\17MB190FRR1_24022836\Top\M-AX5-20CPR-Aser-{i % 12}\Program_Output_With_Long_Folder_Name_{i:000}",
                Status = statuses[i % statuses.Length]
            });
        }

        _grid.SetObjects(rows);
    }

    private void ApplyTheme(bool dark)
    {
        _dark = dark;
        Color back = dark ? Color.FromArgb(22, 24, 29) : Color.White;
        Color text = dark ? Color.WhiteSmoke : Color.FromArgb(28, 32, 38);
        BackColor = back;
        ForeColor = text;
        _info.BackColor = dark ? Color.FromArgb(32, 36, 44) : Color.FromArgb(245, 247, 250);
        _info.ForeColor = text;
        PanoTheme theme = PanoTheme.FromParentColor(back, text);
        _grid.ApplyTheme(theme);
        SmartMenuRenderer.ApplyTo(_tool, theme);
        UpdateInfo();
    }

    private void UpdateInfo()
    {
        _info.Text = "v27.3.1 Filter Popup UX — uzun SAP malzeme adı, program yolu ve açıklama filtrelerinde popup kenardan büyütülebilir. " +
                     "Satıra sığmayan filtre değerlerinde tooltip gösterilir, uzun değerlere göre popup ilk açılışta otomatik genişleyebilir ve kolon bazlı son boyut hatırlanır." + Environment.NewLine +
                     $"Resizable={_grid.FilterPopupResizable}, RememberSize={_grid.FilterPopupRememberSize}, Tooltips={_grid.FilterPopupShowValueTooltips}, AutoWidth={_grid.FilterPopupAutoWidthForLongValues}";
    }

    private sealed class FilterPopupRow
    {
        public int Id { get; set; }
        public string RefDes { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Machine { get; set; } = string.Empty;
        public string ProgramPath { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

