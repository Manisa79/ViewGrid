using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using Taylan.Pano.Columns;
using Taylan.Pano.Exporting;

namespace Taylan.Pano.Core;

public partial class PanoControl
{
    private readonly Dictionary<string, string> _cellValidationErrors = new(StringComparer.OrdinalIgnoreCase);

    [Category("Pano - Enterprise"), DefaultValue(true)]
    [Description("Ctrl+K ile komut paletini açar. Kullanıcı kolon, filtre, export ve layout komutlarını hızlı çalıştırır.")]
    public bool EnableCommandPalette { get; set; } = true;

    [Category("Pano - Enterprise"), DefaultValue(true)]
    [Description("Ctrl+F ile filtre odaklı komut paleti açılır.")]
    public bool EnableKeyboardFilterShortcut { get; set; } = true;

    [Category("Pano - Enterprise"), DefaultValue(true)]
    [Description("Hücre doğrulama hatalarını küçük kırmızı rozet/ikon ile gösterir.")]
    public bool ShowCellValidationErrors { get; set; } = true;

    [Category("Pano - Enterprise"), DefaultValue(typeof(Color), "Empty")]
    public Color ValidationErrorColor { get; set; } = Color.Empty;

    [Category("Pano - Enterprise"), DefaultValue("Sıralanıyor...")]
    public string SortBusyTitle { get; set; } = "Sıralanıyor...";

    [Category("Pano - Enterprise"), DefaultValue("Lütfen bekleyin, liste arka planda hazırlanıyor.")]
    public string SortBusyDetail { get; set; } = "Lütfen bekleyin, liste arka planda hazırlanıyor.";

    [Category("Pano - Enterprise"), DefaultValue(460)]
    public int SortBusyOverlayWidth { get; set; } = 460;

    [Category("Pano - Enterprise"), DefaultValue(138)]
    public int SortBusyOverlayHeight { get; set; } = 138;

    [Category("Pano - Enterprise"), DefaultValue(125)]
    public int SortBusyDimOpacity { get; set; } = 125;

    [Browsable(false)]
    public bool CanUndo => _undo.CanUndo;

    [Browsable(false)]
    public bool CanRedo => _undo.CanRedo;

    public event EventHandler<PanoValidationEventArgs>? CellValidationNeeded;

    public void ShowCommandPalette(string? initialSearch = null)
    {
        if (!EnableCommandPalette) return;
        using var form = new PanoCommandPaletteForm(this, initialSearch ?? string.Empty);
        form.ShowDialog((IWin32Window?)FindForm() ?? this);
    }

    public void SetCellError(object rowObject, PanoColumn column, string? message)
    {
        if (rowObject == null || column == null) return;
        var key = GetValidationKey(rowObject, column);
        if (string.IsNullOrWhiteSpace(message)) _cellValidationErrors.Remove(key);
        else _cellValidationErrors[key] = message!;
        Invalidate();
    }

    public string? GetCellError(object rowObject, PanoColumn column)
    {
        if (rowObject == null || column == null) return null;
        var args = new PanoValidationEventArgs(rowObject, column);
        CellValidationNeeded?.Invoke(this, args);
        if (!string.IsNullOrWhiteSpace(args.ErrorText)) return args.ErrorText;
        return _cellValidationErrors.TryGetValue(GetValidationKey(rowObject, column), out var error) ? error : null;
    }

    public void ClearCellErrors()
    {
        _cellValidationErrors.Clear();
        Invalidate();
    }

    public string ExportVisibleHtml(string path, string title = "PanoControl") => SaveHtml(path, title, Columns.VisibleColumns.ToArray(), GetVisibleObjects());
    public string ExportSelectedHtml(string path, string title = "PanoSelection") => SaveHtml(path, title, Columns.VisibleColumns.ToArray(), SelectedObjects);

    public string ExportVisibleCsvWithDialog()
    {
        using var dlg = new SaveFileDialog { Title = "Pano CSV dışa aktar", Filter = "CSV (*.csv)|*.csv|Tüm dosyalar (*.*)|*.*", FileName = "pano-visible.csv" };
        return dlg.ShowDialog(this) == DialogResult.OK ? ExportVisibleCsv(dlg.FileName) : string.Empty;
    }

    public string ExportVisibleExcelWithDialog()
    {
        using var dlg = new SaveFileDialog { Title = "Pano Excel dışa aktar", Filter = "Excel XML (*.xml)|*.xml|Tüm dosyalar (*.*)|*.*", FileName = "pano-visible.xml" };
        return dlg.ShowDialog(this) == DialogResult.OK ? ExportVisibleExcel(dlg.FileName) : string.Empty;
    }

    public string ExportSelectedCsvWithDialog()
    {
        using var dlg = new SaveFileDialog { Title = "Pano seçimi CSV dışa aktar", Filter = "CSV (*.csv)|*.csv|Tüm dosyalar (*.*)|*.*", FileName = "pano-selection.csv" };
        return dlg.ShowDialog(this) == DialogResult.OK ? ExportSelectedCsv(dlg.FileName) : string.Empty;
    }

    private static string SaveHtml(string path, string title, IReadOnlyList<PanoColumn> columns, IEnumerable<object> rows)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Dosya yolu boş olamaz.", nameof(path));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\">");
        sb.Append("<title>").Append(WebUtility.HtmlEncode(title)).AppendLine("</title>");
        sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:16px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ddd;padding:6px 8px;text-align:left}th{background:#f3f5f7}tr:nth-child(even){background:#fafafa}</style>");
        sb.AppendLine("</head><body>");
        sb.Append("<h2>").Append(WebUtility.HtmlEncode(title)).AppendLine("</h2><table><thead><tr>");
        foreach (var c in columns) sb.Append("<th>").Append(WebUtility.HtmlEncode(c.Header)).AppendLine("</th>");
        sb.AppendLine("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            sb.AppendLine("<tr>");
            foreach (var c in columns) sb.Append("<td>").Append(WebUtility.HtmlEncode(Convert.ToString(c.GetValue(row)) ?? string.Empty)).AppendLine("</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table></body></html>");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private void DrawValidationAdornerIfNeeded(Graphics g, Rectangle cellBounds, object row, PanoColumn column)
    {
        if (!ShowCellValidationErrors || cellBounds.Width < 12 || cellBounds.Height < 12) return;
        var error = GetCellError(row, column);
        if (string.IsNullOrWhiteSpace(error)) return;
        var color = ValidationErrorColor == Color.Empty ? Color.FromArgb(220, 42, 42) : ValidationErrorColor;
        var tri = new[]
        {
            new Point(cellBounds.Right - 1, cellBounds.Top + 1),
            new Point(cellBounds.Right - 1, cellBounds.Top + 14),
            new Point(cellBounds.Right - 14, cellBounds.Top + 1)
        };
        using var b = new SolidBrush(color);
        using var p = new Pen(Color.FromArgb(230, color), 1f);
        g.FillPolygon(b, tri);
        g.DrawPolygon(p, tri);
        if (cellBounds.Width >= 38)
        {
            var badge = new Rectangle(cellBounds.Right - 22, cellBounds.Top + Math.Max(2, (cellBounds.Height - 16) / 2), 16, 16);
            using var bg = new SolidBrush(color);
            g.FillEllipse(bg, badge);
            using var f = new Font(Font.FontFamily, Math.Max(7, Font.Size - 1), FontStyle.Bold);
            TextRenderer.DrawText(g, "!", f, badge, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    private static string GetValidationKey(object rowObject, PanoColumn column)
        => RuntimeHelpers.GetHashCode(rowObject).ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + (column.AspectName ?? column.Header ?? string.Empty);
}

public sealed class PanoValidationEventArgs : EventArgs
{
    public PanoValidationEventArgs(object rowObject, PanoColumn column)
    {
        RowObject = rowObject;
        Column = column;
    }

    public object RowObject { get; }
    public PanoColumn Column { get; }
    public string? ErrorText { get; set; }
}
