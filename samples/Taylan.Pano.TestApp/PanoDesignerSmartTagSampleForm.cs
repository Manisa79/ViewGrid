using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed partial class PanoDesignerSmartTagSampleForm : Form
{
    public PanoDesignerSmartTagSampleForm()
    {
        InitializeComponent();

        pano.Columns.Add(new Pano.Columns.PanoColumn("Person", nameof(SmartTagRow.Person), 160));
        pano.Columns.Add(new Pano.Columns.PanoColumn("Occupation", nameof(SmartTagRow.Occupation), 160));
        pano.Columns.Add(new Pano.Columns.PanoColumn("Status", nameof(SmartTagRow.Status), 120));
        pano.Columns.Add(new Pano.Columns.PanoColumn("Notes", nameof(SmartTagRow.Notes), 260) { FillsFreeSpace = true });

        pano.SetObjects(new[]
        {
            new SmartTagRow("Person 1", "Technician", "Active", "Smart Tag içinde kolon düzenleme ve tema seçeneklerini kontrol et."),
            new SmartTagRow("Person 2", "Operator", "Waiting", "FastPanoControl/DataListView/TreePanoControl de aynı designer altyapısını kullanır."),
            new SmartTagRow("Person 3", "Quality", "Done", "Menüde Pano Görevleri görünür ve tüm hızlı işlemler Pano adıyla sunulur.")
        });

        ApplyTheme(WindowsThemeService.CurrentTheme());
    }

    private void ApplyTheme(PanoTheme theme)
    {
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        lblInfo.BackColor = theme.HeaderBackColor;
        lblInfo.ForeColor = theme.ForeColor;
        pano.ApplyTheme(theme);
    }

    private sealed record SmartTagRow(string Person, string Occupation, string Status, string Notes);
}
