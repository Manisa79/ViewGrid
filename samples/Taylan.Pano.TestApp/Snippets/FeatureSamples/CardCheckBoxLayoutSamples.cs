using Taylan.Pano.Core;

namespace Taylan.Pano.FeatureSamples;

public static class CardCheckBoxLayoutSamples
{
    public static void ApplyVisibleTopLeft(PanoControl grid)
    {
        grid.ViewMode = PanoViewMode.DashboardCard;
        grid.TileCheckBoxes = true;
        grid.TileCheckBoxPosition = PanoTileCheckBoxPosition.TopLeft;
        grid.TileCheckBoxReserveTextArea = true;
        grid.TileCheckBoxDrawOnTop = true;
        grid.TileCheckBoxShowBackground = true;
        grid.TileCheckBoxHitPadding = 6;
    }

    public static void ApplyCleanHoverMode(PanoControl grid)
    {
        grid.ViewMode = PanoViewMode.DashboardCard;
        grid.TileCheckBoxes = true;
        grid.TileCheckBoxPosition = PanoTileCheckBoxPosition.TopRight;
        grid.TileCheckBoxReserveTextArea = true;
        grid.TileCheckBoxDrawOnTop = true;
        grid.TileCheckBoxVisibilityMode = PanoTileCheckBoxVisibilityMode.CheckedOrHoverOrSelected;
    }
}
