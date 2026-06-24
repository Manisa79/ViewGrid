using System.Collections;
using System.ComponentModel;
using Taylan.Pano.Columns;

namespace Taylan.Pano.Core;

public enum PanoCardActionPlacement
{
    TopRight = 0,
    TopLeft = 1,
    BottomRight = 2,
    BottomLeft = 3
}

public enum PanoLiveUpdateMode
{
    Off = 0,
    ManualDiff = 1,
    TimerRefresh = 2,
    ProviderWatch = 3
}

public enum PanoChangedRowAnimation
{
    None = 0,
    Flash = 1,
    AccentPulse = 2
}

public sealed class PanoCardAction
{
    public string Key { get; set; } = string.Empty;
    public string? Text { get; set; }
    public Image? Image { get; set; }
    public PanoCardGlyph Glyph { get; set; } = PanoCardGlyph.None;
    public PanoCardActionPlacement Placement { get; set; } = PanoCardActionPlacement.TopRight;
    public Color? BackColor { get; set; }
    public Color? ForeColor { get; set; }
    public string? ToolTipText { get; set; }
    public bool Visible { get; set; } = true;
    public Action<object>? Click { get; set; }
}

public sealed class PanoCardActionClickEventArgs : EventArgs
{
    public PanoCardActionClickEventArgs(object rowObject, PanoCardAction action)
    {
        RowObject = rowObject;
        Action = action;
    }

    public object RowObject { get; }
    public PanoCardAction Action { get; }
}

public sealed class PanoConditionalRule
{
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public PanoColumn? Column { get; set; }
    public Func<object, PanoColumn?, bool> Condition { get; set; } = (_, _) => false;
    public Color? BackColor { get; set; }
    public Color? ForeColor { get; set; }
    public Color? AccentColor { get; set; }
    public Image? Icon { get; set; }
    public bool StopIfTrue { get; set; }
}

public sealed class PanoSmartSearchToken
{
    public string? ColumnKey { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool Exclude { get; set; }
}

public sealed class PanoPluginCollection : CollectionBase
{
    private readonly PanoControl _owner;

    internal PanoPluginCollection(PanoControl owner)
    {
        _owner = owner;
    }

    public void Add(IPanoPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        List.Add(plugin);
        plugin.Attach(_owner);
    }

    public void Remove(IPanoPlugin plugin)
    {
        if (plugin == null) return;
        if (List.Contains(plugin))
        {
            plugin.Detach(_owner);
            List.Remove(plugin);
        }
    }

    public IPanoPlugin this[int index] => (IPanoPlugin)List[index]!;
}

public interface IPanoPlugin
{
    string Name { get; }
    void Attach(PanoControl grid);
    void Detach(PanoControl grid);
}
