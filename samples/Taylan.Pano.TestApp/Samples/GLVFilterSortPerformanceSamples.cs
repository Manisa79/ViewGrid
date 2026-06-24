using System.Windows.Forms;
using Taylan.Pano;
using Taylan.Pano.Columns;

namespace Taylan.Pano.TestApp.Samples;

public static class GLVFilterSortPerformanceSamples
{
    public static FastPanoControl CreateFilterSortPerformanceGrid()
    {
        var grid = new FastPanoControl
        {
            Dock = DockStyle.Fill,
            ShowColumnFilterButtons = true,
            ShowQuickClearFilterInHeaderMenu = true,
            AsyncSortForLargeLists = true,
            CacheSortKeysForLargeLists = true,
            AsyncSortThreshold = 10000,
            AsyncSortMaxRows = 0,
            ShowSortBusyIndicator = true,
            FastFilterMenuForHugeLists = true,
            FastFilterMenuInitialScanRows = 300,
            FastFilterMenuSearchScanRows = 1_000_000,
            TypedFilterSearchesAllRows = true,
            MaxEmbeddedFilterVisibleValues = 2_000,
            AsyncLoadFullFilterValues = true,
            SmartFilterSearchAllRows = true
        };

        grid.Columns.Add(new GLVColumn { Header = "Barcode", AspectName = "Barcode", Width = 160, AllowFilter = true, AllowSort = true });
        grid.Columns.Add(new GLVColumn { Header = "Machine", AspectName = "Machine", Width = 130, AllowFilter = true, AllowSort = true });
        grid.Columns.Add(new GLVColumn { Header = "Result", AspectName = "Result", Width = 100, AllowFilter = true, AllowSort = true });
        grid.Columns.Add(new GLVColumn { Header = "Time", AspectName = "Time", Width = 150, AllowFilter = true, AllowSort = true });

        return grid;
    }
}

