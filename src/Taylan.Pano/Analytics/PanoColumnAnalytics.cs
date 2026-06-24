using Taylan.Pano.Columns;

namespace Taylan.Pano.Core;

public sealed class PanoColumnAnalytics
{
    public string ColumnText { get; init; } = string.Empty;
    public string AspectName { get; init; } = string.Empty;
    public int RowCount { get; init; }
    public int BlankCount { get; init; }
    public int DistinctCount { get; init; }
    public IReadOnlyList<PanoValueCount> TopValues { get; init; } = Array.Empty<PanoValueCount>();

    public static PanoColumnAnalytics From(PanoColumn column, IReadOnlyList<object> rows, int maxDistinct = 8)
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

        return new PanoColumnAnalytics
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
                .Select(kv => new PanoValueCount(kv.Key, kv.Value))
                .ToList()
        };
    }
}

public readonly record struct PanoValueCount(string Value, int Count);
