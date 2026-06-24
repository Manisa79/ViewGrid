using Taylan.Pano.Columns;

namespace Taylan.Pano.Filtering;

public enum PanoFilterMode { Contains, Equals, StartsWith, EndsWith, GreaterThan, LessThan, Between, IsEmpty, IsNotEmpty, ValueList }

public enum PanoFilterLogic
{
    And,
    Or
}

public sealed class PanoColumnFilter
{
    public string AspectName { get; set; } = string.Empty;
    public PanoFilterMode Mode { get; set; } = PanoFilterMode.Contains;
    public string? Text { get; set; }
    public string? Text2 { get; set; }
    public bool Enabled { get; set; } = true;
    public HashSet<string>? SelectedValues { get; set; }

    internal PanoColumnFilter CloneForRead()
    {
        return new PanoColumnFilter
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

public sealed class PanoFilterSet
{
    private readonly object _sync = new();
    private readonly List<PanoColumnFilter> _filters = new();
    private string _globalText = string.Empty;
    private PanoFilterLogic _logic = PanoFilterLogic.And;

    // Önceki sürümlerde doğrudan List dönüyordu. Paralel filtreleme sırasında Set/Clear çalışırsa
    // enumerasyon veya null state bozulabiliyordu. Artık her erişimde güvenli snapshot döner.
    public IReadOnlyList<PanoColumnFilter> Filters
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

    public PanoFilterLogic Logic
    {
        get { lock (_sync) return _logic; }
        set { lock (_sync) _logic = value; }
    }

    public void Set(PanoColumnFilter? filter)
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

    public PanoColumnFilter? Get(string? aspectName)
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

    public bool Passes(object? row, IEnumerable<PanoColumn>? columns)
        => PassesInternal(row, columns == null ? Array.Empty<PanoColumn>() : columns as PanoColumn[] ?? columns.Where(c => c != null).ToArray(), null);

    public bool PassesExcept(object? row, IEnumerable<PanoColumn>? columns, string? ignoredAspectName)
        => PassesInternal(row, columns == null ? Array.Empty<PanoColumn>() : columns as PanoColumn[] ?? columns.Where(c => c != null).ToArray(), ignoredAspectName);

    public bool Passes(object? row, PanoColumn[]? columns)
        => PassesInternal(row, columns ?? Array.Empty<PanoColumn>(), null);

    private bool PassesInternal(object? row, PanoColumn[]? columns, string? ignoredAspectName)
    {
        if (row == null) return false;

        PanoColumn[] safeColumns = columns == null
            ? Array.Empty<PanoColumn>()
            : columns.Where(c => c != null).ToArray();

        // Tek bir snapshot ile hem GlobalText hem kolon filtreleri aynı filtreleme turunda sabit kalır.
        string globalText;
        PanoColumnFilter[] filters;
        lock (_sync)
        {
            globalText = _globalText ?? string.Empty;
            filters = _filters.Select(f => f.CloneForRead()).ToArray();
            var logic = _logic;

            bool hasColumnFilters = filters.Any(f => f != null && f.Enabled && !string.IsNullOrWhiteSpace(f.AspectName));
            if (logic == PanoFilterLogic.Or && hasColumnFilters)
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

            PanoColumn? col = null;
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


    private static bool PassesOr(object row, PanoColumn[] safeColumns, PanoColumnFilter[] filters, string globalText, string? ignoredAspectName)
    {
        bool anyColumnFilterMatched = false;
        bool hasConsideredColumnFilter = false;

        foreach (var f in filters)
        {
            if (f == null || !f.Enabled) continue;
            if (string.IsNullOrWhiteSpace(f.AspectName)) continue;
            if (!string.IsNullOrEmpty(ignoredAspectName) && string.Equals(f.AspectName, ignoredAspectName, StringComparison.OrdinalIgnoreCase))
                continue;

            PanoColumn? col = null;
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

    private static string SafeGetStringValue(PanoColumn column, object row)
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

    private static bool PassesOne(string value, PanoColumnFilter f)
    {
        value ??= string.Empty;
        var t = f.Text ?? string.Empty;
        if (f.Mode == PanoFilterMode.ValueList)
        {
            // null means no value-list restriction. Empty set means the user unchecked every value, so no row should pass.
            var selected = f.SelectedValues;
            return selected == null || selected.Contains(value);
        }
        return f.Mode switch
        {
            PanoFilterMode.Contains => value.IndexOf(t, StringComparison.CurrentCultureIgnoreCase) >= 0,
            PanoFilterMode.Equals => string.Equals(value, t, StringComparison.CurrentCultureIgnoreCase),
            PanoFilterMode.StartsWith => value.StartsWith(t, StringComparison.CurrentCultureIgnoreCase),
            PanoFilterMode.EndsWith => value.EndsWith(t, StringComparison.CurrentCultureIgnoreCase),
            PanoFilterMode.IsEmpty => string.IsNullOrWhiteSpace(value),
            PanoFilterMode.IsNotEmpty => !string.IsNullOrWhiteSpace(value),
            PanoFilterMode.GreaterThan => Compare(value, t) > 0,
            PanoFilterMode.LessThan => Compare(value, t) < 0,
            PanoFilterMode.Between => Compare(value, t) >= 0 && Compare(value, f.Text2 ?? t) <= 0,
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
