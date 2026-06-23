using ViewGrid;
using ViewGrid.Core;
using ViewGrid.Columns;
using ViewGrid.Filtering;
using System.Windows.Forms;

namespace ViewGrid.TestApp.Samples;

public static class GLVMenuVisibilityAndFilterStyleSamples
{
    public static FastViewGridControl Create()
    {
        var grid = new FastViewGridControl
        {
            Dock = DockStyle.Fill,
            MergeBuiltInMenuWithUserContextMenu = true,
            BuiltInMenuMergeText = "Liste özellikleri",
            ShowHeaderMenuFilterItems = true,
            ShowHeaderMenuFilterStyleItems = true,
            ShowHeaderMenuSortItems = true,
            ShowHeaderMenuAutoSizeItems = true,
            ShowHeaderMenuColumnChooserItem = true,
            ShowHeaderMenuLayoutItems = true,
            ShowHeaderMenuGroupingItems = true,
            ShowHeaderMenuThemeItems = true,
            FilterMenuMode = ViewGridFilterMenuMode.PopupMenu,
            AutoSaveUserLayout = true,
            AutoLoadUserLayout = true,
            UserLayoutKey = "Samples.MenuVisibilityAndFilterStyle"
        };

        grid.Columns.AddRange(
            new GLVColumn { Header = "Kod", AspectName = "Code", Width = 120, AllowFilter = true, AllowSort = true },
            new GLVColumn { Header = "Açıklama", AspectName = "Description", Width = 240, AllowFilter = true, AllowSort = true },
            new GLVColumn { Header = "Durum", AspectName = "Status", Width = 100, AllowFilter = true, AllowSort = false }
        );

        var userMenu = new ContextMenuStrip();
        userMenu.Items.Add("Kullanıcı menüsü: Barkodu kopyala");
        userMenu.Items.Add("Kullanıcı menüsü: Özel işlem");
        grid.ContextMenuStrip = userMenu;

        return grid;
    }
}
