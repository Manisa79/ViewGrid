using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Theming;

namespace ViewGrid.Layout;

public sealed class ViewGridCardLayoutDesignerForm : Form
{
    private readonly ListBox _fields = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly ComboBox _role = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
    private readonly CheckBox _visible = new() { Text = "Görünür", Checked = true, Dock = DockStyle.Top };
    private readonly CheckBox _caption = new() { Text = "Caption göster", Dock = DockStyle.Top };
    private readonly NumericUpDown _maxLines = new() { Minimum = 1, Maximum = 8, Value = 1, Dock = DockStyle.Top };
    private readonly ComboBox _density = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
    private readonly Button _up = new() { Text = "Yukarı", Dock = DockStyle.Top, Height = 28 };
    private readonly Button _down = new() { Text = "Aşağı", Dock = DockStyle.Top, Height = 28 };
    private readonly Button _ok = new() { Text = "Uygula", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 32 };
    private readonly Button _cancel = new() { Text = "Vazgeç", DialogResult = DialogResult.Cancel, Dock = DockStyle.Bottom, Height = 32 };
    private readonly ViewGridCardLayoutDefinition _definition;
    private bool _syncing;

    public ViewGridCardLayoutDefinition Result => _definition;

    public ViewGridCardLayoutDesignerForm(IEnumerable<ViewGridColumn> columns, ViewGridCardLayoutDefinition? current = null, ViewGridTheme? theme = null)
    {
        _definition = current ?? ViewGridCardLayoutDefinition.FromColumns(columns);
        Text = "ViewGrid Card Layout Designer";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(760, 520);
        MinimumSize = new Size(620, 420);
        Font = new Font("Segoe UI", 9F);
        ViewGridDialogChrome.ConfigureStandardDialog(this, theme ?? WindowsThemeService.CurrentTheme(), new Size(620, 420), sizeable: true, iconKind: ViewGridDialogIconKind.Designer);
        AcceptButton = _ok;
        CancelButton = _cancel;

        _role.Items.AddRange(Enum.GetNames(typeof(ViewGridCardFieldRole)));
        _density.Items.AddRange(Enum.GetNames(typeof(ViewGridCardLayoutDensity)));
        _density.SelectedItem = _definition.Density.ToString();

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(10) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        var left = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
        var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
        left.Controls.Add(_fields);
        right.Controls.Add(_cancel);
        right.Controls.Add(_ok);
        right.Controls.Add(new Label { Text = "Yoğunluk", Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.BottomLeft });
        right.Controls.Add(_density);
        right.Controls.Add(new Label { Height = 10, Dock = DockStyle.Top });
        right.Controls.Add(_down);
        right.Controls.Add(_up);
        right.Controls.Add(new Label { Text = "Max satır", Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.BottomLeft });
        right.Controls.Add(_maxLines);
        right.Controls.Add(_caption);
        right.Controls.Add(_visible);
        right.Controls.Add(new Label { Text = "Rol", Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.BottomLeft });
        right.Controls.Add(_role);
        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);
        Controls.Add(root);

        _fields.SelectedIndexChanged += (_, _) => LoadSelectedField();
        _role.SelectedIndexChanged += (_, _) => SaveSelectedField();
        _visible.CheckedChanged += (_, _) => SaveSelectedField();
        _caption.CheckedChanged += (_, _) => SaveSelectedField();
        _maxLines.ValueChanged += (_, _) => SaveSelectedField();
        _density.SelectedIndexChanged += (_, _) => { if (Enum.TryParse<ViewGridCardLayoutDensity>(Convert.ToString(_density.SelectedItem), out var d)) _definition.Density = d; };
        _up.Click += (_, _) => MoveSelected(-1);
        _down.Click += (_, _) => MoveSelected(1);

        ApplyTheme(theme ?? ViewGridTheme.DarkTheme());
        Refill();
        if (_fields.Items.Count > 0) _fields.SelectedIndex = 0;
    }

    private void ApplyTheme(ViewGridTheme theme)
    {
        ViewGridDialogChrome.ConfigureOpenViewGridDialogs(this, theme);
    }

    private void Refill()
    {
        _fields.Items.Clear();
        foreach (var field in _definition.Fields.OrderBy(x => x.Order)) _fields.Items.Add(field);
    }

    private ViewGridCardLayoutField? SelectedField => _fields.SelectedItem as ViewGridCardLayoutField;

    private void LoadSelectedField()
    {
        var field = SelectedField;
        _syncing = true;
        try
        {
            _role.SelectedItem = field?.Role.ToString() ?? ViewGridCardFieldRole.Body.ToString();
            _visible.Checked = field?.Visible ?? false;
            _caption.Checked = field?.ShowCaption ?? false;
            _maxLines.Value = Math.Max(_maxLines.Minimum, Math.Min(_maxLines.Maximum, field?.MaxLines ?? 1));
        }
        finally { _syncing = false; }
    }

    private void SaveSelectedField()
    {
        if (_syncing) return;
        var field = SelectedField;
        if (field == null) return;
        if (Enum.TryParse<ViewGridCardFieldRole>(Convert.ToString(_role.SelectedItem), out var role)) field.Role = role;
        field.Visible = _visible.Checked;
        field.ShowCaption = _caption.Checked;
        field.MaxLines = (int)_maxLines.Value;
        int index = _fields.SelectedIndex;
        Refill();
        if (index >= 0 && index < _fields.Items.Count) _fields.SelectedIndex = index;
    }

    private void MoveSelected(int delta)
    {
        int index = _fields.SelectedIndex;
        int target = index + delta;
        if (index < 0 || target < 0 || target >= _fields.Items.Count) return;
        var ordered = _definition.Fields.OrderBy(x => x.Order).ToList();
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (int i = 0; i < ordered.Count; i++) ordered[i].Order = i;
        Refill();
        _fields.SelectedIndex = target;
    }
}

internal static class CardLayoutDesignerControlExtensions
{
    public static IEnumerable<Control> Flatten(this Control.ControlCollection controls)
    {
        foreach (Control c in controls)
        {
            yield return c;
            foreach (Control child in c.Controls.Flatten()) yield return child;
        }
    }
}
