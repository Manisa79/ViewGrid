using ViewGrid.Columns;
namespace ViewGrid.Editing;
public interface ICellEditor
{
    Control CreateEditor(Control owner, Rectangle bounds, ViewGridColumn column, object row, object? value);
    object? GetEditedValue(Control editor);
}
public sealed class TextBoxCellEditor : ICellEditor
{
    public Control CreateEditor(Control owner, Rectangle bounds, ViewGridColumn column, object row, object? value)
    {
        var tb = new TextBox { BorderStyle = BorderStyle.FixedSingle, Text = Convert.ToString(value) ?? string.Empty };
        tb.Bounds = bounds; owner.Controls.Add(tb); tb.Focus(); tb.SelectAll(); return tb;
    }
    public object? GetEditedValue(Control editor) => ((TextBox)editor).Text;
}

public sealed class ComboBoxCellEditor : ICellEditor
{
    private readonly IEnumerable<string>? _items;
    private readonly bool _dropDownList;

    public ComboBoxCellEditor(IEnumerable<string>? items = null, bool dropDownList = true)
    {
        _items = items;
        _dropDownList = dropDownList;
    }

    public Control CreateEditor(Control owner, Rectangle bounds, ViewGridColumn column, object row, object? value)
    {
        var combo = new ComboBox
        {
            DropDownStyle = _dropDownList ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown,
            IntegralHeight = false
        };
        var values = _items ?? column.ComboBoxItems ?? Array.Empty<string>();
        foreach (var item in values) combo.Items.Add(item);
        combo.Text = Convert.ToString(value) ?? string.Empty;
        if (column.ComboBoxImageGetter != null)
        {
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = Math.Max(22, combo.ItemHeight);
            combo.DrawItem += (_, e) => DrawImageComboItem(combo, e, column);
        }
        if (combo.DropDownStyle == ComboBoxStyle.DropDownList && combo.Items.Count > 0 && combo.SelectedIndex < 0)
        {
            int idx = combo.FindStringExact(combo.Text);
            combo.SelectedIndex = idx >= 0 ? idx : 0;
        }
        combo.Bounds = bounds;
        owner.Controls.Add(combo);
        combo.Focus();
        combo.BeginInvoke(new Action(() => { if (!combo.IsDisposed) combo.DroppedDown = true; }));
        return combo;
    }

    public object? GetEditedValue(Control editor) => ((ComboBox)editor).Text;

    private static void DrawImageComboItem(ComboBox combo, DrawItemEventArgs e, ViewGridColumn column)
    {
        e.DrawBackground();
        if (e.Index < 0 || e.Index >= combo.Items.Count) return;
        string text = Convert.ToString(combo.Items[e.Index]) ?? string.Empty;
        var back = (e.State & DrawItemState.Selected) == DrawItemState.Selected ? SystemColors.Highlight : combo.BackColor;
        var fore = (e.State & DrawItemState.Selected) == DrawItemState.Selected ? SystemColors.HighlightText : combo.ForeColor;
        using var backBrush = new SolidBrush(back);
        e.Graphics.FillRectangle(backBrush, e.Bounds);
        int x = e.Bounds.Left + 4;
        var img = column.ComboBoxImageGetter?.Invoke(text);
        if (img != null)
        {
            var ir = new Rectangle(x, e.Bounds.Top + Math.Max(0, (e.Bounds.Height - 18) / 2), 18, 18);
            e.Graphics.DrawImage(img, ir);
            x += 23;
        }
        TextRenderer.DrawText(e.Graphics, text, combo.Font, new Rectangle(x, e.Bounds.Top, Math.Max(0, e.Bounds.Right - x - 4), e.Bounds.Height), fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }
}
