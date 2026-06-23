using System.ComponentModel;
using ViewGrid.Virtualization;

namespace ViewGrid.Core;

public enum ViewGridDropPosition
{
    None,
    Before,
    After,
    Into
}

public enum ViewGridExternalDropKind
{
    Unknown,
    Text,
    Files,
    Rows
}

public sealed class ViewGridRowDragStartingEventArgs : CancelEventArgs
{
    public ViewGridRowDragStartingEventArgs(IReadOnlyList<object> rows, int sourceViewIndex)
    {
        Rows = rows;
        SourceViewIndex = sourceViewIndex;
    }

    public IReadOnlyList<object> Rows { get; }
    public int SourceViewIndex { get; }
}

public sealed class ViewGridRowDropValidatingEventArgs : CancelEventArgs
{
    public ViewGridRowDropValidatingEventArgs(IReadOnlyList<object> rows, int targetViewIndex, ViewGridDropPosition position)
    {
        Rows = rows;
        TargetViewIndex = targetViewIndex;
        Position = position;
    }

    public IReadOnlyList<object> Rows { get; }
    public int TargetViewIndex { get; }
    public ViewGridDropPosition Position { get; set; }
}

public sealed class ViewGridRowDroppedEventArgs : EventArgs
{
    public ViewGridRowDroppedEventArgs(IReadOnlyList<object> rows, int targetViewIndex, ViewGridDropPosition position)
    {
        Rows = rows;
        TargetViewIndex = targetViewIndex;
        Position = position;
    }

    public IReadOnlyList<object> Rows { get; }
    public int TargetViewIndex { get; }
    public ViewGridDropPosition Position { get; }
}

public sealed class ViewGridExternalDropEventArgs : CancelEventArgs
{
    public ViewGridExternalDropEventArgs(ViewGridExternalDropKind kind, object? payload, int targetViewIndex, ViewGridDropPosition position)
    {
        Kind = kind;
        Payload = payload;
        TargetViewIndex = targetViewIndex;
        Position = position;
    }

    public ViewGridExternalDropKind Kind { get; }
    public object? Payload { get; }
    public int TargetViewIndex { get; }
    public ViewGridDropPosition Position { get; }
}

public partial class ViewGridControl
{
    private Point _smartDragStartPoint;
    private int _smartDragStartViewIndex = -1;
    private bool _smartDragInstalled;
    private int _smartDropTargetViewIndex = -1;
    private ViewGridDropPosition _smartDropPosition = ViewGridDropPosition.None;

    [Category("ViewGrid - Smart Drag Drop")]
    [DefaultValue(false)]
    [Description("Satırların sürükle bırak ile taşınmasına izin verir. Multi-select varsa seçili satırlar birlikte taşınır.")]
    public bool AllowRowDragDrop { get; set; }

    [Category("ViewGrid - Smart Drag Drop")]
    [DefaultValue(true)]
    [Description("Sürükle bırak sırasında hedef satır çizgisini gösterir.")]
    public bool ShowDropIndicator { get; set; } = true;

    [Category("ViewGrid - Smart Drag Drop")]
    [DefaultValue(true)]
    [Description("Sürüklerken listenin üst/alt kenarında otomatik scroll yapar.")]
    public bool AutoScrollOnDrag { get; set; } = true;

    [Category("ViewGrid - Smart Drag Drop")]
    [DefaultValue(false)]
    [Description("Dosya veya metin gibi dış kaynakların listeye bırakılmasına izin verir.")]
    public bool AllowExternalDrop { get; set; }

    [Category("ViewGrid - Smart Drag Drop")]
    [DefaultValue(true)]
    [Description("Satır drop sonrası liste sıralamasını otomatik günceller. DB/virtual sağlayıcıda RowDropped event'i ile özel kayıt yapılabilir.")]
    public bool AutoReorderRowsOnDrop { get; set; } = true;

    [Category("ViewGrid - Smart Drag Drop")]
    [DefaultValue(true)]
    [Description("Drop sonrası kullanıcı layout/state otomatik kaydetme açıksa kaydı tetikler.")]
    public bool SaveLayoutAfterDragDrop { get; set; } = true;

    [Category("ViewGrid - Smart Drag Drop")]
    [DefaultValue(false)]
    [Description("Dışarıdan dosya/metin bırakıldığında kullanıcı onayı istenmesini sağlar.")]
    public bool ConfirmExternalDrop { get; set; }

    [Category("ViewGrid - Smart Drag Drop")]
    [DefaultValue(true)]
    [Description("Sürükle bırak için sol mouse tuşu kullanılır.")]
    public bool DragDropUsesLeftMouseButton { get; set; } = true;

    public event EventHandler<ViewGridRowDragStartingEventArgs>? RowDragStarting;
    public event EventHandler<ViewGridRowDropValidatingEventArgs>? RowDropValidating;
    public event EventHandler<ViewGridRowDroppedEventArgs>? RowDropped;
    public event EventHandler<ViewGridExternalDropEventArgs>? ExternalDropReceived;

    private void InstallSmartDragDropHandlers()
    {
        if (_smartDragInstalled) return;
        _smartDragInstalled = true;
        AllowDrop = true;

        MouseDown += SmartDragDrop_MouseDown;
        MouseMove += SmartDragDrop_MouseMove;
        DragEnter += SmartDragDrop_DragEnter;
        DragOver += SmartDragDrop_DragOver;
        DragLeave += SmartDragDrop_DragLeave;
        DragDrop += SmartDragDrop_DragDrop;
    }

    private void SmartDragDrop_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!AllowRowDragDrop) return;
        if (ShowHeader && e.Y < HeaderHeight) return;
        if (DragDropUsesLeftMouseButton && e.Button != MouseButtons.Left) return;
        if (!DragDropUsesLeftMouseButton && e.Button != MouseButtons.Right) return;
        _smartDragStartPoint = e.Location;
        _smartDragStartViewIndex = HitRow(e.Y);
    }

    private void SmartDragDrop_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!AllowRowDragDrop || _smartDragStartViewIndex < 0) return;
        if (DragDropUsesLeftMouseButton && e.Button != MouseButtons.Left) return;
        if (!DragDropUsesLeftMouseButton && e.Button != MouseButtons.Right) return;

        var dragRect = new Rectangle(
            _smartDragStartPoint.X - SystemInformation.DragSize.Width / 2,
            _smartDragStartPoint.Y - SystemInformation.DragSize.Height / 2,
            SystemInformation.DragSize.Width,
            SystemInformation.DragSize.Height);
        if (dragRect.Contains(e.Location)) return;

        var rows = GetRowsForSmartDrag(_smartDragStartViewIndex);
        if (rows.Count == 0) return;

        var args = new ViewGridRowDragStartingEventArgs(rows, _smartDragStartViewIndex);
        RowDragStarting?.Invoke(this, args);
        if (args.Cancel) return;

        DoDragDrop(new SmartRowDragPayload(this, rows), DragDropEffects.Move | DragDropEffects.Copy);
        _smartDragStartViewIndex = -1;
    }

    private void SmartDragDrop_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = GetSmartDragEffect(e);
    }

    private void SmartDragDrop_DragOver(object? sender, DragEventArgs e)
    {
        var pt = PointToClient(new Point(e.X, e.Y));
        UpdateSmartDropTarget(pt);
        if (AutoScrollOnDrag) AutoScrollForDragPoint(pt);
        e.Effect = GetSmartDragEffect(e);
    }

    private void SmartDragDrop_DragLeave(object? sender, EventArgs e)
    {
        _smartDropTargetViewIndex = -1;
        _smartDropPosition = ViewGridDropPosition.None;
        Invalidate();
    }

    private void SmartDragDrop_DragDrop(object? sender, DragEventArgs e)
    {
        var pt = PointToClient(new Point(e.X, e.Y));
        UpdateSmartDropTarget(pt);

        if (e.Data?.GetData(typeof(SmartRowDragPayload)) is SmartRowDragPayload payload)
        {
            var validate = new ViewGridRowDropValidatingEventArgs(payload.Rows, _smartDropTargetViewIndex, _smartDropPosition);
            RowDropValidating?.Invoke(this, validate);
            if (validate.Cancel) return;

            if (AutoReorderRowsOnDrop && ReferenceEquals(payload.Source, this))
                ReorderRowsFromSmartDrop(payload.Rows, validate.TargetViewIndex, validate.Position);

            RowDropped?.Invoke(this, new ViewGridRowDroppedEventArgs(payload.Rows, validate.TargetViewIndex, validate.Position));
            TryAutoSaveLayoutAfterDragDrop();
            ClearSmartDropIndicator();
            return;
        }

        if (AllowExternalDrop)
        {
            var external = CreateExternalDropArgs(e.Data, _smartDropTargetViewIndex, _smartDropPosition);
            if (external != null)
            {
                if (ConfirmExternalDrop)
                {
                    var result = MessageBox.Show(FindForm(), "Bu veriler listeye bırakılsın mı?", "ViewGrid Drag Drop", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result != DialogResult.Yes) return;
                }
                ExternalDropReceived?.Invoke(this, external);
            }
        }

        ClearSmartDropIndicator();
    }

    private DragDropEffects GetSmartDragEffect(DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(typeof(SmartRowDragPayload)) == true && AllowRowDragDrop) return DragDropEffects.Move;
        if (AllowExternalDrop && e.Data != null && (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text))) return DragDropEffects.Copy;
        return DragDropEffects.None;
    }

    private IReadOnlyList<object> GetRowsForSmartDrag(int startViewIndex)
    {
        var selected = SelectedObjects;
        if (selected.Count > 0 && _selectedRows.Contains(startViewIndex)) return selected;
        var row = GetViewRow(startViewIndex);
        return row == null ? Array.Empty<object>() : new[] { row };
    }

    private void UpdateSmartDropTarget(Point pt)
    {
        int rowIndex = HitRow(pt.Y);
        _smartDropTargetViewIndex = rowIndex;
        _smartDropPosition = ViewGridDropPosition.None;
        if (rowIndex >= 0)
        {
            int header = ShowHeader ? HeaderHeight : 0;
            int rowTop = header + (rowIndex - _scrollY) * RowHeight;
            int delta = pt.Y - rowTop;
            _smartDropPosition = delta < RowHeight / 2 ? ViewGridDropPosition.Before : ViewGridDropPosition.After;
        }
        Invalidate();
    }

    private void AutoScrollForDragPoint(Point pt)
    {
        int header = ShowHeader ? HeaderHeight : 0;
        if (pt.Y < header + 24) ScrollVertical(_scrollY - 1);
        else if (pt.Y > Height - 24) ScrollVertical(_scrollY + 1);
    }

    private void ReorderRowsFromSmartDrop(IReadOnlyList<object> rows, int targetViewIndex, ViewGridDropPosition position)
    {
        if (rows.Count == 0 || _provider.Count == 0) return;
        var allRows = new List<object>();
        for (int i = 0; i < _provider.Count; i++)
        {
            var row = _provider.GetRow(i);
            if (row != null) allRows.Add(row);
        }

        var moving = rows.Where(r => allRows.Contains(r)).Distinct().ToList();
        if (moving.Count == 0) return;
        foreach (var row in moving) allRows.Remove(row);

        object? targetObject = targetViewIndex >= 0 ? GetViewRow(targetViewIndex) : null;
        int insertIndex = targetObject == null ? allRows.Count : allRows.IndexOf(targetObject);
        if (insertIndex < 0) insertIndex = allRows.Count;
        if (position == ViewGridDropPosition.After) insertIndex++;
        insertIndex = Math.Clamp(insertIndex, 0, allRows.Count);

        allRows.InsertRange(insertIndex, moving);
        SetObjects(allRows);
        SelectObjects(moving);
    }

    private ViewGridExternalDropEventArgs? CreateExternalDropArgs(IDataObject? data, int targetViewIndex, ViewGridDropPosition position)
    {
        if (data == null) return null;
        if (data.GetDataPresent(DataFormats.FileDrop))
            return new ViewGridExternalDropEventArgs(ViewGridExternalDropKind.Files, data.GetData(DataFormats.FileDrop), targetViewIndex, position);
        if (data.GetDataPresent(DataFormats.Text))
            return new ViewGridExternalDropEventArgs(ViewGridExternalDropKind.Text, data.GetData(DataFormats.Text), targetViewIndex, position);
        return new ViewGridExternalDropEventArgs(ViewGridExternalDropKind.Unknown, data, targetViewIndex, position);
    }

    private void ClearSmartDropIndicator()
    {
        _smartDropTargetViewIndex = -1;
        _smartDropPosition = ViewGridDropPosition.None;
        Invalidate();
    }

    private void TryAutoSaveLayoutAfterDragDrop()
    {
        if (!SaveLayoutAfterDragDrop) return;
        try
        {
            var method = GetType().GetMethod("SaveUserLayout", Type.EmptyTypes);
            method?.Invoke(this, null);
        }
        catch { }
    }

    private sealed class SmartRowDragPayload
    {
        public SmartRowDragPayload(ViewGridControl source, IReadOnlyList<object> rows)
        {
            Source = source;
            Rows = rows;
        }

        public ViewGridControl Source { get; }
        public IReadOnlyList<object> Rows { get; }
    }
}
