using ViewGrid.Columns;

namespace ViewGrid.Undo;

public sealed class ViewGridCellChange
{
    public object RowObject { get; init; } = null!;
    public ViewGridColumn Column { get; init; } = null!;
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}

public sealed class ViewGridUndoService
{
    private readonly Stack<ViewGridCellChange> _undo = new();
    private readonly Stack<ViewGridCellChange> _redo = new();
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public void Push(ViewGridCellChange change) { _undo.Push(change); _redo.Clear(); }
    public ViewGridCellChange? Undo() { if (_undo.Count == 0) return null; var c = _undo.Pop(); c.Column.PutValue(c.RowObject, c.OldValue); _redo.Push(c); return c; }
    public ViewGridCellChange? Redo() { if (_redo.Count == 0) return null; var c = _redo.Pop(); c.Column.PutValue(c.RowObject, c.NewValue); _undo.Push(c); return c; }
    public void Clear() { _undo.Clear(); _redo.Clear(); }
}
