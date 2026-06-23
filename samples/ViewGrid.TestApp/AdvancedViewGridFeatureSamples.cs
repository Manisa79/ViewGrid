using System.Data;
using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Filtering;
using ViewGrid.Details;
using ViewGrid.Formatting;
using ViewGrid.Presets;

namespace ViewGrid.TestApp;

public sealed class ExcelFilterRowSampleForm : Form
{
    private readonly ViewGridControl _grid = new() { Dock = DockStyle.Fill, FullRowSelect = true, ShowGridLines = true, FastFilterMenuForHugeLists = true, AsyncLoadFullFilterValues = true };
    private ViewGridExcelFilterRowPanel? _filterRow;

    public ExcelFilterRowSampleForm()
    {
        Text = "Excel Filtre Satırı";
        Width = 1180;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Program.AppTheme.BackColor;
        ForeColor = Program.AppTheme.ForeColor;
        _grid.ApplyTheme(Program.AppTheme);

        _grid.Columns.Add(new ViewGridColumn("Barkod", "Barcode", 160) { FillFreeSpace = true });
        _grid.Columns.Add(new ViewGridColumn("Makine", "Machine", 120));
        _grid.Columns.Add(new ViewGridColumn("Sonuç", "Result", 90) { Kind = ViewGridColumnKind.Badge });
        _grid.Columns.Add(new ViewGridColumn("Aktif", "Active", 70) { Kind = ViewGridColumnKind.CheckBox });
        _grid.Columns.Add(new ViewGridColumn("Puan", "Score", 80) { Kind = ViewGridColumnKind.Numeric, TextAlign = ContentAlignment.MiddleRight });
        _grid.Columns.Add(new ViewGridColumn("Tarih", "TestDate", 150) { Kind = ViewGridColumnKind.Date });
        _grid.SetObjects(CreateRows(2000));

        _filterRow = new ViewGridExcelFilterRowPanel(_grid);
        var tool = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
        tool.Items.Add(new ToolStripButton("Filtre satırını yenile", null, (_,__) => _filterRow?.BuildEditors()));
        tool.Items.Add(new ToolStripButton("Temizle", null, (_,__) => _filterRow?.ClearFilterText()));
        tool.Items.Add(new ToolStripLabel("Kolon altındaki kutulara yazarak anlık filtrele."));

        Controls.Add(_grid);
        Controls.Add(_filterRow);
        Controls.Add(tool);
    }

    private static List<FeatureRow> CreateRows(int count)
    {
        string[] machines = { "QX250i", "QX500", "Flex", "Flex Ultra" };
        string[] results = { "PASS", "FAIL", "WAIT", "REPAIR" };
        var list = new List<FeatureRow>(count);
        for (int i = 1; i <= count; i++)
            list.Add(new FeatureRow(i, "BC" + i.ToString("000000"), machines[i % machines.Length], results[i % results.Length], i % 3 != 0, i % 101, DateTime.Today.AddMinutes(-i)));
        return list;
    }
}

public sealed class MasterDetailSampleForm : Form
{
    private readonly ViewGridControl _master = new() { Dock = DockStyle.Fill, FullRowSelect = true, ShowGridLines = true };
    private readonly ViewGridControl _detail = new() { Dock = DockStyle.Fill, FullRowSelect = true, ShowGridLines = true, EmptyListMessage = "Üstten kayıt seçin" };
    private readonly List<TestResultRow> _masters;
    private readonly List<FailDetailRow> _details;

    public MasterDetailSampleForm()
    {
        Text = "Master-detail ViewGrid";
        Width = 1280;
        Height = 760;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Program.AppTheme.BackColor;
        ForeColor = Program.AppTheme.ForeColor;
        _master.ApplyTheme(Program.AppTheme);
        _detail.ApplyTheme(Program.AppTheme);

        ViewGridPresets.ApplyAoiFailList(_master);
        _master.Columns.Clear();
        _master.Columns.Add(new ViewGridColumn("Id", "Id", 70));
        _master.Columns.Add(new ViewGridColumn("Barkod", "Barcode", 150) { FillFreeSpace = true });
        _master.Columns.Add(new ViewGridColumn("Makine", "Machine", 120));
        _master.Columns.Add(new ViewGridColumn("Program", "AssemblyName", 220) { FillFreeSpace = true });
        _master.Columns.Add(new ViewGridColumn("Sonuç", "Result", 90) { Kind = ViewGridColumnKind.Badge });
        _master.Columns.Add(new ViewGridColumn("Tarih", "TestDate", 150));
        _master.FrozenColumnCount = 2;

        _detail.Columns.Add(new ViewGridColumn("FailId", "Id", 70));
        _detail.Columns.Add(new ViewGridColumn("Layer", "Layer", 80));
        _detail.Columns.Add(new ViewGridColumn("Board", "Board", 80));
        _detail.Columns.Add(new ViewGridColumn("RefDes", "RefDes", 120));
        _detail.Columns.Add(new ViewGridColumn("Feature", "Feature", 160) { FillFreeSpace = true });
        _detail.Columns.Add(new ViewGridColumn("PassRate", "PassRate", 100) { Kind = ViewGridColumnKind.ProgressBar });
        _detail.Columns.Add(new ViewGridColumn("Decision", "Decision", 120) { Kind = ViewGridColumnKind.ComboBox, Editable = true, ComboBoxItems = new List<string>{"Bekliyor", "FalseCall", "Gerçek Hata"} });

        _masters = CreateMasters(120);
        _details = CreateDetails(_masters);
        _master.SetObjects(_masters);
        _master.CellClick += (_, e) => LoadDetails(e.RowObject as TestResultRow);
        _master.ItemActivate += (_, e) => LoadDetails(e.RowObject as TestResultRow);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 330 };
        split.Panel1.Controls.Add(_master);
        split.Panel2.Controls.Add(_detail);
        Controls.Add(split);
        LoadDetails(_masters.FirstOrDefault());
    }

    private void LoadDetails(TestResultRow? row)
    {
        if (row == null) { _detail.ClearObjects(); return; }
        _detail.SetObjects(_details.Where(x => x.TestResultId == row.Id).ToList());
    }

    private static List<TestResultRow> CreateMasters(int count)
    {
        string[] machines = { "QX250i", "QX500", "Flex", "Flex Ultra" };
        var list = new List<TestResultRow>();
        for (int i = 1; i <= count; i++)
            list.Add(new TestResultRow(i, "BC" + i.ToString("000000"), machines[i % machines.Length], "20CMB20R4B-ALT+" + (23640000 + i), i % 7 == 0 ? "FAIL" : "PASS", DateTime.Today.AddMinutes(-i)));
        return list;
    }

    private static List<FailDetailRow> CreateDetails(IEnumerable<TestResultRow> masters)
    {
        var list = new List<FailDetailRow>();
        int id = 1;
        foreach (var m in masters)
        {
            int count = m.Result == "FAIL" ? 5 : 2;
            for (int i = 0; i < count; i++)
                list.Add(new FailDetailRow(id++, m.Id, i % 2 == 0 ? "Top" : "Bottom", i + 1, "R" + (100 + i), i % 2 == 0 ? "Solder" : "Polarity", (i * 17 + m.Id) % 100, i % 3 == 0 ? "Bekliyor" : "FalseCall"));
        }
        return list;
    }
}

public sealed class FrozenCommandDetailsSampleForm : Form
{
    private readonly ViewGridControl _grid = new() { Dock = DockStyle.Fill, FullRowSelect = true, ShowGridLines = true, EnableRowDetails = true };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 32, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 12, 0) };

    public FrozenCommandDetailsSampleForm()
    {
        Text = "Frozen + Command + Row Details";
        Width = 1180;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;
        _grid.ApplyTheme(Program.AppTheme);
        BackColor = Program.AppTheme.BackColor;
        ForeColor = Program.AppTheme.ForeColor;

        _grid.Columns.Add(new ViewGridColumn("Aç", "Open", 70) { Kind = ViewGridColumnKind.Button, AspectGetter = _ => "Aç" });
        _grid.Columns.Add(new ViewGridColumn("Barkod", "Barcode", 160) { FillFreeSpace = true });
        _grid.Columns.Add(new ViewGridColumn("Makine", "Machine", 120));
        _grid.Columns.Add(new ViewGridColumn("Sonuç", "Result", 90) { Kind = ViewGridColumnKind.Badge });
        _grid.Columns.Add(new ViewGridColumn("Skor", "Score", 100) { Kind = ViewGridColumnKind.ProgressBar });
        _grid.Columns.Add(new ViewGridColumn("Not", "Note", 240) { FillFreeSpace = true });
        _grid.FrozenColumnCount = 2;
        _grid.SetObjects(CreateRows(500));
        _grid.ButtonClick += (_, e) => _status.Text = $"Komut çalıştı: {((FeatureRow)e.RowObject).Barcode}";
        _grid.SetRowDetailsProvider(new ViewGridRowDetailsProvider
        {
            CreateDetailsControl = row =>
            {
                var r = (FeatureRow)row;
                return new Label
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(14),
                    Text = $"Detay: {r.Barcode} | {r.Machine} | {r.Result} | Score={r.Score}\nBuraya AOI resim paneli, metrik kartları veya log akışı eklenebilir.",
                    BackColor = Color.FromArgb(245, 247, 252)
                };
            }
        });

        var tool = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
        tool.Items.Add(new ToolStripButton("Frozen 0", null, (_,__) => { _grid.FrozenColumnCount = 0; _grid.RefreshView(); }));
        tool.Items.Add(new ToolStripButton("Frozen 2", null, (_,__) => { _grid.FrozenColumnCount = 2; _grid.RefreshView(); }));
        tool.Items.Add(new ToolStripButton("Seçili detay", null, (_,__) => _grid.ToggleRowDetails(_grid.SelectedIndex)));
        Controls.Add(_grid);
        Controls.Add(_status);
        Controls.Add(tool);
    }

    private static List<FeatureRow> CreateRows(int count)
    {
        string[] machines = { "QX250i", "QX500", "Flex", "Flex Ultra" };
        string[] results = { "PASS", "FAIL", "WAIT", "REPAIR" };
        var list = new List<FeatureRow>(count);
        for (int i = 1; i <= count; i++)
            list.Add(new FeatureRow(i, "BC" + i.ToString("000000"), machines[i % machines.Length], results[i % results.Length], i % 3 != 0, i % 101, DateTime.Today.AddMinutes(-i), "Satır komutları + detay paneli demo"));
        return list;
    }
}

public sealed class ConditionalFormattingSampleForm : Form
{
    private readonly ViewGridControl _grid = new() { Dock = DockStyle.Fill, FullRowSelect = true, ShowGridLines = true };

    public ConditionalFormattingSampleForm()
    {
        Text = "Conditional Formatting";
        Width = 1180;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Program.AppTheme.BackColor;
        ForeColor = Program.AppTheme.ForeColor;
        _grid.ApplyTheme(Program.AppTheme);

        _grid.Columns.Add(new ViewGridColumn("Barkod", "Barcode", 160) { FillFreeSpace = true });
        _grid.Columns.Add(new ViewGridColumn("Makine", "Machine", 120));
        _grid.Columns.Add(new ViewGridColumn("Sonuç", "Result", 90) { Kind = ViewGridColumnKind.Badge });
        _grid.Columns.Add(new ViewGridColumn("Aktif", "Active", 70) { Kind = ViewGridColumnKind.CheckBox });
        _grid.Columns.Add(new ViewGridColumn("Skor", "Score", 110) { Kind = ViewGridColumnKind.ProgressBar });
        _grid.Columns.Add(new ViewGridColumn("Tarih", "TestDate", 150));
        _grid.RowBackColorGetter = row => ((FeatureRow)row).Result == "FAIL" ? Color.FromArgb(255, 242, 242) : null;
        _grid.RowForeColorGetter = row => ((FeatureRow)row).Result == "FAIL" ? Color.FromArgb(130, 20, 20) : null;
        _grid.ConditionalFormats.Add(new ViewGridConditionalFormat
        {
            Predicate = (_, col, value) => col.AspectName == "Score" && Convert.ToInt32(value) < 35,
            BackColor = Color.FromArgb(255, 235, 210),
            ForeColor = Color.FromArgb(150, 80, 0)
        });
        _grid.SetObjects(CreateRows(1500));

        var tool = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
        tool.Items.Add(new ToolStripButton("Sadece FAIL", null, (_,__) => _grid.SetColumnFilter(new ViewGridColumnFilter { AspectName = "Result", Mode = ViewGridFilterMode.Equals, Text = "FAIL" })));
        tool.Items.Add(new ToolStripButton("Temizle", null, (_,__) => _grid.ClearFilters()));
        tool.Items.Add(new ToolStripLabel("FAIL satırları ve düşük skor hücreleri kural bazlı renklendirilir."));
        Controls.Add(_grid);
        Controls.Add(tool);
    }

    private static List<FeatureRow> CreateRows(int count)
    {
        string[] machines = { "QX250i", "QX500", "Flex", "Flex Ultra" };
        string[] results = { "PASS", "FAIL", "WAIT", "REPAIR" };
        var list = new List<FeatureRow>(count);
        for (int i = 1; i <= count; i++)
            list.Add(new FeatureRow(i, "BC" + i.ToString("000000"), machines[i % machines.Length], results[i % results.Length], i % 3 != 0, i % 101, DateTime.Today.AddMinutes(-i)));
        return list;
    }
}

public sealed record FeatureRow(int Id, string Barcode, string Machine, string Result, bool Active, int Score, DateTime TestDate, string Note = "");
public sealed record TestResultRow(int Id, string Barcode, string Machine, string AssemblyName, string Result, DateTime TestDate);
public sealed record FailDetailRow(int Id, int TestResultId, string Layer, int Board, string RefDes, string Feature, int PassRate, string Decision);
