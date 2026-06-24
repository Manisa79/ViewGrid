using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed class PanoV51RealUsagePilotSampleForm : Form
{
    private readonly SplitContainer _split = new() { Dock = DockStyle.Fill, SplitterDistance = 900 };
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = PanoDataMode.Object,
        ViewMode = PanoViewMode.Poster,
        TilePosterMode = true,
        ShowQuickFilterBar = true,
        ShowActiveFilterChips = true,
        EmptyListMessage = "Audix medya örneği için kayıt yok"
    };
    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly TextBox _details = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Segoe UI", 10F)
    };
    private readonly Label _header = new()
    {
        Dock = DockStyle.Top,
        Height = 74,
        Padding = new Padding(16, 10, 16, 8),
        Font = new Font("Segoe UI", 13F, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly List<AudixTrackRow> _rows = new();
    private bool _dark = true;

    public PanoV51RealUsagePilotSampleForm()
    {
        Text = "Pano 5.1 - Audix Pilot + Theme Audit + Example Cleanup";
        Width = 1360;
        Height = 820;
        MinimumSize = new Size(1100, 680);
        StartPosition = FormStartPosition.CenterScreen;

        ConfigureColumns();
        ConfigureToolbar();
        ConfigureMedia();
        BuildLayout();
        LoadRows();
        ApplyTheme(true);
        UpdateDetails("Pano 5.1 gerçek kullanım pilotu açıldı.");
    }

    private void BuildLayout()
    {
        _split.Panel1.Controls.Add(_grid);
        _split.Panel2.Controls.Add(_details);
        Controls.Add(_split);
        Controls.Add(_header);
        Controls.Add(_tool);
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Clear();
        var cover = new PanoColumn("Kapak", nameof(AudixTrackRow.Cover), 96) { Kind = PanoColumnKind.Image };
        cover.ImageGetter = row => (row as AudixTrackRow)?.Cover;
        _grid.Columns.Add(cover);
        _grid.Columns.Add(new PanoColumn("Şarkı", nameof(AudixTrackRow.Title), 240) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 2 });
        _grid.Columns.Add(new PanoColumn("Sanatçı", nameof(AudixTrackRow.Artist), 170));
        _grid.Columns.Add(new PanoColumn("Albüm", nameof(AudixTrackRow.Album), 180));
        _grid.Columns.Add(new PanoColumn("Tür", nameof(AudixTrackRow.MediaKind), 80) { Kind = PanoColumnKind.Badge });
        _grid.Columns.Add(new PanoColumn("Kalite", nameof(AudixTrackRow.Quality), 80) { Kind = PanoColumnKind.Badge });
        _grid.Columns.Add(new PanoColumn("Durum", nameof(AudixTrackRow.PlaybackState), 110) { Kind = PanoColumnKind.Badge });
    }

    private void ConfigureToolbar()
    {
        _tool.Items.Add(new ToolStripButton("Poster", null, (_, __) => SetMode(PanoViewMode.Poster)));
        _tool.Items.Add(new ToolStripButton("MediaTile", null, (_, __) => SetMode(PanoViewMode.MediaTile)));
        _tool.Items.Add(new ToolStripButton("Gallery", null, (_, __) => SetMode(PanoViewMode.Gallery)));
        _tool.Items.Add(new ToolStripButton("FilmStrip", null, (_, __) => SetMode(PanoViewMode.FilmStrip)));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Audix Defaults", null, (_, __) => { _grid.ApplyAudix51MediaPilotDefaults(); UpdateDetails("Audix 5.1 medya varsayılanları uygulandı."); }));
        _tool.Items.Add(new ToolStripButton("Theme Audit", null, (_, __) => ShowThemeAudit()));
        _tool.Items.Add(new ToolStripButton("Runtime Check", null, (_, __) => ShowRuntimeChecks()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Dark", null, (_, __) => ApplyTheme(true)));
        _tool.Items.Add(new ToolStripButton("Light", null, (_, __) => ApplyTheme(false)));
    }

    private void ConfigureMedia()
    {
        _grid.ApplyAudix51MediaPilotDefaults();
        _grid.MediaQualityBadgeGetter = row => (row as AudixTrackRow)?.Quality;
        _grid.MediaKindGetter = row => (row as AudixTrackRow)?.MediaKind ?? PanoMediaKind.Audio;
        _grid.MediaPlaybackStateGetter = row => (row as AudixTrackRow)?.PlaybackState ?? PanoMediaPlaybackState.None;
        _grid.MediaPlayPauseClicked += (_, e) => TogglePlayback(e.RowObject as AudixTrackRow, e.CurrentState);
        _grid.SelectionChanged += (_, __) => UpdateSelectionDetails();
    }

    private void SetMode(PanoViewMode mode)
    {
        _grid.SetViewMode(mode);
        _grid.TilePosterMode = mode is PanoViewMode.Poster or PanoViewMode.Gallery or PanoViewMode.MediaTile or PanoViewMode.FilmStrip;
        UpdateDetails($"Görünüm modu değişti: {mode}. Audix'te albüm kapağı için en iyi modlar Poster / MediaTile / Gallery / FilmStrip.");
    }

    private void TogglePlayback(AudixTrackRow? clicked, PanoMediaPlaybackState currentState)
    {
        if (clicked == null) return;

        foreach (var row in _rows)
        {
            if (!ReferenceEquals(row, clicked) && row.PlaybackState == PanoMediaPlaybackState.Playing)
                row.PlaybackState = PanoMediaPlaybackState.None;
        }

        clicked.PlaybackState = currentState == PanoMediaPlaybackState.Playing
            ? PanoMediaPlaybackState.Paused
            : PanoMediaPlaybackState.Playing;

        UpdateDetails(clicked.MediaKind == PanoMediaKind.Video
            ? "Video preview/player tetiklendi: " + clicked.Title
            : "Audix çalma durumu değişti: " + clicked.Title);

        _grid.RefreshMediaPlayback();
    }

    private void ShowThemeAudit()
    {
        _grid.ApplyTheme51AuditDefaults(_dark ? PanoThemeStudioPreset.AudixDark : PanoThemeStudioPreset.AudixLight);
        UpdateDetails("Theme Audit: label, button, combo, badge, kart ve medya overlay için okunurluk guard aktif. Koyu/açık geçişlerde metin kaybolmamalı.");
    }

    private void ShowRuntimeChecks()
    {
        var lines = _grid.RunPano51UsageChecks()
            .Select(c => (c.Passed ? "✓ " : "! ") + c.Profile + " / " + c.Check + " - " + c.Message);
        MessageBox.Show(this, string.Join(Environment.NewLine, lines), "Pano 5.1 Usage Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ApplyTheme(bool dark)
    {
        _dark = dark;
        var preset = dark ? PanoThemeStudioPreset.AudixDark : PanoThemeStudioPreset.AudixLight;
        _grid.ApplyTheme51AuditDefaults(preset);
        var theme = PanoThemeStudio.Create(preset);
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        _header.BackColor = theme.HeaderBackColor;
        _header.ForeColor = theme.HeaderForeColor;
        _details.BackColor = theme.PanelBackColor;
        _details.ForeColor = theme.ForeColor;
        _header.Text = dark
            ? "Pano 5.1 Audix Pilot - Koyu tema okunurluk testi"
            : "Pano 5.1 Audix Pilot - Açık tema okunurluk testi";
    }

    private void LoadRows()
    {
        _rows.Clear();
        _rows.Add(new AudixTrackRow("Me Malone O Pateras Mou", "Antzela Dimitriou", "Greatest Hits", PanoMediaKind.Audio, "MP3", "03:28", CreateCover(Color.SteelBlue, "A1")));
        _rows.Add(new AudixTrackRow("Mia Litropi Agapi", "Antzela Dimitriou", "Greatest Hits", PanoMediaKind.Audio, "FLAC", "04:12", CreateCover(Color.MediumPurple, "A2")) { PlaybackState = PanoMediaPlaybackState.Playing });
        _rows.Add(new AudixTrackRow("Na Giriseis Xana", "Antzela Dimitriou", "This Is Greece", PanoMediaKind.Audio, "320", "03:57", CreateCover(Color.SeaGreen, "A3")));
        _rows.Add(new AudixTrackRow("Factory Clip 01", "Video Archive", "Factory Videos", PanoMediaKind.Video, "1080p", "01:14", CreateCover(Color.DarkOrange, "V1")));
        _rows.Add(new AudixTrackRow("AOI Defect Review", "Inspection Video", "AOI Review", PanoMediaKind.Video, "4K", "00:48", CreateCover(Color.Crimson, "V2")));
        _rows.Add(new AudixTrackRow("Kapak Eksik Örneği", "Unknown Artist", "Missing Covers", PanoMediaKind.Audio, "MISSING", "02:49", CreatePlaceholderCover()));
        _grid.SetObjects(_rows);
    }

    private void UpdateSelectionDetails()
    {
        var selected = _grid.SelectedObject as AudixTrackRow;
        if (selected == null) return;
        UpdateDetails("Seçilen medya" + Environment.NewLine +
            "Şarkı/Video: " + selected.Title + Environment.NewLine +
            "Sanatçı: " + selected.Artist + Environment.NewLine +
            "Albüm: " + selected.Album + Environment.NewLine +
            "Tür: " + selected.MediaKind + Environment.NewLine +
            "Kalite: " + selected.Quality + Environment.NewLine +
            "Süre: " + selected.Duration + Environment.NewLine +
            "Durum: " + selected.PlaybackState + Environment.NewLine + Environment.NewLine +
            "Audix entegrasyon notu: Modelde Cover/Image property, PlaybackState, Quality ve MediaKind alanları olursa Pano medya kartı state'i kendi çizebilir.");
    }

    private void UpdateDetails(string message)
    {
        _details.Text = message + Environment.NewLine + Environment.NewLine +
            "Pano 5.1 hedefi:" + Environment.NewLine +
            "• Audix'i gerçek pilot uygulama gibi kullanmak" + Environment.NewLine +
            "• Poster / MediaTile / Gallery / FilmStrip görünümlerinde kapak + play state davranışını net göstermek" + Environment.NewLine +
            "• Koyu/açık tema okunurluğunu her kontrolde test etmek" + Environment.NewLine +
            "• Example Center'da özellikleri kategori + arama + hızlı erişimle bulmayı kolaylaştırmak" + Environment.NewLine +
            "• Yeni özellik eklemekten çok mevcut özellikleri tak-çalıştır hale getirmek";
    }

    private static Bitmap CreateCover(Color baseColor, string text)
    {
        var bmp = new Bitmap(360, 520);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bg = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, bmp.Width, bmp.Height), ControlPaint.Light(baseColor), ControlPaint.DarkDark(baseColor), 48f);
        g.FillRectangle(bg, 0, 0, bmp.Width, bmp.Height);
        using var glow = new SolidBrush(Color.FromArgb(88, Color.White));
        g.FillEllipse(glow, 32, 58, 230, 230);
        using var dark = new SolidBrush(Color.FromArgb(130, Color.Black));
        g.FillRectangle(dark, 0, 350, bmp.Width, 170);
        using var font = new Font("Segoe UI", 42, FontStyle.Bold);
        TextRenderer.DrawText(g, text, font, new Rectangle(0, 168, bmp.Width, 88), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        using var small = new Font("Segoe UI", 13, FontStyle.Bold);
        TextRenderer.DrawText(g, "AUDIX / PANO", small, new Rectangle(0, 394, bmp.Width, 32), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, "Poster • MediaTile • FilmStrip", SystemFonts.CaptionFont, new Rectangle(0, 424, bmp.Width, 30), Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bmp;
    }

    private static Bitmap CreatePlaceholderCover()
    {
        var bmp = new Bitmap(360, 520);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(38, 45, 58));
        using var pen = new Pen(Color.FromArgb(115, Color.White), 4f);
        g.DrawRectangle(pen, 42, 80, 276, 260);
        using var font = new Font("Segoe UI", 30, FontStyle.Bold);
        TextRenderer.DrawText(g, "NO\nCOVER", font, new Rectangle(0, 150, bmp.Width, 120), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        using var small = new Font("Segoe UI", 12, FontStyle.Bold);
        TextRenderer.DrawText(g, "Eksik kapak", small, new Rectangle(0, 394, bmp.Width, 32), Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bmp;
    }

    private sealed class AudixTrackRow
    {
        public AudixTrackRow(string title, string artist, string album, PanoMediaKind mediaKind, string quality, string duration, Image cover)
        {
            Title = title;
            Artist = artist;
            Album = album;
            MediaKind = mediaKind;
            Quality = quality;
            Duration = duration;
            Cover = cover;
        }

        public string Title { get; }
        public string Artist { get; }
        public string Album { get; }
        public PanoMediaKind MediaKind { get; }
        public string Quality { get; }
        public string Duration { get; }
        public Image Cover { get; }
        public PanoMediaPlaybackState PlaybackState { get; set; }
    }
}

