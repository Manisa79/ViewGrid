using ViewGrid.Columns;
using ViewGrid.Core;

namespace ViewGrid.Exporting;

public enum ViewGridPdfExportMode
{
    Auto = 0,
    Table = 1,
    Card = 2
}

public enum ViewGridPdfPageOrientation
{
    Portrait = 0,
    Landscape = 1
}

public enum ViewGridPdfThemeMode
{
    PrintFriendly = 0,
    PreserveGridTheme = 1
}

public sealed class ViewGridPdfExportOptions
{
    public string Title { get; set; } = "ViewGrid Export";
    public string? Subtitle { get; set; }
    public ViewGridPdfExportMode Mode { get; set; } = ViewGridPdfExportMode.Auto;
    public ViewGridPdfPageOrientation Orientation { get; set; } = ViewGridPdfPageOrientation.Landscape;
    public ViewGridPdfThemeMode ThemeMode { get; set; } = ViewGridPdfThemeMode.PrintFriendly;
    public bool FitToPageWidth { get; set; } = true;
    public bool ShowGridLines { get; set; } = true;
    public bool ZebraRows { get; set; } = true;
    public bool ShowHeader { get; set; } = true;
    public bool ShowFooter { get; set; } = true;
    public bool ShowFilterSummary { get; set; } = true;
    public bool IncludeHiddenColumns { get; set; }
    public int MaxRows { get; set; } = 50000;
    public int CardColumns { get; set; } = 2;
    public int CardMinHeight { get; set; } = 92;
    public int CardGap { get; set; } = 10;
    public int MarginLeft { get; set; } = 36;
    public int MarginTop { get; set; } = 36;
    public int MarginRight { get; set; } = 36;
    public int MarginBottom { get; set; } = 36;
    public string? FooterText { get; set; }
    public Func<object, ViewGridCardVisualInfo?>? CardVisualInfoResolver { get; set; }
    public ViewGridCardLayoutDefinition? CardLayout { get; set; }
    public Func<ViewGridColumn, bool>? ColumnPredicate { get; set; }
}
