using System.ComponentModel;
using Taylan.Pano.Core;
using Taylan.Pano.Tree;

namespace Taylan.Pano.Compatibility;

/// <summary>
/// Pano/GLV tabanlı hızlı liste görünümü.
/// Pano tabanlı ana hızlı liste kontrolüdür; eski hızlı alias tamamen kaldırıldı.
/// </summary>
[Designer(typeof(global::Taylan.Pano.Design.PanoControlDesigner))]
[ToolboxItem(true)]
[DesignTimeVisible(true)]
[Description("FastPanoControl: yüksek performanslı Pano liste kontrolü.")]
public class FastPanoControl : PanoControl
{
    public FastPanoControl()
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
/// DataListView benzeri kullanım için hazır ayarlı PanoControl.
/// DataSource/DataTable, AutoGenerateColumns ve inline edit altyapısı PanoControl içindedir.
/// </summary>
[Designer(typeof(global::Taylan.Pano.Design.PanoControlDesigner))]
[ToolboxItem(true)]
[DesignTimeVisible(true)]
[Description("DataListView: DataSource/DataTable odaklı Pano liste kontrolü.")]
public class DataListView : PanoControl
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
/// TreeListView benzeri kullanım için isim uyumluluğu. Asıl ağaç mantığı TreePanoControl'dedir.
/// </summary>
[Designer(typeof(global::Taylan.Pano.Design.PanoControlDesigner))]
[ToolboxItem(true)]
[DesignTimeVisible(true)]
[Description("TreeListView: TreePanoControl tabanlı hiyerarşik Pano kontrolü.")]
public class TreeListView : TreePanoControl
{
    public TreeListView()
    {
        AutoEnsureReadableTextColors = true;
        AutoApplyThemeToColumnHeaders = true;
        UseUnifiedThemeVisuals = true;
        AutoThemeFromParent = true;
    }
}
