using System.ComponentModel;
using Taylan.Pano.Filtering;

namespace Taylan.Pano.Core;

public enum PanoUxPolishPreset
{
    Balanced,
    MasterData,
    SupportDesk,
    PosterGallery,
    LargeData
}

public enum PanoQuickActionBarMode
{
    Hidden,
    Compact,
    Full
}

public partial class PanoControl
{
    [Category("Pano - UX Polish v28")]
    [DefaultValue(true)]
    [Description("Kart/poster/dashboard görünümlerinde filtre butonunu kart üstünde değil üst filtre barında gösteren v28 düzenini kullanır.")]
    public bool EnableV28CardFilterPolish { get; set; } = true;

    [Category("Pano - UX Polish v28")]
    [DefaultValue(PanoQuickActionBarMode.Compact)]
    [Description("Popüler gridlerdeki arama/filtre/temizle benzeri hızlı aksiyonları tek üst barda toplamak için kullanılan mod.")]
    public PanoQuickActionBarMode QuickActionBarMode { get; set; } = PanoQuickActionBarMode.Compact;

    [Category("Pano - UX Polish v28")]
    [DefaultValue(true)]
    [Description("Kullanıcı deneyimi için arama, filtre, kolon seçici, command palette ve aktif filtre chiplerini birlikte açan hazır paket.")]
    public bool EnableBestPracticeUxPack { get; set; } = true;

    [Category("Pano - UX Polish v28")]
    [DefaultValue(true)]
    [Description("Filtre/chip alanı dolduğunda içerik alanını ezmeden kart viewport'unu aşağıdan başlatır.")]
    public bool ReserveTopUxAreaForCardViews { get; set; } = true;

    public void ApplyV28UxPolish(PanoUxPolishPreset preset = PanoUxPolishPreset.Balanced)
    {
        EnableBestPracticeUxPack = true;
        EnableV28CardFilterPolish = true;
        MoveFilterButtonToTopBar = true;
        ShowFloatingFilterButton = false;
        CardFilterUxPlacement = PanoCardFilterUxPlacement.TopBar;
        ShowQuickFilterBar = true;
        ShowActiveFilterChips = true;
        CardViewReserveFilterArea = true;
        FilterMenuMode = PanoFilterMenuMode.Both;
        EnableCommandPalette = true;
        EnableModernEmptyState = true;
        AllowColumnReorder = true;
        ShowColumnChooserInHeaderMenu = true;
        ShowColumnChooserWindowInHeaderMenu = true;
        HeaderContextMenuBehavior = PanoHeaderContextMenuBehavior.Full;
        CloseHeaderContextMenuBeforeOpeningFilterPopup = true;

        switch (preset)
        {
            case PanoUxPolishPreset.MasterData:
                QuickFilterPlaceholderText = "BOM / SAP / RefDes içinde ara...";
                CardFilterBarHeight = 76;
                CardFilterContentSpacing = 8;
                ShowSummaryFooter = true;
                break;
            case PanoUxPolishPreset.SupportDesk:
                QuickFilterPlaceholderText = "Ticket, makine, operatör ara...";
                CardFilterBarHeight = 78;
                CardFilterContentSpacing = 10;
                break;
            case PanoUxPolishPreset.PosterGallery:
                if (ViewMode != PanoViewMode.Poster)
                    SetViewMode(PanoViewMode.Poster);
                QuickFilterPlaceholderText = "Poster / kartlarda ara...";
                CardFilterBarHeight = 72;
                CardFilterContentSpacing = 10;
                ApplyPosterModeDefaults(false);
                break;
            case PanoUxPolishPreset.LargeData:
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
        CardFilterUxPlacement = PanoCardFilterUxPlacement.TopBar;
        ShowQuickFilterBar = true;
        ShowActiveFilterChips = true;
        CardViewReserveFilterArea = true;
        RefreshV28UxPolish();
    }

    public void UseHybridCardFilterUx()
    {
        MoveFilterButtonToTopBar = false;
        ShowFloatingFilterButton = true;
        CardFilterUxPlacement = PanoCardFilterUxPlacement.TopBarAndFloatingButton;
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
