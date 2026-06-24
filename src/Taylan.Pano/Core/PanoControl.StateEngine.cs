using System.ComponentModel;
using System.Reflection;
using Taylan.Pano.State;

namespace Taylan.Pano.Core;

public partial class PanoControl
{
    [Category("Pano - State"), DefaultValue("")]
    [Description("SaveState/LoadState seçimlerini korumak için kullanılacak benzersiz property adı. Örn: Id, Code, MaterialCode, TicketId.")]
    public string StateKeyAspectName { get; set; } = string.Empty;

    [Category("Pano - State"), DefaultValue(true)]
    [Description("SaveState/LoadState çağrılarında seçili satırları StateKeyAspectName üzerinden saklar.")]
    public bool PersistSelectionInState { get; set; } = true;

    [Category("Pano - State"), DefaultValue(true)]
    [Description("SaveState/LoadState çağrılarında checked satırları StateKeyAspectName üzerinden saklar.")]
    public bool PersistCheckedRowsInState { get; set; } = true;

    public PanoState CaptureState(string name = "")
    {
        var state = new PanoState
        {
            Name = name ?? string.Empty,
            ViewMode = ViewMode.ToString(),
            Scenario = ActiveScenario.ToString(),
            Layout = CaptureLayout(),
            SavedAtUtc = DateTime.UtcNow
        };

        if (PersistSelectionInState)
            state.SelectedKeys = SelectedObjects.Select(GetStateKey).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (PersistCheckedRowsInState)
            state.CheckedKeys = CheckedObjects.Select(GetStateKey).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return state;
    }

    public void ApplyState(PanoState state)
    {
        if (state == null) return;

        if (state.Layout != null)
            ApplyLayout(state.Layout);

        if (!string.IsNullOrWhiteSpace(state.Scenario) && Enum.TryParse<PanoScenario>(state.Scenario, out var scenario))
            ActiveScenario = scenario;

        if (!string.IsNullOrWhiteSpace(state.ViewMode) && Enum.TryParse<PanoViewMode>(state.ViewMode, out var viewMode))
            ViewMode = viewMode;

        if (PersistSelectionInState && state.SelectedKeys.Count > 0)
            RestoreSelectionByKeys(state.SelectedKeys);

        if (PersistCheckedRowsInState && state.CheckedKeys.Count > 0)
            RestoreCheckedByKeys(state.CheckedKeys);

        Invalidate();
    }

    public void SaveState(string path, string name = "")
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("State dosya yolu boş olamaz.", nameof(path));
        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(path, CaptureState(name).ToJson());
    }

    public bool LoadState(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        ApplyState(PanoState.FromJson(File.ReadAllText(path)));
        return true;
    }

    private void RestoreSelectionByKeys(IEnumerable<string> keys)
    {
        var wanted = new HashSet<string>(keys.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0) return;

        ClearSelection();
        foreach (object row in GetVisibleObjects())
        {
            string key = GetStateKey(row);
            if (wanted.Contains(key))
                SelectObject(row, addToSelection: true);
        }
    }

    private void RestoreCheckedByKeys(IEnumerable<string> keys)
    {
        var wanted = new HashSet<string>(keys.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0) return;

        foreach (object row in GetVisibleObjects())
        {
            string key = GetStateKey(row);
            if (wanted.Contains(key))
                SetObjectChecked(row, true);
        }
    }

    private string GetStateKey(object? row)
    {
        if (row == null) return string.Empty;
        string aspect = StateKeyAspectName;
        if (string.IsNullOrWhiteSpace(aspect))
            aspect = Columns.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.AspectName))?.AspectName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(aspect))
            return row.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);

        object? value = TryGetValue(row, aspect);
        return Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
    }

    private static object? TryGetValue(object row, string aspect)
    {
        if (row is System.Collections.IDictionary dict && dict.Contains(aspect)) return dict[aspect];
        var type = row.GetType();
        var prop = type.GetProperty(aspect, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop != null) return prop.GetValue(row);
        var field = type.GetField(aspect, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return field?.GetValue(row);
    }
}
