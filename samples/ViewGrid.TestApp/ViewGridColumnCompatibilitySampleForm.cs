using ViewGrid.Columns;
using ViewGrid.Core;

namespace ViewGrid.TestApp;

public sealed class ViewGridColumnCompatibilitySampleForm : Form
{
    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        EmptyListMessage = "ViewGrid kolon uyumluluk örneği boş",
        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full,
        ShowColumnFilterButtons = true,
        AllowEditAllCells = true,
        CheckBoxes = true,
        CheckedAspectName = nameof(CompatRow.Checked),
        FullRowSelect = true,
        ShowGridLines = true,
        AlternateRows = true
    };

    private readonly Panel _topPanel = new()
    {
        Dock = DockStyle.Top,
        Height = 58,
        Padding = new Padding(8)
    };

    private readonly Label _info = new()
    {
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        Text = "ViewGrid uyumlu checkbox görünümü: CheckBoxes=True yeni kolon eklemez; checkbox ve header checkbox ilk görünür veri kolonunun içinde çalışır. Header checkbox tüm görünen satırları işaretler/kaldırır."
    };

    private readonly ToolStrip _tool = new()
    {
        Dock = DockStyle.Top,
        GripStyle = ToolStripGripStyle.Hidden
    };

    private readonly PropertyGrid _propertyGrid = new()
    {
        Dock = DockStyle.Right,
        Width = 360,
        HelpVisible = true,
        ToolbarVisible = true,
        PropertySort = PropertySort.Categorized,
        Visible = false
    };

    public ViewGridColumnCompatibilitySampleForm()
    {
        Text = "ViewGrid Uyumlu Kolon / Checkbox Örneği";
        Width = 1180;
        Height = 720;
        MinimumSize = new Size(900, 560);

        BuildToolbar();
        ConfigureColumns();
        LoadRows();

        _propertyGrid.SelectedObject = _grid.Columns.FirstOrDefault();
        _propertyGrid.PropertyValueChanged += (_, __) => _grid.RefreshView();

        _topPanel.Controls.Add(_info);
        Controls.Add(_grid);
        Controls.Add(_propertyGrid);
        Controls.Add(_topPanel);
        Controls.Add(_tool);
    }

    private void BuildToolbar()
    {
        _tool.Items.Add(new ToolStripButton("Header checkbox aç/kapat", null, (_, __) => ToggleHeaderCheckBox()));
        _tool.Items.Add(new ToolStripButton("Notes Visible değiştir", null, (_, __) => ToggleNotesVisible()));
        _tool.Items.Add(new ToolStripButton("Kolon PropertyGrid", null, (_, __) => TogglePropertyGrid()));
        _tool.Items.Add(new ToolStripButton("ButtonSizing değiştir", null, (_, __) => CycleButtonSizing()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Light", null, (_, __) => ApplyTheme(ViewGrid.Theming.ViewGridTheme.LightTheme())));
        _tool.Items.Add(new ToolStripButton("Dark", null, (_, __) => ApplyTheme(ViewGrid.Theming.ViewGridTheme.DarkTheme())));
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Clear();

        _grid.Columns.Add(new ViewGridColumn("Person", nameof(CompatRow.Person), 220)
        {
            HeaderCheckBox = true,
            HeaderCheckBoxUpdatesRowCheckBoxes = true,
            AutoCompleteEditor = true,
            AutoCompleteEditorMode = AutoCompleteMode.Append,
            Searchable = true,
            UseFiltering = true,
            Groupable = true,
            ToolTipText = "Checkbox bu kolonun içinde çizilir; ayrı checkbox kolonu yoktur."
        });

        _grid.Columns.Add(new ViewGridColumn("Occupation", nameof(CompatRow.Occupation), 150)
        {
            HeaderToolTipText = "Filtrelenebilir meslek kolonu",
            GroupWithItemCountFormat = "{0} ({1} kayıt)",
            GroupWithItemCountSingularFormat = "{0} ({1} kayıt)",
            UseInitialLetterForGroup = true
        });

        _grid.Columns.Add(new ViewGridColumn("Action", nameof(CompatRow.Action), 110)
        {
            Kind = ViewGridColumnKind.Button,
            ButtonSizing = ViewGridColumnButtonSizing.TextBounds,
            ButtonMaxWidth = 90,
            ButtonPadding = new Padding(2),
            EnableButtonWhenItemIsDisabled = true
        });

        _grid.Columns.Add(new ViewGridColumn("Link", nameof(CompatRow.Url), 170)
        {
            Hyperlink = true,
            HeaderTextAlign = ContentAlignment.MiddleCenter
        });

        _grid.Columns.Add(new ViewGridColumn("Notes", nameof(CompatRow.Notes), 260)
        {
            WordWrap = true,
            CellPadding = new Padding(4, 2, 4, 2),
            CellEditUseWholeCell = true,
            FillsFreeSpace = true
        });

        _grid.Columns.Add(new ViewGridColumn("Runtime Hidden", nameof(CompatRow.HiddenValue), 120)
        {
            Visible = false,
            DefaultVisible = false,
            ToolTipText = "Bu kolon designer/runtime Visible=false davranışını test eder."
        });
    }

    private void LoadRows()
    {
        var rows = Enumerable.Range(1, 100).Select(i => new CompatRow
        {
            Checked = i % 3 == 0,
            Person = i switch
            {
                1 => "Wilhelm Frat",
                2 => "Alana Roderick",
                3 => "Frank Price",
                4 => "Eric",
                5 => "Nicola Scotts",
                _ => "Person " + i
            },
            Occupation = (i % 4) switch
            {
                0 => "Technician",
                1 => "Operator",
                2 => "Engineer",
                _ => "Quality"
            },
            Action = "Aç",
            Url = "Detay " + i,
            Notes = "ViewGrid kolon property uyumluluğu test satırı " + i,
            HiddenValue = "Runtime'da görünmemeli " + i
        }).ToList();

        _grid.SetObjects(rows);
    }

    private void ToggleHeaderCheckBox()
    {
        var column = _grid.Columns.VisibleColumns.FirstOrDefault();
        if (column == null) return;

        column.HeaderCheckBox = !column.HeaderCheckBox;
        _grid.RefreshView();
    }

    private void ToggleNotesVisible()
    {
        var column = _grid.Columns.FirstOrDefault(c => c.AspectName == nameof(CompatRow.Notes));
        if (column == null) return;

        column.Visible = !column.Visible;
        _grid.RefreshView();
    }

    private void TogglePropertyGrid()
    {
        _propertyGrid.Visible = !_propertyGrid.Visible;
        _propertyGrid.SelectedObject = _grid.Columns.VisibleColumns.FirstOrDefault() ?? _grid.Columns.FirstOrDefault();
    }

    private void CycleButtonSizing()
    {
        var column = _grid.Columns.FirstOrDefault(c => c.AspectName == nameof(CompatRow.Action));
        if (column == null) return;

        column.ButtonSizing = column.ButtonSizing switch
        {
            ViewGridColumnButtonSizing.TextBounds => ViewGridColumnButtonSizing.CellBounds,
            ViewGridColumnButtonSizing.CellBounds => ViewGridColumnButtonSizing.FixedBounds,
            _ => ViewGridColumnButtonSizing.TextBounds
        };

        _grid.RefreshView();
    }

    private void ApplyTheme(ViewGrid.Theming.ViewGridTheme theme)
    {
        _grid.ApplyTheme(theme);
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        _topPanel.BackColor = theme.ControlBackColor;
        _topPanel.ForeColor = theme.ForeColor;
        _info.BackColor = theme.ControlBackColor;
        _info.ForeColor = theme.ForeColor;
        _propertyGrid.BackColor = theme.ControlBackColor;
        _propertyGrid.ForeColor = theme.ForeColor;
    }

    private sealed class CompatRow
    {
        public bool Checked { get; set; }
        public string Person { get; set; } = string.Empty;
        public string Occupation { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string HiddenValue { get; set; } = string.Empty;
    }
}
