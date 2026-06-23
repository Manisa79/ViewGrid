using System.ComponentModel;
using System.Text.Json.Serialization;
using ViewGrid.Columns;

namespace ViewGrid.Core;

public sealed class ViewGridUltimateOptions
{
    [DefaultValue(true)] public bool EnableQueryLanguage { get; set; } = true;
    [DefaultValue(true)] public bool EnableExpressionEngine { get; set; } = true;
    [DefaultValue(true)] public bool EnableActionPipeline { get; set; } = true;
    [DefaultValue(true)] public bool EnableChangeTracking { get; set; } = true;
    [DefaultValue(true)] public bool EnableLayoutPackages { get; set; } = true;
    [DefaultValue(true)] public bool EnableEventBus { get; set; } = true;
    [DefaultValue(true)] public bool EnableSmartSuggestions { get; set; } = true;
    [DefaultValue(true)] public bool EnableDataProfiling { get; set; } = true;
    [DefaultValue(true)] public bool EnableKeyboardShortcuts { get; set; } = true;
    [DefaultValue(true)] public bool EnablePowerUserCommands { get; set; } = true;
}

public sealed class ViewGridActionContext
{
    public ViewGridActionContext(ViewGridControl grid, object? rowObject, ViewGridColumn? column, string trigger)
    {
        Grid = grid;
        RowObject = rowObject;
        Column = column;
        Trigger = trigger;
    }

    public ViewGridControl Grid { get; }
    public object? RowObject { get; }
    public ViewGridColumn? Column { get; }
    public string Trigger { get; }
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
}

public sealed class ViewGridActionStep
{
    public string Name { get; set; } = string.Empty;
    public string Trigger { get; set; } = "manual";
    public Func<ViewGridActionContext, bool>? Condition { get; set; }
    public Action<ViewGridActionContext>? Execute { get; set; }
    public bool ContinueOnError { get; set; }
}

public sealed class ViewGridActionPipeline
{
    private readonly List<ViewGridActionStep> _steps = new();
    public IReadOnlyList<ViewGridActionStep> Steps => _steps;

    public void Add(ViewGridActionStep step)
    {
        if (step == null) throw new ArgumentNullException(nameof(step));
        _steps.RemoveAll(x => string.Equals(x.Name, step.Name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(step.Name));
        _steps.Add(step);
    }

    public void Clear() => _steps.Clear();

    public int Run(ViewGridActionContext context)
    {
        int count = 0;
        foreach (ViewGridActionStep step in _steps.Where(s => string.Equals(s.Trigger, context.Trigger, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                if (step.Condition != null && !step.Condition(context)) continue;
                step.Execute?.Invoke(context);
                count++;
            }
            catch
            {
                if (!step.ContinueOnError) throw;
            }
        }
        return count;
    }
}

public sealed class ViewGridEventSubscription : IDisposable
{
    private readonly Action _dispose;
    internal ViewGridEventSubscription(Action dispose) { _dispose = dispose; }
    public void Dispose() => _dispose();
}

public sealed class ViewGridEventBus
{
    private readonly Dictionary<string, List<Action<ViewGridEventPayload>>> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public ViewGridEventSubscription Subscribe(string eventName, Action<ViewGridEventPayload> handler)
    {
        if (string.IsNullOrWhiteSpace(eventName)) throw new ArgumentException("Event name cannot be empty.", nameof(eventName));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (!_handlers.TryGetValue(eventName, out List<Action<ViewGridEventPayload>>? list))
        {
            list = new List<Action<ViewGridEventPayload>>();
            _handlers[eventName] = list;
        }
        list.Add(handler);
        return new ViewGridEventSubscription(() => list.Remove(handler));
    }

    public void Publish(string eventName, ViewGridEventPayload payload)
    {
        if (!_handlers.TryGetValue(eventName, out List<Action<ViewGridEventPayload>>? list)) return;
        foreach (Action<ViewGridEventPayload> handler in list.ToArray()) handler(payload);
    }
}

public sealed class ViewGridEventPayload
{
    public ViewGridEventPayload(ViewGridControl grid, object? rowObject = null, ViewGridColumn? column = null)
    {
        Grid = grid;
        RowObject = rowObject;
        Column = column;
    }

    public ViewGridControl Grid { get; }
    public object? RowObject { get; }
    public ViewGridColumn? Column { get; }
    public IDictionary<string, object?> Data { get; } = new Dictionary<string, object?>();
}

public sealed class ViewGridTrackedCellChange
{
    public string ColumnKey { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.Now;
}

public sealed class ViewGridRowChangeSet
{
    public object? RowObject { get; set; }
    public string RowKey { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.Now;
    public List<ViewGridTrackedCellChange> Changes { get; } = new();
}

public sealed class ViewGridDataColumnProfile
{
    public string ColumnKey { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public int VisibleCount { get; set; }
    public int NullCount { get; set; }
    public int BlankCount { get; set; }
    public int UniqueCount { get; set; }
    public bool Numeric { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? Average { get; set; }
    public List<string> TopValues { get; set; } = new();
    public List<string> Samples { get; set; } = new();
}

public sealed class ViewGridLayoutPackage
{
    public string Name { get; set; } = string.Empty;
    public string ViewGridVersion { get; set; } = ViewGridVersionInfo.Version;
    public DateTime ExportedAt { get; set; } = DateTime.Now;
    public string ViewMode { get; set; } = string.Empty;
    public int RowHeight { get; set; }
    public string Query { get; set; } = string.Empty;
    public List<ViewGridColumnLayoutItem> Columns { get; set; } = new();
    public Dictionary<string, string> FilterPresets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ViewGridColumnLayoutItem
{
    public string Key { get; set; } = string.Empty;
    public string AspectName { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public bool Visible { get; set; }
    public int Width { get; set; }
    public int DisplayIndex { get; set; }
    public string PinMode { get; set; } = string.Empty;
}

public sealed class ViewGridSuggestion
{
    public string Text { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string InsertText { get; set; } = string.Empty;
    public override string ToString() => string.IsNullOrWhiteSpace(Category) ? Text : $"{Text}  ({Category})";
}
