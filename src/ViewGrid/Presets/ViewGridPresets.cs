using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Editing;
using ViewGrid.Formatting;

namespace ViewGrid.Presets;

public static class ViewGridPresets
{
    public static void ApplyAoiFailList(ViewGridControl grid)
    {
        grid.Columns.Clear();
        grid.Columns.Add(new ViewGridColumn("Id", "TestResultId", 70) { ReadOnly = true });
        grid.Columns.Add(new ViewGridColumn("Barkod", "Barcode", 150) { FillFreeSpace = true, Editable = true, MaxLength = 60 });
        grid.Columns.Add(new ViewGridColumn("Makine", "MachineName", 120));
        grid.Columns.Add(new ViewGridColumn("Program", "AssemblyName", 220) { FillFreeSpace = true });
        grid.Columns.Add(new ViewGridColumn("AOI", "AoiResult", 80) { Kind = ViewGridColumnKind.Badge });
        grid.Columns.Add(new ViewGridColumn("AI", "AIResult", 80) { Kind = ViewGridColumnKind.Badge });
        grid.Columns.Add(new ViewGridColumn("Bekleyen", "WaitingDecision", 90) { Kind = ViewGridColumnKind.Numeric, TextAlign = ContentAlignment.MiddleRight });
        grid.Columns.Add(new ViewGridColumn("Tarih", "AoiTestTime", 155) { Kind = ViewGridColumnKind.Date, EditorType = ViewGridCellEditorKind.DateTime });
        grid.FastFilterMenuForHugeLists = true;
        grid.AsyncLoadFullFilterValues = true;
        grid.EnableGrouping = false;
        grid.FrozenColumnCount = 2;
        grid.RowColorAspectName = "AIResult";
        grid.EmptyListMessage = "AOI kaydı bulunamadı";
        grid.ConditionalFormats.Clear();
        grid.ConditionalFormats.Add(new ViewGridConditionalFormat
        {
            Predicate = (row, col, value) => string.Equals(Convert.ToString(value), "FAIL", StringComparison.OrdinalIgnoreCase),
            BackColor = Color.FromArgb(255, 235, 235),
            ForeColor = Color.FromArgb(160, 20, 20)
        });
    }

    public static void ApplyDatabaseEditorDefaults(ViewGridControl grid, string primaryKey = "Id")
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
