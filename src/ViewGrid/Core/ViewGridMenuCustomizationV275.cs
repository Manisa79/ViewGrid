using System.ComponentModel;
using System.Drawing;

namespace ViewGrid.Core;

public enum ViewGridMenuIconTheme
{
    Auto,
    Light,
    Dark
}

[Flags]
public enum ViewGridMenuItemKeys
{
    None = 0,
    FilterPopup = 1 << 0,
    FilterWindow = 1 << 1,
    ClearColumnFilter = 1 << 2,
    ClearAllFilters = 1 << 3,
    AdvancedFilter = 1 << 4,
    FilterPreset = 1 << 5,
    SortAscending = 1 << 6,
    SortDescending = 1 << 7,
    ClearSort = 1 << 8,
    FreezeColumn = 1 << 9,
    AutoSizeColumn = 1 << 10,
    AutoSizeAllColumns = 1 << 11,
    LayoutSaveLoad = 1 << 12,
    Grouping = 1 << 13,
    ColumnChooser = 1 << 14,
    ViewMode = 1 << 15,
    Theme = 1 << 16,
    State = 1 << 17,
    Scenario = 1 << 18,
    Clipboard = 1 << 19,
    Editing = 1 << 20,
    RowDetails = 1 << 21,
    Analytics = 1 << 22,
    ExportPrint = 1 << 23,
    All = FilterPopup | FilterWindow | ClearColumnFilter | ClearAllFilters | AdvancedFilter | FilterPreset | SortAscending | SortDescending | ClearSort | FreezeColumn | AutoSizeColumn | AutoSizeAllColumns | LayoutSaveLoad | Grouping | ColumnChooser | ViewMode | Theme | State | Scenario | Clipboard | Editing | RowDetails | Analytics | ExportPrint
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class ViewGridMenuCustomizationOptions
{
    private readonly ViewGridControl _owner;
    internal ViewGridMenuCustomizationOptions(ViewGridControl owner) => _owner = owner;

    [DefaultValue(true)] public bool ShowBuiltInHeaderMenu { get => _owner.UseBuiltInHeaderMenu; set => _owner.UseBuiltInHeaderMenu = value; }
    [DefaultValue(true)] public bool ShowBuiltInBodyMenu { get => _owner.UseBuiltInBodyMenu; set => _owner.UseBuiltInBodyMenu = value; }
    [DefaultValue(true)] public bool MergeWithUserContextMenu { get => _owner.MergeBuiltInMenuWithUserContextMenu; set => _owner.MergeBuiltInMenuWithUserContextMenu = value; }

    [DefaultValue(ViewGridMenuGroups.All)] public ViewGridMenuGroups HeaderGroups { get => _owner.HeaderMenuGroups; set { _owner.HeaderMenuGroups = value; _owner.MenuProfile = ViewGridMenuProfile.Custom; } }
    [DefaultValue(ViewGridMenuGroups.All)] public ViewGridMenuGroups BodyGroups { get => _owner.BodyMenuGroups; set { _owner.BodyMenuGroups = value; _owner.MenuProfile = ViewGridMenuProfile.Custom; } }
    [DefaultValue(ViewGridMenuGroups.All)] public ViewGridMenuGroups MergedGroups { get => _owner.MergedMenuGroups; set { _owner.MergedMenuGroups = value; _owner.MenuProfile = ViewGridMenuProfile.Custom; } }

    [DefaultValue(ViewGridMenuItemKeys.None)] public ViewGridMenuItemKeys HiddenItems { get => _owner.HiddenMenuItems; set => _owner.HiddenMenuItems = value; }
    [DefaultValue(ViewGridMenuItemKeys.All)] public ViewGridMenuItemKeys VisibleItems { get => _owner.VisibleMenuItems; set => _owner.VisibleMenuItems = value; }

    public override string ToString() => "Menu customization";

    public void ApplyFullProfile()
    {
        _owner.MenuProfile = ViewGridMenuProfile.Full;
        HeaderGroups = ViewGridMenuGroups.All;
        BodyGroups = ViewGridMenuGroups.All;
        MergedGroups = ViewGridMenuGroups.All;
        HiddenItems = ViewGridMenuItemKeys.None;
        VisibleItems = ViewGridMenuItemKeys.All;
    }

    public void ApplyMinimalProfile()
    {
        _owner.MenuProfile = ViewGridMenuProfile.Custom;
        HeaderGroups = ViewGridMenuGroups.Filter | ViewGridMenuGroups.Sort | ViewGridMenuGroups.ColumnChooser;
        BodyGroups = ViewGridMenuGroups.Clipboard | ViewGridMenuGroups.Filter;
        MergedGroups = HeaderGroups | BodyGroups;
    }

    public void ApplyReadOnlyProfile()
    {
        _owner.MenuProfile = ViewGridMenuProfile.ReadOnly;
        HiddenItems |= ViewGridMenuItemKeys.Editing;
    }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class ViewGridMenuIconCustomizationOptions
{
    private readonly ViewGridControl _owner;
    internal ViewGridMenuIconCustomizationOptions(ViewGridControl owner) => _owner = owner;

    [DefaultValue(ViewGridMenuIconMode.BuiltInThenCustom)]
    public ViewGridMenuIconMode Mode { get => _owner.MenuIconMode; set { _owner.MenuIconMode = value; _owner.ClearMenuIconCache(); } }

    [DefaultValue(ViewGridMenuIconSize.Small16)]
    public ViewGridMenuIconSize Size { get => _owner.MenuIconSize; set { _owner.MenuIconSize = value; _owner.ClearMenuIconCache(); } }

    [DefaultValue(ViewGridMenuIconTheme.Auto)]
    public ViewGridMenuIconTheme ThemeMode { get => _owner.MenuIconThemeMode; set { _owner.MenuIconThemeMode = value; _owner.ClearMenuIconCache(); } }

    [DefaultValue("")]
    public string CustomFolder { get => _owner.CustomMenuIconFolder; set => _owner.SetCustomMenuIconFolder(value, saveLayout: false); }

    [DefaultValue(null)]
    public ImageList? ImageList { get => _owner.CustomMenuImageList; set { _owner.CustomMenuImageList = value; _owner.ClearMenuIconCache(); } }

    [DefaultValue(null)]
    public ImageList? DarkImageList { get => _owner.CustomMenuDarkImageList; set { _owner.CustomMenuDarkImageList = value; _owner.ClearMenuIconCache(); } }

    [DefaultValue(null)]
    public ImageList? LightImageList { get => _owner.CustomMenuLightImageList; set { _owner.CustomMenuLightImageList = value; _owner.ClearMenuIconCache(); } }

    public override string ToString() => "Icon customization";
}

public partial class ViewGridControl
{
    private readonly ViewGridMenuCustomizationOptions _menuOptions;
    private readonly ViewGridMenuIconCustomizationOptions _menuIcons;

    [Category("ViewGrid - Context Menu")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Description("v27.5: Header/body/merged menü gruplarını ve item bazlı görünürlüğü tek yerden yönetir.")]
    public ViewGridMenuCustomizationOptions MenuOptions => _menuOptions;

    [Category("ViewGrid - Menu Icons")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Description("v27.5: Built-in veya kullanıcı ikonlarını folder/ImageList/light-dark ayrımıyla yönetir.")]
    public ViewGridMenuIconCustomizationOptions MenuIcons => _menuIcons;

    [Category("ViewGrid - Context Menu")]
    [DefaultValue(ViewGridMenuItemKeys.All)]
    [Description("v27.5: Menü item bazlı izin listesi. All varsayılan; örn. AdvancedFilter çıkarılabilir.")]
    public ViewGridMenuItemKeys VisibleMenuItems { get; set; } = ViewGridMenuItemKeys.All;

    [Category("ViewGrid - Context Menu")]
    [DefaultValue(ViewGridMenuItemKeys.None)]
    [Description("v27.5: Menü item bazlı gizleme listesi. VisibleMenuItems ile birlikte değerlendirilir.")]
    public ViewGridMenuItemKeys HiddenMenuItems { get; set; } = ViewGridMenuItemKeys.None;

    [Category("ViewGrid - Menu Icons")]
    [DefaultValue(null)]
    [Description("v27.5: Menü ikonları için ImageList. Key isimleri: filter, sort_asc, advanced_filter, state, scenario, export...")]
    public ImageList? CustomMenuImageList { get; set; }

    [Category("ViewGrid - Menu Icons")]
    [DefaultValue(null)]
    public ImageList? CustomMenuDarkImageList { get; set; }

    [Category("ViewGrid - Menu Icons")]
    [DefaultValue(null)]
    public ImageList? CustomMenuLightImageList { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Description("v27.5: SVG veya özel ikon kaynakları için kullanıcı resolver. Key: filter, sort_asc, advanced_filter vb.")]
    public Func<string, Image?>? CustomMenuIconResolver { get; set; }

    [Category("ViewGrid - Menu Icons")]
    [DefaultValue(ViewGridMenuIconTheme.Auto)]
    public ViewGridMenuIconTheme MenuIconThemeMode { get; set; } = ViewGridMenuIconTheme.Auto;

    public void SetMenuItemVisible(ViewGridMenuItemKeys item, bool visible)
    {
        if (visible)
        {
            VisibleMenuItems |= item;
            HiddenMenuItems &= ~item;
        }
        else
        {
            HiddenMenuItems |= item;
        }
        MenuProfile = ViewGridMenuProfile.Custom;
    }

    public void SetMenuGroupVisible(ViewGridMenuGroups group, bool visible, bool header = true, bool body = true, bool merged = true)
    {
        ShowMenuGroup(group, visible, header, body);
        if (merged) MergedMenuGroups = visible ? MergedMenuGroups | group : MergedMenuGroups & ~group;
    }

    public void SetCustomMenuIconImageList(ImageList? imageList, ImageList? darkImageList = null, ImageList? lightImageList = null)
    {
        CustomMenuImageList = imageList;
        CustomMenuDarkImageList = darkImageList;
        CustomMenuLightImageList = lightImageList;
        MenuIconMode = ViewGridMenuIconMode.BuiltInThenCustom;
        ClearMenuIconCache();
    }

    private bool IsMenuItemVisibleV275(ViewGridMenuItemKeys item)
    {
        if (item == ViewGridMenuItemKeys.None) return true;
        if ((HiddenMenuItems & item) != 0) return false;
        return (VisibleMenuItems & item) != 0;
    }

    private void ApplyMenuCustomizationV275(ContextMenuStrip menu)
    {
        ApplyMenuCustomizationV275(menu.Items);
        RemoveRedundantMenuSeparators(menu);
    }

    private void ApplyMenuCustomizationV275(ToolStripItemCollection items)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            ToolStripItem item = items[i];
            if (item is ToolStripMenuItem mi)
            {
                ApplyMenuCustomizationV275(mi.DropDownItems);
                ViewGridMenuItemKeys key = ResolveMenuItemKeyV275(mi.Text);
                if (!IsMenuItemVisibleV275(key) || (mi.DropDownItems.Count == 0 && key == ViewGridMenuItemKeys.None && mi.Text.StartsWith("__", StringComparison.Ordinal)))
                {
                    items.RemoveAt(i);
                    try { item.Dispose(); } catch { }
                }
            }
        }
        RemoveRedundantMenuSeparators(items);
    }

    private static ViewGridMenuItemKeys ResolveMenuItemKeyV275(string? text)
    {
        string t = (text ?? string.Empty).ToLowerInvariant();
        if (t.Contains("gelişmiş filtre") || t.Contains("advanced filter")) return ViewGridMenuItemKeys.AdvancedFilter;
        if (t.Contains("preset")) return ViewGridMenuItemKeys.FilterPreset;
        if (t.Contains("ayrı filtre") || t.Contains("filter window") || t.Contains("pencere")) return ViewGridMenuItemKeys.FilterWindow;
        if (t.Contains("filtre") || t.Contains("filter"))
        {
            if (t.Contains("temiz") || t.Contains("clear")) return t.Contains("kolon") || t.Contains("column") ? ViewGridMenuItemKeys.ClearColumnFilter : ViewGridMenuItemKeys.ClearAllFilters;
            return ViewGridMenuItemKeys.FilterPopup;
        }
        if (t.Contains("artan") || t.Contains("ascending") || t.Contains("a-z")) return ViewGridMenuItemKeys.SortAscending;
        if (t.Contains("azalan") || t.Contains("descending") || t.Contains("z-a")) return ViewGridMenuItemKeys.SortDescending;
        if (t.Contains("sıralamayı temiz") || t.Contains("clear sort") || t.Contains("unsort")) return ViewGridMenuItemKeys.ClearSort;
        if (t.Contains("sabitle") || t.Contains("frozen") || t.Contains("freeze")) return ViewGridMenuItemKeys.FreezeColumn;
        if (t.Contains("bu kolonu sığdır") || t.Contains("auto size column")) return ViewGridMenuItemKeys.AutoSizeColumn;
        if (t.Contains("sığdır") || t.Contains("width") || t.Contains("genişlik")) return ViewGridMenuItemKeys.AutoSizeAllColumns;
        if (t.Contains("layout") || t.Contains("düzen") || t.Contains("profil")) return ViewGridMenuItemKeys.LayoutSaveLoad;
        if (t.Contains("grup") || t.Contains("group")) return ViewGridMenuItemKeys.Grouping;
        if (t.Contains("kolon") || t.Contains("column")) return ViewGridMenuItemKeys.ColumnChooser;
        if (t.Contains("görün") || t.Contains("view")) return ViewGridMenuItemKeys.ViewMode;
        if (t.Contains("tema") || t.Contains("theme")) return ViewGridMenuItemKeys.Theme;
        if (t.Contains("state") || t.Contains("durum kaydet")) return ViewGridMenuItemKeys.State;
        if (t.Contains("senaryo") || t.Contains("scenario")) return ViewGridMenuItemKeys.Scenario;
        if (t.Contains("kopya") || t.Contains("copy") || t.Contains("paste") || t.Contains("yapıştır")) return ViewGridMenuItemKeys.Clipboard;
        if (t.Contains("düzenle") || t.Contains("edit")) return ViewGridMenuItemKeys.Editing;
        if (t.Contains("detay") || t.Contains("detail")) return ViewGridMenuItemKeys.RowDetails;
        if (t.Contains("analiz") || t.Contains("analytics")) return ViewGridMenuItemKeys.Analytics;
        if (t.Contains("export") || t.Contains("dışa aktar") || t.Contains("yazdır") || t.Contains("print")) return ViewGridMenuItemKeys.ExportPrint;
        return ViewGridMenuItemKeys.None;
    }
}
