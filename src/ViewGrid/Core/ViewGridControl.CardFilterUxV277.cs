using System.ComponentModel;
using ViewGrid.Columns;
using ViewGrid.Filtering;

namespace ViewGrid.Core;

public enum ViewGridCardFilterUxPlacement
{
    TopBar,
    FloatingButton,
    TopBarAndFloatingButton
}

public partial class ViewGridControl
{
    private Panel? _cardFilterBar;
    private TextBox? _cardFilterSearchBox;
    private ComboBox? _cardFilterColumnBox;
    private Button? _cardFilterOpenButton;
    private Button? _cardFilterClearButton;
    private FlowLayoutPanel? _cardFilterChipPanel;
    private Button? _cardFilterFloatingButton;
    private bool _updatingCardFilterUx;

    [Category("ViewGrid - Card Filter UX v27.7")]
    [DefaultValue(true)]
    [Description("Kart, geniş kart, dashboard, kanban, poster gibi header olmayan büyük görünümlerde hızlı filtre barını gösterir.")]
    public bool ShowQuickFilterBar { get; set; } = true;

    [Category("ViewGrid - Card Filter UX v27.7")]
    [DefaultValue(false)]
    [Description("Kart görünümlerinde sağ üstte filtre penceresini açan floating buton gösterir. v28 ile varsayılan kapalıdır; global filtre butonu üst bara taşındı.")]
    public bool ShowFloatingFilterButton { get; set; } = false;

    [Category("ViewGrid - Card Filter UX v27.7")]
    [DefaultValue(true)]
    [Description("Aktif global/kolon filtrelerini kart görünümü üstünde chip olarak gösterir.")]
    public bool ShowActiveFilterChips { get; set; } = true;

    [Category("ViewGrid - Card Filter UX v27.7")]
    [DefaultValue(false)]
    [Description("Preset bar için ayrılmış ayar. v27.7'de hızlı filtre barı içinde kullanılacak şekilde hazırlandı.")]
    public bool ShowFilterPresetsBar { get; set; } = false;

    [Category("ViewGrid - Card Filter UX v27.7")]
    [DefaultValue(true)]
    [Description("True ise hızlı filtre UI sadece kart/poster/dashboard/kanban gibi büyük görünümlerde görünür. False ise tüm görünümlerde kullanılabilir.")]
    public bool CardFilterUxOnlyInCardViews { get; set; } = true;

    [Category("ViewGrid - Card Filter UX v27.7")]
    [DefaultValue(ViewGridCardFilterUxPlacement.TopBar)]
    public ViewGridCardFilterUxPlacement CardFilterUxPlacement { get; set; } = ViewGridCardFilterUxPlacement.TopBar;


    [Category("ViewGrid - UX Polish v28")]
    [DefaultValue(false)]
    [Description("Eski davranış için kart alanında ayrı floating filtre butonunu açık tutar. Varsayılan false: filtre global üst barda durur.")]
    public bool ShowCardInlineFilterButton
    {
        get => ShowFloatingFilterButton;
        set => ShowFloatingFilterButton = value;
    }

    [Category("ViewGrid - UX Polish v28")]
    [DefaultValue(true)]
    [Description("Kart/poster/dashboard görünümlerinde filtre açma aksiyonunu üst hızlı filtre barında tutar.")]
    public bool MoveFilterButtonToTopBar { get; set; } = true;

    [Category("ViewGrid - Card Filter UX v27.7")]
    [DefaultValue(74)]
    public int CardFilterBarHeight { get; set; } = 74;

    [Category("ViewGrid - Card Filter UX v27.7")]
    [DefaultValue("Kartlarda ara...")]
    public string QuickFilterPlaceholderText { get; set; } = "Kartlarda ara...";

    [Category("ViewGrid - Card Filter UX v27.7")]
    [DefaultValue("Filtre")]
    public string FloatingFilterButtonText { get; set; } = "Filtre";

    private void InitializeCardViewFilterUx()
    {
        if (_cardFilterBar != null) return;

        _cardFilterBar = new Panel
        {
            Height = CardFilterBarHeight,
            Visible = false,
            Padding = new Padding(10, 8, 10, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _cardFilterSearchBox = new TextBox
        {
            Width = 260,
            PlaceholderText = QuickFilterPlaceholderText,
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Left | AnchorStyles.Top
        };
        _cardFilterSearchBox.TextChanged += (_, __) =>
        {
            if (_updatingCardFilterUx) return;
            SetGlobalFilter(_cardFilterSearchBox.Text ?? string.Empty);
        };

        _cardFilterColumnBox = new ComboBox
        {
            Width = 210,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            DisplayMember = nameof(ViewGridColumn.Header)
        };

        _cardFilterOpenButton = new Button
        {
            Text = "Kolon filtresi",
            Width = 118,
            Height = 27,
            Anchor = AnchorStyles.Left | AnchorStyles.Top
        };
        _cardFilterOpenButton.Click += (_, __) => OpenSelectedCardFilterColumn();

        _cardFilterClearButton = new Button
        {
            Text = "Temizle",
            Width = 82,
            Height = 27,
            Anchor = AnchorStyles.Left | AnchorStyles.Top
        };
        _cardFilterClearButton.Click += (_, __) => ClearFilters();

        _cardFilterChipPanel = new FlowLayoutPanel
        {
            Height = 30,
            WrapContents = false,
            AutoScroll = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };

        _cardFilterBar.Controls.Add(_cardFilterSearchBox);
        _cardFilterBar.Controls.Add(_cardFilterColumnBox);
        _cardFilterBar.Controls.Add(_cardFilterOpenButton);
        _cardFilterBar.Controls.Add(_cardFilterClearButton);
        _cardFilterBar.Controls.Add(_cardFilterChipPanel);
        Controls.Add(_cardFilterBar);
        _cardFilterBar.BringToFront();

        _cardFilterFloatingButton = new Button
        {
            Text = FloatingFilterButtonText,
            Width = 88,
            Height = 32,
            Visible = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _cardFilterFloatingButton.Click += (_, __) => OpenBestCardFilterColumn();
        Controls.Add(_cardFilterFloatingButton);
        _cardFilterFloatingButton.BringToFront();

        UpdateCardViewFilterUxLayout();
    }

    private void UpdateCardViewFilterUxLayout()
    {
        if (_cardFilterBar == null) return;

        bool allowedByMode = !CardFilterUxOnlyInCardViews || IsCardLikeViewMode(ViewMode) || !ShowHeader;
        bool showBar = allowedByMode && ShowQuickFilterBar && CardFilterUxPlacement != ViewGridCardFilterUxPlacement.FloatingButton;
        bool showFloating = allowedByMode && ShowFloatingFilterButton && !MoveFilterButtonToTopBar && CardFilterUxPlacement != ViewGridCardFilterUxPlacement.TopBar;

        int barHeight = Math.Max(42, CardFilterBarHeight);
        _cardFilterBar.SetBounds(0, 0, Math.Max(0, ClientSize.Width - VBarWidth), barHeight);
        _cardFilterBar.Visible = showBar;

        if (_cardFilterFloatingButton != null)
        {
            _cardFilterFloatingButton.Text = string.IsNullOrWhiteSpace(FloatingFilterButtonText) ? "Filtre" : FloatingFilterButtonText;
            _cardFilterFloatingButton.Visible = showFloating;
            _cardFilterFloatingButton.SetBounds(Math.Max(8, ClientSize.Width - VBarWidth - _cardFilterFloatingButton.Width - 14), showBar ? barHeight + 8 : 10, _cardFilterFloatingButton.Width, _cardFilterFloatingButton.Height);
            _cardFilterFloatingButton.BringToFront();
        }

        if (!showBar)
            return;

        ApplyCardViewFilterUxTheme();
        SyncCardFilterColumnList();
        SyncCardFilterTextFromState();
        BuildActiveFilterChips();

        int y = 9;
        int x = 10;
        int clientW = Math.Max(100, _cardFilterBar.ClientSize.Width - 20);
        int searchW = Math.Min(Math.Max(210, clientW / 4), 360);
        int comboW = Math.Min(Math.Max(170, clientW / 5), 280);

        if (_cardFilterSearchBox != null)
        {
            _cardFilterSearchBox.PlaceholderText = QuickFilterPlaceholderText;
            _cardFilterSearchBox.SetBounds(x, y, searchW, 27);
            x += searchW + 8;
        }
        if (_cardFilterColumnBox != null)
        {
            _cardFilterColumnBox.SetBounds(x, y, comboW, 27);
            x += comboW + 8;
        }
        if (_cardFilterOpenButton != null)
        {
            _cardFilterOpenButton.SetBounds(x, y, 118, 27);
            x += 126;
        }
        if (_cardFilterClearButton != null)
            _cardFilterClearButton.SetBounds(x, y, 82, 27);

        if (_cardFilterChipPanel != null)
        {
            _cardFilterChipPanel.Visible = ShowActiveFilterChips;
            _cardFilterChipPanel.SetBounds(10, 40, Math.Max(10, clientW), Math.Max(24, barHeight - 44));
        }

        _cardFilterBar.BringToFront();
    }

    private void ApplyCardViewFilterUxTheme()
    {
        if (_cardFilterBar == null) return;
        var theme = global::ViewGrid.Theming.ViewGridThemeAccessibility.Normalize(_theme);
        Color surface = theme.PanelBackColor;
        Color barBack = global::ViewGrid.Theming.ViewGridThemeAccessibility.EnsureSurfaceContrast(Blend(surface, theme.BackColor, theme.IsDark ? 0.10 : 0.03), theme.BackColor, theme.IsDark);
        Color fore = global::ViewGrid.Theming.ViewGridThemeAccessibility.EnsureReadableTextColor(theme.ForeColor, barBack, 4.5d);
        Color controlBack = global::ViewGrid.Theming.ViewGridThemeAccessibility.EnsureSurfaceContrast(theme.TextBackColor, barBack, theme.IsDark);
        Color controlFore = global::ViewGrid.Theming.ViewGridThemeAccessibility.EnsureReadableTextColor(theme.ForeColor, controlBack, 4.5d);
        Color buttonBack = global::ViewGrid.Theming.ViewGridThemeAccessibility.ButtonBack(theme, primary: false);
        Color buttonFore = global::ViewGrid.Theming.ViewGridThemeAccessibility.EnsureReadableTextColor(theme.ForeColor, buttonBack, 4.5d);
        Color buttonBorder = global::ViewGrid.Theming.ViewGridThemeAccessibility.ButtonBorder(theme, primary: false);

        _cardFilterBar.BackColor = barBack;
        _cardFilterBar.ForeColor = fore;

        foreach (Control c in _cardFilterBar.Controls)
        {
            if (c is TextBox or ComboBox or ListControl)
            {
                c.BackColor = controlBack;
                c.ForeColor = controlFore;
            }
            else if (c is Button button)
            {
                button.BackColor = buttonBack;
                button.ForeColor = buttonFore;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = buttonBorder;
                button.FlatAppearance.MouseOverBackColor = Blend(buttonBack, theme.AccentColor, theme.IsDark ? 0.18 : 0.12);
                button.FlatAppearance.MouseDownBackColor = Blend(buttonBack, theme.BackColor, theme.IsDark ? 0.18 : 0.08);
            }
            else
            {
                c.BackColor = barBack;
                c.ForeColor = fore;
            }
        }

        if (_cardFilterFloatingButton != null)
        {
            _cardFilterFloatingButton.BackColor = theme.AccentColor;
            _cardFilterFloatingButton.ForeColor = global::ViewGrid.Theming.ViewGridThemeAccessibility.EnsureReadableTextColor(Color.White, theme.AccentColor, 4.5d);
            _cardFilterFloatingButton.FlatStyle = FlatStyle.Flat;
            _cardFilterFloatingButton.FlatAppearance.BorderColor = global::ViewGrid.Theming.ViewGridThemeAccessibility.ButtonBorder(theme, primary: true);
        }
    }

    private void SyncCardFilterColumnList()
    {
        if (_cardFilterColumnBox == null) return;
        var current = _cardFilterColumnBox.SelectedItem as ViewGridColumn;
        var visibleFilterable = Columns.VisibleColumns.Where(c => c.Filterable).ToList();
        if (_cardFilterColumnBox.Items.Count == visibleFilterable.Count && visibleFilterable.SequenceEqual(_cardFilterColumnBox.Items.Cast<ViewGridColumn>()))
            return;

        _cardFilterColumnBox.BeginUpdate();
        _cardFilterColumnBox.Items.Clear();
        foreach (var c in visibleFilterable)
            _cardFilterColumnBox.Items.Add(c);
        if (current != null && visibleFilterable.Contains(current)) _cardFilterColumnBox.SelectedItem = current;
        else if (_cardFilterColumnBox.Items.Count > 0) _cardFilterColumnBox.SelectedIndex = 0;
        _cardFilterColumnBox.EndUpdate();
    }

    private void SyncCardFilterTextFromState()
    {
        if (_cardFilterSearchBox == null) return;
        if (string.Equals(_cardFilterSearchBox.Text, _filters.GlobalText ?? string.Empty, StringComparison.Ordinal)) return;
        _updatingCardFilterUx = true;
        try { _cardFilterSearchBox.Text = _filters.GlobalText ?? string.Empty; }
        finally { _updatingCardFilterUx = false; }
    }

    private void BuildActiveFilterChips()
    {
        if (_cardFilterChipPanel == null) return;
        _cardFilterChipPanel.Controls.Clear();
        if (!ShowActiveFilterChips) return;

        if (!string.IsNullOrWhiteSpace(_filters.GlobalText))
            AddFilterChip("Ara: " + _filters.GlobalText, () => SetGlobalFilter(string.Empty));

        foreach (var f in _filters.Filters.Where(x => x.Enabled).ToList())
        {
            string header = Columns.VisibleColumns.FirstOrDefault(c => string.Equals(c.AspectName, f.AspectName, StringComparison.OrdinalIgnoreCase))?.Header ?? f.AspectName;
            string value = f.Mode == ViewGridFilterMode.ValueList && f.SelectedValues != null
                ? string.Join(", ", f.SelectedValues.Take(2)) + (f.SelectedValues.Count > 2 ? "..." : string.Empty)
                : f.Text ?? f.Mode.ToString();
            AddFilterChip(header + ": " + value, () =>
            {
                _filters.Clear(f.AspectName);
                BuildViewIndex();
                QueueAutoSaveUserLayout();
                UpdateCardViewFilterUxLayout();
            });
        }

        if (_cardFilterChipPanel.Controls.Count == 0)
        {
            var empty = new Label
            {
                AutoSize = true,
                Text = "Aktif filtre yok",
                ForeColor = global::ViewGrid.Theming.ViewGridThemeAccessibility.EnsureReadableTextColor(
                    global::ViewGrid.Theming.ViewGridThemeAccessibility.MutedText(_cardFilterChipPanel.BackColor == Color.Empty ? _theme.PanelBackColor : _cardFilterChipPanel.BackColor, _theme.ForeColor),
                    _cardFilterChipPanel.BackColor == Color.Empty ? _theme.PanelBackColor : _cardFilterChipPanel.BackColor,
                    3.8d),
                Padding = new Padding(2, 5, 2, 0)
            };
            _cardFilterChipPanel.Controls.Add(empty);
        }
    }

    private void AddFilterChip(string text, Action removeAction)
    {
        if (_cardFilterChipPanel == null) return;
        var btn = new Button
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Text = text + "  ×",
            Height = 25,
            Margin = new Padding(0, 1, 6, 1),
            Padding = new Padding(8, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Blend(_theme.AccentColor, _theme.PanelBackColor, _theme.IsDark ? 0.70 : 0.82),
            ForeColor = EnsureReadableTextOn(Blend(_theme.AccentColor, _theme.PanelBackColor, _theme.IsDark ? 0.70 : 0.82), _theme.ForeColor)
        };
        btn.FlatAppearance.BorderColor = Blend(_theme.AccentColor, _theme.PanelBackColor, 0.45);
        btn.Click += (_, __) => removeAction();
        _cardFilterChipPanel.Controls.Add(btn);
    }

    private void OpenSelectedCardFilterColumn()
    {
        if (_cardFilterColumnBox?.SelectedItem is ViewGridColumn col)
        {
            var y = (_cardFilterBar?.Bottom ?? HeaderHeight) + 2;
            ShowConfiguredFilterMenuForColumn(col, new Point(Math.Max(0, GetColumnLeft(col)), y));
            return;
        }
        OpenBestCardFilterColumn();
    }

    private void OpenBestCardFilterColumn()
    {
        var col = Columns.VisibleColumns.FirstOrDefault(c => c.Filterable && string.Equals(c.AspectName, "Status", StringComparison.OrdinalIgnoreCase))
            ?? Columns.VisibleColumns.FirstOrDefault(c => c.Filterable && (c.Header.Contains("Durum", StringComparison.OrdinalIgnoreCase) || c.Header.Contains("Status", StringComparison.OrdinalIgnoreCase)))
            ?? Columns.VisibleColumns.FirstOrDefault(c => c.Filterable);
        if (col == null) return;
        int y = (_cardFilterBar != null && _cardFilterBar.Visible) ? _cardFilterBar.Bottom + 2 : Math.Max(0, (_cardFilterFloatingButton?.Bottom ?? 0) + 4);
        ShowConfiguredFilterMenuForColumn(col, new Point(Math.Max(8, ClientSize.Width - VBarWidth - 420), y));
    }

    private void RefreshCardViewFilterUx()
    {
        if (_cardFilterBar == null) return;
        UpdateCardViewFilterUxLayout();
    }

    [Category("ViewGrid - Card Filter UX v27.8")]
    [DefaultValue(true)]
    [Description("Kart/poster/dashboard gibi büyük görünümlerde hızlı filtre barının kapladığı alanı içerik çiziminden düşer. Kartların filtre barı altında kesilmesini engeller.")]
    public bool CardViewReserveFilterArea { get; set; } = true;

    [Category("ViewGrid - Card Filter UX v27.8")]
    [DefaultValue(6)]
    [Description("Kart görünümü filtre barı ile kartlar arasında bırakılacak ekstra boşluk.")]
    public int CardFilterContentSpacing { get; set; } = 6;

    [Browsable(false)]
    public int ReservedCardFilterAreaHeight => GetCardFilterReservedHeight();

    private int GetCardFilterReservedHeight()
    {
        if (!CardViewReserveFilterArea) return 0;
        if (_cardFilterBar == null || !_cardFilterBar.Visible) return 0;
        int spacing = Math.Max(0, CardFilterContentSpacing);
        return Math.Max(0, _cardFilterBar.Height + spacing);
    }

    private int GetRowsTopOffset()
    {
        int top = ShowHeader ? HeaderHeight : 0;
        top += GetCardFilterReservedHeight();
        return top;
    }

    private Rectangle GetRowsViewportRectangle()
    {
        int top = GetRowsTopOffset();
        int footer = ShowSummaryFooter ? FooterHeight : 0;
        int hbarHeight = _hbar.Visible ? _hbar.Height : 0;
        int w = Math.Max(0, Width - VBarWidth);
        int h = Math.Max(0, Height - top - footer - hbarHeight);
        return new Rectangle(0, top, w, h);
    }

}
