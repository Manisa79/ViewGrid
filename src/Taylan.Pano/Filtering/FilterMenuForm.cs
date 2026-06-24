using Taylan.Pano.Columns;
using Taylan.Pano.Theming;
using Taylan.Pano.Localization;

namespace Taylan.Pano.Filtering;

public sealed class FilterMenuForm : Form
{
    private sealed class NaturalStringComparer : IComparer<string>
    {
        public static readonly NaturalStringComparer Instance = new();
        public int Compare(string? x, string? y)
        {
            x ??= string.Empty; y ??= string.Empty;
            if (decimal.TryParse(x, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var dx) &&
                decimal.TryParse(y, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var dy)) return dx.CompareTo(dy);
            int ix = 0, iy = 0;
            while (ix < x.Length && iy < y.Length)
            {
                if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
                {
                    int sx = ix, sy = iy;
                    while (ix < x.Length && char.IsDigit(x[ix])) ix++;
                    while (iy < y.Length && char.IsDigit(y[iy])) iy++;
                    string nx = x.Substring(sx, ix - sx).TrimStart('0');
                    string ny = y.Substring(sy, iy - sy).TrimStart('0');
                    if (nx.Length == 0) nx = "0"; if (ny.Length == 0) ny = "0";
                    int len = nx.Length.CompareTo(ny.Length); if (len != 0) return len;
                    int num = string.CompareOrdinal(nx, ny); if (num != 0) return num;
                    continue;
                }
                int cmp = string.Compare(x, ix, y, iy, 1, StringComparison.CurrentCultureIgnoreCase);
                if (cmp != 0) return cmp;
                ix++; iy++;
            }
            return x.Length.CompareTo(y.Length);
        }
    }
    private readonly PanoColumn _column;
    private readonly PanoTheme _theme;
    private List<ValueItem> _allItems;
    private readonly TextBox _search = new() { PlaceholderText = PanoText.SearchPlaceholder };
    private readonly CheckedListBox _list = new() { CheckOnClick = true, IntegralHeight = false, HorizontalScrollbar = true, BorderStyle = BorderStyle.FixedSingle };
    private readonly ToolTip _valueToolTip = new() { InitialDelay = 350, ReshowDelay = 100, AutoPopDelay = 8000, ShowAlways = true };
    private readonly bool _showValueTooltips;
    private readonly bool _rememberSize;
    private readonly bool _autoWidthForLongValues;
    private readonly Size _maximumPopupSize;
    private readonly string _sizeMemoryKey;
    private int _lastTooltipIndex = -1;
    private static readonly Dictionary<string, Size> RememberedPopupSizes = new(StringComparer.OrdinalIgnoreCase);
    private readonly CheckBox _selectAll = new() { Text = PanoText.SelectAll, AutoSize = true, ThreeState = true, AutoCheck = false };
    private readonly CheckBox _useTextFilter = new() { Text = PanoText.SearchTypedTextInColumn, AutoSize = true };
    private readonly ComboBox _filterMode = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _betweenTo = new() { PlaceholderText = "Bitiş / To" };
    private readonly Button _apply = new() { Text = PanoText.Apply, Height = 28 };
    private readonly Button _selectOnly = new() { Text = "◎  Sadece bunu seç", Height = 26, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _excludeSelected = new() { Text = "⊘  Seçileni hariç tut", Height = 26, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _invertSelection = new() { Text = "⇄  Seçimi tersine çevir", Height = 26, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _top10 = new() { Text = "★  Top 10", Height = 26, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _aboveAverage = new() { Text = "↗  Ortalama üstü", Height = 26, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _clearAll = new() { Text = "✕  " + PanoText.ClearAllFilters, Height = 26 };
    private readonly Button _sortAsc = new() { Text = "↑  " + PanoText.SortAscending, Height = 26, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _sortDesc = new() { Text = "↓  " + PanoText.SortDescending, Height = 26, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _unsort = new() { Text = "↕  " + PanoText.Unsort, Height = 26, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _groupBy = new() { Text = "▦  " + PanoText.GroupByColumn(""), Height = 26, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _clearGrouping = new() { Text = "▢  " + PanoText.ClearGrouping, Height = 26, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Panel _limitedInfoPanel = new() { Height = 44, Visible = false, Padding = new Padding(8, 5, 28, 4) };
    private readonly Label _limitedInfoLabel = new() { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _limitedInfoClose = new() { Text = "×", Width = 24, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat };
    private readonly System.Windows.Forms.Timer _searchDebounce = new() { Interval = 90 };
    private readonly Func<string, IReadOnlyList<string>>? _searchValueLoader;
    private const int MaxVisibleFilterItems = 1500;
    private int _searchGeneration;
    private CancellationTokenSource? _searchCts;
    private bool _updating;
    private bool _userChangedChecks;
    private bool _selectAllRefreshQueued;
    private bool _fullValuesLoaded;
    private bool _searchLoadInProgress;
    private bool _searchWorkerRunning;
    private string? _queuedSearchText;

    public PanoColumnFilter? Result { get; private set; }
    public FilterMenuAction Action { get; private set; } = FilterMenuAction.ApplyFilter;

    public FilterMenuForm(PanoColumn column, IEnumerable<string> distinctValues, PanoColumnFilter? currentFilter = null, PanoTheme? theme = null, Func<string, IReadOnlyList<string>>? searchValueLoader = null, bool isFastPreview = false, int searchDebounceMs = 150, bool resizable = true, Size? defaultSize = null, Size? minimumSize = null, Size? maximumSize = null, bool rememberSize = true, string? sizeMemoryKey = null, bool showValueTooltips = true, bool autoWidthForLongValues = true)
    {
        _column = column;
        _searchValueLoader = searchValueLoader;
        _fullValuesLoaded = searchValueLoader == null && !isFastPreview;
        _allItems = distinctValues
            .Select(v => v ?? string.Empty)
            .GroupBy(v => v, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new ValueItem(g.Key, g.Count()))
            .OrderBy(x => x.Value, NaturalStringComparer.Instance)
            .ToList();

        _theme = theme ?? PanoTheme.LightTheme();
        _showValueTooltips = showValueTooltips;
        _rememberSize = rememberSize;
        _autoWidthForLongValues = autoWidthForLongValues;
        _maximumPopupSize = maximumSize ?? new Size(2000, 1400);
        _sizeMemoryKey = string.IsNullOrWhiteSpace(sizeMemoryKey) ? column.AspectName ?? column.Header ?? "default" : sizeMemoryKey!;
        _list.DrawMode = DrawMode.OwnerDrawFixed;
        _list.DrawItem += DrawFilterListItem;
        _list.MouseMove += (_, e) => ShowValueTooltipIfNeeded(e.Location);
        _list.MouseLeave += (_, _) => { _lastTooltipIndex = -1; _valueToolTip.Hide(_list); };

        StartPosition = FormStartPosition.Manual;
        Size desiredSize = defaultSize ?? new Size(560, 520);
        MinimumSize = GetEffectiveWindowMinimumSize(minimumSize ?? new Size(320, 360));
        MaximumSize = GetEffectiveWindowMaximumSize(_maximumPopupSize, MinimumSize);
        if (_rememberSize && RememberedPopupSizes.TryGetValue(_sizeMemoryKey, out Size rememberedSize))
            desiredSize = rememberedSize;
        Size = ClampSize(GetEffectiveWindowInitialSize(desiredSize, MinimumSize), MinimumSize, MaximumSize);
        FormBorderStyle = resizable ? FormBorderStyle.SizableToolWindow : FormBorderStyle.FixedToolWindow;
        SizeGripStyle = resizable ? SizeGripStyle.Show : SizeGripStyle.Hide;
        MaximizeBox = false;
        MinimizeBox = false;
        PanoDialogChrome.ConfigureStandardDialog(this, _theme, MinimumSize, sizeable: resizable, iconKind: PanoDialogIconKind.Filter);
        Text = PanoText.ColumnFilterTitle(column.Header);
        _groupBy.Text = "▦  " + PanoText.GroupByColumn(column.Header);
        Font = new Font("Segoe UI", 9F);
        KeyPreview = true;
        AcceptButton = _apply;
        CancelButton = null;
        // v25.15: Enter applies the current filter immediately; ESC closes the popup/window.
        // This keeps Excel-like filter usage fast without forcing the user to click Apply.
        KeyDown += (_, e) => HandleFilterShortcutKey(e);
        _search.KeyDown += (_, e) => HandleFilterShortcutKey(e);
        _list.KeyDown += (_, e) => HandleFilterShortcutKey(e);
        _filterMode.KeyDown += (_, e) => HandleFilterShortcutKey(e);
        _betweenTo.KeyDown += (_, e) => HandleFilterShortcutKey(e);
        // v24.84: A 1 ms debounce can start a full provider scan on every keystroke.
        // Keep the UI instant with typed/local candidates, but let provider/index search
        // start after a tiny idle window so the first search does not stutter.
        _searchDebounce.Interval = Math.Max(25, searchDebounceMs);
        ConfigureFilterModes();

        BuildLayout();
        ApplyTheme(_theme);
        RestoreChecks(currentFilter);
        RefillList();
        AdjustInitialSizeForLongValues();

        _searchDebounce.Tick += async (_, _) =>
        {
            _searchDebounce.Stop();
            string searchText = _search.Text.Trim();
            await LoadValuesForSearchAsync(searchText);
            RefillList();
        };

        // v24.77 Smart Filter Live Search: show the typed value immediately while
        // the full provider/index search is still running. This avoids the confusing
        // write-then-delete refresh gap when typing values that are outside the initial preview.
        _search.TextChanged += (_, _) =>
        {
            string searchText = _search.Text.Trim();
            EnsureTypedSearchCandidate(searchText);
            RefillList();

            // v24.78: start typed search almost immediately. The old 40-60 ms floor plus
            // full distinct scanning made values appear only after waiting while delete/backspace
            // felt instant because cached/preview values were already present.
            _searchDebounce.Stop();
            _searchDebounce.Interval = Math.Max(25, _searchDebounce.Interval);
            _searchDebounce.Start();
        };
        // v25.94: Header checkbox is intentionally controlled manually.
        // WinForms ThreeState checkboxes normally cycle Checked -> Indeterminate -> Unchecked.
        // For a filter "Select All" checkbox that feels wrong: when all items are checked,
        // the first user click must immediately uncheck all visible items.
        _selectAll.Click += (_, _) => ToggleSelectAllFromUser();
        _selectAll.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Space) return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            ToggleSelectAllFromUser();
        };
        _list.ItemCheck += (_, e) =>
        {
            if (_updating) return;
            _userChangedChecks = true;
            if (_list.Items[e.Index] is ValueItem item)
                item.Checked = e.NewValue == CheckState.Checked;

            // CheckedListBox raises ItemCheck before its internal check state is committed.
            // Queue the header checkbox refresh so manual mouse/keyboard changes are reflected
            // after Windows Forms has updated the item state.
            QueueSelectAllStateRefresh();
        };
        _list.MouseUp += (_, _) => QueueSelectAllStateRefresh();
        _list.KeyUp += (_, _) => QueueSelectAllStateRefresh();
        _list.Leave += (_, _) => UpdateSelectAllState();
        _apply.Click += (_, _) => ApplyAndClose();
        _clearAll.Click += (_, _) => { Action = FilterMenuAction.ClearAllFilters; Result = null; DialogResult = DialogResult.OK; };
        _selectOnly.Click += (_, _) => SelectOnlyFocusedItem();
        _excludeSelected.Click += (_, _) => ExcludeFocusedItem();
        _invertSelection.Click += (_, _) => InvertVisibleSelection();
        _top10.Click += (_, _) => ApplyTopN(10);
        _aboveAverage.Click += (_, _) => ApplyAboveAverage();
        _sortAsc.Click += (_, _) => { Action = FilterMenuAction.SortAscending; DialogResult = DialogResult.OK; };
        _sortDesc.Click += (_, _) => { Action = FilterMenuAction.SortDescending; DialogResult = DialogResult.OK; };
        _unsort.Click += (_, _) => { Action = FilterMenuAction.Unsort; DialogResult = DialogResult.OK; };
        _groupBy.Click += (_, _) => { Action = FilterMenuAction.GroupByThisColumn; DialogResult = DialogResult.OK; };
        _clearGrouping.Click += (_, _) => { Action = FilterMenuAction.ClearGrouping; DialogResult = DialogResult.OK; };
    }


    private static Size GetEffectiveWindowMinimumSize(Size requested)
    {
        // v27.3.1 FIX: Ayrı filtre penceresinde sol komut alanı sabittir.
        // Minimum genişlik sadece sağdaki liste alanına göre hesaplanırsa arama/liste bölümü
        // sol buton kolonunun altında kalıyor ve pencere menüsü parçalanmış görünüyordu.
        const int leftCommandAreaWidth = 175;
        const int rootHorizontalPadding = 12;
        const int rightUsableMinimumWidth = 300;
        int width = Math.Max(requested.Width, leftCommandAreaWidth + rootHorizontalPadding + rightUsableMinimumWidth + 24);
        int height = Math.Max(requested.Height, 360);
        return new Size(width, height);
    }

    private static Size GetEffectiveWindowMaximumSize(Size requested, Size minimum)
    {
        int width = Math.Max(minimum.Width, requested.Width);
        int height = Math.Max(minimum.Height, requested.Height);
        return new Size(width, height);
    }

    private static Size GetEffectiveWindowInitialSize(Size requested, Size minimum)
    {
        int width = Math.Max(minimum.Width, requested.Width);
        int height = Math.Max(minimum.Height, requested.Height);
        return new Size(width, height);
    }

    private void ConfigureFilterModes()
    {
        _filterMode.Items.Clear();
        _filterMode.Items.AddRange(new object[]
        {
            "Liste seçimi / Multi-select",
            "Contains",
            "StartsWith",
            "Equals",
            "Blanks",
            "Non-Blanks",
            "GreaterThan",
            "LessThan",
            "Between"
        });
        _filterMode.SelectedIndex = 0;
        _filterMode.SelectedIndexChanged += (_, _) =>
        {
            bool isBetween = string.Equals(Convert.ToString(_filterMode.SelectedItem), "Between", StringComparison.OrdinalIgnoreCase);
            _betweenTo.Visible = isBetween;
            _useTextFilter.Checked = _filterMode.SelectedIndex > 0;
        };
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(6) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var left = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 0, 6, 0) };
        foreach (var b in new[] { _sortAsc, _sortDesc, _unsort, _groupBy, _clearGrouping })
        {
            b.Width = 158;
            left.Controls.Add(b);
        }
        var sep = new Label { AutoSize = false, Height = 1, Width = 158, Margin = new Padding(3, 7, 3, 7), BorderStyle = BorderStyle.Fixed3D };
        left.Controls.Add(sep);
        foreach (var b in new[] { _selectOnly, _excludeSelected, _invertSelection, _top10, _aboveAverage })
        {
            b.Width = 158;
            left.Controls.Add(b);
        }
        var sep2 = new Label { AutoSize = false, Height = 1, Width = 158, Margin = new Padding(3, 7, 3, 7), BorderStyle = BorderStyle.Fixed3D };
        left.Controls.Add(sep2);
        _clearAll.Width = 158;
        left.Controls.Add(_clearAll);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 8, ColumnCount = 1 };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        _search.Dock = DockStyle.Fill;
        _filterMode.Dock = DockStyle.Fill;
        _useTextFilter.Dock = DockStyle.Fill;
        _betweenTo.Dock = DockStyle.Fill;
        _betweenTo.Visible = false;
        _selectAll.Dock = DockStyle.Fill;
        _list.Dock = DockStyle.Fill;
        _apply.Dock = DockStyle.Right;
        _apply.Width = 80;
        _limitedInfoLabel.Text = PanoText.FastPreviewLimitedInfo;
        _limitedInfoClose.Margin = Padding.Empty;
        _limitedInfoClose.FlatAppearance.BorderSize = 0;
        _limitedInfoClose.Click += (_, _) => _limitedInfoPanel.Visible = false;
        _limitedInfoPanel.Controls.Add(_limitedInfoLabel);
        _limitedInfoPanel.Controls.Add(_limitedInfoClose);
        right.Controls.Add(_search, 0, 0);
        right.Controls.Add(_filterMode, 0, 1);
        right.Controls.Add(_useTextFilter, 0, 2);
        right.Controls.Add(_selectAll, 0, 3);
        right.Controls.Add(_list, 0, 4);
        right.Controls.Add(_limitedInfoPanel, 0, 5);
        right.Controls.Add(_betweenTo, 0, 6);
        right.Controls.Add(_apply, 0, 7);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);
        Controls.Add(root);
    }

    private void ApplyTheme(PanoTheme theme)
    {
        PanoDialogChrome.ConfigureOpenPanoDialogs(this, theme);

        Color formBack = PanoDialogThemeApplier.NormalizePanelBack(theme);
        Color controlBack = PanoDialogThemeApplier.NormalizeControlBack(theme);
        Color fore = PanoDialogThemeApplier.NormalizeControlFore(theme);
        Color panelFore = PanoDialogThemeApplier.NormalizePanelFore(theme);

        BackColor = formBack;
        ForeColor = panelFore;
        ApplyThemeRecursive(this, theme, formBack, controlBack, panelFore, fore);

        _search.BackColor = controlBack;
        _search.ForeColor = fore;
        _filterMode.BackColor = controlBack;
        _filterMode.ForeColor = fore;
        _betweenTo.BackColor = controlBack;
        _betweenTo.ForeColor = fore;
        _list.BackColor = controlBack;
        _list.ForeColor = fore;

        _limitedInfoPanel.BackColor = PanoDialogThemeApplier.Blend(controlBack, theme.AccentColor, theme.IsDark ? 0.36 : 0.18);
        _limitedInfoLabel.BackColor = _limitedInfoPanel.BackColor;
        _limitedInfoLabel.ForeColor = EnsureFilterTextColor(theme.ForeColor, _limitedInfoPanel.BackColor);
        _limitedInfoClose.BackColor = _limitedInfoPanel.BackColor;
        _limitedInfoClose.ForeColor = _limitedInfoLabel.ForeColor;
        _limitedInfoClose.FlatAppearance.BorderSize = 0;
    }

    private static void ApplyThemeRecursive(Control parent, PanoTheme theme, Color panelBack, Color controlBack, Color panelFore, Color controlFore)
    {
        foreach (Control control in parent.Controls)
        {
            switch (control)
            {
                case TextBox:
                case ComboBox:
                case CheckedListBox:
                case ListBox:
                    control.BackColor = controlBack;
                    control.ForeColor = controlFore;
                    break;
                case Button button:
                    button.BackColor = controlBack;
                    button.ForeColor = controlFore;
                    button.FlatStyle = FlatStyle.Flat;
                    button.UseVisualStyleBackColor = false;
                    button.FlatAppearance.BorderColor = theme.BorderColor == Color.Empty ? ControlPaint.Dark(controlBack) : theme.BorderColor;
                    Color normalBack = button.BackColor;
                    Color hoverBack = PanoDialogThemeApplier.Blend(controlBack, theme.AccentColor, theme.IsDark ? 0.24 : 0.10);
                    button.MouseEnter += (_, __) => button.BackColor = hoverBack;
                    button.MouseLeave += (_, __) => button.BackColor = normalBack;
                    break;
                case CheckBox checkBox:
                    checkBox.BackColor = panelBack;
                    checkBox.ForeColor = panelFore;
                    checkBox.FlatStyle = FlatStyle.Flat;
                    checkBox.UseVisualStyleBackColor = false;
                    break;
                default:
                    control.BackColor = panelBack;
                    control.ForeColor = panelFore;
                    break;
            }

            if (control.HasChildren)
                ApplyThemeRecursive(control, theme, panelBack, controlBack, panelFore, controlFore);
        }
    }

    private static Color EnsureFilterTextColor(Color preferred, Color back)
    {
        if (preferred == Color.Empty || preferred == Color.Transparent)
            return IsDark(back) ? Color.WhiteSmoke : Color.FromArgb(32, 32, 32);

        double contrast = ContrastRatio(preferred, back);
        if (contrast >= 4.5d) return preferred;
        return IsDark(back) ? Color.WhiteSmoke : Color.FromArgb(32, 32, 32);
    }

    private static bool IsDark(Color color)
    {
        if (color == Color.Empty || color == Color.Transparent) return false;
        return (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) < 140;
    }

    private static double ContrastRatio(Color a, Color b)
    {
        static double Channel(double c)
        {
            c /= 255d;
            return c <= 0.03928d ? c / 12.92d : Math.Pow((c + 0.055d) / 1.055d, 2.4d);
        }

        double la = 0.2126d * Channel(a.R) + 0.7152d * Channel(a.G) + 0.0722d * Channel(a.B);
        double lb = 0.2126d * Channel(b.R) + 0.7152d * Channel(b.G) + 0.0722d * Channel(b.B);
        double lighter = Math.Max(la, lb);
        double darker = Math.Min(la, lb);
        return (lighter + 0.05d) / (darker + 0.05d);
    }

    private void DrawFilterListItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _list.Items.Count) return;

        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        Color back = selected
            ? (_theme.SelectionBackColor == Color.Empty ? SystemColors.Highlight : _theme.SelectionBackColor)
            : _list.BackColor;
        Color fore = selected
            ? EnsureFilterTextColor(_theme.SelectionForeColor == Color.Empty ? Color.White : _theme.SelectionForeColor, back)
            : EnsureFilterTextColor(_list.ForeColor, back);

        using (var brush = new SolidBrush(back))
            e.Graphics.FillRectangle(brush, e.Bounds);

        bool isChecked = _list.GetItemChecked(e.Index);
        var checkRect = new Rectangle(e.Bounds.Left + 3, e.Bounds.Top + Math.Max(0, (e.Bounds.Height - 14) / 2), 14, 14);
        CheckBoxRenderer.DrawCheckBox(e.Graphics, checkRect.Location, isChecked ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);

        string text = Convert.ToString(_list.Items[e.Index]) ?? string.Empty;
        var textRect = new Rectangle(checkRect.Right + 5, e.Bounds.Top, Math.Max(0, e.Bounds.Right - checkRect.Right - 8), e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, text, _list.Font, textRect, fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }


    private void AdjustInitialSizeForLongValues()
    {
        // v27.3.1: İlk açılış artık içerikteki en uzun değere göre otomatik büyümez.
        // Popup / pencere en dar kullanılabilir boyutta açılır; kolon genişliği minimumdan büyükse
        // kolon genişliği kadar açılır. Kullanıcı isterse resize grip veya pencere kenarından büyütür.
        // Uzun değerler için yatay scroll ve tooltip aktif kalır.
    }

    private void ShowValueTooltipIfNeeded(Point location)
    {
        if (!_showValueTooltips) return;

        int index = _list.IndexFromPoint(location);
        if (index < 0 || index >= _list.Items.Count)
        {
            if (_lastTooltipIndex != -1)
            {
                _lastTooltipIndex = -1;
                _valueToolTip.Hide(_list);
            }
            return;
        }

        if (_lastTooltipIndex == index) return;
        _lastTooltipIndex = index;
        string text = Convert.ToString(_list.Items[index]) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return;

        int availableWidth = Math.Max(24, _list.ClientSize.Width - 34);
        int measuredWidth = TextRenderer.MeasureText(text, _list.Font).Width;
        if (measuredWidth <= availableWidth)
        {
            _valueToolTip.Hide(_list);
            return;
        }

        _valueToolTip.Show(text, _list, new Point(Math.Min(location.X + 18, _list.ClientSize.Width - 8), location.Y + 18));
    }

    private static Size ClampSize(Size value, Size minimum, Size maximum)
    {
        int width = Math.Max(minimum.Width, Math.Min(maximum.Width, value.Width));
        int height = Math.Max(minimum.Height, Math.Min(maximum.Height, value.Height));
        return new Size(width, height);
    }

    private void RestoreChecks(PanoColumnFilter? currentFilter)
    {
        bool hasValueFilter = currentFilter?.Mode == PanoFilterMode.ValueList && currentFilter.SelectedValues != null;
        foreach (var item in _allItems)
            item.Checked = !hasValueFilter || currentFilter!.SelectedValues!.Contains(item.Value);
    }

    private void EnsureTypedSearchCandidate(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return;
        if (_allItems.Any(x => string.Equals(x.Value, searchText, StringComparison.CurrentCultureIgnoreCase))) return;

        // v24.84: Always show the typed candidate in both modal and floating filter
        // windows. The previous guard required a provider loader, so separate/modal
        // filters could look empty while typing even though Apply would filter correctly.
        var candidate = new ValueItem(searchText, 1, isSyntheticCandidate: true);
        candidate.Checked = !_userChangedChecks;
        _allItems.Insert(0, candidate);
    }

    private void RefillList()
    {
        if (_updating) return;
        _updating = true;
        string q = _search.Text.Trim();

        // Important: when the search text exactly equals a distinct value, show only that exact value.
        // This prevents confusing Excel-menu behavior such as typing "AOI kayıt 9" and still seeing
        // "AOI kayıt 90 / 900 / 9000..." in the selectable list. Partial text still uses contains search.
        var exact = string.IsNullOrWhiteSpace(q)
            ? null
            : _allItems.FirstOrDefault(x => !x.IsSyntheticCandidate && string.Equals(x.Value, q, StringComparison.CurrentCultureIgnoreCase));

        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            int added = 0;
            var source = (exact != null
                ? new[] { exact }
                : _allItems.Where(item => string.IsNullOrWhiteSpace(q) || item.Value.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0)).ToList();

            // v24.81: The modal/separate filter window must behave exactly like the
            // floating filter. While the full async scan is still running, always show
            // the typed value immediately instead of leaving the list empty. This makes
            // typing and deleting symmetric: both paths have a visible selectable row
            // right away, then the real provider/index results replace/extend it.
            if (!string.IsNullOrWhiteSpace(q) && !source.Any(item => string.Equals(item.Value, q, StringComparison.CurrentCultureIgnoreCase)))
            {
                var typed = new ValueItem(q, 1, isSyntheticCandidate: true) { Checked = !_userChangedChecks };
                source.Insert(0, typed);
            }

            foreach (var item in source)
            {
                _list.Items.Add(item, item.Checked);
                added++;
                if (added >= MaxVisibleFilterItems) break;
            }
        }
        finally
        {
            _list.EndUpdate();
            _updating = false;
        }
        _limitedInfoPanel.Visible = !_fullValuesLoaded && string.IsNullOrWhiteSpace(q);
        if (!string.IsNullOrWhiteSpace(q))
            _limitedInfoPanel.Visible = false;
        UpdateSelectAllState();
    }

    private Task LoadValuesForSearchAsync(string searchText)
    {
        if (_searchValueLoader == null || string.IsNullOrWhiteSpace(searchText))
            return Task.CompletedTask;

        // v24.84: Queue/coalesce provider searches. Without this, rapid typing can start
        // several full scans in parallel; deleting then feels instant because cache is warm,
        // while first typing stutters badly. Only the latest requested search is executed.
        _queuedSearchText = searchText.Trim();
        if (_searchWorkerRunning) return Task.CompletedTask;
        _searchWorkerRunning = true;
        _ = ProcessSearchQueueAsync();
        return Task.CompletedTask;
    }

    private async Task ProcessSearchQueueAsync()
    {
        try
        {
            while (!IsDisposed)
            {
                var search = _queuedSearchText;
                _queuedSearchText = null;
                if (string.IsNullOrWhiteSpace(search)) break;

                await LoadValuesCoreAsync(search);

                // If the user typed again while the scan was running, loop once more
                // with the latest value. Intermediate values are intentionally skipped.
                if (string.IsNullOrWhiteSpace(_queuedSearchText)) break;
            }
        }
        finally
        {
            _searchWorkerRunning = false;
            if (!IsDisposed && !string.IsNullOrWhiteSpace(_queuedSearchText))
                _ = LoadValuesForSearchAsync(_queuedSearchText);
        }
    }

    private Task LoadAllValuesAsync()
    {
        if (_searchValueLoader == null || _fullValuesLoaded)
            return Task.CompletedTask;

        return LoadValuesCoreAsync(string.Empty);
    }

    private async Task LoadValuesCoreAsync(string searchText)
    {
        int generation = ++_searchGeneration;
        _searchLoadInProgress = true;

        try
        {
            var loader = _searchValueLoader;
            if (loader == null) return;

            var requestedSearch = searchText?.Trim() ?? string.Empty;

            // v24.82 FULL STABLE:
            // Do not cancel running filter searches by throwing exceptions. Rapid typing can
            // produce many older requests; those requests are allowed to finish quietly and
            // are ignored by the generation/current-search checks below. This keeps both
            // Visual Studio debugging and production runtime crash-safe.
            var values = await Task.Run(() =>
            {
                try
                {
                    return loader(requestedSearch) ?? Array.Empty<string>();
                }
                catch (OperationCanceledException)
                {
                    return Array.Empty<string>();
                }
                catch
                {
                    return Array.Empty<string>();
                }
            });

            if (IsDisposed) return;

            var currentSearch = _search.Text.Trim();
            bool isCurrentSearch = generation == _searchGeneration;
            bool canReuseBroaderResult =
                requestedSearch.Length > 0 &&
                currentSearch.Length >= requestedSearch.Length &&
                currentSearch.StartsWith(requestedSearch, StringComparison.CurrentCultureIgnoreCase);

            if (!isCurrentSearch && !canReuseBroaderResult) return;

            MergeLoadedValues(values);
            EnsureTypedSearchCandidate(currentSearch);
            RefillList();

            if (string.IsNullOrWhiteSpace(requestedSearch) && isCurrentSearch)
                _fullValuesLoaded = true;
        }
        catch (OperationCanceledException)
        {
            // Expected during rapid typing in some providers. Never close the filter UI.
        }
        catch
        {
            // The filter window must never close/crash during search. Keep the current
            // preview/typed candidate and let Apply continue to use the typed filter text.
        }
        finally
        {
            if (generation == _searchGeneration)
                _searchLoadInProgress = false;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_rememberSize && WindowState == FormWindowState.Normal)
            RememberedPopupSizes[_sizeMemoryKey] = ClampSize(Size, MinimumSize, MaximumSize);
        _searchDebounce.Stop();
        try
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
        }
        catch { }
        _searchCts = null;
        base.OnFormClosed(e);
    }

    private void MergeLoadedValues(IEnumerable<string> values)
    {
        var checkedValues = _allItems
            .Where(x => x.Checked)
            .Select(x => x.Value)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        string currentSearch = _search.Text.Trim();
        bool hasUserSelection = _userChangedChecks;

        _allItems = values
            .Select(v => v ?? string.Empty)
            .GroupBy(v => v, StringComparer.CurrentCultureIgnoreCase)
            .Select(g =>
            {
                var item = new ValueItem(g.Key, g.Count());
                item.Checked = !hasUserSelection || checkedValues.Contains(item.Value);
                return item;
            })
            .OrderBy(x => x.Value, NaturalStringComparer.Instance)
            .ToList();

        if (!string.IsNullOrWhiteSpace(currentSearch))
        {
            foreach (var item in _allItems.Where(x => x.Value.IndexOf(currentSearch, StringComparison.CurrentCultureIgnoreCase) >= 0))
                item.Checked = item.Checked || !hasUserSelection;
        }
    }

    private void ToggleSelectAllFromUser()
    {
        if (_updating) return;

        // Checked -> uncheck all.
        // Unchecked/Indeterminate -> check all.
        bool checkVisibleItems = _selectAll.CheckState != CheckState.Checked;
        _userChangedChecks = true;
        SetVisibleItemsChecked(checkVisibleItems);
    }

    private void SetVisibleItemsChecked(bool isChecked)
    {
        if (_updating) return;
        _updating = true;
        try
        {
            for (int i = 0; i < _list.Items.Count; i++)
            {
                if (_list.Items[i] is ValueItem item) item.Checked = isChecked;
                _list.SetItemChecked(i, isChecked);
            }
        }
        finally
        {
            _updating = false;
        }
        UpdateSelectAllState();
    }

    private void QueueSelectAllStateRefresh()
    {
        if (_updating || _selectAllRefreshQueued || IsDisposed) return;
        _selectAllRefreshQueued = true;

        void RefreshNow()
        {
            _selectAllRefreshQueued = false;
            if (!IsDisposed) UpdateSelectAllState();
        }

        if (IsHandleCreated)
            BeginInvoke((Action)RefreshNow);
        else
            RefreshNow();
    }

    private void UpdateSelectAllState()
    {
        if (_updating) return;

        int count = _list.Items.Count;
        int checkedCount = 0;

        for (int i = 0; i < count; i++)
        {
            bool isChecked = _list.GetItemCheckState(i) == CheckState.Checked;
            if (_list.Items[i] is ValueItem item) item.Checked = isChecked;
            if (isChecked) checkedCount++;
        }

        CheckState newState = count == 0 || checkedCount == 0
            ? CheckState.Unchecked
            : checkedCount == count
                ? CheckState.Checked
                : CheckState.Indeterminate;

        if (_selectAll.CheckState == newState) return;

        _updating = true;
        try
        {
            _selectAll.CheckState = newState;
        }
        finally
        {
            _updating = false;
        }
    }

    private void SelectOnlyFocusedItem()
    {
        int index = _list.SelectedIndex;
        if (index < 0 && _list.Items.Count > 0) index = 0;
        if (index < 0) return;
        _userChangedChecks = true;
        SetVisibleItemsChecked(false);
        if (_list.Items[index] is ValueItem item) item.Checked = true;
        _list.SetItemChecked(index, true);
        UpdateSelectAllState();
    }

    private void ExcludeFocusedItem()
    {
        int index = _list.SelectedIndex;
        if (index < 0) return;
        _userChangedChecks = true;
        if (_list.Items[index] is ValueItem item) item.Checked = false;
        _list.SetItemChecked(index, false);
        UpdateSelectAllState();
    }

    private void InvertVisibleSelection()
    {
        if (_updating) return;
        _userChangedChecks = true;
        _updating = true;
        try
        {
            for (int i = 0; i < _list.Items.Count; i++)
            {
                bool next = !_list.GetItemChecked(i);
                if (_list.Items[i] is ValueItem item) item.Checked = next;
                _list.SetItemChecked(i, next);
            }
        }
        finally { _updating = false; }
        UpdateSelectAllState();
    }

    private void ApplyTopN(int count)
    {
        _userChangedChecks = true;
        var top = _allItems.OrderByDescending(x => x.Count).ThenBy(x => x.Value, NaturalStringComparer.Instance).Take(Math.Max(1, count)).Select(x => x.Value).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        foreach (var item in _allItems) item.Checked = top.Contains(item.Value);
        RefillList();
    }

    private void ApplyAboveAverage()
    {
        var numeric = _allItems.Select(x => new { Item = x, Ok = decimal.TryParse(x.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var d), Value = decimal.TryParse(x.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var d2) ? d2 : 0m }).Where(x => x.Ok).ToList();
        if (numeric.Count == 0) return;
        decimal avg = numeric.Average(x => x.Value);
        _userChangedChecks = true;
        var keep = numeric.Where(x => x.Value > avg).Select(x => x.Item.Value).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        foreach (var item in _allItems) item.Checked = keep.Contains(item.Value);
        RefillList();
    }

    private void HandleFilterShortcutKey(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            ApplyAndClose();
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private void ApplyAndClose()
    {
        SyncVisibleItemsFromList();

        string searchText = _search.Text.Trim();
        string modeName = Convert.ToString(_filterMode.SelectedItem) ?? string.Empty;
        var explicitMode = TryCreateExplicitFilter(modeName, searchText);
        if (explicitMode != null)
        {
            Result = explicitMode;
        }
        else if (_useTextFilter.Checked && !string.IsNullOrWhiteSpace(searchText))
        {
            Result = new PanoColumnFilter
            {
                AspectName = _column.AspectName,
                Mode = PanoFilterMode.Contains,
                Text = searchText,
                Enabled = true
            };
        }
        else if (!_userChangedChecks && !string.IsNullOrWhiteSpace(searchText))
        {
            // Excel-like behavior with a safer exact-match shortcut:
            // If the typed text exactly matches a listed value, filter that exact value only.
            // This prevents "AOI kayıt 9" from also returning "AOI kayıt 90/900/...".
            var exact = _allItems.FirstOrDefault(x => !x.IsSyntheticCandidate && string.Equals(x.Value, searchText, StringComparison.CurrentCultureIgnoreCase));
            if (exact != null)
            {
                Result = new PanoColumnFilter
                {
                    AspectName = _column.AspectName,
                    Mode = PanoFilterMode.ValueList,
                    SelectedValues = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase) { exact.Value },
                    Enabled = true
                };
            }
            else
            {
                Result = new PanoColumnFilter
                {
                    AspectName = _column.AspectName,
                    Mode = PanoFilterMode.Contains,
                    Text = searchText,
                    Enabled = true
                };
            }
        }
        else
        {
            // If the value list is narrowed with the search box, apply only visible/checked values.
            // If the search text exactly equals a value, force exact-value filtering even if the typed
            // text is also a prefix/contains match for many other values.
            IEnumerable<ValueItem> sourceForSelection = _allItems;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var exact = _allItems.FirstOrDefault(x => !x.IsSyntheticCandidate && string.Equals(x.Value, searchText, StringComparison.CurrentCultureIgnoreCase));
                sourceForSelection = exact != null
                    ? new[] { exact }
                    : _allItems.Where(x => x.Value.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0);
            }

            var selected = sourceForSelection
                .Where(x => x.Checked)
                .Select(x => x.Value)
                .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

            // Absolute rule: when the search text exactly matches a distinct value,
            // the applied filter must be exact-value only. This matches Excel/Pano user expectation
            // and prevents AOI kayıt 4 from also returning AOI kayıt 40/400/4000.
            var exactAfterSelection = string.IsNullOrWhiteSpace(searchText)
                ? null
                : _allItems.FirstOrDefault(x => !x.IsSyntheticCandidate && string.Equals(x.Value, searchText, StringComparison.CurrentCultureIgnoreCase));
            if (exactAfterSelection != null)
            {
                selected.Clear();
                selected.Add(exactAfterSelection.Value);
            }

            bool allSelected = string.IsNullOrWhiteSpace(searchText) && selected.Count == _allItems.Count;
            Result = new PanoColumnFilter
            {
                AspectName = _column.AspectName,
                Mode = PanoFilterMode.ValueList,
                SelectedValues = selected,
                Enabled = !allSelected
            };
        }

        Action = FilterMenuAction.ApplyFilter;
        DialogResult = DialogResult.OK;
    }

    private PanoColumnFilter? TryCreateExplicitFilter(string modeName, string searchText)
    {
        PanoFilterMode? mode = modeName switch
        {
            "Contains" => PanoFilterMode.Contains,
            "StartsWith" => PanoFilterMode.StartsWith,
            "Equals" => PanoFilterMode.Equals,
            "Blanks" => PanoFilterMode.IsEmpty,
            "Non-Blanks" => PanoFilterMode.IsNotEmpty,
            "GreaterThan" => PanoFilterMode.GreaterThan,
            "LessThan" => PanoFilterMode.LessThan,
            "Between" => PanoFilterMode.Between,
            _ => null
        };
        if (mode == null) return null;
        if (mode.Value == PanoFilterMode.IsEmpty || mode.Value == PanoFilterMode.IsNotEmpty)
        {
            return new PanoColumnFilter { AspectName = _column.AspectName, Mode = mode.Value, Enabled = true };
        }
        if (string.IsNullOrWhiteSpace(searchText)) return null;
        return new PanoColumnFilter
        {
            AspectName = _column.AspectName,
            Mode = mode.Value,
            Text = searchText,
            Text2 = string.Equals(modeName, "Between", StringComparison.OrdinalIgnoreCase) ? _betweenTo.Text.Trim() : null,
            Enabled = true
        };
    }

    private void SyncVisibleItemsFromList()
    {
        for (int i = 0; i < _list.Items.Count; i++)
        {
            if (_list.Items[i] is ValueItem item)
                item.Checked = _list.GetItemChecked(i);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _searchDebounce.Dispose(); _valueToolTip.Dispose(); _searchCts?.Cancel(); _searchCts?.Dispose(); }
        base.Dispose(disposing);
    }

    private sealed class ValueItem
    {
        public ValueItem(string value, int count, bool isSyntheticCandidate = false) { Value = value; Count = count; IsSyntheticCandidate = isSyntheticCandidate; Checked = true; }
        public string Value { get; }
        public int Count { get; }
        public bool IsSyntheticCandidate { get; }
        public bool Checked { get; set; }
        public override string ToString() => string.IsNullOrEmpty(Value) ? $"(Blanks) ({Count} items)" : $"{Value} ({Count} items)";
    }
}

public enum FilterMenuAction
{
    ApplyFilter,
    ClearAllFilters,
    SortAscending,
    SortDescending,
    Unsort,
    GroupByThisColumn,
    ClearGrouping
}

internal static class FilterMenuControlExtensions
{
    public static IEnumerable<Control> Flatten(this Control.ControlCollection controls)
    {
        foreach (Control c in controls)
        {
            yield return c;
            foreach (var child in c.Controls.Flatten()) yield return child;
        }
    }
}
