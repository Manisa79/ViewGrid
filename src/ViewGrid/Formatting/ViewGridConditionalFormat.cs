using ViewGrid.Columns;

namespace ViewGrid.Formatting;

public sealed class ViewGridConditionalFormat
{
    public ViewGridColumn? Column { get; set; }
    public Func<object, ViewGridColumn, object?, bool> Predicate { get; set; } = (_,_,__) => false;
    public Color? BackColor { get; set; }
    public Color? ForeColor { get; set; }
    public Font? Font { get; set; }
    public Image? Icon { get; set; }

    public bool IsMatch(object row, ViewGridColumn column)
    {
        if (Column != null && !ReferenceEquals(Column, column)) return false;
        return Predicate(row, column, column.GetValue(row));
    }
}
