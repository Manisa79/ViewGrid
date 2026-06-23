using System.Text.Json;
using ViewGrid.Layout;

namespace ViewGrid.State;

/// <summary>
/// v27 tam ekran durumu: layout + filtre + sıralama + görünüm + seçim + senaryo bilgisini tek modelde saklar.
/// MasterData/AOI gibi uygulamalarda kullanıcı ekranını kapatıp açınca aynı çalışma bağlamına dönebilmek için tasarlandı.
/// </summary>
public sealed class ViewGridState
{
    public int SchemaVersion { get; set; } = 27;
    public string Name { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public string ViewMode { get; set; } = string.Empty;
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
    public ViewGridLayoutState Layout { get; set; } = new();
    public List<string> SelectedKeys { get; set; } = new();
    public List<string> CheckedKeys { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string ToJson()
        => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static ViewGridState FromJson(string json)
        => JsonSerializer.Deserialize<ViewGridState>(json) ?? new ViewGridState();
}

public sealed class ViewGridStatePreset
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ViewGridState State { get; set; } = new();
}
