using System.Windows.Forms;
using Taylan.Pano.Columns;
using Taylan.Pano.Filtering;

namespace Taylan.Pano.Virtualization;

public interface IRowProvider
{
    int Count { get; }
    object? GetRow(int index);
}

/// <summary>
/// Optional provider hook: huge/async providers can notify PanoControl when a cached page is ready.
/// </summary>
public interface IProviderChangeNotifier
{
    event EventHandler? RowsChanged;
}

/// <summary>
/// Optional fast path for huge virtual/query providers. It lets PanoControl update or summarize
/// checkbox state without materializing millions of rows on the UI thread.
/// </summary>
public interface IBulkCheckStateProvider
{
    bool TrySetAllCheckStates(PanoColumn column, CheckState state);
    bool TryGetCheckStateSummary(PanoColumn column, out int checkedCount, out int uncheckedCount);
}

/// <summary>
/// Optional fast path for huge/virtual data sources. If implemented, PanoControl can ask the
/// provider for matching real row indexes instead of scanning 1M+ rows on the UI thread.
/// </summary>
public interface IIndexedRowProvider : IRowProvider
{
    bool TryBuildViewIndexes(PanoFilterSet filters, PanoColumn[] columns, PanoColumn? sortColumn, bool sortDescending, int maxFallbackScanRows, out IReadOnlyList<int> indexes);
}

public sealed class ListRowProvider : IIndexedRowProvider
{
    private readonly IList<object> _rows;
    public ListRowProvider(IEnumerable<object> rows) => _rows = rows.ToList();
    public int Count => _rows.Count;
    public object? GetRow(int index) => index >= 0 && index < _rows.Count ? _rows[index] : null;

    /// <summary>
    /// v24.61 fast in-memory path: large lists are filtered with a partitioned scan instead of
    /// forcing the control to walk 1M rows sequentially on the UI path. The control still owns
    /// the final view index list, but the expensive matching phase is parallelized and capped.
    /// </summary>
    public bool TryBuildViewIndexes(PanoFilterSet filters, PanoColumn[] columns, PanoColumn? sortColumn, bool sortDescending, int maxFallbackScanRows, out IReadOnlyList<int> indexes)
    {
        indexes = Array.Empty<int>();
        int count = _rows.Count;
        if (count < 50_000) return false;

        int scanCount = Math.Min(count, Math.Max(1, maxFallbackScanRows));
        var matches = new System.Collections.Concurrent.ConcurrentBag<int>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };

        try
        {
            Parallel.For(0, scanCount, options, i =>
            {
                var row = _rows[i];
                if (row != null && filters.Passes(row, columns)) matches.Add(i);
            });

            var list = matches.ToList();
            if (sortColumn != null)
            {
                list.Sort((a, b) => CompareRows(_rows[a], _rows[b], sortColumn));
                if (sortDescending) list.Reverse();
            }
            else
            {
                list.Sort();
            }

            indexes = list;
            return true;
        }
        catch
        {
            indexes = Array.Empty<int>();
            return false;
        }
    }

    private static int CompareRows(object? a, object? b, PanoColumn col)
    {
        var va = a == null ? null : col.GetValue(a);
        var vb = b == null ? null : col.GetValue(b);
        if (va is IComparable ca) return ca.CompareTo(vb);
        return string.Compare(Convert.ToString(va), Convert.ToString(vb), StringComparison.CurrentCultureIgnoreCase);
    }
}

public sealed class DelegateRowProvider : IRowProvider
{
    private readonly Func<int> _count;
    private readonly Func<int, object?> _getter;
    public DelegateRowProvider(Func<int> count, Func<int, object?> getter) { _count = count; _getter = getter; }
    public int Count => _count();
    public object? GetRow(int index) => _getter(index);
}
