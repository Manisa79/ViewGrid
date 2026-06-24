using Taylan.Pano.Columns;

namespace Taylan.Pano.Summary;

public enum PanoSummaryType { None, Count, Sum, Average, Min, Max }

public sealed class PanoSummaryItem
{
    public PanoColumn Column { get; init; } = null!;
    public PanoSummaryType Type { get; init; }
    public string Format { get; init; } = "{0}";
    public string Calculate(IEnumerable<object> rows)
    {
        var values = rows.Select(r => Column.GetValue(r)).Where(v => v != null && v != DBNull.Value).ToList();
        object result = Type switch
        {
            PanoSummaryType.Count => values.Count,
            PanoSummaryType.Sum => values.Sum(ToDecimal),
            PanoSummaryType.Average => values.Count == 0 ? 0 : values.Average(ToDecimal),
            PanoSummaryType.Min => values.Select(v => Convert.ToString(v)).OrderBy(x => x).FirstOrDefault() ?? string.Empty,
            PanoSummaryType.Max => values.Select(v => Convert.ToString(v)).OrderByDescending(x => x).FirstOrDefault() ?? string.Empty,
            _ => string.Empty
        };
        return string.Format(Format, result);
    }
    private static decimal ToDecimal(object? v) { try { return Convert.ToDecimal(v); } catch { return 0; } }
}
