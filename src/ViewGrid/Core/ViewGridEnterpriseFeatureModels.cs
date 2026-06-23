using System.Collections;
using System.ComponentModel;
using ViewGrid.Columns;

namespace ViewGrid.Core;

public enum ViewGridCardActionPlacement
{
    TopRight = 0,
    TopLeft = 1,
    BottomRight = 2,
    BottomLeft = 3
}

public enum ViewGridLiveUpdateMode
{
    Off = 0,
    ManualDiff = 1,
    TimerRefresh = 2,
    ProviderWatch = 3
}

public enum ViewGridChangedRowAnimation
{
    None = 0,
    Flash = 1,
    AccentPulse = 2
}

public sealed class ViewGridCardAction
{
    public string Key { get; set; } = string.Empty;
    public string? Text { get; set; }
    public Image? Image { get; set; }
    public ViewGridCardGlyph Glyph { get; set; } = ViewGridCardGlyph.None;
    public ViewGridCardActionPlacement Placement { get; set; } = ViewGridCardActionPlacement.TopRight;
    public Color? BackColor { get; set; }
    public Color? ForeColor { get; set; }
    public string? ToolTipText { get; set; }
    public bool Visible { get; set; } = true;
    public Action<object>? Click { get; set; }
}

public sealed class ViewGridCardActionClickEventArgs : EventArgs
{
    public ViewGridCardActionClickEventArgs(object rowObject, ViewGridCardAction action)
    {
        RowObject = rowObject;
        Action = action;
    }

    public object RowObject { get; }
    public ViewGridCardAction Action { get; }
}

public sealed class ViewGridConditionalRule
{
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public ViewGridColumn? Column { get; set; }
    public Func<object, ViewGridColumn?, bool> Condition { get; set; } = (_, _) => false;
    public Color? BackColor { get; set; }
    public Color? ForeColor { get; set; }
    public Color? AccentColor { get; set; }
    public Image? Icon { get; set; }
    public bool StopIfTrue { get; set; }
}

public sealed class ViewGridSmartSearchToken
{
    public string? ColumnKey { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool Exclude { get; set; }
}

public sealed class ViewGridPluginCollection : CollectionBase
{
    private readonly ViewGridControl _owner;

    internal ViewGridPluginCollection(ViewGridControl owner)
    {
        _owner = owner;
    }

    public void Add(IViewGridPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        List.Add(plugin);
        plugin.Attach(_owner);
    }

    public void Remove(IViewGridPlugin plugin)
    {
        if (plugin == null) return;
        if (List.Contains(plugin))
        {
            plugin.Detach(_owner);
            List.Remove(plugin);
        }
    }

    public IViewGridPlugin this[int index] => (IViewGridPlugin)List[index]!;
}

public interface IViewGridPlugin
{
    string Name { get; }
    void Attach(ViewGridControl grid);
    void Detach(ViewGridControl grid);
}
