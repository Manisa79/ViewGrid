using System.ComponentModel;
using ViewGrid.Core;
using ViewGrid.Theming;

namespace ViewGrid.Tree;

public enum TreeViewGridSearchBehavior
{
    SelectOnly,
    ExpandAncestors,
    ExpandAncestorsAndDescendants
}

public partial class TreeViewGridControl
{
    private ContextMenuStrip? _treeContextMenu;
    private string _treeSearchText = string.Empty;

    [Category("ViewGrid Tree - UX v27.8")]
    [DefaultValue(true)]
    [Description("Sağ tıkta ağaç işlemleri menüsünü gösterir: genişlet, daralt, tümünü aç/kapat, seviyeye kadar aç.")]
    public bool EnableTreeContextMenu { get; set; } = true;

    [Category("ViewGrid Tree - UX v27.8")]
    [DefaultValue(true)]
    [Description("Ağaç düğümüne çift tıklanınca genişlet/daralt yapar.")]
    public bool TreeDoubleClickTogglesNode { get; set; } = true;

    [Category("ViewGrid Tree - UX v27.8")]
    [DefaultValue(true)]
    [Description("Arama sonucunda bulunan düğümün üst seviyelerini otomatik açar.")]
    public bool TreeSearchExpandsAncestors { get; set; } = true;

    [Category("ViewGrid Tree - UX v27.8")]
    [DefaultValue(TreeViewGridSearchBehavior.ExpandAncestors)]
    public TreeViewGridSearchBehavior TreeSearchBehavior { get; set; } = TreeViewGridSearchBehavior.ExpandAncestors;

    [Category("ViewGrid Tree - UX v27.8")]
    [DefaultValue(2)]
    [Description("Seviyeye kadar aç komutu için varsayılan derinlik.")]
    public int TreeDefaultExpandLevel { get; set; } = 2;

    [Category("ViewGrid Tree - UX v27.8")]
    [DefaultValue(3)]
    [Description("Arama sonucunda alt dallar da açılacaksa kullanılacak maksimum derinlik.")]
    public int TreeSearchExpandDescendantDepth { get; set; } = 3;

    [Category("ViewGrid Tree - UX v27.8")]
    [DefaultValue("")]
    [Description("Tasarım/runtime tarafında kolay bağlanabilen hızlı arama metni. Set edildiğinde ilk eşleşme seçilir.")]
    public string TreeSearchText
    {
        get => _treeSearchText;
        set
        {
            _treeSearchText = value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_treeSearchText))
                ApplyTreeSearch(_treeSearchText);
        }
    }

    public event EventHandler<TreeNodeEventArgs>? TreeSearchMatchSelected;

    public bool ApplyTreeSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var match = SearchNodes(text).FirstOrDefault();
        if (match == null) return false;

        if (TreeSearchExpandsAncestors || TreeSearchBehavior != TreeViewGridSearchBehavior.SelectOnly)
            ExpandAncestors(match);

        if (TreeSearchBehavior == TreeViewGridSearchBehavior.ExpandAncestorsAndDescendants)
            ExpandDescendants(match, TreeSearchExpandDescendantDepth);

        RebuildVisibleRowsForTreeUx();
        SelectObject(match);
        var node = GetNode(match);
        if (node != null) TreeSearchMatchSelected?.Invoke(this, new TreeNodeEventArgs(node));
        return true;
    }

    public IReadOnlyList<object> GetVisibleTreeObjects() => FlattenVisibleTreeObjects().ToList();

    public string GetNodePath(object model, string separator = " / ")
    {
        var node = GetNode(model);
        if (node == null) return Convert.ToString(model) ?? string.Empty;
        var parts = new Stack<string>();
        while (node != null)
        {
            if (node.Model != null) parts.Push(Convert.ToString(node.Model) ?? string.Empty);
            node = node.Parent;
        }
        return string.Join(separator, parts);
    }

    public void ExpandDescendants(object model, int maxDepth = int.MaxValue)
    {
        var node = GetNode(model);
        if (node == null) return;
        ExpandDescendants(node, 0, Math.Max(0, maxDepth));
        RebuildVisibleRowsForTreeUx();
    }

    public void CollapseDescendants(object model)
    {
        var node = GetNode(model);
        if (node == null) return;
        foreach (var child in node.Children)
            CollapseRecursive(child);
        RebuildVisibleRowsForTreeUx();
    }

    public void CollapseSiblings(object model)
    {
        var node = GetNode(model);
        if (node?.Parent == null) return;
        foreach (var sibling in node.Parent.Children)
        {
            if (!ReferenceEquals(sibling, node))
                sibling.Expanded = false;
        }
        RebuildVisibleRowsForTreeUx();
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (!TreeDoubleClickTogglesNode || e.Button != MouseButtons.Left) return;
        if (SelectedObject != null && HasChildren(SelectedObject))
            ToggleSelectedNode();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Right && EnableTreeContextMenu)
            ShowTreeContextMenu(e.Location);
    }

    private void ShowTreeContextMenu(Point location)
    {
        _treeContextMenu?.Dispose();
        _treeContextMenu = new ContextMenuStrip();
        SmartMenuRenderer.ApplyTo(_treeContextMenu, Theme);

        bool hasSelected = SelectedObject != null;
        var expand = _treeContextMenu.Items.Add("Seçili düğümü genişlet", null, (_, __) => { if (SelectedObject != null) ExpandNode(SelectedObject); });
        expand.Enabled = hasSelected;
        var collapse = _treeContextMenu.Items.Add("Seçili düğümü daralt", null, (_, __) => { if (SelectedObject != null) CollapseNode(SelectedObject); });
        collapse.Enabled = hasSelected;
        var expandBranch = _treeContextMenu.Items.Add("Alt dalları genişlet", null, (_, __) => { if (SelectedObject != null) ExpandDescendants(SelectedObject, TreeDefaultExpandLevel); });
        expandBranch.Enabled = hasSelected;
        var collapseBranch = _treeContextMenu.Items.Add("Alt dalları daralt", null, (_, __) => { if (SelectedObject != null) CollapseDescendants(SelectedObject); });
        collapseBranch.Enabled = hasSelected;
        _treeContextMenu.Items.Add(new ToolStripSeparator());
        _treeContextMenu.Items.Add("Tümünü genişlet", null, (_, __) => ExpandAll());
        _treeContextMenu.Items.Add("Tümünü daralt", null, (_, __) => CollapseAll());
        _treeContextMenu.Items.Add($"{TreeDefaultExpandLevel}. seviyeye kadar aç", null, (_, __) => ExpandToLevel(TreeDefaultExpandLevel));
        _treeContextMenu.Items.Add(new ToolStripSeparator());
        var copyPath = _treeContextMenu.Items.Add("Düğüm yolunu kopyala", null, (_, __) => { if (SelectedObject != null) Clipboard.SetText(GetNodePath(SelectedObject)); });
        copyPath.Enabled = hasSelected;
        _treeContextMenu.Show(this, location);
    }

    private void ExpandDescendants(TreeNodeModel node, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        if (node.Model != null) EnsureChildrenLoadedForTreeUx(node);
        node.Expanded = true;
        foreach (var child in node.Children)
            ExpandDescendants(child, depth + 1, maxDepth);
    }

    private void CollapseRecursive(TreeNodeModel node)
    {
        node.Expanded = false;
        foreach (var child in node.Children)
            CollapseRecursive(child);
    }

    private IEnumerable<object> FlattenVisibleTreeObjects()
    {
        foreach (var root in RootNodes)
            foreach (var item in FlattenVisibleTreeObject(root))
                yield return item;
    }

    private IEnumerable<object> FlattenVisibleTreeObject(TreeNodeModel node)
    {
        if (node.Model != null) yield return node.Model;
        if (!node.Expanded) yield break;
        EnsureChildrenLoadedForTreeUx(node);
        foreach (var child in node.Children)
            foreach (var item in FlattenVisibleTreeObject(child))
                yield return item;
    }

    private void EnsureChildrenLoadedForTreeUx(TreeNodeModel node)
    {
        if (node.ChildrenLoaded || node.Model == null || _childrenGetter == null) return;

        node.Children.Clear();
        foreach (var child in SafeGetChildren(node.Model))
            node.Children.Add(Build(child, node.Level + 1, node));
        node.ChildrenLoaded = true;
        NodeChildrenLoaded?.Invoke(this, new TreeNodeEventArgs(node));
    }

    private void RebuildVisibleRowsForTreeUx()
    {
        SetObjects(FlattenVisibleTreeObjects());
    }
}
