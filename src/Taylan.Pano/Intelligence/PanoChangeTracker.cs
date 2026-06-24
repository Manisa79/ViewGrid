using Taylan.Pano.Columns;

namespace Taylan.Pano.Intelligence;

public sealed class PanoChangeTracker
{
    private readonly Dictionary<string, Dictionary<string, object?>> _snapshot = new(StringComparer.OrdinalIgnoreCase);

    public void Capture(IEnumerable<object> rows, IEnumerable<PanoColumn> columns, Func<object, string>? keySelector = null)
    {
        _snapshot.Clear();
        foreach (object row in rows)
        {
            string key = ResolveKey(row, keySelector);
            _snapshot[key] = CaptureRow(row, columns);
        }
    }

    public IReadOnlyList<Taylan.Pano.Core.PanoRowChangeSet> Detect(IEnumerable<object> rows, IEnumerable<PanoColumn> columns, Func<object, string>? keySelector = null)
    {
        List<Taylan.Pano.Core.PanoRowChangeSet> result = new();
        List<PanoColumn> colList = columns.ToList();
        foreach (object row in rows)
        {
            string key = ResolveKey(row, keySelector);
            Dictionary<string, object?> current = CaptureRow(row, colList);
            if (!_snapshot.TryGetValue(key, out Dictionary<string, object?>? old))
            {
                Taylan.Pano.Core.PanoRowChangeSet added = new Taylan.Pano.Core.PanoRowChangeSet { RowObject = row, RowKey = key, ChangedAt = DateTime.Now };
                foreach (PanoColumn col in colList)
                    added.Changes.Add(new Taylan.Pano.Core.PanoTrackedCellChange { ColumnKey = col.Key, Header = col.Header, OldValue = null, NewValue = current.TryGetValue(col.Key, out object? nv) ? nv : null });
                result.Add(added);
                continue;
            }
            Taylan.Pano.Core.PanoRowChangeSet set = new Taylan.Pano.Core.PanoRowChangeSet { RowObject = row, RowKey = key, ChangedAt = DateTime.Now };
            foreach (PanoColumn col in colList)
            {
                old.TryGetValue(col.Key, out object? ov);
                current.TryGetValue(col.Key, out object? nv);
                if (!object.Equals(Normalize(ov), Normalize(nv)))
                    set.Changes.Add(new Taylan.Pano.Core.PanoTrackedCellChange { ColumnKey = col.Key, Header = col.Header, OldValue = ov, NewValue = nv, ChangedAt = DateTime.Now });
            }
            if (set.Changes.Count > 0) result.Add(set);
        }
        return result;
    }

    private static Dictionary<string, object?> CaptureRow(object row, IEnumerable<PanoColumn> columns)
    {
        Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (PanoColumn col in columns) values[col.Key] = col.GetValue(row);
        return values;
    }

    private static string ResolveKey(object row, Func<object, string>? keySelector)
    {
        if (keySelector != null) return keySelector(row) ?? string.Empty;
        object? id = PanoExpressionEngine.GetValue(row, "Id") ?? PanoExpressionEngine.GetValue(row, "ID") ?? PanoExpressionEngine.GetValue(row, "Key");
        return Convert.ToString(id) ?? row.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static object? Normalize(object? value) => value == DBNull.Value ? null : value;
}
