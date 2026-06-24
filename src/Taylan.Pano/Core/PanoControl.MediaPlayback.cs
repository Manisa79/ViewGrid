using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Taylan.Pano.Columns;

namespace Taylan.Pano.Core;

public enum PanoMediaPlaybackState
{
    None = 0,
    Loading = 1,
    Playing = 2,
    Paused = 3,
    Error = 4
}

public enum PanoMediaKind
{
    Auto = 0,
    Audio = 1,
    Video = 2,
    Image = 3,
    Document = 4
}

public sealed class PanoMediaPlayPauseClickEventArgs : EventArgs
{
    public PanoMediaPlayPauseClickEventArgs(int rowIndex, object rowObject, PanoMediaPlaybackState currentState, PanoMediaKind mediaKind)
    {
        RowIndex = rowIndex;
        RowObject = rowObject;
        CurrentState = currentState;
        MediaKind = mediaKind;
        RequestedState = currentState == PanoMediaPlaybackState.Playing
            ? PanoMediaPlaybackState.Paused
            : PanoMediaPlaybackState.Playing;
    }

    public int RowIndex { get; }
    public object RowObject { get; }
    public PanoMediaPlaybackState CurrentState { get; }
    public PanoMediaPlaybackState RequestedState { get; }
    public PanoMediaKind MediaKind { get; }
    public bool Handled { get; set; }
}

public partial class PanoControl
{
    [Category("Pano - Media Playback")]
    [DefaultValue(true)]
    [Description("Medya kartlarında play/pause/loading/error gibi çalma durumunu görsel olarak gösterir.")]
    public bool ShowMediaPlaybackState { get; set; } = true;

    [Category("Pano - Media Playback")]
    [DefaultValue(true)]
    [Description("Çalan medya kartında 'Şimdi çalıyor' rozeti gösterir.")]
    public bool ShowMediaNowPlayingBadge { get; set; } = true;

    [Category("Pano - Media Playback")]
    [DefaultValue(true)]
    [Description("Çalan medya kartında kapak altında mini equalizer göstergesi çizer.")]
    public bool ShowMediaEqualizerIndicator { get; set; } = true;

    [Category("Pano - Media Playback")]
    [DefaultValue("PlaybackState")]
    [Description("Playback state için okunacak property/kolon adı. Değerler: None, Loading, Playing, Paused, Error.")]
    public string MediaPlaybackStateAspectName { get; set; } = "PlaybackState";

    [Category("Pano - Media Playback")]
    [DefaultValue("MediaKind")]
    [Description("Medya türü için okunacak property/kolon adı. Değerler: Audio, Video, Image, Document.")]
    public string MediaKindAspectName { get; set; } = "MediaKind";

    [Category("Pano - Media Playback")]
    [DefaultValue(false)]
    [Description("Video kartlarında play tıklaması için 'video preview' niyeti bildirir. Asıl player/preview host uygulama tarafından MediaPlayPauseClicked içinde açılır.")]
    public bool MediaVideoPreviewMode { get; set; }

    [Category("Pano - Media Playback")]
    [DefaultValue("Şimdi çalıyor")]
    [Description("Çalan audio/video kartında gösterilecek durum rozeti.")]
    public string MediaNowPlayingText { get; set; } = "Şimdi çalıyor";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<object, PanoMediaPlaybackState>? MediaPlaybackStateGetter { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<object, PanoMediaKind>? MediaKindGetter { get; set; }

    public event EventHandler<PanoMediaPlayPauseClickEventArgs>? MediaPlayPauseClicked;

    public void RefreshMediaPlayback()
    {
        Invalidate();
    }

    private PanoMediaPlaybackState ResolveMediaPlaybackState(object row)
    {
        if (row == null) return PanoMediaPlaybackState.None;
        if (MediaPlaybackStateGetter != null) return MediaPlaybackStateGetter(row);
        object? value = ResolveMediaNamedValue(row, MediaPlaybackStateAspectName);
        if (value == null) return PanoMediaPlaybackState.None;
        if (value is PanoMediaPlaybackState state) return state;
        if (value is bool b) return b ? PanoMediaPlaybackState.Playing : PanoMediaPlaybackState.None;
        if (Enum.TryParse<PanoMediaPlaybackState>(Convert.ToString(value), true, out var parsed)) return parsed;
        return PanoMediaPlaybackState.None;
    }

    private PanoMediaKind ResolveMediaKind(object row)
    {
        if (row == null) return PanoMediaKind.Auto;
        if (MediaKindGetter != null) return MediaKindGetter(row);
        object? value = ResolveMediaNamedValue(row, MediaKindAspectName);
        if (value == null) return PanoMediaKind.Auto;
        if (value is PanoMediaKind kind) return kind;
        if (Enum.TryParse<PanoMediaKind>(Convert.ToString(value), true, out var parsed)) return parsed;
        return PanoMediaKind.Auto;
    }

    private object? ResolveMediaNamedValue(object row, string aspectName)
    {
        if (row == null || string.IsNullOrWhiteSpace(aspectName)) return null;
        var col = Columns.FirstOrDefault(c =>
            string.Equals(c.AspectName, aspectName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Header, aspectName, StringComparison.OrdinalIgnoreCase));
        if (col != null) return col.GetValue(row);
        var prop = row.GetType().GetProperty(aspectName);
        return prop?.GetValue(row);
    }

    private void DrawMediaPlaybackChrome(Graphics g, Rectangle imageBounds, object row, Color cardBack, bool isHot, bool isSelected)
    {
        if (imageBounds.Width < 40 || imageBounds.Height < 40) return;

        var state = ResolveMediaPlaybackState(row);
        var kind = ResolveMediaKind(row);
        bool isPlaying = state == PanoMediaPlaybackState.Playing;
        bool showCenter = ShowMediaOverlayButton && (isHot || isSelected || state != PanoMediaPlaybackState.None);

        if (ShowMediaPlaybackState && isPlaying)
        {
            using var glowPen = new Pen(Color.FromArgb(210, _theme.AccentColor), 2.5f);
            g.DrawRoundedRectangle(glowPen, Rectangle.Inflate(imageBounds, 2, 2), Math.Max(6, _theme.CornerRadius));
        }

        if (ShowMediaPlaybackState && ShowMediaNowPlayingBadge && isPlaying)
            DrawMediaStateBadge(g, imageBounds, string.IsNullOrWhiteSpace(MediaNowPlayingText) ? "Şimdi çalıyor" : MediaNowPlayingText, cardBack);

        if (ShowMediaPlaybackState && ShowMediaEqualizerIndicator && isPlaying)
            DrawMediaEqualizer(g, imageBounds);

        if (showCenter)
            DrawMediaPlaybackButton(g, imageBounds, cardBack, state, kind);
    }

    private void DrawMediaStateBadge(Graphics g, Rectangle imageBounds, string text, Color cardBack)
    {
        Size textSize = TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.NoPadding);
        int w = Math.Min(imageBounds.Width - 12, Math.Max(88, textSize.Width + 20));
        if (w <= 18) return;
        var rect = new Rectangle(imageBounds.Left + 8, imageBounds.Top + 8, w, 24);
        Color back = Color.FromArgb(225, _theme.AccentColor);
        Color fore = BestTextOn(back, Color.White);
        using var brush = new SolidBrush(back);
        g.FillRoundedRectangle(brush, rect, 11);
        TextRenderer.DrawText(g, text, Font, rect, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private void DrawMediaEqualizer(Graphics g, Rectangle imageBounds)
    {
        var rect = new Rectangle(imageBounds.Left + 10, imageBounds.Bottom - 27, 40, 18);
        Color back = Color.FromArgb(170, _theme.IsDark ? Color.Black : Color.White);
        Color fore = EnsureReadableTextOn(back, _theme.AccentColor);
        using var bb = new SolidBrush(back);
        g.FillRoundedRectangle(bb, rect, 7);
        using var pen = new Pen(fore, 3f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        int[] heights = { 7, 13, 10, 16 };
        for (int i = 0; i < heights.Length; i++)
        {
            int x = rect.Left + 9 + i * 7;
            int h = heights[i];
            g.DrawLine(pen, x, rect.Bottom - 5, x, rect.Bottom - 5 - h);
        }
    }

    private void DrawMediaPlaybackButton(Graphics g, Rectangle imageBounds, Color cardBack, PanoMediaPlaybackState state, PanoMediaKind kind)
    {
        var rect = GetMediaPlaybackButtonRect(imageBounds);
        if (rect.IsEmpty) return;

        string text = state switch
        {
            PanoMediaPlaybackState.Playing => "Ⅱ",
            PanoMediaPlaybackState.Loading => "…",
            PanoMediaPlaybackState.Error => "!",
            _ => kind == PanoMediaKind.Video && MediaVideoPreviewMode ? "▶" : (string.IsNullOrWhiteSpace(MediaOverlayButtonText) ? "▶" : MediaOverlayButtonText.Trim())
        };

        Color back = Color.FromArgb(_theme.IsDark ? 220 : 230, _theme.IsDark ? Color.Black : Color.White);
        Color fore = state == PanoMediaPlaybackState.Error
            ? Color.FromArgb(255, 80, 80)
            : EnsureReadableTextOn(back, _theme.AccentColor);

        using var brush = new SolidBrush(back);
        g.FillEllipse(brush, rect);
        using var pen = new Pen(Color.FromArgb(_theme.IsDark ? 190 : 150, state == PanoMediaPlaybackState.Error ? Color.Red : _theme.AccentColor), 2f);
        g.DrawEllipse(pen, rect);
        using var font = new Font(Font.FontFamily, Math.Max(12f, Font.Size + 4f), FontStyle.Bold);
        TextRenderer.DrawText(g, text, font, rect, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private Rectangle GetMediaPlaybackButtonRect(Rectangle imageBounds)
    {
        if (imageBounds.Width < 48 || imageBounds.Height < 48) return Rectangle.Empty;
        int size = Math.Min(58, Math.Max(40, Math.Min(imageBounds.Width, imageBounds.Height) / 4));
        return new Rectangle(imageBounds.Left + (imageBounds.Width - size) / 2, imageBounds.Top + (imageBounds.Height - size) / 2, size, size);
    }

    private bool TryGetMediaImageBounds(int viewRowIndex, out Rectangle imageBounds)
    {
        imageBounds = Rectangle.Empty;
        if (!IsTileView) return false;
        bool mediaLayout = TilePosterMode || ViewMode is PanoViewMode.Poster or PanoViewMode.Gallery or PanoViewMode.MediaTile or PanoViewMode.FilmStrip;
        if (!mediaLayout) return false;
        var row = GetViewRow(viewRowIndex);
        if (row == null) return false;
        var visible = Columns.VisibleColumns.Where(c => c.Kind != PanoColumnKind.CheckBox && c.Width > 0).ToList();
        var iconCol = visible.FirstOrDefault(c => c.Kind == PanoColumnKind.Icon || c.Kind == PanoColumnKind.Image);
        if (iconCol == null) return false;
        var cardBounds = GetCellBounds(viewRowIndex, iconCol);
        if (cardBounds.IsEmpty) return false;

        int defaultImageHeight = ViewMode switch
        {
            PanoViewMode.Poster => PosterImageHeight,
            PanoViewMode.Gallery => Math.Max(110, TilePosterImageHeight),
            PanoViewMode.MediaTile => Math.Max(82, TilePosterImageHeight),
            PanoViewMode.FilmStrip => Math.Max(96, cardBounds.Height - 52),
            _ => TilePosterImageHeight
        };
        int posterH = Math.Min(Math.Max(72, defaultImageHeight), Math.Max(72, cardBounds.Height - 54));
        Rectangle pr = ViewMode == PanoViewMode.FilmStrip
            ? new Rectangle(cardBounds.Left + 12, cardBounds.Top + 10, Math.Min(190, Math.Max(120, cardBounds.Width / 3)), posterH)
            : new Rectangle(cardBounds.Left + 10, cardBounds.Top + 10, Math.Max(30, cardBounds.Width - 20), posterH);
        imageBounds = Rectangle.Inflate(pr, -1, -1);
        return !imageBounds.IsEmpty;
    }

    private bool TryProcessMediaPlaybackClick(Point location, int rowIndex, object row)
    {
        if (!ShowMediaOverlayButton && !ShowMediaPlaybackState) return false;
        if (!TryGetMediaImageBounds(rowIndex, out var imageBounds)) return false;
        var buttonRect = GetMediaPlaybackButtonRect(imageBounds);
        if (buttonRect.IsEmpty || !buttonRect.Contains(location)) return false;

        var args = new PanoMediaPlayPauseClickEventArgs(rowIndex, row, ResolveMediaPlaybackState(row), ResolveMediaKind(row));
        MediaPlayPauseClicked?.Invoke(this, args);
        Invalidate();
        return true;
    }

    private bool IsMediaPlaybackHot(Point location, int rowIndex)
    {
        if (!ShowMediaOverlayButton && !ShowMediaPlaybackState) return false;
        return rowIndex >= 0 && TryGetMediaImageBounds(rowIndex, out var imageBounds) && GetMediaPlaybackButtonRect(imageBounds).Contains(location);
    }
}
