using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Taylan.Pano.Columns;
using Taylan.Pano.Core;

namespace Taylan.Pano.Exporting;

public static class PanoExporter
{
    public static string ToCsv(IEnumerable<PanoColumn> columns, IEnumerable<object> rows, char separator = ';')
    {
        var cols = columns.Where(c => !c.PrivateColumn && c.Visible).ToList();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(separator, cols.Select(c => Escape(c.Header, separator))));
        foreach (var row in rows)
            sb.AppendLine(string.Join(separator, cols.Select(c => Escape(Convert.ToString(c.GetValue(row)) ?? string.Empty, separator))));
        return sb.ToString();
    }

    /// <summary>
    /// Saves as CSV. If the target file is locked or access is denied, retries with a unique file name.
    /// The returned value is the actual file path used.
    /// </summary>
    public static string SaveCsv(string path, IEnumerable<PanoColumn> columns, IEnumerable<object> rows, char separator = ';')
    {
        var text = ToCsv(columns, rows, separator);
        var actualPath = GetWritablePath(path);
        File.WriteAllText(actualPath, text, new UTF8Encoding(true));
        return actualPath;
    }

    public static string ToHtmlTable(IEnumerable<PanoColumn> columns, IEnumerable<object> rows)
    {
        var cols = columns.Where(c => !c.PrivateColumn && c.Visible).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr>" + string.Join("", cols.Select(c => $"<th>{Html(c.Header)}</th>")) + "</tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var row in rows)
            sb.AppendLine("<tr>" + string.Join("", cols.Select(c => $"<td>{Html(Convert.ToString(c.GetValue(row)) ?? string.Empty)}</td>")) + "</tr>");
        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    public static string ToJson(IEnumerable<PanoColumn> columns, IEnumerable<object> rows)
    {
        var cols = columns.Where(c => !c.PrivateColumn && c.Visible).ToList();
        var data = rows.Select(row =>
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in cols) dict[col.Header] = col.GetValue(row);
            return dict;
        }).ToList();
        return System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    public static string SaveJson(string path, IEnumerable<PanoColumn> columns, IEnumerable<object> rows)
    {
        if (string.IsNullOrWhiteSpace(path))
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Pano.json");
        if (!Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
            path = Path.ChangeExtension(path, ".json");
        var actualPath = GetWritablePath(path);
        File.WriteAllText(actualPath, ToJson(columns, rows), new UTF8Encoding(false));
        return actualPath;
    }

    /// <summary>
    /// Saves a production-oriented PDF export. The default overload exports a printable table.
    /// For card/dashboard output use the PanoPdfExportOptions overload with Mode=Card.
    /// </summary>
    public static string SavePdf(string path, IEnumerable<PanoColumn> columns, IEnumerable<object> rows, string title = "PanoControl")
    {
        return SavePdf(path, columns, rows, new PanoPdfExportOptions { Title = title, Mode = PanoPdfExportMode.Table });
    }

    /// <summary>
    /// Saves a styled PDF without external dependencies. Supports Details/Table and Card/Dashboard exports,
    /// paging, headers/footers, basic grid styling, card accent bars/status dots/badges and print-friendly colors.
    /// </summary>
    public static string SavePdf(string path, IEnumerable<PanoColumn> columns, IEnumerable<object> rows, PanoPdfExportOptions options)
    {
        if (string.IsNullOrWhiteSpace(path))
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Pano.pdf");
        if (!Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            path = Path.ChangeExtension(path, ".pdf");

        options ??= new PanoPdfExportOptions();
        var cols = columns
            .Where(c => (options.IncludeHiddenColumns || (!c.PrivateColumn && c.Visible)) && (options.ColumnPredicate == null || options.ColumnPredicate(c)))
            .ToList();
        var visibleRows = rows.Take(Math.Max(1, options.MaxRows)).ToList();
        var actualPath = GetWritablePath(path);
        byte[] pdf = BuildStyledPdf(cols, visibleRows, options);
        File.WriteAllBytes(actualPath, pdf);
        return actualPath;
    }

    private sealed class PdfPage
    {
        public StringBuilder Content { get; } = new();
    }

    private static byte[] BuildStyledPdf(IReadOnlyList<PanoColumn> columns, IReadOnlyList<object> rows, PanoPdfExportOptions options)
    {
        int pageWidth = options.Orientation == PanoPdfPageOrientation.Landscape ? 842 : 595;
        int pageHeight = options.Orientation == PanoPdfPageOrientation.Landscape ? 595 : 842;
        var pages = new List<PdfPage>();
        var mode = options.Mode == PanoPdfExportMode.Auto ? PanoPdfExportMode.Table : options.Mode;
        if (mode == PanoPdfExportMode.Card)
            RenderPdfCards(pages, pageWidth, pageHeight, columns, rows, options);
        else
            RenderPdfTable(pages, pageWidth, pageHeight, columns, rows, options);
        return BuildPdfFromPages(pages, pageWidth, pageHeight);
    }

    private static void RenderPdfTable(List<PdfPage> pages, int pageWidth, int pageHeight, IReadOnlyList<PanoColumn> columns, IReadOnlyList<object> rows, PanoPdfExportOptions options)
    {
        int left = Math.Max(18, options.MarginLeft);
        int right = Math.Max(18, options.MarginRight);
        int top = Math.Max(18, options.MarginTop);
        int bottom = Math.Max(18, options.MarginBottom);
        int usableWidth = pageWidth - left - right;
        int headerReserve = options.ShowHeader ? 56 : 16;
        int footerReserve = options.ShowFooter ? 26 : 8;
        int headerHeight = 22;
        int rowHeight = 18;
        var widths = ComputePdfColumnWidths(columns, usableWidth, options.FitToPageWidth);
        int rowIndex = 0;
        int pageNo = 0;

        while (rowIndex < rows.Count || pages.Count == 0)
        {
            pageNo++;
            var page = NewPdfPage(pages, pageWidth, pageHeight, options, pageNo);
            int y = pageHeight - top - headerReserve;
            DrawPdfRect(page, left, y - headerHeight, usableWidth, headerHeight, Color.FromArgb(235, 239, 246), null);
            int x = left;
            for (int i = 0; i < columns.Count; i++)
            {
                if (options.ShowGridLines) DrawPdfRect(page, x, y - headerHeight, widths[i], headerHeight, Color.Empty, Color.FromArgb(165, 175, 190));
                DrawPdfText(page, TrimForPdf(columns[i].Header, 28), x + 4, y - 15, 8, Color.FromArgb(25, 35, 48), true);
                x += widths[i];
            }
            y -= headerHeight;

            int minY = bottom + footerReserve;
            while (rowIndex < rows.Count && y - rowHeight >= minY)
            {
                object row = rows[rowIndex];
                Color back = options.ZebraRows && rowIndex % 2 == 1 ? Color.FromArgb(248, 250, 252) : Color.White;
                DrawPdfRect(page, left, y - rowHeight, usableWidth, rowHeight, back, options.ShowGridLines ? Color.FromArgb(222, 228, 236) : null);
                x = left;
                for (int i = 0; i < columns.Count; i++)
                {
                    string text = Convert.ToString(columns[i].GetValue(row)) ?? string.Empty;
                    if (options.ShowGridLines) DrawPdfLine(page, x, y, x, y - rowHeight, Color.FromArgb(222, 228, 236), 0.5f);
                    DrawPdfText(page, TrimForPdf(text, Math.Max(8, widths[i] / 5)), x + 4, y - 13, 7.2f, Color.FromArgb(30, 38, 50), false);
                    x += widths[i];
                }
                if (options.ShowGridLines) DrawPdfLine(page, left + usableWidth, y, left + usableWidth, y - rowHeight, Color.FromArgb(222, 228, 236), 0.5f);
                y -= rowHeight;
                rowIndex++;
            }
            DrawPdfFooter(page, pageWidth, pageHeight, options, pageNo, rows.Count);
            if (rowIndex >= rows.Count) break;
        }
    }

    private static void RenderPdfCards(List<PdfPage> pages, int pageWidth, int pageHeight, IReadOnlyList<PanoColumn> columns, IReadOnlyList<object> rows, PanoPdfExportOptions options)
    {
        int left = Math.Max(18, options.MarginLeft);
        int right = Math.Max(18, options.MarginRight);
        int top = Math.Max(18, options.MarginTop);
        int bottom = Math.Max(18, options.MarginBottom);
        int usableWidth = pageWidth - left - right;
        int headerReserve = options.ShowHeader ? 56 : 16;
        int footerReserve = options.ShowFooter ? 26 : 8;
        int gap = Math.Clamp(options.CardGap, 4, 24);
        int cardColumns = Math.Clamp(options.CardColumns, 1, 4);
        int cardWidth = (usableWidth - (cardColumns - 1) * gap) / cardColumns;
        int cardHeight = Math.Clamp(options.CardMinHeight, 72, 180);
        int pageNo = 0;
        PdfPage? page = null;
        int y = 0;
        int col = 0;

        for (int rowIndex = 0; rowIndex < rows.Count || rowIndex == 0 && rows.Count == 0; rowIndex++)
        {
            if (page == null || y - cardHeight < bottom + footerReserve)
            {
                if (page != null) DrawPdfFooter(page, pageWidth, pageHeight, options, pageNo, rows.Count);
                pageNo++;
                page = NewPdfPage(pages, pageWidth, pageHeight, options, pageNo);
                y = pageHeight - top - headerReserve;
                col = 0;
            }
            if (rows.Count == 0) break;

            int x = left + col * (cardWidth + gap);
            DrawPdfCard(page, x, y - cardHeight, cardWidth, cardHeight, columns, rows[rowIndex], options);
            col++;
            if (col >= cardColumns)
            {
                col = 0;
                y -= cardHeight + gap;
            }
        }
        if (page == null)
        {
            pageNo++;
            page = NewPdfPage(pages, pageWidth, pageHeight, options, pageNo);
        }
        DrawPdfFooter(page, pageWidth, pageHeight, options, pageNo, rows.Count);
    }

    private static PdfPage NewPdfPage(List<PdfPage> pages, int pageWidth, int pageHeight, PanoPdfExportOptions options, int pageNo)
    {
        var page = new PdfPage();
        pages.Add(page);
        DrawPdfRect(page, 0, 0, pageWidth, pageHeight, Color.White, null);
        if (options.ShowHeader)
        {
            DrawPdfText(page, options.Title ?? "Pano Export", options.MarginLeft, pageHeight - 30, 13, Color.FromArgb(20, 30, 45), true);
            if (!string.IsNullOrWhiteSpace(options.Subtitle))
                DrawPdfText(page, options.Subtitle!, options.MarginLeft, pageHeight - 46, 8, Color.FromArgb(90, 100, 115), false);
            if (options.ShowFilterSummary)
                DrawPdfText(page, DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture), pageWidth - options.MarginRight - 92, pageHeight - 30, 7.5f, Color.FromArgb(90, 100, 115), false);
            DrawPdfLine(page, options.MarginLeft, pageHeight - 56, pageWidth - options.MarginRight, pageHeight - 56, Color.FromArgb(215, 222, 232), 0.7f);
        }
        return page;
    }

    private static void DrawPdfCard(PdfPage page, int x, int y, int w, int h, IReadOnlyList<PanoColumn> columns, object row, PanoPdfExportOptions options)
    {
        PanoCardVisualInfo? visual = null;
        try { visual = options.CardVisualInfoResolver?.Invoke(row); } catch { visual = null; }
        Color accent = visual?.AccentColor ?? visual?.DotColor ?? Color.FromArgb(59, 130, 246);
        DrawPdfRect(page, x, y, w, h, Color.FromArgb(248, 250, 252), Color.FromArgb(170, 185, 205));
        if (visual?.AccentColor != null)
            DrawPdfRect(page, x, y + h - 5, w, 5, accent, null);
        if (visual?.DotColor != null)
            DrawPdfCircle(page, x + 12, y + h - 19, 4, visual.DotColor.Value, null);

        int textX = x + 20;
        int textY = y + h - 18;
        int line = 0;
        var layout = options.CardLayout;
        IEnumerable<PanoColumn> cardColumns = columns.Where(c => c.VisibleInCard).OrderBy(c => c.CardOrder);
        if (layout?.Fields.Count > 0)
        {
            var map = columns.ToDictionary(c => c.LayoutKey, c => c, StringComparer.OrdinalIgnoreCase);
            cardColumns = layout.Fields.Where(f => f.Visible && map.ContainsKey(f.ColumnKey)).OrderBy(f => f.Order).Select(f => map[f.ColumnKey]);
        }
        foreach (PanoColumn column in cardColumns.Take(6))
        {
            string value = Convert.ToString(column.GetValue(row)) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value)) continue;
            bool title = line == 0;
            string prefix = column.CardShowCaption && !title ? column.Header + ": " : string.Empty;
            DrawPdfText(page, TrimForPdf(prefix + value, title ? 32 : 40), textX, textY - line * 12, title ? 8.8f : 7.4f, title ? Color.FromArgb(18, 30, 46) : Color.FromArgb(70, 82, 98), title);
            line++;
        }

        if (visual?.Badges.Count > 0)
        {
            int bx = x + w - 12;
            foreach (PanoCardBadge badge in visual.Badges.Take(4))
            {
                string text = string.IsNullOrWhiteSpace(badge.Text) ? GlyphText(badge.Glyph) : badge.Text!;
                int bw = Math.Clamp(text.Length * 5 + 10, 18, 48);
                bx -= bw;
                Color bc = badge.BackColor ?? accent;
                DrawPdfRect(page, bx, y + 8, bw, 15, bc, null);
                DrawPdfText(page, TrimForPdf(text, 8), bx + 4, y + 12, 6.4f, badge.ForeColor ?? Color.White, true);
                bx -= 4;
            }
        }
    }

    private static int[] ComputePdfColumnWidths(IReadOnlyList<PanoColumn> columns, int usableWidth, bool fitToPage)
    {
        if (columns.Count == 0) return Array.Empty<int>();
        int[] raw = columns.Select(c => Math.Max(40, c.Width <= 0 ? 100 : c.Width)).ToArray();
        if (!fitToPage) return raw;
        int total = raw.Sum();
        if (total <= 0) return Enumerable.Repeat(Math.Max(40, usableWidth / columns.Count), columns.Count).ToArray();
        int[] widths = raw.Select(w => Math.Max(36, (int)Math.Floor(w * usableWidth / (double)total))).ToArray();
        int delta = usableWidth - widths.Sum();
        if (widths.Length > 0) widths[^1] += delta;
        return widths;
    }

    private static void DrawPdfFooter(PdfPage page, int pageWidth, int pageHeight, PanoPdfExportOptions options, int pageNo, int rowCount)
    {
        if (!options.ShowFooter) return;
        string left = string.IsNullOrWhiteSpace(options.FooterText) ? $"{rowCount:N0} kayıt" : options.FooterText!;
        DrawPdfLine(page, options.MarginLeft, options.MarginBottom + 18, pageWidth - options.MarginRight, options.MarginBottom + 18, Color.FromArgb(220, 226, 235), 0.6f);
        DrawPdfText(page, left, options.MarginLeft, options.MarginBottom + 7, 7, Color.FromArgb(95, 105, 118), false);
        DrawPdfText(page, $"Sayfa {pageNo}", pageWidth - options.MarginRight - 48, options.MarginBottom + 7, 7, Color.FromArgb(95, 105, 118), false);
    }

    private static void DrawPdfText(PdfPage page, string text, float x, float y, float size, Color color, bool bold)
    {
        page.Content.Append("BT ")
            .Append(PdfColor(color)).Append(" rg ")
            .Append(bold ? "/F2 " : "/F1 ").Append(size.ToString("0.##", CultureInfo.InvariantCulture)).Append(" Tf ")
            .Append(x.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
            .Append(y.ToString("0.##", CultureInfo.InvariantCulture)).Append(" Td (")
            .Append(PdfEscape(text)).Append(") Tj ET\n");
    }

    private static void DrawPdfRect(PdfPage page, float x, float y, float w, float h, Color fill, Color? stroke)
    {
        if (fill != Color.Empty)
        {
            page.Content.Append(PdfColor(fill)).Append(" rg ")
                .Append(x.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                .Append(y.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                .Append(w.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                .Append(h.ToString("0.##", CultureInfo.InvariantCulture)).Append(" re f\n");
        }
        if (stroke.HasValue)
        {
            page.Content.Append(PdfColor(stroke.Value)).Append(" RG 0.6 w ")
                .Append(x.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                .Append(y.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                .Append(w.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                .Append(h.ToString("0.##", CultureInfo.InvariantCulture)).Append(" re S\n");
        }
    }

    private static void DrawPdfLine(PdfPage page, float x1, float y1, float x2, float y2, Color color, float width)
    {
        page.Content.Append(PdfColor(color)).Append(" RG ")
            .Append(width.ToString("0.##", CultureInfo.InvariantCulture)).Append(" w ")
            .Append(x1.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
            .Append(y1.ToString("0.##", CultureInfo.InvariantCulture)).Append(" m ")
            .Append(x2.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
            .Append(y2.ToString("0.##", CultureInfo.InvariantCulture)).Append(" l S\n");
    }

    private static void DrawPdfCircle(PdfPage page, float cx, float cy, float r, Color fill, Color? stroke)
    {
        const float k = 0.55228475f;
        page.Content.Append(PdfColor(fill)).Append(" rg ");
        if (stroke.HasValue) page.Content.Append(PdfColor(stroke.Value)).Append(" RG ");
        page.Content.Append((cx + r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append(cy.ToString("0.##", CultureInfo.InvariantCulture)).Append(" m ")
            .Append((cx + r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append((cy + k * r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
            .Append((cx + k * r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append((cy + r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
            .Append(cx.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append((cy + r).ToString("0.##", CultureInfo.InvariantCulture)).Append(" c ")
            .Append((cx - k * r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append((cy + r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
            .Append((cx - r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append((cy + k * r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
            .Append((cx - r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append(cy.ToString("0.##", CultureInfo.InvariantCulture)).Append(" c ")
            .Append((cx - r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append((cy - k * r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
            .Append((cx - k * r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append((cy - r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
            .Append(cx.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append((cy - r).ToString("0.##", CultureInfo.InvariantCulture)).Append(" c ")
            .Append((cx + k * r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append((cy - r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
            .Append((cx + r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append((cy - k * r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
            .Append((cx + r).ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append(cy.ToString("0.##", CultureInfo.InvariantCulture)).Append(stroke.HasValue ? " c B\n" : " c f\n");
    }

    private static string PdfColor(Color color)
    {
        return (color.R / 255d).ToString("0.###", CultureInfo.InvariantCulture) + " " +
               (color.G / 255d).ToString("0.###", CultureInfo.InvariantCulture) + " " +
               (color.B / 255d).ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static byte[] BuildPdfFromPages(IReadOnlyList<PdfPage> pages, int pageWidth, int pageHeight)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "__PAGES__",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"
        };
        var pageObjectIndexes = new List<int>();
        foreach (PdfPage pdfPage in pages)
        {
            string stream = pdfPage.Content.ToString();
            int contentObjectIndex = objects.Count + 1;
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
            int pageObjectIndex = objects.Count + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidth} {pageHeight}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectIndex} 0 R >>");
            pageObjectIndexes.Add(pageObjectIndex);
        }
        string kids = string.Join(" ", pageObjectIndexes.Select(i => $"{i} 0 R"));
        objects[1] = $"<< /Type /Pages /Kids [{kids}] /Count {pageObjectIndexes.Count} >>";

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\n" };
        var offsets = new List<long> { 0 };
        writer.Write("%PDF-1.4\n");
        writer.Flush();
        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            writer.Write(i + 1);
            writer.Write(" 0 obj\n");
            writer.Write(objects[i]);
            writer.Write("\nendobj\n");
            writer.Flush();
        }
        long xref = ms.Position;
        writer.Write("xref\n0 ");
        writer.Write(objects.Count + 1);
        writer.Write("\n0000000000 65535 f \n");
        foreach (long offset in offsets.Skip(1)) writer.Write(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
        writer.Write("trailer\n<< /Size ");
        writer.Write(objects.Count + 1);
        writer.Write(" /Root 1 0 R >>\nstartxref\n");
        writer.Write(xref);
        writer.Write("\n%%EOF");
        writer.Flush();
        return ms.ToArray();
    }

    private static string GlyphText(PanoCardGlyph glyph)
    {
        return glyph switch
        {
            PanoCardGlyph.Warning => "!",
            PanoCardGlyph.Error => "X",
            PanoCardGlyph.Success => "OK",
            PanoCardGlyph.Message => "MSG",
            PanoCardGlyph.Attachment => "ATT",
            PanoCardGlyph.Pin => "PIN",
            PanoCardGlyph.Lock => "LOCK",
            PanoCardGlyph.Star => "*",
            PanoCardGlyph.Check => "OK",
            PanoCardGlyph.Clock => "TIME",
            PanoCardGlyph.Flag => "FLAG",
            PanoCardGlyph.Bell => "BELL",
            _ => "i"
        };
    }

    private static string TrimForPdf(string value, int max)
    {
        value = NormalizePdfText(value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        return value.Length <= max ? value : value[..Math.Max(0, max - 1)] + "…";
    }

    private static string PdfEscape(string value)
    {
        value = NormalizePdfText(value);
        var sb = new StringBuilder(value.Length);
        foreach (char ch in value)
        {
            if (ch == '(' || ch == ')' || ch == '\\') sb.Append('\\');
            if (ch >= 32 && ch <= 126) sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string NormalizePdfText(string value)
    {
        return (value ?? string.Empty)
            .Replace('ı', 'i').Replace('İ', 'I').Replace('ğ', 'g').Replace('Ğ', 'G')
            .Replace('ü', 'u').Replace('Ü', 'U').Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ö', 'o').Replace('Ö', 'O').Replace('ç', 'c').Replace('Ç', 'C')
            .Replace('–', '-').Replace('—', '-').Replace('…', '.');
    }

    /// <summary>
    /// Saves visible rows as a real .xlsx workbook using only built-in .NET zip/xml APIs.
    /// If the caller passes .xls, the extension is automatically changed to .xlsx because
    /// some Windows security policies block legacy .xls writes completely.
    /// </summary>
    public static string SaveExcelWorkbook(string path, IEnumerable<PanoColumn> columns, IEnumerable<object> rows, string worksheetName = "PanoControl")
    {
        path = NormalizeExcelPath(path);
        var cols = columns.Where(c => !c.PrivateColumn && c.Visible).ToList();
        var visibleRows = rows.ToList();
        var actualPath = GetWritablePath(path);

        using (var archive = ZipFile.Open(actualPath, ZipArchiveMode.Create))
        {
            WriteZipEntry(archive, "[Content_Types].xml", BuildContentTypesXml());
            WriteZipEntry(archive, "_rels/.rels", BuildRootRelsXml());
            WriteZipEntry(archive, "xl/workbook.xml", BuildWorkbookXml(SafeWorksheetName(worksheetName)));
            WriteZipEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelsXml());
            WriteZipEntry(archive, "xl/styles.xml", BuildStylesXml());
            WriteZipEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(cols, visibleRows));
        }

        return actualPath;
    }

    /// <summary>
    /// Backward-compatible method name. v24.60 writes .xlsx by default instead of legacy .xls.
    /// </summary>
    public static string SaveExcelXml(string path, IEnumerable<PanoColumn> columns, IEnumerable<object> rows, string worksheetName = "PanoControl")
        => SaveExcelWorkbook(path, columns, rows, worksheetName);

    private static string NormalizeExcelPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Pano.xlsx");

        var extension = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(extension) || extension.Equals(".xls", StringComparison.OrdinalIgnoreCase))
            path = Path.ChangeExtension(path, ".xlsx");

        return path;
    }

    private static void WriteZipEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string BuildContentTypesXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
        "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
        "</Types>";

    private static string BuildRootRelsXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>";

    private static string BuildWorkbookRelsXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
        "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
        "</Relationships>";

    private static string BuildWorkbookXml(string sheetName) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
        "<sheets><sheet name=\"" + XmlEscape(sheetName) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
        "</workbook>";

    private static string BuildStylesXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
        "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>" +
        "<borders count=\"1\"><border/></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs>" +
        "</styleSheet>";

    private static string BuildWorksheetXml(IReadOnlyList<PanoColumn> cols, IReadOnlyList<object> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        sb.Append("<row r=\"1\">");
        for (int c = 0; c < cols.Count; c++)
            AppendXlsxCell(sb, 1, c + 1, cols[c].Header, true);
        sb.Append("</row>");

        for (int r = 0; r < rows.Count; r++)
        {
            int rowIndex = r + 2;
            sb.Append("<row r=\"").Append(rowIndex.ToString(CultureInfo.InvariantCulture)).Append("\">");
            for (int c = 0; c < cols.Count; c++)
                AppendXlsxCell(sb, rowIndex, c + 1, cols[c].GetValue(rows[r]), false);
            sb.Append("</row>");
        }

        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static void AppendXlsxCell(StringBuilder sb, int row, int col, object? value, bool header)
    {
        string cellRef = GetExcelColumnName(col) + row.ToString(CultureInfo.InvariantCulture);
        string style = header ? " s=\"1\"" : string.Empty;
        if (value == null || value == DBNull.Value)
        {
            sb.Append("<c r=\"").Append(cellRef).Append("\"").Append(style).Append("/>");
            return;
        }

        var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
        if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) ||
            type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            var number = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
            sb.Append("<c r=\"").Append(cellRef).Append("\"").Append(style).Append("><v>").Append(number).Append("</v></c>");
            return;
        }

        sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\"").Append(style).Append("><is><t>")
          .Append(XmlEscape(Convert.ToString(value) ?? string.Empty)).Append("</t></is></c>");
    }

    private static string GetExcelColumnName(int index)
    {
        var name = string.Empty;
        while (index > 0)
        {
            index--;
            name = (char)('A' + index % 26) + name;
            index /= 26;
        }
        return name;
    }

    private static string XmlEscape(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;

    private static string GetWritablePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Pano.xlsx");

        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
            directory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        var fileName = Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "PanoControl";

        var extension = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".xlsx";

        directory = NormalizeWritableDirectory(directory);

        foreach (var candidate in BuildCandidatePaths(directory, fileName, extension, path))
        {
            try
            {
                var candidateDirectory = Path.GetDirectoryName(candidate);
                if (!string.IsNullOrWhiteSpace(candidateDirectory))
                    Directory.CreateDirectory(candidateDirectory);

                if (CanCreateOrOverwrite(candidate))
                    return candidate;
            }
            catch
            {
                // Try next folder/name. Export should never crash just because one location is protected.
            }
        }

        throw new UnauthorizedAccessException("PanoControl export için yazılabilir bir klasör bulunamadı. Lütfen farklı bir klasör seçin veya Windows Controlled Folder Access/antivirüs izinlerini kontrol edin.");
    }

    private static string NormalizeWritableDirectory(string directory)
    {
        try
        {
            if (File.Exists(directory))
                return Path.GetDirectoryName(directory) ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.Equals(Path.GetExtension(directory), ".xls", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetExtension(directory), ".xlsx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetExtension(directory), ".csv", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetExtension(directory), ".pdf", StringComparison.OrdinalIgnoreCase))
                return Path.GetDirectoryName(directory) ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }
        catch { }
        return directory;
    }

    private static IEnumerable<string> BuildCandidatePaths(string directory, string fileName, string extension, string originalPath)
    {
        yield return originalPath;

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        for (int i = 1; i <= 25; i++)
            yield return Path.Combine(directory, $"{fileName}_{stamp}_{i:00}{extension}");

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documents))
        {
            for (int i = 1; i <= 25; i++)
                yield return Path.Combine(documents, $"{fileName}_{stamp}_{i:00}{extension}");
        }

        var temp = Path.GetTempPath();
        if (!string.IsNullOrWhiteSpace(temp))
        {
            for (int i = 1; i <= 25; i++)
                yield return Path.Combine(temp, $"{fileName}_{stamp}_{i:00}{extension}");
        }
    }

    private static bool CanCreateOrOverwrite(string path)
    {
        if (Directory.Exists(path)) return false;
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan))
        {
            if (!stream.CanWrite)
                return false;
        }

        // The real writer/zip creator should own the final file. Remove the zero-byte probe file.
        try { File.Delete(path); } catch { }
        return true;
    }

    private static void WriteValueCell(XmlWriter writer, object? value)
    {
        if (value == null || value == DBNull.Value)
        {
            WriteCell(writer, string.Empty, "String");
            return;
        }

        var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
        if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) ||
            type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            WriteCell(writer, text, "Number");
            return;
        }

        if (type == typeof(DateTime))
        {
            var dt = (DateTime)value;
            WriteCell(writer, dt.ToString("s", CultureInfo.InvariantCulture), "DateTime");
            return;
        }

        if (type == typeof(bool))
        {
            WriteCell(writer, (bool)value ? "1" : "0", "Boolean");
            return;
        }

        WriteCell(writer, Convert.ToString(value) ?? string.Empty, "String");
    }

    private static void WriteCell(XmlWriter writer, string text, string type)
    {
        writer.WriteStartElement("Cell");
        writer.WriteStartElement("Data");
        writer.WriteAttributeString("ss", "Type", null, type);
        writer.WriteString(text);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static string SafeWorksheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "PanoControl";
        foreach (char c in new[] { ':', '\\', '/', '?', '*', '[', ']' })
            name = name.Replace(c, '-');
        return name.Length <= 31 ? name : name[..31];
    }

    private static string Escape(string text, char separator)
    {
        bool quote = text.Contains(separator) || text.Contains('"') || text.Contains('\n') || text.Contains('\r');
        text = text.Replace("\"", "\"\"");
        return quote ? $"\"{text}\"" : text;
    }
    private static string Html(string s) => System.Net.WebUtility.HtmlEncode(s);
}
