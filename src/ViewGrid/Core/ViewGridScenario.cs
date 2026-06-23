namespace ViewGrid.Core;

/// <summary>
/// ViewGrid'nin aynı kontrolü farklı iş ekranlarında hızlı ve tutarlı kurabilmesi için hazır görünüm senaryoları.
/// Özellikle MasterData, AOI Support Desk ve üretim/SAP listeleri gibi projelerde aynı UX dilini korumak için kullanılır.
/// </summary>
public enum ViewGridScenario
{
    /// <summary>Klasik SQL / SAP / BOM tablosu.</summary>
    DataTable,

    /// <summary>Çok fazla kayıt gösterilecek Excel benzeri yoğun tablo.</summary>
    DenseData,

    /// <summary>Ürün ağacı, malzeme ağacı veya reçete hiyerarşisi.</summary>
    ProductTree,

    /// <summary>BOM / komponent / pozisyon listesi.</summary>
    BomPositions,

    /// <summary>Dizgi programı, makine programı veya dosya çıktı listesi.</summary>
    ProgramFiles,

    /// <summary>Makine, hat, feeder, istasyon veya operatör seçim ekranı.</summary>
    MachineOrLinePicker,

    /// <summary>Ticket, mesaj veya işlem takip kartları.</summary>
    TicketBoard,

    /// <summary>Operasyon geçmişi, log veya karar akışı.</summary>
    Timeline,

    /// <summary>Üst kayıt + alt detay listesi kullanan bakım ekranları.</summary>
    MasterDetail,

    /// <summary>Albüm, film, fotoğraf ve doküman gibi genel medya kütüphanesi.</summary>
    MediaLibrary,

    /// <summary>Albüm kapağı ve sanatçı/şarkı metadata odaklı müzik arşivi.</summary>
    AlbumLibrary,

    /// <summary>Film afişi, video preview ve oynatma aksiyonu odaklı video arşivi.</summary>
    MovieLibrary,

    /// <summary>Fotoğraf/kapak galerisi ve thumbnail koleksiyonu.</summary>
    PhotoGallery,

    /// <summary>Spotify/Plex benzeri kompakt medya kartları.</summary>
    MediaTiles,

    /// <summary>Netflix/Plex benzeri yatay medya şeridi.</summary>
    FilmStrip,

    /// <summary>Kapak solda, tüm metadata sağda olan medya detay kartı.</summary>
    MediaDetailCard,

    /// <summary>Çalan medya, play/pause, now playing ve equalizer göstergeleri.</summary>
    NowPlaying,

    /// <summary>Video dosyaları için play overlay ve preview niyeti olan görünüm.</summary>
    VideoPreview,

    /// <summary>PDF, görsel, doküman veya dosya önizleme listeleri.</summary>
    DocumentPreview
}

public static class ViewGridScenarioExtensions
{
    public static void ApplyScenario(this ViewGridControl grid, ViewGridScenario scenario)
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));

        grid.AutoSizeTileWidthToContent = false;
        grid.TilePosterMode = false;
        grid.EnforceTilePreferredHeight = true;
        grid.AllowMultilineCells = false;
        grid.MaxCellTextLines = 1;

        switch (scenario)
        {
            case ViewGridScenario.DataTable:
                grid.ShowHeader = true;
                grid.RowHeight = 28;
                grid.SetViewMode(ViewGridMode.Details);
                break;

            case ViewGridScenario.DenseData:
                grid.ShowHeader = true;
                grid.RowHeight = 22;
                grid.SetViewMode(ViewGridMode.DenseList);
                break;

            case ViewGridScenario.ProductTree:
                grid.ShowHeader = true;
                grid.RowHeight = 30;
                grid.AllowMultilineCells = true;
                grid.MaxCellTextLines = 2;
                grid.SetViewMode(ViewGridMode.GroupedList);
                break;

            case ViewGridScenario.BomPositions:
                grid.ShowHeader = true;
                grid.RowHeight = 26;
                grid.SetViewMode(ViewGridMode.DenseList);
                break;

            case ViewGridScenario.ProgramFiles:
                grid.TilePreferredWidth = 320;
                grid.TilePreferredHeight = 110;
                grid.TileMaxTextLines = 4;
                grid.SetViewMode(ViewGridMode.Tile);
                break;

            case ViewGridScenario.MachineOrLinePicker:
                grid.TilePreferredWidth = 185;
                grid.TilePreferredHeight = 98;
                grid.TileMaxTextLines = 2;
                grid.SetViewMode(ViewGridMode.IconGrid);
                break;

            case ViewGridScenario.TicketBoard:
                grid.TilePreferredWidth = 360;
                grid.LargeCardPreferredWidth = 560;
                grid.LargeCardPreferredHeight = 160;
                grid.LargeCardMaxTextLines = 6;
                grid.SetViewMode(ViewGridMode.DashboardCard);
                break;

            case ViewGridScenario.Timeline:
                grid.LargeCardPreferredWidth = 900;
                grid.TilePreferredHeight = 136;
                grid.LargeCardMaxTextLines = 8;
                grid.AllowMultilineCells = true;
                grid.MaxCellTextLines = 4;
                grid.SetViewMode(ViewGridMode.Timeline);
                break;

            case ViewGridScenario.MasterDetail:
                grid.ShowHeader = true;
                grid.AllowMultilineCells = true;
                grid.MaxCellTextLines = 3;
                grid.RowHeight = 34;
                grid.SetViewMode(ViewGridMode.MasterDetail);
                break;

            case ViewGridScenario.MediaLibrary:
                ApplyMediaDefaults(grid);
                grid.TilePreferredWidth = 260;
                grid.TilePreferredHeight = 260;
                grid.TilePosterImageHeight = 160;
                grid.SetViewMode(ViewGridMode.MediaTile);
                break;

            case ViewGridScenario.AlbumLibrary:
                ApplyMediaDefaults(grid);
                grid.TilePreferredWidth = 245;
                grid.TilePreferredHeight = 265;
                grid.TilePosterImageHeight = 170;
                grid.MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
                grid.SetViewMode(ViewGridMode.MediaTile);
                break;

            case ViewGridScenario.MovieLibrary:
                ApplyMediaDefaults(grid);
                grid.PosterPreferredWidth = 190;
                grid.PosterPreferredHeight = 310;
                grid.PosterImageHeight = 235;
                grid.MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
                grid.MediaVideoPreviewMode = true;
                grid.SetViewMode(ViewGridMode.Poster);
                break;

            case ViewGridScenario.PhotoGallery:
                ApplyMediaDefaults(grid);
                grid.TilePreferredWidth = 220;
                grid.TilePreferredHeight = 220;
                grid.TilePosterImageHeight = 160;
                grid.MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
                grid.SetViewMode(ViewGridMode.Gallery);
                break;

            case ViewGridScenario.MediaTiles:
                ApplyMediaDefaults(grid);
                grid.TilePreferredWidth = 230;
                grid.TilePreferredHeight = 250;
                grid.TilePosterImageHeight = 156;
                grid.SetViewMode(ViewGridMode.MediaTile);
                break;

            case ViewGridScenario.FilmStrip:
                ApplyMediaDefaults(grid);
                grid.TilePreferredWidth = 560;
                grid.TilePreferredHeight = 170;
                grid.TilePosterImageHeight = 126;
                grid.SetViewMode(ViewGridMode.FilmStrip);
                break;

            case ViewGridScenario.MediaDetailCard:
                ApplyMediaDefaults(grid);
                grid.ShowHeader = false;
                grid.DetailCardLayout = ViewGridDetailCardLayout.Media;
                grid.DetailCardMediaImageWidth = 158;
                grid.DetailCardMediaImageHeight = 178;
                grid.ShowDetailCardColumnHeaders = true;
                grid.SetViewMode(ViewGridMode.DetailCard);
                break;

            case ViewGridScenario.NowPlaying:
                ApplyMediaDefaults(grid);
                grid.ShowMediaPlaybackState = true;
                grid.ShowMediaNowPlayingBadge = true;
                grid.ShowMediaEqualizerIndicator = true;
                grid.TilePreferredWidth = 245;
                grid.TilePreferredHeight = 265;
                grid.TilePosterImageHeight = 170;
                grid.SetViewMode(ViewGridMode.MediaTile);
                break;

            case ViewGridScenario.VideoPreview:
                ApplyMediaDefaults(grid);
                grid.MediaVideoPreviewMode = true;
                grid.TilePreferredWidth = 560;
                grid.TilePreferredHeight = 178;
                grid.TilePosterImageHeight = 132;
                grid.SetViewMode(ViewGridMode.FilmStrip);
                break;

            case ViewGridScenario.DocumentPreview:
                ApplyMediaDefaults(grid);
                grid.PosterPreferredWidth = 180;
                grid.PosterPreferredHeight = 260;
                grid.PosterImageHeight = 190;
                grid.MediaImageScaleMode = ViewGridMediaImageScaleMode.Contain;
                grid.SetViewMode(ViewGridMode.Poster);
                break;
        }
    }

    private static void ApplyMediaDefaults(ViewGridControl grid)
    {
        grid.ShowHeader = false;
        grid.TilePosterMode = true;
        grid.AutoSizeTileWidthToContent = false;
        grid.EnforceTilePreferredHeight = true;
        grid.AllowMultilineCells = true;
        grid.MaxCellTextLines = 2;
        grid.TileMaxTextLines = 4;
        grid.MediaImageScaleMode = ViewGridMediaImageScaleMode.Cover;
        grid.MediaImageRoundedCorners = true;
        grid.ShowMediaOverlayButton = true;
        grid.ShowMediaQualityBadge = true;
        grid.ShowMediaPlaybackState = true;
        grid.ShowMediaNowPlayingBadge = true;
        grid.ShowMediaEqualizerIndicator = true;
    }

    public static bool IsMediaScenario(this ViewGridScenario scenario)
        => scenario is ViewGridScenario.MediaLibrary
            or ViewGridScenario.AlbumLibrary
            or ViewGridScenario.MovieLibrary
            or ViewGridScenario.PhotoGallery
            or ViewGridScenario.MediaTiles
            or ViewGridScenario.FilmStrip
            or ViewGridScenario.MediaDetailCard
            or ViewGridScenario.NowPlaying
            or ViewGridScenario.VideoPreview
            or ViewGridScenario.DocumentPreview;

    public static string GetDisplayName(this ViewGridScenario scenario)
        => scenario switch
        {
            ViewGridScenario.DataTable => "Standart Tablo",
            ViewGridScenario.DenseData => "Yoğun Veri Tablosu",
            ViewGridScenario.ProductTree => "Ürün Ağacı",
            ViewGridScenario.BomPositions => "BOM / Pozisyon Listesi",
            ViewGridScenario.ProgramFiles => "Program Dosyaları",
            ViewGridScenario.MachineOrLinePicker => "Makine / Hat Seçimi",
            ViewGridScenario.TicketBoard => "Ticket Dashboard",
            ViewGridScenario.Timeline => "İşlem Geçmişi",
            ViewGridScenario.MasterDetail => "MasterData Detay",
            ViewGridScenario.MediaLibrary => "Medya Kütüphanesi",
            ViewGridScenario.AlbumLibrary => "Albüm / Müzik Arşivi",
            ViewGridScenario.MovieLibrary => "Film / Video Afişleri",
            ViewGridScenario.PhotoGallery => "Fotoğraf Galerisi",
            ViewGridScenario.MediaTiles => "MediaTile Kartları",
            ViewGridScenario.FilmStrip => "FilmStrip / Yatay Şerit",
            ViewGridScenario.MediaDetailCard => "Media DetailCard",
            ViewGridScenario.NowPlaying => "Now Playing / Çalan Medya",
            ViewGridScenario.VideoPreview => "Video Preview",
            ViewGridScenario.DocumentPreview => "Doküman Önizleme",
            _ => scenario.ToString()
        };
}
