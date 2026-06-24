namespace Taylan.Pano.Core;

/// <summary>
/// Pano'nin aynı kontrolü farklı iş ekranlarında hızlı ve tutarlı kurabilmesi için hazır görünüm senaryoları.
/// Özellikle MasterData, AOI Support Desk ve üretim/SAP listeleri gibi projelerde aynı UX dilini korumak için kullanılır.
/// </summary>
public enum PanoScenario
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

public static class PanoScenarioExtensions
{
    public static void ApplyScenario(this PanoControl grid, PanoScenario scenario)
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));

        grid.AutoSizeTileWidthToContent = false;
        grid.TilePosterMode = false;
        grid.EnforceTilePreferredHeight = true;
        grid.AllowMultilineCells = false;
        grid.MaxCellTextLines = 1;

        switch (scenario)
        {
            case PanoScenario.DataTable:
                grid.ShowHeader = true;
                grid.RowHeight = 28;
                grid.SetViewMode(PanoViewMode.Details);
                break;

            case PanoScenario.DenseData:
                grid.ShowHeader = true;
                grid.RowHeight = 22;
                grid.SetViewMode(PanoViewMode.DenseList);
                break;

            case PanoScenario.ProductTree:
                grid.ShowHeader = true;
                grid.RowHeight = 30;
                grid.AllowMultilineCells = true;
                grid.MaxCellTextLines = 2;
                grid.SetViewMode(PanoViewMode.GroupedList);
                break;

            case PanoScenario.BomPositions:
                grid.ShowHeader = true;
                grid.RowHeight = 26;
                grid.SetViewMode(PanoViewMode.DenseList);
                break;

            case PanoScenario.ProgramFiles:
                grid.TilePreferredWidth = 320;
                grid.TilePreferredHeight = 110;
                grid.TileMaxTextLines = 4;
                grid.SetViewMode(PanoViewMode.Tile);
                break;

            case PanoScenario.MachineOrLinePicker:
                grid.TilePreferredWidth = 185;
                grid.TilePreferredHeight = 98;
                grid.TileMaxTextLines = 2;
                grid.SetViewMode(PanoViewMode.IconGrid);
                break;

            case PanoScenario.TicketBoard:
                grid.TilePreferredWidth = 360;
                grid.LargeCardPreferredWidth = 560;
                grid.LargeCardPreferredHeight = 160;
                grid.LargeCardMaxTextLines = 6;
                grid.SetViewMode(PanoViewMode.DashboardCard);
                break;

            case PanoScenario.Timeline:
                grid.LargeCardPreferredWidth = 900;
                grid.TilePreferredHeight = 136;
                grid.LargeCardMaxTextLines = 8;
                grid.AllowMultilineCells = true;
                grid.MaxCellTextLines = 4;
                grid.SetViewMode(PanoViewMode.Timeline);
                break;

            case PanoScenario.MasterDetail:
                grid.ShowHeader = true;
                grid.AllowMultilineCells = true;
                grid.MaxCellTextLines = 3;
                grid.RowHeight = 34;
                grid.SetViewMode(PanoViewMode.MasterDetail);
                break;

            case PanoScenario.MediaLibrary:
                ApplyMediaDefaults(grid);
                grid.TilePreferredWidth = 260;
                grid.TilePreferredHeight = 260;
                grid.TilePosterImageHeight = 160;
                grid.SetViewMode(PanoViewMode.MediaTile);
                break;

            case PanoScenario.AlbumLibrary:
                ApplyMediaDefaults(grid);
                grid.TilePreferredWidth = 245;
                grid.TilePreferredHeight = 265;
                grid.TilePosterImageHeight = 170;
                grid.MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
                grid.SetViewMode(PanoViewMode.MediaTile);
                break;

            case PanoScenario.MovieLibrary:
                ApplyMediaDefaults(grid);
                grid.PosterPreferredWidth = 190;
                grid.PosterPreferredHeight = 310;
                grid.PosterImageHeight = 235;
                grid.MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
                grid.MediaVideoPreviewMode = true;
                grid.SetViewMode(PanoViewMode.Poster);
                break;

            case PanoScenario.PhotoGallery:
                ApplyMediaDefaults(grid);
                grid.TilePreferredWidth = 220;
                grid.TilePreferredHeight = 220;
                grid.TilePosterImageHeight = 160;
                grid.MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
                grid.SetViewMode(PanoViewMode.Gallery);
                break;

            case PanoScenario.MediaTiles:
                ApplyMediaDefaults(grid);
                grid.TilePreferredWidth = 230;
                grid.TilePreferredHeight = 250;
                grid.TilePosterImageHeight = 156;
                grid.SetViewMode(PanoViewMode.MediaTile);
                break;

            case PanoScenario.FilmStrip:
                ApplyMediaDefaults(grid);
                grid.TilePreferredWidth = 560;
                grid.TilePreferredHeight = 170;
                grid.TilePosterImageHeight = 126;
                grid.SetViewMode(PanoViewMode.FilmStrip);
                break;

            case PanoScenario.MediaDetailCard:
                ApplyMediaDefaults(grid);
                grid.ShowHeader = false;
                grid.DetailCardLayout = PanoDetailCardLayout.Media;
                grid.DetailCardMediaImageWidth = 158;
                grid.DetailCardMediaImageHeight = 178;
                grid.ShowDetailCardColumnHeaders = true;
                grid.SetViewMode(PanoViewMode.DetailCard);
                break;

            case PanoScenario.NowPlaying:
                ApplyMediaDefaults(grid);
                grid.ShowMediaPlaybackState = true;
                grid.ShowMediaNowPlayingBadge = true;
                grid.ShowMediaEqualizerIndicator = true;
                grid.TilePreferredWidth = 245;
                grid.TilePreferredHeight = 265;
                grid.TilePosterImageHeight = 170;
                grid.SetViewMode(PanoViewMode.MediaTile);
                break;

            case PanoScenario.VideoPreview:
                ApplyMediaDefaults(grid);
                grid.MediaVideoPreviewMode = true;
                grid.TilePreferredWidth = 560;
                grid.TilePreferredHeight = 178;
                grid.TilePosterImageHeight = 132;
                grid.SetViewMode(PanoViewMode.FilmStrip);
                break;

            case PanoScenario.DocumentPreview:
                ApplyMediaDefaults(grid);
                grid.PosterPreferredWidth = 180;
                grid.PosterPreferredHeight = 260;
                grid.PosterImageHeight = 190;
                grid.MediaImageScaleMode = PanoMediaImageScaleMode.Contain;
                grid.SetViewMode(PanoViewMode.Poster);
                break;
        }
    }

    private static void ApplyMediaDefaults(PanoControl grid)
    {
        grid.ShowHeader = false;
        grid.TilePosterMode = true;
        grid.AutoSizeTileWidthToContent = false;
        grid.EnforceTilePreferredHeight = true;
        grid.AllowMultilineCells = true;
        grid.MaxCellTextLines = 2;
        grid.TileMaxTextLines = 4;
        grid.MediaImageScaleMode = PanoMediaImageScaleMode.Cover;
        grid.MediaImageRoundedCorners = true;
        grid.ShowMediaOverlayButton = true;
        grid.ShowMediaQualityBadge = true;
        grid.ShowMediaPlaybackState = true;
        grid.ShowMediaNowPlayingBadge = true;
        grid.ShowMediaEqualizerIndicator = true;
    }

    public static bool IsMediaScenario(this PanoScenario scenario)
        => scenario is PanoScenario.MediaLibrary
            or PanoScenario.AlbumLibrary
            or PanoScenario.MovieLibrary
            or PanoScenario.PhotoGallery
            or PanoScenario.MediaTiles
            or PanoScenario.FilmStrip
            or PanoScenario.MediaDetailCard
            or PanoScenario.NowPlaying
            or PanoScenario.VideoPreview
            or PanoScenario.DocumentPreview;

    public static string GetDisplayName(this PanoScenario scenario)
        => scenario switch
        {
            PanoScenario.DataTable => "Standart Tablo",
            PanoScenario.DenseData => "Yoğun Veri Tablosu",
            PanoScenario.ProductTree => "Ürün Ağacı",
            PanoScenario.BomPositions => "BOM / Pozisyon Listesi",
            PanoScenario.ProgramFiles => "Program Dosyaları",
            PanoScenario.MachineOrLinePicker => "Makine / Hat Seçimi",
            PanoScenario.TicketBoard => "Ticket Dashboard",
            PanoScenario.Timeline => "İşlem Geçmişi",
            PanoScenario.MasterDetail => "MasterData Detay",
            PanoScenario.MediaLibrary => "Medya Kütüphanesi",
            PanoScenario.AlbumLibrary => "Albüm / Müzik Arşivi",
            PanoScenario.MovieLibrary => "Film / Video Afişleri",
            PanoScenario.PhotoGallery => "Fotoğraf Galerisi",
            PanoScenario.MediaTiles => "MediaTile Kartları",
            PanoScenario.FilmStrip => "FilmStrip / Yatay Şerit",
            PanoScenario.MediaDetailCard => "Media DetailCard",
            PanoScenario.NowPlaying => "Now Playing / Çalan Medya",
            PanoScenario.VideoPreview => "Video Preview",
            PanoScenario.DocumentPreview => "Doküman Önizleme",
            _ => scenario.ToString()
        };
}
