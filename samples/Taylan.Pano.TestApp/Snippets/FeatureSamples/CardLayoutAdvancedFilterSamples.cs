using Taylan.Pano.Core;
using Taylan.Pano.Columns;

namespace Taylan.Pano.FeatureSamples;

public static class CardLayoutAdvancedFilterSamples
{
    public static void Configure(PanoControl grid)
    {
        grid.FilterMenuMode = Taylan.Pano.Filtering.PanoFilterMenuMode.PopupMenu;
        grid.LockDetailsRowHeightOnSelection = true;
        grid.DetailsRowHeight = 28;
        grid.AutoRowHeightForMultilineCells = false;

        grid.Columns.Add(new PanoColumn("Durum", "Status", 120) { CardRole = "Title", CardOrder = 0 });
        grid.Columns.Add(new PanoColumn("Makine Tipi", "MachineType", 140) { CardRole = "Subtitle", CardOrder = 1 });
        grid.Columns.Add(new PanoColumn("Makine", "Machine", 160) { CardRole = "Body", CardOrder = 2, CardShowCaption = true });
        grid.Columns.Add(new PanoColumn("Oto Kod", "AutoCode", 120) { CardRole = "BadgeText", CardOrder = 3 });

        grid.ResetCardLayoutDefinition();
        // Runtime designer:
        // grid.ShowCardLayoutDesigner();
    }
}

