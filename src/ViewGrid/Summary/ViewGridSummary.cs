using ViewGrid.Columns;

namespace ViewGrid.Summary;

public enum ViewGridSummaryType { None, Count, Sum, Average, Min, Max }

public sealed class ViewGridSummaryItem
{
    public ViewGridColumn Column { get; init; } = null!;
    public ViewGridSummaryType Type { get; init; }
    public string Format { get; init; } = "{0}";
    public string Calculate(IEnumerable<object> rows)
    {
        var values = rows.Select(r => Column.GetValue(r)).Where(v => v != null && v != DBNull.Value).ToList();
        object result = Type switch
        {
            ViewGridSummaryType.Count => values.Count,
            ViewGridSummaryType.Sum => values.Sum(ToDecimal),
            ViewGridSummaryType.Average => values.Count == 0 ? 0 : values.Average(ToDecimal),
            ViewGridSummaryType.Min => values.Select(v => Convert.ToString(v)).OrderBy(x => x).FirstOrDefault() ?? string.Empty,
            ViewGridSummaryType.Max => values.Select(v => Convert.ToString(v)).OrderByDescending(x => x).FirstOrDefault() ?? string.Empty,
            _ => string.Empty
        };
        return string.Format(Format, result);
    }
    private static decimal ToDecimal(object? v) { try { return Convert.ToDecimal(v); } catch { return 0; } }
}
