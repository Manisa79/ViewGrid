using System.ComponentModel;
using ViewGrid.Columns;
using ViewGrid.Formatting;
using ViewGrid.Layout;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    private ViewGridPluginCollection? _plugins;
    private readonly List<ViewGridConditionalRule> _conditionalRules = new();
    private readonly System.Windows.Forms.Timer _liveRefreshTimer = new();
    private ViewGridLiveUpdateMode _liveUpdateMode;
    private bool _ultraFastMode;
    private bool _fuzzySearchEnabled = true;
    private bool _columnQualifiedSearchEnabled = true;
    private int _liveRefreshInterval = 1000;

    public event EventHandler<ViewGridCardActionClickEventArgs>? CardActionClick;

    [Category("ViewGrid - Enterprise")]
    [DefaultValue(false)]
    [Description("Reflection, ölçüm ve repaint baskısını azaltmaya yönelik hızlı kullanım profili. Büyük listelerde host uygulama bu flag ile kendi hızlı getter/provider yolunu seçebilir.")]
    public bool UltraFastMode
    {
        get => _ultraFastMode;
        set
        {
            if (_ultraFastMode == value) return;
            _ultraFastMode = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - Smart Search")]
    [DefaultValue(true)]
    [Description("Global aramada küçük yazım hatalarını ve kısaltmaları tolere eden fuzzy eşleşmeye izin verir.")]
    public bool FuzzySearchEnabled
    {
        get => _fuzzySearchEnabled;
        set { _fuzzySearchEnabled = value; Invalidate(); }
    }

    [Category("ViewGrid - Smart Search")]
    [DefaultValue(true)]
    [Description("status:open machine:LINE1 gibi kolon anahtarlı arama tokenlarını destekler.")]
    public bool ColumnQualifiedSearchEnabled
    {
        get => _columnQualifiedSearchEnabled;
        set { _columnQualifiedSearchEnabled = value; Invalidate(); }
    }

    [Category("ViewGrid - Live")]
    [DefaultValue(ViewGridLiveUpdateMode.Off)]
    public ViewGridLiveUpdateMode LiveUpdateMode
    {
        get => _liveUpdateMode;
        set
        {
            if (_liveUpdateMode == value) return;
            _liveUpdateMode = value;
            ConfigureLiveRefreshTimer();
        }
    }

    [Category("ViewGrid - Live")]
    [DefaultValue(1000)]
    public int LiveRefreshInterval
    {
        get => _liveRefreshInterval;
        set
        {
            _liveRefreshInterval = Math.Max(250, value);
            ConfigureLiveRefreshTimer();
        }
    }

    [Category("ViewGrid - Live")]
    [DefaultValue(ViewGridChangedRowAnimation.Flash)]
    public ViewGridChangedRowAnimation ChangedRowAnimation { get; set; } = ViewGridChangedRowAnimation.Flash;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IList<ViewGridConditionalRule> ConditionalRules => _conditionalRules;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ViewGridPluginCollection Plugins => _plugins ??= new ViewGridPluginCollection(this);

    private void ConfigureLiveRefreshTimer()
    {
        _liveRefreshTimer.Stop();
        _liveRefreshTimer.Tick -= LiveRefreshTimerOnTick;
        if (_liveUpdateMode != ViewGridLiveUpdateMode.TimerRefresh || DesignMode || IsDisposed) return;
        _liveRefreshTimer.Interval = _liveRefreshInterval;
        _liveRefreshTimer.Tick += LiveRefreshTimerOnTick;
        _liveRefreshTimer.Start();
    }

    private void LiveRefreshTimerOnTick(object? sender, EventArgs e)
    {
        if (IsDisposed || !IsHandleCreated) return;
        RefreshView();
    }

    public void AddConditionalRule(ViewGridConditionalRule rule)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));
        _conditionalRules.Add(rule);
        _conditionalRules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        ConditionalFormats.Add(new ViewGridConditionalFormat
        {
            Column = rule.Column,
            BackColor = rule.BackColor,
            ForeColor = rule.ForeColor,
            Icon = rule.Icon,
            Predicate = (row, column, value) => rule.Condition(row, column)
        });
        Invalidate();
    }

    public void ClearConditionalRules()
    {
        _conditionalRules.Clear();
        Invalidate();
    }

    public void SaveProfile(string profileName) => SaveLayoutProfile(profileName);

    public bool LoadProfile(string profileName) => LoadLayoutProfile(profileName);

    public bool ResetProfile(string profileName) => DeleteLayoutProfile(profileName);

    public void ApplyLayoutProfile(ViewGridLayoutProfile profile)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        ApplyColumnLayout(profile.State);
        profile.UpdatedAt = DateTime.Now;
        Invalidate();
        QueueAutoSaveUserLayout();
    }

    public ViewGridLayoutProfile CaptureLayoutProfile(string name)
    {
        return new ViewGridLayoutProfile
        {
            Name = name,
            DisplayName = name,
            UserName = Environment.UserName,
            MachineName = Environment.MachineName,
            State = CaptureColumnLayout(),
            UpdatedAt = DateTime.Now
        };
    }

    public IReadOnlyList<ViewGridSmartSearchToken> ParseSmartSearch(string query)
    {
        List<ViewGridSmartSearchToken> result = new();
        if (string.IsNullOrWhiteSpace(query)) return result;

        foreach (string raw in query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string token = raw.Trim();
            bool exclude = token.StartsWith("-", StringComparison.Ordinal);
            if (exclude) token = token.Substring(1);
            string? columnKey = null;
            int colon = token.IndexOf(':');
            if (ColumnQualifiedSearchEnabled && colon > 0 && colon < token.Length - 1)
            {
                columnKey = token.Substring(0, colon);
                token = token.Substring(colon + 1);
            }
            result.Add(new ViewGridSmartSearchToken { ColumnKey = columnKey, Text = token, Exclude = exclude });
        }
        return result;
    }

    public bool SmartSearchMatches(object row, string query)
    {
        IReadOnlyList<ViewGridSmartSearchToken> tokens = ParseSmartSearch(query);
        if (tokens.Count == 0) return true;

        foreach (ViewGridSmartSearchToken token in tokens)
        {
            bool match = SmartSearchTokenMatches(row, token);
            if (token.Exclude && match) return false;
            if (!token.Exclude && !match) return false;
        }
        return true;
    }

    private bool SmartSearchTokenMatches(object row, ViewGridSmartSearchToken token)
    {
        IEnumerable<ViewGridColumn> searchColumns = Columns.VisibleColumns.Where(c => c.Searchable);
        if (!string.IsNullOrWhiteSpace(token.ColumnKey))
        {
            searchColumns = searchColumns.Where(c =>
                string.Equals(c.AspectName, token.ColumnKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Header, token.ColumnKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.SearchAlias, token.ColumnKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Name, token.ColumnKey, StringComparison.OrdinalIgnoreCase));
        }

        foreach (ViewGridColumn column in searchColumns)
        {
            string value = Convert.ToString(column.GetValue(row)) ?? string.Empty;
            if (value.IndexOf(token.Text, StringComparison.CurrentCultureIgnoreCase) >= 0) return true;
            if (FuzzySearchEnabled && IsFuzzyMatch(value, token.Text)) return true;
        }
        return false;
    }

    private static bool IsFuzzyMatch(string value, string query)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(query)) return false;
        value = value.ToUpperInvariant();
        query = query.ToUpperInvariant();
        int qi = 0;
        foreach (char c in value)
        {
            if (qi < query.Length && c == query[qi]) qi++;
            if (qi == query.Length) return true;
        }
        return false;
    }

    public void MarkRowChanged(object row, Color? color = null, string? reason = null)
    {
        if (row == null || ChangedRowAnimation == ViewGridChangedRowAnimation.None) return;
        int index = IndexOfObject(row);
        if (index < 0) return;
        HighlightRow(index, color ?? _theme.AccentColor, 1200, reason);
    }

    public object? CalculateAggregate(ViewGridColumn column)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        List<object> rows = GetObjects().ToList();
        if (column.Aggregate == ViewGridAggregateMode.Custom)
            return column.CustomAggregateGetter?.Invoke(rows, column);
        List<double> nums = rows.Select(r => column.GetValue(r)).Where(v => v != null && double.TryParse(Convert.ToString(v), out _)).Select(v => Convert.ToDouble(v)).ToList();
        return column.Aggregate switch
        {
            ViewGridAggregateMode.Count => rows.Count,
            ViewGridAggregateMode.Sum => nums.Sum(),
            ViewGridAggregateMode.Average => nums.Count == 0 ? 0 : nums.Average(),
            ViewGridAggregateMode.Min => nums.Count == 0 ? 0 : nums.Min(),
            ViewGridAggregateMode.Max => nums.Count == 0 ? 0 : nums.Max(),
            _ => null
        };
    }
}
