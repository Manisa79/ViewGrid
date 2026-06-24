using Taylan.Pano.Columns;

namespace Taylan.Pano.Formatting;

public sealed class PanoConditionalFormat
{
    public PanoColumn? Column { get; set; }
    public Func<object, PanoColumn, object?, bool> Predicate { get; set; } = (_,_,__) => false;
    public Color? BackColor { get; set; }
    public Color? ForeColor { get; set; }
    public Font? Font { get; set; }
    public Image? Icon { get; set; }

    public bool IsMatch(object row, PanoColumn column)
    {
        if (Column != null && !ReferenceEquals(Column, column)) return false;
        return Predicate(row, column, column.GetValue(row));
    }
}
