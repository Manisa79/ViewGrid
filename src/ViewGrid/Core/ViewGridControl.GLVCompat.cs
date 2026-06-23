using System.Collections;
using System.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices;
using ViewGrid.Columns;
using ViewGrid.Virtualization;

namespace ViewGrid.Core;

public sealed class ViewGridItemCheckedEventArgs : EventArgs
{
    public ViewGridItemCheckedEventArgs(object model, int rowIndex, CheckState checkState)
    {
        Model = model;
        RowObject = model;
        RowIndex = rowIndex;
        CheckState = checkState;
    }

    public object Model { get; }
    public object RowObject { get; }
    public int RowIndex { get; }
    public CheckState CheckState { get; }
    public bool Checked => CheckState == CheckState.Checked;
}

public sealed class ViewGridObjectsChangedEventArgs : EventArgs
{
    public ViewGridObjectsChangedEventArgs(int objectCount, int visibleCount)
    {
        ObjectCount = objectCount;
        VisibleCount = visibleCount;
    }

    public int ObjectCount { get; }
    public int VisibleCount { get; }
}

public sealed class ViewGridDataModelEventArgs : EventArgs
{
    public ViewGridDataModelEventArgs(object? model, int rowIndex)
    {
        Model = model;
        RowObject = model;
        RowIndex = rowIndex;
    }

    public object? Model { get; }
    public object? RowObject { get; }
    public int RowIndex { get; }
}

public partial class ViewGridControl
{
    private int _compatUpdateDepth;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int WM_SETREDRAW = 0x000B;

    [Category("ViewGrid - Compatibility")]
    public event EventHandler<ViewGridItemCheckedEventArgs>? ItemChecked;

    [Category("ViewGrid - Compatibility")]
    public event EventHandler<ViewGridItemCheckedEventArgs>? ObjectChecked;

    [Category("ViewGrid - Compatibility")]
    public event EventHandler<ViewGridObjectsChangedEventArgs>? ObjectsChanged;

    [Category("ViewGrid - Compatibility")]
    public event EventHandler<ViewGridDataModelEventArgs>? ObjectAdded;

    [Category("ViewGrid - Compatibility")]
    public event EventHandler<ViewGridDataModelEventArgs>? ObjectRemoved;

    [Browsable(false)]
    public bool HasObjects => ObjectCount > 0;

    [Browsable(false)]
    public bool IsEmpty => ObjectCount == 0;

    [Browsable(false)]
    public ViewGridColumn? PrimarySortColumnCompat => _sortColumn;

    [Browsable(false)]
    public ViewGridColumn? SortColumn
    {
        get => _sortColumn;
        set => SortBy(value, _sortDesc);
    }

    [Browsable(false)]
    public global::System.Windows.Forms.SortOrder SortOrder
    {
        get => _sortColumn == null ? global::System.Windows.Forms.SortOrder.None : (_sortDesc ? global::System.Windows.Forms.SortOrder.Descending : global::System.Windows.Forms.SortOrder.Ascending);
        set
        {
            if (_sortColumn == null || value == global::System.Windows.Forms.SortOrder.None)
            {
                ClearSort(null);
                return;
            }
            SortBy(_sortColumn, value == global::System.Windows.Forms.SortOrder.Descending);
        }
    }

    [Browsable(false)]
    public object? CheckedObject => CheckedObjects.FirstOrDefault();

    [Browsable(false)]
    public object? CheckedItem => CheckedObject;

    [Browsable(false)]
    public IReadOnlyList<object> CheckedItems => CheckedObjects;

    [Browsable(false)]
    public int CheckedCount => CheckedObjects.Count;

    [Browsable(false)]
    public int SelectedCount => SelectedObjects.Count;

    /// <summary>GLV compatibility: unfiltered model count.</summary>
    [Browsable(false)]
    public int ObjectCount => _provider?.Count ?? 0;

    /// <summary>Total row count alias. Useful for migration code that expects a list-like Count property.</summary>
    [Browsable(false)]
    public int TotalCount => ObjectCount;

    /// <summary>Visible row count alias after filter/group state. Group headers are excluded by GetVisibleObjects().</summary>
    [Browsable(false)]
    public int VisibleCount => FilteredCount;

    private ViewGridObjectCollection? _items;

    /// <summary>
    /// GLV/ListView compatibility collection.
    /// Enables common migration code such as viewgrid.Items.Count, Items.Add, Items.Clear.
    /// For ViewGrid the collection is a live facade over the current model/tile provider.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ViewGridObjectCollection Items => _items ??= new ViewGridObjectCollection(this);

    /// <summary>Legacy typo/singular alias for old migration code. No data is copied; this returns the same live facade as Items.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ViewGridObjectCollection Item => Items;

    /// <summary>Mode-independent total model row count. Prefer this over Items.Count for Virtual/DataTable/Tree scenarios.</summary>
    [Browsable(false)]
    public int Count => ObjectCount;

    /// <summary>Mode-independent filtered/visible row count. Group headers are excluded when grouping is active.</summary>
    [Browsable(false)]
    public int FilteredCount => GetVisibleObjects()?.Count ?? 0;

    /// <summary>GLV compatibility: current model objects. Setting this refreshes ViewGrid.</summary>
    [Browsable(false)]
    public IEnumerable Objects
    {
        get => EnumerateProviderObjects().ToList();
        set => SetObjects(value == null ? Array.Empty<object>() : value.Cast<object>());
    }

    /// <summary>GLV compatibility: filtered/visible model objects.</summary>
    [Browsable(false)]
    public IEnumerable FilteredObjects => GetVisibleObjects();

    /// <summary>GLV style text filter alias.</summary>
    [Browsable(false)]
    public string TextFilter
    {
        get => _filters.GlobalText;
        set => SetGlobalFilter(value ?? string.Empty);
    }

    /// <summary>ViewGrid/GLV compatibility: current visible item count after filtering/sorting/grouping.</summary>
    public int GetItemCount() => ViewCount;

    /// <summary>Common GLV migration alias.</summary>
    public int GetObjectCount() => ObjectCount;

    /// <summary>Common GLV migration alias for filtered rows.</summary>
    public int GetFilteredObjectCount() => FilteredCount;

    /// <summary>Alias for GLV migration code that calls GetItem(index).</summary>
    public object? GetItem(int index) => GetModelObject(index);

    public void ClearObjects()
    {
        SetObjects(Array.Empty<object>());
        OnObjectsChanged();
    }

    public void AddObject(object? model)
    {
        if (model == null) return;
        var list = EnumerateProviderObjects().ToList();
        list.Add(model);
        SetObjects(list);
        OnObjectAdded(model, list.Count - 1);
    }

    public void AddObjects(IEnumerable models)
    {
        if (models == null) return;
        var list = EnumerateProviderObjects().ToList();
        var added = new List<object>();
        foreach (var model in models)
        {
            if (model == null) continue;
            list.Add(model);
            added.Add(model);
        }
        if (added.Count == 0) return;
        SetObjects(list);
        int firstIndex = Math.Max(0, list.Count - added.Count);
        for (int i = 0; i < added.Count; i++) OnObjectAdded(added[i], firstIndex + i);
    }

    /// <summary>GLV compatible params overload: grid.AddObjects(row1, row2).</summary>
    public void AddObjects(params object[] models)
    {
        AddObjects((IEnumerable)(models ?? Array.Empty<object>()));
    }

    public bool RemoveObject(object? model)
    {
        if (model == null) return false;
        var list = EnumerateProviderObjects().ToList();
        int index = list.FindIndex(x => ReferenceEquals(x, model) || Equals(x, model));
        if (index < 0) return false;
        var removed = list[index];
        list.RemoveAt(index);
        SetObjects(list);
        OnObjectRemoved(removed, index);
        return true;
    }

    public int RemoveObjects(IEnumerable models)
    {
        if (models == null) return 0;
        var remove = models.Cast<object>().ToList();
        if (remove.Count == 0) return 0;
        var list = EnumerateProviderObjects().ToList();
        int before = list.Count;
        list.RemoveAll(x => remove.Any(r => ReferenceEquals(r, x) || Equals(r, x)));
        int removed = before - list.Count;
        if (removed > 0)
        {
            SetObjects(list);
            OnObjectsChanged();
        }
        return removed;
    }

    /// <summary>GLV compatible params overload: grid.RemoveObjects(row1, row2).</summary>
    public int RemoveObjects(params object[] models)
    {
        return RemoveObjects((IEnumerable)(models ?? Array.Empty<object>()));
    }

    public void SetObjects(IEnumerable rows)
    {
        SetObjects(rows == null ? Array.Empty<object>() : rows.Cast<object>());
    }

    /// <summary>
    /// Not: ViewGrid v25.62 ile params SetObjects(row1, row2, ...) overload'u kaldırıldı.
    /// Sebep: SetObjects(params object[]) ile SetObjects&lt;T&gt;(IEnumerable&lt;T&gt;) bazı çağrılarda belirsiz overload hatası üretiyordu.
    /// Tek tek model eklemek için AddObjects(row1, row2) kullanın; liste bağlamak için SetObjects(list) kullanın.
    /// </summary>

    /// <summary>GLV compatible overload. Replaces rows and optionally preserves selection/scroll position.</summary>
    public void SetObjects(IEnumerable rows, bool preserveSelection, bool preserveScroll = true)
    {
        UpdateObjects(rows, preserveSelection, preserveScroll);
    }

    /// <summary>Typed helper for migration code: grid.SetObjects&lt;MyRow&gt;(rows, preserveSelection: true).</summary>
    public void SetObjects<T>(IEnumerable<T> rows, bool preserveSelection, bool preserveScroll = true)
    {
        UpdateObjects(rows == null ? Array.Empty<object>() : rows.Cast<object>(), preserveSelection, preserveScroll);
    }

    /// <summary>Typed snapshot of the current provider objects.</summary>
    public List<T> ObjectsAs<T>() => EnumerateProviderObjects().OfType<T>().ToList();

    /// <summary>Typed visible snapshot after active filters/sort/grouping.</summary>
    public List<T> FilteredObjectsAs<T>() => GetVisibleObjects().OfType<T>().ToList();

    /// <summary>Typed model lookup by visible row index.</summary>
    public T? GetItem<T>(int index) where T : class => GetItem(index) as T;

    /// <summary>Typed selected object helper.</summary>
    public T? SelectedObjectAs<T>() where T : class => SelectedObject as T;

    /// <summary>Typed selected object list helper.</summary>
    public List<T> SelectedObjectsAs<T>() => SelectedObjects.OfType<T>().ToList();

    /// <summary>Typed checked object list helper.</summary>
    public List<T> CheckedObjectsAs<T>() => CheckedObjects.OfType<T>().ToList();

    public bool TryGetSelectedObject<T>(out T? selected) where T : class
    {
        selected = SelectedObject as T;
        return selected != null;
    }

    public bool TryGetItem<T>(int index, out T? item) where T : class
    {
        item = GetItem(index) as T;
        return item != null;
    }

    /// <summary>ListView/GLV style selection by visible index.</summary>
    public void SelectIndex(int index, bool addToSelection = false)
    {
        if (index < 0 || index >= ViewCount || IsGroupRow(index)) return;
        if (!addToSelection || !MultiSelect)
        {
            SelectRow(index);
            return;
        }
        _selectedRows.Add(index);
        _selectedRow = index;
        _selectionAnchorRow = index;
        EnsureRowVisible(index);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void SelectIndices(IEnumerable<int> indices, bool clearExisting = true)
    {
        if (indices == null) return;
        if (clearExisting) _selectedRows.Clear();
        int last = -1;
        foreach (var index in indices)
        {
            if (index < 0 || index >= ViewCount || IsGroupRow(index)) continue;
            _selectedRows.Add(index);
            last = index;
            if (!MultiSelect) break;
        }
        _selectedRow = last;
        _selectionAnchorRow = last;
        if (last >= 0) EnsureRowVisible(last);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void DeselectIndex(int index)
    {
        if (index < 0) return;
        _selectedRows.Remove(index);
        if (_selectedRow == index) _selectedRow = _selectedRows.Count > 0 ? _selectedRows.Last() : -1;
        _selectionAnchorRow = _selectedRow;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    /// <summary>
    /// GLV compatibility alias. Replaces the current object list and optionally preserves selection/scroll.
    /// </summary>
    public void UpdateObjects(IEnumerable rows, bool preserveSelection = true, bool preserveScroll = true)
    {
        var selected = preserveSelection ? SelectedObjects.ToList() : new List<object>();
        int oldScroll = preserveScroll ? _scrollY : 0;

        SetObjects(rows == null ? Array.Empty<object>() : rows.Cast<object>());
        OnObjectsChanged();

        if (preserveSelection && selected.Count > 0)
            SelectObjects(selected);

        if (preserveScroll && ViewCount > 0)
        {
            _scrollY = Math.Max(0, Math.Min(oldScroll, Math.Max(0, ViewCount - 1)));
            UpdateScrollBars();
        }

        Invalidate();
    }

    public void UpdateObject(object? model) => RefreshObject(model);


    /// <summary>
    /// Internal compatibility wrapper. Older GLV helper code used UpdateScrollBars();
    /// ViewGrid's real scrollbar/layout synchronization lives in RefreshView().
    /// Keeping this method prevents stale migration calls from breaking compilation.
    /// </summary>
    private void UpdateScrollBars()
    {
        if (IsDisposed) return;
        RefreshView();
    }

    public void RemoveObjects() => ClearObjects();

    public void RebuildColumns()
    {
        AutoSizeFillColumns();
        RefreshView();
        Invalidate();
    }

    public void BuildList(bool preserveSelection = true)
    {
        var selected = preserveSelection ? SelectedObjects.ToList() : new List<object>();
        InvalidateDataCaches();
        BuildViewIndex();
        if (preserveSelection && selected.Count > 0) SelectObjects(selected);
    }

    public void RefreshObjects()
    {
        InvalidateDataCaches(keepVersion: true);
        BuildViewIndex();
        ApplyAutoFitAfterDataChanged();
    }

    /// <summary>GLV compatible params overload. Refreshes the supplied models and rebuilds indexes once.</summary>
    public void RefreshObjects(params object[] objects)
    {
        if (objects == null || objects.Length == 0)
        {
            RefreshObjects();
            return;
        }
        foreach (var obj in objects) RefreshObject(obj);
        Invalidate();
    }

    public void SelectAll()
    {
        if (!MultiSelect) return;
        _selectedRows.Clear();
        for (int i = 0; i < ViewCount; i++)
            if (!IsGroupRow(i)) _selectedRows.Add(i);
        _selectedRow = _selectedRows.Count > 0 ? _selectedRows.Min : -1;
        _selectionAnchorRow = _selectedRow;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void CheckAll()
    {
        SetAllCheckBoxValues(true);
    }

    public void UncheckAll()
    {
        SetAllCheckBoxValues(false);
    }

    public void ToggleCheckObject(object? model)
    {
        if (model == null) return;
        SetObjectChecked(model, !IsChecked(model));
    }

    public void ToggleCheckSelectedObjects()
    {
        foreach (var obj in SelectedObjects.ToList()) ToggleCheckObject(obj);
    }

    public void SetObjectsChecked(IEnumerable models, bool value)
    {
        if (models == null) return;
        foreach (var model in models.Cast<object>()) SetObjectChecked(model, value);
        Invalidate();
    }

    public ViewGridColumn? GetColumn(string aspectNameOrText)
    {
        if (string.IsNullOrWhiteSpace(aspectNameOrText)) return null;
        return Columns.FirstOrDefault(c =>
            string.Equals(c.AspectName, aspectNameOrText, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Header, aspectNameOrText, StringComparison.OrdinalIgnoreCase));
    }


    /// <summary>
    /// GLV migration helper: designer field null kaldıysa kolonu koleksiyondan bulur, yoksa GLVColumn oluşturup ekler.
    /// MasterData geçişlerinde this.glvTimeUser = fglvLogs.GetOrCreateGLVColumn(...) şeklinde kullanılabilir.
    /// </summary>
    public GLVColumn GetOrCreateGLVColumn(string name, string aspectName, string? text = null, int width = 120)
    {
        var existing = Columns.ByName(name) ?? Columns.ByAspectName(aspectName);
        if (existing is GLVColumn glv)
            return glv;

        if (existing != null)
        {
            var converted = new GLVColumn(existing.Header, existing.AspectName, existing.Width)
            {
                Name = string.IsNullOrWhiteSpace(existing.Name) ? name : existing.Name,
                Visible = existing.Visible,
                DisplayIndex = existing.DisplayIndex,
                FillFreeSpace = existing.FillFreeSpace,
                MinimumWidth = existing.MinimumWidth,
                MaximumWidth = existing.MaximumWidth,
                TextAlign = existing.TextAlign,
                Kind = existing.Kind,
                Sortable = existing.Sortable,
                Filterable = existing.Filterable,
                AllowResize = existing.AllowResize,
                AllowReorder = existing.AllowReorder,
                AllowGroup = existing.AllowGroup,
                AllowColumnChooser = existing.AllowColumnChooser,
                PrivateColumn = existing.PrivateColumn
            };

            int index = Columns.IndexOf(existing);
            Columns[index] = converted;
            return converted;
        }

        var column = new GLVColumn(text ?? aspectName ?? name, aspectName ?? string.Empty, width)
        {
            Name = string.IsNullOrWhiteSpace(name) ? ViewGridColumnNameHelper.CreateNameFromAspectOrText(aspectName, text) : name
        };
        Columns.Add(column);
        RebuildColumns();
        return column;
    }

    /// <summary>
    /// Ref overload: eski formlarda private GLVColumn field null ise tek satırda güvenle doldurur.
    /// </summary>
    public GLVColumn EnsureGLVColumn(ref GLVColumn? field, string name, string aspectName, string? text = null, int width = 120)
    {
        field ??= GetOrCreateGLVColumn(name, aspectName, text, width);
        return field;
    }

    public void HideColumn(string aspectNameOrText)
    {
        var col = GetColumn(aspectNameOrText);
        if (col == null) return;
        col.Visible = false;
        RebuildColumns();
        OnColumnLayoutChanged();
    }

    public void ShowColumn(string aspectNameOrText)
    {
        var col = GetColumn(aspectNameOrText);
        if (col == null) return;
        col.Visible = true;
        RebuildColumns();
        OnColumnLayoutChanged();
    }

    public void BeginUpdate()
    {
        _compatUpdateDepth++;
        if (_compatUpdateDepth == 1 && IsHandleCreated)
            SendMessage(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
    }

    public void EndUpdate()
    {
        if (_compatUpdateDepth <= 0) return;
        _compatUpdateDepth--;
        if (_compatUpdateDepth == 0)
        {
            if (IsHandleCreated)
                SendMessage(Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            RefreshView();
            Invalidate(true);
        }
    }

    /// <summary>Live facade that mimics ListView/ViewGrid Items for migration convenience.</summary>
    public sealed class ViewGridObjectCollection : IList<object>
    {
        private readonly ViewGridControl _owner;
        internal ViewGridObjectCollection(ViewGridControl owner) => _owner = owner;

        public int Count => _owner.ObjectCount;
        public bool IsReadOnly => false;

        public object this[int index]
        {
            get => _owner.GetProviderObject(index) ?? throw new ArgumentOutOfRangeException(nameof(index));
            set
            {
                var list = _owner.EnumerateProviderObjects().ToList();
                if (index < 0 || index >= list.Count) throw new ArgumentOutOfRangeException(nameof(index));
                list[index] = value;
                _owner.SetObjects(list);
            }
        }

        public void Add(object item) => _owner.AddObject(item);
        public void Clear() => _owner.ClearObjects();
        public bool Contains(object item) => IndexOf(item) >= 0;
        public void CopyTo(object[] array, int arrayIndex) => _owner.EnumerateProviderObjects().ToList().CopyTo(array, arrayIndex);
        public IEnumerator<object> GetEnumerator() => _owner.EnumerateProviderObjects().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int IndexOf(object item) => _owner.IndexOfProviderObject(item);
        public void Insert(int index, object item)
        {
            var list = _owner.EnumerateProviderObjects().ToList();
            if (index < 0 || index > list.Count) throw new ArgumentOutOfRangeException(nameof(index));
            list.Insert(index, item);
            _owner.SetObjects(list);
        }
        public bool Remove(object item) => _owner.RemoveObject(item);
        public void RemoveAt(int index)
        {
            var list = _owner.EnumerateProviderObjects().ToList();
            if (index < 0 || index >= list.Count) throw new ArgumentOutOfRangeException(nameof(index));
            list.RemoveAt(index);
            _owner.SetObjects(list);
        }
    }

    private object? GetProviderObject(int index) => _provider != null && index >= 0 && index < _provider.Count ? _provider.GetRow(index) : null;

    private int IndexOfProviderObject(object? model)
    {
        if (model == null) return -1;
        var comparer = EqualityComparer<object>.Default;
        int count = _provider?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            var row = _provider?.GetRow(i);
            if (ReferenceEquals(row, model) || (row != null && comparer.Equals(row, model))) return i;
        }
        return -1;
    }

    private IEnumerable<object> EnumerateProviderObjects()
    {
        int count = _provider?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            var row = _provider?.GetRow(i);
            if (row != null) yield return row;
        }
    }


    public ViewGridColumn? GetColumn(int visibleColumnIndex)
    {
        var visible = Columns.VisibleColumns.ToList();
        return visibleColumnIndex >= 0 && visibleColumnIndex < visible.Count ? visible[visibleColumnIndex] : null;
    }

    public ViewGridColumn? GetColumnByName(string name) => string.IsNullOrWhiteSpace(name) ? null : Columns.ByName(name);

    public ViewGridColumn? GetColumnByAspect(string aspectName) => string.IsNullOrWhiteSpace(aspectName) ? null : Columns.ByAspectName(aspectName);

    public int IndexOf(object? model) => IndexOfObject(model);

    public int IndexOfModel(object? model) => IndexOfObject(model);

    public object? GetObjectAt(int viewRowIndex) => GetModelObject(viewRowIndex);

    public object? GetNthItemInDisplayOrder(int displayIndex) => GetModelObject(displayIndex);

    public object? GetNthItemInDisplayOrderOrNull(int displayIndex) => GetModelObject(displayIndex);

    public object? GetModelObjectAt(int x, int y)
    {
        int row = HitRow(y);
        return row >= 0 ? GetModelObject(row) : null;
    }

    public ViewGridColumn? GetColumnAt(int x) => HitColumn(x);

    public int GetColumnIndexAt(int x)
    {
        var col = HitColumn(x);
        if (col == null) return -1;
        return Columns.VisibleColumns.ToList().IndexOf(col);
    }

    public Rectangle GetCellBounds(int viewRowIndex, int visibleColumnIndex)
    {
        var col = GetColumn(visibleColumnIndex);
        return col == null ? Rectangle.Empty : GetCellBounds(viewRowIndex, col);
    }

    public void EnsureNthItemVisible(int viewRowIndex) => EnsureVisible(viewRowIndex);

    public void EnsureSelectedObjectVisible()
    {
        if (_selectedRow >= 0) EnsureVisible(_selectedRow);
    }

    public void ScrollToObject(object? model) => EnsureObjectVisible(model);

    public void Reveal(object? model) => EnsureObjectVisible(model);

    public void SelectObject(object? model, bool addToSelection, bool ensureVisible)
    {
        SelectObject(model, addToSelection);
        if (ensureVisible) EnsureObjectVisible(model);
    }

    public void SelectObjects(IEnumerable objects, bool addToSelection)
    {
        if (objects == null) return;
        if (!addToSelection) SelectObjects(objects);
        else
        {
            foreach (var obj in objects.Cast<object>()) SelectObject(obj, addToSelection: true);
        }
    }

    public void ClearSelectedObjects() => DeselectAll();

    public void CheckObjects(params object[] objects) => CheckObjects((IEnumerable)(objects ?? Array.Empty<object>()));

    public void UncheckObjects(params object[] objects) => UncheckObjects((IEnumerable)(objects ?? Array.Empty<object>()));

    public void SetObjectCheckedPublic(object? model, bool value) => SetObjectChecked(model, value);

    public void SetObjectCheckedState(object? model, CheckState state) => SetObjectChecked(model, state == CheckState.Checked);

    public CheckState GetObjectCheckState(object? model)
    {
        var col = Columns.FirstOrDefault(x => x.Kind == ViewGridColumnKind.CheckBox);
        if (model == null || col == null) return CheckState.Unchecked;
        return GetRowCheckState(model, col);
    }

    public void ToggleChecks(IEnumerable objects)
    {
        if (objects == null) return;
        BeginUpdate();
        try
        {
            foreach (var obj in objects.Cast<object>()) ToggleCheckObject(obj);
        }
        finally
        {
            EndUpdate();
        }
    }

    public void RefreshObject(object? model, bool preserveSelection)
    {
        var selected = preserveSelection ? SelectedObjects.ToList() : new List<object>();
        RefreshObject(model);
        if (preserveSelection && selected.Count > 0) SelectObjects(selected);
    }

    public void RefreshObjects(IEnumerable objects, bool preserveSelection)
    {
        var selected = preserveSelection ? SelectedObjects.ToList() : new List<object>();
        RefreshObjects(objects);
        if (preserveSelection && selected.Count > 0) SelectObjects(selected);
    }

    public void Sort(string aspectName, global::System.Windows.Forms.SortOrder order = global::System.Windows.Forms.SortOrder.Ascending)
    {
        var col = GetColumn(aspectName);
        if (col == null) return;
        Sort(col, order);
    }

    public void SortByAspect(string aspectName, bool descending = false)
    {
        var col = GetColumn(aspectName);
        if (col != null) SortBy(col, descending);
    }

    public void ClearSort() => ClearSort(null);

    public void ClearAllFilters() => ClearFilters();

    public void ResetFilters() => ClearFilters();

    public void Refilter() => RefreshObjects();

    public void SetFilterText(string text) => SetGlobalFilter(text ?? string.Empty);

    public string GetFilterText() => TextFilter;

    public List<object> GetObjects() => EnumerateProviderObjects().ToList();

    public List<object> GetFilteredObjects() => GetVisibleObjects().ToList();

    public List<T> GetObjects<T>() => ObjectsAs<T>();

    public List<T> GetFilteredObjects<T>() => FilteredObjectsAs<T>();

    public void ReplaceObject(object? oldModel, object? newModel, bool preserveSelection = true)
    {
        if (oldModel == null || newModel == null) return;
        var list = EnumerateProviderObjects().ToList();
        int index = list.FindIndex(x => ReferenceEquals(x, oldModel) || Equals(x, oldModel));
        if (index < 0) return;
        bool wasSelected = preserveSelection && IsSelected(oldModel);
        bool wasChecked = IsChecked(oldModel);
        list[index] = newModel;
        SetObjects(list);
        if (wasSelected) SelectObject(newModel);
        if (wasChecked) SetObjectChecked(newModel, true);
        OnObjectsChanged();
    }

    public void MoveObject(object? model, int newIndex)
    {
        if (model == null) return;
        var list = EnumerateProviderObjects().ToList();
        int oldIndex = list.FindIndex(x => ReferenceEquals(x, model) || Equals(x, model));
        if (oldIndex < 0) return;
        newIndex = Math.Max(0, Math.Min(newIndex, list.Count - 1));
        if (oldIndex == newIndex) return;
        list.RemoveAt(oldIndex);
        list.Insert(newIndex, model);
        SetObjects(list);
        SelectObject(model);
        OnObjectsChanged();
    }

    internal void OnObjectCheckStateChanged(object model, int rowIndex, CheckState state)
    {
        var args = new ViewGridItemCheckedEventArgs(model, rowIndex, state);
        ItemChecked?.Invoke(this, args);
        ObjectChecked?.Invoke(this, args);
    }

    internal void OnObjectsChanged()
    {
        ObjectsChanged?.Invoke(this, new ViewGridObjectsChangedEventArgs(ObjectCount, FilteredCount));
    }

    internal void OnObjectAdded(object model, int rowIndex)
    {
        ObjectAdded?.Invoke(this, new ViewGridDataModelEventArgs(model, rowIndex));
        OnObjectsChanged();
    }

    internal void OnObjectRemoved(object? model, int rowIndex)
    {
        ObjectRemoved?.Invoke(this, new ViewGridDataModelEventArgs(model, rowIndex));
        OnObjectsChanged();
    }

    private void SetAllCheckBoxValues(bool value)
    {
        var col = Columns.FirstOrDefault(x => x.Kind == ViewGridColumnKind.CheckBox);
        if (col == null) return;
        BeginUpdate();
        try
        {
            foreach (var row in EnumerateProviderObjects())
                col.PutValue(row, value);
        }
        finally
        {
            EndUpdate();
        }
    }
}
