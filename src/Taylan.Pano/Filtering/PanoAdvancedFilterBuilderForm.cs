using Taylan.Pano.Columns;
using Taylan.Pano.Theming;

namespace Taylan.Pano.Filtering;

public sealed class PanoAdvancedFilterBuilderForm : Form
{
    private readonly ComboBox _column = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 185 };
    private readonly ComboBox _mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 132 };
    private readonly TextBox _value1 = new() { Width = 190 };
    private readonly TextBox _value2 = new() { Width = 160 };
    private readonly ComboBox _logic = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 96 };
    private readonly TextBox _global = new() { Width = 240 };
    private readonly ListBox _list = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly Button _add = new() { Text = "Koşul ekle", Width = 96 };
    private readonly Button _remove = new() { Text = "Seçileni kaldır", Width = 110 };
    private readonly Button _clear = new() { Text = "Temizle", Width = 90 };
    private readonly Button _ok = new() { Text = "Uygula", DialogResult = DialogResult.OK, Width = 92 };
    private readonly Button _cancel = new() { Text = "İptal", DialogResult = DialogResult.Cancel, Width = 92 };
    private readonly List<PanoColumn> _columns;
    private readonly BindingSource _source = new();
    private readonly List<PanoColumnFilterPresetItem> _items = new();

    public PanoFilterPreset Preset { get; private set; }

    public PanoAdvancedFilterBuilderForm(IEnumerable<PanoColumn> columns, PanoFilterPreset initial, PanoTheme? theme)
    {
        _columns = columns.Where(x => x != null && !string.IsNullOrWhiteSpace(x.AspectName)).ToList();
        Preset = initial ?? new PanoFilterPreset();
        Text = "Gelişmiş Filtre";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        MinimumSize = new Size(780, 480);
        Size = new Size(980, 620);
        PanoDialogChrome.ConfigureStandardDialog(this, theme ?? WindowsThemeService.CurrentTheme(), new Size(780, 480), sizeable: true, iconKind: PanoDialogIconKind.Filter);

        BuildLayout();
        LoadInitial();
        ApplyTheme(theme);

        _add.Click += (_, _) => AddCondition();
        _remove.Click += (_, _) => RemoveSelected();
        _clear.Click += (_, _) => { _items.Clear(); RefreshList(); };
        _ok.Click += (_, _) => CommitPreset();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        top.Controls.Add(new Label { Text = "Mantık:", Width = 52, TextAlign = ContentAlignment.MiddleLeft, Height = 28 });
        top.Controls.Add(_logic);
        top.Controls.Add(new Label { Text = "Genel arama:", Width = 86, TextAlign = ContentAlignment.MiddleLeft, Height = 28, Margin = new Padding(18, 3, 3, 3) });
        top.Controls.Add(_global);

        var condition = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        condition.Controls.Add(_column);
        condition.Controls.Add(_mode);
        condition.Controls.Add(_value1);
        condition.Controls.Add(_value2);
        condition.Controls.Add(_add);
        condition.Controls.Add(_remove);
        condition.Controls.Add(_clear);

        _list.DisplayMember = nameof(PanoColumnFilterPresetItem.DisplayText);
        _source.DataSource = _items;
        _list.DataSource = _source;

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        bottom.Controls.Add(_cancel);
        bottom.Controls.Add(_ok);

        root.Controls.Add(top, 0, 0);
        root.Controls.Add(condition, 0, 1);
        root.Controls.Add(_list, 0, 2);
        root.Controls.Add(bottom, 0, 3);
        Controls.Add(root);
    }

    private void LoadInitial()
    {
        _column.DataSource = _columns.Select(c => new ColumnItem(c.Header, c.AspectName)).ToList();
        _column.DisplayMember = nameof(ColumnItem.Text);
        _column.ValueMember = nameof(ColumnItem.AspectName);
        _mode.DataSource = Enum.GetValues(typeof(PanoFilterMode));
        _logic.DataSource = Enum.GetValues(typeof(PanoFilterLogic));
        _logic.SelectedItem = Preset.Logic;
        _global.Text = Preset.GlobalText ?? string.Empty;
        _items.Clear();
        if (Preset.Filters != null)
            _items.AddRange(Preset.Filters.Select(x => new PanoColumnFilterPresetItem
            {
                AspectName = x.AspectName,
                Mode = x.Mode,
                Text = x.Text,
                Text2 = x.Text2,
                Enabled = x.Enabled,
                SelectedValues = x.SelectedValues == null ? null : new List<string>(x.SelectedValues)
            }));
        RefreshList();
    }

    private void AddCondition()
    {
        string aspect = (_column.SelectedItem as ColumnItem)?.AspectName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(aspect)) return;
        var mode = _mode.SelectedItem is PanoFilterMode m ? m : PanoFilterMode.Contains;
        _items.Add(new PanoColumnFilterPresetItem
        {
            AspectName = aspect,
            Mode = mode,
            Text = _value1.Text,
            Text2 = _value2.Text,
            Enabled = true
        });
        RefreshList();
    }

    private void RemoveSelected()
    {
        if (_list.SelectedItem is PanoColumnFilterPresetItem item)
        {
            _items.Remove(item);
            RefreshList();
        }
    }

    private void RefreshList()
    {
        _source.ResetBindings(false);
    }

    private void CommitPreset()
    {
        Preset = new PanoFilterPreset
        {
            Name = Preset.Name,
            Description = Preset.Description,
            Logic = _logic.SelectedItem is PanoFilterLogic l ? l : PanoFilterLogic.And,
            GlobalText = _global.Text ?? string.Empty,
            Filters = _items.Select(x => new PanoColumnFilterPresetItem
            {
                AspectName = x.AspectName,
                Mode = x.Mode,
                Text = x.Text,
                Text2 = x.Text2,
                Enabled = x.Enabled,
                SelectedValues = x.SelectedValues == null ? null : new List<string>(x.SelectedValues)
            }).ToList()
        };
    }

    private void ApplyTheme(PanoTheme? theme)
    {
        if (theme == null) return;
        PanoDialogChrome.ConfigureOpenPanoDialogs(this, theme);
    }

    private static IEnumerable<Control> AllControls(Control root)
    {
        foreach (Control c in root.Controls)
        {
            yield return c;
            foreach (var child in AllControls(c))
                yield return child;
        }
    }

    private sealed class ColumnItem
    {
        public ColumnItem(string text, string aspectName)
        {
            Text = string.IsNullOrWhiteSpace(text) ? aspectName : text;
            AspectName = aspectName;
        }
        public string Text { get; }
        public string AspectName { get; }
    }
}
