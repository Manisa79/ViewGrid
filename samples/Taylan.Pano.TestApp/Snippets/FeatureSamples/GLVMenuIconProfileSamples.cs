using Taylan.Pano.Core;

namespace Taylan.Pano.FeatureSamples;

public static class GLVMenuIconProfileSamples
{
    public static void ApplyBuiltInMenuIcons(PanoControl grid)
    {
        grid.MenuIconMode = PanoMenuIconMode.BuiltIn;
        grid.MenuIconSize = PanoMenuIconSize.Small16;
        grid.ApplyIconsToMergedUserMenus = true;
        grid.SaveMenuIconPreferencesInUserLayout = true;
        grid.AutoSaveUserLayout = true;
        grid.AutoLoadUserLayout = true;
        grid.UserLayoutKey = "Samples.MenuIcons.MainGrid";
    }

    public static void ApplyCustomMenuIcons(PanoControl grid, string iconFolder)
    {
        grid.MenuIconMode = PanoMenuIconMode.BuiltInThenCustom;
        grid.MenuIconSize = PanoMenuIconSize.Medium20;
        grid.SetCustomMenuIconFolder(iconFolder);
        // Desteklenen dosya adları örnekleri:
        // filter.png, clear_filter.png, sort_asc.png, sort_desc.png,
        // columns.png, layout.png, theme.png, view.png, copy.png, print.png, export.png, group.png, drag.png
    }

    public static void DisableMenuIcons(PanoControl grid)
    {
        grid.MenuIconMode = PanoMenuIconMode.None;
    }
}
