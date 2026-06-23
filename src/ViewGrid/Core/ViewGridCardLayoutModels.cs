using System.ComponentModel;
using ViewGrid.Columns;

namespace ViewGrid.Core;

public enum ViewGridCardFieldRole
{
    Title = 0,
    Subtitle = 1,
    Body = 2,
    Footer = 3,
    BadgeText = 4,
    BadgeColor = 5,
    AccentColor = 6,
    StatusText = 7,
    Hidden = 8
}

public enum ViewGridCardLayoutDensity
{
    Compact = 0,
    Comfortable = 1,
    Spacious = 2
}

public sealed class ViewGridCardLayoutField
{
    public string ColumnKey { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public ViewGridCardFieldRole Role { get; set; } = ViewGridCardFieldRole.Body;
    public int Order { get; set; }
    public bool Visible { get; set; } = true;
    public bool ShowCaption { get; set; }
    public int MaxLines { get; set; } = 1;
    public Color? AccentColor { get; set; }
    public override string ToString() => string.IsNullOrWhiteSpace(Caption) ? ColumnKey : Caption;
}

public sealed class ViewGridCardLayoutDefinition
{
    public string Name { get; set; } = "Default";
    public ViewGridCardLayoutDensity Density { get; set; } = ViewGridCardLayoutDensity.Comfortable;
    public bool ShowAccentBar { get; set; } = true;
    public bool ShowStatusDot { get; set; } = true;
    public bool AutoBadgesFromBadgeColumns { get; set; } = true;
    public List<ViewGridCardLayoutField> Fields { get; } = new();

    public static ViewGridCardLayoutDefinition FromColumns(IEnumerable<ViewGridColumn> columns)
    {
        var definition = new ViewGridCardLayoutDefinition();
        int order = 0;
        foreach (ViewGridColumn column in columns)
        {
            definition.Fields.Add(new ViewGridCardLayoutField
            {
                ColumnKey = column.LayoutKey,
                Caption = column.Header,
                Order = order,
                Role = order switch
                {
                    0 => ViewGridCardFieldRole.Title,
                    1 => ViewGridCardFieldRole.Subtitle,
                    _ => ViewGridCardFieldRole.Body
                },
                Visible = column.Visible,
                ShowCaption = order > 1
            });
            order++;
        }
        return definition;
    }
}
