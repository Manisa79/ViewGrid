using System.ComponentModel;
using Taylan.Pano.Core;

namespace Taylan.Pano.Tree;

public sealed class TreeNodeModel
{
    public object? Model { get; set; }
    public int Level { get; set; }
    public TreeNodeModel? Parent { get; set; }
    public bool Expanded { get; set; } = true;
    public bool IsLoading { get; set; }
    public bool ChildrenLoaded { get; set; }
    public List<TreeNodeModel> Children { get; } = new();

    public bool HasChildren => Children.Count > 0;
    public bool IsRoot => Parent == null;
}

[Designer(typeof(global::Taylan.Pano.Design.PanoControlDesigner))]
[ToolboxItem(true)]
[ToolboxBitmap(typeof(TreePanoControl), "TreePanoControl.bmp")]
[Description("TreePanoControl / Tree Pano: hierarchical PanoControl control.")]
public partial class TreePanoControl : global::Taylan.Pano.Core.PanoControl
{
    private readonly List<TreeNodeModel> _roots = new();
    private readonly Dictionary<object, TreeNodeModel> _nodeByModel = new();
    private Func<object, IEnumerable<object>>? _childrenGetter;
    private Func<object, CancellationToken, Task<IEnumerable<object>>>? _asyncChildrenGetter;

    [Category("Pano Tree")]
    [DefaultValue(true)]
    public bool AutoExpandRoots { get; set; } = true;

    [Category("Pano Tree")]
    [DefaultValue(true)]
    public bool PersistTreeExpansion { get; set; } = true;

    [Category("Pano Tree")]
    [DefaultValue(18)]
    public int TreeIndentWidth { get; set; } = 18;

    [Category("Pano Tree")]
    [DefaultValue(true)]
    public bool ShowTreeLines { get; set; } = true;

    [Category("Pano Tree")]
    [DefaultValue(true)]
    public bool ShowExpandCollapseGlyphs { get; set; } = true;

    [Category("Pano Tree")]
    [DefaultValue(true)]
    public bool LazyLoadChildren { get; set; } = true;

    [Browsable(false)]
    public IReadOnlyList<TreeNodeModel> RootNodes => _roots;

    [Browsable(false)]
    public int TotalNodeCount => _roots.Sum(CountNodes);

    [Browsable(false)]
    public int VisibleNodeCount => Flatten().Count();

    public event EventHandler<TreeNodeEventArgs>? NodeExpanded;
    public event EventHandler<TreeNodeEventArgs>? NodeCollapsed;
    public event EventHandler<TreeNodeEventArgs>? NodeChildrenLoaded;

    public TreePanoControl()
    {
        AutoEnsureReadableTextColors = true;
        AutoApplyThemeToColumnHeaders = true;
        UseUnifiedThemeVisuals = true;
        AutoThemeFromParent = true;
    }

    public void SetChildrenGetter(Func<object, IEnumerable<object>>? childrenGetter) => _childrenGetter = childrenGetter;
    public void SetAsyncChildrenGetter(Func<object, CancellationToken, Task<IEnumerable<object>>>? childrenGetter) => _asyncChildrenGetter = childrenGetter;
    public void ClearChildrenGetter()
    {
        _childrenGetter = null;
        _asyncChildrenGetter = null;
    }

    public void SetTreeObjects(IEnumerable<object> roots)
    {
        var expandedKeys = PersistTreeExpansion ? CaptureExpandedKeys() : new HashSet<string>();

        _roots.Clear();
        _nodeByModel.Clear();

        foreach (var r in roots)
        {
            var node = Build(r, 0, null);
            if (AutoExpandRoots) node.Expanded = true;
            RestoreExpandedState(node, expandedKeys);
            _roots.Add(node);
        }

        RebuildVisibleRows();
    }

    public TreeNodeModel? GetNode(object? model)
    {
        if (model == null) return null;
        return _nodeByModel.TryGetValue(model, out var node) ? node : null;
    }

    public int GetNodeLevel(object? model) => GetNode(model)?.Level ?? 0;
    public bool IsExpanded(object? model) => GetNode(model)?.Expanded ?? false;
    public new bool HasChildren(object? model) => GetNode(model)?.HasChildren ?? false;

    public void ExpandAll()
    {
        foreach (var root in _roots) SetExpandedRecursive(root, true);
        RebuildVisibleRows();
    }

    public void CollapseAll()
    {
        foreach (var root in _roots) SetExpandedRecursive(root, false);
        RebuildVisibleRows();
    }

    public void ExpandToLevel(int level)
    {
        foreach (var root in _roots) SetExpandedToLevel(root, level);
        RebuildVisibleRows();
    }

    public void ToggleNode(object model)
    {
        var node = GetNode(model);
        if (node == null) return;

        if (node.Expanded) CollapseNode(model);
        else ExpandNode(model);
    }

    public void ExpandNode(object model)
    {
        var node = GetNode(model);
        if (node == null) return;

        EnsureChildrenLoaded(node);
        node.Expanded = true;
        RebuildVisibleRows();
        NodeExpanded?.Invoke(this, new TreeNodeEventArgs(node));
    }

    public async Task ExpandNodeAsync(object model, CancellationToken cancellationToken = default)
    {
        var node = GetNode(model);
        if (node == null) return;

        await EnsureChildrenLoadedAsync(node, cancellationToken).ConfigureAwait(true);
        node.Expanded = true;
        RebuildVisibleRows();
        NodeExpanded?.Invoke(this, new TreeNodeEventArgs(node));
    }

    public void CollapseNode(object model)
    {
        var node = GetNode(model);
        if (node == null) return;

        node.Expanded = false;
        RebuildVisibleRows();
        NodeCollapsed?.Invoke(this, new TreeNodeEventArgs(node));
    }

    public void ExpandSelectedNode()
    {
        if (SelectedObject != null) ExpandNode(SelectedObject);
    }

    public void CollapseSelectedNode()
    {
        if (SelectedObject != null) CollapseNode(SelectedObject);
    }

    public void ToggleSelectedNode()
    {
        if (SelectedObject != null) ToggleNode(SelectedObject);
    }

    public IReadOnlyList<object> SearchNodes(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<object>();
        return _nodeByModel.Keys
            .Where(x => x?.ToString()?.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
            .ToList();
    }

    public bool SelectFirstMatch(string text)
    {
        var first = SearchNodes(text).FirstOrDefault();
        if (first == null) return false;

        ExpandAncestors(first);
        RebuildVisibleRows();
        SelectObject(first);
        return true;
    }

    public void ExpandAncestors(object model)
    {
        var node = GetNode(model);
        while (node?.Parent != null)
        {
            node.Parent.Expanded = true;
            node = node.Parent;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if ((keyData == Keys.Space || keyData == Keys.Enter) && SelectedObject != null && HasChildren(SelectedObject))
        {
            ToggleSelectedNode();
            return true;
        }

        if ((keyData == Keys.Add || keyData == Keys.Oemplus || keyData == Keys.Right) && SelectedObject != null && HasChildren(SelectedObject))
        {
            ExpandSelectedNode();
            return true;
        }

        if ((keyData == Keys.Subtract || keyData == Keys.OemMinus || keyData == Keys.Left) && SelectedObject != null && HasChildren(SelectedObject))
        {
            CollapseSelectedNode();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private TreeNodeModel Build(object model, int level, TreeNodeModel? parent)
    {
        var node = new TreeNodeModel
        {
            Model = model,
            Level = level,
            Parent = parent,
            Expanded = AutoExpandRoots && level == 0,
            ChildrenLoaded = _childrenGetter == null || !LazyLoadChildren
        };

        _nodeByModel[model] = node;

        if (_childrenGetter != null && !LazyLoadChildren)
        {
            foreach (var c in SafeGetChildren(model)) node.Children.Add(Build(c, level + 1, node));
            node.ChildrenLoaded = true;
        }

        return node;
    }

    private IEnumerable<object> SafeGetChildren(object model)
    {
        if (_childrenGetter == null) yield break;
        IEnumerable<object>? children = null;
        try { children = _childrenGetter(model); }
        catch { children = Array.Empty<object>(); }
        if (children == null) yield break;
        foreach (var child in children) if (child != null) yield return child;
    }

    private void EnsureChildrenLoaded(TreeNodeModel node)
    {
        if (node.ChildrenLoaded || _childrenGetter == null || node.Model == null) return;
        node.Children.Clear();
        foreach (var c in SafeGetChildren(node.Model)) node.Children.Add(Build(c, node.Level + 1, node));
        node.ChildrenLoaded = true;
        NodeChildrenLoaded?.Invoke(this, new TreeNodeEventArgs(node));
    }

    private async Task EnsureChildrenLoadedAsync(TreeNodeModel node, CancellationToken cancellationToken)
    {
        if (node.ChildrenLoaded || node.Model == null) return;

        if (_asyncChildrenGetter == null)
        {
            EnsureChildrenLoaded(node);
            return;
        }

        node.IsLoading = true;
        try
        {
            var children = await _asyncChildrenGetter(node.Model, cancellationToken).ConfigureAwait(true);
            node.Children.Clear();
            foreach (var c in children ?? Array.Empty<object>()) node.Children.Add(Build(c, node.Level + 1, node));
            node.ChildrenLoaded = true;
            NodeChildrenLoaded?.Invoke(this, new TreeNodeEventArgs(node));
        }
        finally
        {
            node.IsLoading = false;
        }
    }

    private void RebuildVisibleRows() => SetObjects(Flatten());

    private IEnumerable<object> Flatten()
    {
        foreach (var n in _roots)
            foreach (var m in FlattenOne(n))
                yield return m;
    }

    private IEnumerable<object> FlattenOne(TreeNodeModel n)
    {
        if (n.Model != null) yield return n.Model;
        if (!n.Expanded) yield break;

        EnsureChildrenLoaded(n);
        foreach (var c in n.Children)
            foreach (var m in FlattenOne(c))
                yield return m;
    }

    private void SetExpandedRecursive(TreeNodeModel node, bool expanded)
    {
        node.Expanded = expanded;
        EnsureChildrenLoaded(node);
        foreach (var child in node.Children) SetExpandedRecursive(child, expanded);
    }

    private void SetExpandedToLevel(TreeNodeModel node, int level)
    {
        node.Expanded = node.Level < level;
        EnsureChildrenLoaded(node);
        foreach (var child in node.Children) SetExpandedToLevel(child, level);
    }

    private HashSet<string> CaptureExpandedKeys()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var root in _roots) CaptureExpandedKeys(root, result);
        return result;
    }

    private void CaptureExpandedKeys(TreeNodeModel node, HashSet<string> keys)
    {
        if (node.Model != null && node.Expanded) keys.Add(GetModelKey(node.Model));
        foreach (var child in node.Children) CaptureExpandedKeys(child, keys);
    }

    private void RestoreExpandedState(TreeNodeModel node, HashSet<string> keys)
    {
        if (node.Model != null && keys.Contains(GetModelKey(node.Model))) node.Expanded = true;
        foreach (var child in node.Children) RestoreExpandedState(child, keys);
    }

    private static int CountNodes(TreeNodeModel node)
    {
        int count = 1;
        foreach (var child in node.Children) count += CountNodes(child);
        return count;
    }

    private static string GetModelKey(object model) => model.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class TreeNodeEventArgs : EventArgs
{
    public TreeNodeEventArgs(TreeNodeModel node) => Node = node;
    public TreeNodeModel Node { get; }
    public object? Model => Node.Model;
}
