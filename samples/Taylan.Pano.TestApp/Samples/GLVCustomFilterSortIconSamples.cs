using Taylan.Pano;
using System.Drawing;
using Taylan.Pano.Columns;
using Taylan.Pano.Core;

namespace Taylan.Pano.TestApp.Samples;

public static class GLVCustomFilterSortIconSamples
{
    public static void Apply(FastPanoControl grid, GLVColumn barcodeColumn)
    {
        // Eski nokta benzeri filtre simgesi
        grid.FilterIconStyle = PanoFilterIconStyle.Dot;

        // Yeni sort simgesi
        grid.ShowColumnSortGlyphs = true;
        grid.SortGlyphStyle = PanoSortGlyphStyle.Chevron;

        // Büyük listelerde UI donmasını azaltır
        grid.AsyncSortForLargeLists = true;
        grid.AsyncSortThreshold = 50000;
        grid.ShowSortBusyIndicator = true;

        // Kolon bazlı override
        barcodeColumn.FilterIconStyle = PanoFilterIconStyle.Funnel;
        barcodeColumn.SortGlyphStyle = PanoSortGlyphStyle.Triangle;

        // Kendi ikonunu vermek istersen:
        // grid.FilterIconStyle = PanoFilterIconStyle.CustomImage;
        // grid.CustomFilterIcon = Image.FromFile("filter.png");
        // grid.SortGlyphStyle = PanoSortGlyphStyle.CustomImage;
        // grid.CustomSortAscendingIcon = Image.FromFile("sort_asc.png");
        // grid.CustomSortDescendingIcon = Image.FromFile("sort_desc.png");
    }
}

