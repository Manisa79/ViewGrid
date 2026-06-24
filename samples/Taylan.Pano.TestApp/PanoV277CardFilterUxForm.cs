using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Filtering;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed class PanoV277CardFilterUxForm : Form
{
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        ViewMode = PanoViewMode.DashboardCard,
        FilterMenuMode = PanoFilterMenuMode.Both,
        ShowQuickFilterBar = true,
        ShowFloatingFilterButton = true,
        ShowActiveFilterChips = true,
        CardFilterUxOnlyInCardViews = true,
        CardFilterUxPlacement = PanoCardFilterUxPlacement.TopBarAndFloatingButton,
        QuickFilterPlaceholderText = "Ticket, makine, SAP kodu veya açıklama ara...",
        FloatingFilterButtonText = "Filtre",
        FilterPopupResizable = true,
        FilterPopupRememberSize = true,
        FilterPopupShowValueTooltips = true,
        FilterPopupAutoWidthForLongValues = true,
        HeaderContextMenuBehavior = PanoHeaderContextMenuBehavior.Full,
        EmptyListMessage = "Kart filtre UX örneği için kayıt yok"
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };

    public PanoV277CardFilterUxForm()
    {
        Text = "Pano v27.7 - Card / Large View Filter UX";
        Width = 1320;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;

        ConfigureGrid();
        ConfigureToolbar();
        Controls.Add(_grid);
        Controls.Add(_tool);
        ApplyTheme(false);
        LoadRows();
    }

    private void ConfigureToolbar()
    {
        _tool.Items.Add(new ToolStripButton("Dashboard Kart", null, (_, __) => _grid.SetViewMode(PanoViewMode.DashboardCard)));
        _tool.Items.Add(new ToolStripButton("Geniş Kart", null, (_, __) => _grid.SetViewMode(PanoViewMode.LargeCard)));
        _tool.Items.Add(new ToolStripButton("Kanban", null, (_, __) => _grid.SetViewMode(PanoViewMode.Kanban)));
        _tool.Items.Add(new ToolStripButton("Detay Liste", null, (_, __) => _grid.SetViewMode(PanoViewMode.Details)));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Top Bar Aç/Kapat", null, (_, __) => { _grid.ShowQuickFilterBar = !_grid.ShowQuickFilterBar; _grid.RefreshView(); }));
        _tool.Items.Add(new ToolStripButton("Floating Aç/Kapat", null, (_, __) => { _grid.ShowFloatingFilterButton = !_grid.ShowFloatingFilterButton; _grid.RefreshView(); }));
        _tool.Items.Add(new ToolStripButton("Chip Aç/Kapat", null, (_, __) => { _grid.ShowActiveFilterChips = !_grid.ShowActiveFilterChips; _grid.RefreshView(); }));
        _tool.Items.Add(new ToolStripButton("Filtreleri Temizle", null, (_, __) => _grid.ClearFilters()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Açık Tema", null, (_, __) => ApplyTheme(false)));
        _tool.Items.Add(new ToolStripButton("Koyu Tema", null, (_, __) => ApplyTheme(true)));
    }

    private void ConfigureGrid()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new PanoColumn("Seç", nameof(CardFilterRow.Selected), 50) { Kind = PanoColumnKind.CheckBox });
        _grid.Columns.Add(new PanoColumn("Durum", nameof(CardFilterRow.Status), 120) { Kind = PanoColumnKind.Badge, AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("Ticket", nameof(CardFilterRow.Ticket), 140));
        _grid.Columns.Add(new PanoColumn("Başlık", nameof(CardFilterRow.Title), 280) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 3 });
        _grid.Columns.Add(new PanoColumn("Makine", nameof(CardFilterRow.Machine), 140) { AllowGroup = true });
        _grid.Columns.Add(new PanoColumn("SAP / Oto Kod", nameof(CardFilterRow.SapCode), 170));
        _grid.Columns.Add(new PanoColumn("İlerleme", nameof(CardFilterRow.Progress), 100) { Kind = PanoColumnKind.ProgressBar });
        _grid.Columns.Add(new PanoColumn("Etiketler", nameof(CardFilterRow.Tags), 220) { Kind = PanoColumnKind.Tags });
        _grid.Columns.Add(new PanoColumn("Açıklama", nameof(CardFilterRow.Detail), 520) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 5 });
    }

    private void LoadRows()
    {
        string[] statuses = { "Open", "In Progress", "Waiting", "Done", "Fail" };
        string[] machines = { "AOI-QX150", "AOI-FLEX", "LINE-03", "REWORK-01", "AXIAL-02" };
        var rows = new List<CardFilterRow>();
        for (int i = 1; i <= 80; i++)
        {
            rows.Add(new CardFilterRow
            {
                Selected = i % 9 == 0,
                Status = statuses[i % statuses.Length],
                Ticket = "TCK-" + i.ToString("0000"),
                Title = i % 3 == 0 ? "Uzun SAP malzeme açıklaması ve program yolu olan üretim ticket kartı" : "AOI destek / MasterData kart filtre örneği",
                Machine = machines[i % machines.Length],
                SapCode = "SAP-" + (100000 + i) + " / OTO-" + (9000 + i),
                Progress = (i * 7) % 101,
                Tags = i % 2 == 0 ? "BOM;SAP;Program" : "AOI;False Call;Rework",
                Detail = "CardView, DashboardCard, Kanban ve Poster gibi büyük görünümlerde header görünmese bile kullanıcı üst filtre barından arama yapabilir, kolon filtresini açabilir ve aktif filtreleri chip olarak görebilir. Uzun açıklamalar ve SAP yolları filtre popup içinde de büyütülebilir."
            });
        }
        _grid.SetObjects(rows);
    }

    private void ApplyTheme(bool dark)
    {
        PanoTheme theme = dark ? PanoTheme.DarkTheme() : PanoTheme.LightTheme();
        BackColor = theme.BackColor;
        ForeColor = theme.ForeColor;
        _grid.ApplyTheme(theme);
        SmartMenuRenderer.ApplyTo(_tool, theme);
    }

    private sealed class CardFilterRow
    {
        public bool Selected { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Ticket { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Machine { get; set; } = string.Empty;
        public string SapCode { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string Tags { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }
}

