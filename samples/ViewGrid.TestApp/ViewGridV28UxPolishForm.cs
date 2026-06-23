using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Theming;

namespace ViewGrid.TestApp;

public sealed class ViewGridV28UxPolishForm : Form
{
    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        ViewMode = ViewGridMode.Poster,
        ShowHeader = false,
        EmptyListMessage = "Kart yok"
    };

    private readonly ToolStrip _tool = new() { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top };

    public ViewGridV28UxPolishForm()
    {
        Text = "ViewGrid v28 UX Polish - Top Bar Filter";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(900, 560);
        ViewGridWindowChrome.ApplyOnHandleCreated(this, () => Program.AppTheme, true);

        BuildToolbar();
        ConfigureGrid();
        Controls.Add(_grid);
        Controls.Add(_tool);
        Load += (_, __) => LoadRows();
    }

    private void BuildToolbar()
    {
        _tool.Items.Add("Top Bar filtre", null, (_, __) => _grid.UseTopBarFilterOnly());
        _tool.Items.Add("Hybrid eski davranış", null, (_, __) => _grid.UseHybridCardFilterUx());
        _tool.Items.Add("MasterData UX", null, (_, __) => _grid.ApplyV28UxPolish(ViewGridUxPolishPreset.MasterData));
        _tool.Items.Add("SupportDesk UX", null, (_, __) => _grid.ApplyV28UxPolish(ViewGridUxPolishPreset.SupportDesk));
        _tool.Items.Add("Poster UX", null, (_, __) => _grid.ApplyV28UxPolish(ViewGridUxPolishPreset.PosterGallery));
        _tool.Items.Add("Poster Mode", null, (_, __) => { _grid.SetViewMode(ViewGridMode.Poster); _grid.ApplyPosterModeDefaults(); });
        _tool.Items.Add("Filtreleri temizle", null, (_, __) => _grid.ClearFilters());
        _tool.Items.Add(new ToolStripLabel("  v28: global filtre üst barda; kart alanı sadece veri/aksiyon için kalır."));
    }

    private void ConfigureGrid()
    {
        _grid.ApplyTheme(Program.AppTheme);
        _grid.SetViewMode(ViewGridMode.Poster);
        _grid.PosterPreferredWidth = 220;
        _grid.PosterPreferredHeight = 300;
        _grid.PosterImageHeight = 176;
        _grid.ApplyV28UxPolish(ViewGridUxPolishPreset.PosterGallery);
        _grid.Columns.Add(new ViewGridColumn("Poster", nameof(PosterRow.Title), 220) { Filterable = true });
        _grid.Columns.Add(new ViewGridColumn("Yıl", nameof(PosterRow.Year), 90) { Filterable = true });
        _grid.Columns.Add(new ViewGridColumn("Tür", nameof(PosterRow.Type), 140) { Filterable = true });
        _grid.Columns.Add(new ViewGridColumn("Hat / Proje", nameof(PosterRow.Line), 160) { Filterable = true });
        _grid.Columns.Add(new ViewGridColumn("Durum", nameof(PosterRow.Status), 120) { Filterable = true });
    }

    private void LoadRows()
    {
        var rows = new List<PosterRow>();
        string[] types = { "AOI", "Production", "Drama", "MasterData", "SupportDesk" };
        string[] lines = { "AOI/MasterData", "FlexUltra", "QX250i", "Line-01", "Line-02" };
        string[] statuses = { "Open", "Waiting", "Ready", "Closed" };
        for (int i = 1; i <= 80; i++)
        {
            rows.Add(new PosterRow(
                $"ViewGrid Poster {i:00}",
                2020 + (i % 6),
                types[i % types.Length],
                lines[i % lines.Length],
                statuses[i % statuses.Length]));
        }
        _grid.SetObjects(rows);
    }

    private sealed record PosterRow(string Title, int Year, string Type, string Line, string Status);
}
