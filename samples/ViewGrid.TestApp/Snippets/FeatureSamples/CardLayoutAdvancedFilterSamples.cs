using ViewGrid.Core;
using ViewGrid.Columns;

namespace ViewGrid.FeatureSamples;

public static class CardLayoutAdvancedFilterSamples
{
    public static void Configure(ViewGridControl grid)
    {
        grid.FilterMenuMode = ViewGrid.Filtering.ViewGridFilterMenuMode.PopupMenu;
        grid.LockDetailsRowHeightOnSelection = true;
        grid.DetailsRowHeight = 28;
        grid.AutoRowHeightForMultilineCells = false;

        grid.Columns.Add(new ViewGridColumn("Durum", "Status", 120) { CardRole = "Title", CardOrder = 0 });
        grid.Columns.Add(new ViewGridColumn("Makine Tipi", "MachineType", 140) { CardRole = "Subtitle", CardOrder = 1 });
        grid.Columns.Add(new ViewGridColumn("Makine", "Machine", 160) { CardRole = "Body", CardOrder = 2, CardShowCaption = true });
        grid.Columns.Add(new ViewGridColumn("Oto Kod", "AutoCode", 120) { CardRole = "BadgeText", CardOrder = 3 });

        grid.ResetCardLayoutDefinition();
        // Runtime designer:
        // grid.ShowCardLayoutDesigner();
    }
}
