using ViewGrid.Core;

namespace ViewGrid.FeatureSamples;

public static class GLVMenuIconProfileSamples
{
    public static void ApplyBuiltInMenuIcons(ViewGridControl grid)
    {
        grid.MenuIconMode = ViewGridMenuIconMode.BuiltIn;
        grid.MenuIconSize = ViewGridMenuIconSize.Small16;
        grid.ApplyIconsToMergedUserMenus = true;
        grid.SaveMenuIconPreferencesInUserLayout = true;
        grid.AutoSaveUserLayout = true;
        grid.AutoLoadUserLayout = true;
        grid.UserLayoutKey = "Samples.MenuIcons.MainGrid";
    }

    public static void ApplyCustomMenuIcons(ViewGridControl grid, string iconFolder)
    {
        grid.MenuIconMode = ViewGridMenuIconMode.BuiltInThenCustom;
        grid.MenuIconSize = ViewGridMenuIconSize.Medium20;
        grid.SetCustomMenuIconFolder(iconFolder);
        // Desteklenen dosya adları örnekleri:
        // filter.png, clear_filter.png, sort_asc.png, sort_desc.png,
        // columns.png, layout.png, theme.png, view.png, copy.png, print.png, export.png, group.png, drag.png
    }

    public static void DisableMenuIcons(ViewGridControl grid)
    {
        grid.MenuIconMode = ViewGridMenuIconMode.None;
    }
}
