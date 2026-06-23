using ViewGrid.Core;

namespace ViewGrid.FeatureSamples;

public static class CardCheckBoxLayoutSamples
{
    public static void ApplyVisibleTopLeft(ViewGridControl grid)
    {
        grid.ViewMode = ViewGridMode.DashboardCard;
        grid.TileCheckBoxes = true;
        grid.TileCheckBoxPosition = ViewGridTileCheckBoxPosition.TopLeft;
        grid.TileCheckBoxReserveTextArea = true;
        grid.TileCheckBoxDrawOnTop = true;
        grid.TileCheckBoxShowBackground = true;
        grid.TileCheckBoxHitPadding = 6;
    }

    public static void ApplyCleanHoverMode(ViewGridControl grid)
    {
        grid.ViewMode = ViewGridMode.DashboardCard;
        grid.TileCheckBoxes = true;
        grid.TileCheckBoxPosition = ViewGridTileCheckBoxPosition.TopRight;
        grid.TileCheckBoxReserveTextArea = true;
        grid.TileCheckBoxDrawOnTop = true;
        grid.TileCheckBoxVisibilityMode = ViewGridTileCheckBoxVisibilityMode.CheckedOrHoverOrSelected;
    }
}
