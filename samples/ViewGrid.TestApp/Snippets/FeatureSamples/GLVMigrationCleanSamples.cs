using ViewGrid;
using ViewGrid.Columns;
using ViewGrid.Core;
using System.Drawing;
using System.Windows.Forms;

namespace ViewGrid.FeatureSamples;

public static class GLVMigrationCleanSamples
{
    public sealed class DemoRow
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool Selected { get; set; }
    }

    public static FastViewGridControl CreateFullGLVSample()
    {
        var grid = new FastViewGridControl
        {
            Dock = DockStyle.Fill,
            CheckBoxes = true,
            CheckedAspectName = nameof(DemoRow.Selected),
            UseFiltering = true,
            ShowGroups = true,
            GroupByAspectName = nameof(DemoRow.Status),
            EmptyListMsg = "Kayıt bulunamadı",
            CellEditActivation = GLVCellEditActivateMode.DoubleClick,
            SortGroupItemsByPrimaryColumn = true
        };

        grid.Columns.Add(new GLVColumn("Kod", nameof(DemoRow.Code), 120)
        {
            TextAlign = ContentAlignment.MiddleLeft,
            IsVisible = true,
            AspectGetter = row => ((DemoRow)row).Code
        });
        grid.Columns.Add(new GLVColumn("Ad", nameof(DemoRow.Name), 220)
        {
            AspectGetter = row => ((DemoRow)row).Name,
            AspectPutter = (row, value) => ((DemoRow)row).Name = Convert.ToString(value) ?? string.Empty,
            Editable = true
        });
        grid.Columns.Add(new GLVColumn("Durum", nameof(DemoRow.Status), 120));
        grid.Columns.Add(new GLVColumn("Miktar", nameof(DemoRow.Quantity), 90) { TextAlign = ContentAlignment.MiddleRight });

        // OLV ItemCheck değil: ViewGrid checkbox değişimleri için CellValueChanged kullanılır.
        grid.CellValueChanged += (_, e) =>
        {
            if (!e.IsCheckBoxColumn) return;
            if (e.RowObject is not DemoRow changedRow) return;

            bool isSelected = e.NewValueAsBoolean ?? changedRow.Selected;

            // Tek seçim isteniyorsa diğer satırları temizle.
            if (isSelected)
            {
                foreach (DemoRow row in grid.GetObjects().OfType<DemoRow>())
                {
                    if (!ReferenceEquals(row, changedRow)) row.Selected = false;
                }

                grid.RefreshObjects(grid.GetObjects());
            }
        };

        grid.SetObjects(Enumerable.Range(1, 1000).Select(i => new DemoRow
        {
            Code = $"PRG-{i:0000}", Name = $"Malzeme {i:0000}",
            Status = i % 3 == 0 ? "Bekliyor" : i % 3 == 1 ? "Aktif" : "Pasif",
            Quantity = i * 2, Selected = i % 10 == 0
        }));

        grid.ModelFilter = row => ((DemoRow)row).Quantity > 0;
        grid.AdditionalFilter = new GLVModelFilter(row => !string.IsNullOrWhiteSpace(((DemoRow)row).Code));
        grid.PrimarySortColumn = grid.Columns.FirstOrDefault(c => c.AspectName == nameof(DemoRow.Code));
        grid.BuildList();
        return grid;
    }
}
