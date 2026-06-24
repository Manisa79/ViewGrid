using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Taylan.Pano.Columns;

namespace Taylan.Pano.Core;

/// <summary>
/// ObjectListView migration helpers and small productivity APIs.
/// These helpers intentionally depend only on Pano types; there is no BrightIdeasSoftware reference.
/// </summary>
public enum PanoSelectionRestoreMode
{
    None = 0,
    ByReference = 1,
    ByAspectValue = 2
}

public sealed class PanoSelectionSnapshot
{
    internal PanoSelectionSnapshot(List<object> references, List<object?> keys, string keyAspectName, int scrollY)
    {
        References = references;
        Keys = keys;
        KeyAspectName = keyAspectName;
        ScrollY = scrollY;
    }

    public IReadOnlyList<object> References { get; }
    public IReadOnlyList<object?> Keys { get; }
    public string KeyAspectName { get; }
    public int ScrollY { get; }
}

public partial class PanoControl
{
    [Category("Pano - Compatibility")]
    [DefaultValue(true)]
    [Description("ObjectListView geçişi için Ctrl+A ile tüm satırları seçmeye izin verir.")]
    public bool SelectAllOnCtrlA { get; set; } = true;

    [Category("Pano - Compatibility")]
    [DefaultValue(true)]
    [Description("ObjectListView geçişinde CheckBoxes=true iken Space tuşu seçili satırların check durumunu değiştirir.")]
    public bool SpaceTogglesCheckBoxes { get => KeyboardSpaceTogglesSelectedRows; set => KeyboardSpaceTogglesSelectedRows = value; }

    [Category("Pano - Compatibility")]
    [DefaultValue(true)]
    [Description("Header çift tıklama/menü işlemlerinde kolon genişliğini içeriğe göre ayarlayan uyumluluk alias'ı.")]
    public bool AutoSizeColumnOnHeaderDoubleClick { get => EnableColumnAutoResizeOnDoubleClick; set => EnableColumnAutoResizeOnDoubleClick = value; }

    [Category("Pano - Compatibility")]
    [DefaultValue(true)]
    [Description("OLV migration alias. Pano header zaten tema uyumlu çizilir; false verilirse özel header renkleri korunur.")]
    public bool HeaderUsesThemes { get; set; } = true;

    [Browsable(false)]
    public object? FocusedObject => SelectedObject;

    [Browsable(false)]
    public int FocusedIndex => SelectedIndex;

    public PanoSelectionSnapshot CaptureSelectionSnapshot(string keyAspectName = "")
    {
        var selected = SelectedObjects.ToList();
        var keys = !string.IsNullOrWhiteSpace(keyAspectName)
            ? selected.Select(x => GetAspectValue(x, keyAspectName)).ToList()
            : new List<object?>();
        return new PanoSelectionSnapshot(selected, keys, keyAspectName ?? string.Empty, _scrollY);
    }

    public void RestoreSelectionSnapshot(PanoSelectionSnapshot? snapshot, bool ensureVisible = true)
    {
        if (snapshot == null) return;

        if (snapshot.Keys.Count > 0 && !string.IsNullOrWhiteSpace(snapshot.KeyAspectName))
        {
            SelectObjectsByAspectValues(snapshot.KeyAspectName, snapshot.Keys, ensureVisible);
        }
        else
        {
            SelectObjects(snapshot.References);
            if (ensureVisible) EnsureSelectedObjectVisible();
        }

        _scrollY = Math.Max(0, Math.Min(snapshot.ScrollY, Math.Max(0, ViewCount - 1)));
        RefreshView();
    }

    public void UpdateObjectsPreserveSelection(IEnumerable rows, string keyAspectName = "")
    {
        var snapshot = CaptureSelectionSnapshot(keyAspectName);
        SetObjects(rows == null ? Array.Empty<object>() : rows.Cast<object>());
        RestoreSelectionSnapshot(snapshot);
        OnObjectsChanged();
    }

    public object? FindObjectByAspect(string aspectName, object? value)
    {
        if (string.IsNullOrWhiteSpace(aspectName)) return null;
        return EnumerateProviderObjects().FirstOrDefault(x => ValuesEqual(GetAspectValue(x, aspectName), value));
    }

    public List<object> FindObjectsByAspect(string aspectName, object? value)
    {
        if (string.IsNullOrWhiteSpace(aspectName)) return new List<object>();
        return EnumerateProviderObjects().Where(x => ValuesEqual(GetAspectValue(x, aspectName), value)).ToList();
    }

    public bool SelectObjectByAspect(string aspectName, object? value, bool ensureVisible = true)
    {
        var obj = FindObjectByAspect(aspectName, value);
        if (obj == null) return false;
        SelectObject(obj);
        if (ensureVisible) EnsureObjectVisible(obj);
        return true;
    }

    public int SelectObjectsByAspectValues(string aspectName, IEnumerable<object?> values, bool ensureVisible = true)
    {
        if (string.IsNullOrWhiteSpace(aspectName) || values == null) return 0;
        var valueList = values.ToList();
        var found = EnumerateProviderObjects()
            .Where(row => valueList.Any(v => ValuesEqual(GetAspectValue(row, aspectName), v)))
            .ToList();

        SelectObjects(found);
        if (ensureVisible) EnsureSelectedObjectVisible();
        return found.Count;
    }

    public void RevealObjectByAspect(string aspectName, object? value)
    {
        var obj = FindObjectByAspect(aspectName, value);
        if (obj == null) return;
        SelectObject(obj);
        EnsureObjectVisible(obj);
        HighlightObject(obj, null, DefaultHighlightDurationMs, "RevealObjectByAspect");
    }

    public void ToggleColumn(string nameOrAspectName)
    {
        var col = Columns.ByName(nameOrAspectName) ?? Columns.ByAspectName(nameOrAspectName) ?? GetColumn(nameOrAspectName);
        if (col == null) return;
        col.Visible = !col.Visible;
        RebuildColumns();
    }

    public void ShowAllColumns()
    {
        foreach (var col in Columns) col.Visible = true;
        RebuildColumns();
    }

    public void HideAllColumnsExcept(params string[] nameOrAspectNames)
    {
        var keep = new HashSet<string>(nameOrAspectNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        foreach (var col in Columns)
        {
            col.Visible = keep.Contains(col.Name) || keep.Contains(col.AspectName) || keep.Contains(col.Header);
        }
        RebuildColumns();
    }

    public string GetCellText(object? rowObject, string columnNameOrAspectName)
    {
        if (rowObject == null) return string.Empty;
        var col = Columns.ByName(columnNameOrAspectName) ?? Columns.ByAspectName(columnNameOrAspectName) ?? GetColumn(columnNameOrAspectName);
        if (col == null) return Convert.ToString(GetAspectValue(rowObject, columnNameOrAspectName)) ?? string.Empty;
        return Convert.ToString(col.GetValue(rowObject)) ?? string.Empty;
    }

    public object? GetAspectValue(object? rowObject, string aspectName)
    {
        if (rowObject == null || string.IsNullOrWhiteSpace(aspectName)) return null;
        var col = Columns.ByAspectName(aspectName) ?? Columns.ByName(aspectName);
        if (col != null) return col.GetValue(rowObject);

        if (rowObject is IDictionary<string, object?> genericDict && genericDict.TryGetValue(aspectName, out var genericValue)) return genericValue;
        if (rowObject is IDictionary dict && dict.Contains(aspectName)) return dict[aspectName];

        var type = rowObject.GetType();
        var prop = type.GetProperty(aspectName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (prop != null) return prop.GetValue(rowObject);
        var field = type.GetField(aspectName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return field?.GetValue(rowObject);
    }

    private static bool ValuesEqual(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        if (Equals(left, right)) return true;
        return string.Equals(Convert.ToString(left), Convert.ToString(right), StringComparison.OrdinalIgnoreCase);
    }
}
