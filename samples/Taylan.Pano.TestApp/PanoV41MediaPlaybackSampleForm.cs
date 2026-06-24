using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed class PanoV41MediaPlaybackSampleForm : Form
{
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = PanoDataMode.Object,
        ViewMode = PanoViewMode.Poster,
        TilePosterMode = true,
        ShowMediaOverlayButton = true,
        ShowMediaPlaybackState = true,
        ShowMediaNowPlayingBadge = true,
        ShowMediaEqualizerIndicator = true,
        MediaImageScaleMode = PanoMediaImageScaleMode.Cover,
        TilePreferredWidth = 250,
        TilePreferredHeight = 320,
        RowHeight = 312,
        ShowQuickFilterBar = true,
        ShowActiveFilterChips = true
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly List<MediaPlaybackRow> _rows = new();

    public PanoV41MediaPlaybackSampleForm()
    {
        Text = "Pano v41 - Audix / Video Playback State";
        Width = 1180;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;

        ConfigureColumns();
        ConfigureToolbar();
        ConfigurePlayback();
        Controls.Add(_grid);
        Controls.Add(_tool);
        ApplyDarkTheme();
        LoadRows();
    }

    private void ConfigureColumns()
    {
        var cover = new PanoColumn("Kapak", nameof(MediaPlaybackRow.Cover), 90) { Kind = PanoColumnKind.Image };
        cover.ImageGetter = row => (row as MediaPlaybackRow)?.Cover;
        _grid.Columns.Add(cover);
        _grid.Columns.Add(new PanoColumn("Başlık", nameof(MediaPlaybackRow.Title), 220) { FillFreeSpace = true });
        _grid.Columns.Add(new PanoColumn("Sanatçı", nameof(MediaPlaybackRow.Artist), 160));
        _grid.Columns.Add(new PanoColumn("Tür", nameof(MediaPlaybackRow.MediaKind), 90));
        _grid.Columns.Add(new PanoColumn("Kalite", nameof(MediaPlaybackRow.Quality), 90));
        _grid.Columns.Add(new PanoColumn("PlaybackState", nameof(MediaPlaybackRow.PlaybackState), 110));
    }

    private void ConfigureToolbar()
    {
        _tool.Items.Add(new ToolStripButton("Poster", null, (_, __) => _grid.SetViewMode(PanoViewMode.Poster)));
        _tool.Items.Add(new ToolStripButton("MediaTile", null, (_, __) => _grid.SetViewMode(PanoViewMode.MediaTile)));
        _tool.Items.Add(new ToolStripButton("Gallery", null, (_, __) => _grid.SetViewMode(PanoViewMode.Gallery)));
        _tool.Items.Add(new ToolStripButton("FilmStrip", null, (_, __) => _grid.SetViewMode(PanoViewMode.FilmStrip)));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Video Preview Mode", null, (_, __) => { _grid.MediaVideoPreviewMode = !_grid.MediaVideoPreviewMode; _grid.RefreshMediaPlayback(); }));
        _tool.Items.Add(new ToolStripButton("Koyu", null, (_, __) => ApplyDarkTheme()));
        _tool.Items.Add(new ToolStripButton("Açık", null, (_, __) => ApplyLightTheme()));
    }

    private void ConfigurePlayback()
    {
        _grid.MediaQualityBadgeGetter = row => (row as MediaPlaybackRow)?.Quality;
        _grid.MediaKindGetter = row => (row as MediaPlaybackRow)?.MediaKind ?? PanoMediaKind.Audio;
        _grid.MediaPlaybackStateGetter = row => (row as MediaPlaybackRow)?.PlaybackState ?? PanoMediaPlaybackState.None;
        _grid.MediaPlayPauseClicked += (_, e) =>
        {
            var clicked = e.RowObject as MediaPlaybackRow;
            if (clicked == null) return;

            foreach (var item in _rows)
            {
                if (!ReferenceEquals(item, clicked) && item.PlaybackState == PanoMediaPlaybackState.Playing)
                    item.PlaybackState = PanoMediaPlaybackState.None;
            }

            clicked.PlaybackState = e.CurrentState == PanoMediaPlaybackState.Playing
                ? PanoMediaPlaybackState.Paused
                : PanoMediaPlaybackState.Playing;

            Text = clicked.MediaKind == PanoMediaKind.Video
                ? "Pano v41 - Video preview/player tetiklendi: " + clicked.Title
                : "Pano v41 - Audix çalıyor: " + clicked.Title;

            _grid.RefreshMediaPlayback();
        };
    }

    private void LoadRows()
    {
        _rows.Clear();
        _rows.Add(new MediaPlaybackRow("Me Malone O Pateras Mou", "Antzela Dimitriou", PanoMediaKind.Audio, "MP3", CreateCover(Color.SteelBlue, "A1")));
        _rows.Add(new MediaPlaybackRow("Mia Litropi Agapi", "Antzela Dimitriou", PanoMediaKind.Audio, "FLAC", CreateCover(Color.MediumPurple, "A2")) { PlaybackState = PanoMediaPlaybackState.Playing });
        _rows.Add(new MediaPlaybackRow("Na Giriseis Xana", "Antzela Dimitriou", PanoMediaKind.Audio, "320", CreateCover(Color.SeaGreen, "A3")));
        _rows.Add(new MediaPlaybackRow("Factory Clip 01", "Video Archive", PanoMediaKind.Video, "1080p", CreateCover(Color.DarkOrange, "V1")));
        _rows.Add(new MediaPlaybackRow("AOI Defect Review", "Inspection Video", PanoMediaKind.Video, "4K", CreateCover(Color.Crimson, "V2")));
        _grid.SetObjects(_rows);
    }

    private void ApplyDarkTheme()
    {
        var theme = PanoTheme.DarkTheme();
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        _grid.ApplyTheme(theme);
    }

    private void ApplyLightTheme()
    {
        var theme = PanoTheme.LightTheme();
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        _grid.ApplyTheme(theme);
    }

    private static Bitmap CreateCover(Color baseColor, string text)
    {
        var bmp = new Bitmap(360, 520);
        using var g = Graphics.FromImage(bmp);
        using var bg = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, bmp.Width, bmp.Height), ControlPaint.Light(baseColor), ControlPaint.Dark(baseColor), 45f);
        g.FillRectangle(bg, 0, 0, bmp.Width, bmp.Height);
        using var glow = new SolidBrush(Color.FromArgb(80, Color.White));
        g.FillEllipse(glow, 30, 70, 220, 220);
        using var dark = new SolidBrush(Color.FromArgb(120, Color.Black));
        g.FillRectangle(dark, 0, 350, bmp.Width, 170);
        using var font = new Font("Segoe UI", 44, FontStyle.Bold);
        TextRenderer.DrawText(g, text, font, new Rectangle(0, 170, bmp.Width, 90), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        using var small = new Font("Segoe UI", 13, FontStyle.Bold);
        TextRenderer.DrawText(g, "PANO MEDIA", small, new Rectangle(0, 394, bmp.Width, 32), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bmp;
    }

    private sealed class MediaPlaybackRow
    {
        public MediaPlaybackRow(string title, string artist, PanoMediaKind mediaKind, string quality, Image cover)
        {
            Title = title;
            Artist = artist;
            MediaKind = mediaKind;
            Quality = quality;
            Cover = cover;
        }

        public string Title { get; }
        public string Artist { get; }
        public PanoMediaKind MediaKind { get; }
        public string Quality { get; }
        public Image Cover { get; }
        public PanoMediaPlaybackState PlaybackState { get; set; }
    }
}

