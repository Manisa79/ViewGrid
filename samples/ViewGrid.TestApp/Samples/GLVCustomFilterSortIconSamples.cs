using ViewGrid;
using System.Drawing;
using ViewGrid.Columns;
using ViewGrid.Core;

namespace ViewGrid.TestApp.Samples;

public static class GLVCustomFilterSortIconSamples
{
    public static void Apply(FastViewGridControl grid, GLVColumn barcodeColumn)
    {
        // Eski nokta benzeri filtre simgesi
        grid.FilterIconStyle = ViewGridFilterIconStyle.Dot;

        // Yeni sort simgesi
        grid.ShowColumnSortGlyphs = true;
        grid.SortGlyphStyle = ViewGridSortGlyphStyle.Chevron;

        // Büyük listelerde UI donmasını azaltır
        grid.AsyncSortForLargeLists = true;
        grid.AsyncSortThreshold = 50000;
        grid.ShowSortBusyIndicator = true;

        // Kolon bazlı override
        barcodeColumn.FilterIconStyle = ViewGridFilterIconStyle.Funnel;
        barcodeColumn.SortGlyphStyle = ViewGridSortGlyphStyle.Triangle;

        // Kendi ikonunu vermek istersen:
        // grid.FilterIconStyle = ViewGridFilterIconStyle.CustomImage;
        // grid.CustomFilterIcon = Image.FromFile("filter.png");
        // grid.SortGlyphStyle = ViewGridSortGlyphStyle.CustomImage;
        // grid.CustomSortAscendingIcon = Image.FromFile("sort_asc.png");
        // grid.CustomSortDescendingIcon = Image.FromFile("sort_desc.png");
    }
}
