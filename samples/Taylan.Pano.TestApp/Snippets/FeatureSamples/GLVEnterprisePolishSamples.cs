using Taylan.Pano;
using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Filtering;

namespace Taylan.Pano.FeatureSamples;

public static class GLVEnterprisePolishSamples
{
    public static FastPanoControl CreateEnterprisePolishGrid()
    {
        var grid = new FastPanoControl
        {
            Dock = DockStyle.Fill,
            AutoLoadUserLayout = true,
            AutoSaveUserLayout = true,
            UserLayoutKey = "Samples.EnterprisePolish.MainGrid",
            ShowSortBusyIndicator = true,
            SortBusyTitle = "Sıralanıyor...",
            SortBusyDetail = "Büyük liste arka planda hazırlanıyor. Bu pencere artık daha belirgin görünür.",
            SortBusyOverlayWidth = 480,
            SortBusyOverlayHeight = 140,
            SortBusyDimOpacity = 125,
            EnableCommandPalette = true,
            EnableUndoRedo = true,
            ShowCellValidationErrors = true,
            ColumnChooserMenuMode = PanoColumnChooserMenuMode.Both,
            ShowColumnChooserInHeaderMenu = true,
            ShowColumnChooserWindowInHeaderMenu = true
        };

        grid.Columns.Add(new GLVColumn { Header = "Kod", AspectName = "Code", Width = 120, Filterable = true, Sortable = true });
        grid.Columns.Add(new GLVColumn { Header = "Açıklama", AspectName = "Description", Width = 240, Filterable = true, Sortable = true });
        grid.Columns.Add(new GLVColumn { Header = "Durum", AspectName = "State", Width = 120, Filterable = true, Sortable = true });
        grid.Columns.Add(new GLVColumn { Header = "Gizlenebilir", AspectName = "Optional", Width = 130, AllowColumnChooser = true });

        grid.CellValidationNeeded += (_, e) =>
        {
            if (e.Column.AspectName == "Code" && string.IsNullOrWhiteSpace(Convert.ToString(e.Column.GetValue(e.RowObject))))
                e.ErrorText = "Kod boş bırakılamaz.";
        };

        grid.SetObjects(Enumerable.Range(1, 50000).Select(i => new
        {
            Code = "P" + i.ToString("000000"),
            Description = "Demo kayıt " + i,
            State = i % 3 == 0 ? "OK" : "Bekliyor",
            Optional = "Ek " + i
        }).ToList());

        return grid;
    }
}

