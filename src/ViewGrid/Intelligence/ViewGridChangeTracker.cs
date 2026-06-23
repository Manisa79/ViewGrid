using ViewGrid.Columns;

namespace ViewGrid.Intelligence;

public sealed class ViewGridChangeTracker
{
    private readonly Dictionary<string, Dictionary<string, object?>> _snapshot = new(StringComparer.OrdinalIgnoreCase);

    public void Capture(IEnumerable<object> rows, IEnumerable<ViewGridColumn> columns, Func<object, string>? keySelector = null)
    {
        _snapshot.Clear();
        foreach (object row in rows)
        {
            string key = ResolveKey(row, keySelector);
            _snapshot[key] = CaptureRow(row, columns);
        }
    }

    public IReadOnlyList<ViewGrid.Core.ViewGridRowChangeSet> Detect(IEnumerable<object> rows, IEnumerable<ViewGridColumn> columns, Func<object, string>? keySelector = null)
    {
        List<ViewGrid.Core.ViewGridRowChangeSet> result = new();
        List<ViewGridColumn> colList = columns.ToList();
        foreach (object row in rows)
        {
            string key = ResolveKey(row, keySelector);
            Dictionary<string, object?> current = CaptureRow(row, colList);
            if (!_snapshot.TryGetValue(key, out Dictionary<string, object?>? old))
            {
                ViewGrid.Core.ViewGridRowChangeSet added = new ViewGrid.Core.ViewGridRowChangeSet { RowObject = row, RowKey = key, ChangedAt = DateTime.Now };
                foreach (ViewGridColumn col in colList)
                    added.Changes.Add(new ViewGrid.Core.ViewGridTrackedCellChange { ColumnKey = col.Key, Header = col.Header, OldValue = null, NewValue = current.TryGetValue(col.Key, out object? nv) ? nv : null });
                result.Add(added);
                continue;
            }
            ViewGrid.Core.ViewGridRowChangeSet set = new ViewGrid.Core.ViewGridRowChangeSet { RowObject = row, RowKey = key, ChangedAt = DateTime.Now };
            foreach (ViewGridColumn col in colList)
            {
                old.TryGetValue(col.Key, out object? ov);
                current.TryGetValue(col.Key, out object? nv);
                if (!object.Equals(Normalize(ov), Normalize(nv)))
                    set.Changes.Add(new ViewGrid.Core.ViewGridTrackedCellChange { ColumnKey = col.Key, Header = col.Header, OldValue = ov, NewValue = nv, ChangedAt = DateTime.Now });
            }
            if (set.Changes.Count > 0) result.Add(set);
        }
        return result;
    }

    private static Dictionary<string, object?> CaptureRow(object row, IEnumerable<ViewGridColumn> columns)
    {
        Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (ViewGridColumn col in columns) values[col.Key] = col.GetValue(row);
        return values;
    }

    private static string ResolveKey(object row, Func<object, string>? keySelector)
    {
        if (keySelector != null) return keySelector(row) ?? string.Empty;
        object? id = ViewGridExpressionEngine.GetValue(row, "Id") ?? ViewGridExpressionEngine.GetValue(row, "ID") ?? ViewGridExpressionEngine.GetValue(row, "Key");
        return Convert.ToString(id) ?? row.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static object? Normalize(object? value) => value == DBNull.Value ? null : value;
}
