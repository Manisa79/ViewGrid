using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Windows.Forms;
using ViewGrid.Columns;
using ViewGrid.Tree;

namespace ViewGrid.FeatureSamples;

/// <summary>
/// v25.24: TreeView + database/async kullanım örneği.
/// Gerçek SQL kodunu uygulama katmanınızda repository ile bağlayabilirsiniz.
/// </summary>
public static class GLVTreeDatabaseSamples
{
    public static TreeViewGridControl CreateTreeGridSample()
    {
        var tree = new TreeViewGridControl
        {
            Dock = DockStyle.Fill,
            ShowTreeLines = true,
            ShowExpandCollapseGlyphs = true,
            TreeIndentWidth = 20,
            LazyLoadChildren = true,
            AutoExpandRoots = true,
            PersistTreeExpansion = true,
            ShowColumnFilterButtons = true,
            AutoSaveUserLayout = true,
            AutoLoadUserLayout = true,
            UserLayoutKey = "Samples.Tree.Database"
        };

        tree.Columns.Add(new GLVColumn("Kod", nameof(TreeRow.Code), 140) { AllowFilter = true, AllowSort = true });
        tree.Columns.Add(new GLVColumn("Açıklama", nameof(TreeRow.Name), 260) { AllowFilter = true, AllowSort = true });
        tree.Columns.Add(new GLVColumn("Tip", nameof(TreeRow.Kind), 100) { AllowFilter = true, AllowSort = true });

        tree.SetChildrenGetter(parent => parent is TreeRow row ? LoadChildren(row.Id) : Array.Empty<object>());
        tree.SetTreeObjects(LoadRoots());
        return tree;
    }

    public static TreeViewGridControl CreateAsyncTreeGridSample()
    {
        var tree = CreateTreeGridSample();
        tree.SetAsyncChildrenGetter(async (parent, ct) =>
        {
            await Task.Delay(50, ct).ConfigureAwait(true); // DB çağrısı yerine demo gecikmesi
            return parent is TreeRow row ? LoadChildren(row.Id) : Array.Empty<object>();
        });
        return tree;
    }

    private static IEnumerable<object> LoadRoots()
    {
        yield return new TreeRow(1, 0, "LINE-1", "Hat 1", "Line");
        yield return new TreeRow(2, 0, "LINE-2", "Hat 2", "Line");
    }

    private static IEnumerable<object> LoadChildren(int parentId)
    {
        if (parentId == 1)
        {
            yield return new TreeRow(11, 1, "MC-1", "Makine 1", "Machine");
            yield return new TreeRow(12, 1, "MC-2", "Makine 2", "Machine");
        }
        else if (parentId == 2)
        {
            yield return new TreeRow(21, 2, "MC-3", "Makine 3", "Machine");
        }
        else if (parentId == 11)
        {
            yield return new TreeRow(111, 11, "PRG-001", "Program 001", "Program");
            yield return new TreeRow(112, 11, "PRG-002", "Program 002", "Program");
        }
    }

    public sealed record TreeRow(int Id, int ParentId, string Code, string Name, string Kind);
}
