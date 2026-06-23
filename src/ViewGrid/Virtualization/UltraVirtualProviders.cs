using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;
using System.Text;
using ViewGrid.Columns;
using ViewGrid.Filtering;

namespace ViewGrid.Virtualization;

/// <summary>
/// Server/query backed provider contract. ViewGridControl asks the provider to apply the
/// current filter/sort state, then reads only visible rows by index.
/// </summary>
public interface IQueryRowProvider : IRowProvider
{
    long TotalCount64 { get; }
    void ApplyView(ViewGridFilterSet filters, ViewGridColumn[] columns, ViewGridColumn? sortColumn, bool sortDescending);
    bool TryGetDistinctValues(ViewGridColumn column, ViewGridFilterSet filters, ViewGridColumn[] columns, int maxValues, string? searchText, out IReadOnlyList<string> values);
}

/// <summary>
/// Optional preload hook. Providers can warm pages around the visible viewport without blocking the UI.
/// </summary>
public interface IAsyncPreloadRowProvider
{
    void RequestPreload(int startIndex, int count);
}

/// <summary>
/// Generic SQL/DbConnection backed provider for very large tables.
/// It never loads the full table; it uses COUNT + OFFSET/FETCH pages.
/// </summary>
public sealed class DbQueryRowProvider : IQueryRowProvider, IAsyncPreloadRowProvider, IDisposable
{
    private readonly Func<DbConnection> _connectionFactory;
    private readonly string _tableOrViewName;
    private readonly int _pageSize;
    private readonly int _maxCachedPages;
    private readonly object _sync = new();
    private readonly Dictionary<long, IReadOnlyList<Dictionary<string, object?>>> _pages = new();
    private readonly LinkedList<long> _pageLru = new();
    private readonly ConcurrentDictionary<long, byte> _preloadInFlight = new();

    private ViewGridFilterSet _filters = new();
    private ViewGridColumn[] _columns = Array.Empty<ViewGridColumn>();
    private ViewGridColumn? _sortColumn;
    private bool _sortDescending;
    private long? _totalCount;

    public DbQueryRowProvider(Func<DbConnection> connectionFactory, string tableOrViewName, int pageSize = 250, int maxCachedPages = 12)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tableOrViewName = string.IsNullOrWhiteSpace(tableOrViewName) ? throw new ArgumentException("Table/view name is required.", nameof(tableOrViewName)) : tableOrViewName;
        _pageSize = Math.Max(25, pageSize);
        _maxCachedPages = Math.Max(2, maxCachedPages);
    }

    public long TotalCount64
    {
        get
        {
            if (_totalCount.HasValue) return _totalCount.Value;
            _totalCount = ExecuteScalarCount();
            return _totalCount.Value;
        }
    }

    public int Count => (int)Math.Min(int.MaxValue, Math.Max(0, TotalCount64));

    public void ApplyView(ViewGridFilterSet filters, ViewGridColumn[] columns, ViewGridColumn? sortColumn, bool sortDescending)
    {
        _filters = filters;
        _columns = columns ?? Array.Empty<ViewGridColumn>();
        _sortColumn = sortColumn;
        _sortDescending = sortDescending;
        _totalCount = null;
        lock (_sync)
        {
            _pages.Clear();
            _pageLru.Clear();
        }
    }

    public object? GetRow(int index)
    {
        if (index < 0 || index >= Count) return null;
        long pageIndex = index / _pageSize;
        int offset = index % _pageSize;
        var page = GetPage(pageIndex);
        return offset >= 0 && offset < page.Count ? page[offset] : null;
    }

    public bool TryGetDistinctValues(ViewGridColumn column, ViewGridFilterSet filters, ViewGridColumn[] columns, int maxValues, string? searchText, out IReadOnlyList<string> values)
    {
        values = Array.Empty<string>();
        if (column == null || string.IsNullOrWhiteSpace(column.AspectName)) return false;
        if (!IsKnownColumn(column.AspectName, columns)) return false;

        var sql = new StringBuilder();
        var parameters = new List<(string name, object? value)>();
        sql.Append("SELECT ").Append(TopClause(maxValues)).Append(' ')
           .Append(QuoteIdentifier(column.AspectName)).Append(" AS ValueText, COUNT(*) AS Cnt FROM ")
           .Append(_tableOrViewName);
        AppendWhere(sql, parameters, filters, columns, ignoredAspectName: column.AspectName);
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string sp = "@filterSearch";
            if (sql.ToString().IndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase) >= 0)
                sql.Append(" AND ");
            else
                sql.Append(" WHERE ");
            sql.Append("CONVERT(nvarchar(max), ").Append(QuoteIdentifier(column.AspectName)).Append(") LIKE ").Append(sp);
            parameters.Add((sp, "%" + searchText.Trim() + "%"));
        }
        sql.Append(" GROUP BY ").Append(QuoteIdentifier(column.AspectName))
           .Append(" ORDER BY Cnt DESC, ").Append(QuoteIdentifier(column.AspectName));
        if (UsesFetchInsteadOfTop()) sql.Append(" FETCH NEXT ").Append(Math.Max(1, maxValues).ToString(CultureInfo.InvariantCulture)).Append(" ROWS ONLY");

        var list = new List<string>();
        using var conn = _connectionFactory();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql.ToString();
        AddParameters(cmd, parameters);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(Convert.ToString(reader[0], CultureInfo.CurrentCulture) ?? string.Empty);
        values = list;
        return true;
    }

    public void RequestPreload(int startIndex, int count)
    {
        if (startIndex < 0 || count <= 0) return;
        long firstPage = startIndex / _pageSize;
        long lastPage = (startIndex + count - 1L) / _pageSize;
        for (long p = firstPage; p <= lastPage; p++)
        {
            if (IsPageCached(p) || !_preloadInFlight.TryAdd(p, 0)) continue;
            long page = p;
            _ = Task.Run(() =>
            {
                try { _ = GetPage(page); }
                catch { }
                finally { _preloadInFlight.TryRemove(page, out _); }
            });
        }
    }

    private IReadOnlyList<Dictionary<string, object?>> GetPage(long pageIndex)
    {
        lock (_sync)
        {
            if (_pages.TryGetValue(pageIndex, out var cached))
            {
                _pageLru.Remove(pageIndex);
                _pageLru.AddLast(pageIndex);
                return cached;
            }
        }

        var page = ExecutePage(pageIndex * _pageSize, _pageSize);
        lock (_sync)
        {
            _pages[pageIndex] = page;
            _pageLru.Remove(pageIndex);
            _pageLru.AddLast(pageIndex);
            while (_pages.Count > _maxCachedPages && _pageLru.First != null)
            {
                long old = _pageLru.First.Value;
                _pageLru.RemoveFirst();
                _pages.Remove(old);
            }
        }
        return page;
    }

    private bool IsPageCached(long pageIndex)
    {
        lock (_sync) return _pages.ContainsKey(pageIndex);
    }

    private long ExecuteScalarCount()
    {
        var sql = new StringBuilder("SELECT COUNT_BIG(*) FROM ").Append(_tableOrViewName);
        var parameters = new List<(string name, object? value)>();
        AppendWhere(sql, parameters, _filters, _columns, ignoredAspectName: null);
        using var conn = _connectionFactory();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql.ToString();
        AddParameters(cmd, parameters);
        return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private IReadOnlyList<Dictionary<string, object?>> ExecutePage(long startIndex, int count)
    {
        var parameters = new List<(string name, object? value)>();
        var sql = new StringBuilder();
        sql.Append("SELECT * FROM ").Append(_tableOrViewName);
        AppendWhere(sql, parameters, _filters, _columns, ignoredAspectName: null);
        AppendOrderBy(sql);
        sql.Append(" OFFSET ").Append(startIndex.ToString(CultureInfo.InvariantCulture))
           .Append(" ROWS FETCH NEXT ").Append(count.ToString(CultureInfo.InvariantCulture)).Append(" ROWS ONLY");

        var rows = new List<Dictionary<string, object?>>();
        using var conn = _connectionFactory();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql.ToString();
        AddParameters(cmd, parameters);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    private void AppendWhere(StringBuilder sql, List<(string name, object? value)> parameters, ViewGridFilterSet filters, ViewGridColumn[] columns, string? ignoredAspectName)
    {
        var clauses = new List<string>();
        if (!string.IsNullOrWhiteSpace(filters.GlobalText))
        {
            var orParts = new List<string>();
            int g = 0;
            foreach (var c in columns.Where(c => !string.IsNullOrWhiteSpace(c.AspectName)))
            {
                string p = "@g" + g++;
                orParts.Add($"CONVERT(nvarchar(max), {QuoteIdentifier(c.AspectName)}) LIKE {p}");
                parameters.Add((p, "%" + filters.GlobalText + "%"));
            }
            if (orParts.Count > 0) clauses.Add("(" + string.Join(" OR ", orParts) + ")");
        }

        int n = 0;
        foreach (var f in filters.Filters)
        {
            if (!string.IsNullOrWhiteSpace(ignoredAspectName) && string.Equals(f.AspectName, ignoredAspectName, StringComparison.OrdinalIgnoreCase)) continue;
            if (!IsKnownColumn(f.AspectName, columns)) continue;
            string col = QuoteIdentifier(f.AspectName);
            string p = "@p" + n++;
            switch (f.Mode)
            {
                case ViewGridFilterMode.Contains:
                    clauses.Add($"CONVERT(nvarchar(max), {col}) LIKE {p}"); parameters.Add((p, "%" + (f.Text ?? string.Empty) + "%")); break;
                case ViewGridFilterMode.StartsWith:
                    clauses.Add($"CONVERT(nvarchar(max), {col}) LIKE {p}"); parameters.Add((p, (f.Text ?? string.Empty) + "%")); break;
                case ViewGridFilterMode.EndsWith:
                    clauses.Add($"CONVERT(nvarchar(max), {col}) LIKE {p}"); parameters.Add((p, "%" + (f.Text ?? string.Empty))); break;
                case ViewGridFilterMode.Equals:
                    clauses.Add($"CONVERT(nvarchar(max), {col}) = {p}"); parameters.Add((p, f.Text ?? string.Empty)); break;
                case ViewGridFilterMode.GreaterThan:
                    clauses.Add($"{col} > {p}"); parameters.Add((p, f.Text ?? string.Empty)); break;
                case ViewGridFilterMode.LessThan:
                    clauses.Add($"{col} < {p}"); parameters.Add((p, f.Text ?? string.Empty)); break;
                case ViewGridFilterMode.Between:
                    string p2 = "@p" + n++;
                    clauses.Add($"{col} BETWEEN {p} AND {p2}"); parameters.Add((p, f.Text ?? string.Empty)); parameters.Add((p2, f.Text2 ?? f.Text ?? string.Empty)); break;
                case ViewGridFilterMode.IsEmpty:
                    clauses.Add($"({col} IS NULL OR CONVERT(nvarchar(max), {col}) = N'')"); break;
                case ViewGridFilterMode.IsNotEmpty:
                    clauses.Add($"({col} IS NOT NULL AND CONVERT(nvarchar(max), {col}) <> N'')"); break;
                case ViewGridFilterMode.ValueList:
                    if (f.SelectedValues == null) break;
                    if (f.SelectedValues.Count == 0) { clauses.Add("1 = 0"); break; }
                    var inParams = new List<string>();
                    foreach (string v in f.SelectedValues.Take(2000))
                    {
                        string ip = "@p" + n++;
                        inParams.Add(ip);
                        parameters.Add((ip, v));
                    }
                    clauses.Add($"CONVERT(nvarchar(max), {col}) IN (" + string.Join(",", inParams) + ")");
                    break;
            }
        }
        if (clauses.Count > 0) sql.Append(" WHERE ").Append(string.Join(" AND ", clauses));
    }

    private void AppendOrderBy(StringBuilder sql)
    {
        string sort = _sortColumn != null && IsKnownColumn(_sortColumn.AspectName, _columns) ? _sortColumn.AspectName : GetDefaultSortColumn();
        sql.Append(" ORDER BY ").Append(QuoteIdentifier(sort)).Append(_sortDescending ? " DESC" : " ASC");
    }

    private string GetDefaultSortColumn()
        => _columns.FirstOrDefault(c => string.Equals(c.AspectName, "Id", StringComparison.OrdinalIgnoreCase))?.AspectName
           ?? _columns.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.AspectName))?.AspectName
           ?? throw new InvalidOperationException("DbQueryRowProvider requires at least one column with AspectName for ORDER BY.");

    private static bool IsKnownColumn(string? name, ViewGridColumn[] columns)
        => !string.IsNullOrWhiteSpace(name) && columns.Any(c => string.Equals(c.AspectName, name, StringComparison.OrdinalIgnoreCase));

    private static void AddParameters(DbCommand cmd, List<(string name, object? value)> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }

    private static string QuoteIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid column name.", nameof(name));
        return "[" + name.Replace("]", "]]", StringComparison.Ordinal) + "]";
    }

    private static string TopClause(int maxValues) => "TOP (" + Math.Max(1, maxValues).ToString(CultureInfo.InvariantCulture) + ")";
    private static bool UsesFetchInsteadOfTop() => false;

    public void Dispose()
    {
        lock (_sync)
        {
            _pages.Clear();
            _pageLru.Clear();
        }
    }
}
