using Taylan.Pano.Columns;
using Taylan.Pano.Core;

namespace Taylan.Pano.FeatureSamples;

public static class CellOverflowScrollSamples
{
    public sealed class TicketNoteRow
    {
        public int Id { get; set; }
        public string Machine { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TechnicianNote { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }

    public static PanoControl CreateCellOverflowScrollSample()
    {
        var grid = new PanoControl
        {
            Dock = DockStyle.Fill,
            EnableCellOverflowScroll = true,
            ShowCellOverflowScrollBars = true,
            EnableCellOverflowDetailsPopup = true,
            AllowMultilineCells = true,
            RowHeight = 72
        };

        grid.Columns.Add(new PanoColumn("Id", nameof(TicketNoteRow.Id), 70));
        grid.Columns.Add(new PanoColumn("Makine", nameof(TicketNoteRow.Machine), 150));
        grid.Columns.Add(new PanoColumn("Durum", nameof(TicketNoteRow.Status), 120));
        grid.Columns.Add(new PanoColumn("Mesaj / Açıklama", nameof(TicketNoteRow.Message), 320)
        {
            WordWrap = true,
            AllowCellScroll = true,
            CellScrollMaxVisibleLines = 4,
            ShowCellScrollBar = true,
            CellOverflowFade = true,
            CellOverflowDetailsOnDoubleClick = true
        });
        grid.Columns.Add(new PanoColumn("Teknisyen Notu", nameof(TicketNoteRow.TechnicianNote), 300)
        {
            WordWrap = true,
            AllowCellScroll = true,
            CellScrollMaxVisibleLines = 4,
            ShowCellScrollBar = true,
            CellOverflowFade = true,
            CellOverflowDetailsOnDoubleClick = true
        });
        grid.Columns.Add(new PanoColumn("Zaman", nameof(TicketNoteRow.Time), 150));

        var rows = Enumerable.Range(1, 50).Select(i => new TicketNoteRow
        {
            Id = i,
            Machine = i % 2 == 0 ? "LINE01YASREW" : "LINE02YASREW",
            Status = i % 3 == 0 ? "ActionRequired" : "Watching",
            Message = "Bu hücredeki uzun içerik kolon genişliğine göre otomatik sarılır. " +
                      "Satır yüksekliği büyütülmeden hücre içinde mouse wheel ile yukarı/aşağı kaydırılabilir. " +
                      "Kullanıcı isterse çift tıklayarak daha büyük reader popup içinde okuyabilir.",
            TechnicianNote = "Program kontrol ediliyor, SAP/PCB bilgisi karşılaştırılıyor. " +
                             "Gerekirse operatör tekrar denemeden önce teknisyen sonucu bu not alanından takip eder. " +
                             "Bu not özellikle dar kolonlarda hücre içi scroll için örnektir.",
            Time = DateTime.Now.AddMinutes(-i * 7)
        }).ToList();

        grid.SetObjects(rows);
        return grid;
    }
}

