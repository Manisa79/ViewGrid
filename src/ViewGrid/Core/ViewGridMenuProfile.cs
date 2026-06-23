using System;

namespace ViewGrid.Core;

public enum ViewGridMenuProfile
{
    None,
    Full,
    Standard,
    Minimal,
    ReadOnly,
    Custom
}

[Flags]
public enum ViewGridMenuGroups
{
    None = 0,
    Filter = 1 << 0,
    Sort = 1 << 1,
    Freeze = 1 << 2,
    AutoSize = 1 << 3,
    Layout = 1 << 4,
    Grouping = 1 << 5,
    ColumnChooser = 1 << 6,
    Mode = 1 << 7,
    ViewMode = 1 << 8,
    FilterStyle = 1 << 9,
    Theme = 1 << 10,
    Clipboard = 1 << 11,
    Editing = 1 << 12,
    RowDetails = 1 << 13,
    Analytics = 1 << 14,
    State = 1 << 15,
    Scenario = 1 << 16,
    All = Filter | Sort | Freeze | AutoSize | Layout | Grouping | ColumnChooser | Mode | ViewMode | FilterStyle | Theme | Clipboard | Editing | RowDetails | Analytics | State | Scenario
}
public enum ViewGridMenuMergePlacement
{
    Bottom,
    Top,
    BeforeFirstSeparator,
    AfterFirstSeparator
}

public enum ViewGridMenuMergePresentation
{
    SubMenu,
    Inline,
    GroupedSubMenus
}

