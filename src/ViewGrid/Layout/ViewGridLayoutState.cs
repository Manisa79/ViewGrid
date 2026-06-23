using System.Text.Json;
using ViewGrid.Columns;
using ViewGrid.Filtering;

namespace ViewGrid.Layout;

public sealed class ViewGridLayoutState
{
    public List<ViewGridColumnLayout> Columns { get; set; } = new();
    public string? SortAspectName { get; set; }
    public bool SortDescending { get; set; }
    public string GlobalFilter { get; set; } = string.Empty;
    public string? GroupByAspectName { get; set; }
    public bool EnableGrouping { get; set; }
    public int FrozenColumnCount { get; set; }

    public List<ViewGridColumnFilter> ColumnFilters { get; set; } = new();
    public string FilterMenuMode { get; set; } = string.Empty;
    public bool ShowColumnFilterButtons { get; set; } = true;
    public bool ShowColumnSortGlyphs { get; set; } = true;
    public string FilterIconStyle { get; set; } = string.Empty;
    public string SortGlyphStyle { get; set; } = string.Empty;
    public int RowHeight { get; set; }
    public string ViewMode { get; set; } = string.Empty;
    public string ColumnChooserMenuMode { get; set; } = string.Empty;
    public bool ShowColumnChooserInHeaderMenu { get; set; } = true;
    public bool ShowColumnChooserWindowInHeaderMenu { get; set; } = true;
    public string MenuIconMode { get; set; } = string.Empty;
    public string MenuIconSize { get; set; } = string.Empty;
    public string CustomMenuIconFolder { get; set; } = string.Empty;

    public static ViewGridLayoutState Capture(IEnumerable<ViewGridColumn> columns, string globalFilter, string? sortAspect, bool sortDescending)
    {
        var state = new ViewGridLayoutState { GlobalFilter = globalFilter ?? string.Empty, SortAspectName = sortAspect, SortDescending = sortDescending };
        int order = 0;
        foreach (var c in columns)
            state.Columns.Add(new ViewGridColumnLayout { AspectName = c.AspectName, Text = c.Header, Width = c.Width, Visible = c.Visible, DisplayIndex = order++ });
        return state;
    }

    public void Apply(IList<ViewGridColumn> columns)
    {
        foreach (var saved in Columns)
        {
            var col = columns.FirstOrDefault(c => string.Equals(c.AspectName, saved.AspectName, StringComparison.OrdinalIgnoreCase) || string.Equals(c.Header, saved.Text, StringComparison.OrdinalIgnoreCase));
            if (col == null) continue;
            col.Width = Math.Max(30, saved.Width);
            col.ApplyRuntimeVisible(saved.Visible);
        }
        var ordered = columns.OrderBy(c => Columns.FirstOrDefault(s => s.AspectName == c.AspectName || s.Text == c.Header)?.DisplayIndex ?? int.MaxValue).ToList();
        columns.Clear();
        foreach (var c in ordered) columns.Add(c);
    }

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    public static ViewGridLayoutState FromJson(string json) => JsonSerializer.Deserialize<ViewGridLayoutState>(json) ?? new ViewGridLayoutState();
}

public sealed class ViewGridLayoutProfile
{
    public int ProfileVersion { get; set; } = 29;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? UserName { get; set; }
    public string? MachineName { get; set; }
    public string? RoleName { get; set; }
    public string ViewGridVersion { get; set; } = ViewGrid.Core.ViewGridVersionInfo.Version;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ViewGridLayoutState State { get; set; } = new();

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    public static ViewGridLayoutProfile FromJson(string json) => JsonSerializer.Deserialize<ViewGridLayoutProfile>(json) ?? new ViewGridLayoutProfile();
}

public static class ViewGridProfileMigrator
{
    public static ViewGridLayoutProfile FromLegacyColumnProfileJson(string json, string name)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        ViewGridLayoutProfile profile = new ViewGridLayoutProfile { Name = name, DisplayName = name, ProfileVersion = 29 };
        if (doc.RootElement.TryGetProperty("Name", out JsonElement n)) profile.Name = n.GetString() ?? name;
        if (doc.RootElement.TryGetProperty("DisplayName", out JsonElement dn)) profile.DisplayName = dn.GetString() ?? profile.Name;

        if (doc.RootElement.TryGetProperty("Columns", out JsonElement columns) && columns.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in columns.EnumerateArray())
            {
                ViewGridColumnLayout layout = new ViewGridColumnLayout();
                if (item.TryGetProperty("AspectName", out JsonElement aspect)) layout.AspectName = aspect.GetString() ?? string.Empty;
                if (item.TryGetProperty("Text", out JsonElement text)) layout.Text = text.GetString() ?? layout.AspectName;
                if (item.TryGetProperty("Width", out JsonElement widthElement) && widthElement.TryGetInt32(out int width)) layout.Width = width;
                if (item.TryGetProperty("Visible", out JsonElement visibleElement) && (visibleElement.ValueKind == JsonValueKind.True || visibleElement.ValueKind == JsonValueKind.False)) layout.Visible = visibleElement.GetBoolean();
                else layout.Visible = true;
                if (item.TryGetProperty("DisplayIndex", out JsonElement displayIndexElement) && displayIndexElement.TryGetInt32(out int displayIndex)) layout.DisplayIndex = displayIndex;
                else layout.DisplayIndex = profile.State.Columns.Count;
                if (string.IsNullOrWhiteSpace(layout.AspectName)) layout.AspectName = layout.Text;
                if (string.IsNullOrWhiteSpace(layout.Text)) layout.Text = layout.AspectName;
                profile.State.Columns.Add(layout);
            }
            return profile;
        }

        Dictionary<string, int> widths = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, bool> visibility = new(StringComparer.OrdinalIgnoreCase);
        List<string> order = new();

        if (doc.RootElement.TryGetProperty("Widths", out JsonElement w) && w.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in w.EnumerateObject())
            {
                if (prop.Value.TryGetInt32(out int width)) widths[prop.Name] = width;
            }
        }

        if (doc.RootElement.TryGetProperty("Visibility", out JsonElement v) && v.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in v.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False) visibility[prop.Name] = prop.Value.GetBoolean();
            }
        }

        if (doc.RootElement.TryGetProperty("Order", out JsonElement o) && o.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in o.EnumerateArray())
            {
                string? key = item.GetString();
                if (!string.IsNullOrWhiteSpace(key)) order.Add(key);
            }
        }

        int index = 0;
        foreach (string key in order.Concat(widths.Keys).Concat(visibility.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            profile.State.Columns.Add(new ViewGridColumnLayout
            {
                AspectName = key,
                Text = key,
                Width = widths.TryGetValue(key, out int width) ? width : 100,
                Visible = !visibility.TryGetValue(key, out bool visible) || visible,
                DisplayIndex = index++
            });
        }
        return profile;
    }
}

public sealed class ViewGridColumnLayout
{
    public string Text { get; set; } = string.Empty;
    public string AspectName { get; set; } = string.Empty;
    public int Width { get; set; }
    public bool Visible { get; set; }
    public int DisplayIndex { get; set; }
}
