using System.ComponentModel;
using System.Collections;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using ViewGrid.Editing;
using ViewGrid.Intelligence;

namespace ViewGrid.Columns;

/// <summary>ViewGrid uyumlu kolon ikon delegate imzası. BrightIdeasSoftware bağımlılığı yoktur.</summary>
public delegate object? ViewGridImageGetterDelegate(object rowObject);

/// <summary>ViewGrid uyumlu kolon text delegate imzası. BrightIdeasSoftware bağımlılığı yoktur.</summary>
public delegate string? ViewGridAspectToStringConverterDelegate(object? value);

/// <summary>ViewGrid uyumluluğu için kolon içi button boyutlama modu.</summary>
public enum ViewGridColumnButtonSizing
{
    FixedBounds = 0,
    TextBounds = 1,
    CellBounds = 2
}

public enum ViewGridAutoFilterMode
{
    Default = 0,
    Smart = 1,
    Text = 2,
    Number = 3,
    Date = 4,
    Boolean = 5,
    ValueList = 6
}

public enum ViewGridAggregateMode
{
    None = 0,
    Count = 1,
    Sum = 2,
    Average = 3,
    Min = 4,
    Max = 5,
    Custom = 6
}

public enum ViewGridFilterOperator
{
    Contains = 0,
    StartsWith = 1,
    EndsWith = 2,
    Equals = 3,
    NotEquals = 4,
    GreaterThan = 5,
    GreaterOrEqual = 6,
    LessThan = 7,
    LessOrEqual = 8,
    Between = 9,
    IsBlank = 10,
    IsNotBlank = 11,
    Regex = 12
}

[ToolboxItem(false)]
[DesignTimeVisible(false)]
[TypeConverter(typeof(ViewGridColumnTypeConverter))]
public class ViewGridColumn : Component
{
    [Category("ViewGrid - Card Layout")]
    [DefaultValue(true)]
    public bool VisibleInCard { get; set; } = true;
    [Category("ViewGrid - Card Layout")]
    [DefaultValue(0)]
    public int CardOrder { get; set; }
    [Category("ViewGrid - Card Layout")]
    [DefaultValue("")]
    public string CardRole { get; set; } = string.Empty;
    [Category("ViewGrid - Card Layout")]
    [DefaultValue(false)]
    public bool CardShowCaption { get; set; }
    [Category("ViewGrid - Card Layout")]
    [DefaultValue(1)]
    public int CardMaxLines { get; set; } = 1;

    [Category("ViewGrid - Cell Overflow")]
    [DisplayName("Allow Cell Scroll")]
    [Description("Uzun çok satırlı metinlerde satır yüksekliğini büyütmeden hücre içinde dikey kaydırma yapılmasını sağlar.")]
    [DefaultValue(false)]
    public bool AllowCellScroll { get; set; }

    [Category("ViewGrid - Cell Overflow")]
    [DisplayName("Cell Scroll Bar")]
    [Description("Hücre içi overflow olduğunda sağ kenarda ince mini scrollbar çizilir.")]
    [DefaultValue(true)]
    public bool ShowCellScrollBar { get; set; } = true;

    [Category("ViewGrid - Cell Overflow")]
    [DisplayName("Cell Scroll Max Lines")]
    [Description("Hücre içi scroll açıkken görünür maksimum satır sayısı. 0 ise ViewGrid varsayılan MaxCellTextLines kullanılır.")]
    [DefaultValue(0)]
    public int CellScrollMaxVisibleLines { get; set; }

    [Category("ViewGrid - Cell Overflow")]
    [DisplayName("Cell Overflow Fade")]
    [Description("Hücrede aşağı/yukarı taşma varsa kenarda yumuşak fade ipucu çizer.")]
    [DefaultValue(true)]
    public bool CellOverflowFade { get; set; } = true;

    [Category("ViewGrid - Cell Overflow")]
    [DisplayName("Cell Overflow Details On Double Click")]
    [Description("Uzun metinli hücrelerde çift tıklama ile okunabilir detay penceresi açılmasına izin verir.")]
    [DefaultValue(false)]
    public bool CellOverflowDetailsOnDoubleClick { get; set; }

    private static readonly ConcurrentDictionary<(Type type, string name), PropertyInfo?> PropertyCache = new();
    private int _width = 120;
    private bool _visible = true;
    private string _name = string.Empty;
    private string _aspectName = string.Empty;
    private string _header = "Column";
    private int _displayIndex = -1;
    private bool _headerCheckBox;
    private bool _privateColumn;

    private bool IsInDesignerMode =>
        LicenseManager.UsageMode == LicenseUsageMode.Designtime;

    public ViewGridColumn()
    {
        Name = ViewGridColumnNameHelper.CreateDefaultName(1);
        DefaultWidth = 120;
        DefaultVisible = true;
    }
    public ViewGridColumn(string title, string aspectName, int width = 120)
    {
        Header = title;
        AspectName = aspectName;
        Name = ViewGridColumnNameHelper.CreateNameFromAspectOrText(aspectName, title);
        Width = width;
        DefaultWidth = width;
        DefaultVisible = true;
    }

    [Category("ViewGrid")]
    [DisplayName("Header")]
    [Description("Kolon başlığında görünen metindir.")]
    [DefaultValue("Column")]
    public string Header
    {
        get => _header;
        set
        {
            _header = value ?? string.Empty;
            RefreshGeneratedNameIfNeeded();
        }
    }

    /// <summary>
    /// ViewGrid uyumlu kolon kimliği. PropertyGrid'de ayrıca görünmez;
    /// WinForms designer tarafındaki gerçek kimlik Design > (Name) alanıdır.
    /// Kod tarafında eski liste alışkanlığıyla column.Name okunup yazılabilir.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string Name
    {
        get => !string.IsNullOrWhiteSpace(Site?.Name) ? Site!.Name : _name;
        set
        {
            // Design-time'da görünen tek isim alanı Visual Studio'nun gerçek
            // Component (Name) alanıdır. Bu public property sadece runtime/OLV
            // uyumluluğu ve eski kodların column.Name kullanımı için tutulur.
            // Site.Name setter olmadığı için designer rename işlemi VS tarafından
            // yönetilir; getter Site.Name'i öncelikli okuyarak doğru field adını döndürür.
            _name = value ?? string.Empty;
        }
    }


    private void RefreshGeneratedNameIfNeeded()
    {
        // Name artık Text/AspectName değişiminde otomatik türetilmez.
        // Varsayılan yeni kolon adı glvColumn1, glvColumn2... olarak kalır.
        // Kullanıcı Design > (Name) alanına ProgramName gibi özel bir ad yazarsa aynen korunur.
    }

    [Category("ViewGrid"), DefaultValue("")]
    [Description("Satır nesnesinden okunacak property/field/dictionary key adıdır. Name ile aynı olmak zorunda değildir.")]
    public string AspectName
    {
        get => _aspectName;
        set
        {
            _aspectName = value ?? string.Empty;
            RefreshGeneratedNameIfNeeded();
        }
    }

    [Browsable(false)]
    public string Key => !string.IsNullOrWhiteSpace(Name) ? Name : (!string.IsNullOrWhiteSpace(AspectName) ? AspectName : Header);

    /// <summary>
    /// ViewGrid-style designer text: Name (AspectName).
    /// Collection editors call ToString(), so keeping this separate makes both
    /// designer lists and debugging output predictable.
    /// </summary>
    [Browsable(false)]
    public string DisplayText => ViewGridColumnNameHelper.GetDesignerDisplayText(this);

    /// <summary>
    /// Stable layout key. Prefer Name; fall back to AspectName/Header. This mirrors
    /// the ViewGrid habit of using column Name independently from AspectName.
    /// </summary>
    [Browsable(false)]
    public string LayoutKey => Key;

    [Browsable(false)]
    public string ColumnKey => Key;

    [Browsable(false)]
    public string Identifier { get => Name; set => Name = value ?? string.Empty; }

    // ViewGrid / DataGridView geçişini kolaylaştıran alias'lar.
    [Category("ViewGrid - Compatibility"), Browsable(false)]
    public string FieldName { get => AspectName; set => AspectName = value ?? string.Empty; }
    [Category("ViewGrid - Compatibility"), Browsable(false)]
    public string DataField { get => AspectName; set => AspectName = value ?? string.Empty; }
    [Category("ViewGrid - Compatibility"), Browsable(false)]
    public string PropertyName { get => AspectName; set => AspectName = value ?? string.Empty; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Text { get => Header; set => Header = value ?? string.Empty; }

    [Category("ViewGrid - Compatibility"), Browsable(false)]
    public string HeaderText { get => Header; set => Header = value ?? string.Empty; }
    [Category("ViewGrid - Compatibility"), Browsable(false)]
    public bool IsVisible { get => Visible; set => Visible = value; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Func<object, object?>? AspectGetter { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Action<object, object?>? AspectPutter { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public ViewGridAspectToStringConverterDelegate? AspectToStringConverter { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(null)] public string? AspectToStringFormat { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public ViewGridAspectToStringConverterDelegate? ValueToStringConverter { get => AspectToStringConverter; set => AspectToStringConverter = value; }
    [Category("ViewGrid"), DefaultValue(120)]
    public int Width
    {
        get => _width;
        set
        {
            var width = Math.Max(0, value);
            if (MinimumWidth >= 0) width = Math.Max(MinimumWidth, width);
            if (MaximumWidth >= 0) width = Math.Min(MaximumWidth, width);
            _width = width;
            if (DefaultWidth <= 0) DefaultWidth = _width;
        }
    }
    [Category("ViewGrid"), DefaultValue(true)]
    [Description("Kolonun kullanıcı tarafından görünür olup olmadığını belirler. PrivateColumn=true ise bu değer korunur ancak kolon gridde ve runtime kolon seçicide yine de görünmez.")]
    public bool Visible
    {
        get => _visible;
        set
        {
            // Visible normal kullanıcı gizleme durumudur. PrivateColumn bundan ayrı, daha güçlü
            // bir gizleme seviyesidir. Bu yüzden PrivateColumn=true iken bile Visible değerini
            // kaybetmeyiz; kullanıcı PrivateColumn=false yaptığında eski Visible tercihi korunur.
            _visible = value;
            DefaultVisible = _visible;
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ApplyRuntimeVisible(bool visible)
    {
        // Runtime kolon seçici PrivateColumn kolonları zaten listelemez. Yine de bu metod
        // doğrudan çağrılırsa Visible tercihini saklarız; efektif görünürlük
        // IsEffectivelyVisible/VisibleColumns tarafında !PrivateColumn && Visible olarak hesaplanır.
        _visible = visible;
    }

    /// <summary>
    /// Kolonu tamamen dahili/özel kolon yapar.
    /// PrivateColumn=true olan kolonlar design-time grid yüzeyinde, runtime kolon seçicisinde,
    /// header/gövde çiziminde ve görünür export/print akışlarında görünmez.
    /// Kolon ViewGrid kolon düzenleyicisinde görünür kalır; kullanıcı isterse geri alabilir.
    /// Kolon koleksiyonda kalır; AspectName/AspectGetter üzerinden veri erişimi bozulmaz.
    /// </summary>
    [Browsable(true)]
    [Category("ViewGrid - Behavior")]
    [DisplayName("Private Column")]
    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Description("Kolonu dahili/gizli kolon yapar. True yapıldığında kolon tasarım/runtime grid yüzeyinden, runtime kolon seçicisinden, export ve print akışından çıkar; ViewGrid kolon düzenleyicisinde görünür kalır.")]
    public bool PrivateColumn
    {
        get => _privateColumn;
        set
        {
            // PrivateColumn, Visible yerine geçmez. Kolonu grid/design yüzeyinde, runtime kolon
            // seçicide, export/print akışında gizler; fakat kolon editoründe görünür kalır.
            // Visible, AllowColumnChooser ve CanBeHidden gibi kullanıcı tercihleri bozulmaz.
            _privateColumn = value;
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool HiddenColumn { get => PrivateColumn; set => PrivateColumn = value; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool InternalColumn { get => PrivateColumn; set => PrivateColumn = value; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsEffectivelyVisible => !PrivateColumn && Visible;


    [Category("ViewGrid - Smart Filter"), DefaultValue(ViewGridAutoFilterMode.Default)]
    [Description("Kolonun filtre editörünü veri tipine göre otomatik seçer. Smart; text/number/date/bool alanlarını algılamaya uygundur.")]
    public ViewGridAutoFilterMode AutoFilterMode { get; set; } = ViewGridAutoFilterMode.Default;

    [Category("ViewGrid - Smart Filter"), DefaultValue(ViewGridFilterOperator.Contains)]
    [Description("Akıllı filtre penceresinde bu kolon için varsayılan operatör.")]
    public ViewGridFilterOperator DefaultFilterOperator { get; set; } = ViewGridFilterOperator.Contains;

    [Category("ViewGrid - Smart Filter"), DefaultValue(false)]
    [Description("Filtre değer listesinde Top values / Top 10 gibi hızlı seçimlerin önerilmesini sağlar.")]
    public bool ShowTopValuesInFilter { get; set; }

    [Category("ViewGrid - Search"), DefaultValue(null)]
    [Description("Global aramada kullanılacak kısa kolon takma adı. Örn: status:open, machine:LINE1.")]
    public string? SearchAlias { get; set; }

    [Category("ViewGrid - Layout"), DefaultValue(false)]
    [Description("Excel benzeri sabit kolon isteği. Çizim motoru desteklediğinde sol tarafta sabit tutulur; layout/profile içinde de saklanır.")]
    public bool Frozen { get; set; }

    [Category("ViewGrid - Summary"), DefaultValue(ViewGridAggregateMode.None)]
    [Description("Footer/summary alanında bu kolon için hesaplanacak özet türü.")]
    public ViewGridAggregateMode Aggregate { get; set; } = ViewGridAggregateMode.None;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<IEnumerable<object>, ViewGridColumn, object?>? CustomAggregateGetter { get; set; }

    [Category("ViewGrid - Column Manager"), DefaultValue(120)] public int DefaultWidth { get; set; } = 120;
    [Category("ViewGrid - Column Manager"), DefaultValue(true)] public bool DefaultVisible { get; set; } = true;
    [Category("ViewGrid - Behavior"), DefaultValue(true)] public bool Sortable { get; set; } = true;
    [Category("ViewGrid - Behavior"), DefaultValue(true)] public bool Filterable { get; set; } = true;
    [Category("ViewGrid - Behavior"), DefaultValue(true)] public bool AllowResize { get; set; } = true;
    [Category("ViewGrid - Behavior"), DefaultValue(true)] public bool AllowReorder { get; set; } = true;
    [Category("ViewGrid - Behavior"), DefaultValue(true)] public bool AllowGroup { get; set; } = true;
    [Category("ViewGrid - Behavior"), DefaultValue(true)] public bool AllowColumnChooser { get; set; } = true;
    [Category("ViewGrid - Behavior"), DefaultValue(true)]
    [Description("Kolonun kullanıcı tarafından gizlenip gizlenemeyeceğini belirler. ObjectListView uyumluluğu için CanBeHidden adını da destekler.")]
    public bool CanBeHidden { get; set; } = true;
    [Category("ViewGrid - Layout"), DefaultValue(-1)] public int MinimumWidth { get; set; } = -1;
    [Category("ViewGrid - Layout"), DefaultValue(-1)] public int MaximumWidth { get; set; } = -1;
    [Category("ViewGrid - Layout"), DefaultValue(-1)] public int DisplayIndex { get => _displayIndex; set => _displayIndex = value; }
    [Category("ViewGrid - Layout"), DefaultValue(false)] public bool WordWrap { get; set; }
    [Category("ViewGrid - Layout"), DefaultValue(0)]
    [Description("WordWrap aktifken bu kolon için gösterilecek en fazla metin satırı. 0: ViewGrid genel MaxCellTextLines değerini kullanır.")]
    public int MaxTextLines { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool FillsFreeSpace { get => FillFreeSpace; set => FillFreeSpace = value; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool UseFiltering { get => Filterable; set => Filterable = value; }
    [Category("ViewGrid - Compatibility"), DefaultValue(true)] public bool Searchable { get; set; } = true;
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Groupable { get => AllowGroup; set => AllowGroup = value; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsEditable { get => Editable; set => Editable = value; }
    [Category("ViewGrid - Compatibility"), DefaultValue(false)] public bool IsTileViewColumn { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(0)] public int FreeSpaceProportion { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(null)] public string? ToolTipText { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public object? Tag { get; set; }

    // ViewGrid migration aliases. Designer'da kolon bazlı yönetilebilir.
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool AllowFilter { get => Filterable; set => Filterable = value; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool AllowSort { get => Sortable; set => Sortable = value; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool CanGroup { get => AllowGroup; set => AllowGroup = value; }

    // ViewGrid kolon editörü uyumluluk alanları.
    // Bunların bir kısmı ViewGrid tarafında doğrudan çizim davranışına bağlanır,
    // bir kısmı ise eski kolon designer kodlarının kırılmaması ve property gridde
    // aynı mantıkla yönetilebilmesi için metadata olarak tutulur.
    [Category("ViewGrid - Compatibility"), DefaultValue(false)]
    [Description("Kolonu checkbox kolonu yapar. True yapıldığında satır checkboxları ve header checkbox birlikte etkinleşir.")]
    public bool CheckBoxes
    {
        get => Kind == ViewGridColumnKind.CheckBox;
        set
        {
            if (value)
            {
                Kind = ViewGridColumnKind.CheckBox;
                HeaderCheckBox = true;
                HeaderCheckBoxUpdatesRowCheckBoxes = true;
                HeaderTextAlign = ContentAlignment.MiddleCenter;
                TextAlign = ContentAlignment.MiddleCenter;
                Sortable = false;
                Filterable = false;
                if (Width > 90) Width = 34;
                if (DefaultWidth > 90) DefaultWidth = 34;
            }
            else if (Kind == ViewGridColumnKind.CheckBox)
            {
                HeaderCheckBox = false;
                Kind = ViewGridColumnKind.Text;
            }
        }
    }
    [Category("ViewGrid - Compatibility"), DefaultValue(false)] public bool TriStateCheckBoxes { get; set; }
    [Category("ViewGrid - CheckBox"), DefaultValue(false)]
    [Description("Checkbox'u hücre metniyle birlikte aynı kolon içinde gösterir. Kind=CheckBox gibi metni gizlemez; Person, Durum, Not vb. herhangi bir kolonda kullanılabilir.")]
    public bool CellCheckBox { get; set; }

    [Category("ViewGrid - CheckBox"), DefaultValue("")]
    [Description("CellCheckBox/CheckBoxes durumunun okunup yazılacağı bool/CheckState property adıdır. Boşsa kolonun AspectName değeri kullanılır.")]
    public string CheckBoxAspectName { get; set; } = string.Empty;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool HeaderTriStateCheckBox { get => HeaderCheckBoxThreeState; set => HeaderCheckBoxThreeState = value; }
    [Category("ViewGrid - Compatibility"), DefaultValue(false)] public bool HeaderCheckBoxDisabled { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(true)] public bool HeaderCheckBoxUpdatesRowCheckBoxes { get; set; } = true;
    [Category("ViewGrid - Compatibility"), DefaultValue(false)] public bool CellEditUseWholeCell { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(false)] public bool EnableButtonWhenItemIsDisabled { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(false)] public bool Hyperlink { get => Kind == ViewGridColumnKind.Hyperlink; set { if (value) Kind = ViewGridColumnKind.Hyperlink; } }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Hideable { get => AllowColumnChooser; set => AllowColumnChooser = value; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsHeaderVertical { get => HeaderTextVertical; set => HeaderTextVertical = value; }
    [Category("ViewGrid - Compatibility"), DefaultValue(false)] public bool ShowTextInHeader { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(true)] public bool UseInitialLetterForGroup { get; set; } = true;
    [Category("ViewGrid - Compatibility"), DefaultValue(true)] public bool AutoCompleteEditor { get; set; } = true;
    [Category("ViewGrid - Compatibility"), DefaultValue(System.Windows.Forms.AutoCompleteMode.Append)] public System.Windows.Forms.AutoCompleteMode AutoCompleteEditorMode { get; set; } = System.Windows.Forms.AutoCompleteMode.Append;
    [Category("ViewGrid - Button"), DefaultValue("")]
    [Description("Button kolonunda model değerinden bağımsız sabit metin gösterir. Boş bırakılırsa hücre değeri veya kolon başlığı kullanılır.")]
    public string ButtonText { get; set; } = string.Empty;

    [Category("ViewGrid - Compatibility"), DefaultValue(-1)] public int ButtonMaxWidth { get; set; } = -1;
    [Category("ViewGrid - Compatibility"), DefaultValue(typeof(Padding), "0, 0, 0, 0")] public Padding ButtonPadding { get; set; } = Padding.Empty;
    [Category("ViewGrid - Compatibility"), DefaultValue(typeof(Size), "0, 0")] public Size ButtonSize { get; set; } = Size.Empty;
    [Category("ViewGrid - Compatibility"), DefaultValue(ViewGridColumnButtonSizing.TextBounds)] public ViewGridColumnButtonSizing ButtonSizing { get; set; } = ViewGridColumnButtonSizing.TextBounds;
    [Category("ViewGrid - Compatibility"), DefaultValue(typeof(Padding), "0, 0, 0, 0")] public Padding CellPadding { get; set; } = Padding.Empty;
    [Category("ViewGrid - Compatibility"), DefaultValue(ContentAlignment.MiddleCenter)] public ContentAlignment CellVerticalAlignment { get; set; } = ContentAlignment.MiddleCenter;
    [Category("ViewGrid - Compatibility"), DefaultValue(null)] public string? GroupWithItemCountFormat { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(null)] public string? GroupWithItemCountSingularFormat { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(null)] public Font? HeaderFont { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(null)] public object? HeaderFormatStyle { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(null)] public string? HeaderImageKey { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(-1)] public int HeaderImageIndex { get; set; } = -1;
    [Category("ViewGrid - Compatibility"), DefaultValue(null)] public string? HeaderToolTipText { get; set; }
    [Category("ViewGrid - Compatibility"), DefaultValue(null)] public object? Renderer { get; set; }

    [Category("ViewGrid"), DefaultValue(false)] public bool Editable { get; set; }
    [Category("ViewGrid - Database"), DefaultValue(false)] public bool ReadOnly { get; set; }
    [Category("ViewGrid - Database"), DefaultValue(false)] public bool Required { get; set; }
    [Category("ViewGrid - Database"), DefaultValue(0)] public int MaxLength { get; set; }
    [Category("ViewGrid - Editing"), DefaultValue(ViewGrid.Editing.ViewGridCellEditorKind.Auto)] public ViewGrid.Editing.ViewGridCellEditorKind EditorType { get; set; } = ViewGrid.Editing.ViewGridCellEditorKind.Auto;
    [Category("ViewGrid"), DefaultValue(ViewGridColumnKind.Text)] public ViewGridColumnKind Kind { get; set; } = ViewGridColumnKind.Text;
    [Category("ViewGrid"), DefaultValue(ContentAlignment.MiddleLeft)] public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleLeft;

    // v25.18 ViewGrid header visuals: eski liste kontrolündeki kolon checkbox, dikey başlık, renkli başlık ve başlık ikonu davranışları.
    [Category("ViewGrid - Header"), DefaultValue(false)]
    public bool HeaderCheckBox
    {
        get => _headerCheckBox;
        set
        {
            _headerCheckBox = value;
            if (value) HeaderCheckBoxUpdatesRowCheckBoxes = true;
        }
    }
    [Category("ViewGrid - Header"), DefaultValue(CheckState.Unchecked)] public CheckState HeaderCheckState { get; set; } = CheckState.Unchecked;
    [Category("ViewGrid - Header"), DefaultValue(false)] public bool HeaderCheckBoxThreeState { get; set; }
    [Category("ViewGrid - Header"), DefaultValue(false)] public bool HeaderTextVertical { get; set; }
    [Category("ViewGrid - Header"), DefaultValue(0)] public int HeaderTextAngle { get; set; }
    [Category("ViewGrid - Header"), DefaultValue(ContentAlignment.MiddleLeft)] public ContentAlignment HeaderTextAlign { get; set; } = ContentAlignment.MiddleLeft;
    [Category("ViewGrid - Header"), DefaultValue(typeof(Color), "Empty")] public Color HeaderForeColor { get; set; } = Color.Empty;
    [Category("ViewGrid - Header"), DefaultValue(typeof(Color), "Empty")] public Color HeaderBackColor { get; set; } = Color.Empty;
    [Category("ViewGrid - Header"), DefaultValue(null)] public Image? HeaderImage { get; set; }
    [Category("ViewGrid - Header"), DefaultValue(ContentAlignment.MiddleLeft)] public ContentAlignment HeaderImageAlign { get; set; } = ContentAlignment.MiddleLeft;
    [Category("ViewGrid - Header"), DefaultValue(true)] public bool HeaderImageBeforeText { get; set; } = true;
    [Category("ViewGrid - Header"), DefaultValue(16)] public int HeaderImageSize { get; set; } = 16;

    // ViewGrid migration aliases. Yeni kullanımda Header* adları tercih edilir.
    [Category("ViewGrid - Header"), Browsable(false)] public bool HeaderUsesCheckBox { get => HeaderCheckBox; set => HeaderCheckBox = value; }
    [Category("ViewGrid - Header"), Browsable(false)] public bool HeaderIsVertical { get => HeaderTextVertical; set => HeaderTextVertical = value; }
    [Category("ViewGrid - Header"), Browsable(false)] public Color HeaderTextColor { get => HeaderForeColor; set => HeaderForeColor = value; }
    [Category("ViewGrid - Header"), Browsable(false)] public Color HeaderColor { get => HeaderBackColor; set => HeaderBackColor = value; }
    [Category("ViewGrid - Header"), Browsable(false)] public Image? HeaderIcon { get => HeaderImage; set => HeaderImage = value; }

    [Category("ViewGrid - Header Icons"), DefaultValue(ViewGrid.Core.ViewGridFilterIconStyle.Inherit)]
    public ViewGrid.Core.ViewGridFilterIconStyle FilterIconStyle { get; set; } = ViewGrid.Core.ViewGridFilterIconStyle.Inherit;

    [Category("ViewGrid - Header Icons"), DefaultValue(null)]
    public Image? FilterIcon { get; set; }

    [Category("ViewGrid - Header Icons"), DefaultValue(ViewGrid.Core.ViewGridSortGlyphStyle.Inherit)]
    public ViewGrid.Core.ViewGridSortGlyphStyle SortGlyphStyle { get; set; } = ViewGrid.Core.ViewGridSortGlyphStyle.Inherit;

    [Category("ViewGrid - Header Icons"), DefaultValue(null)]
    public Image? SortAscendingIcon { get; set; }

    [Category("ViewGrid - Header Icons"), DefaultValue(null)]
    public Image? SortDescendingIcon { get; set; }

    [Category("ViewGrid - Formula")]
    [DefaultValue("")]
    [Description("Computed column expression. Examples: Qty * Price, Status == 'Done' ? 'OK' : 'Wait'. Numeric expressions use DataTable.Compute; simple ternary equality is supported.")]
    public string Formula { get; set; } = string.Empty;

    [Category("ViewGrid - Formula")]
    [DefaultValue(false)]
    [Description("When true, Formula is preferred over AspectName/AspectGetter for display and export.")]
    public bool UseFormula { get; set; }

    [Category("ViewGrid - Pinning")]
    [DefaultValue(ViewGrid.Core.ViewGridColumnPinMode.None)]
    [Description("Excel/AG Grid style column pinning metadata. Renderers can keep pinned columns fixed left/right.")]
    public ViewGrid.Core.ViewGridColumnPinMode PinMode { get; set; } = ViewGrid.Core.ViewGridColumnPinMode.None;

    [Category("ViewGrid - Sparkline")]
    [DefaultValue(false)]
    [Description("Marks this column as a mini chart/sparkline column for renderers that support trend drawing.")]
    public bool Sparkline { get; set; }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<object, IEnumerable<double>>? SparklineGetter { get; set; }

    [Category("ViewGrid"), DefaultValue(5)] public int MaxRating { get; set; } = 5;
    [Category("ViewGrid"), DefaultValue("★")] public string RatingSymbol { get; set; } = "★";
    [Category("ViewGrid"), DefaultValue(",")] public string TagSeparator { get; set; } = ",";
    [Category("ViewGrid"), DefaultValue(false)] public bool FillFreeSpace { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Image? Image { get; set; }
    /// <summary>
    /// ViewGrid uyumlu kolon bazlı ikon üretici. Image, Icon, Bitmap, string ImageList key,
    /// int ImageList index veya null dönebilir. Eski ViewGrid kodundaki Image döndüren delegate'ler de çalışır.
    /// </summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public ViewGridImageGetterDelegate? ImageGetter { get; set; }

    /// <summary>ViewGrid benzeri sabit ImageList key/index. ImageGetter yoksa kullanılır.</summary>
    [Category("ViewGrid - Compatibility"), DefaultValue(null)] public object? ImageKey { get; set; }

    [Category("ViewGrid - Compatibility"), DefaultValue(-1)] public int ImageIndex { get; set; } = -1;

    /// <summary>ViewGrid benzeri state image üretici. StateImageList veya SmallImageList üzerinden çözümlenir.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public ViewGridImageGetterDelegate? StateImageGetter { get; set; }

    [Category("ViewGrid - Compatibility"), DefaultValue("")] public string ImageAspectName { get; set; } = string.Empty;
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Func<object, CheckState>? CheckStateGetter { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Action<object, CheckState>? CheckStatePutter { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Func<object, bool>? BooleanCheckStateGetter { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Action<object, bool>? BooleanCheckStatePutter { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Func<string, Image?>? ComboBoxImageGetter { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Func<object, string?>? ToolTipGetter { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public ICellEditor? Editor { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public IList<string>? ComboBoxItems { get; set; }
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public IEnumerable<string>? ComboItems { get => ComboBoxItems; set => ComboBoxItems = value?.ToList(); }
    [Category("ViewGrid - Editing"), DefaultValue(-999999999)] public decimal NumericMinimum { get; set; } = -999999999m;
    [Category("ViewGrid - Editing"), DefaultValue(999999999)] public decimal NumericMaximum { get; set; } = 999999999m;
    [Category("ViewGrid - Editing"), DefaultValue(0)] public int NumericDecimalPlaces { get; set; }
    [Category("ViewGrid - Editing"), DefaultValue(ViewGrid.Editing.ViewGridDateTimeEditorFormat.Short)] public ViewGrid.Editing.ViewGridDateTimeEditorFormat DateTimeFormat { get; set; } = ViewGrid.Editing.ViewGridDateTimeEditorFormat.Short;
    [Category("ViewGrid - Editing"), DefaultValue("dd.MM.yyyy HH:mm")] public string DateTimeCustomFormat { get; set; } = "dd.MM.yyyy HH:mm";
    [Category("ViewGrid - Editing"), DefaultValue(true)] public bool DateTimeNullable { get; set; } = true;

    public object? GetImageKeyOrImage(object row)
    {
        if (ImageGetter != null)
        {
            try { return ImageGetter(row); }
            catch { return null; }
        }
        if (!string.IsNullOrWhiteSpace(ImageAspectName))
            return GetValueByName(row, ImageAspectName);
        if (ImageKey != null) return ImageKey;
        if (ImageIndex >= 0) return ImageIndex;
        return Image;
    }

    public object? GetStateImageKeyOrImage(object row)
    {
        if (StateImageGetter != null)
        {
            try { return StateImageGetter(row); }
            catch { return null; }
        }
        return null;
    }

    public CheckState GetCheckState(object row)
    {
        if (CheckStateGetter != null)
        {
            try { return CheckStateGetter(row); }
            catch { return CheckState.Unchecked; }
        }
        if (BooleanCheckStateGetter != null)
        {
            try { return BooleanCheckStateGetter(row) ? CheckState.Checked : CheckState.Unchecked; }
            catch { return CheckState.Unchecked; }
        }
        var value = !string.IsNullOrWhiteSpace(CheckBoxAspectName)
            ? GetValueByName(row, CheckBoxAspectName)
            : GetValue(row);
        if (value is CheckState state) return state;
        try { return Convert.ToBoolean(value) ? CheckState.Checked : CheckState.Unchecked; }
        catch { return CheckState.Unchecked; }
    }

    public void PutCheckState(object row, CheckState state)
    {
        if (ReadOnly) return;
        if (CheckStatePutter != null) { CheckStatePutter(row, state); return; }
        if (BooleanCheckStatePutter != null) { BooleanCheckStatePutter(row, state == CheckState.Checked); return; }
        if (!string.IsNullOrWhiteSpace(CheckBoxAspectName))
        {
            PutValueByName(row, CheckBoxAspectName, state == CheckState.Checked);
            return;
        }
        PutValue(row, state == CheckState.Checked);
    }

    private object? GetValueByName(object row, string aspectName)
    {
        if (row is DataRow dr && dr.Table.Columns.Contains(aspectName)) return dr[aspectName];
        if (row is IDictionary<string, object?> genericDict && genericDict.TryGetValue(aspectName, out var genericValue)) return genericValue;
        if (row is IDictionary dict && dict.Contains(aspectName)) return dict[aspectName];
        var type = row.GetType();
        var key = (type, aspectName);
        var prop = PropertyCache.GetOrAdd(key, static k => k.type.GetProperty(k.name));
        return prop?.GetValue(row);
    }

    private void PutValueByName(object row, string aspectName, object? value)
    {
        if (ReadOnly || string.IsNullOrWhiteSpace(aspectName)) return;
        if (row is DataRow dr && dr.Table.Columns.Contains(aspectName)) { dr[aspectName] = value ?? DBNull.Value; return; }
        if (row is IDictionary<string, object?> genericDict) { genericDict[aspectName] = value; return; }
        if (row is IDictionary dict) { dict[aspectName] = value; return; }
        var type = row.GetType();
        var key = (type, aspectName);
        var prop = PropertyCache.GetOrAdd(key, static k => k.type.GetProperty(k.name));
        if (prop == null || !prop.CanWrite) return;
        try { prop.SetValue(row, value); } catch { }
    }

    public object? GetValue(object row)
    {
        if (UseFormula && !string.IsNullOrWhiteSpace(Formula))
        {
            object? formulaValue = TryEvaluateFormula(row);
            if (formulaValue != null) return formulaValue;
        }
        if (AspectGetter != null) return AspectGetter(row);
        if (string.IsNullOrWhiteSpace(AspectName)) return row;
        if (row is DataRow dr && dr.Table.Columns.Contains(AspectName)) return dr[AspectName];
        if (row is IDictionary<string, object?> genericDict && genericDict.TryGetValue(AspectName, out var genericValue)) return genericValue;
        if (row is IDictionary dict && dict.Contains(AspectName)) return dict[AspectName];
        var type = row.GetType();
        var key = (type, AspectName);
        var prop = PropertyCache.GetOrAdd(key, static k => k.type.GetProperty(k.name));
        return prop?.GetValue(row);
    }

    private object? TryEvaluateFormula(object row)
    {
        try
        {
            return new ViewGridExpressionEngine().Evaluate(row, Formula);
        }
        catch
        {
            return null;
        }
    }

    private bool EvaluateSimpleCondition(object row, string condition)
    {
        string[] operators = { ">=", "<=", "!=", "==", ">", "<" };
        foreach (string op in operators)
        {
            int index = condition.IndexOf(op, StringComparison.Ordinal);
            if (index <= 0) continue;
            string leftName = condition.Substring(0, index).Trim();
            string rightText = condition.Substring(index + op.Length).Trim().Trim('\'', '"');
            object? left = GetValueByName(row, leftName);
            string leftText = Convert.ToString(left) ?? string.Empty;
            if (double.TryParse(leftText, NumberStyles.Any, CultureInfo.CurrentCulture, out double l) &&
                double.TryParse(rightText, NumberStyles.Any, CultureInfo.CurrentCulture, out double r))
            {
                return op switch
                {
                    ">=" => l >= r,
                    "<=" => l <= r,
                    "!=" => Math.Abs(l - r) > double.Epsilon,
                    "==" => Math.Abs(l - r) <= double.Epsilon,
                    ">" => l > r,
                    "<" => l < r,
                    _ => false
                };
            }
            int cmp = string.Compare(leftText, rightText, StringComparison.CurrentCultureIgnoreCase);
            return op switch
            {
                "!=" => cmp != 0,
                "==" => cmp == 0,
                ">" => cmp > 0,
                "<" => cmp < 0,
                ">=" => cmp >= 0,
                "<=" => cmp <= 0,
                _ => false
            };
        }
        return false;
    }

    public string GetStringValue(object row)
    {
        var value = GetValue(row);
        if (AspectToStringConverter != null)
        {
            try { return AspectToStringConverter(value) ?? string.Empty; }
            catch { }
        }
        if (!string.IsNullOrWhiteSpace(AspectToStringFormat))
        {
            try { return string.Format(AspectToStringFormat, value); }
            catch { }
        }
        return Convert.ToString(value) ?? string.Empty;
    }

    public void PutValue(object row, object? value)
    {
        if (ReadOnly) return;
        if (Required && (value == null || value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value)))) return;
        if (MaxLength > 0 && value is string text && text.Length > MaxLength) value = text.Substring(0, MaxLength);
        if (AspectPutter != null) { AspectPutter(row, value); return; }
        if (string.IsNullOrWhiteSpace(AspectName)) return;
        if (row is DataRow dr && dr.Table.Columns.Contains(AspectName)) { dr[AspectName] = value ?? DBNull.Value; return; }
        if (row is IDictionary<string, object?> genericDict) { genericDict[AspectName] = value; return; }
        if (row is IDictionary dict) { dict[AspectName] = value; return; }
        var type = row.GetType();
        var key = (type, AspectName);
        var prop = PropertyCache.GetOrAdd(key, static k => k.type.GetProperty(k.name));
        if (prop == null || !prop.CanWrite) return;
        try
        {
            var target = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            object? converted = value;
            if (value != null && !target.IsInstanceOfType(value)) converted = Convert.ChangeType(value, target);
            prop.SetValue(row, converted);
        }
        catch { }
    }

    public bool ShouldSerializeVisible() => Visible != true;
    public void ResetVisible() => Visible = true;
    public bool ShouldSerializeDefaultVisible() => DefaultVisible != true;
    public void ResetDefaultVisible() => DefaultVisible = true;

    public bool ShouldSerializePrivateColumn() => PrivateColumn;
    public void ResetPrivateColumn() => PrivateColumn = false;

    public bool ShouldSerializeName() => !string.IsNullOrWhiteSpace(Name);
    public bool ShouldSerializeAspectName() => !string.IsNullOrWhiteSpace(AspectName);
    public bool ShouldSerializeMinimumWidth() => MinimumWidth >= 0;
    public bool ShouldSerializeMaximumWidth() => MaximumWidth >= 0;
    public bool ShouldSerializeDisplayIndex() => DisplayIndex >= 0;
    public bool ShouldSerializeToolTipText() => !string.IsNullOrWhiteSpace(ToolTipText);
    public bool ShouldSerializeAspectToStringFormat() => !string.IsNullOrWhiteSpace(AspectToStringFormat);
    public bool ShouldSerializeHeaderImageKey() => !string.IsNullOrWhiteSpace(HeaderImageKey);
    public bool ShouldSerializeHeaderImageIndex() => HeaderImageIndex >= 0;
    public bool ShouldSerializeHeaderToolTipText() => !string.IsNullOrWhiteSpace(HeaderToolTipText);
    public bool ShouldSerializeImageKey() => ImageKey != null;
    public bool ShouldSerializeImageIndex() => ImageIndex >= 0;
    public bool ShouldSerializeImageAspectName() => !string.IsNullOrWhiteSpace(ImageAspectName);
    public bool ShouldSerializeButtonPadding() => ButtonPadding != Padding.Empty;
    public bool ShouldSerializeButtonSize() => ButtonSize != Size.Empty;
    public bool ShouldSerializeCellPadding() => CellPadding != Padding.Empty;
    public bool ShouldSerializeHeaderFont() => HeaderFont != null;
    public bool ShouldSerializeSearchAlias() => !string.IsNullOrWhiteSpace(SearchAlias);

    /// <summary>
    /// Designer collection list display. Matches the ViewGrid convention:
    /// olvColumnName (AspectName). For ViewGrid this becomes glvColumnName (AspectName).
    /// </summary>
    public override string ToString() => DisplayText;
}

public sealed class ViewGridColumnTypeConverter : ExpandableObjectConverter
{
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        if (destinationType == typeof(string)) return true;
        return base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is ViewGridColumn column)
            return column.DisplayText;

        return base.ConvertTo(context, culture, value, destinationType);
    }
}

