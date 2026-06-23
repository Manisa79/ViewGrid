using System.Text.Json.Serialization;

namespace ViewGrid.Filtering;

public sealed class ViewGridFilterPreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ViewGridFilterLogic Logic { get; set; } = ViewGridFilterLogic.And;
    public string GlobalText { get; set; } = string.Empty;
    public List<ViewGridColumnFilterPresetItem> Filters { get; set; } = new();
}

public sealed class ViewGridColumnFilterPresetItem
{
    public string AspectName { get; set; } = string.Empty;
    public ViewGridFilterMode Mode { get; set; } = ViewGridFilterMode.Contains;
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
            if (Mode == ViewGridFilterMode.ValueList)
                return $"{AspectName} {op} ({SelectedValues?.Count ?? 0} değer)";
            if (Mode == ViewGridFilterMode.Between)
                return $"{AspectName} {op} {Text} - {Text2}";
            return $"{AspectName} {op} {Text}";
        }
    }
}
