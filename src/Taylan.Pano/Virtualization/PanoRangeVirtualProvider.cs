namespace Taylan.Pano.Virtualization;

/// <summary>
/// v27 range/page tabanlı sanal provider. SQL/SAP/API gibi kaynaklarda tüm veriyi belleğe almadan,
/// ekranda istenen aralıkları cacheleyerek gösterme altyapısıdır.
/// </summary>
public interface IPanoRangeRowProvider : IRowProvider, IProviderChangeNotifier
{
    int PageSize { get; }
    ValueTask<IReadOnlyList<object?>> FetchRangeAsync(int startIndex, int count, CancellationToken cancellationToken = default);
    void InvalidateCache();
}

public sealed class PanoRangeVirtualProvider : IPanoRangeRowProvider, IDisposable
{
    private readonly Func<int> _countProvider;
    private readonly Func<int, int, CancellationToken, ValueTask<IReadOnlyList<object?>>> _fetchRangeAsync;
    private readonly Dictionary<int, IReadOnlyList<object?>> _pages = new();
    private readonly HashSet<int> _loadingPages = new();
    private readonly object _sync = new();
    private readonly CancellationTokenSource _disposeCts = new();

    public event EventHandler? RowsChanged;

    public PanoRangeVirtualProvider(
        Func<int> countProvider,
        Func<int, int, CancellationToken, ValueTask<IReadOnlyList<object?>>> fetchRangeAsync,
        int pageSize = 250)
    {
        _countProvider = countProvider ?? throw new ArgumentNullException(nameof(countProvider));
        _fetchRangeAsync = fetchRangeAsync ?? throw new ArgumentNullException(nameof(fetchRangeAsync));
        PageSize = Math.Max(25, pageSize);
    }

    public int PageSize { get; }
    public int Count => Math.Max(0, _countProvider());

    public object? GetRow(int index)
    {
        if (index < 0 || index >= Count) return null;

        int pageIndex = index / PageSize;
        int offset = index % PageSize;

        lock (_sync)
        {
            if (_pages.TryGetValue(pageIndex, out var page))
                return offset >= 0 && offset < page.Count ? page[offset] : null;
        }

        _ = EnsurePageAsync(pageIndex, _disposeCts.Token);
        return PanoVirtualPlaceholder.Create(index);
    }

    public async ValueTask<IReadOnlyList<object?>> FetchRangeAsync(int startIndex, int count, CancellationToken cancellationToken = default)
    {
        startIndex = Math.Max(0, startIndex);
        count = Math.Max(0, Math.Min(count, Count - startIndex));
        if (count == 0) return Array.Empty<object?>();
        return await _fetchRangeAsync(startIndex, count, cancellationToken).ConfigureAwait(false);
    }

    public void InvalidateCache()
    {
        lock (_sync)
        {
            _pages.Clear();
            _loadingPages.Clear();
        }
        RowsChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task EnsurePageAsync(int pageIndex, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_pages.ContainsKey(pageIndex) || !_loadingPages.Add(pageIndex)) return;
        }

        try
        {
            int start = pageIndex * PageSize;
            int count = Math.Min(PageSize, Math.Max(0, Count - start));
            var rows = await _fetchRangeAsync(start, count, cancellationToken).ConfigureAwait(false);
            lock (_sync) _pages[pageIndex] = rows;
            RowsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            lock (_sync) _pages[pageIndex] = Array.Empty<object?>();
        }
        finally
        {
            lock (_sync) _loadingPages.Remove(pageIndex);
        }
    }

    public void Dispose()
    {
        _disposeCts.Cancel();
        _disposeCts.Dispose();
    }
}

public sealed record PanoVirtualPlaceholder(int Index, string Text)
{
    public static PanoVirtualPlaceholder Create(int index) => new(index, "Yükleniyor...");
}
