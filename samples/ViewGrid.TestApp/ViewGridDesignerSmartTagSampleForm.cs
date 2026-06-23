using ViewGrid.Theming;

namespace ViewGrid.TestApp;

public sealed partial class ViewGridDesignerSmartTagSampleForm : Form
{
    public ViewGridDesignerSmartTagSampleForm()
    {
        InitializeComponent();

        viewgrid.Columns.Add(new ViewGrid.Columns.ViewGridColumn("Person", nameof(SmartTagRow.Person), 160));
        viewgrid.Columns.Add(new ViewGrid.Columns.ViewGridColumn("Occupation", nameof(SmartTagRow.Occupation), 160));
        viewgrid.Columns.Add(new ViewGrid.Columns.ViewGridColumn("Status", nameof(SmartTagRow.Status), 120));
        viewgrid.Columns.Add(new ViewGrid.Columns.ViewGridColumn("Notes", nameof(SmartTagRow.Notes), 260) { FillsFreeSpace = true });

        viewgrid.SetObjects(new[]
        {
            new SmartTagRow("Person 1", "Technician", "Active", "Smart Tag içinde kolon düzenleme ve tema seçeneklerini kontrol et."),
            new SmartTagRow("Person 2", "Operator", "Waiting", "FastViewGridControl/DataListView/TreeViewGridControl de aynı designer altyapısını kullanır."),
            new SmartTagRow("Person 3", "Quality", "Done", "Menüde ViewGrid Görevleri görünür ve tüm hızlı işlemler ViewGrid adıyla sunulur.")
        });

        ApplyTheme(WindowsThemeService.CurrentTheme());
    }

    private void ApplyTheme(ViewGridTheme theme)
    {
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        lblInfo.BackColor = theme.HeaderBackColor;
        lblInfo.ForeColor = theme.ForeColor;
        viewgrid.ApplyTheme(theme);
    }

    private sealed record SmartTagRow(string Person, string Occupation, string Status, string Notes);
}
