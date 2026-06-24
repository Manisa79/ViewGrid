using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Filtering;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed class OLVExtrasSampleForm : Form
{
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = PanoDataMode.Object,
        ViewMode = PanoViewMode.Details,
        CheckBoxes = true,
        CheckedAspectName = nameof(OlvExtraRow.Selected),
        HeaderContextMenuBehavior = PanoHeaderContextMenuBehavior.Full,
        FilterMenuMode = PanoFilterMenuMode.Both,
        AllowMultilineCells = true,
        MaxCellTextLines = 4,
        AutoRowHeightForMultilineCells = true,
        EmptyListMessage = "OLV uyumluluk ekstra örneği için kayıt yok"
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Top,
        Height = 64,
        Padding = new Padding(12),
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "ObjectListView geçişinde sık lazım olan ek yardımcılar: seçim snapshot/restore, aspect value ile bul-seç-vurgula, kolon göster/gizle, checked nesneler ve çok satırlı not hücreleri."
    };

    public OLVExtrasSampleForm()
    {
        Text = "Pano OLV Ekstra Uyumluluk ve Faydalı API Örneği";
        Width = 1200;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;

        _grid.Columns.Add(new PanoColumn("Person", nameof(OlvExtraRow.Person), 180));
        _grid.Columns.Add(new PanoColumn("Occupation", nameof(OlvExtraRow.Occupation), 140));
        _grid.Columns.Add(new PanoColumn("Status", nameof(OlvExtraRow.Status), 120));
        _grid.Columns.Add(new PanoColumn("Key", nameof(OlvExtraRow.Key), 90));
        _grid.Columns.Add(new PanoColumn("Notes", nameof(OlvExtraRow.Notes), 460) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 4 });

        _tool.Items.Add(new ToolStripButton("Veriyi yükle", null, (_, __) => LoadData()));
        _tool.Items.Add(new ToolStripButton("Seçimi key ile koru + yenile", null, (_, __) => RefreshPreserveByKey()));
        _tool.Items.Add(new ToolStripButton("Key=K-012 bul", null, (_, __) => _grid.RevealObjectByAspect(nameof(OlvExtraRow.Key), "K-012")));
        _tool.Items.Add(new ToolStripButton("Occupation gizle/göster", null, (_, __) => _grid.ToggleColumn(nameof(OlvExtraRow.Occupation))));
        _tool.Items.Add(new ToolStripButton("Sadece temel kolonlar", null, (_, __) => _grid.HideAllColumnsExcept(nameof(OlvExtraRow.Person), nameof(OlvExtraRow.Status), nameof(OlvExtraRow.Notes))));
        _tool.Items.Add(new ToolStripButton("Tüm kolonlar", null, (_, __) => _grid.ShowAllColumns()));
        _tool.Items.Add(new ToolStripButton("Engineer seç", null, (_, __) => _grid.SelectObjectsByAspectValues(nameof(OlvExtraRow.Occupation), new object?[] { "Engineer" })));
        _tool.Items.Add(new ToolStripButton("Seçili check toggle", null, (_, __) => _grid.ToggleCheckSelectedObjects()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Açık tema", null, (_, __) => ApplyTheme(false)));
        _tool.Items.Add(new ToolStripButton("Koyu tema", null, (_, __) => ApplyTheme(true)));

        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);

        LoadData();
        ApplyTheme(Program.AppTheme.IsDark);
    }

    private void LoadData()
    {
        _grid.SetObjects(CreateRows());
    }

    private void RefreshPreserveByKey()
    {
        var rows = CreateRows().ToList();
        foreach (var row in rows.Where((_, i) => i % 5 == 0))
            row.Notes += " Yenileme sonrası aynı key ile seçim korunmalı.";

        _grid.UpdateObjectsPreserveSelection(rows, nameof(OlvExtraRow.Key));
    }

    private static IEnumerable<OlvExtraRow> CreateRows()
    {
        string[] jobs = { "Operator", "Engineer", "Quality", "Technician" };
        string[] statuses = { "Open", "Waiting", "Done", "ActionRequired" };
        for (int i = 1; i <= 80; i++)
        {
            yield return new OlvExtraRow
            {
                Selected = i % 6 == 0,
                Person = i <= 5 ? new[] { "Wilhelm Erat", "Alana Roderick", "Frank Price", "Eric", "Nicola Scotts" }[i - 1] : $"Person {i}",
                Occupation = jobs[i % jobs.Length],
                Status = statuses[i % statuses.Length],
                Key = $"K-{i:000}",
                Notes = "OLV geçişinde uzun notlar, açıklamalar veya hata mesajları bu hücrede kolon genişliğine göre düzgün şekilde sarılır. Seçim key ile korunur ve görünür kayıtlar aspect değerine göre bulunabilir."
            };
        }
    }

    private void ApplyTheme(bool dark)
    {
        BackColor = dark ? Color.FromArgb(28, 29, 33) : Color.FromArgb(246, 248, 252);
        ForeColor = dark ? Color.White : Color.FromArgb(28, 31, 36);
        _info.BackColor = BackColor;
        _info.ForeColor = ForeColor;
        var theme = PanoTheme.FromParentColor(BackColor, ForeColor);
        _grid.ApplyTheme(theme);
        SmartMenuRenderer.ApplyTo(_tool, theme);
    }

    private sealed class OlvExtraRow
    {
        public bool Selected { get; set; }
        public string Person { get; set; } = string.Empty;
        public string Occupation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}

