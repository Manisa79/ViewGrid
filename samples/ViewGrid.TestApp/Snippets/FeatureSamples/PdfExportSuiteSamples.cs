using System.Drawing;
using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Exporting;

namespace ViewGrid.FeatureSamples;

public static class PdfExportSuiteSamples
{
    public sealed class TicketSample
    {
        public string Status { get; set; } = string.Empty;
        public string MachineType { get; set; } = string.Empty;
        public string Machine { get; set; } = string.Empty;
        public string OtoCode { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public static void ConfigureTicketPdfSample(ViewGridControl grid)
    {
        grid.Columns.Clear();
        grid.Columns.Add(new ViewGridColumn("Durum", nameof(TicketSample.Status), 110) { CardRole = "Title", CardOrder = 0 });
        grid.Columns.Add(new ViewGridColumn("Makine Tipi", nameof(TicketSample.MachineType), 120) { CardOrder = 1 });
        grid.Columns.Add(new ViewGridColumn("Makine", nameof(TicketSample.Machine), 140) { CardOrder = 2 });
        grid.Columns.Add(new ViewGridColumn("Oto Kod", nameof(TicketSample.OtoCode), 100) { CardOrder = 3 });
        grid.Columns.Add(new ViewGridColumn("Not", nameof(TicketSample.Note), 180) { CardOrder = 4, CardShowCaption = true });

        grid.SetObjects(new[]
        {
            new TicketSample { Status = "Tamamlandı", MachineType = "QX150i", Machine = "LINE16YASREW", OtoCode = "24016407", Note = "SAP bulundu" },
            new TicketSample { Status = "Bakılıyor", MachineType = "QX250i", Machine = "LINE38REW", OtoCode = "24033147", Note = "FalseCallRequired" }
        });

        grid.CardVisualInfoGetter = row =>
        {
            var ticket = (TicketSample)row;
            Color color = ticket.Status.Contains("Tamam") ? Color.MediumSeaGreen : Color.DodgerBlue;
            return new ViewGridCardVisualInfo
            {
                AccentColor = color,
                DotColor = color,
                Badges =
                {
                    new ViewGridCardBadge { Text = ticket.Note.Contains("SAP") ? "SAP" : "FC", BackColor = ticket.Note.Contains("SAP") ? Color.SeaGreen : Color.Goldenrod, ForeColor = Color.White }
                }
            };
        };
    }

    public static string ExportTable(ViewGridControl grid, string path)
    {
        return grid.ExportVisiblePdf(path, new ViewGridPdfExportOptions
        {
            Title = "ViewGrid Table PDF",
            Mode = ViewGridPdfExportMode.Table,
            Orientation = ViewGridPdfPageOrientation.Landscape,
            FitToPageWidth = true,
            ShowGridLines = true,
            ZebraRows = true
        });
    }

    public static string ExportCards(ViewGridControl grid, string path)
    {
        return grid.ExportVisiblePdf(path, new ViewGridPdfExportOptions
        {
            Title = "ViewGrid Card PDF",
            Mode = ViewGridPdfExportMode.Card,
            Orientation = ViewGridPdfPageOrientation.Portrait,
            CardColumns = 2,
            CardMinHeight = 105
        });
    }
}
