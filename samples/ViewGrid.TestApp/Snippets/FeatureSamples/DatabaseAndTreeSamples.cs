// ViewGrid v25.2 Database + Tree örnekleri
// Bu dosya kopyala/uyarla mantığıyla hazırlanmıştır. TestApp içindeki Demo Hub'da çalışan form örnekleri de vardır.

using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Tree;
using System.Data;
using System.Windows.Forms;

namespace ViewGrid.FeatureSamples;

public static class DatabaseAndTreeSamples
{
    public static ViewGridControl CreateDataTableGrid(DataTable table)
    {
        var grid = new ViewGridControl
        {
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            ShowGridLines = true,
            FastFilterMenuForHugeLists = true,
            AsyncLoadFullFilterValues = true,
            EmptyListMessage = "Database sonucu boş"
        };

        grid.Columns.Add(new ViewGridColumn("Id", "", 70) { AspectGetter = row => ((DataRow)row)["Id"] });
        grid.Columns.Add(new ViewGridColumn("Barkod", "", 150) { AspectGetter = row => ((DataRow)row)["Barcode"] });
        grid.Columns.Add(new ViewGridColumn("Makine", "", 130) { AspectGetter = row => ((DataRow)row)["MachineName"] });
        grid.Columns.Add(new ViewGridColumn("Assembly", "", 220) { AspectGetter = row => ((DataRow)row)["AssemblyName"] });
        grid.Columns.Add(new ViewGridColumn("Sonuç", "", 90) { AspectGetter = row => ((DataRow)row)["AoiResult"] });
        grid.SetObjects(table.Rows.Cast<DataRow>());
        return grid;
    }

    public static TreeViewGridControl CreateTreeGrid(IEnumerable<TreeSampleNode> roots)
    {
        var tree = new TreeViewGridControl
        {
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            ShowGridLines = true,
            EmptyListMessage = "Tree kaydı yok"
        };

        tree.Columns.Add(new ViewGridColumn("Ağaç", "", 340)
        {
            AspectGetter = row => new string(' ', ((TreeSampleNode)row).Level * 4) + ((TreeSampleNode)row).Name
        });
        tree.Columns.Add(new ViewGridColumn("Tip", "Kind", 120));
        tree.Columns.Add(new ViewGridColumn("Kod", "Code", 140));
        tree.Columns.Add(new ViewGridColumn("Durum", "State", 100));
        tree.SetChildrenGetter(row => ((TreeSampleNode)row).Children);
        tree.SetTreeObjects(roots.Cast<object>());
        return tree;
    }
}

public sealed class TreeSampleNode
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Code { get; set; } = "";
    public string State { get; set; } = "";
    public int Level { get; set; }
    public List<TreeSampleNode> Children { get; } = new();
}
