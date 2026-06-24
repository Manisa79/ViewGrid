using Taylan.Pano.Columns;

namespace Taylan.Pano.Undo;

public sealed class PanoCellChange
{
    public object RowObject { get; init; } = null!;
    public PanoColumn Column { get; init; } = null!;
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}

public sealed class PanoUndoService
{
    private readonly Stack<PanoCellChange> _undo = new();
    private readonly Stack<PanoCellChange> _redo = new();
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public void Push(PanoCellChange change) { _undo.Push(change); _redo.Clear(); }
    public PanoCellChange? Undo() { if (_undo.Count == 0) return null; var c = _undo.Pop(); c.Column.PutValue(c.RowObject, c.OldValue); _redo.Push(c); return c; }
    public PanoCellChange? Redo() { if (_redo.Count == 0) return null; var c = _redo.Pop(); c.Column.PutValue(c.RowObject, c.NewValue); _undo.Push(c); return c; }
    public void Clear() { _undo.Clear(); _redo.Clear(); }
}
