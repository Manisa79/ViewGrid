using System.ComponentModel;
using System.Text.Json;
using ViewGrid.Columns;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    private string _v37LastLayoutName = string.Empty;

    [Category("ViewGrid - V37 Enterprise Layout")]
    [DefaultValue(false)]
    [Description("Kolon, filtre, sıralama ve görünüm modunu kullanıcı/proje bazlı saklamak için Enterprise Layout akışını açar.")]
    public bool EnableEnterpriseLayout { get; set; }

    [Category("ViewGrid - V37 Enterprise Layout")]
    [DefaultValue(ViewGridV37LayoutScope.User)]
    [Description("Layout profilinin kullanıcı, proje, makine veya senaryo bazında ele alınacağını belirtir.")]
    public ViewGridV37LayoutScope LayoutScope { get; set; } = ViewGridV37LayoutScope.User;

    [Category("ViewGrid - V37 Enterprise Layout")]
    [DefaultValue(true)]
    [Description("ViewMode bilgisini layout snapshot içine dahil eder.")]
    public bool LayoutIncludeViewMode { get; set; } = true;

    [Category("ViewGrid - V37 Enterprise Layout")]
    [DefaultValue(true)]
    [Description("Kolon genişliği, sıra ve görünürlük bilgisini layout snapshot içine dahil eder.")]
    public bool LayoutIncludeColumns { get; set; } = true;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string LastEnterpriseLayoutName => _v37LastLayoutName;

    public void ApplyV37EnterpriseLayoutPack()
    {
        EnableEnterpriseLayout = true;
        RememberLastViewMode = true;
        PersistVisualPreferences = true;
        PersistColumnFilters = true;
        EnableLayoutStudio = true;
        RefreshView();
    }

    public ViewGridLayoutPresetInfo CreateEnterpriseLayoutSnapshot(string name)
    {
        var preset = new ViewGridLayoutPresetInfo
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim(),
            ViewMode = LayoutIncludeViewMode ? ViewMode.ToString() : string.Empty,
            SavedAt = DateTime.Now
        };

        if (LayoutIncludeColumns)
        {
            foreach (ViewGridColumn column in Columns)
            {
                preset.Columns.Add(new ViewGridColumnLayoutInfo
                {
                    Name = column.Name,
                    Header = column.Header,
                    AspectName = column.AspectName,
                    Width = column.Width,
                    DisplayIndex = column.DisplayIndex,
                    Visible = column.Visible
                });
            }
        }

        return preset;
    }

    public string SaveEnterpriseLayoutToJson(string name)
    {
        var preset = CreateEnterpriseLayoutSnapshot(name);
        _v37LastLayoutName = preset.Name;
        return JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
    }

    public bool LoadEnterpriseLayoutFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        ViewGridLayoutPresetInfo? preset;
        try
        {
            preset = JsonSerializer.Deserialize<ViewGridLayoutPresetInfo>(json);
        }
        catch
        {
            return false;
        }

        if (preset == null) return false;
        _v37LastLayoutName = preset.Name;

        BeginUpdate();
        try
        {
            if (LayoutIncludeViewMode && Enum.TryParse<ViewGridMode>(preset.ViewMode, out var mode))
                SetViewMode(mode);

            foreach (var savedColumn in preset.Columns)
            {
                var column = Columns.FirstOrDefault(c =>
                    string.Equals(c.Name, savedColumn.Name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.AspectName, savedColumn.AspectName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.Header, savedColumn.Header, StringComparison.OrdinalIgnoreCase));

                if (column == null) continue;
                column.Width = Math.Max(24, savedColumn.Width);
                column.DisplayIndex = savedColumn.DisplayIndex;
                column.Visible = savedColumn.Visible;
            }
        }
        finally
        {
            EndUpdate();
        }

        return true;
    }
}
