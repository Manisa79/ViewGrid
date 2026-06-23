using ViewGrid.Columns;

namespace ViewGrid.Filtering;

public enum ViewGridFilterMode { Contains, Equals, StartsWith, EndsWith, GreaterThan, LessThan, Between, IsEmpty, IsNotEmpty, ValueList }

public enum ViewGridFilterLogic
{
    And,
    Or
}

public sealed class ViewGridColumnFilter
{
    public string AspectName { get; set; } = string.Empty;
    public ViewGridFilterMode Mode { get; set; } = ViewGridFilterMode.Contains;
    public string? Text { get; set; }
    public string? Text2 { get; set; }
    public bool Enabled { get; set; } = true;
    public HashSet<string>? SelectedValues { get; set; }

    internal ViewGridColumnFilter CloneForRead()
    {
        return new ViewGridColumnFilter
        {
            AspectName = AspectName ?? string.Empty,
            Mode = Mode,
            Text = Text,
            Text2 = Text2,
            Enabled = Enabled,
            // ValueList popupları açıkken UI thread SelectedValues üzerinde değişiklik yapabilir.
            // Paralel filtreleme aynı anda okuduğunda HashSet güvenli değildir; bu yüzden okuma snapshot'ı alınır.
            SelectedValues = SelectedValues == null ? null : new HashSet<string>(SelectedValues)
        };
    }
}

public sealed class ViewGridFilterSet
{
    private readonly object _sync = new();
    private readonly List<ViewGridColumnFilter> _filters = new();
    private string _globalText = string.Empty;
    private ViewGridFilterLogic _logic = ViewGridFilterLogic.And;

    // Önceki sürümlerde doğrudan List dönüyordu. Paralel filtreleme sırasında Set/Clear çalışırsa
    // enumerasyon veya null state bozulabiliyordu. Artık her erişimde güvenli snapshot döner.
    public IReadOnlyList<ViewGridColumnFilter> Filters
    {
        get
        {
            lock (_sync)
                return _filters.Select(f => f.CloneForRead()).ToArray();
        }
    }

    public string GlobalText
    {
        get { lock (_sync) return _globalText; }
        set { lock (_sync) _globalText = value ?? string.Empty; }
    }

    public ViewGridFilterLogic Logic
    {
        get { lock (_sync) return _logic; }
        set { lock (_sync) _logic = value; }
    }

    public void Set(ViewGridColumnFilter? filter)
    {
        if (filter == null) return;
        var aspectName = filter.AspectName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(aspectName)) return;

        lock (_sync)
        {
            _filters.RemoveAll(x => string.Equals(x.AspectName, aspectName, StringComparison.OrdinalIgnoreCase));
            if (filter.Enabled)
                _filters.Add(filter.CloneForRead());
        }
    }

    public ViewGridColumnFilter? Get(string? aspectName)
    {
        if (string.IsNullOrWhiteSpace(aspectName)) return null;
        lock (_sync)
            return _filters.FirstOrDefault(x => string.Equals(x.AspectName, aspectName, StringComparison.OrdinalIgnoreCase))?.CloneForRead();
    }

    public void Clear(string? aspectName = null)
    {
        lock (_sync)
        {
            if (aspectName == null)
                _filters.Clear();
            else
                _filters.RemoveAll(x => string.Equals(x.AspectName, aspectName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool Passes(object? row, IEnumerable<ViewGridColumn>? columns)
        => PassesInternal(row, columns == null ? Array.Empty<ViewGridColumn>() : columns as ViewGridColumn[] ?? columns.Where(c => c != null).ToArray(), null);

    public bool PassesExcept(object? row, IEnumerable<ViewGridColumn>? columns, string? ignoredAspectName)
        => PassesInternal(row, columns == null ? Array.Empty<ViewGridColumn>() : columns as ViewGridColumn[] ?? columns.Where(c => c != null).ToArray(), ignoredAspectName);

    public bool Passes(object? row, ViewGridColumn[]? columns)
        => PassesInternal(row, columns ?? Array.Empty<ViewGridColumn>(), null);

    private bool PassesInternal(object? row, ViewGridColumn[]? columns, string? ignoredAspectName)
    {
        if (row == null) return false;

        ViewGridColumn[] safeColumns = columns == null
            ? Array.Empty<ViewGridColumn>()
            : columns.Where(c => c != null).ToArray();

        // Tek bir snapshot ile hem GlobalText hem kolon filtreleri aynı filtreleme turunda sabit kalır.
        string globalText;
        ViewGridColumnFilter[] filters;
        lock (_sync)
        {
            globalText = _globalText ?? string.Empty;
            filters = _filters.Select(f => f.CloneForRead()).ToArray();
            var logic = _logic;

            bool hasColumnFilters = filters.Any(f => f != null && f.Enabled && !string.IsNullOrWhiteSpace(f.AspectName));
            if (logic == ViewGridFilterLogic.Or && hasColumnFilters)
                return PassesOr(row, safeColumns, filters, globalText, ignoredAspectName);
        }

        if (!string.IsNullOrWhiteSpace(globalText))
        {
            bool any = false;
            for (int i = 0; i < safeColumns.Length; i++)
            {
                var col = safeColumns[i];
                if (col == null) continue;

                string value = SafeGetStringValue(col, row);
                if (value.IndexOf(globalText, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    any = true;
                    break;
                }
            }
            if (!any) return false;
        }

        foreach (var f in filters)
        {
            if (f == null || !f.Enabled) continue;
            if (string.IsNullOrWhiteSpace(f.AspectName)) continue;
            if (!string.IsNullOrEmpty(ignoredAspectName) && string.Equals(f.AspectName, ignoredAspectName, StringComparison.OrdinalIgnoreCase))
                continue;

            ViewGridColumn? col = null;
            for (int i = 0; i < safeColumns.Length; i++)
            {
                var candidate = safeColumns[i];
                if (candidate == null) continue;
                if (string.Equals(candidate.AspectName, f.AspectName, StringComparison.OrdinalIgnoreCase))
                {
                    col = candidate;
                    break;
                }
            }

            // Kolon artık yoksa eski filtre uygulamayı kilitlememeli.
            if (col == null) continue;
            if (!PassesOne(SafeGetStringValue(col, row), f)) return false;
        }

        return true;
    }


    private static bool PassesOr(object row, ViewGridColumn[] safeColumns, ViewGridColumnFilter[] filters, string globalText, string? ignoredAspectName)
    {
        bool anyColumnFilterMatched = false;
        bool hasConsideredColumnFilter = false;

        foreach (var f in filters)
        {
            if (f == null || !f.Enabled) continue;
            if (string.IsNullOrWhiteSpace(f.AspectName)) continue;
            if (!string.IsNullOrEmpty(ignoredAspectName) && string.Equals(f.AspectName, ignoredAspectName, StringComparison.OrdinalIgnoreCase))
                continue;

            ViewGridColumn? col = null;
            for (int i = 0; i < safeColumns.Length; i++)
            {
                var candidate = safeColumns[i];
                if (candidate == null) continue;
                if (string.Equals(candidate.AspectName, f.AspectName, StringComparison.OrdinalIgnoreCase))
                {
                    col = candidate;
                    break;
                }
            }

            if (col == null) continue;
            hasConsideredColumnFilter = true;
            if (PassesOne(SafeGetStringValue(col, row), f))
            {
                anyColumnFilterMatched = true;
                break;
            }
        }

        if (!hasConsideredColumnFilter)
            anyColumnFilterMatched = true;

        if (string.IsNullOrWhiteSpace(globalText))
            return anyColumnFilterMatched;

        bool globalMatched = false;
        for (int i = 0; i < safeColumns.Length; i++)
        {
            var col = safeColumns[i];
            if (col == null) continue;

            string value = SafeGetStringValue(col, row);
            if (value.IndexOf(globalText, StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                globalMatched = true;
                break;
            }
        }

        return anyColumnFilterMatched || globalMatched;
    }

    private static string SafeGetStringValue(ViewGridColumn column, object row)
    {
        try
        {
            return column.GetStringValue(row) ?? string.Empty;
        }
        catch
        {
            try { return Convert.ToString(column.GetValue(row)) ?? string.Empty; }
            catch { return string.Empty; }
        }
    }

    private static bool PassesOne(string value, ViewGridColumnFilter f)
    {
        value ??= string.Empty;
        var t = f.Text ?? string.Empty;
        if (f.Mode == ViewGridFilterMode.ValueList)
        {
            // null means no value-list restriction. Empty set means the user unchecked every value, so no row should pass.
            var selected = f.SelectedValues;
            return selected == null || selected.Contains(value);
        }
        return f.Mode switch
        {
            ViewGridFilterMode.Contains => value.IndexOf(t, StringComparison.CurrentCultureIgnoreCase) >= 0,
            ViewGridFilterMode.Equals => string.Equals(value, t, StringComparison.CurrentCultureIgnoreCase),
            ViewGridFilterMode.StartsWith => value.StartsWith(t, StringComparison.CurrentCultureIgnoreCase),
            ViewGridFilterMode.EndsWith => value.EndsWith(t, StringComparison.CurrentCultureIgnoreCase),
            ViewGridFilterMode.IsEmpty => string.IsNullOrWhiteSpace(value),
            ViewGridFilterMode.IsNotEmpty => !string.IsNullOrWhiteSpace(value),
            ViewGridFilterMode.GreaterThan => Compare(value, t) > 0,
            ViewGridFilterMode.LessThan => Compare(value, t) < 0,
            ViewGridFilterMode.Between => Compare(value, t) >= 0 && Compare(value, f.Text2 ?? t) <= 0,
            _ => true
        };
    }

    private static int Compare(string a, string b)
    {
        if (decimal.TryParse(a, out var da) && decimal.TryParse(b, out var db)) return da.CompareTo(db);
        if (DateTime.TryParse(a, out var ta) && DateTime.TryParse(b, out var tb)) return ta.CompareTo(tb);
        return string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
    }
}
