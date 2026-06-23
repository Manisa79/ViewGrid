using ViewGrid.Core;

namespace ViewGrid.FeatureSamples;

public static class FilterPopupPolishSamples
{
    public static void ApplyFilterPopupPolish(ViewGridControl grid)
    {
        grid.FilterMenuMode = ViewGrid.Filtering.ViewGridFilterMenuMode.PopupMenu;
        grid.FilterPopupResizable = true;
        grid.FilterPopupEdgeResize = true;
        grid.FilterPopupRememberSize = true;
        grid.FilterPopupShowActionIcons = true;
        grid.FilterPopupHighlightActiveCommands = true;
        grid.FilterPopupDefaultSize = new Size(520, 560);
        grid.FilterPopupMinimumSize = new Size(340, 380);
    }
}
