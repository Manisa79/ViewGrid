using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Filtering;
using ViewGrid.Theming;

namespace ViewGrid.TestApp;

public sealed class ViewGridV273RendererShowcaseForm : Form
{
    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = ViewGridDataMode.Object,
        ViewMode = ViewGridMode.Details,
        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full,
        FilterMenuMode = ViewGridFilterMenuMode.Both,
        EmptyListMessage = "v27.3 renderer showcase için kayıt yok",
        EnableV273RenderingUx = true,
        BadgeUseSemanticStatusColors = true,
        TagsUseChipRenderer = true,
        CellPillCornerRadius = 10,
        EnableModernProgressBar = true,
        ProgressBarShowText = true,
        ProgressBarUseGradient = true,
        ShowGridLines = true,
        AlternateRows = true,
        RowHeight = 42
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Top,
        Height = 74,
        Padding = new Padding(14, 8, 14, 8),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private bool _dark = true;

    public ViewGridV273RendererShowcaseForm()
    {
        Text = "ViewGrid v27.3 Renderer Showcase";
        Width = 1280;
        Height = 760;
        MinimumSize = new Size(980, 620);
        StartPosition = FormStartPosition.CenterParent;

        BuildToolbar();
        ConfigureColumns();
        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);

        ApplyTheme(Program.AppTheme.IsDark);
        LoadRows();
    }

    private void BuildToolbar()
    {
        _tool.Items.Add(new ToolStripButton("Semantic badge açık/kapat", null, (_, __) =>
        {
            _grid.BadgeUseSemanticStatusColors = !_grid.BadgeUseSemanticStatusColors;
            _grid.Invalidate();
        }));
        _tool.Items.Add(new ToolStripButton("Chip tag açık/kapat", null, (_, __) =>
        {
            _grid.TagsUseChipRenderer = !_grid.TagsUseChipRenderer;
            _grid.Invalidate();
        }));
        _tool.Items.Add(new ToolStripButton("Progress animasyon", null, (_, __) =>
        {
            _grid.ProgressBarAnimated = !_grid.ProgressBarAnimated;
            _grid.Invalidate();
        }));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Grupla: Durum", null, (_, __) => _grid.SetGroupBy(nameof(RendererRow.Status))));
        _tool.Items.Add(new ToolStripButton("Grupla: Alan", null, (_, __) => _grid.SetGroupBy(nameof(RendererRow.Area))));
        _tool.Items.Add(new ToolStripButton("Gruplamayı temizle", null, (_, __) => _grid.ClearGrouping()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Açık/Koyu", null, (_, __) => ApplyTheme(!_dark)));
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new ViewGridColumn("Id", nameof(RendererRow.Id), 64) { TextAlign = ContentAlignment.MiddleRight });
        _grid.Columns.Add(new ViewGridColumn("Alan", nameof(RendererRow.Area), 120) { Kind = ViewGridColumnKind.Badge, AllowGroup = true });
        _grid.Columns.Add(new ViewGridColumn("Durum", nameof(RendererRow.Status), 128) { Kind = ViewGridColumnKind.Badge, AllowGroup = true });
        _grid.Columns.Add(new ViewGridColumn("Başlık", nameof(RendererRow.Title), 260) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 2 });
        _grid.Columns.Add(new ViewGridColumn("Etiketler", nameof(RendererRow.Tags), 230) { Kind = ViewGridColumnKind.Tags, TagSeparator = ";" });
        _grid.Columns.Add(new ViewGridColumn("İlerleme", nameof(RendererRow.Progress), 120) { Kind = ViewGridColumnKind.ProgressBar });
        _grid.Columns.Add(new ViewGridColumn("Aksiyon", nameof(RendererRow.Action), 118) { Kind = ViewGridColumnKind.Button, ButtonText = "Aç", ButtonSizing = ViewGridColumnButtonSizing.TextBounds });
        _grid.Columns.Add(new ViewGridColumn("Link", nameof(RendererRow.Link), 170) { Kind = ViewGridColumnKind.Hyperlink });
        _grid.Columns.Add(new ViewGridColumn("Açıklama", nameof(RendererRow.Description), 360) { WordWrap = true, MaxTextLines = 3, FillFreeSpace = true });

        _grid.ButtonClick += (_, e) => MessageBox.Show(this, "Aksiyon: " + ((RendererRow)e.RowObject).Title, "ViewGrid v27.3");
        _grid.HyperlinkClick += (_, e) => MessageBox.Show(this, "Link: " + Convert.ToString(e.Column.GetValue(e.RowObject)), "ViewGrid v27.3");
    }

    private void LoadRows()
    {
        var rows = new List<RendererRow>();
        string[] areas = { "AOI", "MasterData", "SAP", "Program", "Server", "ViewGrid" };
        string[] statuses = { "Open", "In Progress", "Waiting", "Done", "OK", "Eksik", "Offline", "Warning", "Fail", "Hazır" };
        for (int i = 1; i <= 72; i++)
        {
            string area = areas[i % areas.Length];
            string status = statuses[i % statuses.Length];
            rows.Add(new RendererRow
            {
                Id = i,
                Area = area,
                Status = status,
                Title = area switch
                {
                    "AOI" => "False call / makine durdu destek akışı",
                    "MasterData" => "BOM pozisyonu ve SAP kırılımı",
                    "SAP" => "WebService sorgu sonucu ve yarımamül kontrolü",
                    "Program" => "Makine program dosyası ve klasör eşleşmesi",
                    "Server" => "Bağlantı, client ve ticket servis durumu",
                    _ => "ViewGrid renderer ve UX davranışı"
                },
                Tags = BuildTags(area, status, i),
                Progress = Math.Min(100, 8 + (i * 7) % 96),
                Action = "Aç",
                Link = area + " detay",
                Description = "v27.3 renderer engine: durum badge renkleri, chip tag listesi, modern progressbar, button ve hyperlink hücreleri tek örnekte gösterilir."
            });
        }

        _grid.SetObjects(rows);
    }

    private static string BuildTags(string area, string status, int index)
    {
        var tags = new List<string> { area, status };
        if (index % 2 == 0) tags.Add("TOP");
        if (index % 3 == 0) tags.Add("BOT");
        if (index % 5 == 0) tags.Add("SAP");
        if (index % 7 == 0) tags.Add("BOM");
        return string.Join(';', tags);
    }

    private void ApplyTheme(bool dark)
    {
        _dark = dark;
        Color back = dark ? Color.FromArgb(24, 26, 31) : Color.FromArgb(246, 248, 252);
        Color fore = dark ? Color.White : Color.FromArgb(28, 32, 38);
        BackColor = back;
        ForeColor = fore;
        _info.BackColor = dark ? Color.FromArgb(32, 35, 42) : Color.White;
        _info.ForeColor = fore;
        _info.Text = "v27.3 Rendering & UX Engine — Badge, Progress, Tags, Button, Hyperlink ve semantic status renkleri. Bu ekran artık tek Example Center içinde yönetilir.";
        ViewGridTheme theme = ViewGridTheme.FromParentColor(back, fore);
        _grid.ApplyTheme(theme);
        SmartMenuRenderer.ApplyTo(_tool, theme);
    }

    private sealed class RendererRow
    {
        public int Id { get; set; }
        public string Area { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
