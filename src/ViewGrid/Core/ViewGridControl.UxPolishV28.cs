using System.ComponentModel;
using ViewGrid.Filtering;

namespace ViewGrid.Core;

public enum ViewGridUxPolishPreset
{
    Balanced,
    MasterData,
    SupportDesk,
    PosterGallery,
    LargeData
}

public enum ViewGridQuickActionBarMode
{
    Hidden,
    Compact,
    Full
}

public partial class ViewGridControl
{
    [Category("ViewGrid - UX Polish v28")]
    [DefaultValue(true)]
    [Description("Kart/poster/dashboard görünümlerinde filtre butonunu kart üstünde değil üst filtre barında gösteren v28 düzenini kullanır.")]
    public bool EnableV28CardFilterPolish { get; set; } = true;

    [Category("ViewGrid - UX Polish v28")]
    [DefaultValue(ViewGridQuickActionBarMode.Compact)]
    [Description("Popüler gridlerdeki arama/filtre/temizle benzeri hızlı aksiyonları tek üst barda toplamak için kullanılan mod.")]
    public ViewGridQuickActionBarMode QuickActionBarMode { get; set; } = ViewGridQuickActionBarMode.Compact;

    [Category("ViewGrid - UX Polish v28")]
    [DefaultValue(true)]
    [Description("Kullanıcı deneyimi için arama, filtre, kolon seçici, command palette ve aktif filtre chiplerini birlikte açan hazır paket.")]
    public bool EnableBestPracticeUxPack { get; set; } = true;

    [Category("ViewGrid - UX Polish v28")]
    [DefaultValue(true)]
    [Description("Filtre/chip alanı dolduğunda içerik alanını ezmeden kart viewport'unu aşağıdan başlatır.")]
    public bool ReserveTopUxAreaForCardViews { get; set; } = true;

    public void ApplyV28UxPolish(ViewGridUxPolishPreset preset = ViewGridUxPolishPreset.Balanced)
    {
        EnableBestPracticeUxPack = true;
        EnableV28CardFilterPolish = true;
        MoveFilterButtonToTopBar = true;
        ShowFloatingFilterButton = false;
        CardFilterUxPlacement = ViewGridCardFilterUxPlacement.TopBar;
        ShowQuickFilterBar = true;
        ShowActiveFilterChips = true;
        CardViewReserveFilterArea = true;
        FilterMenuMode = ViewGridFilterMenuMode.Both;
        EnableCommandPalette = true;
        EnableModernEmptyState = true;
        AllowColumnReorder = true;
        ShowColumnChooserInHeaderMenu = true;
        ShowColumnChooserWindowInHeaderMenu = true;
        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full;
        CloseHeaderContextMenuBeforeOpeningFilterPopup = true;

        switch (preset)
        {
            case ViewGridUxPolishPreset.MasterData:
                QuickFilterPlaceholderText = "BOM / SAP / RefDes içinde ara...";
                CardFilterBarHeight = 76;
                CardFilterContentSpacing = 8;
                ShowSummaryFooter = true;
                break;
            case ViewGridUxPolishPreset.SupportDesk:
                QuickFilterPlaceholderText = "Ticket, makine, operatör ara...";
                CardFilterBarHeight = 78;
                CardFilterContentSpacing = 10;
                break;
            case ViewGridUxPolishPreset.PosterGallery:
                if (ViewMode != ViewGridMode.Poster)
                    SetViewMode(ViewGridMode.Poster);
                QuickFilterPlaceholderText = "Poster / kartlarda ara...";
                CardFilterBarHeight = 72;
                CardFilterContentSpacing = 10;
                ApplyPosterModeDefaults(false);
                break;
            case ViewGridUxPolishPreset.LargeData:
                QuickFilterPlaceholderText = "Milyon satır içinde ara...";
                CardFilterBarHeight = 66;
                CardFilterContentSpacing = 6;
                FastFilterMenuForHugeLists = true;
                AsyncLoadFullFilterValues = true;
                TypedFilterSearchesAllRows = true;
                break;
            default:
                QuickFilterPlaceholderText = "Ara / filtrele...";
                CardFilterBarHeight = 74;
                CardFilterContentSpacing = 8;
                break;
        }

        RefreshV28UxPolish();
    }

    public void UseTopBarFilterOnly()
    {
        MoveFilterButtonToTopBar = true;
        ShowFloatingFilterButton = false;
        CardFilterUxPlacement = ViewGridCardFilterUxPlacement.TopBar;
        ShowQuickFilterBar = true;
        ShowActiveFilterChips = true;
        CardViewReserveFilterArea = true;
        RefreshV28UxPolish();
    }

    public void UseHybridCardFilterUx()
    {
        MoveFilterButtonToTopBar = false;
        ShowFloatingFilterButton = true;
        CardFilterUxPlacement = ViewGridCardFilterUxPlacement.TopBarAndFloatingButton;
        ShowQuickFilterBar = true;
        ShowActiveFilterChips = true;
        CardViewReserveFilterArea = true;
        RefreshV28UxPolish();
    }

    public void RefreshV28UxPolish()
    {
        RefreshCardViewFilterUx();
        Invalidate();
    }
}
