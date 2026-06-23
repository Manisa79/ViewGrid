using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using Microsoft.DotNet.DesignTools.Designers;
using Microsoft.DotNet.DesignTools.Designers.Actions;
using DesignerActionList = Microsoft.DotNet.DesignTools.Designers.Actions.DesignerActionList;
using DesignerActionListCollection = Microsoft.DotNet.DesignTools.Designers.Actions.DesignerActionListCollection;
using DesignerActionItemCollection = Microsoft.DotNet.DesignTools.Designers.Actions.DesignerActionItemCollection;
using DesignerActionMethodItem = Microsoft.DotNet.DesignTools.Designers.Actions.DesignerActionMethodItem;
using DesignerActionPropertyItem = Microsoft.DotNet.DesignTools.Designers.Actions.DesignerActionPropertyItem;
using DesignerActionUIService = Microsoft.DotNet.DesignTools.Designers.Actions.DesignerActionUIService;
using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Theming;

namespace ViewGrid.Design;

/// <summary>
/// Visual Studio Designer smart-tag support for ViewGridControl.
/// It exposes the most frequently changed ListView/ViewGrid-like settings at design time.
/// </summary>
public sealed class ViewGridControlDesigner : Microsoft.DotNet.DesignTools.Designers.ControlDesigner
{
    private DesignerActionListCollection? _actionLists;
    private DesignerVerbCollection? _verbs;
    private ViewGridControlActionList? _registeredActionList;

    public override DesignerActionListCollection ActionLists
    {
        get
        {
            _actionLists ??= new DesignerActionListCollection { CreateActionList() };
            return _actionLists;
        }
    }

    public override DesignerVerbCollection Verbs
    {
        get
        {
            _verbs ??= new DesignerVerbCollection
            {
                new DesignerVerb("ViewGridControl: ViewGrid Kolon Editörünü Aç...", (_, _) => CreateActionList().EditColumns()),
                new DesignerVerb("ViewGridControl: Varsayılan Kolon Ekle", (_, _) => CreateActionList().AddDefaultColumns()),
                new DesignerVerb("ViewGridControl: Checkbox Seçimini Aç", (_, _) => CreateActionList().AddCheckBoxColumn()),
                new DesignerVerb("ViewGridControl: Ana Kapsayıcıda Yerleştir", (_, _) => CreateActionList().DockFill()),
                new DesignerVerb("ViewGridControl: Aktif Senaryoyu Uygula", (_, _) => CreateActionList().ApplyActiveScenario()),
                new DesignerVerb("ViewGridControl: Designer Açık Temayı Uygula", (_, _) => CreateActionList().UseLightTheme()),
                new DesignerVerb("ViewGridControl: Önizlemeyi Yenile", (_, _) => CreateActionList().RefreshPreview())
            };
            return _verbs;
        }
    }

    public override void Initialize(IComponent component)
    {
        base.Initialize(component);
        AutoResizeHandles = true;

        if (component is not ViewGridControl)
            return;

        _registeredActionList = new ViewGridControlActionList(component);
        _actionLists = null;

        // Önemli: ActionLists override tek kaynak olarak bırakılmalı.
        // DesignerActionService.Add ile ayrıca kayıt yapılırsa VS SmartTag panelinde
        // aynı görev grubu iki kez görünür. .NET 10 out-of-process designer tarafında
        // görünürlük için SDK tabanlı ControlDesigner + ActionLists yeterlidir.
    }

    public override void InitializeNewComponent(IDictionary defaultValues)
    {
        base.InitializeNewComponent(defaultValues);

        if (Component is ViewGridControl viewgrid)
        {
            viewgrid.ShowHeader = true;
            viewgrid.ShowGridLines = true;
            viewgrid.FullRowSelect = true;
            viewgrid.UseUnifiedThemeVisuals = true;
            viewgrid.AutoApplyThemeToColumnHeaders = true;
            viewgrid.AutoEnsureReadableTextColors = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _registeredActionList = null;
        }

        base.Dispose(disposing);
    }

    private ViewGridControlActionList CreateActionList()
    {
        if (_registeredActionList != null)
            return _registeredActionList;

        if (Component == null)
            throw new InvalidOperationException("ViewGridControl designer component is not initialized.");

        _registeredActionList = new ViewGridControlActionList(Component);
        return _registeredActionList;
    }
}

internal sealed class ActionListServiceProvider : IServiceProvider
{
    private readonly Func<Type, object?> _getService;

    public ActionListServiceProvider(Func<Type, object?> getService)
    {
        _getService = getService;
    }

    public object? GetService(Type serviceType)
        => _getService(serviceType);
}

public sealed class ViewGridControlActionList : DesignerActionList
{
    private readonly global::ViewGrid.Core.ViewGridControl _control;
    private readonly DesignerActionUIService? _uiService;
    private bool _editingColumns;
    private System.Windows.Forms.Timer? _editColumnsTimer;
    private Rectangle? _lastNormalBounds;
    private AnchorStyles? _lastNormalAnchor;

    public ViewGridControlActionList(IComponent component) : base(component)
    {
        _control = (global::ViewGrid.Core.ViewGridControl)component;
        _uiService = GetService(typeof(DesignerActionUIService)) as DesignerActionUIService;
    }

    public ViewGridDataMode Mode
    {
        get => _control.Mode;
        set => SetProperty(nameof(_control.Mode), value);
    }

    public ViewGridMode ViewMode
    {
        get => _control.ViewMode;
        set => SetProperty(nameof(_control.ViewMode), value);
    }

    public int RowHeight
    {
        get => _control.RowHeight;
        set => SetProperty(nameof(_control.RowHeight), value);
    }

    public ViewGridScenario ActiveScenario
    {
        get => _control.ActiveScenario;
        set => SetProperty(nameof(_control.ActiveScenario), value);
    }

    public bool ShowStateMenuItems
    {
        get => _control.ShowStateMenuItems;
        set => SetProperty(nameof(_control.ShowStateMenuItems), value);
    }

    public bool ShowScenarioMenuItems
    {
        get => _control.ShowScenarioMenuItems;
        set => SetProperty(nameof(_control.ShowScenarioMenuItems), value);
    }

    public string AutoStateFilePath
    {
        get => _control.AutoStateFilePath;
        set => SetProperty(nameof(_control.AutoStateFilePath), value);
    }

    public bool AutoLoadStateOnCreate
    {
        get => _control.AutoLoadStateOnCreate;
        set => SetProperty(nameof(_control.AutoLoadStateOnCreate), value);
    }

    public bool AutoSaveStateOnDispose
    {
        get => _control.AutoSaveStateOnDispose;
        set => SetProperty(nameof(_control.AutoSaveStateOnDispose), value);
    }

    public void ApplyActiveScenario()
    {
        _control.ApplyActiveScenario();
        RefreshDesignerPreview();
        _uiService?.Refresh(_control);
    }

    public bool ShowHeader
    {
        get => _control.ShowHeader;
        set => SetProperty(nameof(_control.ShowHeader), value);
    }

    public bool ShowGridLines
    {
        get => _control.ShowGridLines;
        set => SetProperty(nameof(_control.ShowGridLines), value);
    }

    public bool AlternateRows
    {
        get => _control.AlternateRows;
        set => SetProperty(nameof(_control.AlternateRows), value);
    }

    public bool FullRowSelect
    {
        get => _control.FullRowSelect;
        set => SetProperty(nameof(_control.FullRowSelect), value);
    }

    public bool MultiSelect
    {
        get => _control.MultiSelect;
        set => SetProperty(nameof(_control.MultiSelect), value);
    }

    public bool CheckBoxes
    {
        get => _control.CheckBoxes;
        set => SetProperty(nameof(_control.CheckBoxes), value);
    }

    public bool SelectionHeaderCheckBox
    {
        get => _control.Columns.VisibleColumns.FirstOrDefault()?.HeaderCheckBox ?? false;
        set
        {
            var column = _control.Columns.VisibleColumns.FirstOrDefault();
            if (column == null) return;
            var host = GetDesignerHost();
            using var tx = host?.CreateTransaction(value ? "ViewGridControl header checkbox açıldı" : "ViewGridControl header checkbox kapatıldı");
            RaiseChanging(nameof(_control.Columns));
            column.HeaderCheckBox = value;
            column.HeaderCheckBoxUpdatesRowCheckBoxes = true;
            RaiseChanged(nameof(_control.Columns));
            tx?.Commit();
            RefreshDesignerPreview();
            _uiService?.Refresh(_control);
        }
    }

    public bool ShowGroups
    {
        get => _control.ShowGroups;
        set => SetProperty(nameof(_control.ShowGroups), value);
    }

    public bool HotTracking
    {
        get => _control.HotTracking;
        set => SetProperty(nameof(_control.HotTracking), value);
    }

    public ImageList? SmallImageList
    {
        get => _control.SmallImageList;
        set => SetProperty(nameof(_control.SmallImageList), value);
    }

    public ImageList? LargeImageList
    {
        get => _control.LargeImageList;
        set => SetProperty(nameof(_control.LargeImageList), value);
    }

    public ImageList? StateImageList
    {
        get => _control.StateImageList;
        set => SetProperty(nameof(_control.StateImageList), value);
    }

    public ViewGridColumnCollection Columns
    {
        get => _control.Columns;
        set
        {
            // Koleksiyon nesnesi ViewGridControl içinde sabittir; Visual Studio SmartTag
            // property item yalnızca standart koleksiyon editörünü açmak için kullanılır.
        }
    }

    public bool ShowFilterMenu
    {
        get => _control.ShowFilterMenu;
        set => SetProperty(nameof(_control.ShowFilterMenu), value);
    }

    public ViewGridHeaderContextMenuBehavior HeaderContextMenuBehavior
    {
        get => _control.HeaderContextMenuBehavior;
        set => SetProperty(nameof(_control.HeaderContextMenuBehavior), value);
    }


    public ViewGridMenuProfile MenuProfile
    {
        get => _control.MenuProfile;
        set => SetProperty(nameof(_control.MenuProfile), value);
    }

    public ViewGridMenuGroups HeaderMenuGroups
    {
        get => _control.HeaderMenuGroups;
        set => SetProperty(nameof(_control.HeaderMenuGroups), value);
    }

    public ViewGridMenuGroups BodyMenuGroups
    {
        get => _control.BodyMenuGroups;
        set => SetProperty(nameof(_control.BodyMenuGroups), value);
    }

    public bool UseBuiltInHeaderMenu
    {
        get => _control.UseBuiltInHeaderMenu;
        set => SetProperty(nameof(_control.UseBuiltInHeaderMenu), value);
    }

    public bool UseBuiltInBodyMenu
    {
        get => _control.UseBuiltInBodyMenu;
        set => SetProperty(nameof(_control.UseBuiltInBodyMenu), value);
    }

    public bool CloseHeaderContextMenuBeforeOpeningFilterPopup
    {
        get => _control.CloseHeaderContextMenuBeforeOpeningFilterPopup;
        set => SetProperty(nameof(_control.CloseHeaderContextMenuBeforeOpeningFilterPopup), value);
    }

    public bool ShowHeaderMenuSortItems
    {
        get => _control.ShowHeaderMenuSortItems;
        set => SetProperty(nameof(_control.ShowHeaderMenuSortItems), value);
    }

    public bool ShowHeaderMenuLayoutItems
    {
        get => _control.ShowHeaderMenuLayoutItems;
        set => SetProperty(nameof(_control.ShowHeaderMenuLayoutItems), value);
    }

    public bool ShowHeaderMenuGroupingItems
    {
        get => _control.ShowHeaderMenuGroupingItems;
        set => SetProperty(nameof(_control.ShowHeaderMenuGroupingItems), value);
    }

    public bool EnableCellEditing
    {
        get => _control.EnableCellEditing;
        set => SetProperty(nameof(_control.EnableCellEditing), value);
    }

    public bool AllowEditAllCells
    {
        get => _control.AllowEditAllCells;
        set => SetProperty(nameof(_control.AllowEditAllCells), value);
    }

    public Keys CellEditActivationKey
    {
        get => _control.CellEditActivationKey;
        set => SetProperty(nameof(_control.CellEditActivationKey), value);
    }

    public bool ShowEditCellMenuItem
    {
        get => _control.ShowEditCellMenuItem;
        set => SetProperty(nameof(_control.ShowEditCellMenuItem), value);
    }

    public string EditCellMenuText
    {
        get => _control.EditCellMenuText;
        set => SetProperty(nameof(_control.EditCellMenuText), value);
    }

    public string EditCellMenuShortcutText
    {
        get => _control.EditCellMenuShortcutText;
        set => SetProperty(nameof(_control.EditCellMenuShortcutText), value);
    }

    public bool KeyboardSpaceTogglesCheckBoxes
    {
        get => _control.KeyboardSpaceTogglesCheckBoxes;
        set => SetProperty(nameof(_control.KeyboardSpaceTogglesCheckBoxes), value);
    }

    public bool KeyboardSpaceTogglesSelectedRows
    {
        get => _control.KeyboardSpaceTogglesSelectedRows;
        set => SetProperty(nameof(_control.KeyboardSpaceTogglesSelectedRows), value);
    }

    public bool EnableColumnResize
    {
        get => _control.EnableColumnResize;
        set => SetProperty(nameof(_control.EnableColumnResize), value);
    }

    public bool EnableColumnAutoResizeOnDoubleClick
    {
        get => _control.EnableColumnAutoResizeOnDoubleClick;
        set => SetProperty(nameof(_control.EnableColumnAutoResizeOnDoubleClick), value);
    }

    public int AutoResizeMaxWidth
    {
        get => _control.AutoResizeMaxWidth;
        set => SetProperty(nameof(_control.AutoResizeMaxWidth), value);
    }

    public bool AutoResizeIncludeHeader
    {
        get => _control.AutoResizeIncludeHeader;
        set => SetProperty(nameof(_control.AutoResizeIncludeHeader), value);
    }

    public bool EnableGrouping
    {
        get => _control.EnableGrouping;
        set => SetProperty(nameof(_control.EnableGrouping), value);
    }

    public bool ShowSummaryFooter
    {
        get => _control.ShowSummaryFooter;
        set => SetProperty(nameof(_control.ShowSummaryFooter), value);
    }

    public bool FollowWindowsTheme
    {
        get => _control.FollowWindowsTheme;
        set => SetProperty(nameof(_control.FollowWindowsTheme), value);
    }

    public ViewGridThemePreset ThemePreset
    {
        get => _control.ThemePreset;
        set => SetProperty(nameof(_control.ThemePreset), value);
    }

    public ViewGridDesignTimeThemePreview DesignTimeThemePreview
    {
        get => _control.DesignTimeThemePreview;
        set => SetProperty(nameof(_control.DesignTimeThemePreview), value);
    }

    public bool AutoThemeFromParent
    {
        get => _control.AutoThemeFromParent;
        set => SetProperty(nameof(_control.AutoThemeFromParent), value);
    }

    public bool DesignTimeSampleData
    {
        get => _control.DesignTimeSampleData;
        set => SetProperty(nameof(_control.DesignTimeSampleData), value);
    }

    public bool EnableDesignTimeThemeSync
    {
        get => _control.EnableDesignTimeThemeSync;
        set => SetProperty(nameof(_control.EnableDesignTimeThemeSync), value);
    }

    public bool DesignTimeFollowParentTheme
    {
        get => _control.DesignTimeFollowParentTheme;
        set => SetProperty(nameof(_control.DesignTimeFollowParentTheme), value);
    }

    public bool DesignTimeThemeSyncMenus
    {
        get => _control.DesignTimeThemeSyncMenus;
        set => SetProperty(nameof(_control.DesignTimeThemeSyncMenus), value);
    }


    public bool ShowEmptyListMessage
    {
        get => _control.ShowEmptyListMessage;
        set => SetProperty(nameof(_control.ShowEmptyListMessage), value);
    }

    public bool EnableModernEmptyState
    {
        get => _control.EnableModernEmptyState;
        set => SetProperty(nameof(_control.EnableModernEmptyState), value);
    }

    public void EditColumns()
    {
        if (_editingColumns)
            return;

        // SmartTag popup kapanmadan native CollectionEditor açılırsa VS bazen pencereyi
        // arkada bırakıyor veya hiç açmıyor. Timer field olarak tutulur; local timer
        // GC tarafından toplanırsa Tick gelmeyebiliyordu.
        _editColumnsTimer?.Stop();
        _editColumnsTimer?.Dispose();
        _editColumnsTimer = new System.Windows.Forms.Timer { Interval = 160 };
        _editColumnsTimer.Tick += (_, _) =>
        {
            _editColumnsTimer?.Stop();
            _editColumnsTimer?.Dispose();
            _editColumnsTimer = null;
            EditColumnsCore();
        };
        _editColumnsTimer.Start();
    }

    private void EditColumnsCore()
    {
        if (_editingColumns)
            return;

        _editingColumns = true;
        try
        {
            var columnsProperty = TypeDescriptor.GetProperties(_control)[nameof(_control.Columns)];
            if (columnsProperty == null)
                return;

            var provider = new DesignerServiceProvider(_control.Site, GetService);
            var before = GetColumnsSnapshot();

            // v26.61: Tek editor mimarisi. Smart Menü ve Properties > Columns aynı
            // ViewGridColumnCollectionEditor formunu açar. VS native CollectionEditor
            // fallback'i kaldırıldı; böylece iki farklı editor/sıra/serialization yolu
            // birbirine karışmaz.
            if (ViewGridColumnCollectionEditor.EditColumns(_control.Columns, _control, columnsProperty, provider, centerScreen: true))
            {
                var after = GetColumnsSnapshot();
                if (!string.Equals(before, after, StringComparison.Ordinal))
                {
                    RaiseChanged(nameof(_control.Columns));
                    RefreshDesignerPreview();
                    _control.RebuildColumns();
                    _control.PerformLayout();
                    _control.Invalidate();
                    _uiService?.Refresh(_control);
                }
            }
        }
        finally
        {
            _editingColumns = false;
        }
    }

    private DialogResult ShowDesignerDialog(Form form)
    {
        // SmartTag içinden açılan pencere designer ana penceresine owner verilirse
        // bazı VS/.NET 10 oturumlarında arkada kalıp IDE'yi donmuş gibi gösteriyor.
        // Bu nedenle editorü bağımsız, CenterScreen ve kısa süre TopMost olarak açıyoruz.
        form.StartPosition = FormStartPosition.CenterScreen;
        form.ShowInTaskbar = true;
        form.TopMost = true;
        form.Shown += (_, _) =>
        {
            form.Activate();
            form.BringToFront();
            form.TopMost = false;
        };

        return form.ShowDialog();
    }

    public void AddCheckBoxColumn()
    {
        // v25.80: ViewGrid uyumlu seçim checkbox'ı ayrı kolon oluşturmaz.
        // İlk görünür veri kolonunun içinde çizilir. Bu nedenle SmartTag artık
        // yeni kolon eklemek yerine CheckBoxes özelliğini açar.
        var host = GetDesignerHost();
        using var tx = host?.CreateTransaction("ViewGridControl seçim checkbox'ı açıldı");
        RaiseChanging(nameof(_control.CheckBoxes));

        SetProperty(nameof(_control.CheckBoxes), true);

        var firstColumn = _control.Columns.VisibleColumns.FirstOrDefault();
        if (firstColumn != null)
        {
            // Header checkbox is optional. This SmartTag action enables row checkboxes
            // only; use the separate "Header Checkbox" quick option or the column
            // editor's HeaderCheckBox property for select-all in the header.
            firstColumn.HeaderCheckBoxUpdatesRowCheckBoxes = true;
            if (firstColumn.Width < 48)
                firstColumn.Width = 48;
        }

        RaiseChanged(nameof(_control.CheckBoxes));
        tx?.Commit();
        RefreshDesignerPreview();
        _uiService?.Refresh(_control);
    }

    public void AddDefaultColumns()
    {
        var host = GetDesignerHost();
        using var tx = host?.CreateTransaction("ViewGridControl varsayılan kolonlar eklendi");
        RaiseChanging(nameof(_control.Columns));
        if (_control.Columns.Count == 0)
        {
            _control.Columns.Add(CreateDesignerColumn("Id", "Id", 80));
            _control.Columns.Add(CreateDesignerColumn("Ad", "Name", 180));
            _control.Columns.Add(CreateDesignerColumn("Durum", "Status", 120));
            _control.Columns.Add(CreateDesignerColumn("Tarih", "Date", 140));
        }
        else
        {
            _control.Columns.Add(CreateDesignerColumn("Yeni Kolon", "NewColumn", 140));
        }
        RaiseChanged(nameof(_control.Columns));
        tx?.Commit();
        RefreshDesignerPreview();
        _uiService?.Refresh(_control);
    }

    private GLVColumn CreateDesignerColumn(string text, string aspectName, int width)
    {
        var name = CreateUniqueDesignerColumnName(aspectName, text);

        // Kolonları designer host component'i olarak üretmiyoruz.
        // Aksi halde Visual Studio formun altındaki component tray alanına
        // colColumn1/colColumn2 gibi ayrı component ikonları ekler.
        return new GLVColumn(text, aspectName, width)
        {
            Name = name,
            DefaultWidth = width
        };
    }

    private string CreateUniqueDesignerColumnName(string aspectName, string text)
    {
        var baseName = ViewGridColumnNameHelper.CreateNameFromAspectOrText(aspectName, text, _control.Columns.Count + 1);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in _control.Columns)
            if (!string.IsNullOrWhiteSpace(column.Name)) used.Add(column.Name);

        if (GetDesignerHost()?.Container != null)
        {
            foreach (IComponent component in GetDesignerHost()!.Container.Components)
                if (!string.IsNullOrWhiteSpace(component.Site?.Name)) used.Add(component.Site.Name);
        }

        if (!used.Contains(baseName)) return baseName;
        int i = 2;
        while (used.Contains(baseName + i.ToString(System.Globalization.CultureInfo.InvariantCulture))) i++;
        return baseName + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public void ClearColumns()
    {
        using var tx = GetDesignerHost()?.CreateTransaction("ViewGridControl kolonları temizlendi");
        RaiseChanging(nameof(_control.Columns));
        _control.Columns.Clear();
        RaiseChanged(nameof(_control.Columns));
        tx?.Commit();
        RefreshDesignerPreview();
        _uiService?.Refresh(_control);
    }

    public void AutoSizeControl()
    {
        SetProperty(nameof(_control.Width), Math.Max(_control.Width, 620));
        SetProperty(nameof(_control.Height), Math.Max(_control.Height, 340));
    }

    public void DockFill()
    {
        if (_control.Dock != DockStyle.Fill)
        {
            _lastNormalBounds = _control.Bounds;
            _lastNormalAnchor = _control.Anchor;
        }

        SetProperty(nameof(_control.Anchor), AnchorStyles.Top | AnchorStyles.Left);
        SetProperty(nameof(_control.Dock), DockStyle.Fill);
        _uiService?.Refresh(_control);
    }

    public void DockNone()
    {
        SetProperty(nameof(_control.Dock), DockStyle.None);

        if (_lastNormalAnchor.HasValue)
            SetProperty(nameof(_control.Anchor), _lastNormalAnchor.Value);
        else
            SetProperty(nameof(_control.Anchor), AnchorStyles.Top | AnchorStyles.Left);

        if (_lastNormalBounds.HasValue && _lastNormalBounds.Value.Width > 40 && _lastNormalBounds.Value.Height > 40)
        {
            SetProperty(nameof(_control.Location), _lastNormalBounds.Value.Location);
            SetProperty(nameof(_control.Size), _lastNormalBounds.Value.Size);
        }
        else
        {
            SetProperty(nameof(_control.Width), Math.Max(_control.Width, 620));
            SetProperty(nameof(_control.Height), Math.Max(_control.Height, 340));
        }

        _uiService?.Refresh(_control);
    }

    public void ToggleDockInParent()
    {
        if (_control.Dock == DockStyle.Fill)
            DockNone();
        else
            DockFill();

        _uiService?.Refresh(_control);
    }

    public void RefreshPreview()
    {
        _control.RefreshDesignTimePreview();
        _uiService?.Refresh(_control);
    }

    public void SetObjectMode() => SetProperty(nameof(_control.Mode), ViewGridDataMode.Object);
    public void SetDataTableMode() => SetProperty(nameof(_control.Mode), ViewGridDataMode.DataTable);
    public void SetVirtualMode() => SetProperty(nameof(_control.Mode), ViewGridDataMode.Virtual);
    public void SetTreeMode() => SetProperty(nameof(_control.Mode), ViewGridDataMode.Tree);
    public void SetTileMode() => SetProperty(nameof(_control.Mode), ViewGridDataMode.Tile);
    public void UseAutoTheme() => SetProperty(nameof(_control.DesignTimeThemePreview), ViewGridDesignTimeThemePreview.Auto);
    public void UseLightTheme() => SetProperty(nameof(_control.DesignTimeThemePreview), ViewGridDesignTimeThemePreview.Light);
    public void UseDarkTheme() => SetProperty(nameof(_control.DesignTimeThemePreview), ViewGridDesignTimeThemePreview.Dark);

    public void MenuFull() => ApplyMenuProfile(ViewGridMenuProfile.Full);
    public void MenuStandard() => ApplyMenuProfile(ViewGridMenuProfile.Standard);
    public void MenuMinimal() => ApplyMenuProfile(ViewGridMenuProfile.Minimal);
    public void MenuReadOnly() => ApplyMenuProfile(ViewGridMenuProfile.ReadOnly);
    public void MenuNone() => ApplyMenuProfile(ViewGridMenuProfile.None);

    private void ApplyMenuProfile(ViewGridMenuProfile profile)
    {
        var prop = TypeDescriptor.GetProperties(_control)[nameof(_control.MenuProfile)];
        RaiseChanging(nameof(_control.MenuProfile));
        _control.ApplyMenuProfile(profile);
        RaiseChanged(nameof(_control.MenuProfile));
        _uiService?.Refresh(_control);
    }

    public override DesignerActionItemCollection GetSortedActionItems()
    {
        // SmartTag kısa tutulmalı: .NET designer popup'ı ekran yüksekliğine göre
        // kaydırma vermiyor ve çok uzun listeler kullanımı zorlaştırıyor.
        // Ana işlemler burada, detaylı özellikler Properties penceresinde kalır.
        var dockText = _control.Dock == DockStyle.Fill
            ? "Ana Kapsayıcıdan Ayır"
            : "Ana Kapsayıcıda Yerleştir";

        var items = new DesignerActionItemCollection
        {
            new DesignerActionMethodItem(this, nameof(EditColumns), "ViewGrid Kolon Editörünü Aç...", "1. Kolonlar", "ViewGrid'ye özgü kolon düzenleme penceresini açar.", true),
            new DesignerActionMethodItem(this, nameof(AddDefaultColumns), "Varsayılan Kolon Ekle", "1. Kolonlar", "Hızlı test/tasarım için kolon ekler.", true),
            new DesignerActionMethodItem(this, nameof(ClearColumns), "Sütunları Temizle", "1. Kolonlar", "Tüm kolonları temizler.", true),

            new DesignerActionMethodItem(this, nameof(ToggleDockInParent), dockText, "2. Yerleşim", "Dock.Fill ile normal yerleşim arasında geçiş yapar.", true),
            new DesignerActionMethodItem(this, nameof(AutoSizeControl), "Önerilen Boyuta Getir", "2. Yerleşim", "Kontrolü kullanışlı bir tasarım boyutuna getirir.", true),
            new DesignerActionMethodItem(this, nameof(RefreshPreview), "Önizlemeyi Yenile", "2. Yerleşim", "Designer önizlemesini yeniler.", true),

            new DesignerActionPropertyItem(nameof(Mode), "Mod:", "3. Ana Görünüm", "Object, DataTable, Virtual, Tree veya Tile çalışma modunu seçer."),
            new DesignerActionPropertyItem(nameof(ViewMode), "Görünüm:", "3. Ana Görünüm", "Details, List, Tile veya ikon görünümünü seçer."),
            new DesignerActionPropertyItem(nameof(ActiveScenario), "Senaryo:", "3. Ana Görünüm", "MasterData, ticket, BOM, timeline gibi hazır görünüm senaryosu seçer."),
            new DesignerActionMethodItem(this, nameof(ApplyActiveScenario), "Senaryoyu Uygula", "3. Ana Görünüm", "Seçili senaryonun görünüm ayarlarını uygular.", true),
            new DesignerActionPropertyItem(nameof(RowHeight), "Satır Yüksekliği:", "3. Ana Görünüm", "Satır yüksekliğini ayarlar."),
            new DesignerActionPropertyItem(nameof(ShowHeader), "Başlıkları Göster", "3. Ana Görünüm", "Kolon başlıklarını gösterir veya gizler."),
            new DesignerActionPropertyItem(nameof(ShowGridLines), "Grid Çizgileri", "3. Ana Görünüm", "Satır/kolon çizgilerini gösterir."),
            new DesignerActionPropertyItem(nameof(AlternateRows), "Alternatif Satırlar", "3. Ana Görünüm", "Zebra satır görünümünü açar/kapatır."),
            new DesignerActionPropertyItem(nameof(FullRowSelect), "Tüm Satırı Seç", "3. Ana Görünüm", "Seçimin tüm satıra yayılmasını sağlar."),
            new DesignerActionPropertyItem(nameof(MultiSelect), "Çoklu Seçim", "3. Ana Görünüm", "Birden fazla satır seçimini açar/kapatır."),
            new DesignerActionPropertyItem(nameof(CheckBoxes), "Checkbox Kullan", "3. Ana Görünüm", "Satır checkbox kullanımını açar/kapatır."),
            new DesignerActionPropertyItem(nameof(ShowGroups), "Grupları Göster", "3. Ana Görünüm", "Gruplama görünümünü açar/kapatır."),

            new DesignerActionPropertyItem(nameof(SmallImageList), "Küçük ImageList:", "4. ImageList", "Satır ve küçük ikon görselleri için ImageList seçer."),
            new DesignerActionPropertyItem(nameof(LargeImageList), "Büyük ImageList:", "4. ImageList", "Tile, LargeIcons ve büyük ikon görselleri için ImageList seçer."),
            new DesignerActionPropertyItem(nameof(StateImageList), "State ImageList:", "4. ImageList", "Durum görselleri için ImageList seçer."),

            new DesignerActionPropertyItem(nameof(MenuProfile), "Menü Profili:", "5. Menü / Filtre", "Full, Standard, Minimal, ReadOnly veya None menü profilini seçer."),
            new DesignerActionPropertyItem(nameof(UseBuiltInHeaderMenu), "Başlık Menüsü", "5. Menü / Filtre", "ViewGrid'nin dahili başlık menüsünü açar/kapatır."),
            new DesignerActionPropertyItem(nameof(UseBuiltInBodyMenu), "Gövde Menüsü", "5. Menü / Filtre", "ViewGrid'nin dahili gövde menüsünü açar/kapatır."),
            new DesignerActionPropertyItem(nameof(ShowFilterMenu), "Filtre Menüsü", "5. Menü / Filtre", "Kolon filtre menüsünü açar/kapatır."),
            new DesignerActionPropertyItem(nameof(ShowStateMenuItems), "State Menüsü", "5. Menü / Filtre", "State kaydet/yükle/preset menüsünü açar/kapatır."),
            new DesignerActionPropertyItem(nameof(ShowScenarioMenuItems), "Senaryo Menüsü", "5. Menü / Filtre", "Hazır görünüm senaryoları menüsünü açar/kapatır."),
            new DesignerActionPropertyItem(nameof(AutoStateFilePath), "State Dosyası:", "5. Menü / Filtre", "Otomatik veya varsayılan state dosya yolunu belirler."),
            new DesignerActionPropertyItem(nameof(AutoLoadStateOnCreate), "Açılışta State Yükle", "5. Menü / Filtre", "Kontrol oluşturulurken varsayılan state dosyasını yükler."),
            new DesignerActionPropertyItem(nameof(AutoSaveStateOnDispose), "Kapanışta State Kaydet", "5. Menü / Filtre", "Kontrol dispose edilirken varsayılan state dosyasını kaydeder."),

            new DesignerActionPropertyItem(nameof(EnableCellEditing), "Hücre Edit Aktif", "6. Edit / Klavye", "Hücre düzenlemeyi açar/kapatır."),
            new DesignerActionPropertyItem(nameof(AllowEditAllCells), "Tüm Hücreler Edit", "6. Edit / Klavye", "Tüm hücrelerde edit izni verir."),
            new DesignerActionPropertyItem(nameof(CellEditActivationKey), "Edit Tuşu:", "6. Edit / Klavye", "Hücre edit aktivasyon tuşunu seçer."),
            new DesignerActionPropertyItem(nameof(KeyboardSpaceTogglesCheckBoxes), "Space Checkbox Değiştirir", "6. Edit / Klavye", "Space tuşuyla checkbox değiştirmeyi açar/kapatır."),

            new DesignerActionPropertyItem(nameof(FollowWindowsTheme), "Windows Temasını İzle", "7. Tema / Designer", "Sistem temasını takip eder."),
            new DesignerActionPropertyItem(nameof(ThemePreset), "Tema Preseti:", "7. Tema / Designer", "ViewGrid tema presetini seçer."),
            new DesignerActionPropertyItem(nameof(DesignTimeThemePreview), "Designer Tema:", "7. Tema / Designer", "Designer'da Auto, Light veya Dark önizleme seçer."),
            new DesignerActionPropertyItem(nameof(EnableDesignTimeThemeSync), "Designer Tema Sync", "7. Tema / Designer", "Tasarım zamanında açık/VS designer uyumlu tema uygular."),
            new DesignerActionPropertyItem(nameof(DesignTimeFollowParentTheme), "Parent Tema İzle", "7. Tema / Designer", "Designer Auto temasını üst form/panel renginden türetir."),
            new DesignerActionPropertyItem(nameof(DesignTimeThemeSyncMenus), "Designer Menü Teması", "7. Tema / Designer", "Tasarım zamanında bağlı menüleri de uyumlu temalar."),
            new DesignerActionPropertyItem(nameof(DesignTimeSampleData), "Designer Örnek Veri", "7. Tema / Designer", "Tasarım zamanında örnek veri gösterir."),
            new DesignerActionPropertyItem(nameof(ShowEmptyListMessage), "Boş Mesajı Göster", "7. Tema / Designer", "Liste boşken empty-state mesajını gösterir/gizler."),
            new DesignerActionPropertyItem(nameof(EnableModernEmptyState), "Modern Boş Durum", "7. Tema / Designer", "Modern empty-state kartını açar/kapatır.")
        };

        return items;
    }

    private IDesignerHost? GetDesignerHost() => GetService(typeof(IDesignerHost)) as IDesignerHost;

    private void SetProperty(string propertyName, object? value)
    {
        var prop = TypeDescriptor.GetProperties(_control)[propertyName];
        if (prop == null) return;
        RaiseChanging(propertyName);
        prop.SetValue(_control, value);
        RaiseChanged(propertyName);
        _uiService?.Refresh(_control);
    }

    private void RaiseChanging(string propertyName)
    {
        var prop = TypeDescriptor.GetProperties(_control)[propertyName];
        if (GetService(typeof(IComponentChangeService)) is IComponentChangeService svc)
            svc.OnComponentChanging(_control, prop);
    }

    private void RaiseChanged(string propertyName)
    {
        var prop = TypeDescriptor.GetProperties(_control)[propertyName];
        if (GetService(typeof(IComponentChangeService)) is IComponentChangeService svc)
            svc.OnComponentChanged(_control, prop, null, null);
        RefreshDesignerPreview();
    }


    private void RefreshDesignerPreview()
    {
        _control.RefreshDesignTimePreview();
        _control.Invalidate();
    }

    private string GetColumnsSnapshot()
    {
        return string.Join("|", _control.Columns.Select((c, i) =>
            i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
            (c.Name ?? string.Empty) + ":" +
            (c.AspectName ?? string.Empty) + ":" +
            (c.Header ?? string.Empty) + ":" +
            c.Width.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
            c.Visible.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
            c.Kind.ToString()));
    }

    private sealed class ViewGridTypeDescriptorContext : ITypeDescriptorContext
    {
        private readonly object _instance;
        private readonly PropertyDescriptor? _propertyDescriptor;
        private readonly IServiceProvider? _serviceProvider;

        public ViewGridTypeDescriptorContext(object instance, PropertyDescriptor? propertyDescriptor, IServiceProvider? serviceProvider = null)
        {
            _instance = instance;
            _propertyDescriptor = propertyDescriptor;
            _serviceProvider = serviceProvider;
        }

        public IContainer? Container => (_instance as IComponent)?.Site?.Container;
        public object Instance => _instance;
        public PropertyDescriptor? PropertyDescriptor => _propertyDescriptor;
        public object? GetService(Type serviceType) => _serviceProvider?.GetService(serviceType) ?? (_instance as IComponent)?.Site?.GetService(serviceType);
        public bool OnComponentChanging() => true;
        public void OnComponentChanged() { }
    }

    private sealed class DesignerServiceProvider : IServiceProvider
    {
        private readonly ISite? _site;
        private readonly Func<Type, object?>? _fallback;

        public DesignerServiceProvider(ISite? site, Func<Type, object?>? fallback = null)
        {
            _site = site;
            _fallback = fallback;
        }

        public object? GetService(Type serviceType)
        {
            var service = _site?.GetService(serviceType);
            return service ?? _fallback?.Invoke(serviceType);
        }
    }
}
