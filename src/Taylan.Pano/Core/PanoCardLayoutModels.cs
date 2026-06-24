using System.ComponentModel;
using Taylan.Pano.Columns;

namespace Taylan.Pano.Core;

public enum PanoCardFieldRole
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

public enum PanoCardLayoutDensity
{
    Compact = 0,
    Comfortable = 1,
    Spacious = 2
}

public sealed class PanoCardLayoutField
{
    public string ColumnKey { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public PanoCardFieldRole Role { get; set; } = PanoCardFieldRole.Body;
    public int Order { get; set; }
    public bool Visible { get; set; } = true;
    public bool ShowCaption { get; set; }
    public int MaxLines { get; set; } = 1;
    public Color? AccentColor { get; set; }
    public override string ToString() => string.IsNullOrWhiteSpace(Caption) ? ColumnKey : Caption;
}

public sealed class PanoCardLayoutDefinition
{
    public string Name { get; set; } = "Default";
    public PanoCardLayoutDensity Density { get; set; } = PanoCardLayoutDensity.Comfortable;
    public bool ShowAccentBar { get; set; } = true;
    public bool ShowStatusDot { get; set; } = true;
    public bool AutoBadgesFromBadgeColumns { get; set; } = true;
    public List<PanoCardLayoutField> Fields { get; } = new();

    public static PanoCardLayoutDefinition FromColumns(IEnumerable<PanoColumn> columns)
    {
        var definition = new PanoCardLayoutDefinition();
        int order = 0;
        foreach (PanoColumn column in columns)
        {
            definition.Fields.Add(new PanoCardLayoutField
            {
                ColumnKey = column.LayoutKey,
                Caption = column.Header,
                Order = order,
                Role = order switch
                {
                    0 => PanoCardFieldRole.Title,
                    1 => PanoCardFieldRole.Subtitle,
                    _ => PanoCardFieldRole.Body
                },
                Visible = column.Visible,
                ShowCaption = order > 1
            });
            order++;
        }
        return definition;
    }
}
