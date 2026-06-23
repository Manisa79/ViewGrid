using System.Windows.Forms;
using System.Linq;
using ViewGrid;
using ViewGrid.Columns;
using ViewGrid.Core;

namespace ViewGrid.TestApp.Samples;

public static class GLVKeyboardHeaderMenuSamples
{
    public static FastViewGridControl CreateKeyboardHeaderMenuGrid()
    {
        var grid = new FastViewGridControl
        {
            Dock = DockStyle.Fill,
            KeyboardColumnContextMenuKeyOpensMenu = true,
            KeyboardContextMenuKeyOpensMenu = true,
            ShowColumnFilterButtons = true,
            HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full
        };

        grid.Columns.Add(new GLVColumn("Barkod", nameof(DemoKeyboardRow.Barcode), 140));
        grid.Columns.Add(new GLVColumn("Durum", nameof(DemoKeyboardRow.State), 110));
        grid.Columns.Add(new GLVColumn("Açıklama", nameof(DemoKeyboardRow.Description), 260));

        grid.SetObjects(Enumerable.Range(1, 100).Select(i => new DemoKeyboardRow
        {
            Barcode = "BC" + i.ToString("000000"),
            State = i % 3 == 0 ? "Fail" : i % 2 == 0 ? "Review" : "Pass",
            Description = "Alt+Down veya Ctrl+Shift+F10 ile kolon menüsünü klavyeden aç."
        }));

        return grid;
    }

    private sealed class DemoKeyboardRow
    {
        public string Barcode { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
