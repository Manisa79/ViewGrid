using ViewGrid.Columns;

namespace ViewGrid.Core;

public sealed class ViewGridColumnAnalytics
{
    public string ColumnText { get; init; } = string.Empty;
    public string AspectName { get; init; } = string.Empty;
    public int RowCount { get; init; }
    public int BlankCount { get; init; }
    public int DistinctCount { get; init; }
    public IReadOnlyList<ViewGridValueCount> TopValues { get; init; } = Array.Empty<ViewGridValueCount>();

    public static ViewGridColumnAnalytics From(ViewGridColumn column, IReadOnlyList<object> rows, int maxDistinct = 8)
    {
        var counts = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
        int blank = 0;
        foreach (var row in rows)
        {
            string value = Convert.ToString(column.GetValue(row)) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value)) blank++;
            string key = string.IsNullOrWhiteSpace(value) ? "(Blanks)" : value.Trim();
            counts.TryGetValue(key, out int current);
            counts[key] = current + 1;
        }

        return new ViewGridColumnAnalytics
        {
            ColumnText = column.Header,
            AspectName = column.AspectName,
            RowCount = rows.Count,
            BlankCount = blank,
            DistinctCount = counts.Count,
            TopValues = counts
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .Take(Math.Max(1, maxDistinct))
                .Select(kv => new ViewGridValueCount(kv.Key, kv.Value))
                .ToList()
        };
    }
}

public readonly record struct ViewGridValueCount(string Value, int Count);
