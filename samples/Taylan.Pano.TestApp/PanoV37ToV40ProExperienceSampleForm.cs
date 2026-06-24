using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed class PanoV37ToV40ProExperienceSampleForm : Form
{
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = PanoDataMode.Object,
        EmptyListMessage = "V37-V40 Pro Experience için kayıt yok",
        HeaderContextMenuBehavior = PanoHeaderContextMenuBehavior.Full,
        FilterMenuMode = PanoFilterMenuMode.Both,
        ShowQuickFilterBar = true,
        ShowActiveFilterChips = true,
        EnableFilterPresets = true,
        EnableGrouping = true
    };
    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly TextBox _notes = new() { Dock = DockStyle.Bottom, Height = 120, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None };
    private string _layoutJson = string.Empty;

    public PanoV37ToV40ProExperienceSampleForm()
    {
        Text = "Pano v37-v40 Pro Experience - Layout + Performance + Interaction + Analytics";
        Width = 1360;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;

        ConfigureColumns();
        ConfigureToolbar();
        Controls.Add(_grid);
        Controls.Add(_notes);
        Controls.Add(_tool);

        ApplyDarkTheme();
        LoadRows();
        ApplyPhase(37);
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new PanoColumn("Tip", nameof(ProExperienceRow.Type), 120) { Kind = PanoColumnKind.Badge, AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("Kod", nameof(ProExperienceRow.Code), 130));
        _grid.Columns.Add(new PanoColumn("Başlık", nameof(ProExperienceRow.Title), 260) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 2 });
        _grid.Columns.Add(new PanoColumn("Sahip", nameof(ProExperienceRow.Owner), 130) { AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("Durum", nameof(ProExperienceRow.Status), 120) { Kind = PanoColumnKind.Badge, AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("Değer", nameof(ProExperienceRow.Value), 90) { Kind = PanoColumnKind.ProgressBar });
        _grid.Columns.Add(new PanoColumn("Açıklama", nameof(ProExperienceRow.Detail), 420) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 4 });
    }

    private void ConfigureToolbar()
    {
        _tool.Items.Add(new ToolStripButton("Faz 37 Layout", null, (_, __) => ApplyPhase(37)));
        _tool.Items.Add(new ToolStripButton("Faz 38 Performance", null, (_, __) => ApplyPhase(38)));
        _tool.Items.Add(new ToolStripButton("Faz 39 Interaction", null, (_, __) => ApplyPhase(39)));
        _tool.Items.Add(new ToolStripButton("Faz 40 Analytics", null, (_, __) => ApplyPhase(40)));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Layout Kaydet", null, (_, __) => { _layoutJson = _grid.SaveEnterpriseLayoutToJson("Audix / Factory preset"); _notes.Text = _layoutJson; }));
        _tool.Items.Add(new ToolStripButton("Layout Yükle", null, (_, __) => { if (!string.IsNullOrWhiteSpace(_layoutJson)) _grid.LoadEnterpriseLayoutFromJson(_layoutJson); }));
        _tool.Items.Add(new ToolStripButton("Command Palette", null, (_, __) => _grid.ShowCommandPalette()));
        _tool.Items.Add(new ToolStripButton("Analytics Özeti", null, (_, __) => _notes.Text = _grid.CreateAnalyticsSummaryText()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Koyu", null, (_, __) => ApplyDarkTheme()));
        _tool.Items.Add(new ToolStripButton("Açık", null, (_, __) => ApplyLightTheme()));
    }

    private void ApplyPhase(int phase)
    {
        switch (phase)
        {
            case 37:
                _grid.ApplyV37EnterpriseLayoutPack();
                _grid.SetViewMode(PanoViewMode.RowPreview);
                _notes.Text = "Faz 37 Enterprise Layout: görünüm modu, kolon genişliği/sırası/görünürlük ve layout snapshot akışı.";
                break;
            case 38:
                _grid.ApplyV38PerformanceProfile(PanoV38PerformancePreset.MediaLibrary);
                _notes.Text = "Faz 38 Performance Pro: büyük veri, hızlı filtre popup, medya lazy-load/cache ve düşük bellek profilleri.";
                break;
            case 39:
                _grid.ApplyV39InteractionProfile(PanoV39InteractionPreset.PowerUser);
                _grid.SetViewMode(PanoViewMode.DashboardCard);
                _notes.Text = string.Join(Environment.NewLine, _grid.ShortcutActions.Select(x => x.KeyText + " - " + x.Title + ": " + x.Description));
                break;
            case 40:
                _grid.ApplyV40AnalyticsProfile(PanoV40AnalyticsPreset.FactoryOverview);
                _notes.Text = _grid.CreateAnalyticsSummaryText();
                break;
        }
    }

    private void ApplyDarkTheme()
    {
        var theme = PanoTheme.DarkTheme();
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        _notes.BackColor = theme.PanelBackColor;
        _notes.ForeColor = theme.ForeColor;
        _grid.ApplyTheme(theme);
    }

    private void ApplyLightTheme()
    {
        var theme = PanoTheme.LightTheme();
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        _notes.BackColor = theme.PanelBackColor;
        _notes.ForeColor = theme.ForeColor;
        _grid.ApplyTheme(theme);
    }

    private void LoadRows()
    {
        var rows = new List<ProExperienceRow>();
        string[] types = { "Layout", "Performance", "Interaction", "Analytics", "Audix", "Factory" };
        string[] owners = { "Audix", "AOI", "Line Workspace", "MasterData", "Factory Navigator" };
        string[] statuses = { "Ready", "Warning", "Success", "Info", "Danger" };
        for (int i = 1; i <= 80; i++)
        {
            rows.Add(new ProExperienceRow
            {
                Type = types[i % types.Length],
                Code = "GX-" + i.ToString("000"),
                Title = "Pano Pro Experience faz örneği " + i,
                Owner = owners[i % owners.Length],
                Status = statuses[i % statuses.Length],
                Value = 20 + (i * 7 % 81),
                Detail = "Bu kayıt; layout kaydı, performans profili, command palette/search ve KPI/heatmap/timeline analiz senaryolarını test etmek için hazırlanmıştır."
            });
        }
        _grid.SetObjects(rows);
    }

    private sealed class ProExperienceRow
    {
        public string Type { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Detail { get; set; } = string.Empty;
    }
}

