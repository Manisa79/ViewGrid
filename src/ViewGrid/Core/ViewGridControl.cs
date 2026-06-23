using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using ViewGrid.Columns;
using ViewGrid.Editing;
using ViewGrid.Filtering;
using ViewGrid.Layout;
using ViewGrid.Exporting;
using ViewGrid.Summary;
using ViewGrid.Formatting;
using ViewGrid.Undo;
using ViewGrid.Details;
using ViewGrid.Theming;
using ViewGrid.Virtualization;
using ViewGrid.Localization;
using ViewGrid.Design;

namespace ViewGrid.Core;

public enum ViewGridEmptyStateSignatureAlignment
{
    BottomRight,
    BottomLeft,
    TopRight,
    TopLeft,
    CenterBottom,
    Hidden
}

public enum ViewGridTileCheckBoxPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public enum ViewGridTileCheckBoxVisibilityMode
{
    Always,
    Hover,
    Selected,
    HoverOrSelected,
    CheckedOrHoverOrSelected
}

public enum ViewGridMediaImageScaleMode
{
    Contain,
    Cover,
    Stretch
}

public enum ViewGridDetailCardLayout
{
    Standard,
    Compact,
    PropertyGrid,
    Media,
    PosterLeft
}

[ToolboxItem(true)]
[DesignTimeVisible(true)]
[DefaultProperty(nameof(Columns))]
[DefaultEvent(nameof(SelectionChanged))]
[Designer(typeof(global::ViewGrid.Design.ViewGridControlDesigner))]
[ToolboxBitmap(typeof(ViewGridControl), "ViewGridControl.bmp")]
[Description("ViewGrid / ViewGridControl: ViewGrid List View modern WinForms list/grid control.")]
public partial class ViewGridControl : Control
{
    private const int HeaderHeight = 34;
    private const int FooterHeight = 28;
    private int _rowHeight = 28;
    private int _detailsRowHeight = 28;
    private bool _showDetailCardColumnHeaders = true;
    private int _scrollY;
    private int _scrollX;
    private int _hotRow = -1;
    private int _selectedRow = -1;
    private ViewGridColumn? _activeColumn;
    private readonly SortedSet<int> _selectedRows = new();
    private readonly HashSet<object> _checkedRows = new();
    private readonly Dictionary<ViewGridColumn, HashSet<object>> _internalCheckRowsByColumn = new();
    private int _selectionAnchorRow = -1;
    private ViewGridColumn? _resizingColumn;
    private int _resizeStartX;
    private int _resizeStartWidth;
    private ViewGridColumn? _dragColumn;
    private int _dragColumnStartX;
    private int _dragColumnInsertIndex = -1;
    private bool _dragColumnActive;
    private IRowProvider _provider = new ListRowProvider(Array.Empty<object>());
    private IProviderChangeNotifier? _providerChangeNotifier;
    private readonly VScrollBar _vbar = new() { Dock = DockStyle.Right };
    private readonly HScrollBar _hbar = new() { Dock = DockStyle.Bottom };
    private readonly ViewGridFilterSet _filters = new();
    private readonly List<int> _viewIndexes = new();
    private readonly List<ViewGridDisplayRow> _displayRows = new();
    private readonly HashSet<string> _collapsedGroups = new(StringComparer.CurrentCultureIgnoreCase);
    private ViewGridColumn? _sortColumn;
    private bool _sortDesc;
    private Control? _editor;
    private ICellEditor? _activeCellEditor;
    private (int row, ViewGridColumn col)? _editingCell;
    private ViewGridTheme _theme = WindowsThemeService.CurrentTheme();
    private bool _applyingTheme;
    private readonly List<ViewGridConditionalFormat> _conditionalFormats = new();
    private readonly List<ViewGridSummaryItem> _summaries = new();
    private readonly ViewGridUndoService _undo = new();
    private ViewGridRowDetailsProvider? _detailsProvider;
    private int _expandedRow = -1;
    private Control? _detailsControl;
    private bool _viewIsDirect;
    private int _dataVersion;
    private Dictionary<ViewGridColumn, string>? _summaryTextCache;
    private readonly ViewGridRenderOptions _renderOptions = new();
    private readonly Dictionary<int, int> _selectionAnimations = new();
    private readonly System.Windows.Forms.Timer _animationTimer = new();
    private readonly Dictionary<string, (int version, int maxRows, List<string> values)> _distinctValueCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, RowHighlightState> _rowHighlights = new();
    private readonly System.Windows.Forms.Timer _highlightTimer = new();
    private string _incrementalSearchBuffer = string.Empty;
    private DateTime _lastIncrementalSearchUtc = DateTime.MinValue;
    private Func<object, bool>? _modelFilter;
    private Predicate<object>? _modelFilterPredicate;
    private CancellationTokenSource? _sortCts;
    private int _sortGeneration;

    private sealed class RowHighlightState
    {
        public Color Color { get; init; }
        public DateTime ExpiresUtc { get; init; }
        public string? Reason { get; init; }
    }
    private readonly Dictionary<string, int> _cellOverflowScrollOffsets = new(StringComparer.OrdinalIgnoreCase);
    private string? _hotOverflowCellKey;
    private int _activeCellPaintViewIndex = -1;
    private readonly ConcurrentDictionary<string, SmartFilterIndexSnapshot> _smartFilterIndexCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Task> _smartFilterIndexBuilds = new(StringComparer.OrdinalIgnoreCase);
    private sealed class SmartFilterIndexSnapshot
    {
        public int Version { get; init; }
        public List<string> Values { get; init; } = new();
        public Dictionary<string, int> Counts { get; init; } = new(StringComparer.CurrentCultureIgnoreCase);
    }
    private readonly System.Windows.Forms.Timer _globalFilterDebounceTimer = new();
    private string _pendingGlobalFilterText = string.Empty;
    private ContextMenuStrip? _activeHeaderMenu;
    private bool _columnChooserMenuKeepOpenRequested;
    private Form? _activeFilterPopupForm;
    private static readonly ConcurrentDictionary<string, Size> _floatingFilterPopupSizeMemory = new(StringComparer.OrdinalIgnoreCase);
    private ViewGridMenuDismissMessageFilter? _activeMenuDismissFilter;
    private int _filterPopupGeneration;
    private string _searchHighlightText = string.Empty;
    private int _lastSearchMatchRow = -1;
    private int _smoothWheelRemainder;
    private ViewGridMode _viewMode = ViewGridMode.Details;
    private ViewGridDataMode _mode = ViewGridDataMode.Object;
    private string? _groupByAspectName;
    private Color _customBackColor = Color.Empty;
    private Color _customAlternateBackColor = Color.Empty;
    private Color _customHotBackColor = Color.Empty;
    private Color _customSelectionBackColor = Color.Empty;
    private Color _customSelectionForeColor = Color.Empty;
    private Color _customGroupBackColor = Color.Empty;
    private Color _customGroupForeColor = Color.Empty;
    private Color _highlightBackColor = Color.Empty;
    private Color _highlightBorderColor = Color.Empty;
    private Color _highlightForeColor = Color.Empty;
    private bool _allowActiveMenuClose;
    private bool _designTimeSampleDataInitialized;
    private int VBarWidth => _vbar.Visible ? _vbar.Width : 0;


    public ViewGridControl()
    {
        _menuOptions = new ViewGridMenuCustomizationOptions(this);
        _menuIcons = new ViewGridMenuIconCustomizationOptions(this);
        MenuIconMode = ViewGridMenuIconMode.BuiltInThenCustom;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Columns = new ViewGridColumnCollection();
        Columns.CollectionChanged += (_, __) =>
        {
            if (IsDisposed || Disposing) return;
            try
            {
                AutoSizeFillColumns();
                BuildViewIndex();
            }
            catch
            {
                Invalidate();
            }
        };
        Controls.Add(_vbar);
        Controls.Add(_hbar);
        _vbar.Scroll += (_,__) => { _scrollY = _vbar.Value; PositionRowDetailsControl(); Invalidate(); };
        _hbar.Scroll += (_,__) => { _scrollX = _hbar.Value; PositionRowDetailsControl(); Invalidate(); };
        WindowsThemeService.ThemeChanged += (_,__) => { if (FollowWindowsTheme || ThemePreset == ViewGridThemePreset.System) ApplySelectedTheme(); };
        Font = new Font("Segoe UI", 9F);
        TabStop = true;
        _animationTimer.Interval = _renderOptions.AnimationIntervalMs;
        _animationTimer.Tick += (_, __) => StepSelectionAnimation();
        _globalFilterDebounceTimer.Interval = 220;
        _globalFilterDebounceTimer.Tick += (_, __) =>
        {
            _globalFilterDebounceTimer.Stop();
            _filters.GlobalText = _pendingGlobalFilterText;
            if (HighlightGlobalFilterText) _searchHighlightText = _pendingGlobalFilterText;
            BuildViewIndex();
            RefreshCardViewFilterUx();
        };
        _highlightTimer.Interval = 250;
        _highlightTimer.Tick += (_, __) => PruneExpiredRowHighlights();
        InstallSmartDragDropHandlers();
        InitializeCardViewFilterUx();
    }

    [Category("ViewGrid")]
    [Description("ViewGrid kolon koleksiyonu. Designer penceresi GLV tarzı GLVColumn düzenleme sağlar.")]
    [Browsable(false)]
    [Editor(typeof(ViewGridColumnCollectionEditor), typeof(UITypeEditor))]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public ViewGridColumnCollection Columns { get; }

    [DefaultValue(true)] public bool FollowWindowsTheme { get; set; } = true;
    private ViewGridThemePreset _themePreset = ViewGridThemePreset.System;
    [Category("ViewGrid - Theme"), DefaultValue(ViewGridThemePreset.System)]
    public ViewGridThemePreset ThemePreset
    {
        get => _themePreset;
        set
        {
            _themePreset = value;
            if (value != ViewGridThemePreset.System) FollowWindowsTheme = false;
            ApplySelectedTheme();
        }
    }
    [Category("ViewGrid - Theme"), DefaultValue(true)]
    [Description("ThemePreset=System iken hem runtime hem designer tarafında üst form/panel renginden otomatik uyumlu tema üretir. Windows teması ile uygulama teması farklıysa uygulama temasını öncelikli kullanır.")]
    public bool AutoThemeFromParent { get; set; } = true;

    private ViewGridDesignTimeThemePreview _designTimeThemePreview = ViewGridDesignTimeThemePreview.Auto;
    [Category("ViewGrid - Design Time"), DefaultValue(ViewGridDesignTimeThemePreview.Auto)]
    [Description("Designer önizlemesi için Auto, Light, Dark veya Fluent tema seçimi.")]
    public ViewGridDesignTimeThemePreview DesignTimeThemePreview
    {
        get => _designTimeThemePreview;
        set
        {
            _designTimeThemePreview = value;
            if (IsInDesignMode)
                ApplyDesignTimePreviewTheme();
        }
    }

    [Category("ViewGrid - Design Time"), DefaultValue(true)]
    [Description("Visual Studio tasarım zamanında ViewGrid'nin açık/VS designer uyumlu tema ile çizilmesini sağlar. Runtime temasını etkilemez.")]
    public bool EnableDesignTimeThemeSync { get; set; } = true;

    [Category("ViewGrid - Design Time"), DefaultValue(false)]
    [Description("EnableDesignTimeThemeSync açıkken Auto designer temasının üst form/panel renginden türetilmesini sağlar. Kapalıysa VS designer için temiz açık tema kullanılır.")]
    public bool DesignTimeFollowParentTheme { get; set; } = false;

    [Category("ViewGrid - Design Time"), DefaultValue(true)]
    [Description("Designer'da ViewGrid'ye bağlı context menülerin de açık/VS designer uyumlu tema ile çizilmesini sağlar.")]
    public bool DesignTimeThemeSyncMenus { get; set; } = true;

    [Category("ViewGrid - Design Time"), DefaultValue(false)]
    [Description("Designer'da örnek kolon/satır önizlemesi gösterir. Varsayılan kapalıdır; forma eklenen gerçek Columns koleksiyonunu kirletmez.")]
    public bool DesignTimeSampleData { get; set; } = false;

    [DefaultValue(true)] public bool AlternateRows { get; set; } = true;
    [Category("ViewGrid - Appearance"), DefaultValue(ViewGridRowColorPreset.ThemeDefault)]
    public ViewGridRowColorPreset RowColorPreset { get; set; } = ViewGridRowColorPreset.ThemeDefault;

    [Category("ViewGrid - Appearance"), DefaultValue("State")]
    public string RowColorAspectName { get; set; } = "State";

    [Category("ViewGrid - Appearance"), DefaultValue(0.18)]
    public double RowColorStrength { get; set; } = 0.18;
    [DefaultValue(true)] public bool ShowGridLines { get; set; } = true;
    [DefaultValue(true)] public bool ShowHeader { get; set; } = true;
    [DefaultValue(true)] public bool SortOnColumnClick { get; set; } = true;
    [DefaultValue(true)] public bool ShowFilterMenu { get; set; } = true;
    [Category("ViewGrid - Filtering"), DefaultValue(true)]
    [Description("Kolon başlıklarında default filtre butonlarını gösterir. Kullanıcı özellikle kapatmadıkça açık gelir.")]
    public bool ShowColumnFilterButtons { get; set; } = true;
    [Category("ViewGrid - Filtering"), DefaultValue(26)] public int ColumnFilterButtonWidth { get; set; } = 26;

    [Category("ViewGrid - Header Icons"), DefaultValue(ViewGridFilterIconStyle.Funnel)]
    [Description("Kolon filtre butonunda kullanılacak default simge: Funnel, Dot veya CustomImage.")]
    public ViewGridFilterIconStyle FilterIconStyle { get; set; } = ViewGridFilterIconStyle.Funnel;

    [Category("ViewGrid - Header Icons"), DefaultValue(null)]
    [Description("FilterIconStyle=CustomImage iken kullanılacak global filtre simgesi.")]
    public Image? CustomFilterIcon { get; set; }

    [Category("ViewGrid - Header Icons"), DefaultValue(true)]
    [Description("Sıralama yapılan kolonda sort simgesini gösterir.")]
    public bool ShowColumnSortGlyphs { get; set; } = true;

    [Category("ViewGrid - Header Icons"), DefaultValue(ViewGridSortGlyphStyle.Chevron)]
    [Description("Sort simge stili: Chevron, Triangle veya CustomImage.")]
    public ViewGridSortGlyphStyle SortGlyphStyle { get; set; } = ViewGridSortGlyphStyle.Chevron;

    [Category("ViewGrid - Header Icons"), DefaultValue(null)]
    [Description("SortGlyphStyle=CustomImage iken artan sıralama için kullanılacak global simge.")]
    public Image? CustomSortAscendingIcon { get; set; }

    [Category("ViewGrid - Header Icons"), DefaultValue(null)]
    [Description("SortGlyphStyle=CustomImage iken azalan sıralama için kullanılacak global simge.")]
    public Image? CustomSortDescendingIcon { get; set; }

    [Category("ViewGrid - Compatibility"), DefaultValue(null)]
    [Description("ViewGrid uyumlu küçük ikon listesi. Kolon ImageGetter string key veya int index döndürürse buradan çözülür.")]
    public ImageList? SmallImageList { get; set; }

    [Category("ViewGrid - Compatibility"), DefaultValue(null)]
    [Description("Tile/LargeIcons modları için ViewGrid uyumlu büyük ikon listesi.")]
    public ImageList? LargeImageList { get; set; }

    [Category("ViewGrid - Compatibility"), DefaultValue(null)]
    [Description("Check/state ikonları için ViewGrid uyumlu state image listesi.")]
    public ImageList? StateImageList { get; set; }

    [Category("ViewGrid - Sorting"), DefaultValue(true)]
    [Description("Büyük listelerde sıralamayı UI thread'i kilitlemeden arka planda yapar.")]
    public bool AsyncSortForLargeLists { get; set; } = true;

    [Category("ViewGrid - Sorting"), DefaultValue(5000)]
    [Description("Bu kayıt sayısından sonra sort işlemi async yürütülür.")]
    public int AsyncSortThreshold { get; set; } = 5000;

    [Category("ViewGrid - Sorting"), DefaultValue(true)]
    [Description("Sort sırasında ViewGrid'nin kısa bekleme göstergisi çizmesine izin verir.")]
    public bool ShowSortBusyIndicator { get; set; } = true;

    [Category("ViewGrid - Sorting"), DefaultValue(0)]
    [Description("Async sort sırasında taranacak maksimum satır sayısı. 0 = tüm satırlar.")]
    public int AsyncSortMaxRows { get; set; } = 0;

    [Category("ViewGrid - Sorting"), DefaultValue(true)]
    [Description("Büyük listelerde sort sırasında değerleri cache'ler ve tekrar AspectGetter çağrılarını azaltır.")]
    public bool CacheSortKeysForLargeLists { get; set; } = true;

    [Category("ViewGrid - Context Menu"), DefaultValue(true)]
    [Description("Başlık sağ tık menüsünde aktif filtre varsa hızlı filtre temizleme seçeneklerini gösterir.")]
    public bool ShowQuickClearFilterInHeaderMenu { get; set; } = true;

    [Browsable(false)]
    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(_filters.GlobalText) || _filters.Filters.Count > 0;

    [Browsable(false)] public bool IsSorting { get; private set; }
    [DefaultValue(true)] public bool HotTracking { get; set; } = true;
    [DefaultValue(null)] public string EmptyListMessage { get; set; } = ViewGridText.EmptyList;
    [Category("ViewGrid - Appearance"), DefaultValue(true)]
    [Description("Liste boşken bilgi/empty-state alanını gösterir. Designer veya koddan kapatılabilir.")]
    public bool ShowEmptyListMessage { get; set; } = true;

    [Category("ViewGrid - Appearance")]
    [DefaultValue(true)]
    [Description("Liste boşken empty-state mesajını gösterir/gizler. ShowEmptyListMessage ile aynı alias'tır.")]
    public bool EmptyListMessageVisible
    {
        get => ShowEmptyListMessage;
        set { ShowEmptyListMessage = value; Invalidate(); }
    }

    [Category("ViewGrid - Appearance"), DefaultValue(true)] public bool AutoHideScrollBarsWhenNotNeeded { get; set; } = true;
    [Category("ViewGrid - Appearance"), DefaultValue(true)] public bool EnableModernEmptyState { get; set; } = true;

    [Category("ViewGrid - Appearance")]
    [DefaultValue(true)]
    [Description("Boş liste kartında ViewGrid sürüm imzasını gösterir. Varsayılan olarak sağ altta görünür.")]
    public bool ShowEmptyStateSignature { get; set; } = true;

    [Category("ViewGrid - Appearance")]
    [DefaultValue(ViewGridEmptyStateSignatureAlignment.BottomRight)]
    [Description("Boş liste kartındaki ViewGrid sürüm imzasının konumunu belirler.")]
    public ViewGridEmptyStateSignatureAlignment EmptyStateSignatureAlignment { get; set; } = ViewGridEmptyStateSignatureAlignment.BottomRight;

    [Category("ViewGrid - Appearance")]
    [DefaultValue(0.62)]
    [Description("Boş liste sürüm imzasının görsel yoğunluğu. 0.20 - 1.00 arası önerilir.")]
    public double EmptyStateSignatureOpacity { get; set; } = 0.62;

    [Category("ViewGrid - Appearance"), DefaultValue(true)] public bool EnableRowHoverGlow { get; set; } = true;
    [Category("ViewGrid - Localization"), DefaultValue(ViewGridLanguage.Auto)] public ViewGridLanguage Language { get => ViewGridText.Language; set { ViewGridText.Language = value; Invalidate(); } }
    public void LoadLocalizationFile(string jsonFilePath) { ViewGridText.LoadCustomTranslations(jsonFilePath); Invalidate(); }
    [DefaultValue(28)] public int RowHeight { get => _rowHeight; set { int next = CoerceRowHeightForCurrentView(value); _rowHeight = next; if (!IsTileView) _detailsRowHeight = next; RefreshView(); } }

    [Category("ViewGrid - Multiline Cells")]
    [DefaultValue(false)]
    [Description("Details görünümünde WordWrap kolonları ve metin hücreleri genişliğe göre 4-5 satıra kadar düzgün bölerek çizer.")]
    public bool AllowMultilineCells
    {
        get => _allowMultilineCells;
        set
        {
            if (_allowMultilineCells == value) return;
            _allowMultilineCells = value;
            _rowHeight = CoerceRowHeightForCurrentView(_rowHeight);
            RefreshView();
        }
    }

    [Category("ViewGrid - Multiline Cells")]
    [DefaultValue(5)]
    [Description("AllowMultilineCells aktifken hücre başına en fazla kaç satır çizileceğini belirler.")]
    public int MaxCellTextLines
    {
        get => _maxCellTextLines;
        set
        {
            int newValue = Math.Clamp(value, 1, 12);
            if (_maxCellTextLines == newValue) return;
            _maxCellTextLines = newValue;
            _rowHeight = CoerceRowHeightForCurrentView(_rowHeight);
            RefreshView();
        }
    }

    [Category("ViewGrid - Multiline Cells")]
    [DefaultValue(false)]
    [Description("AllowMultilineCells aktifken RowHeight değerini satır sayısına göre otomatik minimum yüksekliğe çıkarır. Varsayılan kapalıdır; sadece kullanıcı özellikle açarsa detay görünümünde satır yüksekliği büyür.")]
    public bool AutoRowHeightForMultilineCells
    {
        get => _autoRowHeightForMultilineCells;
        set
        {
            if (_autoRowHeightForMultilineCells == value) return;
            _autoRowHeightForMultilineCells = value;
            _rowHeight = CoerceRowHeightForCurrentView(_rowHeight);
            RefreshView();
        }
    }



    [Category("ViewGrid - Appearance")]
    [DefaultValue(true)]
    [Description("Details/List görünümünde satır seçildiğinde RowHeight değerinin yanlışlıkla kart/dash yüksekliğine genişlemesini engeller. Normal grid davranışı için açık kalmalıdır.")]
    public bool LockDetailsRowHeightOnSelection
    {
        get => _lockDetailsRowHeightOnSelection;
        set { _lockDetailsRowHeightOnSelection = value; Invalidate(); }
    }

    [Category("ViewGrid - Appearance")]
    [DefaultValue(28)]
    [Description("Details/List görünümünde kullanılacak sabit satır yüksekliği. Card/Tile/Dashboard moduna geçişten dönüldüğünde ve seçimde bu değer korunur.")]
    public int DetailsRowHeight
    {
        get => Math.Max(20, _detailsRowHeight);
        set
        {
            _detailsRowHeight = Math.Max(20, value);
            if (!IsTileView)
            {
                _rowHeight = _detailsRowHeight;
                RefreshView();
            }
        }
    }

    [DefaultValue(false)] public bool FullRowSelect { get; set; } = true;

    [Category("ViewGrid - DetailCard")]
    [DefaultValue(ViewGridDetailCardLayout.Standard)]
    [Description("DetailCard görünümünde kullanılacak yerleşim. Media/PosterLeft modlarında kapak görseli solda, bilgiler sağda çizilir.")]
    public ViewGridDetailCardLayout DetailCardLayout { get; set; } = ViewGridDetailCardLayout.Standard;

    [Category("ViewGrid - DetailCard")]
    [DefaultValue(156)]
    [Description("Media/PosterLeft DetailCard görünümünde sol taraftaki kapak/afiş alanının genişliği.")]
    public int DetailCardMediaImageWidth { get; set; } = 156;

    [Category("ViewGrid - DetailCard")]
    [DefaultValue(176)]
    [Description("Media/PosterLeft DetailCard görünümünde sol taraftaki kapak/afiş alanının yüksekliği.")]
    public int DetailCardMediaImageHeight { get; set; } = 176;

    [Category("ViewGrid - DetailCard")]
    [DefaultValue(true)]
    [Description("Media DetailCard görünümünde çalma durumu rozetleri, play/pause overlay ve now playing bilgisini kapak üzerinde gösterir.")]
    public bool DetailCardShowMediaPlaybackChrome { get; set; } = true;

    [Category("ViewGrid - DetailCard")]
    [DefaultValue(true)]
    [Description("DetailCard görünümünde kolon başlıklarını etiket olarak gösterir. Kapatılırsa sadece değerler satır satır çizilir.")]
    public bool ShowDetailCardColumnHeaders
    {
        get => _showDetailCardColumnHeaders;
        set
        {
            if (_showDetailCardColumnHeaders == value) return;
            _showDetailCardColumnHeaders = value;
            if (ViewMode == ViewGridMode.DetailCard || ViewMode == ViewGridMode.PropertyCard)
                Invalidate();
        }
    }

    [Category("ViewGrid - Editing"), DefaultValue(false)] public bool EnableCellEditing { get; set; } = false;
    [Category("ViewGrid - Editing"), DefaultValue(false)] public bool CellEditActivationOnDoubleClick { get; set; } = false;
    [Category("ViewGrid - Editing"), DefaultValue(Keys.None)] public Keys CellEditActivationKey { get; set; } = Keys.None;
    [Category("ViewGrid - Editing"), DefaultValue(true)] public bool AllowEditAllCells { get; set; } = true;

    [Category("ViewGrid - Context Menu"), DefaultValue(false)]
    [Description("Body/context menüsünde hücre düzenleme komutunu gösterir. Varsayılan kapalıdır. Açık değilse ve EnableCellEditing ayrıca etkinleştirilmemişse F2/Enter ile hücre edit başlamaz.")]
    public bool ShowEditCellMenuItem { get; set; } = false;

    [Category("ViewGrid - Context Menu"), DefaultValue("")]
    [Description("Hücre düzenleme menü metni. Boş bırakılırsa lokalize varsayılan metin kullanılır.")]
    public string EditCellMenuText { get; set; } = string.Empty;

    [Category("ViewGrid - Context Menu"), DefaultValue("")]
    [Description("Hücre düzenleme menüsünde gösterilecek kısa yol yazısı. Boş bırakılırsa CellEditActivationKey değeri kullanılır.")]
    public string EditCellMenuShortcutText { get; set; } = string.Empty;

    [Category("ViewGrid - Context Menu")]
    [Description("Hücre düzenleme menüsü tıklandığında çalışır. Event bağlanmazsa varsayılan hücre düzenleme başlatılır.")]
    public event EventHandler? EditCellMenuRequested;

    [DefaultValue(true)] public bool EnableInlineDatabaseEditing { get; set; } = true;
    [DefaultValue(false)] public bool ShowSummaryFooter { get; set; }
    [DefaultValue(50000)] public int MaxSummaryScanRows { get; set; } = 50000;
    [DefaultValue(50000)] public int MaxFilterDistinctScanRows { get; set; } = 50000;
    [DefaultValue(1000000)] public int MaxVirtualFilterScanRows { get; set; } = 1000000;
    [Category("ViewGrid - Filtering"), DefaultValue(true)] public bool FastFilterMenuForHugeLists { get; set; } = true;
    [Category("ViewGrid - Filtering"), DefaultValue(300)] public int FastFilterMenuInitialScanRows { get; set; } = 300;
    [Category("ViewGrid - Filtering"), DefaultValue(1000000)] public int FastFilterMenuSearchScanRows { get; set; } = 1000000;

    [Category("ViewGrid - Header"), DefaultValue(5000)]
    [Description("Provider bulk checkbox özeti desteklemiyorsa header checkbox durumunu hesaplamak için taranacak maksimum satır sayısı.")]
    public int MaxHeaderCheckBoxScanRows { get; set; } = 5000;

    [Category("ViewGrid - Filtering"), DefaultValue(true)] public bool TypedFilterSearchesAllRows { get; set; } = true;
    [Category("ViewGrid - Filtering"), DefaultValue(2000)] public int MaxEmbeddedFilterVisibleValues { get; set; } = 2000;
    [Category("ViewGrid - Filtering"), DefaultValue(true)] public bool AsyncLoadFullFilterValues { get; set; } = true;
    [Category("ViewGrid - Filtering"), DefaultValue(1000000)] public int MaxAsyncFilterDistinctScanRows { get; set; } = 1000000;
    [Category("ViewGrid - Filtering"), DefaultValue(300)] public int FastFilterPopupPreviewRows { get; set; } = 300;
    [Category("ViewGrid - Filtering"), DefaultValue(75000)] public int FastFilterIndexedProviderThreshold { get; set; } = 75000;
    [Category("ViewGrid - Smart Filter"), DefaultValue(true)] public bool EnableSmartFilterEngine { get; set; } = true;
    [Category("ViewGrid - Smart Filter"), DefaultValue(true)] public bool BuildSmartFilterIndexInBackground { get; set; } = true;
    [Category("ViewGrid - Smart Filter"), DefaultValue(35)] public int SmartFilterSearchDebounceMs { get; set; } = 35;
    [Category("ViewGrid - Smart Filter"), DefaultValue(500)] public int SmartFilterPopupValueLimit { get; set; } = 500;
    [Category("ViewGrid - Smart Filter"), DefaultValue(2000000)] public int SmartFilterMaxScanRows { get; set; } = 2000000;
    [Category("ViewGrid - Smart Filter"), DefaultValue(true)] public bool SmartFilterSearchAllRows { get; set; } = true;
    [Category("ViewGrid - Smart Filter"), DefaultValue(true)] public bool SmartFilterTopValuesFirst { get; set; } = true;
    [Category("ViewGrid - Virtualization"), DefaultValue(300)] public int CachePreloadExtraRows { get; set; } = 300;
    [DefaultValue(true)] public bool DebounceGlobalFilterForHugeVirtualLists { get; set; } = true;
    [DefaultValue(true)] public bool HighlightSearchText { get; set; } = true;
    [DefaultValue(true)] public bool HighlightGlobalFilterText { get; set; } = true;
    [Category("ViewGrid - Cell Overflow")]
    [DefaultValue(true)]
    [Description("Allow mouse wheel to scroll long multiline text inside a cell before scrolling the whole grid.")]
    public bool EnableCellOverflowScroll { get; set; } = true;

    [Category("ViewGrid - Cell Overflow")]
    [DefaultValue(true)]
    [Description("Draw a subtle mini scrollbar inside overflowing multiline cells.")]
    public bool ShowCellOverflowScrollBars { get; set; } = true;

    [Category("ViewGrid - Cell Overflow")]
    [DefaultValue(true)]
    [Description("Show a small reader popup when double-clicking an overflowing cell whose column allows it.")]
    public bool EnableCellOverflowDetailsPopup { get; set; } = true;

    [DefaultValue(true)] public bool SmoothMouseWheelScroll { get; set; } = true;
    [DefaultValue(3)] public int MouseWheelRowsPerNotch { get; set; } = 3;
    [DefaultValue(0)] public int FrozenColumnCount { get; set; }
    [DefaultValue(true)] public bool EnableClipboard { get; set; } = true;
    [DefaultValue(true)] public bool EnableIncrementalSearch { get; set; } = true;
    [Category("ViewGrid - Keyboard"), DefaultValue(900)] public int IncrementalSearchResetMs { get; set; } = 900;
    [Category("ViewGrid - Highlight"), DefaultValue(true)] public bool EnableHighlightEngine { get; set; } = true;
    [Category("ViewGrid - Highlight"), DefaultValue(7000)] public int DefaultHighlightDurationMs { get; set; } = 7000;
    [DefaultValue(true)] public bool EnableSelectAllShortcut { get; set; } = true;
    [DefaultValue(true)] public bool EnableUndoRedo { get; set; } = true;
    [DefaultValue(true)] public bool MultiSelect { get; set; } = true;
    [DefaultValue(true)] public bool EnableColumnResize { get; set; } = true;
    [Category("ViewGrid - Column Manager"), DefaultValue(true)] public bool EnableColumnAutoResizeOnDoubleClick { get; set; } = true;
    [Category("ViewGrid - Column Manager"), DefaultValue(600)] public int AutoResizeMaxWidth { get; set; } = 600;
    [Category("ViewGrid - Column Manager"), DefaultValue(true)] public bool AutoResizeIncludeHeader { get; set; } = true;
    [Category("ViewGrid - Column Manager"), DefaultValue(2000)] public int AutoResizeSampleRows { get; set; } = 2000;
    [Category("ViewGrid - Column Manager"), DefaultValue(true)]
    [Description("DataSource/SetObjects/RefreshObjects sonrasında FillFreeSpace kolonlarını ilk çizimde de kontrol genişliğine oturtur. Form resize beklemez.")]
    public bool AutoFitFillColumnsOnDataLoad { get; set; } = true;

    [Category("ViewGrid - Column Manager"), DefaultValue(true)]
    [Description("FillFreeSpace hesabı yapılırken sabit kolonların Width/MinimumWidth değerleri korunur. Log ekranlarında tarih/user gibi sabit kolonların veri eklenirken daralmasını engeller.")]
    public bool PreserveFixedColumnWidthsOnAutoFit { get; set; } = true;

    [Category("ViewGrid - Column Manager")]
    [DefaultValue(true)]
    [Description("Kolon elle genişletilirken önce FillFreeSpace kolonlarındaki kullanılabilir boşluğu daraltır; boşluk bitince yatay scrollbar açılır. ObjectListView FixedFree hissine yakın davranış sağlar.")]
    public bool AbsorbColumnResizeOverflowFromFreeSpace { get; set; } = true;

    [Category("ViewGrid - Images"), DefaultValue(true)]
    [Description("Text kolonunda ImageGetter/StateImageGetter kullanıldığında ikon genişliği AutoResize ölçümüne dahil edilir. Tarih/user + ikon log kolonlarında kesilmeyi azaltır.")]
    public bool IncludeCellImagesInAutoResizeWidth { get; set; } = true;

    [Category("ViewGrid - Images"), DefaultValue(24)]
    [Description("Text kolonlarındaki hücre ikonları için ayrılacak yaklaşık genişlik. Log listelerinde ikon + metin sığdırma hesabında kullanılır.")]
    public int CellImageTextPadding { get; set; } = 24;
    [Category("ViewGrid - Column Manager"), DefaultValue(true)] public bool AllowColumnReorder { get; set; } = true;
    [Category("ViewGrid - Column Manager"), DefaultValue(false)] public bool AutoSaveColumnLayout { get; set; } = false;
    [Category("ViewGrid - Column Manager"), DefaultValue(true)] public bool ShowColumnReorderPreview { get; set; } = true;
    [Category("ViewGrid - Column Manager"), DefaultValue(null)] public string? ColumnLayoutStorageKey { get; set; }
    [DefaultValue(true)] public bool FilterMenuOnRightClick { get; set; } = true;
    [DefaultValue(false)] public bool UseEmbeddedHeaderFilterMenu { get; set; } = false;
    [Category("ViewGrid - Filtering"), DefaultValue(ViewGridFilterMenuMode.PopupMenu)]
    [Description("Header filtre ikonuna tıklandığında açılacak varsayılan filtre deneyimi. PopupMenu hızlı Excel benzeri açılır menüdür; ModalWindow ayrı pencereyi açar; Both popup içinde ayrı pencere bağlantısını da gösterir.")]
    public ViewGridFilterMenuMode FilterMenuMode { get; set; } = ViewGridFilterMenuMode.PopupMenu;
    [Category("ViewGrid - Filtering"), DefaultValue(true)] public bool ShowFilterStyleSelectorInContextMenu { get; set; } = true;

    [Category("ViewGrid - Context Menu"), DefaultValue(ViewGridHeaderContextMenuBehavior.Full)]
    [Description("Kolon başlığına sağ tıklanınca hangi menü davranışının kullanılacağını belirler. None: menü yok, FilterOnly: sadece özel filtre popup'ı, Full: tüm başlık menüsü.")]
    public ViewGridHeaderContextMenuBehavior HeaderContextMenuBehavior { get; set; } = ViewGridHeaderContextMenuBehavior.Full;


    [Category("ViewGrid - Context Menu"), DefaultValue(ViewGridMenuProfile.Full)]
    [Description("ViewGrid'nin hazır menü profilini belirler. None: ViewGrid kendi sağ tık menüsünü göstermez. Custom: grup ve tekil ayarlar aynen kullanılır.")]
    public ViewGridMenuProfile MenuProfile { get; set; } = ViewGridMenuProfile.Full;

    [Category("ViewGrid - Context Menu"), DefaultValue(ViewGridMenuGroups.All)]
    [Description("Başlık/kolon menüsünde hangi özellik gruplarının görüneceğini belirler.")]
    public ViewGridMenuGroups HeaderMenuGroups { get; set; } = ViewGridMenuGroups.All;

    [Category("ViewGrid - Context Menu"), DefaultValue(ViewGridMenuGroups.All)]
    [Description("Liste/satır sağ tık menüsünde hangi özellik gruplarının görüneceğini belirler.")]
    public ViewGridMenuGroups BodyMenuGroups { get; set; } = ViewGridMenuGroups.All;

    [Category("ViewGrid - Context Menu"), DefaultValue(true)]
    [Description("ViewGrid'nin kolon başlığı sağ tık menüsünü kullanır. False ise header tarafında kendi ContextMenuStrip'inizi kullanabilirsiniz.")]
    public bool UseBuiltInHeaderMenu { get; set; } = true;

    [Category("ViewGrid - Context Menu"), DefaultValue(true)]
    [Description("ViewGrid'nin liste/satır sağ tık menüsünü kullanır. False ise body tarafında kendi ContextMenuStrip'inizi kullanabilirsiniz.")]
    public bool UseBuiltInBodyMenu { get; set; } = true;

    [Category("ViewGrid - Context Menu"), DefaultValue(null)]
    [Description("Verilirse kolon başlığında ViewGrid menüsü yerine bu özel ContextMenuStrip açılır.")]
    public ContextMenuStrip? HeaderMenuOverride { get; set; }

    [Category("ViewGrid - Context Menu"), DefaultValue(null)]
    [Description("Verilirse liste/satır alanında ViewGrid menüsü yerine bu özel ContextMenuStrip açılır.")]
    public ContextMenuStrip? BodyMenuOverride { get; set; }

    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool CloseHeaderContextMenuBeforeOpeningFilterPopup { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuFilterItems { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuSortItems { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuFreezeItems { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuAutoSizeItems { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuLayoutItems { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuGroupingItems { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowGroupHeaderContextMenu { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowGroupMenuToggleThisGroupItem { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowGroupMenuExpandAllItem { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowGroupMenuCollapseAllItem { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowGroupMenuClearGroupingItem { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowGroupMenuOnlyThisGroupItem { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuColumnChooserItem { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuModeItems { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuViewModeItems { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuFilterStyleItems { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuThemeItems { get; set; } = true;
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuStateItems { get => ShowStateMenuItems; set => ShowStateMenuItems = value; }
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowHeaderMenuScenarioItems { get => ShowScenarioMenuItems; set => ShowScenarioMenuItems = value; }
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowMergedMenuModeItems { get => ShowHeaderMenuModeItems; set => ShowHeaderMenuModeItems = value; }
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowMergedMenuViewModeItems { get => ShowHeaderMenuViewModeItems; set => ShowHeaderMenuViewModeItems = value; }
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowMergedMenuFilterStyleItems { get => ShowHeaderMenuFilterStyleItems; set => ShowHeaderMenuFilterStyleItems = value; }
    [Category("ViewGrid - Context Menu"), DefaultValue(true)] public bool ShowMergedMenuThemeItems { get => ShowHeaderMenuThemeItems; set => ShowHeaderMenuThemeItems = value; }

    [Category("ViewGrid - Context Menu"), DefaultValue(true)]
    [Description("Alt+Down veya Ctrl+Shift+F10 ile aktif/sıralı/ilk görünür kolon menüsünü klavyeden açar.")]
    public bool KeyboardColumnContextMenuKeyOpensMenu { get; set; } = true;

    [Category("ViewGrid - Context Menu"), DefaultValue(true)]
    [Description("Kullanıcının verdiği ContextMenuStrip içine ViewGrid'nin temel liste özellikleri alt menüsünü otomatik ekler.")]
    public bool MergeBuiltInMenuWithUserContextMenu { get; set; } = true;

    [Category("ViewGrid - Context Menu"), DefaultValue("Liste özellikleri")]
    [Description("ContextMenuStrip ile birleştirilen ViewGrid alt menüsünün başlığı.")]
    public string BuiltInMenuMergeText { get; set; } = string.Empty;

    [Category("ViewGrid - Context Menu"), DefaultValue(ViewGridMenuMergePlacement.Bottom)]
    [Description("ViewGrid menüsü kullanıcı ContextMenuStrip içine nereye eklensin: Top, Bottom, BeforeFirstSeparator veya AfterFirstSeparator.")]
    public ViewGridMenuMergePlacement BuiltInMenuMergePlacement { get; set; } = ViewGridMenuMergePlacement.Bottom;

    [Category("ViewGrid - Context Menu"), DefaultValue(ViewGridMenuMergePresentation.SubMenu)]
    [Description("ViewGrid menüsü kullanıcı menüsüne nasıl eklensin: tek alt menü, direkt inline veya grup alt menüleri.")]
    public ViewGridMenuMergePresentation BuiltInMenuMergePresentation { get; set; } = ViewGridMenuMergePresentation.SubMenu;

    [Category("ViewGrid - Context Menu"), DefaultValue(ViewGridMenuGroups.All)]
    [Description("Kullanıcı menüsüyle merge edilirken hangi ViewGrid grupları eklensin.")]
    public ViewGridMenuGroups MergedMenuGroups { get; set; } = ViewGridMenuGroups.All;

    [Category("ViewGrid - Context Menu"), DefaultValue("Mode,ViewMode,Filter,Sort,ColumnChooser,Clipboard,AutoSize,Layout,Grouping,FilterStyle,Theme")]
    [Description("Merge edilen ViewGrid gruplarının görüntü sırası. Örn: Filter,ColumnChooser,Clipboard,AutoSize,Theme")]
    public string MergedMenuGroupOrder { get; set; } = "Mode,ViewMode,Filter,Sort,ColumnChooser,Clipboard,AutoSize,Layout,Grouping,FilterStyle,Theme";

    [Category("ViewGrid - Context Menu"), DefaultValue(true)]
    [Description("Kullanıcı menüsüyle ViewGrid menüsü arasına otomatik ayraç ekler.")]
    public bool BuiltInMenuMergeSeparator { get; set; } = true;

    [Category("ViewGrid - Keyboard"), DefaultValue(true)] public bool KeyboardSelectFirstRowOnFocus { get; set; } = true;
    [Category("ViewGrid - Keyboard"), DefaultValue(true)] public bool KeyboardContextMenuKeyOpensMenu { get; set; } = true;





    [Category("ViewGrid - Filter Popup v27.3.1"), DefaultValue(true)]
    [Description("Kolon filtre penceresinin kullanıcı tarafından kenar/köşeden boyutlandırılabilmesini sağlar.")]
    public bool FilterPopupResizable { get; set; } = true;

    [Category("ViewGrid - Filter Popup v27.3.1")]
    [Description("Kolon filtre penceresinin varsayılan açılış boyutudur.")]
    public Size FilterPopupDefaultSize { get; set; } = new Size(520, 520);

    [Category("ViewGrid - Filter Popup v27.3.1")]
    [Description("Kolon filtre penceresinin minimum boyutudur.")]
    public Size FilterPopupMinimumSize { get; set; } = new Size(320, 360);

    [Category("ViewGrid - Filter Popup v27.3.1")]
    [Description("Kolon filtre penceresinin maksimum boyutudur.")]
    public Size FilterPopupMaximumSize { get; set; } = new Size(1800, 1200);

    [Category("ViewGrid - Filter Popup v27.3.1"), DefaultValue(true)]
    [Description("Kullanıcının filtre popup boyutunu değiştirmesi halinde aynı kolon için sonraki açılışta bu boyutu hatırlar.")]
    public bool FilterPopupRememberSize { get; set; } = true;

    [Category("ViewGrid - Filter Popup v27.3.1"), DefaultValue(true)]
    [Description("Filtre değerleri satıra sığmadığında tam metni tooltip olarak gösterir.")]
    public bool FilterPopupShowValueTooltips { get; set; } = true;

    [Category("ViewGrid - Filter Popup v27.3.1"), DefaultValue(true)]
    [Description("Uzun filtre değerleri için popup açılış genişliğini maksimum sınıra kadar otomatik büyütür.")]
    public bool FilterPopupAutoWidthForLongValues { get; set; } = true;

    [Category("ViewGrid - Filter Popup v28.10"), DefaultValue(true)]
    [Description("Floating ve ayrı filtre menüsündeki komut satırlarında tema uyumlu ikonlar gösterir.")]
    public bool FilterPopupShowActionIcons { get; set; } = true;

    [Category("ViewGrid - Filter Popup v28.10"), DefaultValue(true)]
    [Description("Aktif sıralama/filtre komutlarını popup içinde accent renkli arka planla vurgular.")]
    public bool FilterPopupHighlightActiveCommands { get; set; } = true;

    [Category("ViewGrid - Filter Popup v28.10"), DefaultValue(true)]
    [Description("Floating filtre popup'ını sadece köşe gripinden değil, sağ/alt kenarlardan da büyütüp küçültmeyi sağlar.")]
    public bool FilterPopupEdgeResize { get; set; } = true;

    [Category("ViewGrid - Filter Popup v27.3.1")]
    [Description("Ayrı filtre penceresinin maksimum boyutudur.")]
    public Size FilterWindowMaximumSize { get; set; } = new Size(2000, 1400);

    [Category("ViewGrid - Filter Popup v27.3.1"), DefaultValue(true)]
    [Description("Popup ve ayrı filtre penceresi büyürken bulunduğu ekranın çalışma alanını aşmasını engeller.")]
    public bool FilterPopupLimitToWorkingArea { get; set; } = true;

    [Category("ViewGrid - Filter Popup v28.12.2"), DefaultValue(true)]
    [Description("Card/Tile/Dashboard gibi başlıksız görünümlerde filtre popup konumu mouse/buton yakınından hesaplanır; Details görünümünde kolon başlığı kullanılır.")]
    public bool FilterPopupAnchorToMouseWhenHeaderUnavailable { get; set; } = true;

    [Category("ViewGrid - Filter Popup v28.12.2"), DefaultValue(true)]
    [Description("Filtre ikonundan ve sağ tık menüsünden açılan popup filtre aynı floating/resizable görünümü kullanır.")]
    public bool UseUnifiedFloatingFilterPopup { get; set; } = true;

    [Category("ViewGrid - Rendering v27.3"), DefaultValue(true)]
    [Description("Badge, tag, button ve hyperlink hücrelerini v27.3 premium renderer görünümüyle çizer.")]
    public bool EnableV273RenderingUx { get; set; } = true;


    [Category("ViewGrid - Rendering v27.3"), DefaultValue(true)]
    [Description("Badge hücrelerinde Open/Closed/Fail/OK/Warning gibi metinlere göre anlamlı durum rengi üretir.")]
    public bool BadgeUseSemanticStatusColors { get; set; } = true;


    [Category("ViewGrid - Rendering v27.3"), DefaultValue(true)]
    [Description("Tag hücrelerinde her etiketi chip/pill olarak ayırır ve uzun metinlerde sığdığı kadarını gösterir.")]
    public bool TagsUseChipRenderer { get; set; } = true;

    [Category("ViewGrid - Rendering v27.3"), DefaultValue(10)]
    [Description("Badge ve tag hücrelerinde kullanılan yuvarlatma yarıçapıdır.")]
    public int CellPillCornerRadius { get; set; } = 10;

    [Category("ViewGrid - ProgressBar"), DefaultValue(true)]
    [Description("ProgressBar hücrelerini modern, yuvarlatılmış ve tema uyumlu çizer.")]
    public bool EnableModernProgressBar { get; set; } = true;

    [Category("ViewGrid - ProgressBar"), DefaultValue(true)]
    [Description("ProgressBar içinde yüzde metnini gösterir.")]
    public bool ProgressBarShowText { get; set; } = true;

    [Category("ViewGrid - ProgressBar"), DefaultValue(true)]
    [Description("ProgressBar dolum renginde Windows/tema accent rengini kullanır. Kapalı ise düşük/orta/yüksek eşik rengi kullanılır.")]
    public bool ProgressBarUseAccentColor { get; set; } = true;

    [Category("ViewGrid - ProgressBar"), DefaultValue(true)]
    [Description("ProgressBar dolumunda yumuşak gradient ve parlama efekti kullanır.")]
    public bool ProgressBarUseGradient { get; set; } = true;

    [Category("ViewGrid - ProgressBar"), DefaultValue(false)]
    [Description("ProgressBar dolumuna hafif hareketli shine efekti ekler. Çok büyük listelerde CPU/bellek için varsayılan kapalıdır.")]
    public bool ProgressBarAnimated { get; set; } = false;

    [Category("ViewGrid - ProgressBar"), DefaultValue(6)]
    public int ProgressBarCornerRadius { get; set; } = 6;

    [Category("ViewGrid - ProgressBar"), DefaultValue(12)]
    public int ProgressBarHeight { get; set; } = 12;

    [Category("ViewGrid - ProgressBar"), DefaultValue(35)]
    public int ProgressBarLowThreshold { get; set; } = 35;

    [Category("ViewGrid - ProgressBar"), DefaultValue(75)]
    public int ProgressBarHighThreshold { get; set; } = 75;

    [Category("ViewGrid - Printing"), DefaultValue(true)]
    [Description("Yazdırma ve önizlemede kolon başlıklarını gösterir.")]
    public bool PrintShowHeader { get; set; } = true;

    [Category("ViewGrid - Printing"), DefaultValue(false)]
    [Description("Yazdırma ve önizlemede ekran temasını birebir kullanır. Varsayılan kapalıdır; çıktı okunaklı, beyaz kağıt uyumlu hazırlanır.")]
    public bool PrintUseThemeColors { get; set; } = false;

    [Category("ViewGrid - Printing"), DefaultValue(true)]
    [Description("Yazdırma ve önizlemede sadece görünen kolonları yazdırır.")]
    public bool PrintOnlyVisibleColumns { get; set; } = true;

    [Category("ViewGrid - Printing"), DefaultValue(false)]
    [Description("Yazdırma ve önizlemede sadece seçili satırları kullanır.")]
    public bool PrintSelectedRowsOnly { get; set; } = false;

    [Category("ViewGrid - Printing"), DefaultValue(5000)]
    [Description("Yanlışlıkla devasa çıktı alınmasını engellemek için yazdırılacak en fazla satır sayısı. 0: sınırsız.")]
    public int PrintMaxRows { get; set; } = 5000;

    [Category("ViewGrid - Printing"), DefaultValue("ViewGridControl")]
    public string PrintTitle { get; set; } = "ViewGridControl";

    [Category("ViewGrid - Printing"), DefaultValue(true)]
    [Description("Yazdırma çıktısında ProgressBar kolonlarını metin yerine modern bar olarak çizer.")]
    public bool PrintRenderProgressBars { get; set; } = true;

    [Category("ViewGrid - Printing"), DefaultValue(true)]
    [Description("Koyu temada bile yazdırma/önizleme çıktısını açık zemin ve yüksek kontrastla hazırlar.")]
    public bool PrintPreferReadableColors { get; set; } = true;

    [Category("ViewGrid - Printing"), DefaultValue(true)]
    [Description("Yazdırma çıktısında kolonları sayfa genişliğine orantılı sığdırır. Excel benzeri okunaklı çıktı için varsayılan açıktır.")]
    public bool PrintFitToPageWidth { get; set; } = true;

    [Category("ViewGrid - Printing"), DefaultValue(true)]
    [Description("Yazdırma çıktısında hücre çizgilerini gösterir.")]
    public bool PrintShowGrid { get; set; } = true;

    [Category("ViewGrid - Printing"), DefaultValue(true)]
    [Description("Yazdırma çıktısında zebra satır arka planı kullanır.")]
    public bool PrintZebraRows { get; set; } = true;

    [DefaultValue(true)] public bool EnableFluentBackdrop { get => _renderOptions.EnableFluentBackdrop; set { _renderOptions.EnableFluentBackdrop = value; Invalidate(); } }
    [DefaultValue(true)] public bool EnableAcrylicSimulation { get => _renderOptions.EnableAcrylicSimulation; set { _renderOptions.EnableAcrylicSimulation = value; Invalidate(); } }
    [DefaultValue(true)] public bool EnableAnimatedSelection { get => _renderOptions.EnableAnimatedSelection; set { _renderOptions.EnableAnimatedSelection = value; Invalidate(); } }
    [DefaultValue(true)] public bool EnableSoftShadows { get => _renderOptions.EnableSoftShadows; set { _renderOptions.EnableSoftShadows = value; Invalidate(); } }
    [DefaultValue(true)] public bool EnableRoundedCells { get => _renderOptions.EnableRoundedCells; set { _renderOptions.EnableRoundedCells = value; Invalidate(); } }
    [DefaultValue(false)] public bool PreferGpuRenderer { get => _renderOptions.PreferGpuRenderer; set { _renderOptions.PreferGpuRenderer = value; Invalidate(); } }
    [DefaultValue(false)] public bool EnableGrouping { get; set; }
    [DefaultValue(null)] public string? GroupByAspectName { get => _groupByAspectName; set { _groupByAspectName = value; BuildViewIndex(); } }
    [DefaultValue(30)] public int GroupHeaderHeight { get; set; } = 30;
    [Category("ViewGrid - Grouping"), DefaultValue(true)] public bool AllowGroupCollapse { get; set; } = true;
    [Category("ViewGrid - Grouping"), DefaultValue(true)] public bool DrawGroupCollapseGlyph { get; set; } = true;
    [Category("ViewGrid - Mode"), DefaultValue(ViewGridDataMode.Object)]
    [Description("ViewGridControl ana çalışma modu: Object, DataTable, Virtual, Tree veya Tile.")]
    public ViewGridDataMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            ApplyModeVisualDefaults(value);
            Invalidate();
        }
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(ViewGridMode.Details)]
    [Description("ViewGrid'nin gelişmiş görünüm modu. Details/List/Tile/LargeIcons gibi modları yönetir.")]
    public ViewGridMode ViewMode { get => _viewMode; set => SetViewMode(value); }

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [DefaultValue(System.Windows.Forms.View.Details)]
    [Description("Eski GLV/ListView kodları için uyumluluk alias'ı. Yeni kullanımda ViewMode tercih edilmelidir.")]
    public System.Windows.Forms.View View
    {
        get => _viewMode switch
        {
            ViewGridMode.Poster => System.Windows.Forms.View.LargeIcon,
            ViewGridMode.MediaTile => System.Windows.Forms.View.Tile,
            ViewGridMode.Gallery => System.Windows.Forms.View.LargeIcon,
            ViewGridMode.FilmStrip => System.Windows.Forms.View.Tile,
            ViewGridMode.ExtraLargeIcons => System.Windows.Forms.View.LargeIcon,
            ViewGridMode.LargeIcons => System.Windows.Forms.View.LargeIcon,
            ViewGridMode.MediumIcons => System.Windows.Forms.View.SmallIcon,
            ViewGridMode.List => System.Windows.Forms.View.List,
            ViewGridMode.Tile => System.Windows.Forms.View.Tile,
            ViewGridMode.LargeCard => System.Windows.Forms.View.Tile,
            ViewGridMode.DashboardCard => System.Windows.Forms.View.Tile,
            ViewGridMode.RowCard => System.Windows.Forms.View.Tile,
            ViewGridMode.DetailCard => System.Windows.Forms.View.Tile,
            ViewGridMode.IconGrid => System.Windows.Forms.View.LargeIcon,
            ViewGridMode.GroupedList => System.Windows.Forms.View.Details,
            ViewGridMode.GroupCard => System.Windows.Forms.View.Tile,
            ViewGridMode.PropertyCard => System.Windows.Forms.View.Tile,
            ViewGridMode.KpiDashboard => System.Windows.Forms.View.Tile,
            ViewGridMode.HeatMap => System.Windows.Forms.View.Tile,
            ViewGridMode.MiniChart => System.Windows.Forms.View.Tile,
            ViewGridMode.RowPreview => System.Windows.Forms.View.Tile,
            ViewGridMode.Kanban => System.Windows.Forms.View.Tile,
            ViewGridMode.Timeline => System.Windows.Forms.View.Tile,
            ViewGridMode.MasterDetail => System.Windows.Forms.View.Details,
            ViewGridMode.DenseList => System.Windows.Forms.View.Details,
            _ => System.Windows.Forms.View.Details
        };
        set => SetViewMode(value switch
        {
            System.Windows.Forms.View.LargeIcon => ViewGridMode.LargeIcons,
            System.Windows.Forms.View.SmallIcon => ViewGridMode.MediumIcons,
            System.Windows.Forms.View.List => ViewGridMode.List,
            System.Windows.Forms.View.Tile => ViewGridMode.Tile,
            _ => ViewGridMode.Details
        });
    }
    private int _tilePreferredWidth = 220;
    private int _tilePreferredHeight = 88;
    private int _tileMaxTextLines = 4;
    private bool _tileShowAllVisibleTextColumns = true;
    private bool _tilePosterMode;
    private int _tilePosterImageHeight = 132;
    private bool _posterModeAutoLayout = true;
    private int _posterPreferredWidth = 220;
    private int _posterPreferredHeight = 300;
    private int _posterImageHeight = 176;
    private ViewGridMediaImageScaleMode _mediaImageScaleMode = ViewGridMediaImageScaleMode.Contain;
    private bool _mediaImageRoundedCorners = true;
    private bool _tileCheckBoxes;
    private ViewGridTileCheckBoxPosition _tileCheckBoxPosition = ViewGridTileCheckBoxPosition.TopRight;
    private int _tileCheckBoxSize = 18;
    private int _tileCheckBoxMargin = 9;
    private string _tileCheckBoxAspectName = string.Empty;
    private bool _tileCheckBoxReserveTextArea = true;
    private bool _tileCheckBoxDrawOnTop = true;
    private bool _tileCheckBoxShowBackground = true;
    private int _tileCheckBoxHitPadding = 4;
    private ViewGridTileCheckBoxVisibilityMode _tileCheckBoxVisibilityMode = ViewGridTileCheckBoxVisibilityMode.Always;
    private bool _autoSizeTileWidthToContent;
    private bool _autoFitTileRowHeightToPreferred = true;
    private bool _allowMultilineCells;
    private int _maxCellTextLines = 5;
    private bool _autoRowHeightForMultilineCells = false;
    private bool _lockDetailsRowHeightOnSelection = true;
    private int _largeCardPreferredWidth = 520;
    private int _largeCardPreferredHeight = 168;
    private int _largeCardMaxTextLines = 8;

    private void ResetTileRowHeightToPreferredIfNeeded()
    {
        if (!AutoFitTileRowHeightToPreferred || !IsTileView) return;
        int preferred = GetMinimumRowHeightForCurrentTileView();
        if (_rowHeight != preferred)
            _rowHeight = preferred;
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(220)]
    [Description("Tile/Kart görünümünde kullanılacak en küçük kart genişliği.")]
    public int TileMinWidth
    {
        get => _tilePreferredWidth;
        set
        {
            int newValue = Math.Max(96, value);
            if (_tilePreferredWidth == newValue) return;
            _tilePreferredWidth = newValue;
            UpdateScrollbars();
            Invalidate();
        }
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(220)]
    [Description("Tile/Kart görünümünde tercih edilen kart genişliği. TileMinWidth ile uyumluluk için aynı değeri kullanır.")]
    public int TilePreferredWidth
    {
        get => _tilePreferredWidth;
        set => TileMinWidth = value;
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(false)]
    [Description("Tile/Kart genişliğini görünür içerik kolonlarına göre otomatik hesaplar.")]
    public bool AutoSizeTileWidthToContent
    {
        get => _autoSizeTileWidthToContent;
        set
        {
            if (_autoSizeTileWidthToContent == value) return;
            _autoSizeTileWidthToContent = value;
            UpdateScrollbars();
            Invalidate();
        }
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(88)]
    public int TilePreferredHeight
    {
        get => _tilePreferredHeight;
        set
        {
            int newValue = Math.Max(48, value);
            if (_tilePreferredHeight == newValue) return;
            _tilePreferredHeight = newValue;
            if (EnforceTilePreferredHeight && IsTileView)
                ResetTileRowHeightToPreferredIfNeeded();
            UpdateScrollbars();
            Invalidate();
        }
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(true)]
    [Description("Tile/kart görünümünde RowHeight değeri yanlışlıkla küçük verilse bile kart yüksekliğini TilePreferredHeight altına düşürmez. Seçim, refresh veya host ayarı sonrası kartların tek satıra çökmesini engeller.")]
    public bool EnforceTilePreferredHeight { get; set; } = true;

    [Category("ViewGrid - View Modes")]
    [DefaultValue(true)]
    [Description("Tile/poster/kart görünümünde tercih edilen yükseklik azaltıldığında RowHeight değerini de yeni tercih edilen yüksekliğe geri çeker. Poster görünümünde büyüt-küçült sonrası çok uzun boş kart oluşmasını engeller.")]
    public bool AutoFitTileRowHeightToPreferred
    {
        get => _autoFitTileRowHeightToPreferred;
        set
        {
            if (_autoFitTileRowHeightToPreferred == value) return;
            _autoFitTileRowHeightToPreferred = value;
            if (value) ResetTileRowHeightToPreferredIfNeeded();
            UpdateScrollbars();
            Invalidate();
        }
    }

    [Category("ViewGrid - Tile / Card CheckBox")]
    [DefaultValue(false)]
    [DisplayName("Kart Checkbox Göster")]
    [Description("Tile/Kart/Geniş Kart/Poster görünümünde kart üzerinde overlay checkbox gösterir.")]
    public bool TileCheckBoxes
    {
        get => _tileCheckBoxes;
        set
        {
            if (_tileCheckBoxes == value) return;
            _tileCheckBoxes = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - Tile / Card CheckBox")]
    [DefaultValue(ViewGridTileCheckBoxPosition.TopRight)]
    [DisplayName("Kart Checkbox Konumu")]
    [Description("Tile/Kart/Geniş Kart modlarında overlay checkbox konumu.")]
    public ViewGridTileCheckBoxPosition TileCheckBoxPosition
    {
        get => _tileCheckBoxPosition;
        set
        {
            if (_tileCheckBoxPosition == value) return;
            _tileCheckBoxPosition = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - Tile / Card CheckBox")]
    [DefaultValue(18)]
    [DisplayName("Kart Checkbox Boyutu")]
    [Description("Tile/Kart/Geniş Kart modlarında çizilecek checkbox boyutu.")]
    public int TileCheckBoxSize
    {
        get => _tileCheckBoxSize;
        set
        {
            int newValue = Math.Clamp(value, 12, 32);
            if (_tileCheckBoxSize == newValue) return;
            _tileCheckBoxSize = newValue;
            Invalidate();
        }
    }



    [Category("ViewGrid - Tile / Card CheckBox")]
    [DefaultValue(9)]
    [DisplayName("Kart Checkbox Kenar Boşluğu")]
    [Description("Tile/Kart/Geniş Kart modlarında checkbox'ın seçilen köşeden piksel cinsinden uzaklığı. Designer ve runtime tarafından değiştirilebilir.")]
    public int TileCheckBoxMargin
    {
        get => _tileCheckBoxMargin;
        set
        {
            int newValue = Math.Clamp(value, 0, 48);
            if (_tileCheckBoxMargin == newValue) return;
            _tileCheckBoxMargin = newValue;
            Invalidate();
        }
    }

    [Category("ViewGrid - Tile / Card CheckBox")]
    [DefaultValue("")]
    [DisplayName("Kart Checkbox AspectName")]
    [Description("Kart checkbox için kullanılacak CheckBox/CellCheckBox kolon AspectName değeri. Boş bırakılırsa ilk uygun checkbox kolonu kullanılır.")]
    public string TileCheckBoxAspectName
    {
        get => _tileCheckBoxAspectName;
        set
        {
            string newValue = value ?? string.Empty;
            if (string.Equals(_tileCheckBoxAspectName, newValue, StringComparison.Ordinal)) return;
            _tileCheckBoxAspectName = newValue;
            Invalidate();
        }
    }

    [Category("ViewGrid - Tile / Card CheckBox")]
    [DefaultValue(true)]
    [DisplayName("Kart Checkbox Metin Alanı Ayır")]
    [Description("Checkbox sol/sağ üst konumdayken başlık ve status dot ile çakışmaması için kart içinde otomatik metin alanı ayırır.")]
    public bool TileCheckBoxReserveTextArea
    {
        get => _tileCheckBoxReserveTextArea;
        set
        {
            if (_tileCheckBoxReserveTextArea == value) return;
            _tileCheckBoxReserveTextArea = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - Tile / Card CheckBox")]
    [DefaultValue(true)]
    [DisplayName("Kart Checkbox En Üste Çiz")]
    [Description("Badge, accent bar, status dot ve aksiyon ikonları çizildikten sonra checkbox'ı en üst katmanda tekrar çizerek kısmen görünmeme sorunlarını engeller.")]
    public bool TileCheckBoxDrawOnTop
    {
        get => _tileCheckBoxDrawOnTop;
        set
        {
            if (_tileCheckBoxDrawOnTop == value) return;
            _tileCheckBoxDrawOnTop = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - Tile / Card CheckBox")]
    [DefaultValue(true)]
    [DisplayName("Kart Checkbox Arka Planı")]
    [Description("Kart üzerinde checkbox okunurluğunu artıran yuvarlatılmış mini arka planı çizer.")]
    public bool TileCheckBoxShowBackground
    {
        get => _tileCheckBoxShowBackground;
        set
        {
            if (_tileCheckBoxShowBackground == value) return;
            _tileCheckBoxShowBackground = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - Tile / Card CheckBox")]
    [DefaultValue(4)]
    [DisplayName("Kart Checkbox Tıklama Payı")]
    [Description("Checkbox çevresinde tıklamayı kolaylaştıran ek hit-test alanı. Görsel boyutu değiştirmez.")]
    public int TileCheckBoxHitPadding
    {
        get => _tileCheckBoxHitPadding;
        set
        {
            int newValue = Math.Clamp(value, 0, 16);
            if (_tileCheckBoxHitPadding == newValue) return;
            _tileCheckBoxHitPadding = newValue;
        }
    }

    [Category("ViewGrid - Tile / Card CheckBox")]
    [DefaultValue(ViewGridTileCheckBoxVisibilityMode.Always)]
    [DisplayName("Kart Checkbox Görünürlük Modu")]
    [Description("Kart checkbox her zaman mı, sadece hover/selected iken mi, yoksa checked satırlarda kalıcı mı görünsün belirler.")]
    public ViewGridTileCheckBoxVisibilityMode TileCheckBoxVisibilityMode
    {
        get => _tileCheckBoxVisibilityMode;
        set
        {
            if (_tileCheckBoxVisibilityMode == value) return;
            _tileCheckBoxVisibilityMode = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(520)]
    [DisplayName("Geniş Kart Tercih Edilen Genişlik")]
    [Description("Geniş Kart görünümünde kartların tercih edilen genişliği. Kart modundan belirgin şekilde daha geniş kullanılabilir.")]
    public int LargeCardPreferredWidth
    {
        get => _largeCardPreferredWidth;
        set
        {
            int newValue = Math.Max(180, value);
            if (_largeCardPreferredWidth == newValue) return;
            _largeCardPreferredWidth = newValue;
            UpdateScrollbars();
            Invalidate();
        }
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(168)]
    [DisplayName("Geniş Kart Tercih Edilen Yükseklik")]
    [Description("Geniş Kart görünümünde kartların tercih edilen yüksekliği. Çok satırlı ticket/mesaj kartları için kullanılır.")]
    public int LargeCardPreferredHeight
    {
        get => _largeCardPreferredHeight;
        set
        {
            int newValue = Math.Max(72, value);
            if (_largeCardPreferredHeight == newValue) return;
            _largeCardPreferredHeight = newValue;
            if (ViewMode == ViewGridMode.LargeCard) ResetTileRowHeightToPreferredIfNeeded();
            UpdateScrollbars();
            Invalidate();
        }
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(8)]
    [DisplayName("Geniş Kart Maksimum Satır Sayısı")]
    [Description("Geniş Kart görünümünde başlık dışında çizilecek en fazla bilgi satırı. Designer üzerinden değiştirilebilir.")]
    public int LargeCardMaxTextLines
    {
        get => _largeCardMaxTextLines;
        set
        {
            int newValue = Math.Clamp(value, 1, 12);
            if (_largeCardMaxTextLines == newValue) return;
            _largeCardMaxTextLines = newValue;
            Invalidate();
        }
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(4)]
    [DisplayName("Kart Maksimum Satır Sayısı")]
    [Description("Kart/Tile görünümünde başlık dışında çizilecek en fazla bilgi satırı. Designer üzerinden değiştirilebilir.")]
    public int TileMaxTextLines
    {
        get => _tileMaxTextLines;
        set
        {
            int newValue = Math.Clamp(value, 1, 12);
            if (_tileMaxTextLines == newValue) return;
            _tileMaxTextLines = newValue;
            Invalidate();
        }
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(true)]
    public bool TileShowAllVisibleTextColumns
    {
        get => _tileShowAllVisibleTextColumns;
        set
        {
            if (_tileShowAllVisibleTextColumns == value) return;
            _tileShowAllVisibleTextColumns = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(false)]
    public bool TilePosterMode
    {
        get => _tilePosterMode;
        set
        {
            if (_tilePosterMode == value) return;
            _tilePosterMode = value;
            ResetTileRowHeightToPreferredIfNeeded();
            UpdateScrollbars();
            Invalidate();
        }
    }

    [Category("ViewGrid - View Modes")]
    [DefaultValue(132)]
    public int TilePosterImageHeight
    {
        get => _tilePosterImageHeight;
        set
        {
            int newValue = Math.Max(32, value);
            if (_tilePosterImageHeight == newValue) return;
            _tilePosterImageHeight = newValue;
            ResetTileRowHeightToPreferredIfNeeded();
            UpdateScrollbars();
            Invalidate();
        }
    }

    [Category("ViewGrid - Poster Mode")]
    [DefaultValue(true)]
    [Description("Poster görünümünde genişlik, yükseklik ve görsel alanını otomatik poster düzenine göre uygular.")]
    public bool PosterModeAutoLayout
    {
        get => _posterModeAutoLayout;
        set
        {
            if (_posterModeAutoLayout == value) return;
            _posterModeAutoLayout = value;
            if (ViewMode == ViewGridMode.Poster)
                ApplyPosterModeDefaults(false);
            Invalidate();
        }
    }

    [Category("ViewGrid - Poster Mode")]
    [DefaultValue(220)]
    [Description("Poster görünümünde önerilen kart genişliği.")]
    public int PosterPreferredWidth
    {
        get => _posterPreferredWidth;
        set
        {
            int newValue = Math.Clamp(value, 120, 800);
            if (_posterPreferredWidth == newValue) return;
            _posterPreferredWidth = newValue;
            if (ViewMode == ViewGridMode.Poster)
                ApplyPosterModeDefaults(false);
            Invalidate();
        }
    }

    [Category("ViewGrid - Poster Mode")]
    [DefaultValue(300)]
    [Description("Poster görünümünde önerilen kart yüksekliği.")]
    public int PosterPreferredHeight
    {
        get => _posterPreferredHeight;
        set
        {
            int newValue = Math.Clamp(value, 160, 1200);
            if (_posterPreferredHeight == newValue) return;
            _posterPreferredHeight = newValue;
            if (ViewMode == ViewGridMode.Poster)
                ApplyPosterModeDefaults(false);
            Invalidate();
        }
    }

    [Category("ViewGrid - Poster Mode")]
    [DefaultValue(176)]
    [Description("Poster görünümünde görsel alan yüksekliği.")]
    public int PosterImageHeight
    {
        get => _posterImageHeight;
        set
        {
            int newValue = Math.Clamp(value, 64, 900);
            if (_posterImageHeight == newValue) return;
            _posterImageHeight = newValue;
            if (ViewMode == ViewGridMode.Poster)
                ApplyPosterModeDefaults(false);
            Invalidate();
        }
    }



    [Category("ViewGrid - Poster Mode")]
    [DefaultValue(ViewGridMediaImageScaleMode.Contain)]
    [Description("Poster/MediaTile/FilmStrip görselinin alana nasıl yerleşeceğini belirler: Contain kırpmaz, Cover alanı doldurur, Stretch gerer.")]
    public ViewGridMediaImageScaleMode MediaImageScaleMode
    {
        get => _mediaImageScaleMode;
        set
        {
            if (_mediaImageScaleMode == value) return;
            _mediaImageScaleMode = value;
            Invalidate();
        }
    }

    [Category("ViewGrid - Poster Mode")]
    [DefaultValue(true)]
    [Description("Poster/MediaTile/FilmStrip görsellerinde yuvarlatılmış kırpma ve premium kart hissi uygular.")]
    public bool MediaImageRoundedCorners
    {
        get => _mediaImageRoundedCorners;
        set
        {
            if (_mediaImageRoundedCorners == value) return;
            _mediaImageRoundedCorners = value;
            Invalidate();
        }
    }

    public void ApplyPosterModeDefaults(bool refresh = true)
    {
        if (!PosterModeAutoLayout)
            return;

        TilePosterMode = true;
        TilePreferredWidth = PosterPreferredWidth;
        TilePreferredHeight = PosterPreferredHeight;
        TilePosterImageHeight = PosterImageHeight;
        TileMaxTextLines = Math.Max(TileMaxTextLines, 3);
        ShowHeader = false;
        CardViewReserveFilterArea = true;
        MoveFilterButtonToTopBar = true;
        ShowQuickFilterBar = true;
        ShowActiveFilterChips = true;
        ShowFloatingFilterButton = false;
        CardFilterUxPlacement = ViewGridCardFilterUxPlacement.TopBar;

        if (refresh)
        {
            ResetTileRowHeightToPreferredIfNeeded();
            RefreshCardViewFilterUx();
            RefreshView();
        }
    }
    [DefaultValue(true)] public bool ShowViewModeMenuItems { get; set; } = true;
    [Category("ViewGrid - Appearance"), DefaultValue(typeof(Color), "")] public Color CustomRowBackColor { get => _customBackColor; set { _customBackColor = value; Invalidate(); } }
    [Category("ViewGrid - Appearance"), DefaultValue(typeof(Color), "")] public Color CustomAlternateRowBackColor { get => _customAlternateBackColor; set { _customAlternateBackColor = value; Invalidate(); } }
    [Category("ViewGrid - Appearance"), DefaultValue(typeof(Color), "")] public Color CustomHotRowBackColor { get => _customHotBackColor; set { _customHotBackColor = value; Invalidate(); } }
    [Category("ViewGrid - Appearance"), DefaultValue(typeof(Color), "")] public Color CustomSelectionBackColor { get => _customSelectionBackColor; set { _customSelectionBackColor = value; Invalidate(); } }
    [Category("ViewGrid - Appearance"), DefaultValue(typeof(Color), "")] public Color CustomSelectionForeColor { get => _customSelectionForeColor; set { _customSelectionForeColor = value; Invalidate(); } }
    [Category("ViewGrid - Appearance"), DefaultValue(typeof(Color), "")] public Color CustomGroupBackColor { get => _customGroupBackColor; set { _customGroupBackColor = value; Invalidate(); } }
    [Category("ViewGrid - Appearance"), DefaultValue(typeof(Color), "")] public Color CustomGroupForeColor { get => _customGroupForeColor; set { _customGroupForeColor = value; Invalidate(); } }
    [Category("ViewGrid - Highlight"), DefaultValue(typeof(Color), "")] public Color SearchHighlightBackColor { get => _highlightBackColor; set { _highlightBackColor = value; Invalidate(); } }
    [Category("ViewGrid - Highlight"), DefaultValue(typeof(Color), "")] public Color SearchHighlightBorderColor { get => _highlightBorderColor; set { _highlightBorderColor = value; Invalidate(); } }
    [Category("ViewGrid - Highlight"), DefaultValue(typeof(Color), "")] public Color SearchHighlightForeColor { get => _highlightForeColor; set { _highlightForeColor = value; Invalidate(); } }
    [DefaultValue(true)] public bool UseGLVStyleHighlight { get; set; } = true;
    [Browsable(false)] public Func<object, Color?>? RowBackColorGetter { get; set; }
    [Browsable(false)] public Func<object, Color?>? RowForeColorGetter { get; set; }

    [Category("ViewGrid - Theme"), DefaultValue(true)]
    [Description("True ise tema/FormatRow/FormatCell/RowForeColorGetter kaynaklı düşük kontrastlı yazı renklerini arka plana göre otomatik okunur hale getirir. FastViewGridControl ve log ekranlarında açık temada beyaz yazı gibi sorunları engeller.")]
    public bool AutoEnsureReadableTextColors { get; set; } = true;

    [Category("ViewGrid - Theme"), DefaultValue(true)]
    [Description("v33 Theme Accessibility Engine. True ise tema paleti uygulanırken panel, textbox, combobox, buton, kart ve bilgi metinleri WCAG benzeri minimum kontrastla normalize edilir.")]
    public bool EnforceThemeAccessibility { get; set; } = true;

    [Category("ViewGrid - Theme"), DefaultValue(true)]
    [Description("True ise ViewGridControl/FastViewGridControl/DataListView/TreeViewGridControl kolon başlığı renkleri aynı tema paletinden çizilir. Eski designer veya ViewGrid uyumluluğundan gelen koyu/açık HeaderBackColor farklarını normalize eder.")]
    public bool AutoApplyThemeToColumnHeaders { get; set; } = true;

    [Category("ViewGrid - Theme"), DefaultValue(true)]
    [Description("True ise header, filter/sort glyph, checkbox, grid çizgisi ve satır renkleri tema değiştiğinde tüm ViewGrid türevlerinde aynı görsel profile çekilir.")]
    public bool UseUnifiedThemeVisuals { get; set; } = true;

    [Category("ViewGrid - Theme"), DefaultValue(true)]
    [Description("Built-in, ViewGrid uyumluluk ve kullanıcı tarafından verilen ContextMenuStrip menülerini ViewGrid teması ile otomatik boyar.")]
    public bool AutoApplyThemeToContextMenus { get; set; } = true;
    [DefaultValue(false)] public bool EnableRowDetails { get; set; }
    [Browsable(false)] public IList<ViewGridConditionalFormat> ConditionalFormats => _conditionalFormats;
    [Browsable(false)] public IList<ViewGridSummaryItem> Summaries => _summaries;
    [Browsable(false)] public ViewGridUndoService UndoService => _undo;
    [Browsable(false)] public int SelectedIndex => _selectedRow;
    [Browsable(false)] public IReadOnlyList<int> SelectedIndices => _selectedRows.ToList();
    [Browsable(false)]
    public object? SelectedObject
    {
        get => GetViewRow(_selectedRow);
        set
        {
            if (value == null) { ClearSelection(); return; }
            SelectObject(value);
        }
    }
    [Browsable(false)] public IReadOnlyList<object> SelectedObjects => _selectedRows.Select(i => GetViewRow(i)).Where(o => o != null).Cast<object>().ToList();

    /// <summary>ViewGrid migration alias.</summary>
    [Browsable(false)] public object? SelectedItem { get => SelectedObject; set => SelectedObject = value; }

    /// <summary>ViewGrid migration alias.</summary>
    [Browsable(false)] public IReadOnlyList<object> SelectedItems => SelectedObjects;
    [Browsable(false)] public IReadOnlyList<int> CheckedIndices => GetCheckedIndices().ToList();
    [Browsable(false)] public IReadOnlyList<object> CheckedObjects => GetCheckedObjects().ToList();
    [Category("ViewGrid - Interaction"), DefaultValue(true)]
    [Description("Button kolonları mouse ile yalnızca sol tuşla çalışır. Sağ tuş satır/menü işlemi için ayrılır.")]
    public bool ButtonClickOnlyWithLeftMouseButton { get; set; } = true;
    [Category("ViewGrid - Keyboard"), DefaultValue(true)]
    [Description("Seçili hücre Button kolonu ise Enter ButtonClick olayını tetikler. Space, checkbox varsa öncelikle checkbox toggle yapar.")]
    public bool KeyboardActivatesButtonCells { get; set; } = true;

    [Category("ViewGrid - Keyboard")]
    [DefaultValue(true)]
    [Description("CheckBoxes=true veya aktif checkbox kolonu varsa Space tuşu seçili satırların checkbox durumunu işaretler/kaldırır.")]
    public bool KeyboardSpaceTogglesCheckBoxes { get; set; } = true;

    [Category("ViewGrid - Keyboard")]
    [DefaultValue(true)]
    [Description("Birden fazla satır seçiliyken Space tüm seçili satırların checkbox durumunu birlikte değiştirir.")]
    public bool KeyboardSpaceTogglesSelectedRows { get; set; } = true;
    [Browsable(false)] public ViewGridFilterSet Filters => _filters;

    /// <summary>
    /// GLV model bazlı özel filtre.
    /// Örn: grid.ModelFilter = row => ((MyRow)row).State == "OK";
    /// </summary>
    [Browsable(false)]
    public Func<object, bool>? ModelFilter
    {
        get => _modelFilter;
        set { _modelFilter = value; _modelFilterPredicate = value == null ? null : new Predicate<object>(value); BuildViewIndex(); }
    }

    /// <summary>Predicate tabanlı ViewGrid uyumlu filtre alias'ı.</summary>
    [Browsable(false)]
    public Predicate<object>? ModelFilterPredicate
    {
        get => _modelFilterPredicate;
        set { _modelFilterPredicate = value; _modelFilter = value == null ? null : new Func<object, bool>(value.Invoke); BuildViewIndex(); }
    }
    public void SetRowDetailsProvider(ViewGridRowDetailsProvider? provider) => _detailsProvider = provider;
    public void ClearRowDetailsProvider() => _detailsProvider = null;

    public PrintDocument CreatePrintDocument(string? title = null)
    {
        var state = new ViewGridPrintState { Title = string.IsNullOrWhiteSpace(title) ? PrintTitle : title! };
        var doc = new PrintDocument { DocumentName = state.Title };
        // v24.56: Excel benzeri okunaklı çıktı için varsayılan kenar boşluklarını biraz azalttık.
        doc.DefaultPageSettings.Margins = new Margins(45, 45, 42, 42);
        doc.BeginPrint += (_, __) => PreparePrintState(state);
        doc.PrintPage += (_, e) => PrintPage(e, state);
        return doc;
    }

    public void Print(string? title = null)
    {
        using var doc = CreatePrintDocument(title);
        using var dialog = new PrintDialog { Document = doc, UseEXDialog = true };
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            doc.Print();
    }

    public void ShowPrintPreview(string? title = null)
    {
        using var doc = CreatePrintDocument(title);
        using var preview = new PrintPreviewDialog
        {
            Document = doc,
            Width = 1100,
            Height = 760,
            StartPosition = FormStartPosition.CenterParent,
            Text = string.IsNullOrWhiteSpace(title) ? PrintTitle + " - " + ViewGridText.PrintPreviewSuffix : title + " - " + ViewGridText.PrintPreviewSuffix,
            UseAntiAlias = true
        };
        preview.Shown += (_, __) =>
        {
            foreach (Control c in preview.Controls)
            {
                if (c is PrintPreviewControl pvc)
                {
                    pvc.AutoZoom = true;
                    break;
                }
            }
        };
        preview.ShowDialog(FindForm());
    }

    public void ShowPageSetup(string? title = null)
    {
        using var doc = CreatePrintDocument(title);
        using var setup = new PageSetupDialog { Document = doc };
        setup.ShowDialog(FindForm());
    }

    private sealed class ViewGridPrintState
    {
        public string Title = "ViewGridControl";
        public int RowIndex;
        public List<object> Rows = new();
        public List<ViewGridColumn> Columns = new();
        public int PageNo;
    }

    private void PreparePrintState(ViewGridPrintState state)
    {
        state.RowIndex = 0;
        state.PageNo = 0;
        state.Columns = Columns.Where(c => !c.PrivateColumn && (!PrintOnlyVisibleColumns || c.Visible)).ToList();
        var rows = new List<object>();
        if (PrintSelectedRowsOnly && _selectedRows.Count > 0)
        {
            foreach (int index in _selectedRows.OrderBy(i => i))
            {
                var row = GetViewRow(index);
                if (row != null) rows.Add(row);
            }
        }
        else
        {
            for (int i = 0; i < ViewCount; i++)
            {
                var row = GetViewRow(i);
                if (row != null) rows.Add(row);
                if (PrintMaxRows > 0 && rows.Count >= PrintMaxRows) break;
            }
        }
        state.Rows = rows;
    }

    private void PrintPage(PrintPageEventArgs e, ViewGridPrintState state)
    {
        state.PageNo++;
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // IMPORTANT: PrintDocument gives MarginBounds/PageBounds in 1/100 inch units.
        // Do not switch PageUnit or apply ScaleTransform here; doing so causes tiny/offset preview output.
        Rectangle bounds = e.MarginBounds;

        // v24.56 Print Polish: sayfa alanını daha verimli kullan, fakat fiziksel yazıcı sınırlarını aşma.
        bounds = Rectangle.Inflate(bounds, 10, 8);
        bounds.Intersect(Rectangle.Inflate(e.PageBounds, -28, -28));

        int y = bounds.Top;
        int rowHeight = Math.Max(26, Math.Min(36, (int)Math.Ceiling(Font.GetHeight(g) + 15)));
        int headerHeight = PrintShowHeader ? Math.Max(30, rowHeight + 3) : 0;
        int titleHeight = 46;
        int footerHeight = 28;

        using var titleFont = new Font("Segoe UI", Math.Max(13f, Font.Size + 5f), FontStyle.Bold, GraphicsUnit.Point);
        using var headerFont = new Font("Segoe UI", Math.Max(9.4f, Font.Size + 0.7f), FontStyle.Bold, GraphicsUnit.Point);
        using var printFont = new Font("Segoe UI", Math.Max(9.2f, Font.Size + 0.45f), FontStyle.Regular, GraphicsUnit.Point);
        using var smallFont = new Font("Segoe UI", 8.3f, FontStyle.Regular, GraphicsUnit.Point);
        using var footerFont = new Font("Segoe UI", 8.6f, FontStyle.Regular, GraphicsUnit.Point);

        bool readable = PrintPreferReadableColors || !PrintUseThemeColors;
        Color pageBack = Color.White;
        Color fore = readable ? Color.FromArgb(28, 32, 38) : BestTextOn(_theme.BackColor, _theme.ForeColor);
        Color muted = readable ? Color.FromArgb(92, 99, 110) : _theme.MutedForeColor;
        Color headerBack = readable ? Color.FromArgb(231, 235, 241) : _theme.TextBackColor;
        Color headerFore = readable ? Color.FromArgb(22, 26, 32) : BestTextOn(headerBack, _theme.TextForeColor);
        Color rowBack = readable ? Color.White : _theme.BackColor;
        Color altBack = readable ? Color.FromArgb(248, 250, 253) : GetAlternateRowBack(rowBack);
        Color grid = readable ? Color.FromArgb(188, 196, 207) : EnsureVisibleStroke(_theme.BorderColor, rowBack);
        Color accent = readable
            ? EnsureVisibleAccent(_theme.AccentColor == Color.Empty ? Color.FromArgb(48, 121, 232) : _theme.AccentColor, rowBack)
            : EnsureVisibleAccent(_theme.AccentColor, rowBack);

        using (var pageBrush = new SolidBrush(pageBack))
            g.FillRectangle(pageBrush, e.PageBounds);

        using var titleBrush = new SolidBrush(fore);
        using var mutedBrush = new SolidBrush(muted);
        using var headerTextBrush = new SolidBrush(headerFore);
        using var textBrush = new SolidBrush(fore);
        using var gridPen = new Pen(grid, 1f);

        var leftFormat = new StringFormat(StringFormatFlags.NoWrap)
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        var centerFormat = new StringFormat(StringFormatFlags.NoWrap)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        var rightFormat = new StringFormat(StringFormatFlags.NoWrap)
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };

        g.DrawString(state.Title, titleFont, titleBrush, new RectangleF(bounds.Left, y, bounds.Width, 27), leftFormat);
        string sub = $"{DateTime.Now:g}   •   Sayfa {state.PageNo}   •   Kayıt: {state.Rows.Count}";
        g.DrawString(sub, smallFont, mutedBrush, new RectangleF(bounds.Left, y + 27, bounds.Width, 18), leftFormat);
        y += titleHeight;

        var colRects = CalculatePrintColumnRects(state.Columns, bounds.Left, bounds.Width, PrintFitToPageWidth);
        int tableRight = colRects.Count == 0 ? bounds.Left : colRects[^1].Right;
        int tableWidth = Math.Max(1, tableRight - bounds.Left);

        if (PrintShowHeader && state.Columns.Count > 0)
        {
            using var hb = new SolidBrush(headerBack);
            g.FillRectangle(hb, bounds.Left, y, tableWidth, headerHeight);
            if (PrintShowGrid) g.DrawRectangle(gridPen, bounds.Left, y, tableWidth, headerHeight);

            for (int i = 0; i < state.Columns.Count; i++)
            {
                var cell = colRects[i];
                var r = new RectangleF(cell.X + 5, y + 1, Math.Max(0, cell.Width - 10), headerHeight - 2);
                g.DrawString(state.Columns[i].Header, headerFont, headerTextBrush, r, leftFormat);
                if (PrintShowGrid) g.DrawLine(gridPen, cell.Right, y, cell.Right, y + headerHeight);
            }
            y += headerHeight;
        }

        while (state.RowIndex < state.Rows.Count && y + rowHeight + footerHeight <= bounds.Bottom)
        {
            object row = state.Rows[state.RowIndex];
            Color back = PrintZebraRows && state.RowIndex % 2 == 1 ? altBack : rowBack;
            using (var rb = new SolidBrush(back)) g.FillRectangle(rb, bounds.Left, y, tableWidth, rowHeight);

            for (int i = 0; i < state.Columns.Count; i++)
            {
                var col = state.Columns[i];
                var cell = colRects[i];
                var r = new Rectangle(cell.X + 5, y + 3, Math.Max(0, cell.Width - 10), rowHeight - 6);
                object? value = col.GetValue(row);

                if (PrintRenderProgressBars && col.Kind == ViewGridColumnKind.ProgressBar)
                {
                    DrawPrintProgress(g, r, ToInt(value), fore, back, grid, accent, printFont);
                }
                else
                {
                    string text = col.Kind == ViewGridColumnKind.ProgressBar ? ToInt(value) + "%" : Convert.ToString(value) ?? string.Empty;
                    StringFormat fmt = col.TextAlign == ContentAlignment.MiddleRight ? rightFormat :
                                       col.TextAlign == ContentAlignment.MiddleCenter ? centerFormat : leftFormat;
                    g.DrawString(text, printFont, textBrush, new RectangleF(r.X, r.Y, r.Width, r.Height), fmt);
                }

                if (PrintShowGrid) g.DrawLine(gridPen, cell.Right, y, cell.Right, y + rowHeight);
            }

            if (PrintShowGrid) g.DrawLine(gridPen, bounds.Left, y + rowHeight, tableRight, y + rowHeight);
            y += rowHeight;
            state.RowIndex++;
        }

        string footer = $"ViewGridControl {ViewGridVersionInfo.Version}  •  Sayfa {state.PageNo}";
        g.DrawString(footer, footerFont, mutedBrush, new RectangleF(bounds.Left, bounds.Bottom - footerHeight + 5, bounds.Width, footerHeight), rightFormat);
        e.HasMorePages = state.RowIndex < state.Rows.Count;
    }

    private void DrawPrintProgress(Graphics g, Rectangle r, int value, Color fore, Color cellBack, Color grid, Color accent, Font font)
    {
        value = Math.Clamp(value, 0, 100);
        int h = Math.Max(14, Math.Min(18, r.Height - 3));
        var bar = new Rectangle(r.Left, r.Top + Math.Max(0, (r.Height - h) / 2), Math.Max(10, r.Width), h);
        int radius = Math.Max(4, h / 2);
        Color track = Color.FromArgb(238, 242, 247);
        Color fill = value < ProgressBarLowThreshold
            ? Color.FromArgb(220, 70, 70)
            : value >= ProgressBarHighThreshold
                ? Color.FromArgb(34, 164, 93)
                : accent;

        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var b = new SolidBrush(track)) g.FillRoundedRectangle(b, bar, radius);
        using (var p = new Pen(grid)) g.DrawRoundedRectangle(p, bar, radius);
        int fw = Math.Max(0, (bar.Width - 2) * value / 100);
        if (fw > 0)
        {
            var fr = new Rectangle(bar.Left + 1, bar.Top + 1, fw, Math.Max(1, bar.Height - 2));
            using var fb = new LinearGradientBrush(fr, Blend(fill, Color.White, 0.22), fill, LinearGradientMode.Vertical);
            g.FillRoundedRectangle(fb, fr, Math.Max(3, radius - 1));
        }
        g.SmoothingMode = old;

        using var textBrush = new SolidBrush(value >= 45 ? BestTextOn(fill, Color.White) : fore);
        using var progressFont = new Font(font.FontFamily, Math.Max(8.8f, font.Size), FontStyle.Bold, GraphicsUnit.Point);
        using var sf = new StringFormat(StringFormatFlags.NoWrap)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        g.DrawString(value + "%", progressFont, textBrush, new RectangleF(bar.X, bar.Y - 0.5f, bar.Width, bar.Height + 1), sf);
    }

    private static List<Rectangle> CalculatePrintColumnRects(IReadOnlyList<ViewGridColumn> columns, int left, int width, bool fitToPageWidth)
    {
        var rects = new List<Rectangle>();
        if (columns.Count == 0) return rects;

        // v24.56 Print Polish:
        // - CheckBox/boolean kolonlarını çok dar bırakma.
        // - FitToPageWidth açıkken artan pikselleri küçük kolonlara yığmadan orantılı dağıt.
        // - Son kolonda yuvarlama kaynaklı boşluk/taşma bırakma.
        int[] minWidths = new int[columns.Count];
        int[] baseWidths = new int[columns.Count];

        for (int i = 0; i < columns.Count; i++)
        {
            var c = columns[i];
            bool checkLike = c.Kind == ViewGridColumnKind.CheckBox;
            minWidths[i] = checkLike ? 38 : c.Kind == ViewGridColumnKind.ProgressBar ? 74 : 46;
            baseWidths[i] = Math.Max(minWidths[i], c.Width);
        }

        int total = baseWidths.Sum();
        double scale = fitToPageWidth || total > width ? width / (double)Math.Max(1, total) : 1.0;

        int x = left;
        int remainingWidth = width;
        int remainingBase = total;

        for (int i = 0; i < columns.Count; i++)
        {
            int w;
            if (i == columns.Count - 1 && (fitToPageWidth || total > width))
            {
                w = remainingWidth;
            }
            else if (fitToPageWidth || total > width)
            {
                // Kalan alana göre yeniden oranla; yuvarlama hataları birikmez.
                w = (int)Math.Round(remainingWidth * (baseWidths[i] / (double)Math.Max(1, remainingBase)));
            }
            else
            {
                w = baseWidths[i];
            }

            w = Math.Max(minWidths[i], w);

            // Çok kolon varsa son hücrelerin taşmasını engelle.
            if (fitToPageWidth || total > width)
            {
                int columnsLeft = columns.Count - i - 1;
                int minForRest = 0;
                for (int j = i + 1; j < columns.Count; j++) minForRest += minWidths[j];
                w = Math.Min(w, Math.Max(minWidths[i], remainingWidth - minForRest));
            }

            rects.Add(new Rectangle(x, 0, Math.Max(20, w), 1));
            x += w;
            remainingWidth -= w;
            remainingBase -= baseWidths[i];
        }

        return rects;
    }

    private Color GetAlternateRowBack(Color rowBack)
        => CustomAlternateRowBackColor != Color.Empty ? CustomAlternateRowBackColor : Blend(rowBack, _theme.AccentColor, _theme.IsDark ? 0.055 : 0.035);

    public void ClearSelection()
    {
        _selectedRows.Clear();
        _selectedRow = -1;
        _selectionAnchorRow = -1;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void InvertSelection()
    {
        var next = new SortedSet<int>();
        for (int i = 0; i < ViewCount; i++)
            if (!IsGroupRow(i) && !_selectedRows.Contains(i) && i != _selectedRow) next.Add(i);
        _selectedRows.Clear();
        foreach (var i in next) _selectedRows.Add(i);
        _selectedRow = _selectedRows.Count > 0 ? _selectedRows.Min : -1;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void EnsureVisible(int viewRowIndex)
    {
        if (viewRowIndex < 0 || viewRowIndex >= ViewCount) return;
        int visualIndex = IsTileView ? viewRowIndex / Math.Max(1, GetTilesPerRow()) : viewRowIndex;
        int visibleBands = Math.Max(1, (Height - (ShowHeader ? HeaderHeight : 0) - (ShowSummaryFooter ? FooterHeight : 0)) / Math.Max(1, RowHeight));
        if (visualIndex < _scrollY) ScrollVertical(visualIndex);
        else if (visualIndex >= _scrollY + visibleBands) ScrollVertical(visualIndex - visibleBands + 1);
    }

    public object? GetModelObject(int viewRowIndex) => GetViewRow(viewRowIndex);

    public int IndexOfObject(object? model)
    {
        if (model == null) return -1;
        var comparer = EqualityComparer<object>.Default;
        for (int i = 0; i < ViewCount; i++)
        {
            var row = GetViewRow(i);
            if (ReferenceEquals(row, model) || (row != null && comparer.Equals(row, model)))
                return i;
        }
        return -1;
    }

    public void SelectObject(object? model, bool addToSelection = false)
    {
        int index = IndexOfObject(model);
        if (index < 0) return;
        if (!addToSelection || !MultiSelect)
        {
            SelectRow(index);
            return;
        }
        _selectedRows.Add(index);
        _selectedRow = index;
        _selectionAnchorRow = index;
        EnsureRowVisible(index);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void SelectObjects(IEnumerable objects)
    {
        if (objects == null) return;
        _selectedRows.Clear();
        int last = -1;
        foreach (var obj in objects)
        {
            int index = IndexOfObject(obj);
            if (index >= 0) { _selectedRows.Add(index); last = index; if (!MultiSelect) break; }
        }
        _selectedRow = last;
        _selectionAnchorRow = last;
        if (last >= 0) EnsureRowVisible(last);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void DeselectObject(object? model)
    {
        int index = IndexOfObject(model);
        if (index < 0 || !_selectedRows.Remove(index)) return;
        _selectedRow = _selectedRows.Count > 0 ? _selectedRows.Last() : -1;
        _selectionAnchorRow = _selectedRow;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void RefreshObject(object? model)
    {
        int index = IndexOfObject(model);
        if (index >= 0) Invalidate();
    }

    public void RefreshObjects(IEnumerable objects)
    {
        if (objects == null) return;
        foreach (var obj in objects) RefreshObject(obj);
    }

    public void CheckObject(object? model) => SetObjectChecked(model, true);
    public void UncheckObject(object? model) => SetObjectChecked(model, false);
    public void CheckObjects(IEnumerable objects) { if (objects == null) return; foreach (var obj in objects) CheckObject(obj); }
    public void UncheckObjects(IEnumerable objects) { if (objects == null) return; foreach (var obj in objects) UncheckObject(obj); }
    public bool IsChecked(object? model)
    {
        var col = GetActiveCheckBoxColumn();
        if (model == null || col == null) return false;
        return GetRowCheckState(model, col) == CheckState.Checked;
    }

    private void SetObjectChecked(object? model, bool value)
    {
        var col = GetActiveCheckBoxColumn();
        if (model == null || col == null) return;
        int index = IndexOfObject(model);
        var oldState = GetRowCheckState(model, col);
        var newState = value ? CheckState.Checked : CheckState.Unchecked;
        SetRowCheckState(model, col, newState);
        if (oldState != newState) OnObjectCheckStateChanged(model, index, newState);
        UpdateCompatibilityHeaderCheckState(col);
        if (index >= 0) Invalidate();
    }

    private ViewGridColumn? GetActiveCheckBoxColumn()
    {
        if (_checkBoxes)
            return GetCompatibilityCheckBoxHostColumn();
        return Columns.FirstOrDefault(x => x.Kind == ViewGridColumnKind.CheckBox || x.CellCheckBox);
    }

    private static bool UsesInternalCheckStore(ViewGridColumn col)
    {
        bool hasExplicitCheckBinding = !string.IsNullOrWhiteSpace(col.CheckBoxAspectName)
            || col.CheckStateGetter != null
            || col.BooleanCheckStateGetter != null
            || col.CheckStatePutter != null
            || col.BooleanCheckStatePutter != null;

        if (col.Kind == ViewGridColumnKind.CheckBox)
            return string.IsNullOrWhiteSpace(col.AspectName) && !hasExplicitCheckBinding;

        // CellCheckBox is a text/value column with an extra checkbox glyph.
        // If the checkbox state is not explicitly bound with CheckBoxAspectName/getter/putter,
        // keep the check state in ViewGrid's internal store instead of trying to convert the
        // visible cell text (for example Notes) to bool or overwriting it with true/false.
        if (col.CellCheckBox)
            return !hasExplicitCheckBinding;

        return false;
    }

    private HashSet<object> GetInternalCheckRows(ViewGridColumn col)
    {
        if (!_internalCheckRowsByColumn.TryGetValue(col, out var rows))
        {
            rows = new HashSet<object>();
            _internalCheckRowsByColumn[col] = rows;
        }

        return rows;
    }

    private CheckState GetRowCheckState(object row, ViewGridColumn col)
    {
        if (row == null || col == null) return CheckState.Unchecked;

        if (IsCompatibilityCheckBoxHostColumn(col))
        {
            if (string.IsNullOrWhiteSpace(_checkedAspectName))
                return _checkedRows.Contains(row) ? CheckState.Checked : CheckState.Unchecked;

            var value = GetCompatibilityCheckAspectValue(row);
            if (value is CheckState checkState) return checkState;
            if (value is bool boolValue) return boolValue ? CheckState.Checked : CheckState.Unchecked;
            if (value is int intValue) return intValue != 0 ? CheckState.Checked : CheckState.Unchecked;
            if (value is string stringValue && bool.TryParse(stringValue, out var parsed)) return parsed ? CheckState.Checked : CheckState.Unchecked;
            return CheckState.Unchecked;
        }

        if (UsesInternalCheckStore(col))
            return GetInternalCheckRows(col).Contains(row) ? CheckState.Checked : CheckState.Unchecked;
        return col.GetCheckState(row);
    }

    private void SetRowCheckState(object row, ViewGridColumn col, CheckState state)
    {
        if (row == null || col == null || col.ReadOnly) return;

        if (IsCompatibilityCheckBoxHostColumn(col))
        {
            bool isChecked = state == CheckState.Checked;
            if (string.IsNullOrWhiteSpace(_checkedAspectName))
            {
                if (isChecked) _checkedRows.Add(row);
                else _checkedRows.Remove(row);
            }
            else
            {
                SetCompatibilityCheckAspectValue(row, isChecked);
            }
            return;
        }

        if (UsesInternalCheckStore(col))
        {
            var rows = GetInternalCheckRows(col);
            if (state == CheckState.Checked) rows.Add(row);
            else rows.Remove(row);
            return;
        }
        col.PutCheckState(row, state);
    }

    public void ScrollToTop() => ScrollVertical(0);
    public void ScrollToBottom() => ScrollVertical(Math.Max(0, _vbar.Maximum));

    public void AutoResizeColumnsToContent(int maxRows = -1, int padding = 24)
    {
        AutoResizeAllColumnsToContent(maxRows, padding);
    }

    public void AutoResizeAllColumnsToContent(int maxRows = -1, int padding = 24)
    {
        foreach (var c in Columns.VisibleColumns.ToList())
            AutoResizeColumnToContent(c, maxRows, padding, refresh: false);
        RefreshView();
        OnColumnLayoutChanged();
    }

    public void AutoResizeColumnToContent(ViewGridColumn col, int maxRows = -1, int padding = 24)
    {
        AutoResizeColumnToContent(col, maxRows, padding, refresh: true);
    }

    private void AutoResizeColumnToContent(ViewGridColumn col, int maxRows, int padding, bool refresh)
    {
        if (col == null || !Columns.Contains(col)) return;
        int sampleRows = maxRows > 0 ? maxRows : Math.Max(1, AutoResizeSampleRows);
        int maxWidth = Math.Max(80, AutoResizeMaxWidth);
        int minWidth = Math.Max(40, col.MinimumWidth > 0 ? col.MinimumWidth : 40);
        int imagePadding = IncludeCellImagesInAutoResizeWidth && ColumnMayDrawCellImage(col)
            ? Math.Max(0, CellImageTextPadding)
            : 0;
        int width = AutoResizeIncludeHeader ? TextRenderer.MeasureText(col.Header ?? string.Empty, Font).Width + padding : minWidth;
        int rows = Math.Min(ViewCount, sampleRows);

        for (int i = 0; i < rows; i++)
        {
            if (IsGroupRow(i)) continue;
            var row = GetViewRow(i);
            if (row == null) continue;
            string text = Convert.ToString(col.GetValue(row)) ?? string.Empty;
            if (text.Length == 0) continue;
            width = Math.Max(width, TextRenderer.MeasureText(text, Font).Width + padding + imagePadding);
            if (width >= maxWidth) { width = maxWidth; break; }
        }

        col.Width = Math.Clamp(width, minWidth, maxWidth);
        if (refresh)
        {
            RefreshView();
            OnColumnLayoutChanged();
        }
    }


    public event EventHandler? ColumnLayoutChanged;

    private void OnColumnLayoutChanged()
    {
        if (AutoSaveColumnLayout) SaveColumnLayout();
        QueueAutoSaveUserLayout();
        ColumnLayoutChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void MoveColumn(ViewGridColumn column, int targetVisibleIndex)
    {
        if (column == null || !Columns.Contains(column)) return;

        var visible = Columns.VisibleColumns.ToList();
        if (visible.Count <= 1) return;

        targetVisibleIndex = Math.Max(0, Math.Min(targetVisibleIndex, visible.Count - 1));
        int currentVisibleIndex = visible.IndexOf(column);
        if (currentVisibleIndex < 0 || currentVisibleIndex == targetVisibleIndex) return;

        var ordered = Columns.ToList();
        ordered.Remove(column);

        var visibleAfterRemove = ordered.Where(c => !c.PrivateColumn && c.Visible).ToList();
        int insertIndex;
        if (targetVisibleIndex >= visibleAfterRemove.Count)
            insertIndex = ordered.Count;
        else
            insertIndex = ordered.IndexOf(visibleAfterRemove[targetVisibleIndex]);

        if (insertIndex < 0) insertIndex = ordered.Count;
        ordered.Insert(insertIndex, column);

        Columns.Clear();
        foreach (var c in ordered) Columns.Add(c);

        RefreshView();
        OnColumnLayoutChanged();
    }

    public string GetColumnLayoutFilePath()
    {
        string key = string.IsNullOrWhiteSpace(ColumnLayoutStorageKey) ? Name : ColumnLayoutStorageKey!;
        if (string.IsNullOrWhiteSpace(key)) key = GetType().FullName ?? "ViewGridControl";
        foreach (var ch in Path.GetInvalidFileNameChars()) key = key.Replace(ch, '_');
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ViewGridControl", "Layouts");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, key + ".json");
    }

    public ViewGridLayoutState CaptureColumnLayout()
    {
        var state = ViewGridLayoutState.Capture(Columns, _filters.GlobalText, _sortColumn?.AspectName, _sortDesc);
        state.GroupByAspectName = _groupByAspectName;
        state.EnableGrouping = EnableGrouping;
        state.FrozenColumnCount = FrozenColumnCount;
        return state;
    }

    public void ApplyColumnLayout(ViewGridLayoutState state)
    {
        if (state == null) return;
        state.Apply(Columns);
        _sortColumn = !string.IsNullOrWhiteSpace(state.SortAspectName) ? Columns[state.SortAspectName!] : null;
        _sortDesc = state.SortDescending;
        _filters.GlobalText = state.GlobalFilter ?? string.Empty;
        _groupByAspectName = state.GroupByAspectName;
        EnableGrouping = state.EnableGrouping && !string.IsNullOrWhiteSpace(_groupByAspectName);
        FrozenColumnCount = Math.Max(0, state.FrozenColumnCount);
        BuildViewIndex();
        ColumnLayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SaveColumnLayout()
    {
        try
        {
            File.WriteAllText(GetColumnLayoutFilePath(), CaptureColumnLayout().ToJson());
        }
        catch
        {
            // Layout persistence must never break the control at runtime/designer-time.
        }
    }

    public bool LoadColumnLayout()
    {
        try
        {
            string file = GetColumnLayoutFilePath();
            if (!File.Exists(file)) return false;
            ApplyColumnLayout(ViewGridLayoutState.FromJson(File.ReadAllText(file)));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void ResetColumnLayout()
    {
        foreach (var c in Columns)
        {
            c.Visible = c.DefaultVisible;
            c.Width = Math.Max(0, c.DefaultWidth);
        }
        try
        {
            string file = GetColumnLayoutFilePath();
            if (File.Exists(file)) File.Delete(file);
        }
        catch { }
        _sortColumn = null;
        _sortDesc = false;
        RefreshView();
        ColumnLayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetColumnLayoutProfileFilePath(string profileName)
    {
        string safeProfile = MakeSafeFileName(string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName.Trim());
        string key = string.IsNullOrWhiteSpace(ColumnLayoutStorageKey) ? Name : ColumnLayoutStorageKey!;
        if (string.IsNullOrWhiteSpace(key)) key = GetType().Name;
        key = MakeSafeFileName(key);
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ViewGridControl", "Layouts", key);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, safeProfile + ".json");
    }

    public void SaveColumnLayoutProfile(string profileName)
    {
        try { File.WriteAllText(GetColumnLayoutProfileFilePath(profileName), CaptureColumnLayout().ToJson()); }
        catch { }
    }

    public bool LoadColumnLayoutProfile(string profileName)
    {
        try
        {
            string file = GetColumnLayoutProfileFilePath(profileName);
            if (!File.Exists(file)) return false;
            ApplyColumnLayout(ViewGridLayoutState.FromJson(File.ReadAllText(file)));
            return true;
        }
        catch { return false; }
    }

    public bool DeleteColumnLayoutProfile(string profileName)
    {
        try
        {
            string file = GetColumnLayoutProfileFilePath(profileName);
            if (!File.Exists(file)) return false;
            File.Delete(file);
            return true;
        }
        catch { return false; }
    }

    public IReadOnlyList<string> GetColumnLayoutProfileNames()
    {
        try
        {
            string dir = Path.GetDirectoryName(GetColumnLayoutProfileFilePath("Default")) ?? string.Empty;
            if (!Directory.Exists(dir)) return Array.Empty<string>();
            return Directory.GetFiles(dir, "*.json").Select(Path.GetFileNameWithoutExtension).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().OrderBy(x => x).ToList();
        }
        catch { return Array.Empty<string>(); }
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (char ch in Path.GetInvalidFileNameChars()) value = value.Replace(ch, '_');
        return value;
    }

    public event EventHandler<ViewGridCellClickEventArgs>? CellClick;
    public event EventHandler<ViewGridCellClickEventArgs>? ButtonClick;
    public event EventHandler<ViewGridCellClickEventArgs>? HyperlinkClick;
    public event EventHandler<ViewGridCellClickEventArgs>? ItemActivate;
    public event EventHandler<ViewGridCellEditEventArgs>? CellValueChanged;
    public event EventHandler? SelectionChanged;

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ViewGridTheme Theme
    {
        get => _theme;
        set => ApplyTheme(value ?? WindowsThemeService.CurrentTheme());
    }

    public void ApplyTheme(ViewGridTheme theme)
    {
        _applyingTheme = true;
        try
        {
            theme = EnforceThemeAccessibility
                ? global::ViewGrid.Theming.ViewGridThemeAccessibility.Normalize(theme)
                : theme;
            _theme = theme;
            BackColor = theme.BackColor;
            ForeColor = theme.ForeColor;
            _renderOptions.EnableFluentBackdrop = theme.UseFluentBackdrop;
            _renderOptions.EnableAcrylicSimulation = theme.UseAcrylicEffect;
            _renderOptions.EnableAnimatedSelection = theme.UseAnimatedSelection;
            ApplyThemeToAllAttachedContextMenus();
        }
        finally
        {
            _applyingTheme = false;
        }
        Invalidate();
    }

    public void ApplyViewGridThemeToMenu(ContextMenuStrip? menu)
    {
        if (!AutoApplyThemeToContextMenus) return;
        if (IsInDesignMode && EnableDesignTimeThemeSync && !DesignTimeThemeSyncMenus) return;
        if (menu == null || menu.IsDisposed) return;

        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(menu, _theme);
    }

    private void ApplyThemeToAllAttachedContextMenus()
    {
        if (!AutoApplyThemeToContextMenus) return;

        ApplyViewGridThemeToMenu(ContextMenuStrip);
        ApplyViewGridThemeToMenu(HeaderMenuOverride);
        ApplyViewGridThemeToMenu(BodyMenuOverride);
    }

    public void ApplyThemePreset(ViewGridThemePreset preset)
    {
        ThemePreset = preset;
    }

    private void ApplySelectedTheme()
    {
        if (IsInDesignMode)
        {
            ApplyDesignTimePreviewTheme();
            return;
        }

        ApplyTheme(ResolveRuntimeTheme());
    }

    public void RefreshThemeFromParent()
    {
        ApplySelectedTheme();
        RefreshView();
    }

    private ViewGridTheme ResolveRuntimeTheme()
    {
        if (AutoThemeFromParent && ThemePreset == ViewGridThemePreset.System)
            return ResolveAutoThemeFromParent(useFluent: false);

        return ViewGridTheme.FromPreset(ThemePreset);
    }

    private bool IsInDesignMode => DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime || Site?.DesignMode == true;

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (IsInDesignMode)
        {
            ApplyDesignTimePreviewTheme();
            EnsureDesignTimeSampleData();
        }
        else if (AutoThemeFromParent && ThemePreset == ViewGridThemePreset.System)
        {
            ApplySelectedTheme();
        }
    }

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        if (IsInDesignMode)
        {
            ApplyDesignTimePreviewTheme();
            EnsureDesignTimeSampleData();
        }
        else
        {
            ApplySelectedTheme();
        }
        TryAutoLoadUserLayoutOnce();
        TryAutoLoadV27State();
    }

    protected override void OnContextMenuStripChanged(EventArgs e)
    {
        base.OnContextMenuStripChanged(e);
        ApplyViewGridThemeToMenu(ContextMenuStrip);
    }

    public void RefreshDesignTimePreview()
    {
        if (!IsInDesignMode) return;
        ApplyDesignTimePreviewTheme();
        EnsureDesignTimeSampleData();
        RefreshView();
    }

    private void ApplyDesignTimePreviewTheme()
    {
        if (!IsInDesignMode) return;

        if (!EnableDesignTimeThemeSync)
        {
            ApplyTheme(ResolveRuntimeTheme());
            return;
        }

        ViewGridTheme theme = ResolveDesignTimeTheme();
        ApplyTheme(theme);
    }

    private ViewGridTheme ResolveDesignTimeTheme()
    {
        ViewGridTheme theme = DesignTimeThemePreview switch
        {
            ViewGridDesignTimeThemePreview.Light => ViewGridTheme.LightTheme(),
            ViewGridDesignTimeThemePreview.Dark => ViewGridTheme.DarkTheme(),
            ViewGridDesignTimeThemePreview.Fluent => DesignTimeFollowParentTheme
                ? ResolveAutoThemeFromParent(useFluent: true)
                : ViewGridTheme.FluentLightTheme(),
            _ => DesignTimeFollowParentTheme
                ? ResolveAutoThemeFromParent(useFluent: false)
                : ViewGridTheme.LightTheme()
        };

        return NormalizeDesignTimeTheme(theme);
    }

    private ViewGridTheme NormalizeDesignTimeTheme(ViewGridTheme theme)
    {
        if (theme == null) return ViewGridTheme.LightTheme();

        // Visual Studio WinForms designer yüzeyi varsayılan olarak açık tema hissi verir.
        // Kullanıcı Dark seçmedikçe Auto tasarım zamanı önizlemesini açık ve okunabilir tutuyoruz.
        if (DesignTimeThemePreview != ViewGridDesignTimeThemePreview.Dark && !theme.IsDark)
        {
            theme.BackColor = Color.White;
            theme.ControlBackColor = Color.White;
            theme.PanelBackColor = Color.FromArgb(250, 251, 253);
            theme.HeaderBackColor = Color.FromArgb(246, 247, 250);
            theme.HeaderForeColor = Color.FromArgb(25, 25, 25);
            theme.ForeColor = Color.FromArgb(25, 25, 25);
            theme.GridColor = Color.FromArgb(226, 230, 236);
            theme.BorderColor = Color.FromArgb(210, 214, 220);
            theme.AlternateBackColor = Color.FromArgb(250, 251, 253);
            theme.HotBackColor = Color.FromArgb(236, 244, 255);
            theme.SelectionBackColor = Color.FromArgb(0, 120, 215);
            theme.SelectionForeColor = Color.White;
            theme.MutedForeColor = Color.FromArgb(105, 105, 105);
            theme.EmptyTextColor = Color.FromArgb(120, 120, 120);
            theme.AccentColor = Color.FromArgb(0, 120, 215);
            theme.IsDark = false;
            theme.UseAcrylicEffect = false;
            theme.UseFluentBackdrop = false;
        }

        return theme;
    }

    private ViewGridTheme ResolveAutoThemeFromParent(bool useFluent)
    {
        if (AutoThemeFromParent)
        {
            var parent = FindThemeSourceControl();
            if (parent != null && parent.BackColor != Color.Empty && parent.BackColor != Color.Transparent)
            {
                var fromParent = ViewGridTheme.FromParentColor(parent.BackColor, parent.ForeColor);
                if (useFluent)
                    return fromParent.IsDark ? ViewGridTheme.FluentDarkTheme() : ViewGridTheme.FluentLightTheme();
                return fromParent;
            }
        }

        var systemTheme = ViewGridTheme.FromPreset(ThemePreset);
        if (useFluent)
            return systemTheme.IsDark ? ViewGridTheme.FluentDarkTheme() : ViewGridTheme.FluentLightTheme();
        return systemTheme;
    }

    private Control? FindThemeSourceControl()
    {
        for (Control? c = Parent; c != null; c = c.Parent)
        {
            if (c.BackColor != Color.Empty && c.BackColor != Color.Transparent)
                return c;
        }
        return Parent;
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        if (_applyingTheme) return;
        if (IsInDesignMode && DesignTimeThemePreview == ViewGridDesignTimeThemePreview.Auto)
            ApplyDesignTimePreviewTheme();
    }

    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        if (_applyingTheme) return;
        if (IsInDesignMode && DesignTimeThemePreview == ViewGridDesignTimeThemePreview.Auto)
            ApplyDesignTimePreviewTheme();
    }

    protected override void OnParentBackColorChanged(EventArgs e)
    {
        base.OnParentBackColorChanged(e);
        if (_applyingTheme) return;
        if (IsInDesignMode && DesignTimeThemePreview == ViewGridDesignTimeThemePreview.Auto)
            ApplyDesignTimePreviewTheme();
        else if (AutoThemeFromParent && ThemePreset == ViewGridThemePreset.System)
            ApplySelectedTheme();
    }

    protected override void OnParentForeColorChanged(EventArgs e)
    {
        base.OnParentForeColorChanged(e);
        if (_applyingTheme) return;
        if (IsInDesignMode && DesignTimeThemePreview == ViewGridDesignTimeThemePreview.Auto)
            ApplyDesignTimePreviewTheme();
        else if (AutoThemeFromParent && ThemePreset == ViewGridThemePreset.System)
            ApplySelectedTheme();
    }

    private static bool IsDarkColor(Color color)
    {
        if (color == Color.Empty || color == Color.Transparent) return false;
        double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255d;
        return luminance < 0.45d;
    }

    private void EnsureDesignTimeSampleData()
    {
        if (!IsInDesignMode || !DesignTimeSampleData || _designTimeSampleDataInitialized) return;
        _designTimeSampleDataInitialized = true;

        try
        {
            if (Columns.Count == 0)
            {
                Columns.Add(new ViewGridColumn("Durum", "State", 90) { Editable = true });
                Columns.Add(new ViewGridColumn("Ad", "Name", 170) { Editable = true, FillFreeSpace = true });
                Columns.Add(new ViewGridColumn("Oran", "Rate", 80) { Editable = true, TextAlign = ContentAlignment.MiddleRight });
                Columns.Add(new ViewGridColumn("Puan", "Rating", 80) { Kind = ViewGridColumnKind.Rating, Editable = true });
            }

            if (ViewCount == 0)
            {
                var rows = new[]
                {
                    new Dictionary<string, object?> { ["State"] = "OK", ["Name"] = "AOI kayıt 1", ["Rate"] = "98%", ["Rating"] = 5 },
                    new Dictionary<string, object?> { ["State"] = "Review", ["Name"] = "AOI kayıt 2", ["Rate"] = "76%", ["Rating"] = 3 },
                    new Dictionary<string, object?> { ["State"] = "Fail", ["Name"] = "AOI kayıt 3", ["Rate"] = "24%", ["Rating"] = 1 },
                    new Dictionary<string, object?> { ["State"] = "OK", ["Name"] = "AOI kayıt 4", ["Rate"] = "91%", ["Rating"] = 4 }
                };
                SetObjects(rows);
            }
        }
        catch
        {
            // Designer preview must never break Visual Studio or host forms.
            // If sample data cannot be created in a specific designer session,
            // keep the real Columns/Objects untouched and simply skip preview rows.
            try
            {
                SetProviderCore(new ListRowProvider(Array.Empty<object>()));
                InvalidateDataCaches();
                BuildViewIndex();
            }
            catch
            {
                // ignored intentionally: design-time safety net
            }
        }
    }

    public void SetObjects<T>(IEnumerable<T> rows)
    {
        if (_mode != ViewGridDataMode.Tile) _mode = ViewGridDataMode.Object;
        SetProviderCore(new ListRowProvider(rows.Cast<object>()));
        InvalidateDataCaches();
        BuildViewIndex();
        ApplyAutoFitAfterDataChanged();
        OnObjectsChanged();
    }

    public Task SetObjectsAsync<T>(Func<CancellationToken, Task<IEnumerable<T>>> loader, CancellationToken token = default)
    {
        return Task.Run(async () =>
        {
            var data = (await loader(token)).Cast<object>().ToList();
            if (!IsDisposed) BeginInvoke(new Action(() => SetObjects(data)));
        }, token);
    }

    public void SetVirtualProvider(IRowProvider provider)
    {
        _mode = ViewGridDataMode.Virtual;
        SetProviderCore(provider ?? throw new ArgumentNullException(nameof(provider)));
        InvalidateDataCaches();
        BuildViewIndex(useAllForHugeMode:false);
        ApplyAutoFitAfterDataChanged();
    }

    public void SetUltraVirtualProvider(IQueryRowProvider provider)
    {
        _mode = ViewGridDataMode.Virtual;
        SetProviderCore(provider ?? throw new ArgumentNullException(nameof(provider)));
        InvalidateDataCaches();
        BuildViewIndex(useAllForHugeMode:false);
        ApplyAutoFitAfterDataChanged();
    }

    private void SetProviderCore(IRowProvider provider)
    {
        if (_providerChangeNotifier != null)
            _providerChangeNotifier.RowsChanged -= ProviderRowsChanged;

        _provider = provider;
        _providerChangeNotifier = provider as IProviderChangeNotifier;

        if (_providerChangeNotifier != null)
            _providerChangeNotifier.RowsChanged += ProviderRowsChanged;
    }

    private void ProviderRowsChanged(object? sender, EventArgs e)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(ApplyAutoFitAfterDataChanged)); }
            catch { }
            return;
        }
        ApplyAutoFitAfterDataChanged();
    }

    [Browsable(false)]
    public long TotalRowCount64 => _provider is IQueryRowProvider queryProvider ? queryProvider.TotalCount64 : _provider.Count;



    private void ApplyModeVisualDefaults(ViewGridDataMode mode)
    {
        switch (mode)
        {
            case ViewGridDataMode.Tile:
                if (_viewMode != ViewGridMode.Tile) SetViewMode(ViewGridMode.Tile);
                break;
            case ViewGridDataMode.Tree:
                if (_viewMode != ViewGridMode.Details) SetViewMode(ViewGridMode.Details);
                break;
            default:
                if (_viewMode == ViewGridMode.Tile) SetViewMode(ViewGridMode.Details);
                break;
        }
    }

    public void SetViewMode(ViewGridMode mode)
    {
        _viewMode = mode;
        RememberCurrentViewModeForActiveScenario();
        if (mode == ViewGridMode.Poster)
            ApplyPosterModeDefaults(false);
        if (IsCardLikeViewMode(mode))
            _mode = ViewGridDataMode.Tile;
        else if (_mode == ViewGridDataMode.Tile)
            _mode = ViewGridDataMode.Object;

        if (mode == ViewGridMode.GroupedList && !EnableGrouping)
        {
            var groupColumn = Columns.VisibleColumns.FirstOrDefault(c => c.AspectName.Equals("Status", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Durum", StringComparison.OrdinalIgnoreCase))
                ?? Columns.VisibleColumns.FirstOrDefault(c => c.AspectName.Equals("Machine", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Makine", StringComparison.OrdinalIgnoreCase))
                ?? Columns.VisibleColumns.FirstOrDefault();
            if (groupColumn != null)
                SetGroupBy(groupColumn.AspectName);
        }

        bool detailsLikeMode = mode == ViewGridMode.Details || mode == ViewGridMode.DenseList || mode == ViewGridMode.GroupedList || mode == ViewGridMode.MasterDetail;
        ShowHeader = detailsLikeMode;

        // v28.14: Kart/poster modundan Details/List moduna dönerken önceki kart yüksekliği
        // _detailsRowHeight içine taşınmışsa seçimden sonra satır yüksekliği devasa kalıyordu.
        // Details tarafı kendi sabit yüksekliğini doğrudan geri alsın.
        if (detailsLikeMode || mode == ViewGridMode.List)
        {
            int detailsHeight = mode == ViewGridMode.DenseList ? 22 : (mode == ViewGridMode.List ? 30 : 28);
            _detailsRowHeight = detailsHeight;
            _rowHeight = detailsHeight;
        }
        else
        {
            RowHeight = mode switch
            {
                ViewGridMode.Poster => Math.Max(180, PosterPreferredHeight),
                ViewGridMode.MediaTile => Math.Max(132, TilePreferredHeight),
                ViewGridMode.Gallery => Math.Max(170, TilePreferredHeight),
                ViewGridMode.FilmStrip => Math.Max(150, TilePreferredHeight),
                ViewGridMode.ExtraLargeIcons => 96,
                ViewGridMode.LargeIcons => 72,
                ViewGridMode.MediumIcons => 48,
                ViewGridMode.Tile => TilePosterMode ? Math.Max(180, TilePreferredHeight) : Math.Max(72, TilePreferredHeight),
                ViewGridMode.IconGrid => Math.Max(92, TilePreferredHeight),
                ViewGridMode.DashboardCard => Math.Max(150, Math.Max(TilePreferredHeight, LargeCardPreferredHeight - 12)),
                ViewGridMode.KpiDashboard => Math.Max(130, TilePreferredHeight),
                ViewGridMode.HeatMap => Math.Max(118, TilePreferredHeight),
                ViewGridMode.MiniChart => Math.Max(108, TilePreferredHeight),
                ViewGridMode.RowPreview => Math.Max(96, TilePreferredHeight),
                ViewGridMode.RowCard => Math.Max(118, TilePreferredHeight),
                ViewGridMode.PropertyCard => GetDetailCardPreferredHeight(),
                ViewGridMode.DetailCard => GetDetailCardPreferredHeight(),
                ViewGridMode.GroupCard => Math.Max(150, Math.Max(TilePreferredHeight, LargeCardPreferredHeight - 20)),
                ViewGridMode.Kanban => Math.Max(168, Math.Max(TilePreferredHeight, LargeCardPreferredHeight)),
                ViewGridMode.Timeline => Math.Max(132, TilePreferredHeight),
                ViewGridMode.LargeCard => Math.Max(LargeCardPreferredHeight, Math.Max(132, TilePreferredHeight)),
                _ => 28
            };
        }
        RefreshCardViewFilterUx();
        RefreshView();
    }

    private static bool IsCardLikeViewMode(ViewGridMode mode)
        => mode is ViewGridMode.Tile
            or ViewGridMode.Poster
            or ViewGridMode.MediaTile
            or ViewGridMode.Gallery
            or ViewGridMode.FilmStrip
            or ViewGridMode.LargeCard
            or ViewGridMode.ExtraLargeIcons
            or ViewGridMode.LargeIcons
            or ViewGridMode.MediumIcons
            or ViewGridMode.DashboardCard
            or ViewGridMode.RowCard
            or ViewGridMode.DetailCard
            or ViewGridMode.IconGrid
            or ViewGridMode.GroupCard
            or ViewGridMode.PropertyCard
            or ViewGridMode.KpiDashboard
            or ViewGridMode.HeatMap
            or ViewGridMode.MiniChart
            or ViewGridMode.RowPreview
            or ViewGridMode.Kanban
            or ViewGridMode.Timeline;

    public static string GetViewModeDisplayName(ViewGridMode mode)
        => mode switch
        {
            ViewGridMode.Details => "Detay Liste",
            ViewGridMode.DenseList => "Yoğun Liste",
            ViewGridMode.List => "Basit Liste",
            ViewGridMode.Tile => "Kart Görünümü",
            ViewGridMode.LargeCard => "Geniş Kart",
            ViewGridMode.DashboardCard => "Dashboard Kart",
            ViewGridMode.RowCard => "Satır Kart",
            ViewGridMode.DetailCard => "DetailCard",
            ViewGridMode.MediaTile => "MediaTile",
            ViewGridMode.Gallery => "Gallery",
            ViewGridMode.FilmStrip => "FilmStrip",
            ViewGridMode.IconGrid => "İkon Grid",
            ViewGridMode.GroupedList => "Gruplu Liste",
            ViewGridMode.GroupCard => "GroupCard",
            ViewGridMode.PropertyCard => "PropertyCard",
            ViewGridMode.KpiDashboard => "KPI Dashboard",
            ViewGridMode.HeatMap => "HeatMap",
            ViewGridMode.MiniChart => "MiniChart",
            ViewGridMode.RowPreview => "RowPreview",
            ViewGridMode.Kanban => "Kanban",
            ViewGridMode.Timeline => "Zaman Akışı",
            ViewGridMode.MasterDetail => "Master-Detail",
            ViewGridMode.Poster => "Poster Görünümü",
            ViewGridMode.ExtraLargeIcons => "Poster Kart",
            ViewGridMode.LargeIcons => "Büyük İkon",
            ViewGridMode.MediumIcons => "Kompakt İkon",
            _ => mode.ToString()
        };

    public void SetGroupBy(string aspectName)
    {
        EnableGrouping = true;
        _groupByAspectName = aspectName;
        _collapsedGroups.Clear();
        BuildViewIndex();
    }

    public void ToggleGroupBy(string aspectName)
    {
        if (EnableGrouping && string.Equals(_groupByAspectName, aspectName, StringComparison.OrdinalIgnoreCase)) ClearGrouping();
        else SetGroupBy(aspectName);
    }

    public IReadOnlyList<string> GetGroupKeys()
        => _displayRows.Where(r => r.IsGroup).Select(r => r.GroupKey).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

    public void ClearGrouping()
    {
        EnableGrouping = false;
        _groupByAspectName = null;
        _collapsedGroups.Clear();
        BuildViewIndex();
    }

    public void ToggleGroup(string groupKey)
    {
        if (!AllowGroupCollapse || string.IsNullOrWhiteSpace(groupKey)) return;
        if (_collapsedGroups.Contains(groupKey)) _collapsedGroups.Remove(groupKey);
        else _collapsedGroups.Add(groupKey);
        BuildDisplayRows();
        RefreshView();
        QueueAutoSaveUserLayout();
    }

    public void ExpandGroup(string groupKey)
    {
        if (string.IsNullOrWhiteSpace(groupKey)) return;
        if (_collapsedGroups.Remove(groupKey)) { BuildDisplayRows(); RefreshView(); }
    }

    public void CollapseGroup(string groupKey)
    {
        if (string.IsNullOrWhiteSpace(groupKey)) return;
        if (_collapsedGroups.Add(groupKey)) { BuildDisplayRows(); RefreshView(); }
    }

    public void ExpandAllGroups()
    {
        if (_collapsedGroups.Count == 0) return;
        _collapsedGroups.Clear();
        BuildDisplayRows();
        RefreshView();
    }

    public void CollapseAllGroups()
    {
        if (!EnableGrouping || string.IsNullOrWhiteSpace(_groupByAspectName)) return;
        foreach (var row in _displayRows.Where(r => r.IsGroup)) _collapsedGroups.Add(row.GroupKey);
        BuildDisplayRows();
        RefreshView();
    }

    public void ShowOnlyGroup(string groupKey)
    {
        if (!EnableGrouping || string.IsNullOrWhiteSpace(_groupByAspectName) || string.IsNullOrWhiteSpace(groupKey)) return;
        _collapsedGroups.Clear();
        foreach (var row in _displayRows.Where(r => r.IsGroup))
        {
            if (!string.Equals(row.GroupKey, groupKey, StringComparison.CurrentCultureIgnoreCase))
                _collapsedGroups.Add(row.GroupKey);
        }
        BuildDisplayRows();
        RefreshView();
    }

    public void SetGlobalFilter(string text)
    {
        text ??= string.Empty;

        // v26.58: Clearing a huge virtual/global filter must be immediate.
        // Debouncing empty text kept the old provider view alive for one timer tick and
        // made the sample look frozen when the user pressed "clear filter".
        bool isClearRequest = string.IsNullOrWhiteSpace(text);
        if (string.Equals(_filters.GlobalText, text, StringComparison.Ordinal) &&
            string.Equals(_pendingGlobalFilterText, text, StringComparison.Ordinal))
        {
            if (HighlightGlobalFilterText) _searchHighlightText = text;
            Invalidate();
            return;
        }

        if (!isClearRequest && DebounceGlobalFilterForHugeVirtualLists && _provider.Count > 20_000)
        {
            _pendingGlobalFilterText = text;
            _globalFilterDebounceTimer.Stop();
            _globalFilterDebounceTimer.Start();
            return;
        }

        _globalFilterDebounceTimer.Stop();
        _pendingGlobalFilterText = text;
        _filters.GlobalText = text;
        if (HighlightGlobalFilterText) _searchHighlightText = text;
        BuildViewIndex();
        RefreshCardViewFilterUx();
    }

    public void SetColumnFilter(ViewGridColumnFilter filter)
    {
        _filters.Set(filter);
        BuildViewIndex();
        QueueAutoSaveUserLayout();
        RefreshCardViewFilterUx();
    }

    public void ClearFilters()
    {
        if (_filters.Filters.Count == 0 && string.IsNullOrEmpty(_filters.GlobalText) && string.IsNullOrEmpty(_searchHighlightText) && string.IsNullOrWhiteSpace(_v2815QueryText))
            return;

        _globalFilterDebounceTimer.Stop();
        _pendingGlobalFilterText = string.Empty;
        _filters.Clear();
        _filters.GlobalText = string.Empty;
        _searchHighlightText = string.Empty;
        _lastSearchMatchRow = -1;
        _v2815QueryText = string.Empty;
        _v2815QueryPredicate = null;
        BuildViewIndex();
        QueueAutoSaveUserLayout();
    }

    public IReadOnlyList<object> GetVisibleObjects() => EnumerateVisibleObjects().ToList();
    private IEnumerable<object> EnumerateVisibleObjects(int? limit = null)
    {
        int max = limit.HasValue ? Math.Min(ViewCount, limit.Value) : ViewCount;
        for (int i = 0; i < max; i++) { var row = GetViewRow(i); if (row != null) yield return row; }
    }


    public bool JumpToFirstMatch(string text)
    {
        _lastSearchMatchRow = -1;
        return FindNext(text, wrap: true);
    }

    public bool FindNext(string text, bool wrap = true)
    {
        return FindMatch(text, Math.Max(0, _lastSearchMatchRow + 1), forward: true, wrap: wrap);
    }

    public bool FindPrevious(string text, bool wrap = true)
    {
        int start = _lastSearchMatchRow < 0 ? ViewCount - 1 : _lastSearchMatchRow - 1;
        return FindMatch(text, start, forward: false, wrap: wrap);
    }

    public void ClearSearchHighlight()
    {
        _searchHighlightText = string.Empty;
        _lastSearchMatchRow = -1;
        Invalidate();
    }

    private bool FindMatch(string text, int startIndex, bool forward, bool wrap)
    {
        text ??= string.Empty;
        _searchHighlightText = text;
        if (string.IsNullOrWhiteSpace(text) || ViewCount <= 0)
        {
            _lastSearchMatchRow = -1;
            Invalidate();
            return false;
        }

        bool Scan(int from, int to)
        {
            if (forward)
            {
                for (int i = from; i <= to; i++)
                    if (RowContainsText(i, text)) { SelectRow(i); _lastSearchMatchRow = i; Invalidate(); return true; }
            }
            else
            {
                for (int i = from; i >= to; i--)
                    if (RowContainsText(i, text)) { SelectRow(i); _lastSearchMatchRow = i; Invalidate(); return true; }
            }
            return false;
        }

        startIndex = Math.Clamp(startIndex, 0, ViewCount - 1);
        if (forward)
        {
            if (Scan(startIndex, ViewCount - 1)) return true;
            if (wrap && startIndex > 0 && Scan(0, startIndex - 1)) return true;
        }
        else
        {
            if (Scan(startIndex, 0)) return true;
            if (wrap && startIndex < ViewCount - 1 && Scan(ViewCount - 1, startIndex + 1)) return true;
        }

        Invalidate();
        return false;
    }

    private bool RowContainsText(int viewIndex, string text)
    {
        var row = GetViewRow(viewIndex);
        if (row == null) return false;
        foreach (var col in Columns.VisibleColumns)
        {
            var value = Convert.ToString(col.GetValue(row)) ?? string.Empty;
            if (value.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    public void ShowColumnChooser()
    {
        if (IsDisposed || Disposing)
            return;

        try
        {
            // Runtime context/header menus can still be closing when this is called.
            // Always create a fresh dialog instance and use the top-level form as owner
            // so the chooser is not hidden behind the sample app or swallowed by ToolStrip.
            IWin32Window owner = (IWin32Window?)FindForm() ?? this;
            using var form = new ColumnChooserForm(Columns.ToList(), _theme);
            if (form.ShowDialog(owner) == DialogResult.OK)
            {
                AutoSizeFillColumns();
                RefreshView();
                Invalidate();
                if (AutoSaveLayoutOnColumnVisibilityChange) QueueAutoSaveUserLayout();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show((IWin32Window?)FindForm() ?? this, ex.ToString(), "ViewGrid kolon seçici hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    internal void ShowColumnChooserDeferred()
    {
        if (IsDisposed || Disposing)
            return;

        if (!IsHandleCreated)
        {
            ShowColumnChooser();
            return;
        }

        // ToolStripDropDown/ContextMenu kapanmadan ShowDialog çağrılırsa Windows Forms
        // bazen dialogu owner arkasında bırakabiliyor veya açılışı yutabiliyor.
        // Küçük bir UI timer ile menünün tamamen kapanmasını bekletiyoruz.
        var timer = new System.Windows.Forms.Timer { Interval = 120 };
        timer.Tick += (_, __) =>
        {
            timer.Stop();
            timer.Dispose();
            if (!IsDisposed && !Disposing) ShowColumnChooser();
        };
        timer.Start();
    }

    public void SaveLayout(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Layout dosya yolu boş olamaz.", nameof(path));

        var state = CaptureLayout();
        SanitizeLayoutFiltersIfNeeded(state);

        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder);

        File.WriteAllText(path, state.ToJson());
    }

    public void LoadLayout(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

        var state = ViewGridLayoutState.FromJson(File.ReadAllText(path));
        SanitizeLayoutFiltersIfNeeded(state);
        ApplyLayout(state);
    }

    private void SanitizeLayoutFiltersIfNeeded(ViewGridLayoutState state)
    {
        if (state == null || PersistColumnFilters) return;

        state.GlobalFilter = string.Empty;
        state.ColumnFilters = new List<ViewGridColumnFilter>();
    }
    public ViewGridLayoutState CaptureLayout()
    {
        var state = ViewGridLayoutState.Capture(Columns, PersistColumnFilters ? _filters.GlobalText : string.Empty, _sortColumn?.AspectName, _sortDesc);
        state.ColumnFilters = PersistColumnFilters
            ? _filters.Filters.Select(f => new ViewGridColumnFilter
            {
                AspectName = f.AspectName,
                Mode = f.Mode,
                Text = f.Text,
                Text2 = f.Text2,
                Enabled = f.Enabled,
                SelectedValues = f.SelectedValues == null ? null : new HashSet<string>(f.SelectedValues)
            }).ToList()
            : new List<ViewGridColumnFilter>();
        state.GroupByAspectName = _groupByAspectName;
        state.EnableGrouping = EnableGrouping;
        state.FrozenColumnCount = FrozenColumnCount;
        if (PersistVisualPreferences)
        {
            state.FilterMenuMode = FilterMenuMode.ToString();
            state.ShowColumnFilterButtons = ShowColumnFilterButtons;
            state.ShowColumnSortGlyphs = ShowColumnSortGlyphs;
            state.FilterIconStyle = FilterIconStyle.ToString();
            state.SortGlyphStyle = SortGlyphStyle.ToString();
            state.RowHeight = RowHeight;
            state.ViewMode = ViewMode.ToString();
            state.ColumnChooserMenuMode = ColumnChooserMenuMode.ToString();
            state.ShowColumnChooserInHeaderMenu = ShowColumnChooserInHeaderMenu;
            state.ShowColumnChooserWindowInHeaderMenu = ShowColumnChooserWindowInHeaderMenu;
        }
        if (SaveMenuIconPreferencesInUserLayout)
        {
            state.MenuIconMode = MenuIconMode.ToString();
            state.MenuIconSize = MenuIconSize.ToString();
            state.CustomMenuIconFolder = CustomMenuIconFolder ?? string.Empty;
        }
        return state;
    }
    public void ApplyLayout(ViewGridLayoutState state)
    {
        if (state == null) return;
        state.Apply(Columns);
        _filters.Clear();
        _globalFilterDebounceTimer.Stop();
        _pendingGlobalFilterText = string.Empty;
        _searchHighlightText = string.Empty;
        if (PersistColumnFilters)
        {
            _filters.GlobalText = state.GlobalFilter ?? string.Empty;
            _pendingGlobalFilterText = _filters.GlobalText;
            if (HighlightGlobalFilterText) _searchHighlightText = _filters.GlobalText;
            if (state.ColumnFilters != null)
            {
                foreach (var filter in state.ColumnFilters.Where(f => f != null && f.Enabled && !string.IsNullOrWhiteSpace(f.AspectName)))
                    _filters.Set(filter);
            }
        }
        else
        {
            _filters.GlobalText = string.Empty;
        }
        _sortColumn = Columns.FirstOrDefault(c => c.AspectName == state.SortAspectName);
        _sortDesc = state.SortDescending;
        _groupByAspectName = state.GroupByAspectName;
        EnableGrouping = state.EnableGrouping && !string.IsNullOrWhiteSpace(_groupByAspectName);
        FrozenColumnCount = Math.Max(0, state.FrozenColumnCount);
        if (PersistVisualPreferences)
        {
            if (!string.IsNullOrWhiteSpace(state.FilterMenuMode) && Enum.TryParse<ViewGridFilterMenuMode>(state.FilterMenuMode, out var filterMode)) FilterMenuMode = filterMode;
            if (!string.IsNullOrWhiteSpace(state.FilterIconStyle) && Enum.TryParse<ViewGridFilterIconStyle>(state.FilterIconStyle, out var filterIconStyle)) FilterIconStyle = filterIconStyle;
            if (!string.IsNullOrWhiteSpace(state.SortGlyphStyle) && Enum.TryParse<ViewGridSortGlyphStyle>(state.SortGlyphStyle, out var sortGlyphStyle)) SortGlyphStyle = sortGlyphStyle;
            ShowColumnFilterButtons = state.ShowColumnFilterButtons;
            ShowColumnSortGlyphs = state.ShowColumnSortGlyphs;
            if (state.RowHeight > 0) RowHeight = state.RowHeight;
            if (!string.IsNullOrWhiteSpace(state.ViewMode) && Enum.TryParse<ViewGridMode>(state.ViewMode, out var viewMode)) ViewMode = viewMode;
            if (!string.IsNullOrWhiteSpace(state.ColumnChooserMenuMode) && Enum.TryParse<ViewGridColumnChooserMenuMode>(state.ColumnChooserMenuMode, out var chooserMode)) ColumnChooserMenuMode = chooserMode;
            ShowColumnChooserInHeaderMenu = state.ShowColumnChooserInHeaderMenu;
            ShowColumnChooserWindowInHeaderMenu = state.ShowColumnChooserWindowInHeaderMenu;
            if (!string.IsNullOrWhiteSpace(state.MenuIconMode) && Enum.TryParse<ViewGridMenuIconMode>(state.MenuIconMode, out var menuIconMode)) MenuIconMode = menuIconMode;
            if (!string.IsNullOrWhiteSpace(state.MenuIconSize) && Enum.TryParse<ViewGridMenuIconSize>(state.MenuIconSize, out var menuIconSize)) MenuIconSize = menuIconSize;
            if (!string.IsNullOrWhiteSpace(state.CustomMenuIconFolder)) CustomMenuIconFolder = state.CustomMenuIconFolder;
        }
        BuildViewIndex();
    }

    public string ExportVisibleCsv(string path, char separator = ';') => ViewGridExporter.SaveCsv(path, Columns, GetVisibleObjects(), separator);
    public string ExportVisibleExcel(string path, string worksheetName = "ViewGridControl") => ViewGridExporter.SaveExcelXml(path, Columns, GetVisibleObjects(), worksheetName);
    public string ExportVisiblePdf(string path, string title = "ViewGridControl") => ViewGridExporter.SavePdf(path, Columns, GetVisibleObjects(), title);
    public string ExportVisiblePdf(string path, ViewGridPdfExportOptions options)
    {
        options ??= new ViewGridPdfExportOptions();
        if (options.Mode == ViewGridPdfExportMode.Auto)
        {
            options.Mode = ViewMode is ViewGridMode.DashboardCard or ViewGridMode.LargeCard or ViewGridMode.RowCard or ViewGridMode.DetailCard or ViewGridMode.Tile or ViewGridMode.Kanban or ViewGridMode.Poster or ViewGridMode.MediaTile or ViewGridMode.FilmStrip
                ? ViewGridPdfExportMode.Card
                : ViewGridPdfExportMode.Table;
        }
        options.CardVisualInfoResolver ??= row => ResolveCardVisualInfo(row, ResolveCardStatusColor(row));
        options.CardLayout ??= CardLayoutDefinition;
        return ViewGridExporter.SavePdf(path, Columns, GetVisibleObjects(), options);
    }
    public void CopySelectionToClipboard()
    {
        if (!EnableClipboard) return;
        var rows = (_selectedRows.Count > 0 ? SelectedObjects : (SelectedObject == null ? Array.Empty<object>() : new[] { SelectedObject })).ToList();
        if (rows.Count == 0) return;
        var visible = Columns.VisibleColumns.ToArray();
        var lines = new List<string>(rows.Count + 1)
        {
            string.Join("\t", visible.Select(c => c.Header))
        };
        foreach (var row in rows)
            lines.Add(string.Join("\t", visible.Select(c => Convert.ToString(c.GetValue(row)) ?? string.Empty)));
        Clipboard.SetText(string.Join(Environment.NewLine, lines));
    }

    public string GetSelectionText(bool includeHeaders = true, char separator = '\t')
    {
        var rows = (_selectedRows.Count > 0 ? SelectedObjects : (SelectedObject == null ? Array.Empty<object>() : new[] { SelectedObject })).ToList();
        if (rows.Count == 0) return string.Empty;
        var visible = Columns.VisibleColumns.ToArray();
        var lines = new List<string>(rows.Count + 1);
        if (includeHeaders) lines.Add(string.Join(separator, visible.Select(c => c.Header)));
        foreach (var row in rows) lines.Add(string.Join(separator, visible.Select(c => Convert.ToString(c.GetValue(row)) ?? string.Empty)));
        return string.Join(Environment.NewLine, lines);
    }

    public void CopySelectedCellToClipboard()
    {
        if (!EnableClipboard || _selectedRow < 0) return;
        var row = GetViewRow(_selectedRow);
        var col = _activeColumn ?? Columns.VisibleColumns.FirstOrDefault();
        if (row == null || col == null) return;
        Clipboard.SetText(Convert.ToString(col.GetValue(row)) ?? string.Empty);
    }

    public void CopySelectionAsJsonToClipboard()
    {
        if (!EnableClipboard) return;
        var rows = (_selectedRows.Count > 0 ? SelectedObjects : (SelectedObject == null ? Array.Empty<object>() : new[] { SelectedObject })).ToList();
        if (rows.Count == 0) return;
        Clipboard.SetText(ViewGridExporter.ToJson(Columns, rows));
    }

    public string ExportSelectedCsv(string path, char separator = ';') => ViewGridExporter.SaveCsv(path, Columns, SelectedObjects, separator);
    public string ExportSelectedExcel(string path, string worksheetName = "ViewGridSelection") => ViewGridExporter.SaveExcelXml(path, Columns, SelectedObjects, worksheetName);
    public string ExportVisibleJson(string path) => ViewGridExporter.SaveJson(path, Columns, GetVisibleObjects());
    public string ExportSelectedJson(string path) => ViewGridExporter.SaveJson(path, Columns, SelectedObjects);

    public IReadOnlyList<ViewGridColumnAnalytics> GetVisibleAnalytics(int maxDistinctPerColumn = 8, int maxRows = 50000)
    {
        var rows = EnumerateVisibleObjects(maxRows).ToList();
        var result = new List<ViewGridColumnAnalytics>();
        foreach (var col in Columns.VisibleColumns)
            result.Add(ViewGridColumnAnalytics.From(col, rows, maxDistinctPerColumn));
        return result;
    }

    public ViewGridColumnAnalytics? GetColumnAnalytics(string aspectName, int maxDistinct = 12, int maxRows = 100000)
    {
        var col = Columns.FirstOrDefault(c => string.Equals(c.AspectName, aspectName, StringComparison.OrdinalIgnoreCase) || string.Equals(c.Header, aspectName, StringComparison.OrdinalIgnoreCase));
        return col == null ? null : ViewGridColumnAnalytics.From(col, EnumerateVisibleObjects(maxRows).ToList(), maxDistinct);
    }

    public void CopyMiniAnalyticsToClipboard()
    {
        var lines = new List<string> { "Column\tRows\tBlank\tDistinct\tTop values" };
        foreach (var item in GetVisibleAnalytics())
            lines.Add($"{item.ColumnText}\t{item.RowCount}\t{item.BlankCount}\t{item.DistinctCount}\t{string.Join(", ", item.TopValues.Select(x => $"{x.Value} ({x.Count})"))}");
        Clipboard.SetText(string.Join(Environment.NewLine, lines));
    }

    public void HighlightRow(int viewRowIndex, Color? color = null, int durationMs = -1, string? reason = null)
    {
        if (!EnableHighlightEngine || viewRowIndex < 0 || viewRowIndex >= ViewCount) return;
        _rowHighlights[viewRowIndex] = new RowHighlightState
        {
            Color = color ?? (_theme.IsDark ? Color.FromArgb(255, 193, 7) : Color.FromArgb(255, 224, 130)),
            ExpiresUtc = DateTime.UtcNow.AddMilliseconds(durationMs > 0 ? durationMs : DefaultHighlightDurationMs),
            Reason = reason
        };
        if (!_highlightTimer.Enabled) _highlightTimer.Start();
        Invalidate();
    }

    public void HighlightObject(object? model, Color? color = null, int durationMs = -1, string? reason = null)
    {
        int index = IndexOfObject(model);
        if (index >= 0) HighlightRow(index, color, durationMs, reason);
    }

    public void ClearRowHighlights()
    {
        _rowHighlights.Clear();
        _highlightTimer.Stop();
        Invalidate();
    }

    private void PruneExpiredRowHighlights()
    {
        if (_rowHighlights.Count == 0) { _highlightTimer.Stop(); return; }
        var now = DateTime.UtcNow;
        var expired = _rowHighlights.Where(kv => kv.Value.ExpiresUtc <= now).Select(kv => kv.Key).ToList();
        foreach (var key in expired) _rowHighlights.Remove(key);
        if (_rowHighlights.Count == 0) _highlightTimer.Stop();
        if (expired.Count > 0) Invalidate();
    }

    public void PasteTextToSelectedCell(string text)
    {
        if (!EnableClipboard || _selectedRow < 0) return;
        var row = GetViewRow(_selectedRow);
        var col = Columns.VisibleColumns.FirstOrDefault(c => c.Editable);
        if (row == null || col == null) return;
        SetCellValueWithUndo(_selectedRow, row, col, text);
    }

    public void Undo() { if (EnableUndoRedo && _undo.Undo() != null) Invalidate(); }
    public void Redo() { if (EnableUndoRedo && _undo.Redo() != null) Invalidate(); }

    public void ToggleRowDetails(int viewRowIndex)
    {
        if (!EnableRowDetails || viewRowIndex < 0) return;
        if (_expandedRow == viewRowIndex) { CloseRowDetails(); return; }
        CloseRowDetails();
        _expandedRow = viewRowIndex;
        var row = GetViewRow(viewRowIndex);
        if (row != null && _detailsProvider?.CreateDetailsControl != null)
        {
            _detailsControl = _detailsProvider.CreateDetailsControl(row);
            _detailsControl.Visible = true;
            Controls.Add(_detailsControl);
            _detailsControl.BringToFront();
            PositionRowDetailsControl();
        }
        Invalidate();
    }

    public void CloseRowDetails()
    {
        _expandedRow = -1;
        if (_detailsControl != null) { Controls.Remove(_detailsControl); _detailsControl.Dispose(); _detailsControl = null; }
        Invalidate();
    }


    private Dictionary<ViewGridColumn, string> BuildSummaryTextCache()
    {
        var result = new Dictionary<ViewGridColumn, string>();
        if (_summaries.Count == 0) return result;
        var rows = EnumerateVisibleObjects(MaxSummaryScanRows).ToList();
        foreach (var summary in _summaries)
            result[summary.Column] = summary.Calculate(rows);
        return result;
    }

    private void InvalidateDataCaches(bool keepVersion = false)
    {
        if (!keepVersion) _dataVersion++;
        _summaryTextCache = null;
        _distinctValueCache.Clear();
        _smartFilterIndexCache.Clear();
    }

    private void PositionRowDetailsControl()
    {
        if (_detailsControl == null || _expandedRow < 0) return;
        int top = ShowHeader ? HeaderHeight : 0;
        int y = top + (_expandedRow - _scrollY + 1) * RowHeight;
        int h = Math.Max(40, _detailsProvider?.PreferredHeight ?? 90);
        _detailsControl.Bounds = new Rectangle(0, y, Math.Max(0, Width - VBarWidth), h);
        _detailsControl.Visible = y >= top && y < Height - (ShowSummaryFooter ? FooterHeight : 0);
    }

    private void BuildViewIndex(bool useAllForHugeMode=true)
    {
        _viewIndexes.Clear();
        _summaryTextCache = null;
        int count = _provider.Count;
        bool hasActiveFilter = _filters.Filters.Count > 0 || !string.IsNullOrWhiteSpace(_filters.GlobalText) || _modelFilter != null || _additionalFilterFunc != null || _v2815QueryPredicate != null;
        bool groupingActive = EnableGrouping && !string.IsNullOrWhiteSpace(_groupByAspectName);

        // UltraVirtual/server-backed mode: push filters and sorting to the provider and keep the list direct.
        // This avoids creating a gigantic _viewIndexes array for 1M+ / theoretically huge row counts.
        if (_modelFilter == null && _additionalFilterFunc == null && _v2815QueryPredicate == null && !groupingActive && _provider is IQueryRowProvider queryProvider)
        {
            queryProvider.ApplyView(_filters, Columns.ToArray(), _sortColumn, _sortDesc);
            _viewIsDirect = true;
            BuildDisplayRows();
            RefreshView();
            return;
        }

        bool canUseDirectVirtualView = count > 200_000 && !hasActiveFilter && _sortColumn == null && !groupingActive;

        // Direct virtual view is only safe when there is no active filter or sort.
        _viewIsDirect = canUseDirectVirtualView;
        if (!_viewIsDirect)
        {
            var allColumns = Columns.ToArray();

            if (_modelFilter == null && _additionalFilterFunc == null && _v2815QueryPredicate == null && count >= FastFilterIndexedProviderThreshold && _provider is IIndexedRowProvider indexed && indexed.TryBuildViewIndexes(_filters, allColumns, _sortColumn, _sortDesc, MaxVirtualFilterScanRows, out var indexes))
            {
                _viewIndexes.AddRange(indexes);
            }
            else
            {
                int scanCount = count;
                if (count > 200_000 && hasActiveFilter)
                    scanCount = Math.Min(count, Math.Max(10_000, MaxVirtualFilterScanRows));

                for (int i = 0; i < scanCount; i++)
                {
                    var row = _provider.GetRow(i);
                    if (row != null && _filters.Passes(row, allColumns) && (_modelFilter == null || SafeModelFilterPasses(row)) && SafeAdditionalFilterPasses(row) && SafeUltimateQueryPasses(row)) _viewIndexes.Add(i);
                }
                if (_sortColumn != null)
                {
                    _viewIndexes.Sort((a,b) =>
                    {
                        int primary = CompareRows(_provider.GetRow(a), _provider.GetRow(b), _sortColumn);
                        if (primary != 0 || _secondarySortColumn == null) return primary;
                        int secondary = CompareRows(_provider.GetRow(a), _provider.GetRow(b), _secondarySortColumn);
                        return _secondarySortDescending ? -secondary : secondary;
                    });
                    if (_sortDesc) _viewIndexes.Reverse();
                }
            }
        }
        BuildDisplayRows();
        RefreshView();
    }

    private bool SafeModelFilterPasses(object row)
    {
        try { return _modelFilter == null || _modelFilter(row); }
        catch { return false; }
    }

    private void BuildDisplayRows()
    {
        _displayRows.Clear();
        if (!EnableGrouping || string.IsNullOrWhiteSpace(_groupByAspectName)) return;
        var groupCol = Columns.FirstOrDefault(c => string.Equals(c.AspectName, _groupByAspectName, StringComparison.OrdinalIgnoreCase));
        if (groupCol == null) return;

        IEnumerable<int> sourceIndexes = _viewIsDirect
            ? Enumerable.Range(0, Math.Min(_provider.Count, MaxVirtualFilterScanRows))
            : _viewIndexes;

        var groups = new SortedDictionary<string, List<int>>(StringComparer.CurrentCultureIgnoreCase);
        foreach (int realIndex in sourceIndexes)
        {
            var row = _provider.GetRow(realIndex);
            if (row == null) continue;
            string key = Convert.ToString(groupCol.GetValue(row)) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key)) key = ViewGridText.EmptyValue;
            if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<int>();
            list.Add(realIndex);
        }

        foreach (var pair in groups)
        {
            bool collapsed = _collapsedGroups.Contains(pair.Key);
            _displayRows.Add(ViewGridDisplayRow.Group(pair.Key, pair.Value.Count, collapsed));
            if (!collapsed)
            {
                foreach (int real in pair.Value) _displayRows.Add(ViewGridDisplayRow.Data(real));
            }
        }
        _viewIsDirect = false;
    }

    private bool IsGroupingActive => EnableGrouping && _displayRows.Count > 0;
    /// <summary>Gets the number of rows currently visible in the view after filtering/grouping.</summary>
    [Browsable(false)]
    public int ViewCount => IsGroupingActive ? _displayRows.Count : (_viewIsDirect ? _provider.Count : _viewIndexes.Count);

    /// <summary>GLV compatibility alias for ViewCount.</summary>
    [Browsable(false)]
    public int FilteredObjectCount => ViewCount;
    private object? GetViewRow(int viewIndex)
    {
        if (viewIndex < 0 || viewIndex >= ViewCount) return null;
        if (IsGroupingActive)
        {
            var display = _displayRows[viewIndex];
            return display.IsGroup ? null : _provider.GetRow(display.RealIndex);
        }
        int real = _viewIsDirect ? viewIndex : _viewIndexes[viewIndex];
        return _provider.GetRow(real);
    }

    private bool IsGroupRow(int viewIndex) => IsGroupingActive && viewIndex >= 0 && viewIndex < _displayRows.Count && _displayRows[viewIndex].IsGroup;
    private string GetGroupCaption(int viewIndex) => IsGroupRow(viewIndex) ? _displayRows[viewIndex].Caption : string.Empty;
    private string GetGroupKey(int viewIndex) => IsGroupRow(viewIndex) ? _displayRows[viewIndex].GroupKey : string.Empty;
    private bool IsGroupCollapsed(int viewIndex) => IsGroupRow(viewIndex) && _displayRows[viewIndex].Collapsed;

    private readonly record struct ViewGridSortEntry(int Index, object? PrimaryKey, object? SecondaryKey);

    private static int CompareSortValues(object? va, object? vb)
    {
        if (ReferenceEquals(va, vb)) return 0;
        if (va == null) return -1;
        if (vb == null) return 1;
        if (va is IComparable ca)
        {
            try { return ca.CompareTo(vb); }
            catch { }
        }
        return string.Compare(Convert.ToString(va), Convert.ToString(vb), StringComparison.CurrentCultureIgnoreCase);
    }

    private static int CompareRows(object? a, object? b, ViewGridColumn col)
    {
        var va = col.GetValue(a!); var vb = col.GetValue(b!);
        return CompareSortValues(va, vb);
    }

    private bool IsTileView => IsCardLikeViewMode(ViewMode);

    private int GetMinimumRowHeightForCurrentTileView()
    {
        return ViewMode switch
        {
            ViewGridMode.Poster => Math.Max(180, PosterPreferredHeight),
            ViewGridMode.MediaTile => Math.Max(132, TilePreferredHeight),
            ViewGridMode.Gallery => Math.Max(170, TilePreferredHeight),
            ViewGridMode.FilmStrip => Math.Max(150, TilePreferredHeight),
            ViewGridMode.ExtraLargeIcons => TilePosterMode ? Math.Max(180, TilePreferredHeight) : 96,
            ViewGridMode.LargeIcons => TilePosterMode ? Math.Max(180, TilePreferredHeight) : 72,
            ViewGridMode.MediumIcons => 48,
            ViewGridMode.Tile => TilePosterMode ? Math.Max(180, TilePreferredHeight) : Math.Max(72, TilePreferredHeight),
            ViewGridMode.IconGrid => Math.Max(92, TilePreferredHeight),
            ViewGridMode.DashboardCard => Math.Max(150, Math.Max(TilePreferredHeight, LargeCardPreferredHeight - 12)),
            ViewGridMode.KpiDashboard => Math.Max(130, TilePreferredHeight),
            ViewGridMode.HeatMap => Math.Max(118, TilePreferredHeight),
            ViewGridMode.MiniChart => Math.Max(108, TilePreferredHeight),
            ViewGridMode.RowPreview => Math.Max(96, TilePreferredHeight),
            ViewGridMode.RowCard => Math.Max(118, TilePreferredHeight),
            ViewGridMode.PropertyCard => GetDetailCardPreferredHeight(),
            ViewGridMode.DetailCard => GetDetailCardPreferredHeight(),
            ViewGridMode.GroupCard => Math.Max(150, Math.Max(TilePreferredHeight, LargeCardPreferredHeight - 20)),
            ViewGridMode.Kanban => Math.Max(168, Math.Max(TilePreferredHeight, LargeCardPreferredHeight)),
            ViewGridMode.Timeline => Math.Max(132, TilePreferredHeight),
            ViewGridMode.LargeCard => Math.Max(LargeCardPreferredHeight, Math.Max(132, TilePreferredHeight)),
            _ => 20
        };
    }

    private int GetDetailCardPreferredHeight()
    {
        int visibleCount = Columns.VisibleColumns.Count(c => c.Width > 0);
        int lineHeight = Math.Max(17, TextRenderer.MeasureText("Ag", Font, Size.Empty, TextFormatFlags.NoPadding).Height + 2);
        int headerHeight = 38;
        int padding = 22;
        int footerReserve = 8;
        int textHeight = headerHeight + padding + footerReserve + Math.Max(1, visibleCount) * lineHeight;
        if (DetailCardLayout is ViewGridDetailCardLayout.Media or ViewGridDetailCardLayout.PosterLeft)
            return Math.Max(Math.Max(150, textHeight), Math.Max(150, DetailCardMediaImageHeight + 54));
        return Math.Max(150, textHeight);
    }

    private int CoerceRowHeightForCurrentView(int value)
    {
        int rowHeight = Math.Max(20, value);
        if (EnforceTilePreferredHeight && IsTileView)
            rowHeight = Math.Max(rowHeight, GetMinimumRowHeightForCurrentTileView());
        if (!IsTileView)
        {
            if (LockDetailsRowHeightOnSelection && !AutoRowHeightForMultilineCells)
                return Math.Max(20, _detailsRowHeight);
            if (AllowMultilineCells && AutoRowHeightForMultilineCells)
                rowHeight = Math.Max(rowHeight, GetMinimumMultilineRowHeight());
        }
        return rowHeight;
    }

    private int GetMinimumMultilineRowHeight()
    {
        int lineHeight = TextRenderer.MeasureText("Ag", Font, Size.Empty, TextFormatFlags.NoPadding).Height;
        return Math.Max(28, 8 + Math.Max(1, MaxCellTextLines) * Math.Max(14, lineHeight));
    }

    private int GetTileWidth()
    {
        int preferred = ViewMode switch
        {
            ViewGridMode.Poster => Math.Max(160, PosterPreferredWidth),
            ViewGridMode.MediaTile => Math.Max(156, TilePreferredWidth),
            ViewGridMode.Gallery => Math.Max(180, TilePreferredWidth),
            ViewGridMode.FilmStrip => Math.Max(260, TilePreferredWidth),
            ViewGridMode.ExtraLargeIcons => TilePosterMode ? 160 : 270,
            ViewGridMode.LargeIcons => TilePosterMode ? 150 : 240,
            ViewGridMode.MediumIcons => TilePosterMode ? 140 : 210,
            ViewGridMode.Tile => TilePreferredWidth,
            ViewGridMode.IconGrid => Math.Max(160, TilePreferredWidth - 30),
            ViewGridMode.DashboardCard => Math.Max(320, TilePreferredWidth + 120),
            ViewGridMode.KpiDashboard => Math.Max(220, TilePreferredWidth),
            ViewGridMode.HeatMap => Math.Max(160, TilePreferredWidth - 20),
            ViewGridMode.MiniChart => Math.Max(300, TilePreferredWidth + 80),
            ViewGridMode.RowPreview => Math.Max(560, LargeCardPreferredWidth),
            ViewGridMode.RowCard => Math.Max(600, LargeCardPreferredWidth),
            ViewGridMode.PropertyCard => Math.Max(600, ClientSize.Width - VBarWidth - 12),
            ViewGridMode.DetailCard => Math.Max(600, ClientSize.Width - VBarWidth - 12),
            ViewGridMode.GroupCard => Math.Max(300, TilePreferredWidth + 90),
            ViewGridMode.Kanban => Math.Max(300, TilePreferredWidth + 90),
            ViewGridMode.Timeline => Math.Max(620, LargeCardPreferredWidth),
            ViewGridMode.LargeCard => Math.Max(LargeCardPreferredWidth, TilePreferredWidth + 140),
            _ => TilePreferredWidth
        };

        if (AutoSizeTileWidthToContent)
        {
            int visibleWidth = Columns.VisibleColumns
                .Take(TileShowAllVisibleTextColumns ? int.MaxValue : 3)
                .Sum(c => Math.Max(48, c.Width));

            if (visibleWidth > 0)
                preferred = Math.Max(preferred, Math.Min(420, visibleWidth + 36));
        }

        return Math.Max(120, preferred);
    }

    private int GetTilesPerRow()
    {
        if (!IsTileView) return 1;
        if (ViewMode is ViewGridMode.RowCard or ViewGridMode.RowPreview or ViewGridMode.DetailCard or ViewGridMode.PropertyCard or ViewGridMode.Timeline or ViewGridMode.FilmStrip) return 1;
        int available = Math.Max(1, ClientSize.Width - VBarWidth - 8);
        int min = Math.Max(120, GetTileWidth());
        return Math.Max(1, available / min);
    }

    private int GetVisualRowCount()
        => IsTileView ? Math.Max(0, (int)Math.Ceiling(ViewCount / (double)Math.Max(1, GetTilesPerRow()))) : ViewCount;

    private void UpdateScrollbars()
    {
        RefreshView();
    }

    public void RefreshView()
    {
        // v25.83: ViewGrid uyumlu CheckBoxes modunda eski/boş selector kolonları
        // runtime'da tekrar görünmesin; ilk gerçek kolon host olarak kalsın.
        if (_checkBoxes)
            NormalizeCompatibilityCheckBoxColumns();

        int contentTop = GetRowsTopOffset();
        int footer = ShowSummaryFooter ? FooterHeight : 0;
        int hbarHeight = _hbar.Visible ? _hbar.Height : SystemInformation.HorizontalScrollBarHeight;
        int visibleRows = Math.Max(1, (Height - contentTop - footer - hbarHeight) / RowHeight);
        int visualRows = GetVisualRowCount();
        if (AutoHideScrollBarsWhenNotNeeded)
            _vbar.Visible = visualRows > visibleRows;
        else
            _vbar.Visible = true;
        _vbar.Maximum = Math.Max(0, visualRows - 1);
        _vbar.LargeChange = visibleRows;
        _vbar.SmallChange = 1;
        _vbar.Value = Math.Min(_vbar.Value, Math.Max(0, _vbar.Maximum - visibleRows + 1));
        _scrollY = _vbar.Value;

        int totalWidth = IsTileView ? Math.Max(0, ClientSize.Width - VBarWidth) : Columns.VisibleColumns.Sum(c => c.Width);
        int availableWidth = Math.Max(0, ClientSize.Width - VBarWidth);
        _hbar.Visible = !IsTileView && totalWidth > availableWidth;
        _hbar.Maximum = Math.Max(0, totalWidth - 1);
        _hbar.LargeChange = Math.Max(1, availableWidth);
        _hbar.SmallChange = 32;
        _hbar.Value = Math.Min(_hbar.Value, Math.Max(0, _hbar.Maximum - _hbar.LargeChange + 1));
        _scrollX = _hbar.Visible ? _hbar.Value : 0;
        PositionRowDetailsControl();
        RefreshCardViewFilterUx();
        Invalidate();
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); AutoSizeFillColumns(); PositionRowDetailsControl(); RefreshCardViewFilterUx(); RefreshView(); }

    private void ApplyAutoFitAfterDataChanged()
    {
        if (!AutoFitFillColumnsOnDataLoad || IsTileView || IsDisposed) return;

        // GLV'deki kullanım hissi: veri yüklendikten sonra kolonlar ilk çizimde
        // kontrol genişliğine otursun; kullanıcının formu büyütüp/küçültmesini beklemesin.
        AutoSizeFillColumns();
        RefreshView();
    }

    private void AutoSizeFillColumns()
    {
        var visible = Columns.VisibleColumns.ToList();
        var fills = visible.Where(c => c.FillFreeSpace).ToList();
        if (fills.Count == 0 || visible.Count == 0) return;

        int availableWidth = Math.Max(0, ClientSize.Width - (_vbar.Visible ? _vbar.Width : VBarWidth) - 2);
        int fixedWidth = visible.Where(c => !c.FillFreeSpace).Sum(c => Math.Max(0, c.Width));
        int free = availableWidth - fixedWidth;

        if (free <= 0)
        {
            // Sabit kolonlara dokunma. Sadece FillFreeSpace kolonları gerekirse kendi
            // MinimumWidth değerine kadar küçülür. Böylece log ekranlarında Width=300
            // verilen tarih/user kolonu veri geldikçe daralmaz.
            foreach (var c in fills)
            {
                int min = Math.Max(40, c.MinimumWidth > 0 ? c.MinimumWidth : 40);
                c.Width = Math.Max(min, Math.Min(c.Width, AutoResizeMaxWidth));
            }
            return;
        }

        int totalWeight = fills.Sum(c => Math.Max(1, c.FreeSpaceProportion > 0 ? c.FreeSpaceProportion : 1));
        int used = 0;
        for (int i = 0; i < fills.Count; i++)
        {
            var c = fills[i];
            int weight = Math.Max(1, c.FreeSpaceProportion > 0 ? c.FreeSpaceProportion : 1);
            int min = Math.Max(40, c.MinimumWidth > 0 ? c.MinimumWidth : 40);
            int w = i == fills.Count - 1
                ? Math.Max(min, free - used)
                : Math.Max(min, (int)Math.Round(free * (weight / (double)totalWeight)));
            c.Width = w;
            used += w;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var paintWatch = EnablePaintPerformanceMetrics ? System.Diagnostics.Stopwatch.StartNew() : null;
        if (_checkBoxes)
            NormalizeCompatibilityCheckBoxColumns();

        try
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DrawFluentBackground(g);
            DrawHeader(g);
            DrawRows(g);
            if (IsSorting && ShowSortBusyIndicator) DrawSortBusyIndicator(g);
            if (ShowSummaryFooter) DrawSummaryFooter(g);
            if (ShowColumnReorderPreview) DrawColumnDragPreview(g);
            if (ViewCount == 0 && ShowEmptyListMessage) DrawEmpty(g);
            using var pen = new Pen(_theme.BorderColor);
            g.DrawRectangle(pen, 0,0,Width-1,Height-1);
        }
        finally
        {
            if (paintWatch != null)
            {
                paintWatch.Stop();
                RecordPaintPerformance(paintWatch.Elapsed.TotalMilliseconds);
            }
        }
    }

    private void DrawSortBusyIndicator(Graphics g)
    {
        if (!ShowSortBusyIndicator) return;
        var title = string.IsNullOrWhiteSpace(SortBusyTitle) ? "Sıralanıyor..." : SortBusyTitle;
        var detail = string.IsNullOrWhiteSpace(SortBusyDetail)
            ? "Lütfen bekleyin, liste arka planda hazırlanıyor."
            : SortBusyDetail;

        int overlayWidth = Math.Min(Math.Max(360, SortBusyOverlayWidth), Math.Max(220, Width - 32));
        int overlayHeight = Math.Min(Math.Max(118, SortBusyOverlayHeight), Math.Max(96, Height - HeaderHeight - 24));
        var r = new Rectangle(
            Math.Max(8, (Width - VBarWidth - overlayWidth) / 2),
            Math.Max(HeaderHeight + 10, HeaderHeight + (Height - HeaderHeight - overlayHeight) / 3),
            overlayWidth,
            overlayHeight);

        using var dim = new SolidBrush(Color.FromArgb(SortBusyDimOpacity, _theme.IsDark ? Color.Black : Color.White));
        g.FillRectangle(dim, new Rectangle(0, HeaderHeight, Math.Max(0, Width - VBarWidth), Math.Max(0, Height - HeaderHeight)));

        if (EnableSoftShadows) DrawSoftShadow(g, Rectangle.Inflate(r, 4, 4), 12);

        using var b = new SolidBrush(Color.FromArgb(245, _theme.PanelBackColor));
        using var p = new Pen(_theme.AccentColor, 3f);
        g.FillRoundedRectangle(b, r, 12);
        g.DrawRoundedRectangle(p, r, 12);

        var spinner = new Rectangle(r.Left + 24, r.Top + (r.Height - 52) / 2, 52, 52);
        DrawSortBusySpinner(g, spinner);

        using var titleFont = new Font(Font.FontFamily, Math.Max(Font.Size + 2f, 11f), FontStyle.Bold);
        var titleRect = new Rectangle(spinner.Right + 18, r.Top + 22, Math.Max(20, r.Right - spinner.Right - 32), 32);
        var detailRect = new Rectangle(titleRect.Left, titleRect.Bottom + 3, titleRect.Width, Math.Max(20, r.Bottom - titleRect.Bottom - 12));
        TextRenderer.DrawText(g, title, titleFont, titleRect, _theme.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, detail, Font, detailRect, Blend(_theme.ForeColor, _theme.PanelBackColor, 0.35), TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
    }

    private void DrawSortBusySpinner(Graphics g, Rectangle r)
    {
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var back = new Pen(Blend(_theme.AccentColor, _theme.PanelBackColor, 0.70), 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var fore = new Pen(_theme.AccentColor, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(back, r, 0, 360);
        int tick = Environment.TickCount & int.MaxValue;
        int start = (tick / 4) % 360;
        g.DrawArc(fore, r, start, 92);
        g.SmoothingMode = old;
    }

    private void DrawHeader(Graphics g)
    {
        if (!ShowHeader) return;
        var headerRect = new Rectangle(0, 0, Width - VBarWidth, HeaderHeight);
        Color headerBack = GetEffectiveHeaderBackColor();
        using (var b = new SolidBrush(headerBack))
            g.FillRectangle(b, headerRect);
        if (_theme.UseFluentBackdrop && EnableAcrylicSimulation)
            DrawAcrylicGloss(g, headerRect);
        UpdateAllCompatibilityHeaderCheckStates();
        int x = -_scrollX;
        foreach (var c in Columns.VisibleColumns)
        {
            var r = new Rectangle(x, 0, c.Width, HeaderHeight);
            if (r.Right < 0) { x += c.Width; continue; }
            if (r.Left > Width - VBarWidth) break;
            DrawColumnHeaderContent(g, r, c);
            DrawColumnFilterButton(g, r, c);
            DrawColumnSortGlyph(g, r, c);
            using var p = new Pen(_theme.GridColor); g.DrawLine(p, r.Right-1, 0, r.Right-1, Height); g.DrawLine(p, r.Left, HeaderHeight-1, r.Right, HeaderHeight-1);
            x += c.Width;
        }
    }


    private void DrawColumnHeaderContent(Graphics g, Rectangle r, ViewGridColumn col)
    {
        if (r.Width <= 0 || r.Height <= 0) return;

        Color back = GetEffectiveHeaderBackColor(col);
        Color fore = GetEffectiveHeaderForeColor(back, col);

        using (var b = new SolidBrush(back))
            g.FillRectangle(b, r);

        var content = Rectangle.Inflate(r, -6, -3);

        // Sort/filter ikonları sağda kalır; başlık metni ve header checkbox
        // bu ikonların altına girmez. ListView'deki gibi header içeriği solda,
        // ikonlar sağda bağımsız davranır.
        int reservedRight = 6;
        if (ShouldShowColumnFilterButton(col))
            reservedRight += ColumnFilterButtonWidth + 4;
        if (ShowColumnSortGlyphs && ReferenceEquals(_sortColumn, col))
            reservedRight += 22;
        content.Width = Math.Max(0, r.Right - reservedRight - content.Left);

        if (content.Width <= 0 || content.Height <= 0) return;

        if (col.HeaderCheckBox)
        {
            var cb = GetHeaderCheckBoxRect(col);
            if (cb.Width > 0 && cb.Height > 0)
            {
                DrawModernHeaderCheckBox(g, cb, col.HeaderCheckState, col.HeaderCheckBoxDisabled);
                content.X = Math.Max(content.X, cb.Right + 6);
                content.Width = Math.Max(0, r.Right - reservedRight - content.X);
            }
        }

        int imageSize = Math.Max(8, Math.Min(64, col.HeaderImageSize));
        Rectangle imageRect = Rectangle.Empty;
        Image? headerImage = ResolveHeaderImage(col);
        if (headerImage != null && content.Width > imageSize + 2)
        {
            int iy = content.Top + Math.Max(0, (content.Height - imageSize) / 2);
            int ix = col.HeaderImageAlign switch
            {
                ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => content.Right - imageSize,
                ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => content.Left + Math.Max(0, (content.Width - imageSize) / 2),
                _ => content.Left
            };
            imageRect = new Rectangle(ix, iy, imageSize, imageSize);
            g.DrawImage(headerImage, imageRect);

            if (col.HeaderImageBeforeText)
            {
                content.X = imageRect.Right + 4;
                content.Width = Math.Max(0, r.Right - content.X - 6);
            }
            else
            {
                content.Width = Math.Max(0, imageRect.Left - content.Left - 4);
            }
        }

        string text = col.Header ?? string.Empty;
        if (IsCompatibilityCheckBoxHostColumn(col) && string.IsNullOrWhiteSpace(text))
            text = col.AspectName ?? string.Empty;
        if (string.IsNullOrEmpty(text) || content.Width <= 0 || content.Height <= 0) return;

        bool vertical = col.HeaderTextVertical || (col.HeaderTextAngle % 360 != 0);
        if (vertical)
        {
            DrawRotatedHeaderText(g, text, col.HeaderFont ?? Font, content, fore, col.HeaderTextAngle == 0 ? -90 : col.HeaderTextAngle);
            return;
        }

        TextRenderer.DrawText(g, text, col.HeaderFont ?? Font, content, fore, HeaderTextFlags(col.HeaderTextAlign));
    }

    private void DrawModernHeaderCheckBox(Graphics g, Rectangle bounds, CheckState state, bool disabled)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color border = disabled ? Blend(_theme.BorderColor, _theme.BackColor, 0.45) : Blend(_theme.BorderColor, _theme.AccentColor, 0.28);
        Color fill = state == CheckState.Unchecked
            ? Blend(GetEffectiveHeaderBackColor(), _theme.BackColor, _theme.IsDark ? 0.12 : 0.04)
            : _theme.AccentColor;
        Color glyph = state == CheckState.Unchecked
            ? Color.Transparent
            : GetReadableTextColor(fill);

        Rectangle box = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        using (var b = new SolidBrush(fill)) g.FillRoundedRectangle(b, box, 4);
        using (var p = new Pen(border)) g.DrawRoundedRectangle(p, box, 4);

        if (state == CheckState.Checked)
        {
            using var checkPen = new Pen(glyph, Math.Max(2, bounds.Width / 7)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            var a = new Point(bounds.Left + bounds.Width / 4, bounds.Top + bounds.Height / 2);
            var b = new Point(bounds.Left + bounds.Width / 2 - 1, bounds.Bottom - bounds.Height / 4);
            var c = new Point(bounds.Right - bounds.Width / 5, bounds.Top + bounds.Height / 4);
            g.DrawLines(checkPen, new[] { a, b, c });
        }
        else if (state == CheckState.Indeterminate)
        {
            using var dashPen = new Pen(glyph, Math.Max(2, bounds.Width / 6)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            int y = bounds.Top + bounds.Height / 2;
            g.DrawLine(dashPen, bounds.Left + bounds.Width / 4, y, bounds.Right - bounds.Width / 4, y);
        }

        g.SmoothingMode = old;
    }

    private Rectangle GetHeaderCheckBoxRect(ViewGridColumn col)
    {
        if (col == null || !ShowHeader || !col.Visible) return Rectangle.Empty;
        int left = GetColumnLeft(col);
        var r = new Rectangle(left, 0, col.Width, HeaderHeight);
        int size = Math.Min(18, Math.Max(13, HeaderHeight - 12));
        int x = r.Left + 6;
        int y = r.Top + Math.Max(0, (r.Height - size) / 2);
        if (x + size > r.Right) return Rectangle.Empty;
        return new Rectangle(x, y, size, size);
    }

    private Rectangle GetRowCompatibilityCheckBoxRect(Rectangle cellBounds)
    {
        int size = Math.Min(18, Math.Max(13, RowHeight - 10));
        int x = cellBounds.Left + 6;
        int y = cellBounds.Top + Math.Max(0, (cellBounds.Height - size) / 2);
        return new Rectangle(x, y, size, size);
    }

    private void ToggleHeaderCheckBox(ViewGridColumn col)
    {
        if (col == null || col.HeaderCheckBoxDisabled) return;

        // Header state can become stale after bulk operations, filtering or internal per-column checks.
        // Recalculate before deciding the next state so repeated header clicks can reliably
        // check all -> uncheck all without requiring a row-level click in between.
        UpdateCompatibilityHeaderCheckState(col);

        // User click behavior must be two-state even when the header can DISPLAY
        // an indeterminate state. Indeterminate is a calculated visual state that means
        // some rows are checked and some are not; clicking it should select all.
        // Previous behavior cycled Checked -> Indeterminate, which did not update rows
        // and made the next "uncheck all" click appear broken until a row checkbox was
        // manually changed.
        CheckState next = col.HeaderCheckState == CheckState.Checked
            ? CheckState.Unchecked
            : CheckState.Checked;

        col.HeaderCheckState = next;

        // Excel/ListView benzeri davranış: header checkbox, mevcut filtrelenmiş görünümdeki satırları değiştirir.
        // Çok büyük sanal providerlarda sadece provider'ın döndürdüğü view indexleri üzerinde çalışır; ağır ekstra tarama yapmaz.
        bool value = next == CheckState.Checked;
        if ((col.Kind == ViewGridColumnKind.CheckBox || col.CellCheckBox || IsCompatibilityCheckBoxHostColumn(col)) && col.HeaderCheckBoxUpdatesRowCheckBoxes && next != CheckState.Indeterminate)
        {
            if (_viewIsDirect && _provider is IBulkCheckStateProvider bulk && bulk.TrySetAllCheckStates(col, value ? CheckState.Checked : CheckState.Unchecked))
            {
                InvalidateDataCaches(keepVersion: true);
                CellValueChanged?.Invoke(this, CreateHeaderCheckBoxEditEventArgs(col, value));
            }
            else
            {
                var indexes = _displayRows.Count > 0
                    ? _displayRows.Where(d => !d.IsGroup).Select(d => d.RealIndex).ToList()
                    : (_viewIsDirect ? Enumerable.Range(0, Math.Min(ViewCount, MaxHeaderCheckBoxScanRows)).ToList() : _viewIndexes.ToList());

                foreach (int realIndex in indexes)
                {
                    var row = _provider.GetRow(realIndex);
                    if (row == null) continue;
                    try { SetRowCheckState(row, col, value ? CheckState.Checked : CheckState.Unchecked); }
                    catch { }
                }

                InvalidateDataCaches(keepVersion: true);
                CellValueChanged?.Invoke(this, CreateHeaderCheckBoxEditEventArgs(col, value));
            }
        }

        UpdateCompatibilityHeaderCheckState(col);
        Invalidate();
    }

    private ViewGridCellEditEventArgs CreateHeaderCheckBoxEditEventArgs(ViewGridColumn col, object? newValue)
    {
        int rowIndex = -1;
        object? rowObject = null;

        try
        {
            if (_selectedRow >= 0 && _selectedRow < ViewCount)
            {
                rowIndex = _selectedRow;
                rowObject = GetViewRow(_selectedRow);
            }

            if (rowObject == null)
            {
                int firstRealIndex = -1;

                if (_displayRows.Count > 0)
                {
                    foreach (var displayRow in _displayRows)
                    {
                        if (!displayRow.IsGroup)
                        {
                            firstRealIndex = displayRow.RealIndex;
                            break;
                        }
                    }
                }
                else if (_viewIndexes.Count > 0)
                {
                    firstRealIndex = _viewIndexes[0];
                }
                else if (ViewCount > 0)
                {
                    firstRealIndex = 0;
                }

                if (firstRealIndex >= 0)
                {
                    rowObject = _provider.GetRow(firstRealIndex);
                    rowIndex = IndexOfObject(rowObject);
                }
            }
        }
        catch
        {
            rowIndex = -1;
            rowObject = null;
        }

        return new ViewGridCellEditEventArgs(rowIndex, rowObject!, col, newValue);
    }

    private void UpdateCompatibilityHeaderCheckState(ViewGridColumn? col)
    {
        if (col == null || !col.HeaderCheckBox) return;
        if (!(col.Kind == ViewGridColumnKind.CheckBox || col.CellCheckBox || IsCompatibilityCheckBoxHostColumn(col))) return;

        int checkedCount = 0;
        int uncheckedCount = 0;

        if (_viewIsDirect && _provider is IBulkCheckStateProvider bulk && bulk.TryGetCheckStateSummary(col, out checkedCount, out uncheckedCount))
        {
            col.HeaderCheckState = checkedCount > 0 && uncheckedCount > 0
                ? CheckState.Indeterminate
                : checkedCount > 0 ? CheckState.Checked : CheckState.Unchecked;
            return;
        }

        var indexes = _displayRows.Count > 0
            ? _displayRows.Where(d => !d.IsGroup).Select(d => d.RealIndex)
            : (_viewIsDirect ? Enumerable.Range(0, Math.Min(ViewCount, MaxHeaderCheckBoxScanRows)) : _viewIndexes.AsEnumerable());

        foreach (int realIndex in indexes)
        {
            var row = _provider.GetRow(realIndex);
            if (row == null) continue;
            var state = GetRowCheckState(row, col);
            if (state == CheckState.Checked) checkedCount++;
            else uncheckedCount++;
            if (checkedCount > 0 && uncheckedCount > 0)
            {
                col.HeaderCheckState = CheckState.Indeterminate;
                return;
            }
        }

        col.HeaderCheckState = checkedCount > 0 && uncheckedCount == 0
            ? CheckState.Checked
            : CheckState.Unchecked;
    }

    private void UpdateAllCompatibilityHeaderCheckStates()
    {
        foreach (var col in Columns.VisibleColumns)
            UpdateCompatibilityHeaderCheckState(col);
    }

    private Color GetEffectiveHeaderBackColor(ViewGridColumn? col = null)
    {
        Color themeBack = UseUnifiedThemeVisuals
            ? GetSurfaceColor(_theme.TextBackColor, 0.06)
            : _theme.TextBackColor;

        if (col == null) return themeBack;
        if (AutoApplyThemeToColumnHeaders || col.HeaderBackColor.IsEmpty || col.HeaderBackColor == Color.Transparent)
            return themeBack;

        return col.HeaderBackColor;
    }

    private Color GetEffectiveHeaderForeColor(Color headerBack, ViewGridColumn? col = null)
    {
        Color preferred = (col == null || AutoApplyThemeToColumnHeaders || col.HeaderForeColor.IsEmpty || col.HeaderForeColor == Color.Transparent)
            ? _theme.TextForeColor
            : col.HeaderForeColor;

        return EnsureReadableTextOn(headerBack, preferred);
    }

    private Color GetThemeGlyphColor(Color headerBack, bool active)
    {
        Color preferred = active ? _theme.AccentColor : _theme.MutedForeColor;
        return active ? EnsureVisibleAccent(preferred, headerBack) : EnsureReadableTextOn(headerBack, preferred);
    }

    private void DrawRotatedHeaderText(Graphics g, string text, Font font, Rectangle bounds, Color color, int angle)
    {
        if (string.IsNullOrEmpty(text) || bounds.Width <= 0 || bounds.Height <= 0) return;

        var state = g.Save();
        try
        {
            g.TranslateTransform(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
            g.RotateTransform(angle);
            var rotated = new Rectangle(-bounds.Height / 2, -bounds.Width / 2, bounds.Height, bounds.Width);
            TextRenderer.DrawText(g, text, font, rotated, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
        finally
        {
            g.Restore(state);
        }
    }

    private static TextFormatFlags HeaderTextFlags(ContentAlignment alignment)
    {
        TextFormatFlags flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;

        flags |= alignment switch
        {
            ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => TextFormatFlags.Left,
            ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter,
            ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => TextFormatFlags.Right,
            _ => TextFormatFlags.Left
        };

        flags |= alignment switch
        {
            ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => TextFormatFlags.Top,
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => TextFormatFlags.Bottom,
            _ => TextFormatFlags.VerticalCenter
        };

        return flags;
    }


    private bool ShouldShowColumnFilterButton(ViewGridColumn col)
        => ShowHeader && ShowFilterMenu && ShowColumnFilterButtons && col != null && col.Filterable && col.Visible;

    private Rectangle GetColumnFilterButtonRect(Rectangle headerBounds, ViewGridColumn col)
    {
        if (!ShouldShowColumnFilterButton(col) || headerBounds.Width < 34) return Rectangle.Empty;
        int w = Math.Max(18, Math.Min(34, ColumnFilterButtonWidth));
        return new Rectangle(headerBounds.Right - w - 3, headerBounds.Top + 5, w, Math.Max(16, headerBounds.Height - 10));
    }

    private Rectangle GetColumnFilterButtonRect(ViewGridColumn col)
    {
        if (col == null || !col.Visible) return Rectangle.Empty;
        int left = GetColumnLeft(col);
        return GetColumnFilterButtonRect(new Rectangle(left, 0, col.Width, HeaderHeight), col);
    }

    private void DrawColumnFilterButton(Graphics g, Rectangle headerBounds, ViewGridColumn col)
    {
        var r = GetColumnFilterButtonRect(headerBounds, col);
        if (r.IsEmpty) return;
        bool active = _filters.Get(col.AspectName) != null;
        bool hot = r.Contains(PointToClient(MousePosition));
        Color headerBack = GetEffectiveHeaderBackColor(col);
        Color back = active ? Blend(_theme.AccentColor, headerBack, _theme.IsDark ? 0.35 : 0.55)
                            : (hot ? GetSurfaceColor(headerBack, 0.10) : Color.Transparent);
        if (back != Color.Transparent)
        {
            using var b = new SolidBrush(back);
            g.FillRoundedRectangle(b, r, 5);
        }

        var style = col.FilterIconStyle == ViewGridFilterIconStyle.Inherit ? FilterIconStyle : col.FilterIconStyle;
        var customIcon = col.FilterIcon ?? CustomFilterIcon;
        if (style == ViewGridFilterIconStyle.CustomImage && customIcon != null)
        {
            DrawHeaderIconImage(g, customIcon, r);
        }
        else if (style == ViewGridFilterIconStyle.Dot)
        {
            using var b = new SolidBrush(GetThemeGlyphColor(headerBack, active));
            int d = active ? 8 : 6;
            g.FillEllipse(b, r.Left + (r.Width - d) / 2, r.Top + (r.Height - d) / 2, d, d);
            if (active)
            {
                using var p = new Pen(GetThemeGlyphColor(headerBack, true), 1.4f);
                g.DrawEllipse(p, r.Left + (r.Width - 13) / 2, r.Top + (r.Height - 13) / 2, 13, 13);
            }
        }
        else
        {
            using var pen = new Pen(GetThemeGlyphColor(headerBack, active), active ? 1.6f : 1f);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = r.Left + r.Width / 2;
            int top = r.Top + Math.Max(4, r.Height / 4);
            var funnel = new[]
            {
                new Point(cx - 6, top), new Point(cx + 6, top),
                new Point(cx + 2, top + 6), new Point(cx + 2, top + 11),
                new Point(cx - 2, top + 13), new Point(cx - 2, top + 6)
            };
            g.DrawPolygon(pen, funnel);
        }

        if (active && style != ViewGridFilterIconStyle.Dot)
        {
            using var b = new SolidBrush(GetThemeGlyphColor(headerBack, true));
            g.FillEllipse(b, r.Right - 8, r.Top + 4, 5, 5);
        }
    }

    private void DrawColumnSortGlyph(Graphics g, Rectangle headerBounds, ViewGridColumn col)
    {
        if (!ShowColumnSortGlyphs || _sortColumn != col || col == null || !col.Visible) return;
        int rightPadding = ShouldShowColumnFilterButton(col) ? ColumnFilterButtonWidth + 9 : 6;
        var r = new Rectangle(headerBounds.Right - rightPadding - 18, headerBounds.Top + 7, 16, Math.Max(14, headerBounds.Height - 14));
        if (r.Width <= 0 || r.Height <= 0) return;

        var style = col.SortGlyphStyle == ViewGridSortGlyphStyle.Inherit ? SortGlyphStyle : col.SortGlyphStyle;
        var customIcon = _sortDesc ? (col.SortDescendingIcon ?? CustomSortDescendingIcon) : (col.SortAscendingIcon ?? CustomSortAscendingIcon);
        if (style == ViewGridSortGlyphStyle.CustomImage && customIcon != null)
        {
            DrawHeaderIconImage(g, customIcon, r);
            return;
        }

        Color headerBack = GetEffectiveHeaderBackColor(col);
        Color glyphColor = GetThemeGlyphColor(headerBack, true);
        using var pen = new Pen(glyphColor, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(glyphColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int cx = r.Left + r.Width / 2;
        int cy = r.Top + r.Height / 2;
        if (style == ViewGridSortGlyphStyle.Triangle)
        {
            var pts = _sortDesc
                ? new[] { new Point(cx - 5, cy - 3), new Point(cx + 5, cy - 3), new Point(cx, cy + 4) }
                : new[] { new Point(cx - 5, cy + 3), new Point(cx + 5, cy + 3), new Point(cx, cy - 4) };
            g.FillPolygon(brush, pts);
            return;
        }

        if (_sortDesc)
        {
            g.DrawLine(pen, cx - 5, cy - 3, cx, cy + 3);
            g.DrawLine(pen, cx, cy + 3, cx + 5, cy - 3);
        }
        else
        {
            g.DrawLine(pen, cx - 5, cy + 3, cx, cy - 3);
            g.DrawLine(pen, cx, cy - 3, cx + 5, cy + 3);
        }
    }

    private static void DrawHeaderIconImage(Graphics g, Image image, Rectangle bounds)
    {
        int size = Math.Min(Math.Min(bounds.Width, bounds.Height), 18);
        if (size <= 0) return;
        var r = new Rectangle(bounds.Left + (bounds.Width - size) / 2, bounds.Top + (bounds.Height - size) / 2, size, size);
        g.DrawImage(image, r);
    }

    private void DrawColumnDragPreview(Graphics g)
    {
        if (!_dragColumnActive || _dragColumnInsertIndex < 0 || IsTileView || !ShowHeader) return;
        int x = GetColumnInsertX(_dragColumnInsertIndex);
        using var p = new Pen(_theme.AccentColor, 2);
        g.DrawLine(p, x, 1, x, Math.Max(HeaderHeight, Height - (_hbar.Visible ? _hbar.Height : 0) - 1));
        var tri = new[] { new Point(x - 6, HeaderHeight - 7), new Point(x + 6, HeaderHeight - 7), new Point(x, HeaderHeight - 1) };
        using var b = new SolidBrush(_theme.AccentColor);
        g.FillPolygon(b, tri);
    }

    private Color GetThemeAwareRowBack(object row, int viewIndex, Color currentBack)
    {
        if (RowColorPreset == ViewGridRowColorPreset.ThemeDefault)
            return currentBack;

        double strength = Math.Max(0.02, Math.Min(0.60, RowColorStrength));
        string key = GetRowColorKey(row);
        Color accent = GetSemanticAccent(key, viewIndex);

        return RowColorPreset switch
        {
            ViewGridRowColorPreset.SubtleZebra => viewIndex % 2 == 1
                ? Blend(_theme.AlternateBackColor, _theme.AccentColor, _theme.IsDark ? 0.06 : 0.035)
                : currentBack,
            ViewGridRowColorPreset.SoftAccent => Blend(currentBack, _theme.AccentColor, _theme.IsDark ? strength * 0.75 : strength * 0.55),
            ViewGridRowColorPreset.AOIRisk => string.IsNullOrWhiteSpace(key) ? currentBack : Blend(currentBack, accent, _theme.IsDark ? strength * 1.10 : strength),
            ViewGridRowColorPreset.StatusPills => string.IsNullOrWhiteSpace(key) ? currentBack : Blend(currentBack, accent, _theme.IsDark ? strength * 0.85 : strength * 0.70),
            ViewGridRowColorPreset.SeverityBands => Blend(currentBack, accent, _theme.IsDark ? strength * 1.20 : strength * 0.95),
            ViewGridRowColorPreset.PastelCards => Blend(currentBack, accent, _theme.IsDark ? strength * 0.65 : strength * 0.45),
            ViewGridRowColorPreset.FocusGlow => viewIndex == _hotRow
                ? Blend(currentBack, _theme.AccentColor, _theme.IsDark ? 0.22 : 0.14)
                : Blend(currentBack, _theme.AccentColor, _theme.IsDark ? 0.045 : 0.025),
            _ => currentBack
        };
    }

    private string GetRowColorKey(object row)
    {
        var col = Columns.FirstOrDefault(c => string.Equals(c.AspectName, RowColorAspectName, StringComparison.OrdinalIgnoreCase));
        object? value = col?.GetValue(row);
        if (value == null && !string.IsNullOrWhiteSpace(RowColorAspectName))
        {
            if (row is IDictionary<string, object?> dict && dict.TryGetValue(RowColorAspectName, out var v)) value = v;
            else value = row.GetType().GetProperty(RowColorAspectName)?.GetValue(row);
        }
        return Convert.ToString(value)?.Trim() ?? string.Empty;
    }

    private Color GetSemanticAccent(string key, int viewIndex)
    {
        string k = (key ?? string.Empty).Trim().ToLowerInvariant();
        if (k.Contains("fail") || k.Contains("hata") || k.Contains("red") || k.Contains("kritik") || k.Contains("critical"))
            return _theme.IsDark ? Color.FromArgb(235, 95, 95) : Color.FromArgb(220, 48, 48);
        if (k.Contains("review") || k.Contains("false") || k.Contains("warning") || k.Contains("uyarı"))
            return _theme.IsDark ? Color.FromArgb(178, 132, 245) : Color.FromArgb(128, 104, 176);
        if (k.Contains("ok") || k.Contains("pass") || k.Contains("başar") || k.Contains("green"))
            return _theme.IsDark ? Color.FromArgb(77, 190, 132) : Color.FromArgb(30, 150, 92);
        if (k.Contains("wait") || k.Contains("pending") || k.Contains("bekle") || k.Contains("hold"))
            return _theme.IsDark ? Color.FromArgb(235, 178, 75) : Color.FromArgb(214, 132, 54);

        Color[] palette = _theme.IsDark
            ? new[] { Color.FromArgb(90, 150, 230), Color.FromArgb(178, 132, 245), Color.FromArgb(77, 190, 132), Color.FromArgb(235, 178, 75), Color.FromArgb(235, 110, 145), Color.FromArgb(0, 180, 216) }
            : new[] { Color.FromArgb(59, 130, 246), Color.FromArgb(128, 104, 176), Color.FromArgb(18, 148, 96), Color.FromArgb(212, 132, 54), Color.FromArgb(214, 90, 138), Color.FromArgb(0, 132, 176) };
        int hash = string.IsNullOrWhiteSpace(k) ? viewIndex : Math.Abs(k.GetHashCode());
        return palette[hash % palette.Length];
    }

    private void DrawRows(Graphics g)
    {
        if (IsTileView)
        {
            DrawTileRows(g);
            return;
        }

        int top = GetRowsTopOffset();
        int first = _scrollY;
        int footer = ShowSummaryFooter ? FooterHeight : 0;
        int hbarHeight = _hbar.Visible ? _hbar.Height : 0;
        int visible = Math.Max(1, (Height - top - footer - hbarHeight) / RowHeight + 2);
        if (_provider is IAsyncPreloadRowProvider preloadProvider)
            preloadProvider.RequestPreload(Math.Max(0, first), visible + Math.Max(2, CachePreloadExtraRows));
        for (int i = 0; i < visible; i++)
        {
            int viewIndex = first + i; if (viewIndex >= ViewCount) break;
            var rr = new Rectangle(0, top + i * RowHeight, Width - VBarWidth, RowHeight);
            if (IsGroupRow(viewIndex)) { DrawGroupHeader(g, rr, GetGroupCaption(viewIndex), IsGroupCollapsed(viewIndex)); continue; }
            var row = GetViewRow(viewIndex); if (row == null) continue;
            bool isSelected = _selectedRows.Contains(viewIndex) || viewIndex == _selectedRow;
            int anim = _selectionAnimations.TryGetValue(viewIndex, out var av) ? av : (isSelected ? 255 : 0);
            Color normalBack = _customBackColor == Color.Empty ? _theme.BackColor : _customBackColor;
            Color alternateBack = _customAlternateBackColor == Color.Empty ? _theme.AlternateBackColor : _customAlternateBackColor;
            Color hotBack = _customHotBackColor == Color.Empty ? _theme.HotBackColor : _customHotBackColor;
            Color selectBack = _customSelectionBackColor == Color.Empty ? _theme.SelectionBackColor : _customSelectionBackColor;
            Color selectFore = _customSelectionForeColor == Color.Empty ? _theme.SelectionForeColor : _customSelectionForeColor;
            Color baseBack = HotTracking && viewIndex == _hotRow ? hotBack : (AlternateRows && viewIndex % 2 == 1 ? alternateBack : normalBack);
            baseBack = GetThemeAwareRowBack(row, viewIndex, baseBack);
            var rowCustomBack = RowBackColorGetter?.Invoke(row);
            if (rowCustomBack.HasValue && rowCustomBack.Value != Color.Empty) baseBack = rowCustomBack.Value;
            Color rowBack = isSelected ? Blend(baseBack, selectBack, Math.Max(0.35, anim / 255.0)) : baseBack;
            Color rowFore = isSelected ? BestTextOn(rowBack, selectFore) : (RowForeColorGetter?.Invoke(row) ?? _theme.ForeColor);
            if (!isSelected) ApplyFormatRowCompatibility(viewIndex, row, ref rowBack, ref rowFore);
            rowFore = EnsureReadableTextOn(rowBack, rowFore);
            DrawRowBackground(g, rr, rowBack, isSelected, anim);
            if (EnableHighlightEngine && _rowHighlights.TryGetValue(viewIndex, out var rowHighlight))
            {
                double leftMs = Math.Max(0, (rowHighlight.ExpiresUtc - DateTime.UtcNow).TotalMilliseconds);
                double alpha = Math.Min(0.55, 0.18 + 0.37 * Math.Min(1.0, leftMs / Math.Max(1, DefaultHighlightDurationMs)));
                using var hb = new SolidBrush(Color.FromArgb((int)(alpha * 255), rowHighlight.Color));
                g.FillRectangle(hb, rr);
                using var hp = new Pen(Color.FromArgb(180, rowHighlight.Color));
                g.DrawLine(hp, rr.Left, rr.Top, rr.Right, rr.Top);
                g.DrawLine(hp, rr.Left, rr.Bottom - 1, rr.Right, rr.Bottom - 1);
            }
            int x = -_scrollX;
            foreach (var col in Columns.VisibleColumns)
            {
                var cr = new Rectangle(x, rr.Top, col.Width, rr.Height);
                if (cr.Right < 0) { x += col.Width; continue; }
                if (cr.Left > Width - VBarWidth) break;
                DrawCell(g, cr, row, col, rowFore, rowBack, isSelected, viewIndex);
                if (EnableRowDetails && viewIndex == _expandedRow) DrawRowDetailsGlyph(g, rr);
                if (ShowGridLines) using (var p = new Pen(_theme.GridColor)) { g.DrawLine(p, cr.Right-1, cr.Top, cr.Right-1, cr.Bottom); g.DrawLine(p, cr.Left, cr.Bottom-1, cr.Right, cr.Bottom-1); }
                x += col.Width;
            }
        }
    }

    private void DrawTileRows(Graphics g)
    {
        int top = GetRowsTopOffset();
        int footer = ShowSummaryFooter ? FooterHeight : 0;
        int hbarHeight = _hbar.Visible ? _hbar.Height : 0;
        int visibleBands = Math.Max(1, (Height - top - footer - hbarHeight) / RowHeight + 2);
        int perRow = Math.Max(1, GetTilesPerRow());
        int available = Math.Max(1, Width - VBarWidth - 8);
        int tileW = Math.Max(140, available / perRow);

        for (int band = 0; band < visibleBands; band++)
        {
            int visualRow = _scrollY + band;
            int y = top + band * RowHeight + 4;
            for (int tile = 0; tile < perRow; tile++)
            {
                int viewIndex = visualRow * perRow + tile;
                if (viewIndex >= ViewCount) break;
                var r = new Rectangle(4 + tile * tileW, y, Math.Max(40, tileW - 8), Math.Max(24, RowHeight - 8));
                if (IsGroupRow(viewIndex))
                {
                    DrawGroupHeader(g, r, GetGroupCaption(viewIndex), IsGroupCollapsed(viewIndex));
                    continue;
                }
                var row = GetViewRow(viewIndex);
                if (row == null) continue;
                DrawTileCard(g, r, viewIndex, row);
            }
        }
    }

    private ViewGridColumn? GetTileCheckBoxColumn()
    {
        if (!TileCheckBoxes) return null;

        bool IsCheckColumn(ViewGridColumn c)
            => c.Kind == ViewGridColumnKind.CheckBox || c.CellCheckBox || IsCompatibilityCheckBoxHostColumn(c);

        if (!string.IsNullOrWhiteSpace(TileCheckBoxAspectName))
        {
            var explicitColumn = Columns.FirstOrDefault(c =>
                IsCheckColumn(c) &&
                (string.Equals(c.AspectName, TileCheckBoxAspectName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(c.CheckBoxAspectName, TileCheckBoxAspectName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(c.Header, TileCheckBoxAspectName, StringComparison.OrdinalIgnoreCase)));
            if (explicitColumn != null) return explicitColumn;
        }

        return Columns.VisibleColumns.FirstOrDefault(IsCheckColumn)
            ?? Columns.FirstOrDefault(IsCheckColumn);
    }

    private Rectangle GetTileCheckBoxRect(Rectangle cardBounds)
    {
        int size = Math.Clamp(TileCheckBoxSize, 12, 32);
        int margin = Math.Clamp(TileCheckBoxMargin, 0, 48);

        int left = cardBounds.Left + margin;
        int right = cardBounds.Right - margin - size;
        int top = cardBounds.Top + margin;
        int bottom = cardBounds.Bottom - margin - size;

        int x;
        int y;

        switch (TileCheckBoxPosition)
        {
            case ViewGridTileCheckBoxPosition.TopLeft:
                x = left;
                y = top;
                break;

            case ViewGridTileCheckBoxPosition.BottomLeft:
                x = left;
                y = bottom;
                break;

            case ViewGridTileCheckBoxPosition.BottomRight:
                x = right;
                y = bottom;
                break;

            case ViewGridTileCheckBoxPosition.TopRight:
            default:
                x = right;
                y = top;
                break;
        }

        // Always keep the checkbox fully inside the card. This prevents partial rendering when
        // the host uses tight card sizes or a very small margin.
        x = Math.Clamp(x, cardBounds.Left + 2, Math.Max(cardBounds.Left + 2, cardBounds.Right - size - 2));
        y = Math.Clamp(y, cardBounds.Top + 2, Math.Max(cardBounds.Top + 2, cardBounds.Bottom - size - 2));

        return new Rectangle(x, y, size, size);
    }

    private bool ShouldDisplayTileCheckBox(int viewIndex, object row, ViewGridColumn checkColumn)
    {
        bool isSelected = _selectedRows.Contains(viewIndex) || viewIndex == _selectedRow;
        bool isHot = HotTracking && viewIndex == _hotRow;
        bool isChecked = GetRowCheckState(row, checkColumn) == CheckState.Checked;

        return TileCheckBoxVisibilityMode switch
        {
            ViewGridTileCheckBoxVisibilityMode.Hover => isHot,
            ViewGridTileCheckBoxVisibilityMode.Selected => isSelected,
            ViewGridTileCheckBoxVisibilityMode.HoverOrSelected => isHot || isSelected,
            ViewGridTileCheckBoxVisibilityMode.CheckedOrHoverOrSelected => isChecked || isHot || isSelected,
            _ => true
        };
    }

    private void DrawTileCheckBoxAdorner(Graphics g, Rectangle cardBounds, object row, ViewGridColumn checkColumn)
    {
        var cb = GetTileCheckBoxRect(cardBounds);
        if (cb.Width <= 0 || cb.Height <= 0) return;

        if (TileCheckBoxShowBackground)
        {
            Rectangle backRect = Rectangle.Intersect(Rectangle.Inflate(cb, 4, 4), Rectangle.Inflate(cardBounds, -1, -1));
            using var checkBack = new SolidBrush(Color.FromArgb(_theme.IsDark ? 205 : 235, _theme.IsDark ? Color.FromArgb(28, 32, 38) : Color.White));
            g.FillRoundedRectangle(checkBack, backRect, Math.Max(4, _theme.CornerRadius - 2));
            using var checkBorder = new Pen(Color.FromArgb(_theme.IsDark ? 180 : 140, _theme.BorderColor));
            g.DrawRoundedRectangle(checkBorder, backRect, Math.Max(4, _theme.CornerRadius - 2));
        }

        DrawModernCheckBox(g, cb, GetRowCheckState(row, checkColumn) == CheckState.Checked, Enabled && !checkColumn.ReadOnly);
    }

    private bool TryGetTileCheckBoxHit(Point location, int viewIndex, out ViewGridColumn? checkColumn)
    {
        checkColumn = null;
        if (!IsTileView || !TileCheckBoxes || viewIndex < 0) return false;

        checkColumn = GetTileCheckBoxColumn();
        if (checkColumn == null || checkColumn.ReadOnly) return false;

        var row = GetViewRow(viewIndex);
        if (row == null) return false;

        var card = GetCellBounds(viewIndex, checkColumn);
        if (card.IsEmpty || !card.Contains(location)) return false;
        if (!ShouldDisplayTileCheckBox(viewIndex, row, checkColumn)) return false;

        return Rectangle.Inflate(GetTileCheckBoxRect(card), TileCheckBoxHitPadding, TileCheckBoxHitPadding).Contains(location);
    }

    private void DrawTileCard(Graphics g, Rectangle r, int viewIndex, object row)
    {
        bool isSelected = _selectedRows.Contains(viewIndex) || viewIndex == _selectedRow;
        bool isHot = HotTracking && viewIndex == _hotRow;
        Color baseBack = isHot ? _theme.HotBackColor : GetThemeAwareRowBack(row, viewIndex, _theme.BackColor);
        var rowCustomBack = RowBackColorGetter?.Invoke(row);
        if (rowCustomBack.HasValue && rowCustomBack.Value != Color.Empty) baseBack = rowCustomBack.Value;
        Color selectBack = _customSelectionBackColor == Color.Empty ? _theme.SelectionBackColor : _customSelectionBackColor;
        Color back = isSelected ? Blend(baseBack, selectBack, 0.55) : baseBack;
        Color fore = isSelected ? BestTextOn(back, _theme.SelectionForeColor) : (RowForeColorGetter?.Invoke(row) ?? _theme.ForeColor);
        fore = EnsureReadableTextOn(back, fore);

        if (EnableSoftShadows) DrawSoftShadow(g, Rectangle.Inflate(r, -2, -2), Math.Max(6, _theme.CornerRadius));
        using (var b = new SolidBrush(back)) g.FillRoundedRectangle(b, r, Math.Max(6, _theme.CornerRadius));
        using (var p = new Pen(isSelected ? _theme.AccentColor : _theme.BorderColor, isSelected ? 2f : 1f))
            g.DrawRoundedRectangle(p, Rectangle.Inflate(r, -1, -1), Math.Max(6, _theme.CornerRadius));

        Color? cardStatusColor = ResolveCardStatusColor(row);
        ViewGridCardVisualInfo? cardVisualInfo = ResolveCardVisualInfo(row, cardStatusColor);

        if (ShouldDrawCardVisualAccent(cardVisualInfo, cardStatusColor))
        {
            DrawCardAccentAdorner(g, r, back, isSelected, cardVisualInfo, cardStatusColor);
        }
        else if (ViewMode == ViewGridMode.Timeline)
        {
            using var linePen = new Pen(Blend(_theme.AccentColor, back, 0.25), 3f);
            g.DrawLine(linePen, r.Left + 9, r.Top + 12, r.Left + 9, r.Bottom - 12);
            using var dotBrush = new SolidBrush(_theme.AccentColor);
            g.FillEllipse(dotBrush, r.Left + 5, r.Top + 16, 9, 9);
        }

        var tileCheckColumn = GetTileCheckBoxColumn();
        bool showTileCheckBox = tileCheckColumn != null && ShouldDisplayTileCheckBox(viewIndex, row, tileCheckColumn);
        if (showTileCheckBox && tileCheckColumn != null && !TileCheckBoxDrawOnTop)
            DrawTileCheckBoxAdorner(g, r, row, tileCheckColumn);

        if (ViewMode == ViewGridMode.DetailCard || ViewMode == ViewGridMode.PropertyCard)
        {
            DrawDetailCardContent(g, r, viewIndex, row, fore, back, cardVisualInfo, cardStatusColor, showTileCheckBox);
            DrawCardBadges(g, r, cardVisualInfo, back);
            DrawCardActions(g, r, cardVisualInfo, back);
            if (showTileCheckBox && tileCheckColumn != null && TileCheckBoxDrawOnTop)
                DrawTileCheckBoxAdorner(g, r, row, tileCheckColumn);
            return;
        }

        if (ViewMode == ViewGridMode.KpiDashboard)
        {
            DrawKpiDashboardContent(g, r, row, fore, back, cardStatusColor);
            DrawCardBadges(g, r, cardVisualInfo, back);
            return;
        }

        if (ViewMode == ViewGridMode.HeatMap)
        {
            DrawHeatMapContent(g, r, row, fore, back, cardStatusColor);
            DrawCardBadges(g, r, cardVisualInfo, back);
            return;
        }

        if (ViewMode == ViewGridMode.MiniChart)
        {
            DrawMiniChartContent(g, r, row, fore, back);
            DrawCardBadges(g, r, cardVisualInfo, back);
            return;
        }

        var visible = Columns.VisibleColumns.Where(c => c.Kind != ViewGridColumnKind.CheckBox && c.Width > 0).ToList();
        var iconCol = visible.FirstOrDefault(c => c.Kind == ViewGridColumnKind.Icon || c.Kind == ViewGridColumnKind.Image)
            ?? FindDetailCardImageColumn(visible, row);
        var titleCol = visible.FirstOrDefault(c => c.Editable) ?? visible.FirstOrDefault(c => c.AspectName.Equals("Name", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Ad", StringComparison.OrdinalIgnoreCase)) ?? visible.FirstOrDefault();
        int iconSize = ViewMode switch
        {
            ViewGridMode.Poster => 44,
            ViewGridMode.Gallery => 44,
            ViewGridMode.MediaTile => 38,
            ViewGridMode.FilmStrip => 42,
            ViewGridMode.ExtraLargeIcons => 42,
            ViewGridMode.IconGrid => 40,
            ViewGridMode.MediumIcons => 24,
            ViewGridMode.LargeCard or ViewGridMode.DashboardCard or ViewGridMode.GroupCard or ViewGridMode.RowCard or ViewGridMode.RowPreview or ViewGridMode.DetailCard or ViewGridMode.PropertyCard or ViewGridMode.Timeline or ViewGridMode.KpiDashboard or ViewGridMode.HeatMap or ViewGridMode.MiniChart => 28,
            _ => 32
        };
        int textX = ViewMode == ViewGridMode.Timeline ? r.Left + 24 : r.Left + 10;

        if (iconCol != null)
        {
            var img = ResolveDetailCardImage(iconCol, row);
            if (img != null)
            {
                var ir = new Rectangle(r.Left + 10, r.Top + Math.Max(6, (r.Height - iconSize) / 2), iconSize, iconSize);
                g.DrawImage(img, ir);
                textX = ir.Right + 8;
            }
        }

        int y = r.Top + 7;
        int textW = Math.Max(20, r.Right - textX - 8);
        Color? cardDotColor = ResolveCardDotColor(cardVisualInfo, cardStatusColor);
        if (ShouldDrawCardStatusDot(cardDotColor) && TryDrawCardStatusDot(g, row, cardDotColor!.Value, back, ref textX, ref textW, y))
        {
            // Dot consumes left-side title space in card-like renderers so Dashboard/CardView indicators stay visible.
        }
        if (showTileCheckBox && TileCheckBoxReserveTextArea && (TileCheckBoxPosition == ViewGridTileCheckBoxPosition.TopRight || TileCheckBoxPosition == ViewGridTileCheckBoxPosition.BottomRight))
            textW = Math.Max(20, textW - TileCheckBoxSize - 12);
        if (showTileCheckBox && TileCheckBoxReserveTextArea && (TileCheckBoxPosition == ViewGridTileCheckBoxPosition.TopLeft || TileCheckBoxPosition == ViewGridTileCheckBoxPosition.BottomLeft) && iconCol == null)
        {
            textX += TileCheckBoxSize + 12;
            textW = Math.Max(20, r.Right - textX - 8);
        }

        bool mediaLayout = TilePosterMode || ViewMode is ViewGridMode.Poster or ViewGridMode.Gallery or ViewGridMode.MediaTile or ViewGridMode.FilmStrip;
        if (mediaLayout && iconCol != null)
        {
            var poster = ResolveDetailCardImage(iconCol, row) ?? MediaPlaceholderImage;
            if (poster != null)
            {
                int defaultImageHeight = ViewMode switch
                {
                    ViewGridMode.Poster => PosterImageHeight,
                    ViewGridMode.Gallery => Math.Max(110, TilePosterImageHeight),
                    ViewGridMode.MediaTile => Math.Max(82, TilePosterImageHeight),
                    ViewGridMode.FilmStrip => Math.Max(96, r.Height - 52),
                    _ => TilePosterImageHeight
                };
                int posterH = Math.Min(Math.Max(72, defaultImageHeight), Math.Max(72, r.Height - 54));
                var pr = ViewMode == ViewGridMode.FilmStrip
                    ? new Rectangle(r.Left + 12, r.Top + 10, Math.Min(190, Math.Max(120, r.Width / 3)), posterH)
                    : new Rectangle(r.Left + 10, r.Top + 10, Math.Max(30, r.Width - 20), posterH);
                using (var posterBack = new SolidBrush(Blend(back, Color.Black, _theme.IsDark ? 0.18 : 0.04)))
                    g.FillRoundedRectangle(posterBack, pr, Math.Max(4, _theme.CornerRadius - 2));
                var mediaImageBounds = Rectangle.Inflate(pr, -1, -1);
                DrawMediaImage(g, poster, mediaImageBounds);
                DrawMediaQualityBadge(g, mediaImageBounds, row, back);
                DrawMediaPlaybackChrome(g, mediaImageBounds, row, back, isHot, isSelected);
                if (ViewMode == ViewGridMode.FilmStrip)
                {
                    y = r.Top + 16;
                    textX = pr.Right + 14;
                    textW = Math.Max(20, r.Right - textX - 12);
                }
                else
                {
                    y = pr.Bottom + 8;
                    textX = r.Left + 10;
                    textW = Math.Max(20, r.Width - 20);
                }
            }
        }

        if (titleCol != null)
        {
            using var bold = new Font(Font, FontStyle.Bold);
            TextRenderer.DrawText(g, Convert.ToString(titleCol.GetValue(row)) ?? string.Empty, bold, new Rectangle(textX, y, textW, 20), fore, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            y += 19;
        }

        int maxLines = ViewMode switch
        {
            ViewGridMode.LargeCard => Math.Max(1, LargeCardMaxTextLines),
            ViewGridMode.DashboardCard => Math.Max(4, Math.Min(8, LargeCardMaxTextLines)),
            ViewGridMode.KpiDashboard => Math.Max(2, Math.Min(4, LargeCardMaxTextLines)),
            ViewGridMode.HeatMap => Math.Max(2, Math.Min(4, LargeCardMaxTextLines)),
            ViewGridMode.MiniChart => Math.Max(3, Math.Min(5, LargeCardMaxTextLines)),
            ViewGridMode.RowPreview => Math.Max(1, Math.Min(3, LargeCardMaxTextLines)),
            ViewGridMode.GroupCard => Math.Max(4, Math.Min(7, LargeCardMaxTextLines)),
            ViewGridMode.RowCard => Math.Max(4, Math.Min(9, LargeCardMaxTextLines)),
            ViewGridMode.DetailCard or ViewGridMode.PropertyCard => Math.Max(1, Columns.VisibleColumns.Count(c => c.Width > 0)),
            ViewGridMode.Kanban => Math.Max(4, Math.Min(7, LargeCardMaxTextLines)),
            ViewGridMode.Timeline => Math.Max(5, Math.Min(10, LargeCardMaxTextLines)),
            ViewGridMode.Tile => Math.Max(1, TileMaxTextLines),
            ViewGridMode.Poster => Math.Max(2, Math.Min(5, TileMaxTextLines)),
            ViewGridMode.Gallery => Math.Max(2, Math.Min(5, TileMaxTextLines)),
            ViewGridMode.MediaTile => Math.Max(2, Math.Min(5, TileMaxTextLines)),
            ViewGridMode.FilmStrip => Math.Max(4, Math.Min(7, TileMaxTextLines)),
            ViewGridMode.IconGrid => 2,
            ViewGridMode.ExtraLargeIcons => 4,
            ViewGridMode.LargeIcons => 3,
            _ => 2
        };
        foreach (var col in visible)
        {
            if (col == titleCol || col == iconCol) continue;
            if (maxLines-- <= 0) break;
            string text = Convert.ToString(col.GetValue(row)) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) continue;
            Color muted = Blend(fore, back, 0.35);
            if (y + 17 > r.Bottom - 6) break;
            TextRenderer.DrawText(g, text, Font, new Rectangle(textX, y, textW, 18), muted, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            y += 17;
        }

        DrawCardBadges(g, r, cardVisualInfo, back);
        DrawCardActions(g, r, cardVisualInfo, back);

        if (showTileCheckBox && tileCheckColumn != null && TileCheckBoxDrawOnTop)
        {
            // Draw last by default so badges, accent bars and action glyphs never hide it.
            // This fixes card/dashboard scenarios where top-left indicators could partially cover the checkbox.
            DrawTileCheckBoxAdorner(g, r, row, tileCheckColumn);
        }
    }


    private void DrawKpiDashboardContent(Graphics g, Rectangle r, object row, Color fore, Color back, Color? accent)
    {
        var visible = Columns.VisibleColumns.Where(c => c.Kind != ViewGridColumnKind.CheckBox && c.Kind != ViewGridColumnKind.Icon && c.Kind != ViewGridColumnKind.Image && c.Width > 0).ToList();
        var titleCol = visible.FirstOrDefault(c => c.AspectName.Equals("Title", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Name", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Ad", StringComparison.OrdinalIgnoreCase)) ?? visible.FirstOrDefault();
        var valueCol = visible.FirstOrDefault(c => c.AspectName.Equals("Value", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Count", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Deger", StringComparison.OrdinalIgnoreCase)) ?? visible.Skip(1).FirstOrDefault() ?? titleCol;
        string title = titleCol == null ? "KPI" : Convert.ToString(titleCol.GetValue(row)) ?? "KPI";
        string value = valueCol == null ? string.Empty : Convert.ToString(valueCol.GetValue(row)) ?? string.Empty;
        Color line = accent ?? _theme.AccentColor;
        using var titleFont = new Font(Font.FontFamily, Math.Max(8.5f, Font.Size), FontStyle.Bold);
        using var valueFont = new Font(Font.FontFamily, Math.Max(20f, Font.Size * 2.6f), FontStyle.Bold);
        TextRenderer.DrawText(g, title, titleFont, new Rectangle(r.Left + 16, r.Top + 14, r.Width - 32, 22), EnsureReadableTextOn(back, Blend(fore, back, 0.20)), TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, value, valueFont, new Rectangle(r.Left + 16, r.Top + 42, r.Width - 32, 42), EnsureReadableTextOn(back, fore), TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        using var pen = new Pen(Blend(line, back, 0.30), 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(pen, r.Left + 18, r.Bottom - 18, r.Right - 18, r.Bottom - 18);
    }

    private void DrawHeatMapContent(Graphics g, Rectangle r, object row, Color fore, Color back, Color? accent)
    {
        var visible = Columns.VisibleColumns.Where(c => c.Kind != ViewGridColumnKind.CheckBox && c.Kind != ViewGridColumnKind.Icon && c.Kind != ViewGridColumnKind.Image && c.Width > 0).ToList();
        var titleCol = visible.FirstOrDefault() ;
        var valueCol = visible.FirstOrDefault(c => c.AspectName.Equals("Percent", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Risk", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Score", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Value", StringComparison.OrdinalIgnoreCase)) ?? visible.Skip(1).FirstOrDefault();
        string title = titleCol == null ? "Heat" : Convert.ToString(titleCol.GetValue(row)) ?? "Heat";
        string valueText = valueCol == null ? string.Empty : Convert.ToString(valueCol.GetValue(row)) ?? string.Empty;
        double value = 0;
        double.TryParse(valueText.Replace("%", string.Empty).Trim(), out value);
        value = Math.Max(0, Math.Min(100, value));
        Color heat = accent ?? (value >= 80 ? Color.FromArgb(215, 64, 64) : value >= 50 ? Color.FromArgb(218, 155, 55) : Color.FromArgb(54, 170, 102));
        using var fill = new SolidBrush(Blend(back, heat, _theme.IsDark ? 0.32 : 0.18));
        var heatRect = Rectangle.Inflate(r, -12, -12);
        g.FillRoundedRectangle(fill, heatRect, Math.Max(5, _theme.CornerRadius - 1));
        using var titleFont = new Font(Font, FontStyle.Bold);
        TextRenderer.DrawText(g, title, titleFont, new Rectangle(r.Left + 18, r.Top + 16, r.Width - 36, 22), EnsureReadableTextOn(back, fore), TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, valueText, Font, new Rectangle(r.Left + 18, r.Top + 42, r.Width - 36, 20), EnsureReadableTextOn(back, Blend(fore, back, 0.20)), TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        int barW = Math.Max(8, (int)((r.Width - 36) * (value / 100.0)));
        using var bar = new SolidBrush(heat);
        g.FillRoundedRectangle(bar, new Rectangle(r.Left + 18, r.Bottom - 25, barW, 7), 4);
    }

    private void DrawMiniChartContent(Graphics g, Rectangle r, object row, Color fore, Color back)
    {
        var visible = Columns.VisibleColumns.Where(c => c.Kind != ViewGridColumnKind.CheckBox && c.Kind != ViewGridColumnKind.Icon && c.Kind != ViewGridColumnKind.Image && c.Width > 0).ToList();
        var titleCol = visible.FirstOrDefault();
        var valueCol = visible.Skip(1).FirstOrDefault();
        string title = titleCol == null ? "Trend" : Convert.ToString(titleCol.GetValue(row)) ?? "Trend";
        string value = valueCol == null ? string.Empty : Convert.ToString(valueCol.GetValue(row)) ?? string.Empty;
        using var titleFont = new Font(Font, FontStyle.Bold);
        TextRenderer.DrawText(g, title, titleFont, new Rectangle(r.Left + 16, r.Top + 12, r.Width - 32, 22), fore, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, value, Font, new Rectangle(r.Left + 16, r.Top + 36, r.Width - 32, 18), EnsureReadableTextOn(back, Blend(fore, back, 0.25)), TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        int left = r.Left + 18;
        int bottom = r.Bottom - 18;
        int w = Math.Max(80, r.Width - 36);
        int h = 32;
        using var pen = new Pen(_theme.AccentColor, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        var points = new List<Point>();
        int seed = Math.Abs((title + value).GetHashCode());
        for (int i = 0; i < 10; i++)
        {
            int x = left + (int)(i * (w / 9.0));
            int y = bottom - 4 - ((seed >> (i % 12)) & 23) - (i % 3) * 2;
            points.Add(new Point(x, Math.Max(bottom - h, Math.Min(bottom - 3, y))));
        }
        if (points.Count > 1) g.DrawLines(pen, points.ToArray());
    }

    private void DrawDetailCardContent(Graphics g, Rectangle r, int viewIndex, object row, Color fore, Color back, ViewGridCardVisualInfo? cardVisualInfo, Color? cardStatusColor, bool reserveCheckBoxArea)
    {
        var visible = Columns.VisibleColumns.Where(c => c.Width > 0).ToList();
        var detailImageColumn = FindDetailCardImageColumn(visible, row);
        if (detailImageColumn != null &&
            (DetailCardLayout is ViewGridDetailCardLayout.Media or ViewGridDetailCardLayout.PosterLeft ||
             ViewMode is ViewGridMode.DetailCard or ViewGridMode.PropertyCard))
        {
            DrawMediaDetailCardContent(g, r, viewIndex, row, fore, back, cardVisualInfo, cardStatusColor, reserveCheckBoxArea, visible, detailImageColumn);
            return;
        }

        var textColumns = visible.Where(c => c.Kind != ViewGridColumnKind.Icon && c.Kind != ViewGridColumnKind.Image).ToList();
        var titleCol = textColumns.FirstOrDefault(c => c.Editable)
            ?? textColumns.FirstOrDefault(c => c.AspectName.Equals("Name", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Ad", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Title", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Baslik", StringComparison.OrdinalIgnoreCase))
            ?? textColumns.FirstOrDefault();

        int left = r.Left + 16;
        int rightPadding = 16 + (reserveCheckBoxArea && TileCheckBoxReserveTextArea && (TileCheckBoxPosition == ViewGridTileCheckBoxPosition.TopRight || TileCheckBoxPosition == ViewGridTileCheckBoxPosition.BottomRight) ? TileCheckBoxSize + 12 : 0);
        int width = Math.Max(40, r.Right - left - rightPadding);
        int y = r.Top + 12;

        int dotTextX = left;
        int dotTextW = width;
        Color? dot = ResolveCardDotColor(cardVisualInfo, cardStatusColor);
        if (ShouldDrawCardStatusDot(dot) && TryDrawCardStatusDot(g, row, dot!.Value, back, ref dotTextX, ref dotTextW, y + 1))
        {
            left = dotTextX;
            width = dotTextW;
        }

        using var titleFont = new Font(Font, FontStyle.Bold);
        string title = titleCol != null ? Convert.ToString(titleCol.GetValue(row)) ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(title)) title = "Kayıt Detayı";
        TextRenderer.DrawText(g, title, titleFont, new Rectangle(left, y, width, 22), fore, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        y += 26;

        using var separator = new Pen(Blend(_theme.BorderColor, back, 0.25));
        g.DrawLine(separator, r.Left + 14, y, r.Right - 14, y);
        y += 8;

        bool propertyMode = ViewMode == ViewGridMode.PropertyCard;
        bool showLabels = ShowDetailCardColumnHeaders || propertyMode;
        int measuredLabelWidth = visible.Count == 0 ? 110 : visible.Max(c => TextRenderer.MeasureText(string.IsNullOrWhiteSpace(c.Header) ? c.AspectName : c.Header, Font).Width + 12);
        int labelWidth = showLabels ? Math.Min(Math.Max(propertyMode ? 130 : 110, measuredLabelWidth), Math.Max(120, r.Width / 3)) : 0;
        Color labelFore = EnsureReadableTextOn(back, Blend(fore, back, 0.25));
        Color valueFore = EnsureReadableTextOn(back, fore);
        int lineHeight = Math.Max(propertyMode ? 20 : 17, TextRenderer.MeasureText("Ag", Font, Size.Empty, TextFormatFlags.NoPadding).Height + (propertyMode ? 5 : 2));
        int propertyRow = 0;

        foreach (var col in visible)
        {
            if (y + lineHeight > r.Bottom - 10) break;
            string label = string.IsNullOrWhiteSpace(col.Header) ? col.AspectName : col.Header;
            string value;
            if (col.Kind == ViewGridColumnKind.CheckBox || col.CellCheckBox || IsCompatibilityCheckBoxHostColumn(col))
            {
                value = GetRowCheckState(row, col) == CheckState.Checked ? "Evet" : "Hayır";
            }
            else if (col.Kind == ViewGridColumnKind.Icon || col.Kind == ViewGridColumnKind.Image)
            {
                value = Convert.ToString(col.GetValue(row)) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(value)) value = "Görsel";
            }
            else
            {
                value = Convert.ToString(col.GetValue(row)) ?? string.Empty;
            }

            string displayValue = string.IsNullOrWhiteSpace(value) ? "-" : value;
            if (showLabels)
            {
                if (propertyMode)
                    DrawPropertyCardRowBackground(g, new Rectangle(left - 4, y - 1, width + 8, lineHeight), back, propertyRow++);

                var labelRect = new Rectangle(left, y, labelWidth, lineHeight);
                var valueRect = new Rectangle(left + labelWidth + 8, y, Math.Max(20, width - labelWidth - 8), lineHeight);
                TextRenderer.DrawText(g, label, Font, labelRect, labelFore, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, displayValue, Font, valueRect, valueFore, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
            }
            else
            {
                var valueRect = new Rectangle(left, y, width, lineHeight);
                TextRenderer.DrawText(g, displayValue, Font, valueRect, valueFore, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
            }
            y += lineHeight;
        }
    }

    private void DrawMediaDetailCardContent(Graphics g, Rectangle r, int viewIndex, object row, Color fore, Color back, ViewGridCardVisualInfo? cardVisualInfo, Color? cardStatusColor, bool reserveCheckBoxArea, List<ViewGridColumn> visible, ViewGridColumn imageColumn)
    {
        var textColumns = visible.Where(c => c != imageColumn && c.Kind != ViewGridColumnKind.CheckBox && c.Width > 0).ToList();
        var titleCol = textColumns.FirstOrDefault(c => c.Editable)
            ?? textColumns.FirstOrDefault(c => c.AspectName.Equals("Title", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Name", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Ad", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Şarkı", StringComparison.OrdinalIgnoreCase) || c.AspectName.Equals("Sarki", StringComparison.OrdinalIgnoreCase))
            ?? textColumns.FirstOrDefault();

        int left = r.Left + 16;
        int top = r.Top + 12;
        int rightPadding = 16 + (reserveCheckBoxArea && TileCheckBoxReserveTextArea && (TileCheckBoxPosition == ViewGridTileCheckBoxPosition.TopRight || TileCheckBoxPosition == ViewGridTileCheckBoxPosition.BottomRight) ? TileCheckBoxSize + 12 : 0);
        int width = Math.Max(40, r.Right - left - rightPadding);

        Color? dot = ResolveCardDotColor(cardVisualInfo, cardStatusColor);
        int titleX = left;
        int titleW = width;
        if (ShouldDrawCardStatusDot(dot) && TryDrawCardStatusDot(g, row, dot!.Value, back, ref titleX, ref titleW, top + 1))
        {
            left = titleX;
            width = titleW;
        }

        using var titleFont = new Font(Font, FontStyle.Bold);
        string title = titleCol != null ? Convert.ToString(titleCol.GetValue(row)) ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(title)) title = "Kayıt Detayı";
        TextRenderer.DrawText(g, title, titleFont, new Rectangle(left, top, width, 24), fore, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        int y = top + 30;
        using var separator = new Pen(Blend(_theme.BorderColor, back, 0.25));
        g.DrawLine(separator, r.Left + 14, y, r.Right - 14, y);
        y += 12;

        int imageW = DetailCardLayout == ViewGridDetailCardLayout.PosterLeft
            ? Math.Max(132, Math.Min(240, DetailCardMediaImageWidth))
            : Math.Max(96, Math.Min(190, DetailCardMediaImageWidth));
        int imageH = DetailCardLayout == ViewGridDetailCardLayout.PosterLeft
            ? Math.Max(150, Math.Min(Math.Max(150, r.Height - 72), DetailCardMediaImageHeight))
            : Math.Max(110, Math.Min(Math.Max(110, r.Height - 72), DetailCardMediaImageHeight));
        var imageFrame = new Rectangle(r.Left + 16, y, imageW, Math.Min(imageH, Math.Max(72, r.Bottom - y - 12)));
        using (var imageBack = new SolidBrush(Blend(back, Color.Black, _theme.IsDark ? 0.18 : 0.04)))
            g.FillRoundedRectangle(imageBack, imageFrame, Math.Max(4, _theme.CornerRadius));

        var poster = ResolveDetailCardImage(imageColumn, row) ?? MediaPlaceholderImage;
        var imageBounds = Rectangle.Inflate(imageFrame, -3, -3);
        if (poster != null)
        {
            DrawMediaImage(g, poster, imageBounds);
            DrawMediaQualityBadge(g, imageBounds, row, back);
            if (DetailCardShowMediaPlaybackChrome)
                DrawMediaPlaybackChrome(g, imageBounds, row, back, isHot: viewIndex == _hotRow, isSelected: viewIndex == _selectedRow || _selectedRows.Contains(viewIndex));
        }
        else
        {
            Color phFore = EnsureReadableTextOn(back, Blend(fore, back, 0.25));
            TextRenderer.DrawText(g, "Görsel", Font, imageBounds, phFore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        int detailLeft = imageFrame.Right + 18;
        int detailWidth = Math.Max(80, r.Right - detailLeft - 16 - rightPadding);
        int detailY = y;
        bool propertyMode = ViewMode == ViewGridMode.PropertyCard;
        bool showLabels = ShowDetailCardColumnHeaders || propertyMode;
        int measuredLabelWidth = textColumns.Count == 0 ? 110 : textColumns.Max(c => TextRenderer.MeasureText(string.IsNullOrWhiteSpace(c.Header) ? c.AspectName : c.Header, Font).Width + 12);
        int labelWidth = showLabels ? Math.Min(Math.Max(propertyMode ? 118 : 94, measuredLabelWidth), Math.Max(110, detailWidth / 3)) : 0;
        Color labelFore = EnsureReadableTextOn(back, Blend(fore, back, 0.28));
        Color valueFore = EnsureReadableTextOn(back, fore);
        int lineHeight = Math.Max(propertyMode ? 20 : 17, TextRenderer.MeasureText("Ag", Font, Size.Empty, TextFormatFlags.NoPadding).Height + (propertyMode ? 5 : 3));

        using var valueBold = new Font(Font, FontStyle.Bold);
        bool titleDrawn = false;
        int propertyRow = 0;
        foreach (var col in textColumns)
        {
            if (detailY + lineHeight > r.Bottom - 12) break;
            string label = string.IsNullOrWhiteSpace(col.Header) ? col.AspectName : col.Header;
            string value = Convert.ToString(col.GetValue(row)) ?? string.Empty;
            string displayValue = string.IsNullOrWhiteSpace(value) ? "-" : value;

            var valueFont = (!titleDrawn && col == titleCol) ? valueBold : Font;
            if (showLabels)
            {
                if (propertyMode)
                    DrawPropertyCardRowBackground(g, new Rectangle(detailLeft - 4, detailY - 1, detailWidth + 8, lineHeight), back, propertyRow++);

                var labelRect = new Rectangle(detailLeft, detailY, labelWidth, lineHeight);
                var valueRect = new Rectangle(detailLeft + labelWidth + 10, detailY, Math.Max(20, detailWidth - labelWidth - 10), lineHeight);
                TextRenderer.DrawText(g, label, Font, labelRect, labelFore, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, displayValue, valueFont, valueRect, valueFore, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
            }
            else
            {
                var valueRect = new Rectangle(detailLeft, detailY, detailWidth, lineHeight);
                TextRenderer.DrawText(g, displayValue, valueFont, valueRect, valueFore, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
            }
            if (col == titleCol) titleDrawn = true;
            detailY += lineHeight;
        }

        if (detailY < r.Bottom - 28)
        {
            string compact = BuildMediaCompactInfo(row, textColumns);
            if (!string.IsNullOrWhiteSpace(compact))
            {
                using var compactPen = new Pen(Blend(_theme.BorderColor, back, 0.35));
                int compactY = Math.Max(detailY + 6, imageFrame.Bottom - 24);
                if (compactY < r.Bottom - 20)
                {
                    g.DrawLine(compactPen, detailLeft, compactY - 5, r.Right - 16, compactY - 5);
                    TextRenderer.DrawText(g, compact, Font, new Rectangle(detailLeft, compactY, detailWidth, 20), labelFore, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
                }
            }
        }
    }

    private string BuildMediaCompactInfo(object row, List<ViewGridColumn> columns)
    {
        string[] preferred = { "Year", "Yıl", "Duration", "Süre", "Format", "Bitrate", "Quality", "Kalite" };
        var parts = new List<string>();
        foreach (string name in preferred)
        {
            var col = columns.FirstOrDefault(c => string.Equals(c.AspectName, name, StringComparison.OrdinalIgnoreCase) || string.Equals(c.Header, name, StringComparison.OrdinalIgnoreCase));
            if (col == null) continue;
            string value = Convert.ToString(col.GetValue(row)) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(value) && value != "-") parts.Add(value);
        }
        return string.Join(" • ", parts.Distinct());
    }

    private ViewGridColumn? FindDetailCardImageColumn(List<ViewGridColumn> columns, object row)
    {
        return columns.FirstOrDefault(c => c.Width > 0 && ResolveDetailCardImage(c, row) != null);
    }

    private void DrawPropertyCardRowBackground(Graphics g, Rectangle bounds, Color cardBack, int rowIndex)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var tint = rowIndex % 2 == 0
            ? Blend(cardBack, _theme.ForeColor, _theme.IsDark ? 0.035 : 0.025)
            : Blend(cardBack, _theme.AccentColor, _theme.IsDark ? 0.030 : 0.018);

        using var fill = new SolidBrush(tint);
        g.FillRoundedRectangle(fill, bounds, Math.Max(3, _theme.CornerRadius - 4));
    }

    private Image? ResolveDetailCardImage(ViewGridColumn column, object row)
    {
        var resolved = ResolveColumnImage(column, row, preferLarge: true);
        if (resolved != null) return resolved;

        return TryGetColumnValueImage(column, row);
    }

    private static Image? TryGetColumnValueImage(ViewGridColumn column, object row)
    {
        object? value;
        try
        {
            value = column.GetValue(row);
        }
        catch
        {
            return null;
        }

        return value switch
        {
            Image image => image,
            Icon icon => icon.ToBitmap(),
            _ => null
        };
    }

    private void DrawMediaImage(Graphics g, Image image, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var oldClip = g.Clip;
        if (MediaImageRoundedCorners)
        {
            using var path = RoundedRectPath(bounds, Math.Max(3, _theme.CornerRadius - 3));
            g.SetClip(path, CombineMode.Replace);
        }

        switch (MediaImageScaleMode)
        {
            case ViewGridMediaImageScaleMode.Cover:
                DrawImageCover(g, image, bounds);
                break;
            case ViewGridMediaImageScaleMode.Stretch:
                g.DrawImage(image, bounds);
                break;
            default:
                DrawImageContain(g, image, bounds);
                break;
        }

        if (MediaImageRoundedCorners)
        {
            g.Clip = oldClip;
            using var border = new Pen(Color.FromArgb(_theme.IsDark ? 120 : 80, _theme.BorderColor), 1f);
            g.DrawRoundedRectangle(border, bounds, Math.Max(3, _theme.CornerRadius - 3));
        }
    }

    private static void DrawImageContain(Graphics g, Image image, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        float ratio = Math.Min(bounds.Width / (float)image.Width, bounds.Height / (float)image.Height);
        int w = Math.Max(1, (int)(image.Width * ratio));
        int h = Math.Max(1, (int)(image.Height * ratio));
        var dest = new Rectangle(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
        g.DrawImage(image, dest);
    }

    private static void DrawImageCover(Graphics g, Image image, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        float ratio = Math.Max(bounds.Width / (float)image.Width, bounds.Height / (float)image.Height);
        int w = Math.Max(1, (int)(image.Width * ratio));
        int h = Math.Max(1, (int)(image.Height * ratio));
        var dest = new Rectangle(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
        g.DrawImage(image, dest);
    }

    private static GraphicsPath RoundedRectPath(Rectangle bounds, int radius)
    {
        int r = Math.Max(1, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, r, r, 180, 90);
        path.AddArc(bounds.Right - r, bounds.Top, r, r, 270, 90);
        path.AddArc(bounds.Right - r, bounds.Bottom - r, r, r, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - r, r, r, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void DrawGroupHeader(Graphics g, Rectangle r, string caption, bool collapsed)
    {
        var back = _customGroupBackColor == Color.Empty ? Blend(_theme.TextBackColor, _theme.AccentColor, _theme.IsDark ? 0.18 : 0.08) : _customGroupBackColor;
        using var b = new SolidBrush(back);
        using var p = new Pen(_theme.BorderColor);
        g.FillRectangle(b, r);
        g.DrawLine(p, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);

        Color fore = _customGroupForeColor == Color.Empty ? BestTextOn(back, _theme.TextForeColor) : _customGroupForeColor;
        int textLeft = r.Left + 10;
        if (DrawGroupCollapseGlyph)
        {
            var glyphRect = new Rectangle(r.Left + 9, r.Top + Math.Max(0, (r.Height - 16) / 2), 16, 16);
            DrawGroupExpandCollapseGlyph(g, glyphRect, collapsed, fore);
            textLeft = glyphRect.Right + 6;
        }

        using var f = new Font(Font, FontStyle.Bold);
        TextRenderer.DrawText(g, caption, f, new Rectangle(textLeft, r.Top, Math.Max(10, r.Right - textLeft - 8), r.Height), fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    private void DrawGroupExpandCollapseGlyph(Graphics g, Rectangle r, bool collapsed, Color fore)
    {
        // Daha modern görünüm: düz '>' yerine yumuşak rozet içinde chevron.
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color badge = Blend(_theme.AccentColor, _theme.PanelBackColor, _theme.IsDark ? 0.22 : 0.12);
        using (var b = new SolidBrush(badge))
            g.FillEllipse(b, Rectangle.Inflate(r, -1, -1));
        using (var outline = new Pen(Blend(_theme.AccentColor, _theme.BorderColor, 0.35), 1f))
            g.DrawEllipse(outline, Rectangle.Inflate(r, -1, -1));
        using var p = new Pen(BestTextOn(badge, fore), 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        if (collapsed)
        {
            g.DrawLines(p, new[] { new Point(r.Left + 6, r.Top + 4), new Point(r.Left + 11, r.Top + 8), new Point(r.Left + 6, r.Top + 12) });
        }
        else
        {
            g.DrawLines(p, new[] { new Point(r.Left + 4, r.Top + 6), new Point(r.Left + 8, r.Top + 11), new Point(r.Left + 12, r.Top + 6) });
        }
        g.SmoothingMode = old;
    }

    private Color EnsureReadableTextOn(Color back, Color preferredFore)
    {
        if (!AutoEnsureReadableTextColors) return preferredFore;
        if (back == Color.Empty || back == Color.Transparent) return preferredFore;
        return global::ViewGrid.Theming.ViewGridThemeAccessibility.EnsureReadableTextColor(preferredFore, back, 4.5d);
    }

    private static double TextContrastRatio(Color a, Color b)
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

    private void DrawCell(Graphics g, Rectangle r, object row, ViewGridColumn col, Color fore, Color cellBack, bool isSelected, int viewIndex)
    {
        _activeCellPaintViewIndex = viewIndex;
        var format = _conditionalFormats.FirstOrDefault(f => f.IsMatch(row, col));
        if (format?.BackColor != null) { using var fb = new SolidBrush(Blend(format.BackColor.Value, _theme.BackColor, _theme.IsDark ? 0.15 : 0.05)); g.FillRectangle(fb, r); }
        if (format?.ForeColor != null) fore = format.ForeColor.Value;
        var val = col.GetValue(row);
        if (IsCompatibilityCheckBoxHostColumn(col)
            && val is null
            && !string.IsNullOrWhiteSpace(col.AspectName)
            && !string.Equals(col.AspectName, _checkedAspectName, StringComparison.OrdinalIgnoreCase))
        {
            try { val = col.GetValue(row); } catch { }
        }
        var beforeCellBack = cellBack;
        ApplyFormatCellCompatibility(viewIndex, row, col, val, ref cellBack, ref fore);
        if (cellBack != beforeCellBack && cellBack != Color.Empty)
        {
            using var cb = new SolidBrush(cellBack);
            g.FillRectangle(cb, r);
        }
        fore = EnsureReadableTextOn(cellBack, fore);
        var inner = Rectangle.Inflate(r, -6, -3);
        bool compatibilityCheckHost = IsCompatibilityCheckBoxHostColumn(col);
        bool cellCheckBoxHost = col.CellCheckBox && col.Kind != ViewGridColumnKind.CheckBox && !compatibilityCheckHost;
        if (compatibilityCheckHost || cellCheckBoxHost)
        {
            var cb = GetRowCompatibilityCheckBoxRect(r);
            DrawModernCheckBox(g, cb, GetRowCheckState(row, col) == CheckState.Checked, Enabled);
            inner.X = cb.Right + 5;
            inner.Width = Math.Max(0, r.Right - inner.X - 6);
        }
        if (format?.Icon != null) { g.DrawImage(format.Icon, new Rectangle(inner.X, inner.Y + (inner.Height-18)/2, 18, 18)); inner = new Rectangle(inner.X + 22, inner.Y, Math.Max(0, inner.Width - 22), inner.Height); }

        // Grid-level CheckBoxes = true modunda host kolon, yanlışlıkla Kind=CheckBox
        // kalsa bile ayrı checkbox kolonu gibi davranmamalıdır. Checkbox sol iç
        // alana çizildikten sonra kolonun kendi metni normal Text olarak çizilir.
        var effectiveKind = compatibilityCheckHost && col.Kind == ViewGridColumnKind.CheckBox
            ? ViewGridColumnKind.Text
            : col.Kind;

        switch (effectiveKind)
        {
            case ViewGridColumnKind.CheckBox:
                var cb = new Rectangle(inner.X, inner.Y + (inner.Height-18)/2, 18, 18);
                DrawModernCheckBox(g, cb, GetRowCheckState(row, col) == CheckState.Checked, Enabled);
                break;
            case ViewGridColumnKind.ProgressBar:
                DrawProgress(g, inner, ToInt(val), fore, cellBack); break;
            case ViewGridColumnKind.Button:
                DrawButton(g, inner, ResolveButtonCellText(col, val), col); break;
            case ViewGridColumnKind.Rating:
                DrawRating(g, inner, ToInt(val), col, fore, cellBack); break;
            case ViewGridColumnKind.Image:
            case ViewGridColumnKind.Icon:
                DrawIconText(g, inner, row, col, val, fore); break;
            case ViewGridColumnKind.Badge:
                DrawBadge(g, inner, Convert.ToString(val) ?? string.Empty, cellBack); break;
            case ViewGridColumnKind.Hyperlink:
                DrawHyperlink(g, inner, Convert.ToString(val) ?? string.Empty, cellBack); break;
            case ViewGridColumnKind.ToggleSwitch:
                DrawToggleSwitch(g, inner, Convert.ToBoolean(val)); break;
            case ViewGridColumnKind.Sparkline:
                DrawSparkline(g, inner, val, cellBack); break;
            case ViewGridColumnKind.Tags:
                DrawTags(g, inner, Convert.ToString(val) ?? string.Empty, col, cellBack); break;
            case ViewGridColumnKind.ColorSwatch:
                DrawColorSwatch(g, inner, val, fore); break;
            case ViewGridColumnKind.ComboBox:
                DrawComboBoxCell(g, inner, Convert.ToString(val) ?? string.Empty, col, fore, cellBack); break;
            default:
                if (ImageGetterAppliesToTextColumns && ShowImagesOnSubItems && (col.ImageGetter != null || col.StateImageGetter != null || col.ImageKey != null || !string.IsNullOrWhiteSpace(col.ImageAspectName) || col.Image != null))
                {
                    var textRect = DrawColumnImagesIfNeeded(g, inner, row, col);
                    DrawTextWithFilterHighlight(g, textRect, Convert.ToString(val) ?? string.Empty, col, fore, cellBack);
                }
                else
                {
                    DrawTextWithFilterHighlight(g, inner, Convert.ToString(val) ?? string.Empty, col, fore, cellBack);
                }
                break;
        }
        DrawValidationAdornerIfNeeded(g, r, row, col);
    }

    private void DrawComboBoxCell(Graphics g, Rectangle r, string text, ViewGridColumn col, Color fore, Color cellBack)
    {
        var br = Rectangle.Inflate(r, -1, -3);
        using var back = new SolidBrush(Blend(_theme.ControlBackColor == Color.Empty ? _theme.PanelBackColor : _theme.ControlBackColor, cellBack, _theme.IsDark ? 0.35 : 0.18));
        using var border = new Pen(Blend(_theme.BorderColor, _theme.AccentColor, 0.18));
        g.FillRoundedRectangle(back, br, 5);
        g.DrawRoundedRectangle(border, br, 5);

        int textX = br.X + 8;
        var img = col.ComboBoxImageGetter?.Invoke(text);
        if (img != null)
        {
            var ir = new Rectangle(br.X + 6, br.Y + Math.Max(0, (br.Height - 18) / 2), 18, 18);
            g.DrawImage(img, ir);
            textX += 24;
        }
        var textRect = new Rectangle(textX, br.Y, Math.Max(0, br.Right - textX - 24), br.Height);
        TextRenderer.DrawText(g, text, Font, textRect, fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        int cx = br.Right - 14;
        int cy = br.Top + br.Height / 2;
        using var p = new Pen(EnsureVisibleStroke(_theme.MutedForeColor, cellBack), 1.4f);
        g.DrawLines(p, new[] { new Point(cx - 4, cy - 2), new Point(cx, cy + 2), new Point(cx + 4, cy - 2) });
    }

    private void DrawTextWithFilterHighlight(Graphics g, Rectangle r, string text, ViewGridColumn col, Color fore, Color cellBack)
    {
        if (ShouldDrawMultilineCell(col))
        {
            DrawMultilineCellText(g, r, text, col, fore);
            return;
        }

        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
        string needle = string.Empty;

        // Column filter text has priority for its own column. Otherwise global/search text is used.
        var columnFilter = _filters.Get(col.AspectName);
        if (columnFilter?.Mode == ViewGridFilterMode.Contains && !string.IsNullOrWhiteSpace(columnFilter.Text))
            needle = columnFilter.Text!;
        else if (HighlightSearchText && !string.IsNullOrWhiteSpace(_searchHighlightText))
            needle = _searchHighlightText;

        if (string.IsNullOrWhiteSpace(needle) || string.IsNullOrEmpty(text))
        {
            TextRenderer.DrawText(g, text, Font, r, fore, flags);
            return;
        }

        int index = text.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase);
        if (index < 0)
        {
            TextRenderer.DrawText(g, text, Font, r, fore, flags);
            return;
        }

        if (!UseGLVStyleHighlight)
        {
            TextRenderer.DrawText(g, text, Font, r, fore, flags);
            return;
        }

        DrawSegmentedHighlightText(g, r, text, index, Math.Min(needle.Length, text.Length - index), fore, cellBack);
    }

    private bool ShouldDrawMultilineCell(ViewGridColumn col)
    {
        if (IsTileView) return false;
        if (!AllowMultilineCells && !col.WordWrap) return false;
        if (col.Kind != ViewGridColumnKind.Text && col.Kind != ViewGridColumnKind.Hyperlink) return false;
        return col.WordWrap || AllowMultilineCells;
    }

    private int ResolveMultilineCellMaxLines(ViewGridColumn col)
    {
        if (col.MaxTextLines > 0) return Math.Clamp(col.MaxTextLines, 1, 12);
        return Math.Clamp(MaxCellTextLines, 1, 12);
    }

    private void DrawMultilineCellText(Graphics g, Rectangle r, string text, ViewGridColumn col, Color fore)
    {
        if (string.IsNullOrEmpty(text)) return;

        int maxLines = col.AllowCellScroll && col.CellScrollMaxVisibleLines > 0
            ? Math.Clamp(col.CellScrollMaxVisibleLines, 1, 24)
            : ResolveMultilineCellMaxLines(col);
        int lineHeight = TextRenderer.MeasureText(g, "Ag", Font, Size.Empty, TextFormatFlags.NoPadding).Height;
        int maxHeight = Math.Min(r.Height, Math.Max(lineHeight, maxLines * Math.Max(12, lineHeight)));
        var textRect = new Rectangle(r.Left, r.Top + Math.Max(0, (r.Height - maxHeight) / 2), Math.Max(1, r.Width - (col.AllowCellScroll ? 7 : 0)), maxHeight);
        var flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;

        if (!EnableCellOverflowScroll || !col.AllowCellScroll)
        {
            TextRenderer.DrawText(g, text, Font, textRect, fore, flags);
            return;
        }

        int fullHeight = TextRenderer.MeasureText(g, text, Font, new Size(Math.Max(1, textRect.Width), int.MaxValue), TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
        int overflow = Math.Max(0, fullHeight - textRect.Height);
        string key = BuildCellOverflowKey(_activeCellPaintViewIndex, col);
        int offset = key.Length == 0 ? 0 : GetCellOverflowOffset(key, overflow);

        var oldClip = g.Clip;
        g.SetClip(textRect);
        var shifted = new Rectangle(textRect.Left, textRect.Top - offset, textRect.Width, Math.Max(textRect.Height, fullHeight + 4));
        TextRenderer.DrawText(g, text, Font, shifted, fore, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        g.Clip = oldClip;

        if (overflow > 0)
            DrawCellOverflowAdorners(g, r, textRect, col, fore, offset, overflow);
    }

    private string BuildCellOverflowKey(int viewIndex, ViewGridColumn col)
    {
        if (viewIndex < 0 || col == null) return string.Empty;
        string columnKey = !string.IsNullOrWhiteSpace(col.Name) ? col.Name : (!string.IsNullOrWhiteSpace(col.AspectName) ? col.AspectName : col.Header);
        return viewIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + columnKey;
    }

    private int GetCellOverflowOffset(string key, int maxOffset)
    {
        if (!_cellOverflowScrollOffsets.TryGetValue(key, out int value)) return 0;
        value = Math.Clamp(value, 0, Math.Max(0, maxOffset));
        if (value == 0) _cellOverflowScrollOffsets.Remove(key);
        else _cellOverflowScrollOffsets[key] = value;
        return value;
    }

    private void DrawCellOverflowAdorners(Graphics g, Rectangle cellRect, Rectangle textRect, ViewGridColumn col, Color fore, int offset, int maxOffset)
    {
        bool showBar = ShowCellOverflowScrollBars && col.ShowCellScrollBar;
        if (showBar)
        {
            var track = new Rectangle(cellRect.Right - 5, textRect.Top + 2, 3, Math.Max(8, textRect.Height - 4));
            using var trackBrush = new SolidBrush(Color.FromArgb(_theme.IsDark ? 55 : 45, fore));
            g.FillRectangle(trackBrush, track);
            int thumbH = Math.Max(12, (int)Math.Round(track.Height * (textRect.Height / (double)(textRect.Height + maxOffset))));
            int thumbY = track.Top + (int)Math.Round((track.Height - thumbH) * (offset / (double)Math.Max(1, maxOffset)));
            using var thumbBrush = new SolidBrush(Blend(_theme.AccentColor, fore, 0.35));
            g.FillRoundedRectangle(thumbBrush, new Rectangle(track.Left - 1, thumbY, 5, thumbH), 2);
        }

        if (!col.CellOverflowFade) return;
        using var topFade = new LinearGradientBrush(new Rectangle(textRect.Left, textRect.Top, textRect.Width, 10), Color.FromArgb(offset > 0 ? 80 : 0, _theme.BackColor), Color.Transparent, LinearGradientMode.Vertical);
        using var bottomFade = new LinearGradientBrush(new Rectangle(textRect.Left, textRect.Bottom - 10, textRect.Width, 10), Color.Transparent, Color.FromArgb(offset < maxOffset ? 80 : 0, _theme.BackColor), LinearGradientMode.Vertical);
        g.FillRectangle(topFade, new Rectangle(textRect.Left, textRect.Top, textRect.Width, 10));
        g.FillRectangle(bottomFade, new Rectangle(textRect.Left, textRect.Bottom - 10, textRect.Width, 10));
    }

    private bool TryScrollCellOverflowAt(Point location, int wheelDelta)
    {
        if (!EnableCellOverflowScroll || IsTileView || location.Y < GetRowsTopOffset()) return false;
        int rowIndex = HitRow(location.Y);
        var col = HitColumn(location.X);
        if (rowIndex < 0 || col == null || !col.AllowCellScroll || !ShouldDrawMultilineCell(col)) return false;
        var row = GetViewRow(rowIndex);
        if (row == null) return false;
        string text = Convert.ToString(col.GetValue(row)) ?? string.Empty;
        if (text.Length == 0) return false;
        var bounds = Rectangle.Inflate(GetCellBounds(rowIndex, col), -6, -3);
        if (bounds.Width <= 0 || bounds.Height <= 0) return false;
        int lineHeight = TextRenderer.MeasureText("Ag", Font, Size.Empty, TextFormatFlags.NoPadding).Height;
        int maxLines = col.CellScrollMaxVisibleLines > 0 ? Math.Clamp(col.CellScrollMaxVisibleLines, 1, 24) : ResolveMultilineCellMaxLines(col);
        int visibleHeight = Math.Min(bounds.Height, Math.Max(lineHeight, maxLines * Math.Max(12, lineHeight)));
        int textWidth = Math.Max(1, bounds.Width - 7);
        int fullHeight = TextRenderer.MeasureText(text, Font, new Size(textWidth, int.MaxValue), TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
        int maxOffset = Math.Max(0, fullHeight - visibleHeight);
        if (maxOffset <= 0) return false;
        string key = BuildCellOverflowKey(rowIndex, col);
        int current = GetCellOverflowOffset(key, maxOffset);
        int step = Math.Max(8, lineHeight);
        int next = Math.Clamp(current - Math.Sign(wheelDelta) * step, 0, maxOffset);
        if (next == current) return false;
        _cellOverflowScrollOffsets[key] = next;
        _hotOverflowCellKey = key;
        Invalidate(GetCellBounds(rowIndex, col));
        return true;
    }

    private void ShowCellOverflowReaderPopup(int rowIndex, ViewGridColumn col, object row)
    {
        if (!EnableCellOverflowDetailsPopup || !col.CellOverflowDetailsOnDoubleClick) return;
        string text = Convert.ToString(col.GetValue(row)) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return;
        using var form = new Form
        {
            Text = string.IsNullOrWhiteSpace(col.Header) ? "Hücre detayı" : col.Header,
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(640, 420),
            MinimizeBox = false,
            MaximizeBox = true,
            BackColor = _theme.PanelBackColor,
            ForeColor = _theme.ForeColor
        };
        var box = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Text = text,
            BackColor = _theme.ControlBackColor == Color.Empty ? _theme.BackColor : _theme.ControlBackColor,
            ForeColor = _theme.ForeColor,
            Font = Font
        };
        form.Controls.Add(box);
        form.ShowDialog(FindForm());
    }

    private void DrawSegmentedHighlightText(Graphics g, Rectangle r, string text, int matchIndex, int matchLength, Color fore, Color cellBack)
    {
        // GLV inline highlight: the yellow mark is drawn behind only the matching characters.
        // It avoids repainting the full text over the marker, so the result stays clean in filters,
        // search, selected rows, dark theme, and striped rows.
        string prefix = text[..matchIndex];
        string match = text.Substring(matchIndex, matchLength);
        string suffix = text[(matchIndex + matchLength)..];

        int prefixWidth = MeasureTextWidth(g, prefix);
        int matchWidth = Math.Max(7, MeasureTextWidth(g, match));
        int textHeight = TextRenderer.MeasureText(g, "Ag", Font, Size.Empty, TextFormatFlags.NoPadding).Height;
        int y = r.Top + Math.Max(1, (r.Height - textHeight) / 2);
        int baseX = r.Left;
        int matchX = baseX + prefixWidth;

        if (matchX >= r.Right)
        {
            TextRenderer.DrawText(g, text, Font, r, fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            return;
        }

        var clip = g.ClipBounds;
        g.SetClip(r);
        try
        {
            if (!string.IsNullOrEmpty(prefix))
                TextRenderer.DrawText(g, prefix, Font, new Point(baseX, y), fore, TextFormatFlags.NoPadding);

            var hi = new Rectangle(matchX - 1, y + 1, Math.Min(matchWidth + 3, r.Right - matchX + 1), Math.Max(9, textHeight - 1));
            Color hiBack = _highlightBackColor == Color.Empty ? Color.FromArgb(255, 255, 235, 84) : _highlightBackColor;
            Color hiBorder = _highlightBorderColor == Color.Empty ? Color.FromArgb(210, 180, 120, 0) : _highlightBorderColor;
            Color hiFore = _highlightForeColor == Color.Empty ? BestTextOn(hiBack, fore) : _highlightForeColor;

            using (var b = new SolidBrush(hiBack))
            using (var p = new Pen(hiBorder))
            {
                g.FillRectangle(b, hi);
                g.DrawRectangle(p, hi);
            }

            TextRenderer.DrawText(g, match, Font, new Point(matchX, y), hiFore, TextFormatFlags.NoPadding);

            int suffixX = matchX + matchWidth;
            if (!string.IsNullOrEmpty(suffix) && suffixX < r.Right)
                TextRenderer.DrawText(g, suffix, Font, new Point(suffixX, y), fore, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }
        finally
        {
            g.SetClip(clip);
        }
    }

    private int MeasureTextWidth(Graphics g, string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return TextRenderer.MeasureText(g, text, Font, Size.Empty, TextFormatFlags.NoPadding).Width;
    }

    private void DrawModernCheckBox(Graphics g, Rectangle r, bool isChecked, bool enabled)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var back = enabled
            ? (isChecked ? _theme.AccentColor : _theme.BackColor)
            : ControlPaint.Light(_theme.BackColor);

        var border = enabled
            ? (isChecked ? _theme.AccentColor : _theme.BorderColor)
            : ControlPaint.Dark(_theme.BorderColor);

        using (var b = new SolidBrush(back))
        using (var p = new Pen(border, 1.4f))
        {
            var box = new Rectangle(r.X + 1, r.Y + 1, r.Width - 3, r.Height - 3);
            g.FillRoundedRectangle(b, box, 4);
            g.DrawRoundedRectangle(p, box, 4);
        }

        if (isChecked)
        {
            using var checkPen = new Pen(_theme.SelectionForeColor, 2.1f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            int x1 = r.Left + 5;
            int y1 = r.Top + r.Height / 2;
            int x2 = r.Left + 8;
            int y2 = r.Bottom - 6;
            int x3 = r.Right - 4;
            int y3 = r.Top + 5;
            g.DrawLines(checkPen, new[] { new Point(x1, y1), new Point(x2, y2), new Point(x3, y3) });
        }

        g.SmoothingMode = SmoothingMode.None;
    }

    private void DrawProgress(Graphics g, Rectangle r, int value, Color fore, Color cellBack)
    {
        value = Math.Clamp(value, 0, 100);

        int desiredHeight = Math.Clamp(ProgressBarHeight, 6, Math.Max(6, r.Height - 2));
        int y = r.Y + Math.Max(0, (r.Height - desiredHeight) / 2);
        var bar = new Rectangle(r.X, y, Math.Max(4, r.Width - 1), desiredHeight);

        if (!EnableModernProgressBar)
        {
            DrawClassicProgress(g, r, bar, value, fore, cellBack);
            return;
        }

        var oldSmoothing = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int radius = Math.Clamp(ProgressBarCornerRadius, 0, Math.Max(1, bar.Height / 2));
        Color trackColor = _theme.ControlBackColor == Color.Empty
            ? Blend(cellBack, _theme.BackColor, _theme.IsDark ? 0.10 : 0.04)
            : Blend(_theme.ControlBackColor, cellBack, _theme.IsDark ? 0.30 : 0.42);
        trackColor = EnsureContrastSurface(trackColor, cellBack);
        Color borderColor = Blend(EnsureVisibleStroke(_theme.BorderColor, cellBack), _theme.AccentColor, _theme.IsDark ? 0.16 : 0.10);
        Color fillColor = GetProgressFillColor(value, cellBack);

        using (var back = new SolidBrush(trackColor))
            g.FillRoundedRectangle(back, bar, radius);

        // Üst tarafta hafif aydınlık yüzey: düz progressbar yerine modern, camımsı hissi verir.
        var topGlow = new Rectangle(bar.X + 1, bar.Y + 1, Math.Max(1, bar.Width - 2), Math.Max(1, bar.Height / 2));
        using (var glowBrush = new LinearGradientBrush(topGlow, Color.FromArgb(_theme.IsDark ? 32 : 70, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical))
            g.FillRoundedRectangle(glowBrush, topGlow, Math.Max(1, radius - 1));

        using (var border = new Pen(borderColor))
            g.DrawRoundedRectangle(border, bar, radius);

        int fillWidth = Math.Max(0, (bar.Width - 2) * value / 100);
        if (fillWidth > 0)
        {
            var fill = new Rectangle(bar.X + 1, bar.Y + 1, fillWidth, Math.Max(1, bar.Height - 2));
            int fillRadius = Math.Max(1, radius - 1);

            if (ProgressBarUseGradient && fill.Width > 2)
            {
                Color c1 = Blend(fillColor, Color.White, _theme.IsDark ? 0.18 : 0.30);
                Color c2 = Blend(fillColor, Color.Black, _theme.IsDark ? 0.08 : 0.04);
                using var fillBrush = new LinearGradientBrush(fill, c1, c2, LinearGradientMode.Vertical);
                g.FillRoundedRectangle(fillBrush, fill, fillRadius);
            }
            else
            {
                using var fillBrush = new SolidBrush(fillColor);
                g.FillRoundedRectangle(fillBrush, fill, fillRadius);
            }

            if (ProgressBarAnimated && fill.Width > 18)
            {
                int shineWidth = Math.Max(18, Math.Min(44, fill.Width / 3));
                int offset = (Environment.TickCount / 18) % (fill.Width + shineWidth);
                var shine = new Rectangle(fill.Left + offset - shineWidth, fill.Top, shineWidth, fill.Height);
                if (shine.Right > fill.Left && shine.Left < fill.Right)
                {
                    var clipState = g.Save();
                    g.SetClip(fill);
                    using var shineBrush = new LinearGradientBrush(shine, Color.FromArgb(0, Color.White), Color.FromArgb(_theme.IsDark ? 58 : 92, Color.White), LinearGradientMode.ForwardDiagonal);
                    g.FillRectangle(shineBrush, shine);
                    g.Restore(clipState);
                }
            }
        }

        if (ProgressBarShowText)
        {
            string text = value + "%";
            Color textColor = BestTextOn(cellBack, fore);
            if (fillWidth > bar.Width * 0.48) textColor = BestTextOn(fillColor, textColor);
            using var textFont = new Font(Font.FontFamily, Math.Max(7.5f, Font.Size - 0.5f), FontStyle.Bold);
            TextRenderer.DrawText(g, text, textFont, r, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        g.SmoothingMode = oldSmoothing;
    }

    private void DrawClassicProgress(Graphics g, Rectangle r, Rectangle bar, int value, Color fore, Color cellBack)
    {
        Color trackColor = _theme.ControlBackColor == Color.Empty ? cellBack : Blend(_theme.ControlBackColor, cellBack, 0.35);
        trackColor = EnsureContrastSurface(trackColor, cellBack);
        Color borderColor = EnsureVisibleStroke(_theme.BorderColor, cellBack);
        Color fillColor = EnsureVisibleAccent(_theme.AccentColor, cellBack);

        using var back = new SolidBrush(trackColor);
        using var border = new Pen(borderColor);
        g.FillRoundedRectangle(back, bar, 4);
        g.DrawRoundedRectangle(border, bar, 4);

        var fill = new Rectangle(bar.X + 1, bar.Y + 1, Math.Max(0, (bar.Width - 2) * value / 100), bar.Height - 2);
        using var b = new SolidBrush(fillColor);
        if (fill.Width > 2) g.FillRoundedRectangle(b, fill, 3);

        if (ProgressBarShowText)
        {
            Color textColor = BestTextOn(cellBack, fore);
            if (fill.Width > bar.Width / 2) textColor = BestTextOn(fillColor, textColor);
            TextRenderer.DrawText(g, value + "%", Font, r, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    private Color GetProgressFillColor(int value, Color cellBack)
    {
        if (ProgressBarUseAccentColor)
        {
            var accent = EnsureVisibleAccent(_theme.AccentColor, cellBack);
            if (value < ProgressBarLowThreshold) return Blend(accent, Color.FromArgb(220, 70, 70), 0.38);
            if (value >= ProgressBarHighThreshold) return Blend(accent, Color.FromArgb(34, 164, 93), 0.30);
            return accent;
        }

        Color low = Color.FromArgb(220, 70, 70);
        Color mid = Color.FromArgb(230, 160, 45);
        Color high = Color.FromArgb(34, 164, 93);
        Color chosen = value < ProgressBarLowThreshold ? low : value >= ProgressBarHighThreshold ? high : mid;
        return EnsureVisibleAccent(chosen, cellBack);
    }
    private static Rectangle CenterRect(Rectangle bounds, int desiredWidth, int desiredHeight)
    {
        int width = Math.Max(1, Math.Min(bounds.Width, desiredWidth));
        int height = Math.Max(1, Math.Min(bounds.Height, desiredHeight));
        return new Rectangle(
            bounds.Left + Math.Max(0, (bounds.Width - width) / 2),
            bounds.Top + Math.Max(0, (bounds.Height - height) / 2),
            width,
            height);
    }

    private static string ResolveButtonCellText(ViewGridColumn col, object? value)
    {
        if (!string.IsNullOrWhiteSpace(col.ButtonText))
            return col.ButtonText.Trim();

        string? text = Convert.ToString(value);
        if (!string.IsNullOrWhiteSpace(text))
            return text;

        return col.Header;
    }

    private void DrawButton(Graphics g, Rectangle r, string text, ViewGridColumn? col = null)
    {
        var br = Rectangle.Inflate(r, -1, -3);
        if (col != null)
        {
            if (col.ButtonPadding != Padding.Empty)
                br = new Rectangle(br.Left + col.ButtonPadding.Left, br.Top + col.ButtonPadding.Top, Math.Max(0, br.Width - col.ButtonPadding.Horizontal), Math.Max(0, br.Height - col.ButtonPadding.Vertical));

            if (col.ButtonSizing == ViewGridColumnButtonSizing.TextBounds && !string.IsNullOrEmpty(text))
            {
                var measured = TextRenderer.MeasureText(g, text, Font, Size.Empty, TextFormatFlags.NoPadding);
                int desiredWidth = measured.Width + 22;
                int desiredHeight = measured.Height + 10;
                if (col.ButtonMaxWidth > 0) desiredWidth = Math.Min(desiredWidth, col.ButtonMaxWidth);
                br = CenterRect(br, desiredWidth, desiredHeight);
            }
            else if (col.ButtonSize.Width > 0 || col.ButtonSize.Height > 0)
            {
                int desiredWidth = col.ButtonSize.Width > 0 ? col.ButtonSize.Width : br.Width;
                int desiredHeight = col.ButtonSize.Height > 0 ? col.ButtonSize.Height : br.Height;
                br = CenterRect(br, desiredWidth, desiredHeight);
            }
        }
        if (br.Width <= 0 || br.Height <= 0) return;
        if (EnableSoftShadows) DrawSoftShadow(g, br, 6);
        using var b = new SolidBrush(GetSurfaceColor(_theme.ControlBackColor == Color.Empty ? _theme.TextBackColor : _theme.ControlBackColor, 0.04));
        using var p = new Pen(Blend(_theme.BorderColor, _theme.AccentColor, 0.25));
        g.FillRoundedRectangle(b, br, 6); g.DrawRoundedRectangle(p, br, 6);
        TextRenderer.DrawText(g, text, Font, br, BestTextOn(_theme.ControlBackColor == Color.Empty ? _theme.TextBackColor : _theme.ControlBackColor, _theme.ForeColor), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
    private void DrawRating(Graphics g, Rectangle r, int value, ViewGridColumn col, Color fore, Color cellBack)
    {
        value = Math.Clamp(value, 0, col.MaxRating);
        string filled = new string(col.RatingSymbol.FirstOrDefault('★'), value);
        string empty = new string('☆', col.MaxRating - value);
        using var ratingFont = new Font(Font.FontFamily, Font.Size + 2, FontStyle.Bold);

        Color filledColor = EnsureVisibleAccent(_theme.AccentColor, cellBack);
        Color emptyColor = EnsureVisibleStroke(_theme.GridColor, cellBack);
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding;

        TextRenderer.DrawText(g, filled, ratingFont, r, filledColor, flags);
        int filledWidth = TextRenderer.MeasureText(g, filled, ratingFont, Size.Empty, flags).Width;
        if (!string.IsNullOrEmpty(empty))
        {
            var emptyRect = new Rectangle(r.X + filledWidth, r.Y, Math.Max(0, r.Width - filledWidth), r.Height);
            TextRenderer.DrawText(g, empty, ratingFont, emptyRect, emptyColor, flags);
        }
    }
    private Image? ResolveHeaderImage(ViewGridColumn col)
    {
        if (col.HeaderImage != null) return col.HeaderImage;

        ImageList? primary = SmallImageList ?? LargeImageList;
        if (!string.IsNullOrWhiteSpace(col.HeaderImageKey) && primary != null)
        {
            string key = col.HeaderImageKey;
            if (primary.Images.ContainsKey(key)) return primary.Images[key];
            if (int.TryParse(key, out var keyIndex) && keyIndex >= 0 && keyIndex < primary.Images.Count) return primary.Images[keyIndex];
        }

        if (col.HeaderImageIndex >= 0 && primary != null && col.HeaderImageIndex < primary.Images.Count)
            return primary.Images[col.HeaderImageIndex];

        return null;
    }

    private Image? ResolveColumnImage(ViewGridColumn col, object row, bool preferLarge = false)
    {
        object? spec = col.GetImageKeyOrImage(row);
        if (spec == null) return null;
        if (spec is Image image) return image;
        if (spec is Icon icon) return icon.ToBitmap();
        ImageList? primary = preferLarge ? (LargeImageList ?? SmallImageList) : (SmallImageList ?? LargeImageList);
        if (spec is int index)
        {
            if (primary != null && index >= 0 && index < primary.Images.Count) return primary.Images[index];
            if (StateImageList != null && index >= 0 && index < StateImageList.Images.Count) return StateImageList.Images[index];
            return null;
        }
        string key = Convert.ToString(spec) ?? string.Empty;
        if (key.Length == 0) return null;
        if (primary != null)
        {
            if (primary.Images.ContainsKey(key)) return primary.Images[key];
            if (int.TryParse(key, out var numericIndex) && numericIndex >= 0 && numericIndex < primary.Images.Count) return primary.Images[numericIndex];
        }
        if (StateImageList != null)
        {
            if (StateImageList.Images.ContainsKey(key)) return StateImageList.Images[key];
            if (int.TryParse(key, out var stateIndex) && stateIndex >= 0 && stateIndex < StateImageList.Images.Count) return StateImageList.Images[stateIndex];
        }
        return null;
    }

    private Image? ResolveColumnStateImage(ViewGridColumn col, object row)
    {
        object? spec = col.GetStateImageKeyOrImage(row);
        if (spec == null) return null;
        if (spec is Image image) return image;
        if (spec is Icon icon) return icon.ToBitmap();
        if (spec is int index)
        {
            if (StateImageList != null && index >= 0 && index < StateImageList.Images.Count) return StateImageList.Images[index];
            if (SmallImageList != null && index >= 0 && index < SmallImageList.Images.Count) return SmallImageList.Images[index];
            return null;
        }
        string key = Convert.ToString(spec) ?? string.Empty;
        if (key.Length == 0) return null;
        if (StateImageList != null)
        {
            if (StateImageList.Images.ContainsKey(key)) return StateImageList.Images[key];
            if (int.TryParse(key, out var stateIndex) && stateIndex >= 0 && stateIndex < StateImageList.Images.Count) return StateImageList.Images[stateIndex];
        }
        if (SmallImageList != null)
        {
            if (SmallImageList.Images.ContainsKey(key)) return SmallImageList.Images[key];
            if (int.TryParse(key, out var smallIndex) && smallIndex >= 0 && smallIndex < SmallImageList.Images.Count) return SmallImageList.Images[smallIndex];
        }
        return null;
    }

    private bool ColumnMayDrawCellImage(ViewGridColumn col)
    {
        if (col == null) return false;
        if (col.Kind == ViewGridColumnKind.Image || col.Kind == ViewGridColumnKind.Icon) return true;
        if (!ImageGetterAppliesToTextColumns || !ShowImagesOnSubItems) return false;
        return col.ImageGetter != null
            || col.StateImageGetter != null
            || col.ImageKey != null
            || col.ImageIndex >= 0
            || !string.IsNullOrWhiteSpace(col.ImageAspectName)
            || col.Image != null;
    }

    private Rectangle DrawColumnImagesIfNeeded(Graphics g, Rectangle r, object row, ViewGridColumn col)
    {
        int x = r.X;
        int iconSize = Math.Min(20, Math.Max(16, r.Height - 6));

        var state = ResolveColumnStateImage(col, row);
        if (state != null)
        {
            var sr = new Rectangle(x, r.Y + (r.Height - iconSize) / 2, iconSize, iconSize);
            g.DrawImage(state, sr);
            x += iconSize + 4;
        }

        var img = ResolveColumnImage(col, row, preferLarge: false);
        if (img != null)
        {
            var ir = new Rectangle(x, r.Y + (r.Height - iconSize) / 2, iconSize, iconSize);
            g.DrawImage(img, ir);
            x += iconSize + 4;
        }

        return new Rectangle(x, r.Y, Math.Max(0, r.Right - x), r.Height);
    }

    private void DrawIconText(Graphics g, Rectangle r, object row, ViewGridColumn col, object? val, Color fore)
    {
        var textRect = DrawColumnImagesIfNeeded(g, r, row, col);
        TextRenderer.DrawText(g, Convert.ToString(val) ?? string.Empty, Font, textRect, fore, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
    private void DrawBadge(Graphics g, Rectangle r, string text, Color cellBack)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        Color accent = BadgeUseSemanticStatusColors ? ResolveSemanticStatusColor(text) : _theme.AccentColor;
        Color back = EnableV273RenderingUx
            ? Blend(accent, cellBack, _theme.IsDark ? 0.72 : 0.84)
            : Color.FromArgb(70, accent);
        Color border = EnsureVisibleStroke(accent, cellBack);
        Color fore = BestTextOn(back, _theme.ForeColor);

        var size = TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.NoPadding);
        int width = Math.Min(r.Width, Math.Max(24, size.Width + (EnableV273RenderingUx ? 22 : 18)));
        int height = Math.Max(16, Math.Min(r.Height - 4, size.Height + 8));
        var br = new Rectangle(r.X, r.Y + Math.Max(0, (r.Height - height) / 2), width, height);

        using var b = new SolidBrush(back);
        using var p = new Pen(border);
        g.FillRoundedRectangle(b, br, Math.Clamp(CellPillCornerRadius, 4, 18));
        g.DrawRoundedRectangle(p, br, Math.Clamp(CellPillCornerRadius, 4, 18));
        using var badgeFont = EnableV273RenderingUx ? new Font(Font.FontFamily, Font.Size, FontStyle.Bold) : null;
        TextRenderer.DrawText(g, text, badgeFont ?? Font, br, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private Color ResolveSemanticStatusColor(string text)
    {
        string key = (text ?? string.Empty).Trim().ToUpperInvariant();
        if (key.Contains("FAIL") || key.Contains("ERROR") || key.Contains("HATA") || key.Contains("DURDU") || key.Contains("OFFLINE") || key.Contains("EKSİK") || key.Contains("EKSIK"))
            return Color.FromArgb(225, 83, 83);
        if (key.Contains("WARN") || key.Contains("UYARI") || key.Contains("WAIT") || key.Contains("BEK") || key.Contains("KONTROL") || key.Contains("REVIEW"))
            return Color.FromArgb(230, 166, 60);
        if (key.Contains("OK") || key.Contains("PASS") || key.Contains("DONE") || key.Contains("CLOSED") || key.Contains("ONLINE") || key.Contains("HAZIR") || key.Contains("ÇÖZ") || key.Contains("COZ"))
            return Color.FromArgb(75, 181, 118);
        if (key.Contains("PROGRESS") || key.Contains("OPEN") || key.Contains("AÇIK") || key.Contains("ACIK") || key.Contains("INFO") || key.Contains("BİLGİ") || key.Contains("BILGI"))
            return Color.FromArgb(84, 151, 236);
        if (key.Contains("SAP") || key.Contains("BOM") || key.Contains("PCB"))
            return Color.FromArgb(137, 110, 235);
        return _theme.AccentColor;
    }

    private void DrawHyperlink(Graphics g, Rectangle r, string text, Color cellBack)
    {
        Color link = EnsureVisibleAccent(_theme.AccentColor, cellBack);
        using var f = new Font(Font, EnableV273RenderingUx ? FontStyle.Bold | FontStyle.Underline : FontStyle.Underline);
        TextRenderer.DrawText(g, text, f, r, link, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private void DrawToggleSwitch(Graphics g, Rectangle r, bool isOn)
    {
        var sw = new Rectangle(r.X, r.Y + Math.Max(0, (r.Height - 18) / 2), Math.Min(42, r.Width), 18);
        Color back = isOn ? EnsureVisibleAccent(_theme.AccentColor, _theme.BackColor) : EnsureContrastSurface(_theme.ControlBackColor, _theme.BackColor);
        Color border = EnsureVisibleStroke(_theme.BorderColor, _theme.BackColor);
        using var b = new SolidBrush(back);
        using var p = new Pen(border);
        g.FillRoundedRectangle(b, sw, 9);
        g.DrawRoundedRectangle(p, sw, 9);
        int knobX = isOn ? sw.Right - 17 : sw.Left + 2;
        using var kb = new SolidBrush(Color.White);
        g.FillEllipse(kb, knobX, sw.Top + 2, 14, 14);
    }

    private void DrawSparkline(Graphics g, Rectangle r, object? val, Color cellBack)
    {
        var values = ExtractSparklineValues(val).ToArray();
        if (values.Length < 2) return;
        double min = values.Min();
        double max = values.Max();
        double span = Math.Abs(max - min) < 0.0001 ? 1 : max - min;
        var pts = new PointF[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            float x = r.Left + i * (r.Width - 2f) / Math.Max(1, values.Length - 1);
            float y = r.Bottom - 3 - (float)((values[i] - min) / span * Math.Max(1, r.Height - 6));
            pts[i] = new PointF(x, y);
        }
        using var p = new Pen(EnsureVisibleAccent(_theme.AccentColor, cellBack), 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLines(p, pts);
    }

    private static IEnumerable<double> ExtractSparklineValues(object? val)
    {
        if (val is IEnumerable<int> ints) return ints.Select(x => (double)x);
        if (val is IEnumerable<double> doubles) return doubles;
        if (val is IEnumerable<float> floats) return floats.Select(x => (double)x);
        var text = Convert.ToString(val) ?? string.Empty;
        return text.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => double.TryParse(x, out var n) ? n : double.NaN)
            .Where(x => !double.IsNaN(x));
    }

    private void DrawTags(Graphics g, Rectangle r, string text, ViewGridColumn col, Color cellBack)
    {
        int x = r.X;
        foreach (var tag in text.Split(new[] { col.TagSeparator }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).Where(t => t.Length > 0))
        {
            Color accent = TagsUseChipRenderer ? ResolveSemanticStatusColor(tag) : _theme.AccentColor;
            int w = Math.Min(r.Right - x, TextRenderer.MeasureText(tag, Font, Size.Empty, TextFormatFlags.NoPadding).Width + 18);
            if (w <= 12) break;
            var br = new Rectangle(x, r.Y + Math.Max(0, (r.Height - Math.Max(16, r.Height - 8)) / 2), w, Math.Max(16, r.Height - 8));
            Color back = Blend(accent, cellBack, _theme.IsDark ? 0.76 : 0.88);
            using var b = new SolidBrush(back);
            using var p = new Pen(EnsureVisibleStroke(accent, cellBack));
            g.FillRoundedRectangle(b, br, Math.Clamp(CellPillCornerRadius, 4, 18));
            g.DrawRoundedRectangle(p, br, Math.Clamp(CellPillCornerRadius, 4, 18));
            TextRenderer.DrawText(g, tag, Font, br, BestTextOn(back, _theme.ForeColor), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            x += w + 5;
        }
    }

    private void DrawColorSwatch(Graphics g, Rectangle r, object? val, Color fore)
    {
        Color color = Color.Empty;
        if (val is Color c) color = c;
        else
        {
            try { color = ColorTranslator.FromHtml(Convert.ToString(val) ?? string.Empty); } catch { color = _theme.AccentColor; }
        }
        var sw = new Rectangle(r.X, r.Y + 5, 22, Math.Max(12, r.Height - 10));
        using var b = new SolidBrush(color);
        using var p = new Pen(EnsureVisibleStroke(_theme.BorderColor, _theme.BackColor));
        g.FillRoundedRectangle(b, sw, 4);
        g.DrawRoundedRectangle(p, sw, 4);
        TextRenderer.DrawText(g, Convert.ToString(val) ?? color.Name, Font, new Rectangle(sw.Right + 6, r.Y, Math.Max(0, r.Right - sw.Right - 6), r.Height), fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    private void DrawSummaryFooter(Graphics g)
    {
        var r = new Rectangle(0, Height - FooterHeight, Width - VBarWidth, FooterHeight);
        using var b = new SolidBrush(_theme.TextBackColor); g.FillRectangle(b, r);
        using var p = new Pen(_theme.BorderColor); g.DrawLine(p, r.Left, r.Top, r.Right, r.Top);
        int x = -_scrollX;
        _summaryTextCache ??= BuildSummaryTextCache();
        foreach (var col in Columns.VisibleColumns)
        {
            var cr = new Rectangle(x, r.Top, col.Width, r.Height);
            if (cr.Right < 0) { x += col.Width; continue; }
            if (cr.Left > Width - VBarWidth) break;
            _summaryTextCache.TryGetValue(col, out var text);
            TextRenderer.DrawText(g, text, Font, Rectangle.Inflate(cr, -6, 0), _theme.TextForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.EndEllipsis);
            g.DrawLine(p, cr.Right-1, cr.Top, cr.Right-1, cr.Bottom);
            x += col.Width;
        }
    }
    private void DrawRowDetailsGlyph(Graphics g, Rectangle rowRect)
    {
        using var p = new Pen(_theme.AccentColor, 2);
        g.DrawLine(p, rowRect.Left + 3, rowRect.Bottom - 2, rowRect.Right - 3, rowRect.Bottom - 2);
    }
    private void DrawEmpty(Graphics g)
    {
        if (!EnableModernEmptyState)
        {
            using var italic = new Font(Font, FontStyle.Italic);
            TextRenderer.DrawText(g, EmptyListMessage, italic, ClientRectangle, _theme.EmptyTextColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        string title = string.IsNullOrWhiteSpace(EmptyListMessage) ? ViewGridText.EmptyList : EmptyListMessage;
        int w = Math.Min(420, Math.Max(240, ClientSize.Width - 48));
        int h = 118;
        var card = new Rectangle((ClientSize.Width - w) / 2, Math.Max(HeaderHeight + 20, (ClientSize.Height - h) / 2), w, h);
        using var shadow = new SolidBrush(Color.FromArgb(_theme.IsDark ? 70 : 26, Color.Black));
        var shadowRect = card; shadowRect.Offset(0, 3);
        g.FillRoundedRectangle(shadow, shadowRect, 18);
        using (var b = new SolidBrush(Blend(_theme.PanelBackColor == Color.Empty ? _theme.BackColor : _theme.PanelBackColor, _theme.AccentColor, _theme.IsDark ? 0.08 : 0.04)))
            g.FillRoundedRectangle(b, card, 18);
        using (var pen = new Pen(Blend(_theme.BorderColor, _theme.AccentColor, 0.22))) g.DrawRoundedRectangle(pen, card, 18);

        var iconRect = new Rectangle(card.Left + 22, card.Top + 32, 54, 54);
        using (var ib = new SolidBrush(Blend(_theme.AccentColor, _theme.BackColor, _theme.IsDark ? 0.62 : 0.78))) g.FillEllipse(ib, iconRect);
        using (var ip = new Pen(_theme.AccentColor, 2))
        {
            g.DrawEllipse(ip, iconRect);
            g.DrawLine(ip, iconRect.Left + 16, iconRect.Top + 29, iconRect.Right - 16, iconRect.Top + 29);
            g.DrawLine(ip, iconRect.Left + 16, iconRect.Top + 38, iconRect.Right - 22, iconRect.Top + 38);
        }

        var titleRect = new Rectangle(card.Left + 92, card.Top + 28, card.Width - 116, 32);
        using var titleFont = new Font(Font.FontFamily, Math.Max(10.5f, Font.Size + 1.5f), FontStyle.Bold);
        TextRenderer.DrawText(g, title, titleFont, titleRect, _theme.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        var subRect = new Rectangle(card.Left + 92, card.Top + 60, card.Width - 116, 34);
        Color subFore = global::ViewGrid.Theming.ViewGridDialogThemeApplier.EnsureReadableTextColor(_theme.MutedForeColor, _theme.PanelBackColor == Color.Empty ? _theme.BackColor : _theme.PanelBackColor);
        TextRenderer.DrawText(g, ViewGridText.SearchPlaceholder, Font, subRect, subFore, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);

        DrawEmptyStateSignature(g, card);
    }

    private void DrawEmptyStateSignature(Graphics g, Rectangle card)
    {
        if (!ShowEmptyStateSignature || EmptyStateSignatureAlignment == ViewGridEmptyStateSignatureAlignment.Hidden) return;

        string signature = GetEmptyStateSignatureText();
        if (string.IsNullOrWhiteSpace(signature)) return;

        using var sigFont = new Font(Font.FontFamily, Math.Max(7.5f, Font.Size - 1.0f), FontStyle.Regular);
        int pad = 10;
        int maxTextWidth = Math.Max(120, card.Width - (pad * 2));
        Size textSize = TextRenderer.MeasureText(signature, sigFont, new Size(maxTextWidth, int.MaxValue), TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        int textWidth = Math.Min(maxTextWidth, Math.Max(120, textSize.Width + 2));
        int textHeight = textSize.Height + 2;
        int x;
        int y;

        switch (EmptyStateSignatureAlignment)
        {
            case ViewGridEmptyStateSignatureAlignment.BottomLeft:
                x = card.Left + pad;
                y = card.Bottom - textHeight - 6;
                break;
            case ViewGridEmptyStateSignatureAlignment.TopRight:
                x = card.Right - textWidth - pad;
                y = card.Top + 6;
                break;
            case ViewGridEmptyStateSignatureAlignment.TopLeft:
                x = card.Left + pad;
                y = card.Top + 6;
                break;
            case ViewGridEmptyStateSignatureAlignment.CenterBottom:
                x = card.Left + (card.Width - textWidth) / 2;
                y = card.Bottom - textHeight - 6;
                break;
            case ViewGridEmptyStateSignatureAlignment.BottomRight:
            default:
                x = card.Right - textWidth - pad;
                y = card.Bottom - textHeight - 6;
                break;
        }

        Color surface = _theme.PanelBackColor == Color.Empty ? _theme.BackColor : _theme.PanelBackColor;
        Color baseFore = global::ViewGrid.Theming.ViewGridDialogThemeApplier.EnsureReadableTextColor(_theme.MutedForeColor, surface);
        double opacity = Math.Clamp(EmptyStateSignatureOpacity, 0.20, 1.0);
        Color signatureFore = Blend(surface, baseFore, opacity);
        var textRect = new Rectangle(x, y, textWidth, textHeight);
        TextRenderer.DrawText(g, signature, sigFont, textRect, signatureFore, TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
    }

    private static string GetEmptyStateSignatureText()
    {
        string versionText = "ViewGrid";
        try
        {
            var version = typeof(ViewGridControl).Assembly.GetName().Version;
            if (version != null)
                versionText = $"ViewGrid v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
        catch
        {
        }

        return versionText + Environment.NewLine + "Created by Güner Taylan";
    }

    private static int ToInt(object? v) { try { return Convert.ToInt32(v); } catch { return 0; } }

    private void DrawFluentBackground(Graphics g)
    {
        var baseColor = _theme.BackColor;
        using (var back = new SolidBrush(baseColor)) g.FillRectangle(back, ClientRectangle);
        if (!EnableFluentBackdrop && !_theme.UseFluentBackdrop) return;

        var panel = _theme.PanelBackColor == Color.Empty ? Blend(baseColor, _theme.AccentColor, _theme.IsDark ? 0.06 : 0.03) : _theme.PanelBackColor;
        using var overlay = new SolidBrush(Color.FromArgb(Math.Clamp(_theme.AcrylicOpacity, 170, 255), panel));
        var r = new Rectangle(1, 1, Math.Max(1, Width - VBarWidth - 2), Math.Max(1, Height - _hbar.Height - 2));
        g.FillRectangle(overlay, r);

        if (EnableAcrylicSimulation || _theme.UseAcrylicEffect)
        {
            using var glow = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(_theme.IsDark ? 34 : 26, _theme.AccentColor),
                Color.FromArgb(0, _theme.AccentColor), LinearGradientMode.ForwardDiagonal);
            g.FillRectangle(glow, ClientRectangle);
        }
    }

    private void DrawAcrylicGloss(Graphics g, Rectangle r)
    {
        using var gloss = new LinearGradientBrush(r,
            Color.FromArgb(_theme.IsDark ? 18 : 45, Color.White),
            Color.FromArgb(0, Color.White), LinearGradientMode.Vertical);
        g.FillRectangle(gloss, r);
    }

    private void DrawRowBackground(Graphics g, Rectangle r, Color rowBack, bool selected, int anim)
    {
        if (selected && EnableSoftShadows)
            DrawSoftShadow(g, Rectangle.Inflate(r, -3, -2), Math.Max(4, _theme.CornerRadius));
        using var b = new SolidBrush(rowBack);
        if (selected && EnableRoundedCells)
            g.FillRoundedRectangle(b, Rectangle.Inflate(r, -2, -2), Math.Max(4, _theme.CornerRadius));
        else
            g.FillRectangle(b, r);
        if (selected && EnableAnimatedSelection && anim > 0)
        {
            using var glow = new Pen(Color.FromArgb(Math.Min(180, anim), _theme.AccentColor), 2f);
            g.DrawRoundedRectangle(glow, Rectangle.Inflate(r, -3, -3), Math.Max(4, _theme.CornerRadius));
        }
    }

    private void DrawSoftShadow(Graphics g, Rectangle r, int radius)
    {
        if (r.Width <= 4 || r.Height <= 4) return;
        using var p1 = new Pen(Color.FromArgb(_theme.IsDark ? 80 : 34, Color.Black), 1f);
        using var p2 = new Pen(Color.FromArgb(_theme.IsDark ? 35 : 18, Color.Black), 1f);
        g.DrawRoundedRectangle(p2, OffsetRect(r, 0, 2), radius);
        g.DrawRoundedRectangle(p1, OffsetRect(r, 0, 1), radius);
    }

    private Color GetSurfaceColor(Color baseColor, double amount)
        => Blend(baseColor, _theme.AccentColor, amount);

    private static Color Blend(Color a, Color b, double amount)
    {
        amount = Math.Max(0, Math.Min(1, amount));
        return Color.FromArgb(
            (int)(a.R + (b.R - a.R) * amount),
            (int)(a.G + (b.G - a.G) * amount),
            (int)(a.B + (b.B - a.B) * amount));
    }

    private static Color BestTextOn(Color back, Color preferred)
    {
        return global::ViewGrid.Theming.ViewGridThemeAccessibility.EnsureReadableTextColor(preferred, back, 4.5d);
    }

    private static Color EnsureVisibleAccent(Color accent, Color back)
    {
        if (ContrastRatio(back, accent) >= 3.0) return accent;
        return GetLuminance(back) < 0.5 ? Color.FromArgb(255, 214, 64) : Color.FromArgb(0, 86, 160);
    }

    private static Color EnsureVisibleStroke(Color stroke, Color back)
    {
        if (ContrastRatio(back, stroke) >= 2.0) return stroke;
        return GetLuminance(back) < 0.5 ? Color.FromArgb(210, 210, 210) : Color.FromArgb(70, 70, 70);
    }

    private static Color EnsureContrastSurface(Color surface, Color back)
    {
        if (ContrastRatio(back, surface) >= 1.25) return surface;
        return GetLuminance(back) < 0.5 ? Color.FromArgb(55, 55, 55) : Color.FromArgb(238, 238, 238);
    }

    private static Rectangle OffsetRect(Rectangle r, int dx, int dy)
        => new Rectangle(r.X + dx, r.Y + dy, r.Width, r.Height);

    private static double ContrastRatio(Color a, Color b)
    {
        var l1 = GetLuminance(a) + 0.05;
        var l2 = GetLuminance(b) + 0.05;
        return Math.Max(l1, l2) / Math.Min(l1, l2);
    }

    private static double GetLuminance(Color c)
    {
        static double Channel(byte v)
        {
            var x = v / 255.0;
            return x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    private void StartSelectionAnimation(int row)
    {
        if (!EnableAnimatedSelection || row < 0) return;
        _selectionAnimations[row] = 40;
        if (!_animationTimer.Enabled) _animationTimer.Start();
    }

    private void StepSelectionAnimation()
    {
        if (_selectionAnimations.Count == 0) { _animationTimer.Stop(); return; }
        foreach (var key in _selectionAnimations.Keys.ToList())
        {
            int next = _selectionAnimations[key] + _renderOptions.SelectionAnimationStep;
            if (next >= 255) _selectionAnimations.Remove(key);
            else _selectionAnimations[key] = next;
        }
        Invalidate();
    }


    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (TryScrollCellOverflowAt(e.Location, e.Delta)) return;
                if (ModifierKeys.HasFlag(Keys.Control))
        {
            RowHeight = Math.Clamp(RowHeight + (e.Delta > 0 ? 2 : -2), 20, 80);
            return;
        }
        if (ModifierKeys.HasFlag(Keys.Shift) && _hbar.Visible)
        {
            int hMove = Math.Sign(e.Delta) * -_hbar.SmallChange * 3;
            ScrollHorizontal(_scrollX + hMove);
            return;
        }
        int rowsPerNotch = Math.Max(1, MouseWheelRowsPerNotch);
        if (SmoothMouseWheelScroll)
        {
            _smoothWheelRemainder += e.Delta;
            int notches = _smoothWheelRemainder / 120;
            if (notches == 0) return;
            _smoothWheelRemainder -= notches * 120;
            ScrollVertical(_scrollY - notches * rowsPerNotch);
        }
        else
        {
            int lines = Math.Max(1, SystemInformation.MouseWheelScrollLines);
            int move = Math.Sign(e.Delta) * -lines;
            ScrollVertical(_scrollY + move);
        }
    }

    private void ScrollVertical(int value)
    {
        int max = Math.Max(0, _vbar.Maximum - _vbar.LargeChange + 1);
        value = Math.Clamp(value, 0, max);
        if (_vbar.Value != value) _vbar.Value = value;
        _scrollY = value;
        PositionRowDetailsControl();
        Invalidate();
    }

    private void ScrollHorizontal(int value)
    {
        int max = Math.Max(0, _hbar.Maximum - _hbar.LargeChange + 1);
        value = Math.Clamp(value, 0, max);
        if (_hbar.Value != value) _hbar.Value = value;
        _scrollX = value;
        PositionRowDetailsControl();
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_resizingColumn != null)
        {
            SetResizingColumnWidth(_resizingColumn, Math.Max(28, _resizeStartWidth + e.X - _resizeStartX));
            RefreshView();
            return;
        }
        if (_dragColumn != null)
        {
            if (!_dragColumnActive && Math.Abs(e.X - _dragColumnStartX) > SystemInformation.DragSize.Width / 2)
                _dragColumnActive = true;
            if (_dragColumnActive)
            {
                _dragColumnInsertIndex = HitColumnInsertIndex(e.X);
                Cursor = Cursors.SizeWE;
                Invalidate();
                return;
            }
        }
        int row = HitRow(e.Y);
        if (!IsTileView && ShowHeader && e.Y < HeaderHeight && EnableColumnResize && HitColumnResizeEdge(e.X) != null) Cursor = Cursors.VSplit;
        else if (IsTileView && IsMediaPlaybackHot(e.Location, row)) Cursor = Cursors.Hand;
        else if (IsTileView && TryGetTileCheckBoxHit(e.Location, row, out _)) Cursor = Cursors.Hand;
        else Cursor = Cursors.Default;
        if (row != _hotRow) { _hotRow = row; Invalidate(); }
    }
    protected override void OnMouseLeave(EventArgs e) { if (_resizingColumn == null) { _hotRow = -1; Cursor = Cursors.Default; Invalidate(); } base.OnMouseLeave(e); }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_resizingColumn != null)
        {
            _resizingColumn = null;
            Cursor = Cursors.Default;
            OnColumnLayoutChanged();
            return;
        }
        if (_dragColumn != null)
        {
            var dragged = _dragColumn;
            int target = _dragColumnInsertIndex;
            bool wasActive = _dragColumnActive;
            _dragColumn = null;
            _dragColumnActive = false;
            _dragColumnInsertIndex = -1;
            Cursor = Cursors.Default;
            if (wasActive && target >= 0) MoveColumn(dragged, target);
            else HeaderClick(e.Location);
            return;
        }
        if (e.Button == MouseButtons.Right)
        {
            if (TryShowGLVBodyContextMenu(e.Location))
                return;

            if (ContextMenuStrip != null && MergeBuiltInMenuWithUserContextMenu)
            {
                MergeBuiltInMenuIntoUserContextMenu(e.Location);
                ApplyViewGridThemeToMenu(ContextMenuStrip);
                // WinForms will show the user ContextMenuStrip normally after MouseUp.
                return;
            }

            if (ContextMenuStrip != null)
                ApplyViewGridThemeToMenu(ContextMenuStrip);

            ShowBuiltInContextMenu(e.Location);
            return;
        }
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (!IsTileView && ShowHeader && e.Y < HeaderHeight && EnableColumnResize)
        {
            var resizeCol = HitColumnResizeEdge(e.X);
            if (resizeCol != null && resizeCol.AllowResize && e.Button == MouseButtons.Left)
            {
                _resizingColumn = resizeCol;
                _resizeStartX = e.X;
                _resizeStartWidth = resizeCol.Width;
                Cursor = Cursors.VSplit;
                return;
            }
        }
        if (ShowHeader && e.Y < HeaderHeight)
        {
            if (e.Button == MouseButtons.Left && AllowColumnReorder && !IsTileView)
            {
                var dragCol = HitColumn(e.X);
                if (dragCol != null)
                {
                    int right = GetColumnLeft(dragCol) + dragCol.Width;
                    if (dragCol.AllowReorder && Math.Abs(e.X - right) > 6 && !GetColumnFilterButtonRect(dragCol).Contains(e.Location))
                    {
                        _dragColumn = dragCol;
                        _dragColumnStartX = e.X;
                        _dragColumnInsertIndex = Columns.VisibleColumns.ToList().IndexOf(dragCol);
                        return;
                    }
                }
            }
            if (e.Button == MouseButtons.Left) HeaderClick(e.Location);
            return;
        }
        int rowIndex = HitRow(e.Y); var col = HitColumn(e.X);
        if (rowIndex < 0) return;
        _activeColumn = col;
        if (IsGroupRow(rowIndex))
        {
            if (e.Button == MouseButtons.Left) ToggleGroup(GetGroupKey(rowIndex));
            return;
        }

        var row = GetViewRow(rowIndex);
        if (row == null) return;

        if (e.Button == MouseButtons.Left && TryProcessMediaPlaybackClick(e.Location, rowIndex, row))
        {
            Invalidate();
            return;
        }

        if (e.Button == MouseButtons.Left && TryProcessCardActionClick(e.Location, rowIndex, row))
        {
            Invalidate();
            return;
        }

        if (e.Button == MouseButtons.Left && TryGetTileCheckBoxHit(e.Location, rowIndex, out var tileCheckColumn) && tileCheckColumn != null)
        {
            UpdateSelectionFromMouse(rowIndex, ModifierKeys);
            _activeColumn = tileCheckColumn;
            var tileArgs = new ViewGridCellClickEventArgs(rowIndex, row, tileCheckColumn);
            CellClick?.Invoke(this, tileArgs);
            ToggleBool(row, tileCheckColumn);
            return;
        }

        if (col == null) return;

        if (e.Button == MouseButtons.Right)
        {
            // Preserve an existing multi-selection when the user right-clicks one of the
            // already selected rows. This mirrors ListView/ViewGrid behavior and
            // avoids rebuilding SelectedObjects for virtual/query providers.
            if (!_selectedRows.Contains(rowIndex))
                UpdateSelectionFromMouse(rowIndex, Keys.None);
        }
        else
        {
            UpdateSelectionFromMouse(rowIndex, ModifierKeys);
        }

        var cellArgs = new ViewGridCellClickEventArgs(rowIndex, row, col);
        CellClick?.Invoke(this, cellArgs);

        bool leftButton = e.Button == MouseButtons.Left;
        bool allowMouseAction = leftButton || !ButtonClickOnlyWithLeftMouseButton;
        if (leftButton && (IsCompatibilityCheckBoxHostColumn(col) || col.CellCheckBox) && GetRowCompatibilityCheckBoxRect(GetCellBounds(rowIndex, col)).Contains(e.Location)) ToggleBool(row, col);
        else if (leftButton && (col.Kind == ViewGridColumnKind.CheckBox || col.Kind == ViewGridColumnKind.ToggleSwitch)) ToggleBool(row, col);
        else if (leftButton && col.Kind == ViewGridColumnKind.Rating) SetRatingFromClick(e.X, row, col);
        else if (allowMouseAction && col.Kind == ViewGridColumnKind.Button) ButtonClick?.Invoke(this, cellArgs);
        else if (leftButton && col.Kind == ViewGridColumnKind.Hyperlink) HyperlinkClick?.Invoke(this, cellArgs);
        else if (leftButton && col.Kind == ViewGridColumnKind.ComboBox && EnableCellEditing && CanEditColumn(col)) BeginEdit(rowIndex, col);
        Invalidate();
    }
    protected override void OnDoubleClick(EventArgs e)
    {
        base.OnDoubleClick(e);
        var pt = PointToClient(MousePosition);
        if (EnableColumnAutoResizeOnDoubleClick && !IsTileView && ShowHeader && pt.Y < HeaderHeight)
        {
            var edgeCol = HitColumnResizeEdge(pt.X);
            if (edgeCol != null && edgeCol.AllowResize)
            {
                AutoResizeColumnToContent(edgeCol);
                return;
            }
        }
        int rowIndex = HitRow(pt.Y);
        if (IsGroupRow(rowIndex)) { ToggleGroup(GetGroupKey(rowIndex)); return; }
        if (EnableRowDetails) ToggleRowDetails(rowIndex);
        var col = HitColumn(pt.X);
        var activeRow = rowIndex >= 0 ? GetViewRow(rowIndex) : null;
        if (activeRow != null && col != null && col.CellOverflowDetailsOnDoubleClick)
        {
            ShowCellOverflowReaderPopup(rowIndex, col, activeRow);
            return;
        }
        if (activeRow != null && col != null) ItemActivate?.Invoke(this, new ViewGridCellClickEventArgs(rowIndex, activeRow, col));
        if (!EnableCellEditing || !CellEditActivationOnDoubleClick) return;
        if (rowIndex < 0 || col == null || !CanEditColumn(col)) return;
        BeginEdit(rowIndex, col);
    }

    private void HeaderClick(Point p)
    {
        var col = HitColumn(p.X); if (col == null) return;
        int left = GetColumnLeft(col);
        int right = left + col.Width;
        if (col.HeaderCheckBox && GetHeaderCheckBoxRect(col).Contains(p))
        {
            ToggleHeaderCheckBox(col);
            return;
        }
        if (ShouldShowColumnFilterButton(col) && GetColumnFilterButtonRect(col).Contains(p))
        {
            ShowConfiguredFilterMenuForColumn(col, new Point(left, HeaderHeight));
            return;
        }
        if (SortOnColumnClick && col.Sortable)
        {
            if (_sortColumn == col) SortBy(col, !_sortDesc); else SortBy(col, false);
        }
    }


    private void ShowConfiguredFilterMenuForColumn(ViewGridColumn col, Point clientLocation)
    {
        if (FilterMenuMode == ViewGridFilterMenuMode.PopupMenu || UseEmbeddedHeaderFilterMenu)
        {
            ShowPopupFilterDropDown(col, clientLocation, includeOpenWindowItem: true);
            return;
        }

        if (FilterMenuMode == ViewGridFilterMenuMode.Both)
        {
            ShowPopupFilterDropDown(col, clientLocation, includeOpenWindowItem: true);
            return;
        }

        ShowFilterMenuForColumn(col, clientLocation);
    }

    private void ShowPopupFilterDropDown(ViewGridColumn col, Point clientLocation, bool includeOpenWindowItem)
    {
        if (_activeHeaderMenu != null && !_activeHeaderMenu.IsDisposed)
        {
            try { _activeHeaderMenu.Close(ToolStripDropDownCloseReason.CloseCalled); }
            catch { }
        }

        // v28.12.2: Filtre ikonundan açılan popup ile menü içinden açılan popup aynı render/resize yolunu kullansın.
        // Eski ContextMenuStrip-hosted yol görsel olarak farklı kalıyordu ve Card/Tile/Dashboard gibi başlıksız
        // görünümlerde anchor noktası header'a göre hesaplandığı için popup alakasız yerde açılabiliyordu.
        if (UseUnifiedFloatingFilterPopup || FilterPopupResizable || FilterPopupShowValueTooltips || FilterPopupAutoWidthForLongValues)
        {
            ShowFloatingEmbeddedFilterForm(null, col, PointToScreen(NormalizeFilterPopupClientAnchor(col, clientLocation)));
            return;
        }

        _allowActiveMenuClose = false;
        var menu = new ContextMenuStrip
        {
            Renderer = new global::ViewGrid.Theming.SmartMenuRenderer(_theme),
            BackColor = _theme.PanelBackColor,
            ForeColor = _theme.ForeColor,
            AutoClose = true,
            ShowImageMargin = false,
            ShowCheckMargin = false
        };
        _activeHeaderMenu = menu;
        ToolStripItem? filterLauncherItem = null;
        menu.Closing += (_, e) =>
        {
            bool pointerInside = menu.Bounds.Contains(Control.MousePosition) || PointerInsideOpenDropDown(menu.Items);
            bool hostedControlHasFocus = HostedControlContainsFocus(menu.Items);
            if (!_allowActiveMenuClose && (pointerInside || hostedControlHasFocus) &&
                e.CloseReason != ToolStripDropDownCloseReason.CloseCalled &&
                e.CloseReason != ToolStripDropDownCloseReason.ItemClicked)
            {
                e.Cancel = true;
            }
        };
        menu.Closed += (_, __) =>
        {
            RemoveMenuDismissMessageFilter();
            _allowActiveMenuClose = false;
            if (ReferenceEquals(_activeHeaderMenu, menu)) _activeHeaderMenu = null;
            BeginInvoke(new Action(() =>
            {
                try { if (!menu.IsDisposed) menu.Dispose(); }
                catch { }
            }));
        };

        foreach (ToolStripItem item in CreateEmbeddedFilterMenuItem(col).DropDownItems.Cast<ToolStripItem>().ToList())
        {
            if (item is ToolStripSeparator) menu.Items.Add(new ToolStripSeparator());
            else menu.Items.Add(item);
        }

        if (includeOpenWindowItem)
        {
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(ViewGridText.SeparateFilterWindow, null, (_,__) =>
            {
                CloseActiveMenusSafe();
                BeginInvoke(new Action(() => ShowFilterMenuForColumn(col, new Point(Math.Max(0, GetColumnLeft(col)), HeaderHeight))));
            });
        }
        menu.Show(this, clientLocation);
    }

    private string BuildFilterPopupSizeMemoryKey(ViewGridColumn col)
    {
        string gridKey = string.IsNullOrWhiteSpace(Name) ? GetType().Name : Name;
        string columnKey = string.IsNullOrWhiteSpace(col.AspectName) ? col.Header : col.AspectName;
        return gridKey + "::" + columnKey;
    }

    private Size GetEffectiveFilterPopupMinimumSize()
    {
        int width = Math.Max(260, FilterPopupMinimumSize.Width);
        int height = Math.Max(320, FilterPopupMinimumSize.Height);
        return new Size(width, height);
    }

    private Size GetEffectiveFilterPopupMaximumSize(Point screenLocation, Size minimumSize)
    {
        Size configured = FilterPopupMaximumSize.IsEmpty ? new Size(1800, 1200) : FilterPopupMaximumSize;
        int width = Math.Max(minimumSize.Width, configured.Width);
        int height = Math.Max(minimumSize.Height, configured.Height);

        if (FilterPopupLimitToWorkingArea)
        {
            Rectangle workingArea = Screen.FromPoint(screenLocation).WorkingArea;
            width = Math.Min(width, Math.Max(minimumSize.Width, workingArea.Width - 24));
            height = Math.Min(height, Math.Max(minimumSize.Height, workingArea.Height - 24));
        }

        return new Size(width, height);
    }

    private Size GetEffectiveFilterWindowMaximumSize(Point clientLocation, Size minimumSize)
    {
        Point screenLocation = PointToScreen(clientLocation);
        Size configured = FilterWindowMaximumSize.IsEmpty ? new Size(2000, 1400) : FilterWindowMaximumSize;
        int width = Math.Max(minimumSize.Width, configured.Width);
        int height = Math.Max(minimumSize.Height, configured.Height);

        if (FilterPopupLimitToWorkingArea)
        {
            Rectangle workingArea = Screen.FromPoint(screenLocation).WorkingArea;
            width = Math.Min(width, Math.Max(minimumSize.Width, workingArea.Width - 24));
            height = Math.Min(height, Math.Max(minimumSize.Height, workingArea.Height - 24));
        }

        return new Size(width, height);
    }

    private Size GetInitialFilterPopupSize(ViewGridColumn col, Size minimumSize, Size maximumSize, string sizeMemoryKey, bool allowRememberedSize)
    {
        if (allowRememberedSize && FilterPopupRememberSize && _floatingFilterPopupSizeMemory.TryGetValue(sizeMemoryKey, out Size rememberedSize))
            return ClampFilterPopupSize(rememberedSize, minimumSize, maximumSize);

        int columnBasedWidth = Math.Max(minimumSize.Width, Math.Max(0, col.Width) + 44);
        int preferredHeight = FilterPopupDefaultSize.Height > 0 ? FilterPopupDefaultSize.Height : minimumSize.Height;
        var desired = new Size(columnBasedWidth, Math.Max(minimumSize.Height, preferredHeight));
        return ClampFilterPopupSize(desired, minimumSize, maximumSize);
    }


    private Size GetInitialFilterWindowSize(ViewGridColumn col, Size minimumSize, Size maximumSize, string sizeMemoryKey, bool allowRememberedSize)
    {
        if (allowRememberedSize && FilterPopupRememberSize && _floatingFilterPopupSizeMemory.TryGetValue(sizeMemoryKey, out Size rememberedSize))
            return ClampFilterPopupSize(rememberedSize, minimumSize, maximumSize);

        // v27.3.1 FIX: Separate window has a fixed command panel at the left.
        // Width must include this area plus the right value/search panel. Otherwise
        // column-fit sizing can open too narrow and the menu appears broken.
        const int leftCommandAreaWidth = 175;
        const int rootPaddingAndGutter = 36;
        int rightPanelWidth = Math.Max(Math.Max(0, col.Width) + 44, 300);
        int preferredWidth = Math.Max(minimumSize.Width, leftCommandAreaWidth + rootPaddingAndGutter + rightPanelWidth);
        int preferredHeight = FilterPopupDefaultSize.Height > 0 ? FilterPopupDefaultSize.Height : minimumSize.Height;
        var desired = new Size(preferredWidth, Math.Max(minimumSize.Height, preferredHeight));
        return ClampFilterPopupSize(desired, minimumSize, maximumSize);
    }

    private static Size ClampFilterPopupSize(Size value, Size minimumSize, Size maximumSize)
    {
        int width = Math.Max(minimumSize.Width, Math.Min(maximumSize.Width, value.Width));
        int height = Math.Max(minimumSize.Height, Math.Min(maximumSize.Height, value.Height));
        return new Size(width, height);
    }

    private void ShowFilterMenuForColumn(ViewGridColumn col, Point clientLocation)
    {
        int initialScanLimit = GetFilterMenuScanLimit();
        bool fastPreview = IsHugeFastFilterPreview() && initialScanLimit < _provider.Count;
        if (EnableSmartFilterEngine && BuildSmartFilterIndexInBackground)
            BeginBuildSmartFilterIndex(col);
        // v24.84 FINAL FILTER FIX:
        // Separate/modal filter window now uses the same smart search provider as the embedded popup.
        // Previously the modal window only received a provider in fast-preview mode, so some paths
        // showed only the initially loaded/selected values while Apply still filtered correctly.
        Func<string, IReadOnlyList<string>>? modalSearchLoader = EnableSmartFilterEngine || fastPreview
            ? searchText => GetDistinctColumnValues(col, GetFilterSearchScanLimit(), searchText)
                .Take(Math.Max(Math.Max(25, SmartFilterPopupValueLimit), MaxEmbeddedFilterVisibleValues))
                .ToList()
            : null;
        string modalSizeKey = BuildFilterPopupSizeMemoryKey(col) + "::window";
        Size modalMinimumSize = GetEffectiveFilterPopupMinimumSize();
        Size modalMaximumSize = GetEffectiveFilterWindowMaximumSize(clientLocation, modalMinimumSize);
        Size modalDefaultSize = GetInitialFilterWindowSize(col, modalMinimumSize, modalMaximumSize, modalSizeKey, allowRememberedSize: false);

        using var menu = new FilterMenuForm(
            col,
            GetDistinctColumnValues(col, initialScanLimit),
            _filters.Get(col.AspectName),
            _theme,
            modalSearchLoader,
            fastPreview || modalSearchLoader != null,
            Math.Max(1, SmartFilterSearchDebounceMs),
            FilterPopupResizable,
            modalDefaultSize,
            modalMinimumSize,
            modalMaximumSize,
            FilterPopupRememberSize,
            modalSizeKey,
            FilterPopupShowValueTooltips,
            false);
        menu.Location = PointToScreen(clientLocation);
        if (menu.ShowDialog(this) == DialogResult.OK)
        {
            switch (menu.Action)
            {
                case FilterMenuAction.ClearAllFilters:
                    ClearFilters();
                    break;
                case FilterMenuAction.SortAscending:
                    SortBy(col, false);
                    break;
                case FilterMenuAction.SortDescending:
                    SortBy(col, true);
                    break;
                case FilterMenuAction.Unsort:
                    ClearSort(col);
                    break;
                case FilterMenuAction.GroupByThisColumn:
                    SetGroupBy(col.AspectName);
                    break;
                case FilterMenuAction.ClearGrouping:
                    ClearGrouping();
                    break;
                default:
                    if (menu.Result != null) SetColumnFilter(menu.Result);
                    break;
            }
        }
    }

    public void SortBy(ViewGridColumn? column, bool descending = false)
    {
        if (column == null || !column.Sortable) return;
        _sortColumn = column;
        _sortDesc = descending;
        BuildViewIndexSmartSort();
        QueueAutoSaveUserLayout();
    }

    private void BuildViewIndexSmartSort()
    {
        if (!AsyncSortForLargeLists || _sortColumn == null || _provider is IQueryRowProvider || _provider.Count < AsyncSortThreshold)
        {
            BuildViewIndex();
            return;
        }

        _sortCts?.Cancel();
        _sortCts = new CancellationTokenSource();
        var token = _sortCts.Token;
        int generation = Interlocked.Increment(ref _sortGeneration);
        var sortColumn = _sortColumn;
        bool sortDesc = _sortDesc;
        var secondaryColumn = _secondarySortColumn;
        bool secondaryDesc = _secondarySortDescending;
        var filters = _filters;
        var columns = Columns.ToArray();
        var provider = _provider;
        int count = provider.Count;
        int scanCount = AsyncSortMaxRows > 0 ? Math.Min(count, AsyncSortMaxRows) : count;

        SetSortBusyState(true);

        Task.Run(() =>
        {
            var entries = new List<ViewGridSortEntry>(Math.Min(scanCount, 250_000));
            for (int i = 0; i < scanCount; i++)
            {
                if (token.IsCancellationRequested) return null;
                var row = provider.GetRow(i);
                if (row == null) continue;
                if (!filters.Passes(row, columns)) continue;
                if (_modelFilter != null && !SafeModelFilterPasses(row)) continue;
                if (!SafeAdditionalFilterPasses(row)) continue;

                object? primaryKey = CacheSortKeysForLargeLists ? sortColumn.GetValue(row) : null;
                object? secondaryKey = CacheSortKeysForLargeLists && secondaryColumn != null ? secondaryColumn.GetValue(row) : null;
                entries.Add(new ViewGridSortEntry(i, primaryKey, secondaryKey));
            }

            entries.Sort((a, b) =>
            {
                if (token.IsCancellationRequested) return 0;
                int primary = CacheSortKeysForLargeLists
                    ? CompareSortValues(a.PrimaryKey, b.PrimaryKey)
                    : CompareRows(provider.GetRow(a.Index), provider.GetRow(b.Index), sortColumn);
                if (primary != 0 || secondaryColumn == null) return sortDesc ? -primary : primary;

                int secondary = CacheSortKeysForLargeLists
                    ? CompareSortValues(a.SecondaryKey, b.SecondaryKey)
                    : CompareRows(provider.GetRow(a.Index), provider.GetRow(b.Index), secondaryColumn);
                return secondaryDesc ? -secondary : secondary;
            });

            return entries.Select(x => x.Index).ToList();
        }, token).ContinueWith(t =>
        {
            try
            {
                if (IsDisposed || token.IsCancellationRequested || generation != _sortGeneration) return;
                if (t.Status == TaskStatus.RanToCompletion && t.Result != null)
                {
                    _viewIndexes.Clear();
                    _viewIndexes.AddRange(t.Result);
                    _viewIsDirect = false;
                    BuildDisplayRows();
                }
            }
            catch { }
            finally
            {
                if (!IsDisposed && generation == _sortGeneration)
                {
                    SetSortBusyState(false);
                    RefreshView();
                }
            }
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void ClearSort(ViewGridColumn? column = null)
    {
        if (column == null || _sortColumn == column)
        {
            _sortColumn = null;
            _sortDesc = false;
            BuildViewIndex();
            QueueAutoSaveUserLayout();
        }
    }

    public void BeginEdit(int viewRowIndex, ViewGridColumn col)
    {
        if (!EnableCellEditing || !CanEditColumn(col)) return;
        EndEdit(false);
        var row = GetViewRow(viewRowIndex); if (row == null) return;
        var bounds = GetCellBounds(viewRowIndex, col); if (bounds.IsEmpty) return;
        var editor = col.Editor ?? CreateBuiltInEditor(col);
        _activeCellEditor = editor;
        _editor = editor.CreateEditor(this, bounds, col, row, col.GetValue(row));
        _editingCell = (viewRowIndex, col);
        _editor.KeyDown += (_,e) => { if (e.KeyCode == Keys.Enter) EndEdit(true); if (e.KeyCode == Keys.Escape) EndEdit(false); };
        _editor.Leave += (_,__) => EndEdit(true);
    }


    private static ICellEditor CreateBuiltInEditor(ViewGridColumn col)
    {
        var type = col.EditorType;
        if (type == ViewGridCellEditorKind.Auto)
        {
            type = col.Kind switch
            {
                ViewGridColumnKind.CheckBox or ViewGridColumnKind.ToggleSwitch => ViewGridCellEditorKind.CheckBox,
                ViewGridColumnKind.ComboBox => ViewGridCellEditorKind.ComboBox,
                ViewGridColumnKind.Numeric or ViewGridColumnKind.Rating or ViewGridColumnKind.ProgressBar => ViewGridCellEditorKind.Numeric,
                ViewGridColumnKind.Date => ViewGridCellEditorKind.DateTime,
                _ => ViewGridCellEditorKind.TextBox
            };
        }

        return type switch
        {
            ViewGridCellEditorKind.CheckBox => new CheckBoxCellEditor(),
            ViewGridCellEditorKind.ComboBox => new ComboBoxCellEditor(col.ComboBoxItems),
            ViewGridCellEditorKind.Numeric => new NumericCellEditor(),
            ViewGridCellEditorKind.DateTime => new DateTimeCellEditor(),
            _ => new TextBoxCellEditor()
        };
    }

    public void EndEdit(bool commit)
    {
        if (_editor == null || _editingCell == null) return;
        var editor = _editor; var cellEditor = _activeCellEditor; var cell = _editingCell.Value; _editor = null; _activeCellEditor = null; _editingCell = null;
        if (commit)
        {
            var row = GetViewRow(cell.row);
            if (row != null)
            {
                object? value = cellEditor?.GetEditedValue(editor) ?? (editor is TextBox tb ? tb.Text : null);
                SetCellValueWithUndo(cell.row, row, cell.col, value);
            }
        }
        Controls.Remove(editor); editor.Dispose(); Invalidate();
    }

    private void SetCellValueWithUndo(int viewRowIndex, object row, ViewGridColumn col, object? value)
    {
        var old = col.GetValue(row);
        col.PutValue(row, value);
        if (EnableUndoRedo) _undo.Push(new ViewGrid.Undo.ViewGridCellChange { RowObject = row, Column = col, OldValue = old, NewValue = value });
        CellValueChanged?.Invoke(this, new ViewGridCellEditEventArgs(viewRowIndex, row, col, value));
        InvalidateDataCaches(keepVersion: true);
        Invalidate();
    }
    private void ToggleBool(object row, ViewGridColumn col)
    {
        if (col.Kind == ViewGridColumnKind.CheckBox || col.CellCheckBox || IsCompatibilityCheckBoxHostColumn(col))
        {
            var oldState = GetRowCheckState(row, col);
            var newState = oldState == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
            SetRowCheckState(row, col, newState);
            CellValueChanged?.Invoke(this, new ViewGridCellEditEventArgs(_selectedRow, row, col, newState == CheckState.Checked));
            if (oldState != newState) OnObjectCheckStateChanged(row, IndexOfObject(row), newState);
            UpdateCompatibilityHeaderCheckState(col);
            Invalidate();
            return;
        }
        var v = col.GetValue(row);
        bool current = false;
        if (v is bool boolValue) current = boolValue;
        else if (v is CheckState checkState) current = checkState == CheckState.Checked;
        else if (v is int intValue) current = intValue != 0;
        else if (v is string stringValue && bool.TryParse(stringValue, out var parsed)) current = parsed;
        SetCellValueWithUndo(_selectedRow, row, col, !current);
    }
    private void SetRatingFromClick(int x, object row, ViewGridColumn col)
    {
        int left = GetColumnLeft(col) + 6; int rating = Math.Clamp(((x - left) / 18) + 1, 0, col.MaxRating); SetCellValueWithUndo(_selectedRow, row, col, rating);
    }

    private int GetFilterMenuScanLimit()
    {
        // v24.23: 1M+ virtual lists must open the filter menu immediately.
        // The value list is a fast preview; typed text filtering still applies to the full data source.
        if (FastFilterMenuForHugeLists && _provider.Count > 20_000)
            return Math.Max(100, Math.Min(FastFilterMenuInitialScanRows, FastFilterPopupPreviewRows));
        if (_provider.Count > 20_000)
            return Math.Max(MaxFilterDistinctScanRows, MaxVirtualFilterScanRows);
        return MaxFilterDistinctScanRows;
    }

    private int GetFilterSearchScanLimit()
    {
        // v24.75: Popup preview can stay tiny, but typed search must be allowed to reach
        // values that are far outside the first preview page. Otherwise applying typed text
        // may filter correctly while the selectable value list looks empty.
        if (EnableSmartFilterEngine && SmartFilterSearchAllRows && _provider.Count > 20_000)
            return Math.Max(GetFilterMenuScanLimit(), _provider.Count);
        if (EnableSmartFilterEngine && _provider.Count > 20_000)
            return Math.Max(GetFilterMenuScanLimit(), Math.Min(_provider.Count, Math.Max(FastFilterMenuSearchScanRows, SmartFilterMaxScanRows)));
        if (FastFilterMenuForHugeLists && _provider.Count > 20_000)
            return Math.Max(GetFilterMenuScanLimit(), FastFilterMenuSearchScanRows);
        return GetFilterMenuScanLimit();
    }

    private string GetSmartFilterIndexKey(ViewGridColumn col) => col.AspectName + "|" + col.Header + "|" + _dataVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private void BeginBuildSmartFilterIndex(ViewGridColumn col)
    {
        if (!EnableSmartFilterEngine || col == null || _provider.Count <= 20_000) return;
        string key = GetSmartFilterIndexKey(col);
        if (_smartFilterIndexCache.TryGetValue(key, out var existing) && existing.Version == _dataVersion) return;
        _smartFilterIndexBuilds.GetOrAdd(key, _ => Task.Run(() =>
        {
            try
            {
                var values = ComputeDistinctColumnValuesSnapshot(col, Math.Min(_provider.Count, Math.Max(1, SmartFilterMaxScanRows)), null, useTopValueOrder: SmartFilterTopValuesFirst);
                var counts = values.GroupBy(v => v, StringComparer.CurrentCultureIgnoreCase).ToDictionary(g => g.Key, g => g.Count(), StringComparer.CurrentCultureIgnoreCase);
                _smartFilterIndexCache[key] = new SmartFilterIndexSnapshot { Version = _dataVersion, Values = values, Counts = counts };
            }
            catch { }
            finally
            {
                // C# nullable annotations do not change method signatures, and some projects
                // can bind `out _` to a local string named `_`. Keep the out parameter
                // explicitly typed so ConcurrentDictionary<string, Task>.TryRemove always binds
                // to TValue = Task.
                Task? removedBuild;
                _smartFilterIndexBuilds.TryRemove(key, out removedBuild);
            }
        }));
    }

    private bool IsHugeFastFilterPreview() => FastFilterMenuForHugeLists && _provider.Count > 20_000;

    public void ShowFilterMenuForAspect(string aspectName)
    {
        var col = Columns.FirstOrDefault(c => string.Equals(c.AspectName, aspectName, StringComparison.OrdinalIgnoreCase));
        if (col != null) ShowConfiguredFilterMenuForColumn(col, GetDefaultFilterPopupClientAnchor(col));
    }

    public void ShowFilterMenuForAspect(string aspectName, Point clientAnchor)
    {
        var col = Columns.FirstOrDefault(c => string.Equals(c.AspectName, aspectName, StringComparison.OrdinalIgnoreCase));
        if (col != null) ShowConfiguredFilterMenuForColumn(col, NormalizeFilterPopupClientAnchor(col, clientAnchor));
    }

    public void ShowFilterMenuForAspect(string aspectName, Control anchorControl)
    {
        if (anchorControl == null) { ShowFilterMenuForAspect(aspectName); return; }
        var screen = anchorControl.PointToScreen(new Point(0, anchorControl.Height));
        ShowFilterMenuForAspectAtScreen(aspectName, screen);
    }

    public void ShowFilterMenuForAspectAtScreen(string aspectName, Point screenAnchor)
    {
        var col = Columns.FirstOrDefault(c => string.Equals(c.AspectName, aspectName, StringComparison.OrdinalIgnoreCase));
        if (col != null) ShowConfiguredFilterMenuForColumn(col, PointToClient(screenAnchor));
    }

    private Point GetDefaultFilterPopupClientAnchor(ViewGridColumn col)
    {
        if (ShowHeader && (ViewMode == ViewGridMode.Details || ViewMode == ViewGridMode.DenseList || ViewMode == ViewGridMode.GroupedList || ViewMode == ViewGridMode.MasterDetail))
            return new Point(Math.Max(0, GetColumnLeft(col)), HeaderHeight);

        if (FilterPopupAnchorToMouseWhenHeaderUnavailable)
        {
            Point mouse = PointToClient(Control.MousePosition);
            if (ClientRectangle.Contains(mouse))
                return mouse;
        }

        return new Point(Math.Max(4, ClientRectangle.Left + 4), Math.Max(4, ClientRectangle.Top + 4));
    }

    private Point NormalizeFilterPopupClientAnchor(ViewGridColumn col, Point requestedClientAnchor)
    {
        if (ShowHeader && HeaderHeight > 0 && requestedClientAnchor.Y <= HeaderHeight + 2)
            return new Point(Math.Max(0, GetColumnLeft(col)), HeaderHeight);

        if (ClientRectangle.Contains(requestedClientAnchor))
            return requestedClientAnchor;

        return GetDefaultFilterPopupClientAnchor(col);
    }

    public void ShowHeaderContextMenuForColumn(ViewGridColumn col, Point clientLocation)
    {
        ShowBuiltInContextMenu(clientLocation);
    }

    public void ShowHeaderContextMenuForAspect(string aspectName)
    {
        var col = Columns.FirstOrDefault(c => string.Equals(c.AspectName, aspectName, StringComparison.OrdinalIgnoreCase));
        if (col != null) ShowHeaderContextMenuForColumn(col, new Point(Math.Max(0, GetColumnLeft(col)), Math.Max(2, HeaderHeight - 2)));
    }

    private IReadOnlyList<string> GetDistinctColumnValues(ViewGridColumn col, int maxScanRows, string? searchText = null)
    {
        string key = col.AspectName + "|" + col.Header + "|search=" + (searchText ?? string.Empty) + "|" + _filters.GlobalText + "|" + string.Join(",", _filters.Filters.Where(f => !string.Equals(f.AspectName, col.AspectName, StringComparison.OrdinalIgnoreCase)).Select(f => f.AspectName + ":" + f.Mode + ":" + f.Text + ":" + (f.SelectedValues == null ? "*" : string.Join("~", f.SelectedValues.OrderBy(x => x)))));
        if (_distinctValueCache.TryGetValue(key, out var cached) && cached.version == _dataVersion && cached.maxRows == maxScanRows)
            return cached.values;

        // v24.82: If the background Smart Filter index is ready, use it for typed
        // search even when SmartFilterSearchAllRows is true. This makes writing feel
        // as instant as deleting/backspacing, because both paths can use the same
        // already-built value index instead of waiting for a fresh full scan.
        if (EnableSmartFilterEngine && !string.IsNullOrWhiteSpace(searchText))
        {
            string indexKey = GetSmartFilterIndexKey(col);
            if (_smartFilterIndexCache.TryGetValue(indexKey, out var snapshot) && snapshot.Version == _dataVersion)
            {
                string q = searchText.Trim();
                var indexed = snapshot.Values
                    .Where(v => v.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    .OrderBy(v => string.Equals(v, q, StringComparison.CurrentCultureIgnoreCase) ? 0 : v.StartsWith(q, StringComparison.CurrentCultureIgnoreCase) ? 1 : 2)
                    .ThenBy(v => v, StringComparer.CurrentCultureIgnoreCase)
                    .Take(Math.Max(Math.Max(25, SmartFilterPopupValueLimit), MaxEmbeddedFilterVisibleValues))
                    .ToList();
                _distinctValueCache[key] = (_dataVersion, maxScanRows, indexed);
                return indexed;
            }
        }

        var values = ComputeDistinctColumnValuesSnapshot(col, maxScanRows, searchText, useTopValueOrder: EnableSmartFilterEngine && SmartFilterTopValuesFirst && string.IsNullOrWhiteSpace(searchText));
        if (EnableSmartFilterEngine && !string.IsNullOrWhiteSpace(searchText))
            values = values.Take(Math.Max(Math.Max(25, SmartFilterPopupValueLimit), MaxEmbeddedFilterVisibleValues)).ToList();
        _distinctValueCache[key] = (_dataVersion, maxScanRows, values);
        return values;
    }

    private List<string> ComputeDistinctColumnValuesSnapshot(ViewGridColumn col, int maxScanRows, string? searchText = null, bool useTopValueOrder = false)
    {
        var allColumns = Columns.ToArray();
        if (_provider is IQueryRowProvider queryProvider &&
            queryProvider.TryGetDistinctValues(col, _filters, allColumns, Math.Max(1, maxScanRows), searchText, out var providerValues))
        {
            var list = providerValues.ToList();
            list.Sort(CompareFilterValuesNatural);
            return list;
        }

        var values = new List<string>();
        int count = Math.Min(_provider.Count, Math.Max(1, maxScanRows));
        var seen = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        var counts = useTopValueOrder ? new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase) : null;
        string? q = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim();
        int searchValueLimit = q != null ? Math.Max(25, SmartFilterPopupValueLimit) : int.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var row = _provider.GetRow(i);
            if (row == null) continue;
            if (!_filters.PassesExcept(row, allColumns, col.AspectName)) continue;
            string value = Convert.ToString(col.GetValue(row)) ?? string.Empty;
            if (q != null && value.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) < 0) continue;
            if (seen.Add(value))
            {
                values.Add(value);
                // v24.78: typed search must feel instant. For search-specific distinct
                // lists we only need the first visible page of matching values, so stop
                // as soon as enough candidates are found instead of scanning 1M+ rows.
                if (q != null && values.Count >= searchValueLimit) break;
            }
            if (counts != null) counts[value] = counts.TryGetValue(value, out var c) ? c + 1 : 1;
        }
        if (q != null)
        {
            values.Sort((a, b) =>
            {
                int ra = string.Equals(a, q, StringComparison.CurrentCultureIgnoreCase) ? 0 : a.StartsWith(q, StringComparison.CurrentCultureIgnoreCase) ? 1 : 2;
                int rb = string.Equals(b, q, StringComparison.CurrentCultureIgnoreCase) ? 0 : b.StartsWith(q, StringComparison.CurrentCultureIgnoreCase) ? 1 : 2;
                return ra != rb ? ra.CompareTo(rb) : CompareFilterValuesNatural(a, b);
            });
        }
        else if (counts != null)
            values = values.OrderByDescending(v => counts.TryGetValue(v, out var c) ? c : 0).ThenBy(v => v, StringComparer.CurrentCultureIgnoreCase).ToList();
        else
            values.Sort(CompareFilterValuesNatural);
        return values;
    }


    private static int CompareFilterValuesNatural(string? x, string? y)
    {
        x ??= string.Empty;
        y ??= string.Empty;
        if (decimal.TryParse(x, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var dx) &&
            decimal.TryParse(y, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var dy))
            return dx.CompareTo(dy);

        int ix = 0, iy = 0;
        while (ix < x.Length && iy < y.Length)
        {
            char cx = x[ix], cy = y[iy];
            if (char.IsDigit(cx) && char.IsDigit(cy))
            {
                int sx = ix, sy = iy;
                while (ix < x.Length && char.IsDigit(x[ix])) ix++;
                while (iy < y.Length && char.IsDigit(y[iy])) iy++;
                string nx = x.Substring(sx, ix - sx).TrimStart('0');
                string ny = y.Substring(sy, iy - sy).TrimStart('0');
                if (nx.Length == 0) nx = "0";
                if (ny.Length == 0) ny = "0";
                int len = nx.Length.CompareTo(ny.Length);
                if (len != 0) return len;
                int num = string.CompareOrdinal(nx, ny);
                if (num != 0) return num;
                continue;
            }
            int cmp = string.Compare(x, ix, y, iy, 1, StringComparison.CurrentCultureIgnoreCase);
            if (cmp != 0) return cmp;
            ix++; iy++;
        }
        return x.Length.CompareTo(y.Length);
    }

    private int HitRow(int y)
    {
        int top = GetRowsTopOffset();
        if (y < top) return -1;
        int band = (y - top) / RowHeight;
        if (!IsTileView)
        {
            int r = _scrollY + band;
            return r >= 0 && r < ViewCount ? r : -1;
        }
        int perRow = Math.Max(1, GetTilesPerRow());
        int available = Math.Max(1, Width - VBarWidth - 8);
        int tileW = Math.Max(140, available / perRow);
        int tile = Math.Clamp((Math.Max(0, PointToClient(MousePosition).X - 4)) / tileW, 0, perRow - 1);
        int idx = (_scrollY + band) * perRow + tile;
        return idx >= 0 && idx < ViewCount ? idx : -1;
    }
    private ViewGridColumn? HitColumn(int x) { if (IsTileView) return Columns.VisibleColumns.FirstOrDefault(c => c.Editable) ?? Columns.VisibleColumns.FirstOrDefault(); int xx = -_scrollX; foreach (var c in Columns.VisibleColumns) { if (x >= xx && x < xx + c.Width) return c; xx += c.Width; } return null; }
    private ViewGridColumn? HitColumnResizeEdge(int x)
    {
        int xx = -_scrollX;
        foreach (var c in Columns.VisibleColumns)
        {
            int right = xx + c.Width;
            if (c.AllowResize && Math.Abs(x - right) <= 4) return c;
            xx += c.Width;
        }
        return null;
    }
    private int GetColumnLeft(ViewGridColumn col) { int x=-_scrollX; foreach (var c in Columns.VisibleColumns) { if (c == col) return x; x += c.Width; } return 0; }

    private void SetResizingColumnWidth(ViewGridColumn column, int requestedWidth)
    {
        int minWidth = GetColumnMinimumResizeWidth(column);
        column.Width = Math.Max(minWidth, requestedWidth);

        if (!AbsorbColumnResizeOverflowFromFreeSpace || IsTileView)
            return;

        AbsorbHorizontalOverflowFromFillColumns(column);
    }

    private int GetColumnMinimumResizeWidth(ViewGridColumn column)
    {
        return Math.Max(28, column.MinimumWidth > 0 ? column.MinimumWidth : 28);
    }

    private void AbsorbHorizontalOverflowFromFillColumns(ViewGridColumn resizedColumn)
    {
        var visible = Columns.VisibleColumns.ToList();
        if (visible.Count == 0) return;

        int availableWidth = Math.Max(0, ClientSize.Width - VBarWidth - 2);
        int totalWidth = visible.Sum(c => Math.Max(0, c.Width));
        int overflow = totalWidth - availableWidth;
        if (overflow <= 0) return;

        var shrinkCandidates = visible
            .Where(c => !ReferenceEquals(c, resizedColumn) && c.FillFreeSpace && c.AllowResize)
            .OrderByDescending(c => c.Width)
            .ToList();

        foreach (var c in shrinkCandidates)
        {
            if (overflow <= 0) break;

            int min = GetColumnMinimumResizeWidth(c);
            int canShrink = Math.Max(0, c.Width - min);
            if (canShrink <= 0) continue;

            int shrink = Math.Min(canShrink, overflow);
            c.Width -= shrink;
            overflow -= shrink;
        }
    }

    private int HitColumnInsertIndex(int x)
    {
        var visible = Columns.VisibleColumns.ToList();
        int xx = -_scrollX;
        for (int i = 0; i < visible.Count; i++)
        {
            int mid = xx + visible[i].Width / 2;
            if (x < mid) return i;
            xx += visible[i].Width;
        }
        return Math.Max(0, visible.Count - 1);
    }
    private int GetColumnInsertX(int visibleIndex)
    {
        var visible = Columns.VisibleColumns.ToList();
        visibleIndex = Math.Clamp(visibleIndex, 0, Math.Max(0, visible.Count));
        int x = -_scrollX;
        for (int i = 0; i < visibleIndex && i < visible.Count; i++) x += visible[i].Width;
        return x;
    }
    public Rectangle GetCellBounds(int viewRowIndex, ViewGridColumn col)
    {
        int top = GetRowsTopOffset();
        if (IsTileView)
        {
            int perRow = Math.Max(1, GetTilesPerRow());
            int band = viewRowIndex / perRow;
            int tile = viewRowIndex % perRow;
            int available = Math.Max(1, Width - VBarWidth - 8);
            int tileW = Math.Max(140, available / perRow);
            int y = top + (band - _scrollY) * RowHeight + 4;
            if (y < top || y > Height) return Rectangle.Empty;
            return new Rectangle(4 + tile * tileW, y, Math.Max(40, tileW - 8), Math.Max(24, RowHeight - 8));
        }
        int y2 = top + (viewRowIndex - _scrollY) * RowHeight; if (y2 < top || y2 > Height) return Rectangle.Empty;
        return new Rectangle(GetColumnLeft(col), y2, col.Width, RowHeight);
    }
    protected override bool IsInputKey(Keys keyData)
    {
        var key = keyData & Keys.KeyCode;
        if (key is Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End or Keys.Space or Keys.Enter or Keys.Escape)
            return true;
        return base.IsInputKey(keyData);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        if (KeyboardSelectFirstRowOnFocus && _selectedRow < 0 && ViewCount > 0)
            SelectRow(Math.Max(0, Math.Min(_scrollY, ViewCount - 1)));
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (!EnableIncrementalSearch || _editor != null || char.IsControl(e.KeyChar)) return;
        var now = DateTime.UtcNow;
        if ((now - _lastIncrementalSearchUtc).TotalMilliseconds > Math.Max(100, IncrementalSearchResetMs))
            _incrementalSearchBuffer = string.Empty;
        _lastIncrementalSearchUtc = now;
        _incrementalSearchBuffer += e.KeyChar;
        if (FindNext(_incrementalSearchBuffer, wrap: true)) e.Handled = true;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_editor != null) return base.ProcessCmdKey(ref msg, keyData);
        if (HandleViewGridKeyboardAccessibilityShortcut(keyData)) return true;
        if (EnableCommandPalette && keyData == (Keys.Control | Keys.K)) { ShowCommandPalette(); return true; }
        if (keyData == (Keys.Control | Keys.F))
        {
            if (EnableModernSearchPanel) { ShowModernSearchPanel(); return true; }
            if (EnableKeyboardFilterShortcut) { ShowCommandPalette("filter"); return true; }
        }
        if (EnableClipboard && keyData == (Keys.Control | Keys.C)) { CopySelectionToClipboard(); return true; }
        if (EnableClipboard && keyData == (Keys.Control | Keys.Shift | Keys.C)) { CopySelectedCellToClipboard(); return true; }
        if (EnableClipboard && keyData == (Keys.Control | Keys.Shift | Keys.J)) { CopySelectionAsJsonToClipboard(); return true; }
        if (EnableClipboard && keyData == (Keys.Control | Keys.V)) { PasteTextToSelectedCell(Clipboard.GetText()); return true; }
        if (EnableUndoRedo && keyData == (Keys.Control | Keys.Z)) { Undo(); return true; }
        if (EnableUndoRedo && keyData == (Keys.Control | Keys.Y)) { Redo(); return true; }
        if (keyData == (Keys.Control | Keys.A)) { SelectAllRows(); return true; }
        if (keyData == (Keys.Shift | Keys.Up)) { ExtendSelectionTo(Math.Max(0, _selectedRow - 1)); return true; }
        if (keyData == (Keys.Shift | Keys.Down)) { ExtendSelectionTo(Math.Min(ViewCount - 1, _selectedRow < 0 ? 0 : _selectedRow + 1)); return true; }
        if (keyData == (Keys.Shift | Keys.PageUp)) { ExtendSelectionTo(Math.Max(0, (_selectedRow < 0 ? _scrollY : _selectedRow) - VisibleRowCapacity())); return true; }
        if (keyData == (Keys.Shift | Keys.PageDown)) { ExtendSelectionTo(Math.Min(ViewCount - 1, (_selectedRow < 0 ? _scrollY : _selectedRow) + VisibleRowCapacity())); return true; }
        if (keyData == Keys.Up) { SelectRow(Math.Max(0, _selectedRow - 1)); return true; }
        if (keyData == Keys.Down) { SelectRow(Math.Min(ViewCount - 1, _selectedRow < 0 ? 0 : _selectedRow + 1)); return true; }
        if (keyData == Keys.PageUp) { SelectRow(Math.Max(0, (_selectedRow < 0 ? _scrollY : _selectedRow) - VisibleRowCapacity())); return true; }
        if (keyData == Keys.PageDown) { SelectRow(Math.Min(ViewCount - 1, (_selectedRow < 0 ? _scrollY : _selectedRow) + VisibleRowCapacity())); return true; }
        if (keyData == Keys.Home || keyData == (Keys.Control | Keys.Home)) { SelectRow(0); return true; }
        if (keyData == Keys.End || keyData == (Keys.Control | Keys.End)) { SelectRow(ViewCount - 1); return true; }
        if (keyData == Keys.Left) { ScrollHorizontal(_scrollX - 32); return true; }
        if (keyData == Keys.Right) { ScrollHorizontal(_scrollX + 32); return true; }
        if (keyData == Keys.Space)
        {
            if (TryToggleSelectedCheckBoxesFromKeyboard()) return true;
            // Space yalnızca checkbox için doğal toggle davranışıdır. Checkbox yoksa, geriye dönük
            // uyumluluk için button hücresini çalıştırabilir; Enter ise birincil aksiyon tuşudur.
            if (TryActivateSelectedButtonCell()) return true;
            return true;
        }
        if (EnableCellEditing && IsCellEditActivationKey(keyData)) return BeginEditSelectedCell();
        if (keyData == Keys.Enter)
        {
            if (TryActivateSelectedButtonCell()) return true;
            if (EnableRowDetails) { ToggleRowDetails(_selectedRow); return true; }
            return BeginEditSelectedCell();
        }
        if (keyData == Keys.Escape) { CloseActiveMenusSafe(); CloseRowDetails(); return true; }
        if (KeyboardColumnContextMenuKeyOpensMenu && (keyData == (Keys.Alt | Keys.Down) || keyData == (Keys.Control | Keys.Shift | Keys.F10))) { ShowKeyboardColumnContextMenu(); return true; }
        if (KeyboardContextMenuKeyOpensMenu && (keyData == Keys.Apps || keyData == (Keys.Shift | Keys.F10))) { ShowKeyboardContextMenu(); return true; }
        if (keyData == Keys.Delete) { CellClick?.Invoke(this, new ViewGridCellClickEventArgs(_selectedRow, SelectedObject ?? this, Columns.VisibleColumns.FirstOrDefault() ?? new ViewGridColumn("", "", 0))); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private int VisibleRowCapacity()
    {
        int top = GetRowsTopOffset();
        int footer = ShowSummaryFooter ? FooterHeight : 0;
        int hbarHeight = _hbar.Visible ? _hbar.Height : 0;
        return Math.Max(1, (Height - top - footer - hbarHeight) / RowHeight);
    }

    private void SelectRow(int viewRowIndex)
    {
        if (ViewCount <= 0) viewRowIndex = -1;
        else viewRowIndex = Math.Clamp(viewRowIndex, 0, ViewCount - 1);
        if (_selectedRow == viewRowIndex && _selectedRows.Count == 1 && _selectedRows.Contains(viewRowIndex)) return;
        _selectedRow = viewRowIndex;
        _selectedRows.Clear();
        if (viewRowIndex >= 0) _selectedRows.Add(viewRowIndex);
        StartSelectionAnimation(viewRowIndex);
        _selectionAnchorRow = viewRowIndex;
        EnsureRowVisible(viewRowIndex);
        EnsureDetailsRowHeightStableAfterSelection();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void ExtendSelectionTo(int viewRowIndex)
    {
        if (ViewCount <= 0) return;
        viewRowIndex = Math.Clamp(viewRowIndex, 0, ViewCount - 1);
        if (!MultiSelect) { SelectRow(viewRowIndex); return; }
        if (_selectionAnchorRow < 0) _selectionAnchorRow = _selectedRow >= 0 ? _selectedRow : viewRowIndex;
        _selectedRow = viewRowIndex;
        _selectedRows.Clear();
        int a = Math.Min(_selectionAnchorRow, viewRowIndex);
        int b = Math.Max(_selectionAnchorRow, viewRowIndex);
        for (int i = a; i <= b; i++) _selectedRows.Add(i);
        EnsureRowVisible(viewRowIndex);
        EnsureDetailsRowHeightStableAfterSelection();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void ShowKeyboardColumnContextMenu()
    {
        if (!ShowHeader || !Columns.VisibleColumns.Any())
        {
            ShowKeyboardContextMenu();
            return;
        }

        var col = _activeColumn ?? _sortColumn ?? Columns.VisibleColumns.FirstOrDefault();
        if (col == null)
        {
            ShowKeyboardContextMenu();
            return;
        }

        int left = Math.Max(0, GetColumnLeft(col));
        int x = Math.Min(Math.Max(4, left + Math.Min(Math.Max(24, col.Width / 2), Math.Max(8, col.Width - 8))), Math.Max(4, Width - 8));
        int y = Math.Max(2, Math.Min(HeaderHeight - 2, HeaderHeight / 2));
        ShowBuiltInContextMenu(new Point(x, y));
    }

    private void ShowKeyboardContextMenu()
    {
        if (_selectedRow >= 0)
        {
            int top = ShowHeader ? HeaderHeight : 0;
            int y = top + Math.Max(0, _selectedRow - _scrollY) * RowHeight + Math.Max(2, RowHeight / 2);
            ShowBuiltInContextMenu(new Point(Math.Max(8, Width / 3), Math.Min(Math.Max(HeaderHeight + 4, y), Math.Max(HeaderHeight + 4, Height - 8))));
            return;
        }
        ShowBuiltInContextMenu(new Point(8, ShowHeader ? Math.Max(2, HeaderHeight - 2) : 8));
    }

    private void EnsureDetailsRowHeightStableAfterSelection()
    {
        if (!LockDetailsRowHeightOnSelection || IsTileView || AutoRowHeightForMultilineCells) return;
        int wanted = Math.Max(20, _detailsRowHeight);
        if (_rowHeight != wanted)
        {
            _rowHeight = wanted;
            UpdateScrollbars();
        }
    }

    private void UpdateSelectionFromMouse(int rowIndex, Keys modifierKeys)
    {
        if (!MultiSelect) { SelectRow(rowIndex); return; }
        if (modifierKeys.HasFlag(Keys.Shift) && _selectionAnchorRow >= 0)
        {
            _selectedRows.Clear();
            int a = Math.Min(_selectionAnchorRow, rowIndex);
            int b = Math.Max(_selectionAnchorRow, rowIndex);
            for (int i = a; i <= b; i++) _selectedRows.Add(i);
        }
        else if (modifierKeys.HasFlag(Keys.Control))
        {
            if (_selectedRows.Contains(rowIndex)) _selectedRows.Remove(rowIndex); else { _selectedRows.Add(rowIndex); StartSelectionAnimation(rowIndex); }
            _selectionAnchorRow = rowIndex;
        }
        else
        {
            _selectedRows.Clear();
            _selectedRows.Add(rowIndex);
            StartSelectionAnimation(rowIndex);
            _selectionAnchorRow = rowIndex;
        }
        _selectedRow = rowIndex;
        EnsureRowVisible(rowIndex);
        EnsureDetailsRowHeightStableAfterSelection();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void SelectAllRows()
    {
        _selectedRows.Clear();
        int max = Math.Min(ViewCount, 250_000);
        for (int i = 0; i < max; i++) _selectedRows.Add(i);
        _selectedRow = ViewCount > 0 ? 0 : -1;
        _selectionAnchorRow = _selectedRow;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void EnsureRowVisible(int viewRowIndex)
    {
        if (viewRowIndex < 0) return;
        int visible = VisibleRowCapacity();
        int visualIndex = IsTileView ? viewRowIndex / Math.Max(1, GetTilesPerRow()) : viewRowIndex;
        if (visualIndex < _scrollY) ScrollVertical(visualIndex);
        else if (visualIndex >= _scrollY + visible) ScrollVertical(visualIndex - visible + 1);
    }

    private bool TryActivateSelectedButtonCell()
    {
        if (!KeyboardActivatesButtonCells || _selectedRow < 0) return false;
        var row = GetViewRow(_selectedRow);
        var col = _activeColumn != null && _activeColumn.Kind == ViewGridColumnKind.Button
            ? _activeColumn
            : Columns.VisibleColumns.FirstOrDefault(c => c.Kind == ViewGridColumnKind.Button);
        if (row == null || col == null) return false;
        ButtonClick?.Invoke(this, new ViewGridCellClickEventArgs(_selectedRow, row, col));
        return true;
    }

    private void ToggleFirstCheckBoxColumn()
    {
        if (_selectedRow < 0) return;
        var row = GetViewRow(_selectedRow);
        var col = GetActiveCheckBoxColumn();
        if (row != null && col != null) ToggleBool(row, col);
    }

    private bool TryToggleSelectedCheckBoxesFromKeyboard()
    {
        if (!KeyboardSpaceTogglesCheckBoxes || _selectedRow < 0) return false;

        var col = GetActiveCheckBoxColumn();
        if (col == null) return false;

        var targetRows = new List<object>();

        if (KeyboardSpaceTogglesSelectedRows && MultiSelect && _selectedRows.Count > 1)
        {
            foreach (int viewIndex in _selectedRows.OrderBy(i => i))
            {
                var selectedRow = GetViewRow(viewIndex);
                if (selectedRow != null) targetRows.Add(selectedRow);
            }
        }
        else
        {
            var row = GetViewRow(_selectedRow);
            if (row != null) targetRows.Add(row);
        }

        if (targetRows.Count == 0) return false;

        // Windows/OLV tarzı: Space mevcut seçimi tersine çevirir. Çoklu seçimde karışık
        // durumda hepsini Checked yapar; hepsi Checked ise Unchecked yapar.
        bool allChecked = targetRows.All(row => GetRowCheckState(row, col) == CheckState.Checked);
        var nextState = allChecked ? CheckState.Unchecked : CheckState.Checked;

        foreach (var row in targetRows)
        {
            var oldState = GetRowCheckState(row, col);
            if (oldState == nextState) continue;

            SetRowCheckState(row, col, nextState);
            CellValueChanged?.Invoke(this, new ViewGridCellEditEventArgs(IndexOfObject(row), row, col, nextState == CheckState.Checked));
            OnObjectCheckStateChanged(row, IndexOfObject(row), nextState);
        }

        UpdateCompatibilityHeaderCheckState(col);
        Invalidate();
        return true;
    }

    private bool CanEditColumn(ViewGridColumn? col)
    {
        if (col == null) return false;
        return AllowEditAllCells || col.Editable;
    }

    private bool IsCellEditActivationKey(Keys keyData)
    {
        if (CellEditActivationKey == Keys.None) return false;
        return keyData == CellEditActivationKey;
    }

    private bool BeginEditSelectedCell()
    {
        if (_selectedRow < 0 || !EnableCellEditing) return false;
        var col = CanEditColumn(_activeColumn)
                  ? _activeColumn
                  : Columns.VisibleColumns.FirstOrDefault(c => c.Editable)
                    ?? Columns.VisibleColumns.FirstOrDefault(c => CanEditColumn(c));
        if (!CanEditColumn(col)) return false;
        BeginEdit(_selectedRow, col!);
        return true;
    }


    private ToolStripMenuItem CreateEmbeddedFilterMenuItem(ViewGridColumn col)
    {
        var root = new ToolStripMenuItem(ViewGridText.Filtering);
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(root.DropDown, _theme);
        // v24.49: Filtre artık ana menünün stabil alt menüsü olarak çalışır.
        // Dışarı tıklayınca WinForms'un doğal AutoClose davranışı devrede kalır;
        // böylece menü ekranda takılı kalmaz, ana menü de filtre açılırken zıplamaz.
        root.DropDown.AutoClose = true;

        root.DropDownItems.Add(ViewGridText.SortAscending, null, (_,__) => { SortBy(col, false); CloseActiveMenusSafe(); }).Enabled = col.Sortable;
        root.DropDownItems.Add(ViewGridText.SortDescending, null, (_,__) => { SortBy(col, true); CloseActiveMenusSafe(); }).Enabled = col.Sortable;
        root.DropDownItems.Add(ViewGridText.Unsort, null, (_,__) => { ClearSort(col); CloseActiveMenusSafe(); }).Enabled = _sortColumn == col;
        root.DropDownItems.Add(new ToolStripSeparator());

        var current = _filters.Get(col.AspectName);
        Color filterPanelBack = GetFilterPopupPanelBack();
        Color filterControlBack = GetFilterPopupControlBack();
        Color filterPanelFore = GetFilterPopupPanelFore();
        Color filterControlFore = GetFilterPopupControlFore();
        Color filterMutedFore = GetFilterPopupMutedFore(filterPanelBack);
        int embeddedScanLimit = GetFilterMenuScanLimit();
        List<string> values = GetDistinctColumnValues(col, embeddedScanLimit).ToList();
        int maxPopupItems = Math.Max(200, MaxEmbeddedFilterVisibleValues);
        bool embeddedUpdating = false;
        bool fullValuesLoaded = embeddedScanLimit >= _provider.Count;
        int popupGeneration = ++_filterPopupGeneration;

        var panel = new Panel { Width = 288, Height = 414, Padding = new Padding(8), BackColor = filterPanelBack };
        var caption = new Label { Text = ViewGridText.ColumnFilterTitle(col.Header), Left = 8, Top = 6, Width = 272, Height = 20, ForeColor = filterPanelFore, BackColor = filterPanelBack, Font = new Font(Font, FontStyle.Bold) };
        var search = new TextBox { PlaceholderText = ViewGridText.SearchPlaceholder, Left = 8, Top = 30, Width = 272, BackColor = filterControlBack, ForeColor = filterControlFore, BorderStyle = BorderStyle.FixedSingle };
        var useTextFilter = new CheckBox { Text = ViewGridText.ApplyTypedTextAsFilter, Left = 8, Top = 58, Width = 272, Height = 22, ForeColor = filterControlFore, BackColor = filterPanelBack, FlatStyle = FlatStyle.Flat };
        if (TypedFilterSearchesAllRows && FastFilterMenuForHugeLists && _provider.Count > 20_000) useTextFilter.Checked = true;
        var selectAll = new CheckBox { Text = ViewGridText.SelectAll, Left = 8, Top = 84, Width = 272, Height = 22, ForeColor = filterControlFore, BackColor = filterPanelBack, FlatStyle = FlatStyle.Flat, ThreeState = true, AutoCheck = false };
        var list = new CheckedListBox { Left = 8, Top = 110, Width = 272, Height = 190, CheckOnClick = true, IntegralHeight = false, BackColor = filterControlBack, ForeColor = filterControlFore, BorderStyle = BorderStyle.FixedSingle };
        var previewInfo = new Label { Text = IsHugeFastFilterPreview() ? ViewGridText.FastPreviewInfo : string.Empty, Left = 8, Top = 304, Width = 272, Height = 16, ForeColor = filterMutedFore, BackColor = filterPanelBack };
        var limitedInfo = new Panel { Left = 8, Top = 322, Width = 272, Height = 42, BackColor = ControlPaint.Light(_theme.AccentColor, 0.78f), Visible = IsHugeFastFilterPreview() };
        var limitedIcon = new Label { Text = "ⓘ", Left = 6, Top = 10, Width = 20, Height = 20, BackColor = limitedInfo.BackColor, ForeColor = Color.Black };
        var limitedLabel = new Label { Text = ViewGridText.FastPreviewLimitedInfo, Left = 28, Top = 4, Width = 212, Height = 34, BackColor = limitedInfo.BackColor, ForeColor = Color.Black };
        var limitedClose = new Label { Text = "×", Left = 246, Top = 8, Width = 20, Height = 22, BackColor = limitedInfo.BackColor, ForeColor = Color.Black, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        limitedClose.Click += (_,__) => limitedInfo.Visible = false;
        limitedInfo.Controls.AddRange(new Control[] { limitedIcon, limitedLabel, limitedClose });
        var clear = new Button { Text = ViewGridText.Clear, Left = 112, Top = 374, Width = 78, Height = 28, BackColor = filterControlBack, ForeColor = filterControlFore, FlatStyle = FlatStyle.Flat };
        var apply = new Button { Text = ViewGridText.Apply, Left = 202, Top = 374, Width = 78, Height = 28, BackColor = _theme.AccentColor, ForeColor = GetReadableTextColor(_theme.AccentColor), FlatStyle = FlatStyle.Flat };
        apply.FlatAppearance.BorderColor = _theme.AccentColor;
        clear.FlatAppearance.BorderColor = _theme.BorderColor;
        panel.Controls.Add(caption); panel.Controls.Add(search); panel.Controls.Add(useTextFilter); panel.Controls.Add(selectAll); panel.Controls.Add(list); panel.Controls.Add(previewInfo); panel.Controls.Add(limitedInfo); panel.Controls.Add(clear); panel.Controls.Add(apply);
        ApplyFilterPopupTheme(panel);
        apply.BackColor = _theme.AccentColor;
        apply.ForeColor = GetReadableTextColor(_theme.AccentColor);
        apply.FlatAppearance.BorderColor = _theme.AccentColor;
        clear.FlatAppearance.BorderColor = _theme.BorderColor == Color.Empty ? ControlPaint.Dark(filterControlBack) : _theme.BorderColor;

        bool IsChecked(string value) => current?.Mode == ViewGridFilterMode.ValueList && current.SelectedValues != null ? current.SelectedValues.Contains(value) : true;
        void Refill()
        {
            string q = search.Text.Trim();
            list.BeginUpdate();
            try
            {
                list.Items.Clear();
                var exact = q.Length == 0 ? null : values.FirstOrDefault(v => string.Equals(v, q, StringComparison.CurrentCultureIgnoreCase));
                var source = (exact != null
                    ? new[] { exact }
                    : values.Where(v => q.Length == 0 || v.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0)).ToList();

                // v24.77: while the async full-search is still running, show the typed
                // value immediately. This removes the empty-list gap when typing values
                // that are not part of the initial fast preview.
                if (q.Length > 0 && !source.Any(v => string.Equals(v, q, StringComparison.CurrentCultureIgnoreCase)))
                    source.Insert(0, q);

                int added = 0;
                foreach (string v in source)
                {
                    list.Items.Add(v, IsChecked(v));
                    if (++added >= maxPopupItems) break;
                }
            }
            finally { list.EndUpdate(); }
            UpdateEmbeddedSelectAllState();
            if (previewInfo != null)
                previewInfo.Text = fullValuesLoaded ? string.Format("{0:N0}", values.Count) : ViewGridText.FastPreviewInfo;
            if (limitedInfo != null)
                limitedInfo.Visible = !fullValuesLoaded && values.Count >= embeddedScanLimit && string.IsNullOrWhiteSpace(search.Text);
        }

        int searchGeneration = 0;
        async void LoadValuesForCurrentSearchAsync()
        {
            string q = search.Text.Trim();
            bool hasSearch = !string.IsNullOrWhiteSpace(q);
            if (!hasSearch && (!AsyncLoadFullFilterValues || fullValuesLoaded || _provider.Count <= embeddedScanLimit)) return;

            // v24.82: Refill() already shows the typed candidate immediately.
            // Keep full distinct matching on the background thread to avoid UI freezes.

            int generation = ++searchGeneration;

            int scanRows = hasSearch
                ? GetFilterSearchScanLimit()
                : Math.Min(_provider.Count, Math.Max(embeddedScanLimit, MaxAsyncFilterDistinctScanRows));
            try
            {
                string requestedSearch = q;
                // v24.82 FULL STABLE: no cancellation exception while typing. Older
                // searches may finish later, but generation/current-search checks decide
                // whether their values are still useful.
                var loadedValues = await Task.Run(() =>
                {
                    try
                    {
                        return GetDistinctColumnValues(col, scanRows, hasSearch ? requestedSearch : null).ToList();
                    }
                    catch (OperationCanceledException)
                    {
                        return new List<string>();
                    }
                    catch
                    {
                        return new List<string>();
                    }
                });
                if (IsDisposed || popupGeneration != _filterPopupGeneration) return;

                string currentSearch = search.Text.Trim();
                bool isCurrentSearch = generation == searchGeneration;
                bool canReuseBroaderResult =
                    requestedSearch.Length > 0 &&
                    currentSearch.Length >= requestedSearch.Length &&
                    currentSearch.StartsWith(requestedSearch, StringComparison.CurrentCultureIgnoreCase);

                if (!isCurrentSearch && !canReuseBroaderResult) return;

                values = loadedValues;
                fullValuesLoaded = !hasSearch && scanRows >= _provider.Count && isCurrentSearch;
                Refill();
            }
            catch (OperationCanceledException) { }
            catch { }
        }
        // v24.61: Keep ToolStrip AutoClose enabled. NonClosingToolStripControlHost prevents inner-control clicks from closing it; outside clicks now close reliably.
        search.MouseDown += (_,__) => { if (!search.Focused) search.Focus(); };
        search.Click += (_,__) => { if (!search.Focused) search.Focus(); };
        var popupSearchDebounce = new System.Windows.Forms.Timer { Interval = 90 };
        popupSearchDebounce.Tick += (_,__) => { popupSearchDebounce.Stop(); LoadValuesForCurrentSearchAsync(); };
        search.TextChanged += (_,__) =>
        {
            Refill();
            if (!fullValuesLoaded && !string.IsNullOrWhiteSpace(search.Text) && previewInfo != null)
                previewInfo.Text = ViewGridText.SearchingPreviewResults;
            popupSearchDebounce.Stop();
            popupSearchDebounce.Start();
        };
        void SetEmbeddedVisibleItemsChecked(bool isChecked)
        {
            if (embeddedUpdating) return;
            embeddedUpdating = true;
            try
            {
                for (int i = 0; i < list.Items.Count; i++)
                    list.SetItemChecked(i, isChecked);
            }
            finally
            {
                embeddedUpdating = false;
            }
            UpdateEmbeddedSelectAllState();
        }
        void ToggleEmbeddedSelectAllFromUser()
        {
            // Only a real Checked state means "all selected". Indeterminate means partial selection
            // and the next click should select all, not clear the user's partial filter choice.
            SetEmbeddedVisibleItemsChecked(selectAll.CheckState != CheckState.Checked);
        }
        void QueueEmbeddedSelectAllStateRefresh()
        {
            if (embeddedUpdating || IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke((Action)UpdateEmbeddedSelectAllState);
            }
            catch (InvalidOperationException)
            {
                UpdateEmbeddedSelectAllState();
            }
        }
        void UpdateEmbeddedSelectAllState()
        {
            if (embeddedUpdating) return;
            int count = list.Items.Count;
            int checkedCount = 0;
            for (int i = 0; i < count; i++)
                if (list.GetItemCheckState(i) == CheckState.Checked) checkedCount++;
            CheckState newState;
            if (count == 0 || checkedCount == 0)
                newState = CheckState.Unchecked;
            else if (checkedCount == count)
                newState = CheckState.Checked;
            else
                newState = CheckState.Indeterminate;
            if (selectAll.CheckState == newState) return;
            embeddedUpdating = true;
            try
            {
                selectAll.CheckState = newState;
                selectAll.Checked = newState == CheckState.Checked;
            }
            finally { embeddedUpdating = false; }
        }
        selectAll.Click += (_,__) => ToggleEmbeddedSelectAllFromUser();
        selectAll.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Space) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            ToggleEmbeddedSelectAllFromUser();
        };
        list.ItemCheck += (_,__) => QueueEmbeddedSelectAllStateRefresh();
        list.MouseUp += (_,__) => QueueEmbeddedSelectAllStateRefresh();
        list.KeyUp += (_,__) => QueueEmbeddedSelectAllStateRefresh();
        clear.Click += (_,__) =>
        {
            _filters.Clear(col.AspectName);
            BuildViewIndex();
            CloseActiveMenusSafe();
        };
        apply.Click += (_,__) =>
        {
            string q = search.Text.Trim();
            var exact = q.Length == 0 ? null : values.FirstOrDefault(v => string.Equals(v, q, StringComparison.CurrentCultureIgnoreCase));

            if (useTextFilter.Checked && !string.IsNullOrWhiteSpace(q))
            {
                SetColumnFilter(new ViewGridColumnFilter { AspectName = col.AspectName, Mode = ViewGridFilterMode.Contains, Text = q, Enabled = true });
                CloseActiveMenusSafe();
                return;
            }

            var selected = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            if (exact != null)
            {
                // Exact typed value always wins. This prevents prefix/contains leakage such as "AOI kayıt 4" -> "AOI kayıt 40".
                selected.Add(exact);
            }
            else
            {
                for (int i = 0; i < list.Items.Count; i++)
                    if (list.GetItemChecked(i)) selected.Add(Convert.ToString(list.Items[i]) ?? string.Empty);
            }

            bool allSelected = string.IsNullOrWhiteSpace(q) && selectAll.Checked;
            if (allSelected)
            {
                _filters.Clear(col.AspectName);
                BuildViewIndex();
            }
            else
            {
                SetColumnFilter(new ViewGridColumnFilter { AspectName = col.AspectName, Mode = ViewGridFilterMode.ValueList, SelectedValues = selected, Enabled = true });
            }
            CloseActiveMenusSafe();
        };
        void EmbeddedFilterShortcut(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                apply.PerformClick();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                CloseActiveMenusSafe();
            }
        }
        search.KeyDown += EmbeddedFilterShortcut;
        list.KeyDown += EmbeddedFilterShortcut;
        useTextFilter.KeyDown += EmbeddedFilterShortcut;
        selectAll.KeyDown += EmbeddedFilterShortcut;
        Refill();
        LoadValuesForCurrentSearchAsync();
        root.DropDownItems.Add(new NonClosingToolStripControlHost(panel) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = panel.Size });
        root.DropDown.Closed += (_, __) => { try { _filterPopupGeneration++; popupSearchDebounce.Dispose(); } catch { } };
        root.DropDownItems.Add(new ToolStripSeparator());
        root.DropDownItems.Add(ViewGridText.ClearAllFilters, null, (_,__) => { ClearFilters(); CloseActiveMenusSafe(); });
        root.DropDownItems.Add(ViewGridText.SeparateFilterWindow, null, (_,__) => { ShowFilterMenuForColumn(col, new Point(GetColumnLeft(col), HeaderHeight)); CloseActiveMenusSafe(); });
        return root;
    }

    private static bool PointerInsideOpenDropDown(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            if (item is ToolStripMenuItem menuItem)
            {
                if (menuItem.DropDown.Visible && menuItem.DropDown.Bounds.Contains(Control.MousePosition))
                    return true;
                if (PointerInsideOpenDropDown(menuItem.DropDownItems))
                    return true;
            }
        }
        return false;
    }


    private static bool HostedControlContainsFocus(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            if (item is ToolStripControlHost host && host.Control != null && host.Control.ContainsFocus)
                return true;

            if (item is ToolStripMenuItem menuItem)
            {
                if (HostedControlContainsFocus(menuItem.DropDownItems))
                    return true;
            }
        }
        return false;
    }

    private Color GetReadableTextColor(Color backColor)
    {
        return global::ViewGrid.Theming.ViewGridThemeAccessibility.EnsureReadableTextColor(_theme.ForeColor, backColor, 4.5d);
    }

    private Color GetFilterPopupPanelBack() => global::ViewGrid.Theming.ViewGridDialogThemeApplier.NormalizePanelBack(_theme);
    private Color GetFilterPopupControlBack() => global::ViewGrid.Theming.ViewGridDialogThemeApplier.NormalizeControlBack(_theme);
    private Color GetFilterPopupPanelFore() => global::ViewGrid.Theming.ViewGridDialogThemeApplier.NormalizePanelFore(_theme);
    private Color GetFilterPopupControlFore() => global::ViewGrid.Theming.ViewGridDialogThemeApplier.NormalizeControlFore(_theme);
    private Color GetFilterPopupMutedFore(Color back) => global::ViewGrid.Theming.ViewGridDialogThemeApplier.EnsureReadableTextColor(_theme.MutedForeColor, back);

    private void ApplyFilterPopupTheme(Control root)
    {
        if (root == null) return;

        var panelBack = GetFilterPopupPanelBack();
        var controlBack = GetFilterPopupControlBack();
        var panelFore = GetFilterPopupPanelFore();
        var controlFore = GetFilterPopupControlFore();

        foreach (Control control in EnumerateFilterPopupControls(root))
        {
            if (control is TextBoxBase || control is ListBox || control is CheckedListBox || control is ComboBox)
            {
                control.BackColor = controlBack;
                control.ForeColor = controlFore;
                continue;
            }

            if (control is Button button)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.UseVisualStyleBackColor = false;
                button.BackColor = controlBack;
                button.ForeColor = controlFore;
                button.FlatAppearance.BorderColor = _theme.BorderColor == Color.Empty ? ControlPaint.Dark(controlBack) : _theme.BorderColor;
                continue;
            }

            if (control is CheckBox checkBox)
            {
                checkBox.FlatStyle = FlatStyle.Flat;
                checkBox.UseVisualStyleBackColor = false;
                checkBox.BackColor = panelBack;
                checkBox.ForeColor = panelFore;
                continue;
            }

            if (control is Label label)
            {
                var back = label.BackColor == Color.Empty || label.BackColor == Color.Transparent ? panelBack : label.BackColor;
                label.ForeColor = global::ViewGrid.Theming.ViewGridDialogThemeApplier.EnsureReadableTextColor(label.ForeColor == Color.Empty ? panelFore : label.ForeColor, back);
                continue;
            }

            if (control is Panel)
            {
                if (control.BackColor == Color.Empty || control.BackColor == SystemColors.Control)
                    control.BackColor = panelBack;
                control.ForeColor = global::ViewGrid.Theming.ViewGridDialogThemeApplier.EnsureReadableTextColor(control.ForeColor == Color.Empty ? panelFore : control.ForeColor, control.BackColor);
                continue;
            }

            control.BackColor = panelBack;
            control.ForeColor = panelFore;
        }
    }

    private static IEnumerable<Control> EnumerateFilterPopupControls(Control root)
    {
        yield return root;
        foreach (Control child in root.Controls)
        {
            foreach (var nested in EnumerateFilterPopupControls(child))
                yield return nested;
        }
    }

    private void CloseActiveMenusSafe()
    {
        RemoveMenuDismissMessageFilter();
        if (_activeFilterPopupForm != null && !_activeFilterPopupForm.IsDisposed)
        {
            try { _activeFilterPopupForm.Close(); } catch { }
        }
        var menu = _activeHeaderMenu;
        if (menu == null || menu.IsDisposed) return;
        try
        {
            _allowActiveMenuClose = true;
            if (!menu.IsDisposed) menu.Close(ToolStripDropDownCloseReason.CloseCalled);
            if (!IsDisposed && IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    _allowActiveMenuClose = true;
                    if (!menu.IsDisposed) menu.Close(ToolStripDropDownCloseReason.CloseCalled);
                }));
            }
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    private ToolStripMenuItem CreateStableFilterLauncherMenuItem(ContextMenuStrip ownerMenu, ViewGridColumn col)
    {
        var launcher = new ToolStripMenuItem(ViewGridText.Filtering);
        bool opening = false;

        void OpenStablePopup()
        {
            if (opening) return;
            opening = true;
            try
            {
                var screenPoint = ownerMenu.PointToScreen(new Point(ownerMenu.Width - 1, Math.Max(0, launcher.Bounds.Top - 1)));

                // v24.46: Filtre için artık ana sağ tık menüsü açık tutulmuyor.
                // Eski yaklaşımda ContextMenuStrip + borderless Form odak zinciri çakışıyor ve dış tıklamada
                // ana menü ekranda kalabiliyordu. Filtre popup'ı açmadan önce owner menüyü kesin kapatıyoruz.
                _allowActiveMenuClose = true;
                RemoveMenuDismissMessageFilter();
                try { if (ownerMenu != null && !ownerMenu.IsDisposed) ownerMenu.Close(ToolStripDropDownCloseReason.CloseCalled); } catch { }

                BeginInvoke(new Action(() =>
                {
                    opening = false;
                    ShowFloatingEmbeddedFilterForm(null, col, screenPoint);
                }));
            }
            catch
            {
                opening = false;
            }
        }

        launcher.MouseEnter += (_, __) => OpenStablePopup();
        launcher.Click += (_, __) => OpenStablePopup();
        return launcher;
    }


    private string GetFilterActionText(string icon, string text)
        => FilterPopupShowActionIcons && !string.IsNullOrWhiteSpace(icon) ? icon + "  " + text : text;

    private Control CreateFloatingFilterActionLabel(string icon, string text, int left, int top, int width, bool enabled, bool active, Action clickAction)
    {
        Color panelBack = GetFilterPopupPanelBack();
        Color panelFore = GetFilterPopupPanelFore();
        Color mutedFore = GetFilterPopupMutedFore(panelBack);
        Color activeBack = ViewGridDialogThemeApplier.Blend(panelBack, _theme.AccentColor, _theme.IsDark ? 0.38 : 0.16);
        Color hoverBack = ViewGridDialogThemeApplier.Blend(panelBack, _theme.AccentColor, _theme.IsDark ? 0.25 : 0.10);
        Color activeFore = GetReadableTextColor(activeBack);
        Color iconFore = enabled ? (_theme.AccentColor == Color.Empty ? Color.FromArgb(0, 120, 215) : _theme.AccentColor) : mutedFore;

        var row = new Panel
        {
            Left = left,
            Top = top,
            Width = width,
            Height = 24,
            BackColor = active && FilterPopupHighlightActiveCommands ? activeBack : panelBack,
            Cursor = enabled ? Cursors.Hand : Cursors.Default,
            Tag = active
        };

        var iconLabel = new Label
        {
            Text = FilterPopupShowActionIcons ? icon : string.Empty,
            Left = 4,
            Top = 0,
            Width = FilterPopupShowActionIcons ? 24 : 0,
            Height = 24,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = active && FilterPopupHighlightActiveCommands ? activeFore : iconFore,
            BackColor = Color.Transparent,
            Font = new Font(Font.FontFamily, Font.Size + 1f, FontStyle.Bold)
        };

        var textLabel = new Label
        {
            Text = text,
            Left = FilterPopupShowActionIcons ? 30 : 7,
            Top = 0,
            Width = Math.Max(10, width - (FilterPopupShowActionIcons ? 34 : 10)),
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = enabled ? (active && FilterPopupHighlightActiveCommands ? activeFore : panelFore) : mutedFore,
            BackColor = Color.Transparent,
            Font = active && FilterPopupHighlightActiveCommands ? new Font(Font, FontStyle.Bold) : Font
        };

        row.Controls.Add(iconLabel);
        row.Controls.Add(textLabel);
        row.Resize += (_, __) => textLabel.Width = Math.Max(10, row.Width - textLabel.Left - 4);

        void SetRowBack(Color color)
        {
            row.BackColor = color;
            iconLabel.BackColor = color;
            textLabel.BackColor = color;
        }

        if (enabled)
        {
            row.MouseEnter += (_, __) =>
            {
                bool isActive = row.Tag is bool value && value;
                if (!(isActive && FilterPopupHighlightActiveCommands)) SetRowBack(hoverBack);
            };
            row.MouseLeave += (_, __) =>
            {
                bool isActive = row.Tag is bool value && value;
                SetRowBack(isActive && FilterPopupHighlightActiveCommands ? activeBack : panelBack);
            };
            row.Click += (_, __) => clickAction();
            iconLabel.Click += (_, __) => clickAction();
            textLabel.Click += (_, __) => clickAction();
            void ChildEnter()
            {
                bool isActive = row.Tag is bool value && value;
                if (!(isActive && FilterPopupHighlightActiveCommands)) SetRowBack(hoverBack);
            }
            void ChildLeave()
            {
                bool isActive = row.Tag is bool value && value;
                SetRowBack(isActive && FilterPopupHighlightActiveCommands ? activeBack : panelBack);
            }
            iconLabel.MouseEnter += (_, __) => ChildEnter();
            textLabel.MouseEnter += (_, __) => ChildEnter();
            iconLabel.MouseLeave += (_, __) => ChildLeave();
            textLabel.MouseLeave += (_, __) => ChildLeave();
        }

        SetRowBack(active && FilterPopupHighlightActiveCommands ? activeBack : panelBack);
        return row;
    }

    private Point ClampFilterPopupScreenLocation(Point desired, Size popupSize)
    {
        if (!FilterPopupLimitToWorkingArea)
            return desired;

        Rectangle work = Screen.FromPoint(desired).WorkingArea;
        int x = desired.X;
        int y = desired.Y;
        if (x + popupSize.Width > work.Right)
            x = Math.Max(work.Left, work.Right - popupSize.Width);
        if (y + popupSize.Height > work.Bottom)
            y = Math.Max(work.Top, work.Bottom - popupSize.Height);
        if (x < work.Left) x = work.Left;
        if (y < work.Top) y = work.Top;
        return new Point(x, y);
    }

    private void ShowFloatingEmbeddedFilterForm(ContextMenuStrip? ownerMenu, ViewGridColumn col, Point screenLocation)
    {
        if (_activeFilterPopupForm != null && !_activeFilterPopupForm.IsDisposed)
        {
            try { _activeFilterPopupForm.Close(); } catch { }
        }

        var current = _filters.Get(col.AspectName);
        Color filterPanelBack = GetFilterPopupPanelBack();
        Color filterControlBack = GetFilterPopupControlBack();
        Color filterPanelFore = GetFilterPopupPanelFore();
        Color filterControlFore = GetFilterPopupControlFore();
        Color filterMutedFore = GetFilterPopupMutedFore(filterPanelBack);
        int embeddedScanLimit = GetFilterMenuScanLimit();
        List<string> values = GetDistinctColumnValues(col, embeddedScanLimit).ToList();
        int maxPopupItems = Math.Max(200, MaxEmbeddedFilterVisibleValues);
        bool embeddedUpdating = false;
        bool fullValuesLoaded = embeddedScanLimit >= _provider.Count;
        int popupGeneration = ++_filterPopupGeneration;

        string popupSizeKey = BuildFilterPopupSizeMemoryKey(col);
        Size minPopupSize = GetEffectiveFilterPopupMinimumSize();
        Size maxPopupSize = GetEffectiveFilterPopupMaximumSize(screenLocation, minPopupSize);
        Size desiredPopupSize = GetInitialFilterPopupSize(col, minPopupSize, maxPopupSize, popupSizeKey, allowRememberedSize: true);
        Point popupLocation = ClampFilterPopupScreenLocation(screenLocation, desiredPopupSize);

        var popup = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false,
            TopMost = true,
            BackColor = filterPanelBack,
            ForeColor = filterControlFore,
            MinimumSize = minPopupSize,
            MaximumSize = maxPopupSize,
            Size = desiredPopupSize,
            Location = popupLocation,
            Padding = Padding.Empty
        };
        _activeFilterPopupForm = popup;
        InstallMenuDismissMessageFilter(ownerMenu, popup);

        popup.Paint += (_, pe) =>
        {
            pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var backBrush = new SolidBrush(filterPanelBack);
            pe.Graphics.FillRectangle(backBrush, popup.ClientRectangle);
        };

        var panel = new Panel { Left = 0, Top = 0, Width = Math.Max(280, popup.ClientSize.Width), Height = popup.ClientSize.Height, Padding = new Padding(12), BackColor = filterPanelBack, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom };
        popup.Controls.Add(panel);
        panel.Paint += (_, pe) =>
        {
            using var pen = new Pen(_theme.BorderColor);
            pe.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        var sortAsc = CreateFloatingFilterActionLabel("▲", ViewGridText.SortAscending, 12, 10, 310, col.Sortable, _sortColumn == col && !_sortDesc, () => { SortBy(col, false); popup.Close(); });
        var sortDesc = CreateFloatingFilterActionLabel("▼", ViewGridText.SortDescending, 12, 34, 310, col.Sortable, _sortColumn == col && _sortDesc, () => { SortBy(col, true); popup.Close(); });
        var unsort = CreateFloatingFilterActionLabel("↕", ViewGridText.Unsort, 12, 58, 310, _sortColumn == col, false, () => { ClearSort(col); popup.Close(); });
        var sep1 = new Panel { Left = 10, Top = 84, Width = 326, Height = 1, BackColor = _theme.BorderColor };
        var caption = new Label { Text = ViewGridText.ColumnFilterTitle(col.Header), Left = 16, Top = 100, Width = 310, Height = 22, ForeColor = filterPanelFore, BackColor = filterPanelBack, Font = new Font(Font, FontStyle.Bold) };
        var search = new TextBox { PlaceholderText = ViewGridText.SearchPlaceholder, Left = 16, Top = 126, Width = 300, Height = 24, BackColor = filterControlBack, ForeColor = filterControlFore, BorderStyle = BorderStyle.FixedSingle };
        var useTextFilter = new CheckBox { Text = ViewGridText.ApplyTypedTextAsFilter, Left = 16, Top = 158, Width = 310, Height = 22, ForeColor = filterControlFore, BackColor = filterPanelBack, FlatStyle = FlatStyle.Flat };
        if (TypedFilterSearchesAllRows && FastFilterMenuForHugeLists && _provider.Count > 20_000) useTextFilter.Checked = true;
        var selectAll = new CheckBox { Text = ViewGridText.SelectAll, Left = 16, Top = 184, Width = 310, Height = 22, ForeColor = filterControlFore, BackColor = filterPanelBack, FlatStyle = FlatStyle.Flat, ThreeState = true, AutoCheck = false };
        var list = new CheckedListBox { Left = 16, Top = 210, Width = 300, Height = 180, CheckOnClick = true, IntegralHeight = false, HorizontalScrollbar = true, BackColor = filterControlBack, ForeColor = filterControlFore, BorderStyle = BorderStyle.FixedSingle };
        var previewInfo = new Label { Text = IsHugeFastFilterPreview() ? ViewGridText.FastPreviewInfo : string.Empty, Left = 16, Top = 394, Width = 300, Height = 16, ForeColor = filterMutedFore, BackColor = filterPanelBack };
        var limitedInfo = new Panel { Left = 16, Top = 414, Width = 300, Height = 46, BackColor = ControlPaint.Light(_theme.AccentColor, 0.78f), Visible = IsHugeFastFilterPreview() };
        var limitedIcon = new Label { Text = "ⓘ", Left = 6, Top = 12, Width = 22, Height = 22, BackColor = limitedInfo.BackColor, ForeColor = Color.Black };
        var limitedLabel = new Label { Text = ViewGridText.FastPreviewLimitedInfo, Left = 30, Top = 4, Width = 238, Height = 38, BackColor = limitedInfo.BackColor, ForeColor = Color.Black };
        var limitedClose = new Label { Text = "×", Left = 274, Top = 10, Width = 20, Height = 22, BackColor = limitedInfo.BackColor, ForeColor = Color.Black, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        limitedClose.Click += (_,__) => limitedInfo.Visible = false;
        limitedInfo.Controls.AddRange(new Control[] { limitedIcon, limitedLabel, limitedClose });
        var clear = new Button { Text = ViewGridText.Clear, Left = 130, Top = 470, Width = 78, Height = 30, BackColor = filterControlBack, ForeColor = filterControlFore, FlatStyle = FlatStyle.Flat };
        var apply = new Button { Text = ViewGridText.Apply, Left = 220, Top = 470, Width = 78, Height = 30, BackColor = _theme.AccentColor, ForeColor = GetReadableTextColor(_theme.AccentColor), FlatStyle = FlatStyle.Flat };
        var sep2 = new Panel { Left = 10, Top = 510, Width = 326, Height = 1, BackColor = _theme.BorderColor };
        var openWindow = CreateFloatingFilterActionLabel("▽", ViewGridText.SeparateFilterWindow, 12, 520, 310, true, false, () => { popup.Close(); BeginInvoke(new Action(() => ShowFilterMenuForColumn(col, PointToClient(popup.Location)))); });
        var clearAll = CreateFloatingFilterActionLabel("×", ViewGridText.ClearAllFilters, 12, 542, 310, true, current != null, () => { ClearFilters(); popup.Close(); });
        var resizeGrip = new Panel
        {
            Width = 22,
            Height = 22,
            Cursor = Cursors.SizeNWSE,
            BackColor = filterPanelBack,
            Visible = FilterPopupResizable,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        resizeGrip.Paint += (_, pe) =>
        {
            pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(filterMutedFore);
            int right = resizeGrip.Width - 5;
            int bottom = resizeGrip.Height - 5;
            pe.Graphics.DrawLine(pen, right, bottom - 12, right - 12, bottom);
            pe.Graphics.DrawLine(pen, right, bottom - 8, right - 8, bottom);
            pe.Graphics.DrawLine(pen, right, bottom - 4, right - 4, bottom);
        };

        clear.FlatAppearance.BorderColor = _theme.BorderColor;
        apply.FlatAppearance.BorderColor = _theme.AccentColor;
        panel.Controls.AddRange(new Control[] { sortAsc, sortDesc, unsort, sep1, caption, search, useTextFilter, selectAll, list, previewInfo, limitedInfo, clear, apply, sep2, openWindow, clearAll, resizeGrip });
        ApplyFilterPopupTheme(panel);
        apply.BackColor = _theme.AccentColor;
        apply.ForeColor = GetReadableTextColor(_theme.AccentColor);
        apply.FlatAppearance.BorderColor = _theme.AccentColor;
        clear.FlatAppearance.BorderColor = _theme.BorderColor == Color.Empty ? ControlPaint.Dark(filterControlBack) : _theme.BorderColor;

        void LayoutFloatingFilterPopup()
        {
            panel.SetBounds(0, 0, Math.Max(280, popup.ClientSize.Width), popup.ClientSize.Height);
            int w = Math.Max(220, panel.ClientSize.Width - 32);
            int bottom = panel.ClientSize.Height;
            sortAsc.Width = w;
            sortDesc.Width = w;
            unsort.Width = w;
            sep1.Width = w + 16;
            caption.Width = w;
            search.Width = w;
            useTextFilter.Width = w;
            selectAll.Width = w;
            list.Width = w;

            int clearTop = Math.Max(300, bottom - 100);
            int listHeight = Math.Max(86, clearTop - list.Top - 76);
            list.Height = listHeight;
            previewInfo.Top = list.Bottom + 4;
            previewInfo.Width = w;
            limitedInfo.Top = previewInfo.Bottom + 4;
            limitedInfo.Width = w;
            limitedLabel.Width = Math.Max(80, limitedInfo.Width - 60);
            limitedClose.Left = limitedInfo.Width - 26;

            clear.Top = clearTop;
            apply.Top = clearTop;
            apply.Left = Math.Max(16, panel.ClientSize.Width - apply.Width - 28);
            clear.Left = Math.Max(16, apply.Left - clear.Width - 12);
            sep2.Top = bottom - 60;
            sep2.Width = w + 16;
            openWindow.Top = bottom - 50;
            openWindow.Width = w;
            clearAll.Top = bottom - 28;
            clearAll.Width = w;
            resizeGrip.Left = panel.ClientSize.Width - resizeGrip.Width - 3;
            resizeGrip.Top = panel.ClientSize.Height - resizeGrip.Height - 3;
            resizeGrip.BringToFront();
            panel.Invalidate();
            popup.Invalidate();
        }

        bool resizingPopup = false;
        Point resizeStartMouse = Point.Empty;
        Size resizeStartSize = Size.Empty;
        resizeGrip.MouseDown += (_, e) =>
        {
            if (!FilterPopupResizable || e.Button != MouseButtons.Left) return;
            resizingPopup = true;
            resizeStartMouse = Control.MousePosition;
            resizeStartSize = popup.Size;
            resizeGrip.Capture = true;
        };
        resizeGrip.MouseMove += (_, __) =>
        {
            if (!resizingPopup) return;
            Point mouse = Control.MousePosition;
            int newWidth = resizeStartSize.Width + (mouse.X - resizeStartMouse.X);
            int newHeight = resizeStartSize.Height + (mouse.Y - resizeStartMouse.Y);
            newWidth = Math.Max(minPopupSize.Width, Math.Min(maxPopupSize.Width, newWidth));
            newHeight = Math.Max(minPopupSize.Height, Math.Min(maxPopupSize.Height, newHeight));
            popup.Size = new Size(newWidth, newHeight);
        };
        resizeGrip.MouseUp += (_, __) =>
        {
            if (!resizingPopup) return;
            resizingPopup = false;
            resizeGrip.Capture = false;
            if (FilterPopupRememberSize) _floatingFilterPopupSizeMemory[popupSizeKey] = popup.Size;
        };

        bool edgeResizingPopup = false;
        Point edgeResizeStartMouse = Point.Empty;
        Size edgeResizeStartSize = Size.Empty;
        void StartEdgeResize(MouseEventArgs e)
        {
            if (!FilterPopupResizable || !FilterPopupEdgeResize || e.Button != MouseButtons.Left) return;
            bool nearRight = e.X >= popup.ClientSize.Width - 8;
            bool nearBottom = e.Y >= popup.ClientSize.Height - 8;
            if (!nearRight && !nearBottom) return;
            edgeResizingPopup = true;
            edgeResizeStartMouse = Control.MousePosition;
            edgeResizeStartSize = popup.Size;
            popup.Capture = true;
        }
        void ContinueEdgeResize(MouseEventArgs e)
        {
            bool nearRight = e.X >= popup.ClientSize.Width - 8;
            bool nearBottom = e.Y >= popup.ClientSize.Height - 8;
            if (!edgeResizingPopup)
            {
                if (FilterPopupResizable && FilterPopupEdgeResize && (nearRight || nearBottom)) popup.Cursor = Cursors.SizeNWSE;
                else if (popup.Cursor == Cursors.SizeNWSE) popup.Cursor = Cursors.Default;
                return;
            }
            Point mouse = Control.MousePosition;
            int newWidth = edgeResizeStartSize.Width + (mouse.X - edgeResizeStartMouse.X);
            int newHeight = edgeResizeStartSize.Height + (mouse.Y - edgeResizeStartMouse.Y);
            popup.Size = new Size(Math.Max(minPopupSize.Width, Math.Min(maxPopupSize.Width, newWidth)), Math.Max(minPopupSize.Height, Math.Min(maxPopupSize.Height, newHeight)));
        }
        void EndEdgeResize()
        {
            if (!edgeResizingPopup) return;
            edgeResizingPopup = false;
            popup.Capture = false;
            if (FilterPopupRememberSize) _floatingFilterPopupSizeMemory[popupSizeKey] = popup.Size;
        }
        popup.MouseDown += (_, e) => StartEdgeResize(e);
        popup.MouseMove += (_, e) => ContinueEdgeResize(e);
        popup.MouseUp += (_, __) => EndEdgeResize();
        panel.MouseDown += (_, e) => StartEdgeResize(new MouseEventArgs(e.Button, e.Clicks, e.X + panel.Left, e.Y + panel.Top, e.Delta));
        panel.MouseMove += (_, e) => ContinueEdgeResize(new MouseEventArgs(e.Button, e.Clicks, e.X + panel.Left, e.Y + panel.Top, e.Delta));
        panel.MouseUp += (_, __) => EndEdgeResize();

        void HookChildEdgeResize(Control rootControl)
        {
            foreach (Control child in rootControl.Controls)
            {
                child.MouseDown += (_, e) => StartEdgeResize(new MouseEventArgs(e.Button, e.Clicks, e.X + child.Left + panel.Left, e.Y + child.Top + panel.Top, e.Delta));
                child.MouseMove += (_, e) => ContinueEdgeResize(new MouseEventArgs(e.Button, e.Clicks, e.X + child.Left + panel.Left, e.Y + child.Top + panel.Top, e.Delta));
                child.MouseUp += (_, __) => EndEdgeResize();
                if (child.HasChildren) HookChildEdgeResize(child);
            }
        }
        HookChildEdgeResize(panel);

        popup.Resize += (_, __) =>
        {
            popup.Location = ClampFilterPopupScreenLocation(popup.Location, popup.Size);
            LayoutFloatingFilterPopup();
            if (FilterPopupRememberSize) _floatingFilterPopupSizeMemory[popupSizeKey] = popup.Size;
        };

        ToolTip? valueToolTip = null;
        int lastToolTipIndex = -1;
        if (FilterPopupShowValueTooltips)
        {
            valueToolTip = new ToolTip
            {
                ShowAlways = true,
                AutomaticDelay = 250,
                AutoPopDelay = 12000,
                InitialDelay = 250,
                ReshowDelay = 100
            };
            list.MouseMove += (_, e) =>
            {
                int index = list.IndexFromPoint(e.Location);
                if (index == lastToolTipIndex) return;
                lastToolTipIndex = index;
                if (index < 0 || index >= list.Items.Count)
                {
                    valueToolTip.SetToolTip(list, string.Empty);
                    return;
                }
                string text = Convert.ToString(list.Items[index]) ?? string.Empty;
                int availableWidth = Math.Max(40, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 28);
                bool clipped = TextRenderer.MeasureText(text, list.Font).Width > availableWidth;
                valueToolTip.SetToolTip(list, clipped ? text : string.Empty);
            };
            list.MouseLeave += (_, __) =>
            {
                lastToolTipIndex = -1;
                valueToolTip.SetToolTip(list, string.Empty);
            };
        }

        LayoutFloatingFilterPopup();

        bool IsChecked(string value) => current?.Mode == ViewGridFilterMode.ValueList && current.SelectedValues != null ? current.SelectedValues.Contains(value) : true;
        void Refill()
        {
            string q = search.Text.Trim();
            list.BeginUpdate();
            try
            {
                list.Items.Clear();
                var exact = q.Length == 0 ? null : values.FirstOrDefault(v => string.Equals(v, q, StringComparison.CurrentCultureIgnoreCase));
                var source = (exact != null ? new[] { exact } : values.Where(v => q.Length == 0 || v.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0)).ToList();
                if (q.Length > 0 && !source.Any(v => string.Equals(v, q, StringComparison.CurrentCultureIgnoreCase)))
                    source.Insert(0, q);
                int added = 0;
                foreach (string v in source)
                {
                    list.Items.Add(v, IsChecked(v));
                    if (++added >= maxPopupItems) break;
                }
            }
            finally { list.EndUpdate(); }
            UpdateEmbeddedSelectAllState();
            if (previewInfo != null)
                previewInfo.Text = fullValuesLoaded ? string.Format("{0:N0}", values.Count) : ViewGridText.FastPreviewInfo;
            if (limitedInfo != null)
                limitedInfo.Visible = !fullValuesLoaded && values.Count >= embeddedScanLimit && string.IsNullOrWhiteSpace(search.Text);
        }

        int searchGeneration = 0;
        async void LoadValuesForCurrentSearchAsync()
        {
            string q = search.Text.Trim();
            bool hasSearch = !string.IsNullOrWhiteSpace(q);
            if (!hasSearch && (!AsyncLoadFullFilterValues || fullValuesLoaded || _provider.Count <= embeddedScanLimit)) return;

            // v24.82: Refill() already shows the typed candidate immediately.
            // Keep full distinct matching on the background thread to avoid UI freezes.

            int generation = ++searchGeneration;

            int scanRows = hasSearch
                ? GetFilterSearchScanLimit()
                : Math.Min(_provider.Count, Math.Max(embeddedScanLimit, MaxAsyncFilterDistinctScanRows));
            try
            {
                string requestedSearch = q;
                // v24.82 FULL STABLE: no cancellation exception while typing. Older
                // searches may finish later, but generation/current-search checks decide
                // whether their values are still useful.
                var loadedValues = await Task.Run(() =>
                {
                    try
                    {
                        return GetDistinctColumnValues(col, scanRows, hasSearch ? requestedSearch : null).ToList();
                    }
                    catch (OperationCanceledException)
                    {
                        return new List<string>();
                    }
                    catch
                    {
                        return new List<string>();
                    }
                });
                if (IsDisposed || popupGeneration != _filterPopupGeneration || popup.IsDisposed) return;

                string currentSearch = search.Text.Trim();
                bool isCurrentSearch = generation == searchGeneration;
                bool canReuseBroaderResult =
                    requestedSearch.Length > 0 &&
                    currentSearch.Length >= requestedSearch.Length &&
                    currentSearch.StartsWith(requestedSearch, StringComparison.CurrentCultureIgnoreCase);

                if (!isCurrentSearch && !canReuseBroaderResult) return;

                values = loadedValues;
                fullValuesLoaded = !hasSearch && scanRows >= _provider.Count && isCurrentSearch;
                Refill();
            }
            catch (OperationCanceledException) { }
            catch { }
        }
        void CloseAllMenus()
        {
            try { popup.Close(); } catch { }
            _allowActiveMenuClose = true;
            try { if (ownerMenu != null && !ownerMenu.IsDisposed) ownerMenu.Close(ToolStripDropDownCloseReason.CloseCalled); } catch { }
        }
        var popupSearchDebounce = new System.Windows.Forms.Timer { Interval = 90 };
        popupSearchDebounce.Tick += (_,__) => { popupSearchDebounce.Stop(); LoadValuesForCurrentSearchAsync(); };
        search.TextChanged += (_,__) =>
        {
            Refill();
            if (!fullValuesLoaded && !string.IsNullOrWhiteSpace(search.Text) && previewInfo != null)
                previewInfo.Text = ViewGridText.SearchingPreviewResults;
            popupSearchDebounce.Stop();
            popupSearchDebounce.Start();
        };
        void SetEmbeddedVisibleItemsChecked(bool isChecked)
        {
            if (embeddedUpdating) return;
            embeddedUpdating = true;
            try
            {
                for (int i = 0; i < list.Items.Count; i++)
                    list.SetItemChecked(i, isChecked);
            }
            finally
            {
                embeddedUpdating = false;
            }
            UpdateEmbeddedSelectAllState();
        }
        void ToggleEmbeddedSelectAllFromUser()
        {
            // Only a real Checked state means "all selected". Indeterminate means partial selection
            // and the next click should select all, not clear the user's partial filter choice.
            SetEmbeddedVisibleItemsChecked(selectAll.CheckState != CheckState.Checked);
        }
        void QueueEmbeddedSelectAllStateRefresh()
        {
            if (embeddedUpdating || IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke((Action)UpdateEmbeddedSelectAllState);
            }
            catch (InvalidOperationException)
            {
                UpdateEmbeddedSelectAllState();
            }
        }
        void UpdateEmbeddedSelectAllState()
        {
            if (embeddedUpdating) return;
            int count = list.Items.Count;
            int checkedCount = 0;
            for (int i = 0; i < count; i++)
                if (list.GetItemCheckState(i) == CheckState.Checked) checkedCount++;
            CheckState newState;
            if (count == 0 || checkedCount == 0)
                newState = CheckState.Unchecked;
            else if (checkedCount == count)
                newState = CheckState.Checked;
            else
                newState = CheckState.Indeterminate;
            if (selectAll.CheckState == newState) return;
            embeddedUpdating = true;
            try
            {
                selectAll.CheckState = newState;
                selectAll.Checked = newState == CheckState.Checked;
            }
            finally { embeddedUpdating = false; }
        }
        selectAll.Click += (_,__) => ToggleEmbeddedSelectAllFromUser();
        selectAll.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Space) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            ToggleEmbeddedSelectAllFromUser();
        };
        list.ItemCheck += (_,__) => QueueEmbeddedSelectAllStateRefresh();
        list.MouseUp += (_,__) => QueueEmbeddedSelectAllStateRefresh();
        list.KeyUp += (_,__) => QueueEmbeddedSelectAllStateRefresh();
        clear.Click += (_,__) => { _filters.Clear(col.AspectName); BuildViewIndex(); CloseAllMenus(); };
        apply.Click += (_,__) =>
        {
            string q = search.Text.Trim();
            var exact = q.Length == 0 ? null : values.FirstOrDefault(v => string.Equals(v, q, StringComparison.CurrentCultureIgnoreCase));
            if (useTextFilter.Checked && !string.IsNullOrWhiteSpace(q))
            {
                SetColumnFilter(new ViewGridColumnFilter { AspectName = col.AspectName, Mode = ViewGridFilterMode.Contains, Text = q, Enabled = true });
                CloseAllMenus();
                return;
            }
            var selected = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            if (exact != null) selected.Add(exact);
            else for (int i = 0; i < list.Items.Count; i++) if (list.GetItemChecked(i)) selected.Add(Convert.ToString(list.Items[i]) ?? string.Empty);
            // v28.4.1: CheckBox.Checked is true for Indeterminate in WinForms.
            // Therefore using Checked here incorrectly treated a partial selection as "all selected"
            // and cleared the filter. Only CheckState.Checked may clear the column filter.
            bool allSelected = string.IsNullOrWhiteSpace(q) && list.Items.Count > 0 && selectAll.CheckState == CheckState.Checked;
            if (allSelected) { _filters.Clear(col.AspectName); BuildViewIndex(); }
            else SetColumnFilter(new ViewGridColumnFilter { AspectName = col.AspectName, Mode = ViewGridFilterMode.ValueList, SelectedValues = selected, Enabled = true });
            CloseAllMenus();
        };
        void FloatingFilterShortcut(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                apply.PerformClick();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                CloseAllMenus();
            }
        }
        popup.KeyPreview = true;
        popup.KeyDown += FloatingFilterShortcut;
        search.KeyDown += FloatingFilterShortcut;
        list.KeyDown += FloatingFilterShortcut;
        useTextFilter.KeyDown += FloatingFilterShortcut;
        selectAll.KeyDown += FloatingFilterShortcut;

        popup.Deactivate += (_,__) =>
        {
            var p = Control.MousePosition;
            bool insideOwner = ownerMenu != null && !ownerMenu.IsDisposed && ownerMenu.Bounds.Contains(p);
            if (!popup.Bounds.Contains(p) && !insideOwner)
            {
                try { popup.Close(); } catch { }
                if (ownerMenu != null) CloseOwnerMenuAfterFilterPopupDismiss(ownerMenu);
            }
        };
        popup.FormClosed += (_,__) =>
        {
            try { if (FilterPopupRememberSize) _floatingFilterPopupSizeMemory[popupSizeKey] = popup.Size; } catch { }
            try { valueToolTip?.Dispose(); } catch { }
            try { popupSearchDebounce.Dispose(); } catch { }
            if (ReferenceEquals(_activeFilterPopupForm, popup))
            {
                _activeFilterPopupForm = null;
                RemoveMenuDismissMessageFilter();
            }
            var p = Control.MousePosition;
            if (ownerMenu != null && !ownerMenu.IsDisposed && !ownerMenu.Bounds.Contains(p))
                CloseOwnerMenuAfterFilterPopupDismiss(ownerMenu);
        };
        Refill();
        LoadValuesForCurrentSearchAsync();
        popup.Show(this);
        InstallMenuDismissMessageFilter(null, popup);
        search.Focus();
    }

    private void InstallMenuDismissMessageFilter(ContextMenuStrip? ownerMenu, Form popup)
    {
        RemoveMenuDismissMessageFilter();
        _activeMenuDismissFilter = new ViewGridMenuDismissMessageFilter(
            isInsideAllowedArea: () =>
            {
                var p = Control.MousePosition;
                bool insideOwner = ownerMenu != null && !ownerMenu.IsDisposed && ownerMenu.Bounds.Contains(p);
                bool insidePopup = popup != null && !popup.IsDisposed && popup.Bounds.Contains(p);
                return insideOwner || insidePopup;
            },
            closeAll: () =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        _allowActiveMenuClose = true;
                        RemoveMenuDismissMessageFilter();
                        try { if (popup != null && !popup.IsDisposed) popup.Close(); } catch { }
                        try { if (ownerMenu != null && !ownerMenu.IsDisposed) ownerMenu.Close(ToolStripDropDownCloseReason.CloseCalled); } catch { }
                    }));
                }
                catch { }
            });
        Application.AddMessageFilter(_activeMenuDismissFilter);
    }

    private void RemoveMenuDismissMessageFilter()
    {
        var filter = _activeMenuDismissFilter;
        if (filter == null) return;
        _activeMenuDismissFilter = null;
        try
        {
            filter.Disable();
            Application.RemoveMessageFilter(filter);
        }
        catch { }
    }

    private void CloseActiveFilterPopupOnly()
    {
        if (_activeFilterPopupForm == null || _activeFilterPopupForm.IsDisposed) return;
        try { _activeFilterPopupForm.Close(); }
        catch { }
    }

    private void CloseOwnerMenuAfterFilterPopupDismiss(ContextMenuStrip ownerMenu)
    {
        // v24.38: Filtre popup'ından vazgeçilince ana sağ tık menüsü eski sürümlerde
        // Closing iptal mantığı nedeniyle ekranda takılı kalabiliyordu. Popup artık aktif değilse
        // ana menüyü de güvenli şekilde kapatıyoruz.
        if (ownerMenu == null || ownerMenu.IsDisposed) return;
        try
        {
            _allowActiveMenuClose = true;
            ownerMenu.Close(ToolStripDropDownCloseReason.CloseCalled);
        }
        catch { }
    }

    private void AttachFilterPopupDismissHandlers(ContextMenuStrip menu, ToolStripItem? filterLauncher)
    {
        foreach (ToolStripItem item in menu.Items)
        {
            if (ReferenceEquals(item, filterLauncher)) continue;
            item.MouseEnter += (_, __) => CloseActiveFilterPopupOnly();
            if (item is ToolStripMenuItem mi)
            {
                mi.DropDownOpening += (_, __) => CloseActiveFilterPopupOnly();
            }
        }
    }

    private bool IsMenuGroupVisible(ViewGridMenuGroups group, bool header)
    {
        if (MenuProfile == ViewGridMenuProfile.None) return false;
        ViewGridMenuGroups groups = header ? HeaderMenuGroups : BodyMenuGroups;
        if ((groups & group) != group) return false;

        return MenuProfile switch
        {
            ViewGridMenuProfile.Full => true,
            ViewGridMenuProfile.Custom => true,
            ViewGridMenuProfile.Standard => group is ViewGridMenuGroups.Filter or ViewGridMenuGroups.Sort or ViewGridMenuGroups.AutoSize or ViewGridMenuGroups.ColumnChooser or ViewGridMenuGroups.ViewMode or ViewGridMenuGroups.State or ViewGridMenuGroups.Scenario or ViewGridMenuGroups.Clipboard or ViewGridMenuGroups.Editing or ViewGridMenuGroups.RowDetails,
            ViewGridMenuProfile.Minimal => group is ViewGridMenuGroups.Filter or ViewGridMenuGroups.Sort or ViewGridMenuGroups.AutoSize or ViewGridMenuGroups.Clipboard,
            ViewGridMenuProfile.ReadOnly => group is ViewGridMenuGroups.Filter or ViewGridMenuGroups.Sort or ViewGridMenuGroups.AutoSize or ViewGridMenuGroups.ColumnChooser or ViewGridMenuGroups.ViewMode or ViewGridMenuGroups.State or ViewGridMenuGroups.Scenario or ViewGridMenuGroups.Clipboard or ViewGridMenuGroups.RowDetails,
            _ => true
        };
    }

    public void ApplyMenuProfile(ViewGridMenuProfile profile)
    {
        MenuProfile = profile;
        switch (profile)
        {
            case ViewGridMenuProfile.None:
                UseBuiltInHeaderMenu = false;
                UseBuiltInBodyMenu = false;
                HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.None;
                break;
            case ViewGridMenuProfile.Full:
                UseBuiltInHeaderMenu = true;
                UseBuiltInBodyMenu = true;
                HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full;
                HeaderMenuGroups = ViewGridMenuGroups.All;
                BodyMenuGroups = ViewGridMenuGroups.All;
                break;
            case ViewGridMenuProfile.Standard:
                UseBuiltInHeaderMenu = true;
                UseBuiltInBodyMenu = true;
                HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full;
                HeaderMenuGroups = ViewGridMenuGroups.Filter | ViewGridMenuGroups.Sort | ViewGridMenuGroups.AutoSize | ViewGridMenuGroups.ColumnChooser | ViewGridMenuGroups.ViewMode | ViewGridMenuGroups.State | ViewGridMenuGroups.Scenario;
                BodyMenuGroups = ViewGridMenuGroups.Clipboard | ViewGridMenuGroups.Editing | ViewGridMenuGroups.RowDetails | ViewGridMenuGroups.AutoSize | ViewGridMenuGroups.ColumnChooser | ViewGridMenuGroups.ViewMode | ViewGridMenuGroups.State | ViewGridMenuGroups.Scenario;
                break;
            case ViewGridMenuProfile.Minimal:
                UseBuiltInHeaderMenu = true;
                UseBuiltInBodyMenu = true;
                HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full;
                HeaderMenuGroups = ViewGridMenuGroups.Filter | ViewGridMenuGroups.Sort | ViewGridMenuGroups.AutoSize;
                BodyMenuGroups = ViewGridMenuGroups.Clipboard | ViewGridMenuGroups.AutoSize;
                break;
            case ViewGridMenuProfile.ReadOnly:
                UseBuiltInHeaderMenu = true;
                UseBuiltInBodyMenu = true;
                HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full;
                HeaderMenuGroups = ViewGridMenuGroups.Filter | ViewGridMenuGroups.Sort | ViewGridMenuGroups.AutoSize | ViewGridMenuGroups.ColumnChooser | ViewGridMenuGroups.ViewMode | ViewGridMenuGroups.State | ViewGridMenuGroups.Scenario;
                BodyMenuGroups = ViewGridMenuGroups.Clipboard | ViewGridMenuGroups.RowDetails | ViewGridMenuGroups.AutoSize | ViewGridMenuGroups.ColumnChooser | ViewGridMenuGroups.ViewMode | ViewGridMenuGroups.State | ViewGridMenuGroups.Scenario;
                break;
        }
        Invalidate();
    }

    public void ShowMenuGroup(ViewGridMenuGroups group, bool visible, bool header = true, bool body = true)
    {
        if (header) HeaderMenuGroups = visible ? HeaderMenuGroups | group : HeaderMenuGroups & ~group;
        if (body) BodyMenuGroups = visible ? BodyMenuGroups | group : BodyMenuGroups & ~group;
        if (MenuProfile != ViewGridMenuProfile.None) MenuProfile = ViewGridMenuProfile.Custom;
    }

    private void ShowBuiltInContextMenu(Point location)
    {
        bool isHeaderMenu = ShowHeader && location.Y < HeaderHeight;
        if (MenuProfile == ViewGridMenuProfile.None) return;
        if (isHeaderMenu && !UseBuiltInHeaderMenu) return;
        if (!isHeaderMenu && !UseBuiltInBodyMenu) return;
        if (isHeaderMenu && HeaderMenuOverride != null && !HeaderMenuOverride.IsDisposed)
        {
            ApplyViewGridThemeToMenu(HeaderMenuOverride);
            HeaderMenuOverride.Show(this, location);
            return;
        }

        if (!isHeaderMenu && BodyMenuOverride != null && !BodyMenuOverride.IsDisposed)
        {
            ApplyViewGridThemeToMenu(BodyMenuOverride);
            BodyMenuOverride.Show(this, location);
            return;
        }

        if (_activeHeaderMenu != null && !_activeHeaderMenu.IsDisposed)
        {
            try { _activeHeaderMenu.Close(ToolStripDropDownCloseReason.CloseCalled); }
            catch { }
        }

        if (ShowHeader && location.Y < HeaderHeight)
        {
            var headerColForBehavior = HitColumn(location.X);
            if (HeaderContextMenuBehavior == ViewGridHeaderContextMenuBehavior.None) return;
            if (HeaderContextMenuBehavior == ViewGridHeaderContextMenuBehavior.FilterOnly)
            {
                if (ShowFilterMenu && ShowHeaderMenuFilterItems && headerColForBehavior != null && headerColForBehavior.Filterable)
                    ShowFloatingEmbeddedFilterForm(null, headerColForBehavior, PointToScreen(new Point(Math.Max(0, GetColumnLeft(headerColForBehavior)), HeaderHeight)));
                return;
            }
        }

        _allowActiveMenuClose = false;
        var menu = new ContextMenuStrip
        {
            Renderer = new global::ViewGrid.Theming.SmartMenuRenderer(_theme),
            BackColor = _theme.PanelBackColor,
            ForeColor = _theme.ForeColor,
            AutoClose = true,
            ShowImageMargin = true,
            ShowCheckMargin = false
        };
        _activeHeaderMenu = menu;
        ToolStripItem? filterLauncherItem = null;
        menu.Closing += (_, e) =>
        {
            if (ColumnChooserMenuStaysOpen && e.CloseReason == ToolStripDropDownCloseReason.ItemClicked && _columnChooserMenuKeepOpenRequested)
            {
                _columnChooserMenuKeepOpenRequested = false;
                e.Cancel = true;
                return;
            }

            // v24.47: Sağ tık menüsü artık filtre editörünü kendi içinde host etmiyor;
            // filtre ayrı borderless popup form olarak açılıyor. Bu yüzden Closing iptal
            // edilirse menü dışarı tıklamada ekranda takılı kalabiliyor. Normal WinForms
            // davranışına izin veriyoruz. Popup gerekiyorsa kendi message filter'ı kapanmayı yönetir.
            _allowActiveMenuClose = true;
            e.Cancel = false;
        };
        menu.ItemClicked += (_, e) =>
        {
            // Hızlı kolon görünürlük menüsünde ObjectListView gibi menü açık kalabilir.
            if (IsColumnChooserVisibilityMenuItem(e.ClickedItem))
            {
                _columnChooserMenuKeepOpenRequested = true;
                _allowActiveMenuClose = false;
                return;
            }

            // Alt menü başlığı veya embedded host tıklanınca kapatma yok; normal komut seçilince menü kapanır.
            if (e.ClickedItem is ToolStripControlHost) return;
            if (e.ClickedItem is ToolStripMenuItem mi && mi.DropDownItems.Count > 0) return;
            _allowActiveMenuClose = true;
            try { menu.Close(ToolStripDropDownCloseReason.ItemClicked); } catch { }
        };
        menu.Closed += (_, __) =>
        {
            RemoveMenuDismissMessageFilter();
            _allowActiveMenuClose = false;
            if (ReferenceEquals(_activeHeaderMenu, menu)) _activeHeaderMenu = null;
            // Do not dispose synchronously here. WinForms can still process pending ToolStrip messages.
            BeginInvoke(new Action(() =>
            {
                try { if (!menu.IsDisposed) menu.Dispose(); }
                catch { }
            }));
        };

        if (ShowHeader && location.Y < HeaderHeight)
        {
            var col = HitColumn(location.X);
            if (col != null)
            {
                if (ShowHeaderMenuFilterItems && IsMenuGroupVisible(ViewGridMenuGroups.Filter, true) && ShowFilterMenu && col.Filterable)
                {
                    if (FilterMenuMode == ViewGridFilterMenuMode.PopupMenu || FilterMenuMode == ViewGridFilterMenuMode.Both || UseEmbeddedHeaderFilterMenu)
                    {
                        // Seçili filtre stiline göre sadece ilgili filtre menüsünü göster.
                        // Both seçilirse hem popup filtre hem ayrı pencere görünür.
                        filterLauncherItem = CreateEmbeddedFilterMenuItem(col);
                        menu.Items.Add(filterLauncherItem);
                    }
                    if (FilterMenuMode == ViewGridFilterMenuMode.ModalWindow || FilterMenuMode == ViewGridFilterMenuMode.Both)
                    {
                        menu.Items.Add(ViewGridText.FilterWindow(col.Header), null, (_,__) =>
                        {
                            var openLocation = location;
                            CloseActiveMenusSafe();
                            BeginInvoke(new Action(() => ShowFilterMenuForColumn(col, openLocation)));
                        });
                    }
                    if (ShowHeaderMenuFilterStyleItems && IsMenuGroupVisible(ViewGridMenuGroups.FilterStyle, true) && ShowFilterStyleSelectorInContextMenu) AddFilterStyleMenu(menu);
                    if (ShowAdvancedFilterMenuItems) AddAdvancedFilterMenu(menu.Items);
                    AddCardLayoutDesignerMenu(menu.Items);
                    if (ShowQuickClearFilterInHeaderMenu)
                    {
                        var colFilter = _filters.Get(col.AspectName);
                        if (colFilter != null)
                            menu.Items.Add(ViewGridText.ClearColumnFilter, null, (_,__) => { _filters.Clear(col.AspectName); BuildViewIndex(); Invalidate(); QueueAutoSaveUserLayout(); });
                        if (HasActiveFilters)
                            menu.Items.Add(ViewGridText.ClearAllFilters, null, (_,__) => ClearFilters());
                    }
                    menu.Items.Add(new ToolStripSeparator());
                }
                if (ShowHeaderMenuSortItems && IsMenuGroupVisible(ViewGridMenuGroups.Sort, true))
                {
                    AddSortMenu(menu.Items, col);
                    menu.Items.Add(new ToolStripSeparator());
                }
                if (ShowHeaderMenuFreezeItems && IsMenuGroupVisible(ViewGridMenuGroups.Freeze, true))
                {
                    AddFreezeMenu(menu.Items, col);
                }
                if (ShowHeaderMenuAutoSizeItems && IsMenuGroupVisible(ViewGridMenuGroups.AutoSize, true))
                {
                    AddAutoSizeMenu(menu.Items, col);
                }
                if (ShowHeaderMenuLayoutItems && IsMenuGroupVisible(ViewGridMenuGroups.Layout, true))
                {
                    AddLayoutMenu(menu.Items);
                }
                if (ShowHeaderMenuFreezeItems || ShowHeaderMenuAutoSizeItems || ShowHeaderMenuLayoutItems)
                    menu.Items.Add(new ToolStripSeparator());
                if (ShowHeaderMenuGroupingItems && IsMenuGroupVisible(ViewGridMenuGroups.Grouping, true))
                {
                    AddGroupingMenu(menu.Items, col);
                }
                if (ShowViewModeMenuItems && IsMenuGroupVisible(ViewGridMenuGroups.ViewMode, true)) AddViewModeMenu(menu);
                if ((IsMenuGroupVisible(ViewGridMenuGroups.Scenario, true) && ShowScenarioMenuItems) || (IsMenuGroupVisible(ViewGridMenuGroups.State, true) && ShowStateMenuItems))
                {
                    menu.Items.Add(new ToolStripSeparator());
                    if (IsMenuGroupVisible(ViewGridMenuGroups.Scenario, true) && ShowScenarioMenuItems) AddScenarioMenu(menu.Items);
                    if (IsMenuGroupVisible(ViewGridMenuGroups.State, true) && ShowStateMenuItems) AddStateMenu(menu.Items);
                }
                if (ShowHeaderMenuColumnChooserItem && IsMenuGroupVisible(ViewGridMenuGroups.ColumnChooser, true))
                {
                    menu.Items.Add(new ToolStripSeparator());
                    AddColumnChooserMenu(menu.Items);
                }
            }
        }
        else
        {
            int rowIndex = HitRow(location.Y);
            if (IsGroupRow(rowIndex))
            {
                AddGroupHeaderContextMenuItems(menu, rowIndex);
            }
            else
            {
            if (rowIndex >= 0 && !_selectedRows.Contains(rowIndex)) SelectRow(rowIndex);
            if (IsMenuGroupVisible(ViewGridMenuGroups.Clipboard, false))
            {
                menu.Items.Add(ViewGridText.Copy, null, (_,__) => CopySelectionToClipboard()).Enabled = EnableClipboard && SelectedObject != null;
                menu.Items.Add(ViewGridText.CopyCell, null, (_,__) => CopySelectedCellToClipboard()).Enabled = EnableClipboard && SelectedObject != null;
                menu.Items.Add(ViewGridText.CopySelectionJson, null, (_,__) => CopySelectionAsJsonToClipboard()).Enabled = EnableClipboard && SelectedObject != null;
                if (IsMenuGroupVisible(ViewGridMenuGroups.Analytics, false))
                    menu.Items.Add(ViewGridText.CopyMiniAnalytics, null, (_,__) => CopyMiniAnalyticsToClipboard()).Enabled = EnableClipboard && ViewCount > 0;
                menu.Items.Add(ViewGridText.Paste, null, (_,__) => PasteTextToSelectedCell(Clipboard.GetText())).Enabled = EnableClipboard && SelectedObject != null && MenuProfile != ViewGridMenuProfile.ReadOnly;
            }
            if (IsMenuGroupVisible(ViewGridMenuGroups.Editing, false) && MenuProfile != ViewGridMenuProfile.ReadOnly)
                AddEditCellMenuItem(menu.Items);
            menu.Items.Add(new ToolStripSeparator());
            if (IsMenuGroupVisible(ViewGridMenuGroups.Filter, false))
                menu.Items.Add(ViewGridText.ClearFilters, null, (_,__) => ClearFilters()).Enabled = HasActiveFilters;
            if (ShowHeaderMenuFilterStyleItems && ShowFilterStyleSelectorInContextMenu && IsMenuGroupVisible(ViewGridMenuGroups.FilterStyle, false))
                AddFilterStyleMenu(menu);
            if (ShowAdvancedFilterMenuItems) AddAdvancedFilterMenu(menu.Items);
                    AddCardLayoutDesignerMenu(menu.Items);
            if (ShowViewModeMenuItems && IsMenuGroupVisible(ViewGridMenuGroups.ViewMode, false)) AddViewModeMenu(menu);
            if ((IsMenuGroupVisible(ViewGridMenuGroups.Scenario, false) && ShowScenarioMenuItems) || (IsMenuGroupVisible(ViewGridMenuGroups.State, false) && ShowStateMenuItems))
            {
                if (IsMenuGroupVisible(ViewGridMenuGroups.Scenario, false) && ShowScenarioMenuItems) AddScenarioMenu(menu.Items);
                if (IsMenuGroupVisible(ViewGridMenuGroups.State, false) && ShowStateMenuItems) AddStateMenu(menu.Items);
            }
            if (IsMenuGroupVisible(ViewGridMenuGroups.AutoSize, false))
                AddAutoSizeMenu(menu.Items, null);
            if (IsMenuGroupVisible(ViewGridMenuGroups.Layout, false))
                AddLayoutMenu(menu.Items);
            if (IsMenuGroupVisible(ViewGridMenuGroups.Grouping, false))
                AddGroupingMenu(menu.Items, null);
            if (IsMenuGroupVisible(ViewGridMenuGroups.ColumnChooser, false)) AddColumnChooserMenu(menu.Items);
            if (ShowHeaderMenuThemeItems && IsMenuGroupVisible(ViewGridMenuGroups.Theme, false)) AddThemeMenu(menu.Items);
            if (IsMenuGroupVisible(ViewGridMenuGroups.Analytics, false) && !IsMenuGroupVisible(ViewGridMenuGroups.Clipboard, false))
                menu.Items.Add(ViewGridText.CopyMiniAnalytics, null, (_,__) => CopyMiniAnalyticsToClipboard()).Enabled = EnableClipboard && ViewCount > 0;
            menu.Items.Add(new ToolStripSeparator());
            if (EnableRowDetails && IsMenuGroupVisible(ViewGridMenuGroups.RowDetails, false))
                menu.Items.Add(ViewGridText.RowDetails, null, (_,__) => ToggleRowDetails(_selectedRow)).Enabled = SelectedObject != null;
            }
        }
        ApplyMenuCustomizationV275(menu);
        RemoveRedundantMenuSeparators(menu);
        if (menu.Items.Count == 0)
        {
            try { menu.Dispose(); } catch { }
            return;
        }
        AttachFilterPopupDismissHandlers(menu, filterLauncherItem);
        ApplyMenuIcons(menu.Items);
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(menu, _theme);
        menu.Show(this, location);
    }


    private void MergeBuiltInMenuIntoUserContextMenu(Point location)
    {
        if (ContextMenuStrip == null) return;
        if (MenuProfile == ViewGridMenuProfile.None) return;
        bool isHeaderMenu = ShowHeader && location.Y < HeaderHeight;
        if (isHeaderMenu && !UseBuiltInHeaderMenu) return;
        if (!isHeaderMenu && !UseBuiltInBodyMenu) return;
        AttachBuiltInMenuTo(ContextMenuStrip, location);
    }

    public void AttachBuiltInMenuTo(ContextMenuStrip userMenu, Point? clientLocation = null, string? menuText = null)
    {
        if (userMenu == null || userMenu.IsDisposed) return;

        const string mergeTag = "__ViewGridBuiltInMergedMenu__";
        RemoveMergedViewGridMenuItems(userMenu.Items, mergeTag);

        var text = string.IsNullOrWhiteSpace(menuText)
            ? (string.IsNullOrWhiteSpace(BuiltInMenuMergeText) ? ViewGridText.BuiltInMenuMergeText : BuiltInMenuMergeText.Trim())
            : menuText.Trim();

        var location = clientLocation ?? PointToClient(Cursor.Position);
        var mergedItems = CreateViewGridMergedMenuItems(text, location, mergeTag);
        if (mergedItems.Count == 0) return;

        int insertIndex = GetMergeInsertIndex(userMenu.Items);
        bool hasUserItems = userMenu.Items.Count > 0;
        bool separatorBefore = BuiltInMenuMergeSeparator && hasUserItems && insertIndex > 0;
        bool separatorAfter = BuiltInMenuMergeSeparator && hasUserItems && insertIndex == 0;

        if (separatorBefore)
        {
            userMenu.Items.Insert(insertIndex, new ToolStripSeparator { Tag = mergeTag });
            insertIndex++;
        }

        foreach (var item in mergedItems)
        {
            item.Tag = mergeTag;
            userMenu.Items.Insert(insertIndex, item);
            insertIndex++;
        }

        if (separatorAfter)
        {
            userMenu.Items.Insert(insertIndex, new ToolStripSeparator { Tag = mergeTag });
        }

        ApplyMenuCustomizationV275(userMenu.Items);
        RemoveRedundantMenuSeparators(userMenu.Items);
        if (ApplyIconsToMergedUserMenus) ApplyMenuIcons(userMenu.Items);
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(userMenu, _theme);
    }

    private static void RemoveMergedViewGridMenuItems(ToolStripItemCollection items, string mergeTag)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (Equals(items[i].Tag, mergeTag))
            {
                var old = items[i];
                items.RemoveAt(i);
                try { old.Dispose(); } catch { }
            }
        }
    }

    private int GetMergeInsertIndex(ToolStripItemCollection items)
    {
        if (items.Count == 0) return 0;

        switch (BuiltInMenuMergePlacement)
        {
            case ViewGridMenuMergePlacement.Top:
                return 0;
            case ViewGridMenuMergePlacement.BeforeFirstSeparator:
                for (int i = 0; i < items.Count; i++)
                    if (items[i] is ToolStripSeparator) return i;
                return items.Count;
            case ViewGridMenuMergePlacement.AfterFirstSeparator:
                for (int i = 0; i < items.Count; i++)
                    if (items[i] is ToolStripSeparator) return Math.Min(items.Count, i + 1);
                return items.Count;
            case ViewGridMenuMergePlacement.Bottom:
            default:
                return items.Count;
        }
    }

    private List<ToolStripItem> CreateViewGridMergedMenuItems(string text, Point location, string mergeTag)
    {
        var result = new List<ToolStripItem>();

        if (BuiltInMenuMergePresentation == ViewGridMenuMergePresentation.SubMenu)
        {
            var root = CreateViewGridMergedMenu(text, location);
            if (root.DropDownItems.Count > 0)
                result.Add(root);
            return result;
        }

        foreach (var group in GetMergedMenuGroupOrder())
        {
            if (!IsMergedMenuGroupVisible(group, location)) continue;

            if (BuiltInMenuMergePresentation == ViewGridMenuMergePresentation.GroupedSubMenus)
            {
                var groupMenu = CreateViewGridMergedGroupMenu(group, location);
                if (groupMenu.DropDownItems.Count > 0)
                    result.Add(groupMenu);
            }
            else
            {
                AddMergedGroupItems(result, group, location, mergeTag);
            }
        }

        RemoveRedundantMenuSeparators(result);
        foreach (var item in result) item.Tag = mergeTag;
        return result;
    }

    private IEnumerable<ViewGridMenuGroups> GetMergedMenuGroupOrder()
    {
        var used = new HashSet<ViewGridMenuGroups>();
        foreach (var token in (MergedMenuGroupOrder ?? string.Empty).Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (Enum.TryParse(token.Trim(), true, out ViewGridMenuGroups group) && group != ViewGridMenuGroups.None && group != ViewGridMenuGroups.All && used.Add(group))
                yield return group;
        }

        ViewGridMenuGroups[] defaults =
        {
            ViewGridMenuGroups.Mode,
            ViewGridMenuGroups.ViewMode,
            ViewGridMenuGroups.Filter,
            ViewGridMenuGroups.Sort,
            ViewGridMenuGroups.ColumnChooser,
            ViewGridMenuGroups.Clipboard,
            ViewGridMenuGroups.AutoSize,
            ViewGridMenuGroups.Freeze,
            ViewGridMenuGroups.Layout,
            ViewGridMenuGroups.Grouping,
            ViewGridMenuGroups.Editing,
            ViewGridMenuGroups.RowDetails,
            ViewGridMenuGroups.Analytics,
            ViewGridMenuGroups.FilterStyle,
            ViewGridMenuGroups.Theme
        };

        foreach (var group in defaults)
            if (used.Add(group))
                yield return group;
    }

    private bool IsMergedMenuGroupVisible(ViewGridMenuGroups group, Point location)
    {
        if ((MergedMenuGroups & group) != group) return false;
        bool header = ShowHeader && location.Y < HeaderHeight;
        return IsMenuGroupVisible(group, header);
    }

    private ToolStripMenuItem CreateViewGridMergedGroupMenu(ViewGridMenuGroups group, Point location)
    {
        var menu = new ToolStripMenuItem(GetMergedGroupText(group));
        var items = new List<ToolStripItem>();
        AddMergedGroupItems(items, group, location, null);
        foreach (var item in items) menu.DropDownItems.Add(item);
        RemoveRedundantMenuSeparators(menu.DropDownItems);
        ApplyMenuIcons(menu.DropDownItems);
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(menu.DropDown, _theme);
        return menu;
    }

    private string GetMergedGroupText(ViewGridMenuGroups group)
    {
        return group switch
        {
            ViewGridMenuGroups.Filter => ViewGridText.Filtering,
            ViewGridMenuGroups.Sort => ViewGridText.Sorting,
            ViewGridMenuGroups.Freeze => ViewGridText.Freeze,
            ViewGridMenuGroups.AutoSize => ViewGridText.ColumnWidth,
            ViewGridMenuGroups.Layout => ViewGridText.Layout,
            ViewGridMenuGroups.Grouping => ViewGridText.Grouping,
            ViewGridMenuGroups.ColumnChooser => ViewGridText.Columns,
            ViewGridMenuGroups.Mode => ViewGridText.Mode,
            ViewGridMenuGroups.ViewMode => ViewGridText.View,
            ViewGridMenuGroups.FilterStyle => ViewGridText.FilterStyle,
            ViewGridMenuGroups.Theme => ViewGridText.Theme,
            ViewGridMenuGroups.Clipboard => ViewGridText.Clipboard,
            ViewGridMenuGroups.Editing => ViewGridText.Editing,
            ViewGridMenuGroups.RowDetails => ViewGridText.RowDetails,
            ViewGridMenuGroups.Analytics => ViewGridText.Analytics,
            _ => group.ToString()
        };
    }

    private void AddMergedGroupItems(List<ToolStripItem> result, ViewGridMenuGroups group, Point location, object? tag)
    {
        using var temp = new ContextMenuStrip();
        AddMergedGroupItems(temp.Items, group, location);
        RemoveRedundantMenuSeparators(temp.Items);
        while (temp.Items.Count > 0)
        {
            var item = temp.Items[0];
            temp.Items.RemoveAt(0);
            if (tag != null) item.Tag = tag;
            result.Add(item);
        }
    }

    private void AddMergedGroupItems(ToolStripItemCollection items, ViewGridMenuGroups group, Point location)
    {
        bool header = ShowHeader && location.Y < HeaderHeight;
        var col = header ? HitColumn(location.X) : null;

        switch (group)
        {
            case ViewGridMenuGroups.Mode:
                if (ShowHeaderMenuModeItems) AddModeMenu(items);
                break;
            case ViewGridMenuGroups.ViewMode:
                if (ShowHeaderMenuViewModeItems) AddViewModeMenuItems(items);
                break;
            case ViewGridMenuGroups.Filter:
                if (col != null && ShowHeaderMenuFilterItems && ShowFilterMenu && col.Filterable)
                {
                    if (FilterMenuMode != ViewGridFilterMenuMode.ModalWindow || UseEmbeddedHeaderFilterMenu)
                        items.Add(CreateEmbeddedFilterMenuItem(col));
                    items.Add(ViewGridText.FilterWindow(col.Header), null, (_,__) => ShowFilterMenuForColumn(col, location)).Enabled = true;
                    if (ShowQuickClearFilterInHeaderMenu)
                    {
                        if (_filters.Get(col.AspectName) != null)
                            items.Add(ViewGridText.ClearColumnFilter, null, (_,__) => { _filters.Clear(col.AspectName); BuildViewIndex(); Invalidate(); QueueAutoSaveUserLayout(); });
                        if (HasActiveFilters)
                            items.Add(ViewGridText.ClearAllFilters, null, (_,__) => ClearFilters());
                    }
                }
                else
                {
                    items.Add(ViewGridText.ClearFilters, null, (_,__) => ClearFilters()).Enabled = HasActiveFilters;
                }
                break;
            case ViewGridMenuGroups.FilterStyle:
                if (ShowHeaderMenuFilterStyleItems && ShowFilterStyleSelectorInContextMenu) AddFilterStyleMenu(items);
                if (ShowAdvancedFilterMenuItems) AddAdvancedFilterMenu(items);
                AddCardLayoutDesignerMenu(items);
                break;
            case ViewGridMenuGroups.Sort:
                if (col != null && ShowHeaderMenuSortItems) AddSortMenu(items, col);
                break;
            case ViewGridMenuGroups.Freeze:
                if (col != null && ShowHeaderMenuFreezeItems) AddFreezeMenu(items, col);
                break;
            case ViewGridMenuGroups.AutoSize:
                if (ShowHeaderMenuAutoSizeItems) AddAutoSizeMenu(items, col);
                break;
            case ViewGridMenuGroups.Layout:
                if (ShowHeaderMenuLayoutItems) AddLayoutMenu(items);
                break;
            case ViewGridMenuGroups.Grouping:
                if (ShowHeaderMenuGroupingItems) AddGroupingMenu(items, col);
                break;
            case ViewGridMenuGroups.ColumnChooser:
                if (ShowHeaderMenuColumnChooserItem) AddColumnChooserMenu(items);
                break;
            case ViewGridMenuGroups.Theme:
                if (ShowHeaderMenuThemeItems) AddThemeMenu(items);
                break;
            case ViewGridMenuGroups.State:
                if (ShowStateMenuItems) AddStateMenu(items);
                break;
            case ViewGridMenuGroups.Scenario:
                if (ShowScenarioMenuItems) AddScenarioMenu(items);
                break;
            case ViewGridMenuGroups.Clipboard:
                items.Add(ViewGridText.Copy, null, (_,__) => CopySelectionToClipboard()).Enabled = EnableClipboard && SelectedObject != null;
                items.Add(ViewGridText.CopyCell, null, (_,__) => CopySelectedCellToClipboard()).Enabled = EnableClipboard && SelectedObject != null;
                items.Add(ViewGridText.CopySelectionJson, null, (_,__) => CopySelectionAsJsonToClipboard()).Enabled = EnableClipboard && SelectedObject != null;
                items.Add(ViewGridText.Paste, null, (_,__) => PasteTextToSelectedCell(Clipboard.GetText())).Enabled = EnableClipboard && SelectedObject != null && MenuProfile != ViewGridMenuProfile.ReadOnly;
                break;
            case ViewGridMenuGroups.Editing:
                if (MenuProfile != ViewGridMenuProfile.ReadOnly)
                    AddEditCellMenuItem(items);
                break;
            case ViewGridMenuGroups.RowDetails:
                if (EnableRowDetails)
                    items.Add(ViewGridText.RowDetails, null, (_,__) => ToggleRowDetails(_selectedRow)).Enabled = SelectedObject != null;
                break;
            case ViewGridMenuGroups.Analytics:
                items.Add(ViewGridText.CopyMiniAnalytics, null, (_,__) => CopyMiniAnalyticsToClipboard()).Enabled = EnableClipboard && ViewCount > 0;
                break;
        }
    }

    private void AddEditCellMenuItem(ToolStripItemCollection items)
    {
        if (!ShowEditCellMenuItem) return;

        var item = new ToolStripMenuItem(ResolveEditCellMenuText(), null, (_, __) => ExecuteEditCellMenuAction())
        {
            Enabled = EnableCellEditing && SelectedObject != null
        };

        string shortcutText = ResolveEditCellMenuShortcutText();
        if (!string.IsNullOrWhiteSpace(shortcutText))
            item.ShortcutKeyDisplayString = shortcutText;

        items.Add(item);
    }

    private string ResolveEditCellMenuText()
    {
        return string.IsNullOrWhiteSpace(EditCellMenuText) ? ViewGridText.EditCell : EditCellMenuText.Trim();
    }

    private string ResolveEditCellMenuShortcutText()
    {
        if (!string.IsNullOrWhiteSpace(EditCellMenuShortcutText)) return EditCellMenuShortcutText.Trim();
        if (CellEditActivationKey == Keys.None) return string.Empty;
        return new KeysConverter().ConvertToString(CellEditActivationKey) ?? string.Empty;
    }

    private void ExecuteEditCellMenuAction()
    {
        if (!EnableCellEditing || SelectedObject == null) return;

        var handler = EditCellMenuRequested;
        if (handler != null)
        {
            handler(this, EventArgs.Empty);
            return;
        }

        BeginEditSelectedCell();
    }

    public void AttachBuiltInHeaderMenuTo(ContextMenuStrip userMenu, ViewGridColumn? column = null, string? menuText = null)
    {
        if (userMenu == null || userMenu.IsDisposed) return;
        var col = column ?? HitColumn(PointToClient(Cursor.Position).X) ?? Columns.FirstOrDefault();
        var location = col == null ? Point.Empty : new Point(Math.Max(0, GetColumnLeft(col)), Math.Max(0, HeaderHeight - 1));
        AttachBuiltInMenuTo(userMenu, location, menuText ?? ViewGridText.BuiltInHeaderMenu);
    }

    public void AttachBuiltInHeaderMenuTo(ContextMenuStrip userMenu, string aspectName, string? menuText = null)
    {
        var col = Columns.FirstOrDefault(c => string.Equals(c.AspectName, aspectName, StringComparison.OrdinalIgnoreCase));
        AttachBuiltInHeaderMenuTo(userMenu, col, menuText);
    }

    private ToolStripMenuItem CreateViewGridMergedMenu(string text, Point location)
    {
        var root = new ToolStripMenuItem(text);
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(root.DropDown, _theme);

        foreach (var group in GetMergedMenuGroupOrder())
        {
            if (!IsMergedMenuGroupVisible(group, location)) continue;

            int before = root.DropDownItems.Count;
            AddMergedGroupItems(root.DropDownItems, group, location);
            if (root.DropDownItems.Count > before)
                root.DropDownItems.Add(new ToolStripSeparator());
        }

        RemoveRedundantMenuSeparators(root.DropDownItems);
        ApplyMenuIcons(root.DropDownItems);
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(root.DropDown, _theme);
        return root;
    }

    private static void RemoveRedundantMenuSeparators(ToolStripItemCollection items)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            bool firstOrLast = i == 0 || i == items.Count - 1;
            bool duplicate = i > 0 && items[i] is ToolStripSeparator && items[i - 1] is ToolStripSeparator;
            if (items[i] is ToolStripSeparator && (firstOrLast || duplicate))
                items.RemoveAt(i);
        }
    }

    private static void RemoveRedundantMenuSeparators(List<ToolStripItem> items)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            bool firstOrLast = i == 0 || i == items.Count - 1;
            bool duplicate = i > 0 && items[i] is ToolStripSeparator && items[i - 1] is ToolStripSeparator;
            if (items[i] is ToolStripSeparator && (firstOrLast || duplicate))
                items.RemoveAt(i);
        }
    }

    private void AddViewModeMenuItems(ToolStripItemCollection items)
    {
        items.Add(CreateViewModeMenu());
    }

    private void AddViewModeMenuItem(ToolStripItemCollection items, ViewGridMode mode)
    {
        AddCheckedMenuItem(items, GetViewModeDisplayName(mode), ViewMode == mode, (_,__) =>
        {
            SetViewMode(mode);
            CloseActiveMenusSafe();
        });
    }

    private ToolStripMenuItem CreateViewModeMenu()
    {
        var viewMenu = new ToolStripMenuItem(ViewGridText.ViewMode)
        {
            ToolTipText = "ViewGrid görünümünü tablo, kart, medya, dashboard veya workflow senaryosuna göre değiştirir."
        };

        var activeInfo = new ToolStripMenuItem($"Aktif: {GetViewModeDisplayName(ViewMode)}")
        {
            Enabled = false,
            ToolTipText = GetPerformanceSummary()
        };
        viewMenu.DropDownItems.Add(activeInfo);
        viewMenu.DropDownItems.Add(new ToolStripSeparator());

        var presets = new ToolStripMenuItem("Akıllı presetler");
        presets.Image = CreateMenuDotIcon(_theme.AccentColor);
        AddPresetMenuItem(presets.DropDownItems, "Audix medya vitrini", "Poster, kapak, play overlay ve medya cache ayarlarını birlikte uygular.", () =>
        {
            ApplyAudix51MediaPilotDefaults();
            RefreshView();
        });
        AddPresetMenuItem(presets.DropDownItems, "KPI dashboard", "Metrik kartları ve dashboard builder ayarlarını açar.", () =>
        {
            ApplyV40AnalyticsProfile(ViewGridV40AnalyticsPreset.KpiDashboard);
        });
        AddPresetMenuItem(presets.DropDownItems, "Factory heatmap", "Üretim/fabrika takibi için heatmap ve factory overlay profilini uygular.", () =>
        {
            ApplyV40AnalyticsProfile(ViewGridV40AnalyticsPreset.FactoryOverview);
        });
        AddPresetMenuItem(presets.DropDownItems, "Timeline akışı", "Olay, iş emri veya ticket zaman akışı görünümüne geçer.", () =>
        {
            ApplyV40AnalyticsProfile(ViewGridV40AnalyticsPreset.Timeline);
        });
        AddPresetMenuItem(presets.DropDownItems, "Performance: medya cache", "Lazy image, medya cache ve paint ölçümünü açar.", () =>
        {
            EnablePaintPerformanceMetrics = true;
            ApplyV38PerformanceProfile(ViewGridV38PerformancePreset.MediaLibrary);
        });
        viewMenu.DropDownItems.Add(presets);

        var mediaPresets = new ToolStripMenuItem("Media Smart Preset");
        mediaPresets.Image = CreateMenuDotIcon(Color.FromArgb(88, 156, 255));
        AddMediaPresetMenuItem(mediaPresets.DropDownItems, "Müzik", ViewGridMediaSmartPreset.Music, "Albüm kapağı, now playing ve equalizer odaklı MediaTile.");
        AddMediaPresetMenuItem(mediaPresets.DropDownItems, "Film", ViewGridMediaSmartPreset.Movie, "Afiş/poster, kalite rozeti ve playback overlay odaklı görünüm.");
        AddMediaPresetMenuItem(mediaPresets.DropDownItems, "Fotoğraf", ViewGridMediaSmartPreset.Photo, "Galeri, cover ölçekleme ve görsel katalog modu.");
        AddMediaPresetMenuItem(mediaPresets.DropDownItems, "Doküman", ViewGridMediaSmartPreset.Document, "PDF/doküman önizleme için contain ölçekli DetailCard.");
        viewMenu.DropDownItems.Add(mediaPresets);

        var dashboard = new ToolStripMenuItem("Dashboard preset editor");
        dashboard.Image = CreateMenuDotIcon(Color.FromArgb(92, 200, 128));
        AddDashboardWidgetMenuItem(dashboard.DropDownItems, "KPI", ViewGridDashboardWidgetKind.Kpi, ViewGridMode.KpiDashboard);
        AddDashboardWidgetMenuItem(dashboard.DropDownItems, "HeatMap", ViewGridDashboardWidgetKind.HeatMap, ViewGridMode.HeatMap);
        AddDashboardWidgetMenuItem(dashboard.DropDownItems, "MiniChart", ViewGridDashboardWidgetKind.Chart, ViewGridMode.MiniChart);
        dashboard.DropDownItems.Add(new ToolStripSeparator());
        dashboard.DropDownItems.Add("Performans özeti", null, (_, __) =>
        {
            MessageBox.Show(FindForm(), GetPerformanceSummary(), "ViewGrid Performance", MessageBoxButtons.OK, MessageBoxIcon.Information);
            CloseActiveMenusSafe();
        });
        dashboard.DropDownItems.Add("Diagnostics Center", CreateMenuDotIcon(Color.FromArgb(120, 155, 255)), (_, __) =>
        {
            ShowViewGridDiagnosticsCenter();
            CloseActiveMenusSafe();
        });
        viewMenu.DropDownItems.Add(dashboard);
        viewMenu.DropDownItems.Add(new ToolStripSeparator());

        var memoryItem = AddCheckedMenuItem(viewMenu.DropDownItems, "Senaryoya göre görünümü hatırla", RememberViewModePerScenario, (_, __) =>
        {
            RememberViewModePerScenario = !RememberViewModePerScenario;
            if (RememberViewModePerScenario)
                RememberCurrentViewModeForActiveScenario();
            CloseActiveMenusSafe();
        });
        memoryItem.Image = CreateMenuDotIcon(Color.FromArgb(170, 170, 220));
        memoryItem.ToolTipText = "Aktif senaryo için son seçilen ViewMode değerini runtime boyunca saklar.";
        viewMenu.DropDownItems.Add(new ToolStripSeparator());

        AddViewModeGroup(viewMenu.DropDownItems, "Tablo ve liste", "Yoğun veri, klasik tablo ve sade liste görünümleri.",
            ViewGridMode.Details,
            ViewGridMode.DenseList,
            ViewGridMode.List,
            ViewGridMode.GroupedList);

        AddViewModeGroup(viewMenu.DropDownItems, "Kart ve detay", "Ticket, ürün, master/detail ve okunabilir kart görünümleri.",
            ViewGridMode.Tile,
            ViewGridMode.LargeCard,
            ViewGridMode.RowCard,
            ViewGridMode.RowPreview,
            ViewGridMode.DetailCard,
            ViewGridMode.PropertyCard,
            ViewGridMode.MasterDetail);

        AddViewModeGroup(viewMenu.DropDownItems, "Medya ve galeri", "Müzik, film, resim, kapak ve katalog senaryoları.",
            ViewGridMode.Poster,
            ViewGridMode.Gallery,
            ViewGridMode.MediaTile,
            ViewGridMode.FilmStrip,
            ViewGridMode.IconGrid,
            ViewGridMode.ExtraLargeIcons,
            ViewGridMode.LargeIcons,
            ViewGridMode.MediumIcons);

        AddViewModeGroup(viewMenu.DropDownItems, "Dashboard ve analiz", "KPI, dashboard, heatmap ve mini trend görünümleri.",
            ViewGridMode.DashboardCard,
            ViewGridMode.KpiDashboard,
            ViewGridMode.HeatMap,
            ViewGridMode.MiniChart);

        AddViewModeGroup(viewMenu.DropDownItems, "Workflow", "Kanban, timeline ve grup kart akışları.",
            ViewGridMode.Kanban,
            ViewGridMode.Timeline,
            ViewGridMode.GroupCard);

        RemoveRedundantMenuSeparators(viewMenu.DropDownItems);
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(viewMenu.DropDown, _theme);
        return viewMenu;
    }

    private void AddMediaPresetMenuItem(ToolStripItemCollection items, string text, ViewGridMediaSmartPreset preset, string tooltip)
    {
        var item = AddCheckedMenuItem(items, text, MediaSmartPreset == preset, (_, __) =>
        {
            ApplyMediaSmartPreset(preset);
            CloseActiveMenusSafe();
        });
        item.Image = CreateMenuDotIcon(preset switch
        {
            ViewGridMediaSmartPreset.Music => Color.FromArgb(120, 155, 255),
            ViewGridMediaSmartPreset.Movie => Color.FromArgb(180, 120, 255),
            ViewGridMediaSmartPreset.Photo => Color.FromArgb(90, 190, 150),
            ViewGridMediaSmartPreset.Document => Color.FromArgb(220, 170, 80),
            _ => _theme.AccentColor
        });
        item.ToolTipText = tooltip;
    }

    private void AddDashboardWidgetMenuItem(ToolStripItemCollection items, string text, ViewGridDashboardWidgetKind kind, ViewGridMode viewMode)
    {
        var item = AddCheckedMenuItem(items, text, IsDashboardWidgetEnabled(kind), (_, __) =>
        {
            SetDashboardWidgetEnabled(kind, !IsDashboardWidgetEnabled(kind));
            SetViewMode(viewMode);
            CloseActiveMenusSafe();
        });
        item.Image = CreateMenuDotIcon(viewMode switch
        {
            ViewGridMode.KpiDashboard => Color.FromArgb(92, 200, 128),
            ViewGridMode.HeatMap => Color.FromArgb(230, 120, 80),
            ViewGridMode.MiniChart => Color.FromArgb(90, 170, 230),
            _ => _theme.AccentColor
        });
        item.ToolTipText = $"{text} dashboard bileşenini aç/kapat ve ilgili görünüme geç.";
    }

    private Image CreateMenuDotIcon(Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var fill = new SolidBrush(color);
        using var border = new Pen(EnsureReadableTextOn(color, _theme.ForeColor), 1f);
        g.FillEllipse(fill, 3, 3, 10, 10);
        g.DrawEllipse(border, 3, 3, 10, 10);
        return bmp;
    }

    private void AddViewModeGroup(ToolStripItemCollection items, string text, string tooltip, params ViewGridMode[] modes)
    {
        var group = new ToolStripMenuItem(text) { ToolTipText = tooltip };
        foreach (var mode in modes)
            AddViewModeMenuItem(group.DropDownItems, mode);

        if (group.DropDownItems.Count == 0) return;
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(group.DropDown, _theme);
        items.Add(group);
    }

    private void AddPresetMenuItem(ToolStripItemCollection items, string text, string tooltip, Action action)
    {
        var item = new ToolStripMenuItem(text)
        {
            ToolTipText = tooltip
        };
        item.Click += (_, __) =>
        {
            action();
            CloseActiveMenusSafe();
        };
        items.Add(item);
    }

    private void AddModeMenu(ToolStripItemCollection items)
    {
        var modeMenu = new ToolStripMenuItem(ViewGridText.Mode);
        foreach (ViewGridDataMode m in Enum.GetValues(typeof(ViewGridDataMode)))
        {
            var local = m;
            AddCheckedMenuItem(modeMenu.DropDownItems, ViewGridText.ViewGridDataModeName(local.ToString()), Mode == local, (_,__) => { Mode = local; RefreshView(); CloseActiveMenusSafe(); });
        }
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(modeMenu.DropDown, _theme);
        items.Add(modeMenu);
    }

    private void AddThemeMenu(ToolStripItemCollection items)
    {
        var themeMenu = new ToolStripMenuItem(ViewGridText.Theme);
        foreach (ViewGridThemePreset p in Enum.GetValues(typeof(ViewGridThemePreset)))
        {
            var local = p;
            AddCheckedMenuItem(themeMenu.DropDownItems, ViewGridText.ThemePresetName(local.ToString()), ThemePreset == local, (_,__) => { ThemePreset = local; RefreshView(); CloseActiveMenusSafe(); });
        }
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(themeMenu.DropDown, _theme);
        items.Add(themeMenu);
    }


    private bool IsGroupContextMenuVisible =>
        ShowGroupHeaderContextMenu &&
        EnableGrouping &&
        !string.IsNullOrWhiteSpace(_groupByAspectName) &&
        (ShowGroupMenuToggleThisGroupItem || ShowGroupMenuCollapseAllItem || ShowGroupMenuExpandAllItem || ShowGroupMenuOnlyThisGroupItem || ShowGroupMenuClearGroupingItem);

    private void AddGroupHeaderContextMenuItems(ContextMenuStrip menu, int rowIndex)
    {
        if (!IsGroupContextMenuVisible || !IsGroupRow(rowIndex)) return;

        string groupKey = GetGroupKey(rowIndex);
        bool collapsed = IsGroupCollapsed(rowIndex);

        if (ShowGroupMenuToggleThisGroupItem)
            menu.Items.Add(collapsed ? ViewGridText.ExpandThisGroup : ViewGridText.CollapseThisGroup, null, (_, __) => ToggleGroup(groupKey));

        if (ShowGroupMenuOnlyThisGroupItem)
            menu.Items.Add(ViewGridText.ShowOnlyThisGroup, null, (_, __) => ShowOnlyGroup(groupKey));

        if (ShowGroupMenuToggleThisGroupItem || ShowGroupMenuOnlyThisGroupItem)
            menu.Items.Add(new ToolStripSeparator());

        if (ShowGroupMenuCollapseAllItem)
            menu.Items.Add(ViewGridText.CollapseAllGroups, null, (_, __) => CollapseAllGroups()).Enabled = EnableGrouping;

        if (ShowGroupMenuExpandAllItem)
            menu.Items.Add(ViewGridText.ExpandAllGroups, null, (_, __) => ExpandAllGroups()).Enabled = EnableGrouping;

        if ((ShowGroupMenuCollapseAllItem || ShowGroupMenuExpandAllItem) && ShowGroupMenuClearGroupingItem)
            menu.Items.Add(new ToolStripSeparator());

        if (ShowGroupMenuClearGroupingItem)
            menu.Items.Add(ViewGridText.ClearGrouping, null, (_, __) => ClearGrouping()).Enabled = EnableGrouping;
    }


    private void AddSortMenu(ToolStripItemCollection items, ViewGridColumn? col)
    {
        var root = new ToolStripMenuItem(ViewGridText.Sorting);
        if (col != null)
        {
            root.DropDownItems.Add(ViewGridText.SortAscending, null, (_,__) => { SortBy(col, false); }).Enabled = col.Sortable;
            root.DropDownItems.Add(ViewGridText.SortDescending, null, (_,__) => { SortBy(col, true); }).Enabled = col.Sortable;
            root.DropDownItems.Add(ViewGridText.UnsortColumn, null, (_,__) => ClearSort(col)).Enabled = _sortColumn == col;
            root.DropDownItems.Add(new ToolStripSeparator());
        }
        root.DropDownItems.Add(ViewGridText.ClearSort, null, (_,__) => ClearSort(null)).Enabled = _sortColumn != null;
        RemoveRedundantMenuSeparators(root.DropDownItems);
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(root.DropDown, _theme);
        items.Add(root);
    }

    private void AddFreezeMenu(ToolStripItemCollection items, ViewGridColumn? col)
    {
        var root = new ToolStripMenuItem(ViewGridText.FrozenColumn);
        if (col != null)
            root.DropDownItems.Add(ViewGridText.FreezeColumn, null, (_,__) => { FrozenColumnCount = Math.Max(FrozenColumnCount, Columns.VisibleColumns.ToList().IndexOf(col) + 1); RefreshView(); });
        root.DropDownItems.Add(ViewGridText.ClearFrozenColumns, null, (_,__) => { FrozenColumnCount = 0; RefreshView(); }).Enabled = FrozenColumnCount > 0;
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(root.DropDown, _theme);
        items.Add(root);
    }

    private void AddAutoSizeMenu(ToolStripItemCollection items, ViewGridColumn? col)
    {
        var root = new ToolStripMenuItem(ViewGridText.ColumnWidth);
        if (col != null)
            root.DropDownItems.Add(ViewGridText.AutoSizeColumn, null, (_,__) => AutoResizeColumn(col));
        root.DropDownItems.Add(ViewGridText.AutoSizeAllColumns, null, (_,__) => AutoResizeAllColumnsToContent());
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(root.DropDown, _theme);
        items.Add(root);
    }

    private void AddLayoutMenu(ToolStripItemCollection items)
    {
        var root = new ToolStripMenuItem(ViewGridText.Layout);
        root.DropDownItems.Add(ViewGridText.SaveColumnLayout, null, (_,__) => SaveColumnLayout());
        root.DropDownItems.Add(ViewGridText.LoadColumnLayout, null, (_,__) => LoadColumnLayout());
        root.DropDownItems.Add(ViewGridText.SaveDefaultLayoutProfile, null, (_,__) => SaveColumnLayoutProfile("Default"));
        var profileMenu = new ToolStripMenuItem(ViewGridText.LayoutProfiles);
        foreach (var profile in GetColumnLayoutProfileNames())
        {
            string p = profile;
            profileMenu.DropDownItems.Add(p, null, (_,__) => LoadColumnLayoutProfile(p));
        }
        profileMenu.Enabled = profileMenu.DropDownItems.Count > 0;
        root.DropDownItems.Add(profileMenu);
        root.DropDownItems.Add(ViewGridText.ResetColumnLayout, null, (_,__) => ResetColumnLayout());
        root.DropDownItems.Add(new ToolStripSeparator());
        root.DropDownItems.Add(ViewGridText.SaveUserLayout, null, (_,__) => SaveUserLayout());
        root.DropDownItems.Add(ViewGridText.LoadUserLayout, null, (_,__) => LoadUserLayout());
        root.DropDownItems.Add(ViewGridText.ResetUserLayout, null, (_,__) => ResetUserLayout());
        root.DropDownItems.Add(ViewGridText.ExportUserLayout, null, (_,__) => ExportUserLayoutWithDialog());
        root.DropDownItems.Add(ViewGridText.ImportUserLayout, null, (_,__) => ImportUserLayoutWithDialog());
        RemoveRedundantMenuSeparators(root.DropDownItems);
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(root.DropDown, _theme);
        items.Add(root);
    }

    private void AddGroupingMenu(ToolStripItemCollection items, ViewGridColumn? col)
    {
        var root = new ToolStripMenuItem(ViewGridText.Grouping);
        if (col != null)
        {
            root.DropDownItems.Add(ViewGridText.GroupByColumn(col.Header), null, (_,__) => SetGroupBy(col.AspectName)).Enabled = col.AllowGroup;
            root.DropDownItems.Add(new ToolStripSeparator());
        }
        root.DropDownItems.Add(ViewGridText.CollapseAllGroups, null, (_,__) => CollapseAllGroups()).Enabled = EnableGrouping;
        root.DropDownItems.Add(ViewGridText.ExpandAllGroups, null, (_,__) => ExpandAllGroups()).Enabled = EnableGrouping;
        root.DropDownItems.Add(ViewGridText.ClearGrouping, null, (_,__) => ClearGrouping()).Enabled = EnableGrouping;
        RemoveRedundantMenuSeparators(root.DropDownItems);
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(root.DropDown, _theme);
        items.Add(root);
    }

    private static void RemoveRedundantMenuSeparators(ContextMenuStrip menu)
    {
        for (int i = menu.Items.Count - 1; i >= 0; i--)
        {
            bool firstOrLast = i == 0 || i == menu.Items.Count - 1;
            bool duplicate = i > 0 && menu.Items[i] is ToolStripSeparator && menu.Items[i - 1] is ToolStripSeparator;
            if (menu.Items[i] is ToolStripSeparator && (firstOrLast || duplicate))
                menu.Items.RemoveAt(i);
        }
    }

    private void AddFilterStyleMenu(ContextMenuStrip menu) => AddFilterStyleMenu(menu.Items);

    private void AddFilterStyleMenu(ToolStripItemCollection items)
    {
        var filterStyleMenu = new ToolStripMenuItem(ViewGridText.FilterStyle);
        filterStyleMenu.DropDown.Renderer = new global::ViewGrid.Theming.SmartMenuRenderer(_theme);
        AddCheckedMenuItem(filterStyleMenu.DropDownItems, ViewGridText.PopupMenu, FilterMenuMode == ViewGridFilterMenuMode.PopupMenu, (_,__) => { FilterMenuMode = ViewGridFilterMenuMode.PopupMenu; QueueAutoSaveUserLayout(); CloseActiveMenusSafe(); });
        AddCheckedMenuItem(filterStyleMenu.DropDownItems, ViewGridText.ModalWindow, FilterMenuMode == ViewGridFilterMenuMode.ModalWindow, (_,__) => { FilterMenuMode = ViewGridFilterMenuMode.ModalWindow; QueueAutoSaveUserLayout(); CloseActiveMenusSafe(); });
        AddCheckedMenuItem(filterStyleMenu.DropDownItems, ViewGridText.Both, FilterMenuMode == ViewGridFilterMenuMode.Both, (_,__) => { FilterMenuMode = ViewGridFilterMenuMode.Both; QueueAutoSaveUserLayout(); CloseActiveMenusSafe(); });
        global::ViewGrid.Theming.SmartMenuRenderer.ApplyTo(filterStyleMenu.DropDown, _theme);
        items.Add(filterStyleMenu);
    }

    private void AddViewModeMenu(ContextMenuStrip menu)
    {
        menu.Items.Add(CreateViewModeMenu());
    }

    private static ToolStripMenuItem AddCheckedMenuItem(ToolStripItemCollection items, string text, bool isChecked, EventHandler onClick)
    {
        var item = new ToolStripMenuItem(text)
        {
            Checked = isChecked,
            CheckOnClick = false
        };
        item.Click += onClick;
        items.Add(item);
        return item;
    }

    public void AutoResizeColumn(ViewGridColumn col) => AutoResizeColumnToContent(col);

    private IEnumerable<int> GetCheckedIndices()
    {
        var c = GetActiveCheckBoxColumn(); if (c == null) yield break;
        for (int i = 0; i < ViewCount; i++)
        {
            var row = GetViewRow(i);
            if (row == null) continue;
            if (GetRowCheckState(row, c) == CheckState.Checked) yield return i;
        }
    }

    private IEnumerable<object> GetCheckedObjects()
    {
        var c = GetActiveCheckBoxColumn(); if (c == null) yield break;
        for (int i = 0; i < ViewCount; i++)
        {
            var row = GetViewRow(i);
            if (row == null) continue;
            if (GetRowCheckState(row, c) == CheckState.Checked) yield return row;
        }
    }
}

internal sealed class ViewGridMenuDismissMessageFilter : IMessageFilter
{
    private readonly Func<bool> _isInsideAllowedArea;
    private readonly Action _closeAll;
    private bool _disabled;
    private bool _closing;

    public ViewGridMenuDismissMessageFilter(Func<bool> isInsideAllowedArea, Action closeAll)
    {
        _isInsideAllowedArea = isInsideAllowedArea;
        _closeAll = closeAll;
    }

    public void Disable() => _disabled = true;

    public bool PreFilterMessage(ref Message m)
    {
        if (_disabled || _closing) return false;

        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_RBUTTONDOWN = 0x0204;
        const int WM_MBUTTONDOWN = 0x0207;
        const int WM_NCLBUTTONDOWN = 0x00A1;
        const int WM_NCRBUTTONDOWN = 0x00A4;

        if (m.Msg != WM_LBUTTONDOWN &&
            m.Msg != WM_RBUTTONDOWN &&
            m.Msg != WM_MBUTTONDOWN &&
            m.Msg != WM_NCLBUTTONDOWN &&
            m.Msg != WM_NCRBUTTONDOWN)
            return false;

        bool inside;
        try { inside = _isInsideAllowedArea(); }
        catch { inside = false; }

        if (!inside)
        {
            _closing = true;
            try { _closeAll(); }
            catch { }
        }

        return false;
    }
}

internal readonly struct ViewGridDisplayRow
{
    private ViewGridDisplayRow(bool isGroup, int realIndex, string groupKey, string caption, int count, bool collapsed)
    {
        IsGroup = isGroup; RealIndex = realIndex; GroupKey = groupKey; Caption = caption; Count = count; Collapsed = collapsed;
    }
    public bool IsGroup { get; }
    public int RealIndex { get; }
    public string GroupKey { get; }
    public string Caption { get; }
    public int Count { get; }
    public bool Collapsed { get; }
    public static ViewGridDisplayRow Data(int realIndex) => new(false, realIndex, string.Empty, string.Empty, 0, false);
    public static ViewGridDisplayRow Group(string caption, int count, bool collapsed) => new(true, -1, caption, $"{caption} ({count})", count, collapsed);
}

public class ViewGridCellClickEventArgs : EventArgs
{
    public ViewGridCellClickEventArgs(int rowIndex, object rowObject, ViewGridColumn column) { RowIndex = rowIndex; RowObject = rowObject; Column = column; }
    public int RowIndex { get; }
    public object RowObject { get; }
    public ViewGridColumn Column { get; }

    /// <summary>True ise tıklanan hücre ViewGrid checkbox/toggle veya GLV compatibility checkbox hücresidir.</summary>
    public bool IsCheckBoxColumn => Column.Kind == ViewGridColumnKind.CheckBox || Column.Kind == ViewGridColumnKind.ToggleSwitch || Column.CellCheckBox;
}
public sealed class ViewGridCellEditEventArgs : ViewGridCellClickEventArgs
{
    public ViewGridCellEditEventArgs(int rowIndex, object rowObject, ViewGridColumn column, object? newValue) : base(rowIndex,rowObject,column) => NewValue = newValue;
    public object? NewValue { get; }

    /// <summary>Checkbox/toggle kolonları için NewValue değerini güvenli bool olarak döndürür.</summary>
    public bool? NewValueAsBoolean
    {
        get
        {
            if (NewValue is bool b) return b;
            if (NewValue is CheckState state) return state == CheckState.Checked;
            if (NewValue is int i) return i != 0;
            if (NewValue is string s && bool.TryParse(s, out var parsed)) return parsed;
            return null;
        }
    }
}
internal sealed class NonClosingToolStripControlHost : ToolStripControlHost
{
    public NonClosingToolStripControlHost(Control control) : base(control)
    {
        AutoSize = false;
        control.TabStop = true;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        try { Control?.Focus(); } catch { }
    }
    protected override void OnSubscribeControlEvents(Control? control)
    {
        base.OnSubscribeControlEvents(control);
        if (control == null) return;
        control.MouseDown += KeepDropDownOpen;
        control.MouseUp += KeepDropDownOpen;
        foreach (Control child in control.Controls) SubscribeRecursive(child);
    }
    protected override void OnUnsubscribeControlEvents(Control? control)
    {
        if (control == null) { base.OnUnsubscribeControlEvents(control); return; }
        control.MouseDown -= KeepDropDownOpen;
        control.MouseUp -= KeepDropDownOpen;
        foreach (Control child in control.Controls) UnsubscribeRecursive(child);
        base.OnUnsubscribeControlEvents(control);
    }
    private void SubscribeRecursive(Control c)
    {
        c.MouseDown += KeepDropDownOpen;
        c.MouseUp += KeepDropDownOpen;
        foreach (Control child in c.Controls) SubscribeRecursive(child);
    }
    private void UnsubscribeRecursive(Control c)
    {
        c.MouseDown -= KeepDropDownOpen;
        c.MouseUp -= KeepDropDownOpen;
        foreach (Control child in c.Controls) UnsubscribeRecursive(child);
    }
    private static void KeepDropDownOpen(object? sender, MouseEventArgs e) { }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush b, Rectangle r, int radius) { using var p = Rounded(r, radius); g.FillPath(b, p); }
    public static void DrawRoundedRectangle(this Graphics g, Pen p, Rectangle r, int radius) { using var path = Rounded(r, radius); g.DrawPath(p, path); }
    private static GraphicsPath Rounded(Rectangle r, int d) { var gp = new GraphicsPath(); gp.AddArc(r.X,r.Y,d,d,180,90); gp.AddArc(r.Right-d,r.Y,d,d,270,90); gp.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); gp.AddArc(r.X,r.Bottom-d,d,d,90,90); gp.CloseFigure(); return gp; }
}
