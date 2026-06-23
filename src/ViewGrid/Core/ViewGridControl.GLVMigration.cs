using System.Collections;
using System.ComponentModel;
using ViewGrid.Columns;

namespace ViewGrid.Core;

/// <summary>ViewGrid hücre düzenleme tetikleme modu.</summary>
public enum GLVCellEditActivateMode
{
    None,
    SingleClick,
    DoubleClick,
    F2Only,
    SingleClickAlways,
    SingleClickWhenSelected
}

/// <summary>ViewGrid model filtresi kontratı.</summary>
public interface IGLVModelFilter
{
    bool Filter(object modelObject);
}

/// <summary>Delegate tabanlı ViewGrid model filtresi.</summary>
public sealed class GLVModelFilter : IGLVModelFilter
{
    private readonly Func<object, bool> _predicate;
    public GLVModelFilter(Func<object, bool> predicate) => _predicate = predicate ?? (_ => true);
    public bool Filter(object modelObject) => _predicate(modelObject);
}

public partial class ViewGridControl
{
    private object? _additionalFilterObject;
    private Func<object, bool>? _additionalFilterFunc;
    private ViewGridColumn? _secondarySortColumn;
    private bool _secondarySortDescending;
    private bool _sortGroupItemsByPrimaryColumn = true;
    private string _checkedAspectName = string.Empty;
    private bool _checkBoxes;
    private GLVCellEditActivateMode _cellEditActivation = GLVCellEditActivateMode.F2Only;

    /// <summary>ViewGrid UseFiltering özelliği. Filtre menüsünü ve mevcut filtrelerin uygulanmasını kontrol eder.</summary>
    [Category("ViewGrid - Compatibility")]
    [DefaultValue(true)]
    public bool UseFiltering
    {
        get => ShowFilterMenu;
        set
        {
            ShowFilterMenu = value;
            if (!value) ClearFilters();
            Invalidate();
        }
    }

    /// <summary>ViewGrid CheckBoxes özelliği. CheckedAspectName verilirse checkbox kolonu otomatik hazırlanır.</summary>
    [Category("ViewGrid - Compatibility")]
    [DefaultValue(false)]
    public bool CheckBoxes
    {
        get => _checkBoxes;
        set
        {
            _checkBoxes = value;
            EnsureCompatibilityCheckBoxColumn();
            Invalidate();
        }
    }

    /// <summary>ViewGrid CheckedAspectName özelliği. Checkbox değerinin okunacağı/yazılacağı model property adıdır.</summary>
    [Category("ViewGrid - Compatibility")]
    [DefaultValue("")]
    public string CheckedAspectName
    {
        get => _checkedAspectName;
        set
        {
            _checkedAspectName = value ?? string.Empty;
            EnsureCompatibilityCheckBoxColumn();
            Invalidate();
        }
    }

    /// <summary>ViewGrid CellEditActivation özelliği.</summary>
    [Category("ViewGrid - Compatibility")]
    [DefaultValue(GLVCellEditActivateMode.F2Only)]
    public GLVCellEditActivateMode CellEditActivation
    {
        get => _cellEditActivation;
        set
        {
            _cellEditActivation = value;
            EnableCellEditing = value != GLVCellEditActivateMode.None;
            CellEditActivationOnDoubleClick = value == GLVCellEditActivateMode.DoubleClick;
            CellEditActivationKey = value == GLVCellEditActivateMode.F2Only || value == GLVCellEditActivateMode.DoubleClick ? Keys.F2 : Keys.None;
        }
    }

    /// <summary>ViewGrid AdditionalFilter özelliği. Func, Predicate ve IGLVModelFilter destekler.</summary>
    [Browsable(false)]
    public object? AdditionalFilter
    {
        get => _additionalFilterObject;
        set
        {
            _additionalFilterObject = value;
            _additionalFilterFunc = ConvertToFilterFunc(value);
            BuildViewIndex();
        }
    }

    /// <summary>ViewGrid tarafında güçlü tipli AdditionalFilter.</summary>
    [Browsable(false)]
    public Func<object, bool>? GLVAdditionalFilter
    {
        get => _additionalFilterFunc;
        set
        {
            _additionalFilterObject = value;
            _additionalFilterFunc = value;
            BuildViewIndex();
        }
    }

    [Browsable(false)]
    public ViewGridColumn? PrimarySortColumn
    {
        get => _sortColumn;
        set { _sortColumn = value; BuildViewIndex(); }
    }

    [Browsable(false)]
    public bool PrimarySortDescending
    {
        get => _sortDesc;
        set { _sortDesc = value; BuildViewIndex(); }
    }

    [Browsable(false)]
    public ViewGridColumn? SecondarySortColumn
    {
        get => _secondarySortColumn;
        set { _secondarySortColumn = value; BuildViewIndex(); }
    }

    [Browsable(false)]
    public bool SecondarySortDescending
    {
        get => _secondarySortDescending;
        set { _secondarySortDescending = value; BuildViewIndex(); }
    }

    [Category("ViewGrid - Compatibility")]
    [DefaultValue(true)]
    public bool SortGroupItemsByPrimaryColumn
    {
        get => _sortGroupItemsByPrimaryColumn;
        set { _sortGroupItemsByPrimaryColumn = value; BuildViewIndex(); }
    }

    /// <summary>ViewGrid ShowGroups özelliği. GroupByAspectName ile birlikte çalışır.</summary>
    [Category("ViewGrid - Compatibility")]
    [DefaultValue(false)]
    public bool ShowGroups
    {
        get => EnableGrouping;
        set { EnableGrouping = value; BuildViewIndex(); }
    }

    /// <summary>ViewGrid EmptyListMsg özelliği.</summary>
    [Category("ViewGrid - Compatibility")]
    public string EmptyListMsg
    {
        get => EmptyListMessage;
        set { EmptyListMessage = value ?? string.Empty; Invalidate(); }
    }

    public void Freeze() => BeginUpdate();
    public void Unfreeze() => EndUpdate();

    public void Sort(ViewGridColumn column, SortOrder order = SortOrder.Ascending)
    {
        if (column == null) return;
        _sortColumn = column;
        _sortDesc = order == SortOrder.Descending;
        BuildViewIndex();
    }

    public void Sort(GLVColumn column, SortOrder order = SortOrder.Ascending) => Sort((ViewGridColumn)column, order);

    internal bool SafeAdditionalFilterPasses(object row)
    {
        try { return _additionalFilterFunc == null || _additionalFilterFunc(row); }
        catch { return false; }
    }

    private static Func<object, bool>? ConvertToFilterFunc(object? filter)
    {
        if (filter == null) return null;
        if (filter is Func<object, bool> func) return func;
        if (filter is Predicate<object> pred) return pred.Invoke;
        if (filter is IGLVModelFilter glvFilter) return glvFilter.Filter;
        return null;
    }

    private const string InternalCheckBoxSelectorTag = "__ViewGridInternalCheckBoxSelector";

    private static bool IsCompatibilityCheckBoxSelector(ViewGridColumn column)
    {
        if (column == null || column.Kind != ViewGridColumnKind.CheckBox) return false;
        if (Equals(column.Tag, InternalCheckBoxSelectorTag)) return true;
        return string.IsNullOrWhiteSpace(column.AspectName)
            && string.IsNullOrWhiteSpace(column.Header);
    }

    private ViewGridColumn? GetCompatibilityCheckBoxHostColumn()
    {
        if (!_checkBoxes) return null;

        // ViewGrid/ListView uyumu: liste seviyesindeki CheckBoxes = true davranışı
        // yeni veya ayrı bir checkbox kolonu üretmez. Checkbox, ilk gerçek görünür
        // veri kolonunun içinde çizilir. Önceki sürümlerden kalmış boş selector
        // kolonları veya gerçek veri taşımayan checkbox kolonları host olarak seçilmez.
        return Columns.VisibleColumns.FirstOrDefault(c => !IsCompatibilityCheckBoxSelector(c));
    }

    private bool IsCompatibilityCheckBoxHostColumn(ViewGridColumn? column)
    {
        return _checkBoxes && column != null && ReferenceEquals(column, GetCompatibilityCheckBoxHostColumn());
    }

    private void RemoveInternalCompatibilityCheckBoxColumns()
    {
        var internalColumns = Columns.Where(IsCompatibilityCheckBoxSelector).ToList();
        if (internalColumns.Count == 0) return;
        foreach (var column in internalColumns)
            Columns.Remove(column);
        RebuildColumns();
    }

    private void EnsureCompatibilityCheckBoxColumn()
    {
        NormalizeCompatibilityCheckBoxColumns();
    }

    private void NormalizeCompatibilityCheckBoxColumns()
    {
        RemoveInternalCompatibilityCheckBoxColumns();
        if (!_checkBoxes) return;

        // ViewGrid uyumlu davranış:
        // CheckBoxes = true kesinlikle yeni/ayrı bir kolon üretmez. Checkbox
        // ilk gerçek görünür veri kolonunun sol iç alanına çizilir.
        // Eski ViewGrid sürümlerinden/designer serileştirmesinden kalmış boş
        // veya yalnızca selector amaçlı CheckBox kolonları runtime'da tekrar
        // görünmemeli.
        var staleSelectorColumns = Columns
            .Where(c => c.Kind == ViewGridColumnKind.CheckBox
                        && (Equals(c.Tag, InternalCheckBoxSelectorTag)
                            || string.IsNullOrWhiteSpace(c.Header)
                            || string.IsNullOrWhiteSpace(c.AspectName)
                            || (!string.IsNullOrWhiteSpace(_checkedAspectName)
                                && string.Equals(c.AspectName, _checkedAspectName, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        foreach (var stale in staleSelectorColumns)
        {
            // Kullanıcının veri kolonu değilse tamamen kaldır. Kullanıcı özellikle
            // CheckedAspectName adında bir kolon oluşturmuşsa veri kaybı olmaması
            // için sadece gizle.
            if (string.IsNullOrWhiteSpace(stale.Header) || string.IsNullOrWhiteSpace(stale.AspectName) || Equals(stale.Tag, InternalCheckBoxSelectorTag))
                Columns.Remove(stale);
            else
                stale.Visible = false;
        }

        var host = GetCompatibilityCheckBoxHostColumn();
        if (host == null) return;

        // Host kolon veri kolonu olarak kalır; checkbox sadece iç çizimdir.
        if (host.Kind == ViewGridColumnKind.CheckBox)
        {
            host.Kind = ViewGridColumnKind.Text;
            host.Sortable = true;
            host.Filterable = true;
        }

        // Header checkbox is now optional. CheckBoxes = true only enables row checkboxes
        // inside the first visible host column. The header checkbox appears only when
        // the host column's HeaderCheckBox property is explicitly true. This keeps
        // OLV/ListView compatibility while allowing projects to hide the select-all box.
        host.HeaderCheckBoxUpdatesRowCheckBoxes = true;

        if (host.HeaderTextAlign == ContentAlignment.MiddleCenter)
            host.HeaderTextAlign = ContentAlignment.MiddleLeft;
        if (host.TextAlign == ContentAlignment.MiddleCenter)
            host.TextAlign = ContentAlignment.MiddleLeft;

        // ViewGrid/ListView hissi: checkbox ve metin aynı kolon içinde rahat görünmeli.
        if (host.Width < 120)
            host.Width = 160;
        if (host.DefaultWidth < 120)
            host.DefaultWidth = Math.Max(host.DefaultWidth, 160);
    }

    private object? GetCompatibilityCheckAspectValue(object row)
    {
        if (row == null || string.IsNullOrWhiteSpace(_checkedAspectName)) return null;
        if (row is IDictionary<string, object?> genericDict && genericDict.TryGetValue(_checkedAspectName, out var genericValue)) return genericValue;
        if (row is IDictionary dict && dict.Contains(_checkedAspectName)) return dict[_checkedAspectName];
        try
        {
            var prop = row.GetType().GetProperty(_checkedAspectName);
            if (prop != null) return prop.GetValue(row);
            var field = row.GetType().GetField(_checkedAspectName);
            if (field != null) return field.GetValue(row);
        }
        catch { }
        return null;
    }

    private void SetCompatibilityCheckAspectValue(object row, bool value)
    {
        if (row == null || string.IsNullOrWhiteSpace(_checkedAspectName)) return;
        if (row is IDictionary<string, object?> genericDict) { genericDict[_checkedAspectName] = value; return; }
        if (row is IDictionary dict) { dict[_checkedAspectName] = value; return; }
        try
        {
            var prop = row.GetType().GetProperty(_checkedAspectName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(row, ConvertToCheckCompatibleValue(value, prop.PropertyType));
                return;
            }
            var field = row.GetType().GetField(_checkedAspectName);
            if (field != null)
                field.SetValue(row, ConvertToCheckCompatibleValue(value, field.FieldType));
        }
        catch { }
    }

    private static object ConvertToCheckCompatibleValue(bool value, Type targetType)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (type == typeof(bool)) return value;
        if (type == typeof(CheckState)) return value ? CheckState.Checked : CheckState.Unchecked;
        if (type == typeof(int)) return value ? 1 : 0;
        if (type == typeof(byte)) return (byte)(value ? 1 : 0);
        if (type == typeof(string)) return value ? "True" : "False";
        return value;
    }
}
