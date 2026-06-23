using System.ComponentModel;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    [Category("ViewGrid - V38 Performance Pro")]
    [DefaultValue(false)]
    [Description("Büyük veri, medya ve sanal liste senaryoları için performans odaklı ViewGrid davranışlarını açar.")]
    public bool EnablePerformancePro { get; set; }

    [Category("ViewGrid - V38 Performance Pro")]
    [DefaultValue(ViewGridV38PerformancePreset.Balanced)]
    public ViewGridV38PerformancePreset PerformancePreset { get; set; } = ViewGridV38PerformancePreset.Balanced;

    [Category("ViewGrid - V38 Performance Pro")]
    [DefaultValue(true)]
    [Description("Medya/poster görünümlerinde görselleri gerektiğinde yükleme niyetini belirtir.")]
    public bool PerformanceLazyImages { get; set; } = true;

    [Category("ViewGrid - V38 Performance Pro")]
    [DefaultValue(300)]
    [Description("Çok büyük listelerde ilk filtre popup tarama satır limiti.")]
    public int PerformanceInitialFilterScanRows { get; set; } = 300;

    [Category("ViewGrid - V38 Performance Pro")]
    [DefaultValue(100000)]
    [Description("Performance Pro için önerilen büyük veri eşik değeri.")]
    public int PerformanceLargeDataThreshold { get; set; } = 100000;

    [Category("ViewGrid - V38 Performance Pro")]
    [DefaultValue(true)]
    [Description("OnPaint süresini hafif sayaçlarla ölçer. Context menü performans özetinde gösterilir.")]
    public bool EnablePaintPerformanceMetrics { get; set; } = true;

    [Browsable(false)]
    public double LastPaintMilliseconds { get; private set; }

    [Browsable(false)]
    public double AveragePaintMilliseconds { get; private set; }

    [Browsable(false)]
    public long PaintCount { get; private set; }

    public void ApplyV38PerformanceProfile(ViewGridV38PerformancePreset preset)
    {
        EnablePerformancePro = true;
        PerformancePreset = preset;
        FastFilterMenuForHugeLists = true;
        FastFilterMenuInitialScanRows = Math.Max(50, PerformanceInitialFilterScanRows);
        FastFilterPopupPreviewRows = Math.Max(50, PerformanceInitialFilterScanRows);
        AsyncLoadFullFilterValues = true;
        TypedFilterSearchesAllRows = true;

        switch (preset)
        {
            case ViewGridV38PerformancePreset.LargeData:
                SetViewMode(ViewGridMode.DenseList);
                MaxFilterDistinctScanRows = 1_000_000;
                MaxVirtualFilterScanRows = 1_000_000;
                MaxAsyncFilterDistinctScanRows = 1_000_000;
                break;
            case ViewGridV38PerformancePreset.MediaLibrary:
                EnableMediaPro = true;
                EnableMediaLazyLoading = true;
                EnableMediaImageCache = true;
                MediaMemoryCacheLimit = Math.Max(MediaMemoryCacheLimit, 256);
                if (ViewMode != ViewGridMode.Poster && ViewMode != ViewGridMode.Gallery && ViewMode != ViewGridMode.MediaTile && ViewMode != ViewGridMode.FilmStrip)
                    SetViewMode(ViewGridMode.Gallery);
                break;
            case ViewGridV38PerformancePreset.VirtualMillionRows:
                EnableVirtualizationPro = true;
                VirtualizationProTargetRows = Math.Max(VirtualizationProTargetRows, 1_000_000);
                SetViewMode(ViewGridMode.DenseList);
                break;
            case ViewGridV38PerformancePreset.LowMemory:
                MediaMemoryCacheLimit = Math.Min(MediaMemoryCacheLimit, 128);
                MaxEmbeddedFilterVisibleValues = Math.Min(MaxEmbeddedFilterVisibleValues, 1000);
                break;
        }

        RefreshView();
    }

    public IDisposable BeginPerformanceBatch()
    {
        BeginUpdate();
        return new ViewGridPerformanceBatchScope(this);
    }

    internal void RecordPaintPerformance(double elapsedMilliseconds)
    {
        if (!EnablePaintPerformanceMetrics) return;

        LastPaintMilliseconds = elapsedMilliseconds;
        PaintCount++;
        AveragePaintMilliseconds = PaintCount <= 1
            ? elapsedMilliseconds
            : (AveragePaintMilliseconds * 0.90d) + (elapsedMilliseconds * 0.10d);
    }

    public string GetPerformanceSummary()
    {
        return $"Paint: {LastPaintMilliseconds:0.0} ms | Avg: {AveragePaintMilliseconds:0.0} ms | Count: {PaintCount} | Rows: {ViewCount}";
    }

    private sealed class ViewGridPerformanceBatchScope : IDisposable
    {
        private ViewGridControl? _owner;
        public ViewGridPerformanceBatchScope(ViewGridControl owner) => _owner = owner;
        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.EndUpdate();
        }
    }
}
