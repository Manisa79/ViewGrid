using Taylan.Pano.Columns;
using Taylan.Pano.Core;

namespace Taylan.Pano.FeatureSamples;

public static class UltimatePlatformSamples
{
    public sealed class UltimateTicket
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Machine { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public static PanoControl CreateUltimateSample()
    {
        PanoControl grid = new PanoControl
        {
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            AllowMultilineCells = true,
            AutoRowHeightForMultilineCells = false,
            DetailsRowHeight = 28
        };

        grid.Columns.Add(new PanoColumn { Header = "Id", AspectName = "Id", Width = 70 });
        grid.Columns.Add(new PanoColumn { Header = "Durum", AspectName = "Status", Width = 120 });
        grid.Columns.Add(new PanoColumn { Header = "Makine", AspectName = "Machine", Width = 140 });
        grid.Columns.Add(new PanoColumn { Header = "Öncelik", AspectName = "Priority", Width = 90 });
        grid.Columns.Add(new PanoColumn { Header = "Başlık", AspectName = "Title", Width = 260 });
        grid.Columns.Add(new PanoColumn { Header = "Yaş", Formula = "IF(Priority>=3,'Acil','Normal')", UseFormula = true, Width = 90 });

        grid.SetObjects(new[]
        {
            new UltimateTicket { Id = 1, Status = "Open", Machine = "LINE01", Priority = 3, Title = "AOI durdu", CreatedAt = DateTime.Now.AddMinutes(-12) },
            new UltimateTicket { Id = 2, Status = "Working", Machine = "LINE02", Priority = 2, Title = "Program isteği", CreatedAt = DateTime.Now.AddMinutes(-44) },
            new UltimateTicket { Id = 3, Status = "Closed", Machine = "LINE03", Priority = 1, Title = "False call", CreatedAt = DateTime.Now.AddHours(-2) }
        });

        grid.EnableUltimateDefaults();
        grid.Query = "Status:Open OR Priority >= 3";
        grid.CaptureChangeSnapshot();
        grid.Actions.Add(new PanoActionStep
        {
            Name = "FlashHighPriority",
            Trigger = "manual",
            Condition = ctx => ctx.RowObject != null && grid.EvaluateExpressionCondition(ctx.RowObject, "Priority>=3"),
            Execute = ctx => { if (ctx.RowObject != null) grid.FlashObject(ctx.RowObject, Color.Gold, "High priority"); }
        });

        return grid;
    }
}

