using System.ComponentModel;
using System.Text.Json;
using Taylan.Pano.Columns;
using Taylan.Pano.Intelligence;

namespace Taylan.Pano.Core;

public partial class PanoControl
{
    private readonly PanoQueryEngine _v2815QueryEngine = new PanoQueryEngine();
    private readonly PanoExpressionEngine _v2815ExpressionEngine = new PanoExpressionEngine();
    private readonly PanoActionPipeline _v2815Actions = new PanoActionPipeline();
    private readonly PanoEventBus _v2815Events = new PanoEventBus();
    private readonly PanoChangeTracker _v2815ChangeTracker = new PanoChangeTracker();
    private Func<object, bool>? _v2815QueryPredicate;
    private string _v2815QueryText = string.Empty;

    [Category("Pano - Ultimate")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public PanoUltimateOptions UltimateFeatures { get; } = new PanoUltimateOptions();

    [Category("Pano - Ultimate")]
    [DefaultValue("")]
    [Description("Power-user query language. Examples: status:open AND machine:LINE1, qty > 10, date >= 2026-01-01.")]
    public string Query
    {
        get => _v2815QueryText;
        set => ApplyQuery(value ?? string.Empty);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PanoActionPipeline Actions => _v2815Actions;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PanoEventBus Events => _v2815Events;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PanoChangeTracker ChangeTracker => _v2815ChangeTracker;

    public void EnableUltimateDefaults()
    {
        EnablePowerKeyboardShortcuts();
        EnableCopyPro = true;
        EnableCommandPalette = true;
        PlatformFeatures.EnableCommandPalette = true;
        PlatformFeatures.EnableCopyPro = true;
        PlatformFeatures.EnableFilterPresets = true;
        PlatformFeatures.EnableQuickFilterBar = true;
        UltimateFeatures.EnableQueryLanguage = true;
        UltimateFeatures.EnableExpressionEngine = true;
        UltimateFeatures.EnableActionPipeline = true;
        UltimateFeatures.EnableChangeTracking = true;
        UltimateFeatures.EnableLayoutPackages = true;
        UltimateFeatures.EnableEventBus = true;
        UltimateFeatures.EnableSmartSuggestions = true;
        UltimateFeatures.EnableDataProfiling = true;
        RegisterUltimateBuiltInCommands();
    }

    public void ApplyQuery(string query)
    {
        _v2815QueryText = query ?? string.Empty;
        _v2815QueryPredicate = string.IsNullOrWhiteSpace(_v2815QueryText) ? null : _v2815QueryEngine.Compile(_v2815QueryText);
        BuildViewIndex();
        QueueAutoSaveUserLayout();
        PublishUltimateEvent("QueryChanged", null, null, ("Query", _v2815QueryText));
    }

    public void ClearQuery()
    {
        if (string.IsNullOrWhiteSpace(_v2815QueryText)) return;
        ApplyQuery(string.Empty);
    }

    public object? EvaluateExpression(object rowObject, string expression)
    {
        if (!UltimateFeatures.EnableExpressionEngine) return null;
        return _v2815ExpressionEngine.Evaluate(rowObject, expression);
    }

    public bool EvaluateExpressionCondition(object rowObject, string condition)
    {
        if (!UltimateFeatures.EnableExpressionEngine) return false;
        return _v2815ExpressionEngine.EvaluateCondition(rowObject, condition);
    }

    public void RunActions(string trigger, object? rowObject = null, PanoColumn? column = null)
    {
        if (!UltimateFeatures.EnableActionPipeline) return;
        PanoActionContext context = new PanoActionContext(this, rowObject, column, trigger);
        int executed = Actions.Run(context);
        PublishUltimateEvent("ActionsExecuted", rowObject, column, ("Trigger", trigger), ("Executed", executed));
    }

    public void CaptureChangeSnapshot(Func<object, string>? keySelector = null)
    {
        ChangeTracker.Capture(GetRowsSnapshot(0, _provider.Count), Columns.VisibleColumns, keySelector);
    }

    public IReadOnlyList<PanoRowChangeSet> DetectChanges(Func<object, string>? keySelector = null, bool flashChangedRows = true)
    {
        IReadOnlyList<PanoRowChangeSet> changes = ChangeTracker.Detect(GetRowsSnapshot(0, _provider.Count), Columns.VisibleColumns, keySelector);
        if (flashChangedRows)
        {
            foreach (PanoRowChangeSet change in changes)
                if (change.RowObject != null) FlashObject(change.RowObject, Color.FromArgb(90, 255, 190, 80), "Changed");
        }
        PublishUltimateEvent("ChangesDetected", null, null, ("Count", changes.Count));
        return changes;
    }

    public PanoLayoutPackage CreateLayoutPackage(string name = "Default")
    {
        PanoLayoutPackage package = new PanoLayoutPackage
        {
            Name = name,
            PanoVersion = PanoVersionInfo.Version,
            ExportedAt = DateTime.Now,
            ViewMode = ViewMode.ToString(),
            RowHeight = RowHeight,
            Query = Query
        };

        int displayIndex = 0;
        foreach (PanoColumn col in Columns)
        {
            package.Columns.Add(new PanoColumnLayoutItem
            {
                Key = col.Key,
                AspectName = col.AspectName,
                Header = col.Header,
                Visible = col.Visible,
                Width = col.Width,
                DisplayIndex = displayIndex++,
                PinMode = col.PinMode.ToString()
            });
        }

        foreach (KeyValuePair<string, PanoFilterPresetInfo> preset in FilterPresets)
            package.FilterPresets[preset.Key] = preset.Value.SerializedState;

        return package;
    }

    public void ApplyLayoutPackage(PanoLayoutPackage package)
    {
        if (package == null) throw new ArgumentNullException(nameof(package));
        foreach (PanoColumnLayoutItem item in package.Columns)
        {
            PanoColumn? col = Columns.FirstOrDefault(c => string.Equals(c.Key, item.Key, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.AspectName, item.AspectName, StringComparison.OrdinalIgnoreCase));
            if (col == null) continue;
            col.Visible = item.Visible;
            col.Width = Math.Max(20, item.Width);
            if (Enum.TryParse(item.PinMode, out PanoColumnPinMode pin)) col.PinMode = pin;
        }

        if (Enum.TryParse(package.ViewMode, out PanoViewMode mode)) SetViewMode(mode);
        if (package.RowHeight > 0) RowHeight = package.RowHeight;
        ApplyQuery(package.Query ?? string.Empty);
        Invalidate();
        PublishUltimateEvent("LayoutPackageApplied", null, null, ("Name", package.Name));
    }

    public void ExportLayoutPackage(string filePath, string name = "Default")
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        PanoLayoutPackage package = CreateLayoutPackage(name);
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true }));
    }

    public PanoLayoutPackage ImportLayoutPackage(string filePath, bool apply = true)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        PanoLayoutPackage? package = JsonSerializer.Deserialize<PanoLayoutPackage>(File.ReadAllText(filePath));
        if (package == null) throw new InvalidOperationException("Layout package could not be read.");
        if (apply) ApplyLayoutPackage(package);
        return package;
    }

    public IReadOnlyList<PanoSuggestion> GetSmartSuggestions(string prefix)
    {
        List<PanoSuggestion> suggestions = new List<PanoSuggestion>();
        string q = prefix ?? string.Empty;
        foreach (PanoColumn col in Columns.VisibleColumns)
        {
            string header = col.Header ?? string.Empty;
            string aspect = col.AspectName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(q) || header.StartsWith(q, StringComparison.CurrentCultureIgnoreCase) || aspect.StartsWith(q, StringComparison.CurrentCultureIgnoreCase))
            {
                suggestions.Add(new PanoSuggestion { Text = header, Category = "Column", InsertText = string.IsNullOrWhiteSpace(aspect) ? header : aspect + ":" });
            }
        }
        foreach (string op in new[] { "AND", "OR", "NOT", ">=", "<=", "!=", "~" })
        {
            if (string.IsNullOrWhiteSpace(q) || op.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                suggestions.Add(new PanoSuggestion { Text = op, Category = "Operator", InsertText = op + " " });
        }
        foreach (PanoCommandPaletteCommand cmd in CommandPaletteCommands)
        {
            if (string.IsNullOrWhiteSpace(q) || cmd.Text.StartsWith(q, StringComparison.CurrentCultureIgnoreCase))
                suggestions.Add(new PanoSuggestion { Text = cmd.Text, Category = "Command", InsertText = cmd.Text });
        }
        return suggestions.Take(40).ToList();
    }

    public PanoDataColumnProfile GetColumnProfile(PanoColumn column, int maxRows = 10000)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        Dictionary<string, int> frequencies = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
        List<string> samples = new List<string>();
        int nullCount = 0;
        int blankCount = 0;
        int numericCount = 0;
        double sum = 0;
        double? min = null;
        double? max = null;
        int count = 0;

        foreach (object row in GetRowsSnapshot(0, Math.Min(_provider.Count, Math.Max(1, maxRows))))
        {
            count++;
            object? value = column.GetValue(row);
            if (value == null || value == DBNull.Value) { nullCount++; continue; }
            string text = Convert.ToString(value) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) blankCount++;
            if (samples.Count < 8 && !string.IsNullOrWhiteSpace(text)) samples.Add(text);
            frequencies[text] = frequencies.TryGetValue(text, out int f) ? f + 1 : 1;
            if (double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out double d))
            {
                numericCount++;
                sum += d;
                min = min.HasValue ? Math.Min(min.Value, d) : d;
                max = max.HasValue ? Math.Max(max.Value, d) : d;
            }
        }

        return new PanoDataColumnProfile
        {
            ColumnKey = column.Key,
            Header = column.Header,
            VisibleCount = count,
            NullCount = nullCount,
            BlankCount = blankCount,
            UniqueCount = frequencies.Count,
            Numeric = numericCount > 0 && numericCount >= Math.Max(1, count - nullCount - blankCount) / 2,
            Min = min,
            Max = max,
            Average = numericCount > 0 ? sum / numericCount : null,
            TopValues = frequencies.OrderByDescending(x => x.Value).ThenBy(x => x.Key).Take(10).Select(x => $"{x.Key} ({x.Value})").ToList(),
            Samples = samples
        };
    }

    public IReadOnlyList<PanoDataColumnProfile> GetDataProfile(int maxRowsPerColumn = 10000)
    {
        return Columns.VisibleColumns.Select(c => GetColumnProfile(c, maxRowsPerColumn)).ToList();
    }

    private void PublishUltimateEvent(string eventName, object? rowObject = null, PanoColumn? column = null, params (string Key, object? Value)[] values)
    {
        PanoEventPayload payload = new PanoEventPayload(this, rowObject, column);
        foreach ((string Key, object? Value) item in values)
            payload.Data[item.Key] = item.Value;
        Events.Publish(eventName, payload);
    }

    private bool SafeUltimateQueryPasses(object row)
    {
        try { return _v2815QueryPredicate == null || _v2815QueryPredicate(row); }
        catch { return false; }
    }

    private void RegisterUltimateBuiltInCommands()
    {
        AddCommand("ultimate-clear-query", "Query temizle", g => g.ClearQuery(), "Ultimate", "Ctrl+Shift+Q");
        AddCommand("ultimate-copy-json", "JSON olarak kopyala", g => g.CopySelection(PanoCopyFormat.Json), "Ultimate", "Ctrl+Shift+C");
        AddCommand("ultimate-profile", "Kolon profilini çıkar", g =>
        {
            PanoColumn? col = g.Columns.VisibleColumns.FirstOrDefault();
            if (col == null) return;
            PanoDataColumnProfile profile = g.GetColumnProfile(col);
            MessageBox.Show($"{profile.Header}\nUnique: {profile.UniqueCount}\nNull: {profile.NullCount}\nTop: {string.Join(", ", profile.TopValues.Take(3))}", "Pano Profil", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }, "Ultimate");
    }
}
