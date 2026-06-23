namespace ViewGrid.Core;

/// <summary>
/// ViewGridControl'in ana çalışma modunu belirtir.
/// ViewMode görsel sunumu (Details/Tile/List) yönetirken, Mode veri/çalışma yaklaşımını netleştirir.
/// </summary>
public enum ViewGridDataMode
{
    /// <summary>POCO/model object listesi ile klasik ViewGrid benzeri kullanım.</summary>
    Object,

    /// <summary>DataTable/DataView/BindingSource tabanlı kullanım.</summary>
    DataTable,

    /// <summary>IRowProvider veya IQueryRowProvider ile sanal / çok büyük veri kullanımı.</summary>
    Virtual,

    /// <summary>TreeViewGridControl veya hiyerarşik veri senaryosu.</summary>
    Tree,

    /// <summary>Kart/tile görünüm odaklı kullanım.</summary>
    Tile
}
