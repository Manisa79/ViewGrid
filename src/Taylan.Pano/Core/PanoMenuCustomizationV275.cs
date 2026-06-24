using System.ComponentModel;
using System.Drawing;

namespace Taylan.Pano.Core;

public enum PanoMenuIconTheme
{
    Auto,
    Light,
    Dark
}

[Flags]
public enum PanoMenuItemKeys
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
public sealed class PanoMenuCustomizationOptions
{
    private readonly PanoControl _owner;
    internal PanoMenuCustomizationOptions(PanoControl owner) => _owner = owner;

    [DefaultValue(true)] public bool ShowBuiltInHeaderMenu { get => _owner.UseBuiltInHeaderMenu; set => _owner.UseBuiltInHeaderMenu = value; }
    [DefaultValue(true)] public bool ShowBuiltInBodyMenu { get => _owner.UseBuiltInBodyMenu; set => _owner.UseBuiltInBodyMenu = value; }
    [DefaultValue(true)] public bool MergeWithUserContextMenu { get => _owner.MergeBuiltInMenuWithUserContextMenu; set => _owner.MergeBuiltInMenuWithUserContextMenu = value; }

    [DefaultValue(PanoMenuGroups.All)] public PanoMenuGroups HeaderGroups { get => _owner.HeaderMenuGroups; set { _owner.HeaderMenuGroups = value; _owner.MenuProfile = PanoMenuProfile.Custom; } }
    [DefaultValue(PanoMenuGroups.All)] public PanoMenuGroups BodyGroups { get => _owner.BodyMenuGroups; set { _owner.BodyMenuGroups = value; _owner.MenuProfile = PanoMenuProfile.Custom; } }
    [DefaultValue(PanoMenuGroups.All)] public PanoMenuGroups MergedGroups { get => _owner.MergedMenuGroups; set { _owner.MergedMenuGroups = value; _owner.MenuProfile = PanoMenuProfile.Custom; } }

    [DefaultValue(PanoMenuItemKeys.None)] public PanoMenuItemKeys HiddenItems { get => _owner.HiddenMenuItems; set => _owner.HiddenMenuItems = value; }
    [DefaultValue(PanoMenuItemKeys.All)] public PanoMenuItemKeys VisibleItems { get => _owner.VisibleMenuItems; set => _owner.VisibleMenuItems = value; }

    public override string ToString() => "Menu customization";

    public void ApplyFullProfile()
    {
        _owner.MenuProfile = PanoMenuProfile.Full;
        HeaderGroups = PanoMenuGroups.All;
        BodyGroups = PanoMenuGroups.All;
        MergedGroups = PanoMenuGroups.All;
        HiddenItems = PanoMenuItemKeys.None;
        VisibleItems = PanoMenuItemKeys.All;
    }

    public void ApplyMinimalProfile()
    {
        _owner.MenuProfile = PanoMenuProfile.Custom;
        HeaderGroups = PanoMenuGroups.Filter | PanoMenuGroups.Sort | PanoMenuGroups.ColumnChooser;
        BodyGroups = PanoMenuGroups.Clipboard | PanoMenuGroups.Filter;
        MergedGroups = HeaderGroups | BodyGroups;
    }

    public void ApplyReadOnlyProfile()
    {
        _owner.MenuProfile = PanoMenuProfile.ReadOnly;
        HiddenItems |= PanoMenuItemKeys.Editing;
    }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class PanoMenuIconCustomizationOptions
{
    private readonly PanoControl _owner;
    internal PanoMenuIconCustomizationOptions(PanoControl owner) => _owner = owner;

    [DefaultValue(PanoMenuIconMode.BuiltInThenCustom)]
    public PanoMenuIconMode Mode { get => _owner.MenuIconMode; set { _owner.MenuIconMode = value; _owner.ClearMenuIconCache(); } }

    [DefaultValue(PanoMenuIconSize.Small16)]
    public PanoMenuIconSize Size { get => _owner.MenuIconSize; set { _owner.MenuIconSize = value; _owner.ClearMenuIconCache(); } }

    [DefaultValue(PanoMenuIconTheme.Auto)]
    public PanoMenuIconTheme ThemeMode { get => _owner.MenuIconThemeMode; set { _owner.MenuIconThemeMode = value; _owner.ClearMenuIconCache(); } }

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

public partial class PanoControl
{
    private readonly PanoMenuCustomizationOptions _menuOptions;
    private readonly PanoMenuIconCustomizationOptions _menuIcons;

    [Category("Pano - Context Menu")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Description("v27.5: Header/body/merged menü gruplarını ve item bazlı görünürlüğü tek yerden yönetir.")]
    public PanoMenuCustomizationOptions MenuOptions => _menuOptions;

    [Category("Pano - Menu Icons")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Description("v27.5: Built-in veya kullanıcı ikonlarını folder/ImageList/light-dark ayrımıyla yönetir.")]
    public PanoMenuIconCustomizationOptions MenuIcons => _menuIcons;

    [Category("Pano - Context Menu")]
    [DefaultValue(PanoMenuItemKeys.All)]
    [Description("v27.5: Menü item bazlı izin listesi. All varsayılan; örn. AdvancedFilter çıkarılabilir.")]
    public PanoMenuItemKeys VisibleMenuItems { get; set; } = PanoMenuItemKeys.All;

    [Category("Pano - Context Menu")]
    [DefaultValue(PanoMenuItemKeys.None)]
    [Description("v27.5: Menü item bazlı gizleme listesi. VisibleMenuItems ile birlikte değerlendirilir.")]
    public PanoMenuItemKeys HiddenMenuItems { get; set; } = PanoMenuItemKeys.None;

    [Category("Pano - Menu Icons")]
    [DefaultValue(null)]
    [Description("v27.5: Menü ikonları için ImageList. Key isimleri: filter, sort_asc, advanced_filter, state, scenario, export...")]
    public ImageList? CustomMenuImageList { get; set; }

    [Category("Pano - Menu Icons")]
    [DefaultValue(null)]
    public ImageList? CustomMenuDarkImageList { get; set; }

    [Category("Pano - Menu Icons")]
    [DefaultValue(null)]
    public ImageList? CustomMenuLightImageList { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Description("v27.5: SVG veya özel ikon kaynakları için kullanıcı resolver. Key: filter, sort_asc, advanced_filter vb.")]
    public Func<string, Image?>? CustomMenuIconResolver { get; set; }

    [Category("Pano - Menu Icons")]
    [DefaultValue(PanoMenuIconTheme.Auto)]
    public PanoMenuIconTheme MenuIconThemeMode { get; set; } = PanoMenuIconTheme.Auto;

    public void SetMenuItemVisible(PanoMenuItemKeys item, bool visible)
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
        MenuProfile = PanoMenuProfile.Custom;
    }

    public void SetMenuGroupVisible(PanoMenuGroups group, bool visible, bool header = true, bool body = true, bool merged = true)
    {
        ShowMenuGroup(group, visible, header, body);
        if (merged) MergedMenuGroups = visible ? MergedMenuGroups | group : MergedMenuGroups & ~group;
    }

    public void SetCustomMenuIconImageList(ImageList? imageList, ImageList? darkImageList = null, ImageList? lightImageList = null)
    {
        CustomMenuImageList = imageList;
        CustomMenuDarkImageList = darkImageList;
        CustomMenuLightImageList = lightImageList;
        MenuIconMode = PanoMenuIconMode.BuiltInThenCustom;
        ClearMenuIconCache();
    }

    private bool IsMenuItemVisibleV275(PanoMenuItemKeys item)
    {
        if (item == PanoMenuItemKeys.None) return true;
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
                PanoMenuItemKeys key = ResolveMenuItemKeyV275(mi.Text);
                if (!IsMenuItemVisibleV275(key) || (mi.DropDownItems.Count == 0 && key == PanoMenuItemKeys.None && mi.Text.StartsWith("__", StringComparison.Ordinal)))
                {
                    items.RemoveAt(i);
                    try { item.Dispose(); } catch { }
                }
            }
        }
        RemoveRedundantMenuSeparators(items);
    }

    private static PanoMenuItemKeys ResolveMenuItemKeyV275(string? text)
    {
        string t = (text ?? string.Empty).ToLowerInvariant();
        if (t.Contains("gelişmiş filtre") || t.Contains("advanced filter")) return PanoMenuItemKeys.AdvancedFilter;
        if (t.Contains("preset")) return PanoMenuItemKeys.FilterPreset;
        if (t.Contains("ayrı filtre") || t.Contains("filter window") || t.Contains("pencere")) return PanoMenuItemKeys.FilterWindow;
        if (t.Contains("filtre") || t.Contains("filter"))
        {
            if (t.Contains("temiz") || t.Contains("clear")) return t.Contains("kolon") || t.Contains("column") ? PanoMenuItemKeys.ClearColumnFilter : PanoMenuItemKeys.ClearAllFilters;
            return PanoMenuItemKeys.FilterPopup;
        }
        if (t.Contains("artan") || t.Contains("ascending") || t.Contains("a-z")) return PanoMenuItemKeys.SortAscending;
        if (t.Contains("azalan") || t.Contains("descending") || t.Contains("z-a")) return PanoMenuItemKeys.SortDescending;
        if (t.Contains("sıralamayı temiz") || t.Contains("clear sort") || t.Contains("unsort")) return PanoMenuItemKeys.ClearSort;
        if (t.Contains("sabitle") || t.Contains("frozen") || t.Contains("freeze")) return PanoMenuItemKeys.FreezeColumn;
        if (t.Contains("bu kolonu sığdır") || t.Contains("auto size column")) return PanoMenuItemKeys.AutoSizeColumn;
        if (t.Contains("sığdır") || t.Contains("width") || t.Contains("genişlik")) return PanoMenuItemKeys.AutoSizeAllColumns;
        if (t.Contains("layout") || t.Contains("düzen") || t.Contains("profil")) return PanoMenuItemKeys.LayoutSaveLoad;
        if (t.Contains("grup") || t.Contains("group")) return PanoMenuItemKeys.Grouping;
        if (t.Contains("kolon") || t.Contains("column")) return PanoMenuItemKeys.ColumnChooser;
        if (t.Contains("görün") || t.Contains("view")) return PanoMenuItemKeys.ViewMode;
        if (t.Contains("tema") || t.Contains("theme")) return PanoMenuItemKeys.Theme;
        if (t.Contains("state") || t.Contains("durum kaydet")) return PanoMenuItemKeys.State;
        if (t.Contains("senaryo") || t.Contains("scenario")) return PanoMenuItemKeys.Scenario;
        if (t.Contains("kopya") || t.Contains("copy") || t.Contains("paste") || t.Contains("yapıştır")) return PanoMenuItemKeys.Clipboard;
        if (t.Contains("düzenle") || t.Contains("edit")) return PanoMenuItemKeys.Editing;
        if (t.Contains("detay") || t.Contains("detail")) return PanoMenuItemKeys.RowDetails;
        if (t.Contains("analiz") || t.Contains("analytics")) return PanoMenuItemKeys.Analytics;
        if (t.Contains("export") || t.Contains("dışa aktar") || t.Contains("yazdır") || t.Contains("print")) return PanoMenuItemKeys.ExportPrint;
        return PanoMenuItemKeys.None;
    }
}
