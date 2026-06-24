using System.Text.Json.Serialization;

namespace Taylan.Pano.Filtering;

public sealed class PanoFilterPreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PanoFilterLogic Logic { get; set; } = PanoFilterLogic.And;
    public string GlobalText { get; set; } = string.Empty;
    public List<PanoColumnFilterPresetItem> Filters { get; set; } = new();
}

public sealed class PanoColumnFilterPresetItem
{
    public string AspectName { get; set; } = string.Empty;
    public PanoFilterMode Mode { get; set; } = PanoFilterMode.Contains;
    public string? Text { get; set; }
    public string? Text2 { get; set; }
    public bool Enabled { get; set; } = true;
    public List<string>? SelectedValues { get; set; }

    [JsonIgnore]
    public string DisplayText
    {
        get
        {
            string op = Mode.ToString();
            if (Mode == PanoFilterMode.ValueList)
                return $"{AspectName} {op} ({SelectedValues?.Count ?? 0} değer)";
            if (Mode == PanoFilterMode.Between)
                return $"{AspectName} {op} {Text} - {Text2}";
            return $"{AspectName} {op} {Text}";
        }
    }
}
