using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Filtering;

namespace ViewGrid.TestApp;

public sealed class ViewGridCompatSampleForm : Form
{
    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        EmptyListMessage = "SetObjects ile yüklenecek kayıt yok",
        FullRowSelect = true,
        MultiSelect = true,
        ShowGridLines = true,
        AlternateRows = true,
        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full,
        FilterMenuMode = ViewGridFilterMenuMode.Both,
        EnableModernEmptyState = true
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Bottom,
        Height = 48,
        Padding = new Padding(10),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private List<CompatRow> _rows = new();

    public ViewGridCompatSampleForm()
    {
        Text = "ViewGrid/GLV uyumluluk örnekleri - SetObjects / events / seçim / checked / sort";
        Width = 1100;
        Height = 680;
        StartPosition = FormStartPosition.CenterScreen;

        ConfigureColumns();
        ConfigureToolbar();
        ConfigureFormatting();

        Controls.Add(_grid);
        Controls.Add(_tool);
        Controls.Add(_info);

        LoadRows(150);
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Add(new ViewGridColumn("Seç", nameof(CompatRow.Checked), 54) { Kind = ViewGridColumnKind.CheckBox, Editable = true });
        _grid.Columns.Add(new ViewGridColumn("Id", nameof(CompatRow.Id), 70) { TextAlign = ContentAlignment.MiddleRight });
        _grid.Columns.Add(new ViewGridColumn("Kod", nameof(CompatRow.Code), 130));
        _grid.Columns.Add(new ViewGridColumn("Ad", nameof(CompatRow.Name), 220) { FillFreeSpace = true });
        _grid.Columns.Add(new ViewGridColumn("Durum", nameof(CompatRow.State), 110));
        _grid.Columns.Add(new ViewGridColumn("Oran", nameof(CompatRow.Rate), 90) { TextAlign = ContentAlignment.MiddleRight });
    }

    private void ConfigureToolbar()
    {
        _tool.Items.Add(new ToolStripButton("SetObjects 150", null, (_,__) => LoadRows(150)));
        _tool.Items.Add(new ToolStripButton("SetObjects preserve", null, (_,__) => ReloadPreserveSelection()));
        _tool.Items.Add(new ToolStripButton("AddObject", null, (_,__) => AddOne()));
        _tool.Items.Add(new ToolStripButton("AddObjects +5", null, (_,__) => AddMany()));
        _tool.Items.Add(new ToolStripButton("Remove Selected", null, (_,__) => RemoveSelected()));
        _tool.Items.Add(new ToolStripButton("Check Selected", null, (_,__) => _grid.SetObjectsChecked(_grid.SelectedObjects, true)));
        _tool.Items.Add(new ToolStripButton("CheckedObjects", null, (_,__) => ShowCheckedCount()));
        _tool.Items.Add(new ToolStripButton("ItemChecked event", null, (_,__) => ToggleFirstSelected()));
        _tool.Items.Add(new ToolStripButton("SelectIndex(10)", null, (_,__) => { _grid.SelectIndex(10); UpdateInfo("SelectIndex(10) çalıştı"); }));
        _tool.Items.Add(new ToolStripButton("InvertSelection", null, (_,__) => { _grid.InvertSelection(); UpdateInfo("InvertSelection çalıştı"); }));
        _tool.Items.Add(new ToolStripButton("Replace selected", null, (_,__) => ReplaceSelected()));
        _tool.Items.Add(new ToolStripButton("Move selected top", null, (_,__) => MoveSelectedTop()));
        _tool.Items.Add(new ToolStripButton("Sort State desc", null, (_,__) => { _grid.Sort(nameof(CompatRow.State), SortOrder.Descending); UpdateInfo("Sort(aspectName, Descending) çalıştı"); }));
        _tool.Items.Add(new ToolStripButton("AutoResize", null, (_,__) => _grid.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)));
        _tool.Items.Add(new ToolStripButton("Filter popup", null, (_,__) => _grid.ShowFilterMenuForAspect(nameof(CompatRow.State))));
        _tool.Items.Add(new ToolStripButton("ClearObjects", null, (_,__) => { _rows.Clear(); _grid.ClearObjects(); UpdateInfo("ClearObjects ile liste temizlendi"); }));
    }

    private void ConfigureFormatting()
    {
        _grid.FormatRow += (_, e) =>
        {
            if (e.RowObject is CompatRow row && row.State == "Fail")
            {
                e.Item.BackColor = Color.FromArgb(255, 238, 238);
                e.Item.ForeColor = Color.FromArgb(150, 20, 20);
            }
        };

        _grid.FormatCell += (_, e) =>
        {
            if (e.RowObject is CompatRow row && e.Column.AspectName == nameof(CompatRow.Rate) && row.Rate >= 90)
                e.SubItem.ForeColor = Color.FromArgb(0, 120, 60);
        };

        _grid.ItemChecked += (_, e) => UpdateInfo($"ItemChecked: {((CompatRow)e.Model).Code} = {e.Checked}");
        _grid.ObjectsChanged += (_, e) => UpdateInfo($"ObjectsChanged: ObjectCount={e.ObjectCount:N0}, Visible={e.VisibleCount:N0}");
    }

    private void LoadRows(int count)
    {
        _rows = Enumerable.Range(1, count).Select(CreateRow).ToList();
        _grid.SetObjects(_rows);
        UpdateInfo("grid.SetObjects(_rows) ile yüklendi");
    }

    private void ReloadPreserveSelection()
    {
        _rows = Enumerable.Range(1, 180).Select(CreateRow).ToList();
        _grid.SetObjects(_rows, preserveSelection: true, preserveScroll: true);
        UpdateInfo("grid.SetObjects(rows, preserveSelection:true, preserveScroll:true) çalıştı");
    }

    private void AddOne()
    {
        var row = CreateRow(_rows.Count + 1);
        _rows.Add(row);
        _grid.AddObject(row);
        _grid.EnsureObjectVisible(row);
        UpdateInfo("grid.AddObject(row) çalıştı");
    }

    private void AddMany()
    {
        var start = _rows.Count + 1;
        var added = Enumerable.Range(start, 5).Select(CreateRow).ToArray();
        _rows.AddRange(added);
        _grid.AddObjects(added);
        _grid.SelectObject(added.Last());
        UpdateInfo("grid.AddObjects(row1, row2...) çalıştı");
    }

    private void RemoveSelected()
    {
        var selected = _grid.SelectedObjectsAs<CompatRow>();
        if (selected.Count == 0) return;
        _grid.RemoveObjects(selected.Cast<object>().ToArray());
        _rows.RemoveAll(x => selected.Contains(x));
        UpdateInfo($"{selected.Count} seçili kayıt RemoveObjects ile silindi");
    }

    private void ShowCheckedCount()
    {
        var checkedRows = _grid.CheckedObjectsAs<CompatRow>();
        UpdateInfo($"CheckedObjectsAs<CompatRow>() = {checkedRows.Count} kayıt | CheckedObject={(_grid.CheckedObject is CompatRow r ? r.Code : "-")}");
    }

    private void ToggleFirstSelected()
    {
        var selected = _grid.SelectedObjectAs<CompatRow>();
        if (selected == null)
        {
            _grid.SelectIndex(0);
            selected = _grid.SelectedObjectAs<CompatRow>();
        }
        if (selected == null) return;
        _grid.ToggleCheckObject(selected);
    }

    private void ReplaceSelected()
    {
        var selected = _grid.SelectedObjectAs<CompatRow>();
        if (selected == null) return;
        var replacement = new CompatRow
        {
            Id = selected.Id,
            Checked = selected.Checked,
            Code = selected.Code + "-R",
            Name = selected.Name + " (değişti)",
            State = "Review",
            Rate = selected.Rate
        };
        int index = _rows.IndexOf(selected);
        if (index >= 0) _rows[index] = replacement;
        _grid.ReplaceObject(selected, replacement);
        UpdateInfo("ReplaceObject selected çalıştı");
    }

    private void MoveSelectedTop()
    {
        var selected = _grid.SelectedObjectAs<CompatRow>();
        if (selected == null) return;
        _rows.Remove(selected);
        _rows.Insert(0, selected);
        _grid.MoveObject(selected, 0);
        UpdateInfo("MoveObject selected -> 0 çalıştı");
    }

    private void UpdateInfo(string action)
    {
        _info.Text = $"{action} | ObjectCount={_grid.ObjectCount:N0} | FilteredCount={_grid.FilteredCount:N0} | Selected={_grid.SelectedObjects.Count:N0} | Checked={_grid.CheckedObjects.Count:N0}";
    }

    private static CompatRow CreateRow(int i) => new()
    {
        Id = i,
        Checked = i % 11 == 0,
        Code = "GLV-" + i.ToString("0000"),
        Name = "ViewGrid uyum satırı " + i,
        State = i % 9 == 0 ? "Fail" : i % 4 == 0 ? "Review" : "OK",
        Rate = Math.Round(55 + (i * 7 % 45) + (i % 10) / 10m, 1)
    };

    private sealed class CompatRow
    {
        public int Id { get; set; }
        public bool Checked { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }
}
