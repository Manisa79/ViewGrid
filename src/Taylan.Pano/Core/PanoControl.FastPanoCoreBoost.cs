using Taylan.Pano.Columns;
using Taylan.Pano.Virtualization;

namespace Taylan.Pano.Core;

public partial class PanoControl
{
    private CancellationTokenSource? _fastPanoAsyncLoadCts;

    /// <summary>
    /// FastPanoControl için önerilen en hızlı ve stabil profil.
    /// Büyük veri listelerinde menünün anında açılması, async değer yükleme,
    /// akıllı filtre index'i, layout manager ve highlight motoru varsayılan aktif gelir.
    /// </summary>
    public void ApplyUltimatePerformanceProfile()
    {
        FastFilterMenuForHugeLists = true;
        FastFilterMenuInitialScanRows = 250;
        FastFilterPopupPreviewRows = 250;
        FastFilterMenuSearchScanRows = Math.Max(FastFilterMenuSearchScanRows, 1_000_000);
        TypedFilterSearchesAllRows = true;
        AsyncLoadFullFilterValues = true;
        MaxAsyncFilterDistinctScanRows = Math.Max(MaxAsyncFilterDistinctScanRows, 1_000_000);
        MaxEmbeddedFilterVisibleValues = Math.Max(MaxEmbeddedFilterVisibleValues, 2_000);

        EnableSmartFilterEngine = true;
        BuildSmartFilterIndexInBackground = true;
        SmartFilterSearchAllRows = true;
        SmartFilterTopValuesFirst = true;
        SmartFilterPopupValueLimit = Math.Max(SmartFilterPopupValueLimit, 750);
        SmartFilterMaxScanRows = Math.Max(SmartFilterMaxScanRows, 2_000_000);
        SmartFilterSearchDebounceMs = Math.Max(1, Math.Min(SmartFilterSearchDebounceMs, 35));

        DebounceGlobalFilterForHugeVirtualLists = true;
        HighlightSearchText = true;
        HighlightGlobalFilterText = true;
        EnableHighlightEngine = true;
        DefaultHighlightDurationMs = Math.Max(DefaultHighlightDurationMs, 7_000);

        EnableColumnAutoResizeOnDoubleClick = true;
        AllowColumnReorder = true;
        ShowColumnReorderPreview = true;
        AutoResizeIncludeHeader = true;
        AutoResizeSampleRows = Math.Max(AutoResizeSampleRows, 2_000);
        AutoResizeMaxWidth = Math.Max(AutoResizeMaxWidth, 600);

        SmoothMouseWheelScroll = true;
        MouseWheelRowsPerNotch = Math.Max(1, MouseWheelRowsPerNotch);
        CachePreloadExtraRows = Math.Max(CachePreloadExtraRows, 400);
        EnableAnimatedSelection = false; // 1M+ satırda gereksiz timer maliyetini kapalı tut.
        EnableRowHoverGlow = true;
        EnableModernEmptyState = true;
        MergeBuiltInMenuWithUserContextMenu = true;

        Invalidate();
    }

    /// <summary>
    /// Seçili kolonlar için akıllı filtre index'ini arka planda hazırlar.
    /// Kullanıcı filtre popup'ını açtığında yazma/silme çok daha seri hissedilir.
    /// </summary>
    public void PrebuildSmartFilterIndexes(params string[] aspectNames)
    {
        if (!EnableSmartFilterEngine) return;

        IEnumerable<PanoColumn> targets = aspectNames == null || aspectNames.Length == 0
            ? Columns.Where(c => c.Filterable)
            : Columns.Where(c => aspectNames.Any(a => string.Equals(a, c.AspectName, StringComparison.OrdinalIgnoreCase)));

        foreach (var col in targets)
            BeginBuildSmartFilterIndex(col);
    }

    /// <summary>
    /// Önceki async yüklemeyi iptal edip yeni veri yükler. UI thread'i bloke etmez.
    /// </summary>
    public Task SetObjectsAsyncSafe<T>(Func<CancellationToken, Task<IEnumerable<T>>> loader, bool applyPerformanceProfile = true, CancellationToken token = default)
    {
        if (loader == null) throw new ArgumentNullException(nameof(loader));
        if (applyPerformanceProfile) ApplyUltimatePerformanceProfile();

        _fastPanoAsyncLoadCts?.Cancel();
        _fastPanoAsyncLoadCts?.Dispose();
        _fastPanoAsyncLoadCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var linked = _fastPanoAsyncLoadCts.Token;

        return Task.Run(async () =>
        {
            var data = (await loader(linked).ConfigureAwait(false)).Cast<object>().ToList();
            linked.ThrowIfCancellationRequested();
            if (!IsDisposed && IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed || linked.IsCancellationRequested) return;
                    SetObjects(data);
                    PrebuildSmartFilterIndexes();
                }));
            }
        }, linked);
    }

    /// <summary>
    /// Delegate tabanlı ultra hafif virtual provider kurulumu.
    /// </summary>
    public void SetFastVirtualProvider(Func<int> countProvider, Func<int, object?> rowProvider, bool applyPerformanceProfile = true)
    {
        if (countProvider == null) throw new ArgumentNullException(nameof(countProvider));
        if (rowProvider == null) throw new ArgumentNullException(nameof(rowProvider));
        if (applyPerformanceProfile) ApplyUltimatePerformanceProfile();
        SetVirtualProvider(new DelegateRowProvider(countProvider, rowProvider));
    }

    /// <summary>
    /// Filtre, sort ve kolon cache'lerini tazeler; büyük veride index ön hazırlığını da başlatır.
    /// </summary>
    public void RefreshFastPanoCore(bool prebuildFilterIndexes = true)
    {
        InvalidateDataCaches();
        BuildViewIndex(useAllForHugeMode: false);
        if (prebuildFilterIndexes) PrebuildSmartFilterIndexes();
    }
}
