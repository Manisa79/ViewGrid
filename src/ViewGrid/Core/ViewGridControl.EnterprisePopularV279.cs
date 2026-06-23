using System.ComponentModel;
using ViewGrid.Formatting;
using ViewGrid.Summary;

namespace ViewGrid.Core;

public enum ViewGridPopularFeaturePreset
{
    Balanced,
    MasterData,
    SupportDesk,
    LargeDataReview,
    DataEntry
}

public partial class ViewGridControl
{
    [Category("ViewGrid - v27.9 Popular Features")]
    [DefaultValue(true)]
    [Description("Popüler grid bileşenlerinde beklenen arama, özet, column chooser, conditional format ve hızlı filtre davranışlarını tek preset altında açar.")]
    public bool EnablePopularFeaturePack { get; set; } = true;

    [Category("ViewGrid - v27.9 Popular Features")]
    [DefaultValue(ViewGridPopularFeaturePreset.Balanced)]
    [Description("MasterData, Support Desk, yoğun veri veya veri giriş ekranları için önerilen ViewGrid popüler özellik preset'i.")]
    public ViewGridPopularFeaturePreset PopularFeaturePreset { get; set; } = ViewGridPopularFeaturePreset.Balanced;

    [Category("ViewGrid - v27.9 Popular Features")]
    [DefaultValue(true)]
    [Description("Ctrl+F modern arama panelini ve aramada sonucu filtreleme seçeneğini varsayılan olarak hazırlar.")]
    public bool PopularFeaturesEnableSearchPanel { get; set; } = true;

    [Category("ViewGrid - v27.9 Popular Features")]
    [DefaultValue(true)]
    [Description("Özet/footer satırını varsayılan olarak etkinleştirir.")]
    public bool PopularFeaturesEnableSummaryFooter { get; set; } = true;

    [Category("ViewGrid - v27.9 Popular Features")]
    [DefaultValue(true)]
    [Description("İlk kolonu dondurma, column chooser ve auto-size menülerini hazır hale getirir.")]
    public bool PopularFeaturesEnableColumnTools { get; set; } = true;

    [Category("ViewGrid - v27.9 Popular Features")]
    [DefaultValue(true)]
    [Description("Filtre popup resize, tooltip, auto-width, advanced filter ve preset davranışlarını açar.")]
    public bool PopularFeaturesEnableFilterTools { get; set; } = true;

    [Category("ViewGrid - v27.9 Popular Features")]
    [DefaultValue(true)]
    [Description("Durum/progress/uyarı kolonları için hızlı conditional format helperlarını etkin kullanıma hazırlar.")]
    public bool PopularFeaturesEnableConditionalFormatting { get; set; } = true;

    public void ApplyPopularFeaturePack(ViewGridPopularFeaturePreset? preset = null)
    {
        if (!EnablePopularFeaturePack) return;

        var effectivePreset = preset ?? PopularFeaturePreset;
        PopularFeaturePreset = effectivePreset;

        if (PopularFeaturesEnableSearchPanel)
        {
            EnableModernSearchPanel = true;
            SearchPanelCanFilterResults = true;
            HighlightSearchText = true;
            HighlightGlobalFilterText = true;
            EnableIncrementalSearch = true;
        }

        if (PopularFeaturesEnableSummaryFooter)
        {
            ShowSummaryFooter = true;
            MaxSummaryScanRows = effectivePreset == ViewGridPopularFeaturePreset.LargeDataReview ? 200_000 : 50_000;
        }

        if (PopularFeaturesEnableColumnTools)
        {
            ShowHeaderMenuColumnChooserItem = true;
            ShowColumnChooserInHeaderMenu = true;
            ShowColumnChooserWindowInHeaderMenu = true;
            ColumnChooserMenuStaysOpen = true;
            EnableColumnAutoResizeOnDoubleClick = true;
            AutoResizeIncludeHeader = true;
            IncludeCellImagesInAutoResizeWidth = true;
            if (effectivePreset is ViewGridPopularFeaturePreset.MasterData or ViewGridPopularFeaturePreset.LargeDataReview)
                FrozenColumnCount = Math.Max(FrozenColumnCount, 1);
        }

        if (PopularFeaturesEnableFilterTools)
        {
            FilterMenuMode = ViewGrid.Filtering.ViewGridFilterMenuMode.Both;
            FilterPopupResizable = true;
            FilterPopupRememberSize = true;
            FilterPopupShowValueTooltips = true;
            FilterPopupAutoWidthForLongValues = true;
            ShowAdvancedFilterMenuItems = true;
            EnableFilterPresets = true;
            EnableSmartFilterEngine = true;
            SmartFilterSearchAllRows = true;
            SmartFilterTopValuesFirst = true;
            FastFilterMenuForHugeLists = true;
            TypedFilterSearchesAllRows = true;
        }

        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full;
        MenuProfile = ViewGridMenuProfile.Full;
        ShowStateMenuItems = true;
        ShowScenarioMenuItems = true;
        PersistColumnFilters = true;
        PersistVisualPreferences = true;
        EnableGrouping = true;
        SmoothMouseWheelScroll = true;

        switch (effectivePreset)
        {
            case ViewGridPopularFeaturePreset.MasterData:
                SetViewMode(ViewGridMode.DenseList);
                RowHeight = Math.Max(RowHeight, 30);
                MaxFilterDistinctScanRows = 1_000_000;
                MaxAsyncFilterDistinctScanRows = 1_000_000;
                break;
            case ViewGridPopularFeaturePreset.SupportDesk:
                SetViewMode(ViewGridMode.DashboardCard);
                ShowQuickFilterBar = true;
                ShowFloatingFilterButton = true;
                ShowActiveFilterChips = true;
                break;
            case ViewGridPopularFeaturePreset.LargeDataReview:
                SetViewMode(ViewGridMode.Details);
                RowHeight = 26;
                MaxFilterDistinctScanRows = 2_000_000;
                MaxAsyncFilterDistinctScanRows = 2_000_000;
                break;
            case ViewGridPopularFeaturePreset.DataEntry:
                SetViewMode(ViewGridMode.Details);
                AllowEditAllCells = true;
                CellEditActivationKey = Keys.F2;
                ShowSummaryFooter = true;
                break;
            default:
                break;
        }

        BuildViewIndex();
        RefreshView();
    }

    public void AddSemanticStatusConditionalFormat(string aspectName)
    {
        var column = Columns.FirstOrDefault(c => string.Equals(c.AspectName, aspectName, StringComparison.OrdinalIgnoreCase));
        if (column == null) return;

        ConditionalFormats.Add(new ViewGridConditionalFormat
        {
            Column = column,
            Predicate = (_, _, value) => ContainsAny(value, "open", "fail", "error", "kritik", "critical", "missing", "eksik"),
            BackColor = Color.FromArgb(88, 32, 38),
            ForeColor = Color.FromArgb(255, 210, 210)
        });
        ConditionalFormats.Add(new ViewGridConditionalFormat
        {
            Column = column,
            Predicate = (_, _, value) => ContainsAny(value, "waiting", "bekliyor", "warning", "uyarı", "hold"),
            BackColor = Color.FromArgb(92, 72, 24),
            ForeColor = Color.FromArgb(255, 236, 180)
        });
        ConditionalFormats.Add(new ViewGridConditionalFormat
        {
            Column = column,
            Predicate = (_, _, value) => ContainsAny(value, "done", "closed", "ok", "tamam", "resolved"),
            BackColor = Color.FromArgb(28, 78, 48),
            ForeColor = Color.FromArgb(196, 245, 214)
        });
        Invalidate();
    }

    public void AddNumericSummary(string aspectName, ViewGridSummaryType type = ViewGridSummaryType.Sum, string format = "Σ {0}")
    {
        var column = Columns.FirstOrDefault(c => string.Equals(c.AspectName, aspectName, StringComparison.OrdinalIgnoreCase));
        if (column == null) return;
        Summaries.Add(new ViewGridSummaryItem { Column = column, Type = type, Format = format });
        ShowSummaryFooter = true;
        Invalidate();
    }

    public void AddCountSummary(string aspectName, string format = "{0} kayıt")
    {
        var column = Columns.FirstOrDefault(c => string.Equals(c.AspectName, aspectName, StringComparison.OrdinalIgnoreCase));
        if (column == null) return;
        Summaries.Add(new ViewGridSummaryItem { Column = column, Type = ViewGridSummaryType.Count, Format = format });
        ShowSummaryFooter = true;
        Invalidate();
    }

    private static bool ContainsAny(object? value, params string[] tokens)
    {
        string text = Convert.ToString(value) ?? string.Empty;
        if (text.Length == 0) return false;
        foreach (string token in tokens)
        {
            if (text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }
}
