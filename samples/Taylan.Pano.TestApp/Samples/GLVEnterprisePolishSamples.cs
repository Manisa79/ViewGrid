using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Taylan.Pano;
using Taylan.Pano.Columns;
using Taylan.Pano.Core;

namespace Taylan.Pano.TestApp.Samples;

public static class GLVEnterprisePolishSamples
{
    public sealed class EnterpriseRow
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
    }

    public static FastPanoControl CreateEnterprisePolishGrid()
    {
        var grid = new FastPanoControl
        {
            Dock = DockStyle.Fill,
            EnableCommandPalette = true,
            EnableKeyboardFilterShortcut = true,
            ShowSortBusyIndicator = true,
            SortBusyTitle = "Liste sıralanıyor...",
            SortBusyDetail = "Büyük veri arka planda hazırlanıyor. İşlem bitince liste otomatik yenilenecek.",
            SortBusyOverlayWidth = 420,
            SortBusyOverlayHeight = 110,
            AsyncSortForLargeLists = true,
            AsyncSortThreshold = 2_000,
            CacheSortKeysForLargeLists = true,
            ShowCellValidationErrors = true,
            AutoSaveUserLayout = true,
            AutoLoadUserLayout = true,
            UserLayoutKey = "Samples.EnterprisePolish.MainGrid"
        };

        var colId = new GLVColumn("Id", "Id", 70) { AllowFilter = true, AllowSort = true };
        var colBarcode = new GLVColumn("Barcode", "Barcode", 160) { AllowFilter = true, AllowSort = true };
        var colStatus = new GLVColumn("Status", "Status", 130) { AllowFilter = true, AllowSort = true, AllowGroup = true };
        var colQty = new GLVColumn("Quantity", "Quantity", 110) { Editable = true, AllowFilter = true, AllowSort = true };
        var colDate = new GLVColumn("Date", "Date", 160) { AllowFilter = true, AllowSort = true };
        grid.Columns.AddRange(new[] { colId, colBarcode, colStatus, colQty, colDate });

        grid.CellValidationNeeded += (_, e) =>
        {
            if (e.Column.AspectName == "Quantity" && e.RowObject is EnterpriseRow row && row.Quantity < 0)
                e.ErrorText = "Quantity negatif olamaz.";
        };

        grid.SetObjects(CreateRows(25000));
        return grid;
    }

    public static void ShowShortcutHelp(FastPanoControl grid)
    {
        MessageBox.Show(
            "Ctrl+K: Komut paleti\n" +
            "Ctrl+F: Filtre komutları\n" +
            "Ctrl+C: Seçimi kopyala\n" +
            "Ctrl+Shift+J: JSON kopyala\n" +
            "Ctrl+Z / Ctrl+Y: Hücre edit undo/redo",
            "Pano Enterprise Polish",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static List<EnterpriseRow> CreateRows(int count)
    {
        var list = new List<EnterpriseRow>(count);
        string[] statuses = { "Bekliyor", "Tamam", "Hata", "Kontrol" };
        for (int i = 1; i <= count; i++)
        {
            list.Add(new EnterpriseRow
            {
                Id = i,
                Barcode = "BC" + i.ToString("000000"),
                Status = statuses[i % statuses.Length],
                Quantity = i % 137 == 0 ? -1 : i % 500,
                Date = DateTime.Today.AddDays(-(i % 365))
            });
        }
        return list;
    }
}

