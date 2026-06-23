using System.ComponentModel;
using ViewGrid.Core;
using ViewGrid.Tree;

namespace ViewGrid.Compatibility;

/// <summary>
/// ViewGrid/GLV tabanlı hızlı liste görünümü.
/// ViewGrid tabanlı ana hızlı liste kontrolüdür; eski hızlı alias tamamen kaldırıldı.
/// </summary>
[Designer(typeof(global::ViewGrid.Design.ViewGridControlDesigner))]
[ToolboxItem(true)]
[DesignTimeVisible(true)]
[Description("FastViewGridControl: yüksek performanslı ViewGrid liste kontrolü.")]
public class FastViewGridControl : ViewGridControl
{
    public FastViewGridControl()
    {
        AutoEnsureReadableTextColors = true;
        AutoApplyThemeToColumnHeaders = true;
        UseUnifiedThemeVisuals = true;
        AutoThemeFromParent = true;
        ApplyUltimatePerformanceProfile();
        PreserveFixedColumnWidthsOnAutoFit = true;
        IncludeCellImagesInAutoResizeWidth = true;
    }
}

/// <summary>
/// DataListView benzeri kullanım için hazır ayarlı ViewGridControl.
/// DataSource/DataTable, AutoGenerateColumns ve inline edit altyapısı ViewGridControl içindedir.
/// </summary>
[Designer(typeof(global::ViewGrid.Design.ViewGridControlDesigner))]
[ToolboxItem(true)]
[DesignTimeVisible(true)]
[Description("DataListView: DataSource/DataTable odaklı ViewGrid liste kontrolü.")]
public class DataListView : ViewGridControl
{
    public DataListView()
    {
        AutoEnsureReadableTextColors = true;
        AutoApplyThemeToColumnHeaders = true;
        UseUnifiedThemeVisuals = true;
        AutoThemeFromParent = true;
        AutoGenerateColumns = true;
        EnableCellEditing = true;
        AllowEditAllCells = true;
        EnableInlineDatabaseEditing = true;
        ShowGridLines = true;
        FullRowSelect = true;
    }
}

/// <summary>
/// TreeListView benzeri kullanım için isim uyumluluğu. Asıl ağaç mantığı TreeViewGridControl'dedir.
/// </summary>
[Designer(typeof(global::ViewGrid.Design.ViewGridControlDesigner))]
[ToolboxItem(true)]
[DesignTimeVisible(true)]
[Description("TreeListView: TreeViewGridControl tabanlı hiyerarşik ViewGrid kontrolü.")]
public class TreeListView : TreeViewGridControl
{
    public TreeListView()
    {
        AutoEnsureReadableTextColors = true;
        AutoApplyThemeToColumnHeaders = true;
        UseUnifiedThemeVisuals = true;
        AutoThemeFromParent = true;
    }
}
