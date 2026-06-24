using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Editing;
using Taylan.Pano.Formatting;

namespace Taylan.Pano.Presets;

public static class PanoPresets
{
    public static void ApplyAoiFailList(PanoControl grid)
    {
        grid.Columns.Clear();
        grid.Columns.Add(new PanoColumn("Id", "TestResultId", 70) { ReadOnly = true });
        grid.Columns.Add(new PanoColumn("Barkod", "Barcode", 150) { FillFreeSpace = true, Editable = true, MaxLength = 60 });
        grid.Columns.Add(new PanoColumn("Makine", "MachineName", 120));
        grid.Columns.Add(new PanoColumn("Program", "AssemblyName", 220) { FillFreeSpace = true });
        grid.Columns.Add(new PanoColumn("AOI", "AoiResult", 80) { Kind = PanoColumnKind.Badge });
        grid.Columns.Add(new PanoColumn("AI", "AIResult", 80) { Kind = PanoColumnKind.Badge });
        grid.Columns.Add(new PanoColumn("Bekleyen", "WaitingDecision", 90) { Kind = PanoColumnKind.Numeric, TextAlign = ContentAlignment.MiddleRight });
        grid.Columns.Add(new PanoColumn("Tarih", "AoiTestTime", 155) { Kind = PanoColumnKind.Date, EditorType = PanoCellEditorKind.DateTime });
        grid.FastFilterMenuForHugeLists = true;
        grid.AsyncLoadFullFilterValues = true;
        grid.EnableGrouping = false;
        grid.FrozenColumnCount = 2;
        grid.RowColorAspectName = "AIResult";
        grid.EmptyListMessage = "AOI kaydı bulunamadı";
        grid.ConditionalFormats.Clear();
        grid.ConditionalFormats.Add(new PanoConditionalFormat
        {
            Predicate = (row, col, value) => string.Equals(Convert.ToString(value), "FAIL", StringComparison.OrdinalIgnoreCase),
            BackColor = Color.FromArgb(255, 235, 235),
            ForeColor = Color.FromArgb(160, 20, 20)
        });
    }

    public static void ApplyDatabaseEditorDefaults(PanoControl grid, string primaryKey = "Id")
    {
        grid.AutoGenerateColumns = true;
        grid.PrimaryKey = primaryKey;
        grid.EnableCellEditing = true;
        grid.AllowEditAllCells = true;
        grid.EnableInlineDatabaseEditing = true;
        grid.ShowGridLines = true;
        grid.FastFilterMenuForHugeLists = true;
        grid.AsyncLoadFullFilterValues = true;
        grid.EnableModernEmptyState = true;
    }
}
