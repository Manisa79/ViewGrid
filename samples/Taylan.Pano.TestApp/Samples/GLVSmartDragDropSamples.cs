using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Taylan.Pano;
using Taylan.Pano.Columns;
using Taylan.Pano.Core;

namespace Taylan.Pano.TestApp.Samples;

public static class GLVSmartDragDropSamples
{
    public sealed class TaskRow
    {
        public int Order { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
    }

    public static FastPanoControl CreateSmartDragDropGrid()
    {
        var grid = new FastPanoControl
        {
            Dock = DockStyle.Fill,
            MultiSelect = true,
            FullRowSelect = true,
            AllowRowDragDrop = true,
            AllowExternalDrop = true,
            ShowDropIndicator = true,
            AutoScrollOnDrag = true,
            AutoReorderRowsOnDrop = true,
            SaveLayoutAfterDragDrop = true,
            UserLayoutKey = "Taylan.Pano.TestApp.SmartDragDrop"
        };

        grid.Columns.Add(new GLVColumn("Sıra", nameof(TaskRow.Order), 70) { TextAlign = System.Drawing.ContentAlignment.MiddleRight });
        grid.Columns.Add(new GLVColumn("Kod", nameof(TaskRow.Code), 120));
        grid.Columns.Add(new GLVColumn("Açıklama", nameof(TaskRow.Description), 260) { FillFreeSpace = true });
        grid.Columns.Add(new GLVColumn("Sorumlu", nameof(TaskRow.Owner), 120));

        grid.RowDropValidating += (_, e) =>
        {
            // Örnek: DB/iş kuralı varsa burada drop engellenebilir.
            if (e.TargetViewIndex < 0) e.Position = PanoDropPosition.After;
        };

        grid.RowDropped += (_, e) =>
        {
            // Örnek: Yeni sıra DB'ye burada yazılabilir.
            Console.WriteLine($"{e.Rows.Count} satır taşındı. Hedef: {e.TargetViewIndex}, Pozisyon: {e.Position}");
        };

        grid.ExternalDropReceived += (_, e) =>
        {
            if (e.Kind == PanoExternalDropKind.Files && e.Payload is string[] files)
                MessageBox.Show("Bırakılan dosya sayısı: " + files.Length);
            else if (e.Kind == PanoExternalDropKind.Text)
                MessageBox.Show("Bırakılan metin: " + Convert.ToString(e.Payload));
        };

        grid.SetObjects(new List<TaskRow>
        {
            new() { Order = 1, Code = "AOI-001", Description = "İlk kontrol", Owner = "Operatör" },
            new() { Order = 2, Code = "AOI-002", Description = "Tekrar kontrol", Owner = "Kalite" },
            new() { Order = 3, Code = "AOI-003", Description = "False call inceleme", Owner = "AI" },
            new() { Order = 4, Code = "AOI-004", Description = "Raporlama", Owner = "Mühendis" }
        });

        return grid;
    }
}

