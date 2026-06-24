using Taylan.Pano.Columns;
using Taylan.Pano.Core;

namespace Taylan.Pano.Exporting;

public enum PanoPdfExportMode
{
    Auto = 0,
    Table = 1,
    Card = 2
}

public enum PanoPdfPageOrientation
{
    Portrait = 0,
    Landscape = 1
}

public enum PanoPdfThemeMode
{
    PrintFriendly = 0,
    PreserveGridTheme = 1
}

public sealed class PanoPdfExportOptions
{
    public string Title { get; set; } = "Pano Export";
    public string? Subtitle { get; set; }
    public PanoPdfExportMode Mode { get; set; } = PanoPdfExportMode.Auto;
    public PanoPdfPageOrientation Orientation { get; set; } = PanoPdfPageOrientation.Landscape;
    public PanoPdfThemeMode ThemeMode { get; set; } = PanoPdfThemeMode.PrintFriendly;
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
    public Func<object, PanoCardVisualInfo?>? CardVisualInfoResolver { get; set; }
    public PanoCardLayoutDefinition? CardLayout { get; set; }
    public Func<PanoColumn, bool>? ColumnPredicate { get; set; }
}
