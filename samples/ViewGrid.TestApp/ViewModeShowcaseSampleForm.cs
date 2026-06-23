using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Filtering;
using ViewGrid.Theming;

namespace ViewGrid.TestApp;

public sealed class ViewModeShowcaseSampleForm : Form
{
    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = ViewGridDataMode.Object,
        ViewMode = ViewGridMode.DashboardCard,
        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full,
        FilterMenuMode = ViewGridFilterMenuMode.Both,
        TilePreferredWidth = 250,
        TilePreferredHeight = 104,
        TileMaxTextLines = 4,
        LargeCardPreferredWidth = 560,
        LargeCardPreferredHeight = 170,
        LargeCardMaxTextLines = 8,
        EnforceTilePreferredHeight = true,
        EmptyListMessage = "Görünüm vitrini için kayıt yok"
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Top,
        Height = 66,
        Padding = new Padding(12),
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "ViewGrid görünüm vitrini: AOI ticket, makine, operatör, mesaj, SAP pozisyon ve dashboard senaryoları için adlandırılmış görünüm seçenekleri. Sağ tuş menüsünde de aynı isimlerle görünür."
    };

    private readonly List<ShowcaseTicket> _rows = new();

    public ViewModeShowcaseSampleForm()
    {
        Text = "ViewGrid Gelişmiş Görünüm Vitrini";
        Width = 1240;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;

        _grid.Columns.Add(new ViewGridColumn("Ticket", nameof(ShowcaseTicket.TicketNo), 120));
        _grid.Columns.Add(new ViewGridColumn("Başlık", nameof(ShowcaseTicket.Title), 260) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 2 });
        _grid.Columns.Add(new ViewGridColumn("Makine", nameof(ShowcaseTicket.Machine), 150));
        _grid.Columns.Add(new ViewGridColumn("Durum", nameof(ShowcaseTicket.Status), 100) { Kind = ViewGridColumnKind.Badge });
        _grid.Columns.Add(new ViewGridColumn("Öncelik", nameof(ShowcaseTicket.Priority), 95));
        _grid.Columns.Add(new ViewGridColumn("Oto Kod", nameof(ShowcaseTicket.OtoCode), 105));
        _grid.Columns.Add(new ViewGridColumn("Son Mesaj", nameof(ShowcaseTicket.LastMessage), 420) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 5 });
        _grid.Columns.Add(new ViewGridColumn("Zaman", nameof(ShowcaseTicket.TimeText), 140));

        AddModeButton(ViewGridMode.Details);
        AddModeButton(ViewGridMode.DenseList);
        AddModeButton(ViewGridMode.Tile);
        AddModeButton(ViewGridMode.LargeCard);
        AddModeButton(ViewGridMode.DashboardCard);
        AddModeButton(ViewGridMode.RowCard);
        AddModeButton(ViewGridMode.RowPreview);
        AddModeButton(ViewGridMode.DetailCard);
        AddModeButton(ViewGridMode.PropertyCard);
        _tool.Items.Add(new ToolStripSeparator());
        AddModeButton(ViewGridMode.Poster);
        AddModeButton(ViewGridMode.MediaTile);
        AddModeButton(ViewGridMode.Gallery);
        AddModeButton(ViewGridMode.FilmStrip);
        AddModeButton(ViewGridMode.IconGrid);
        _tool.Items.Add(new ToolStripSeparator());
        AddModeButton(ViewGridMode.KpiDashboard);
        AddModeButton(ViewGridMode.HeatMap);
        AddModeButton(ViewGridMode.MiniChart);
        AddModeButton(ViewGridMode.GroupCard);
        AddModeButton(ViewGridMode.GroupedList);
        AddModeButton(ViewGridMode.Kanban);
        AddModeButton(ViewGridMode.Timeline);
        AddModeButton(ViewGridMode.MasterDetail);
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Gruplamayı temizle", null, (_, __) => _grid.ClearGrouping()));
        var detailHeaderToggle = new ToolStripButton("DetailCard başlıkları")
        {
            CheckOnClick = true,
            Checked = _grid.ShowDetailCardColumnHeaders
        };
        detailHeaderToggle.CheckedChanged += (_, __) => _grid.ShowDetailCardColumnHeaders = detailHeaderToggle.Checked;
        _tool.Items.Add(detailHeaderToggle);

        var fixedFreeToggle = new ToolStripButton("FixedFree resize")
        {
            CheckOnClick = true,
            Checked = _grid.AbsorbColumnResizeOverflowFromFreeSpace
        };
        fixedFreeToggle.CheckedChanged += (_, __) => _grid.AbsorbColumnResizeOverflowFromFreeSpace = fixedFreeToggle.Checked;
        _tool.Items.Add(fixedFreeToggle);

        _tool.Items.Add(new ToolStripButton("Açık tema", null, (_, __) => ApplyTheme(false)));
        _tool.Items.Add(new ToolStripButton("Koyu tema", null, (_, __) => ApplyTheme(true)));

        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);

        LoadRows();
        ApplyTheme(Program.AppTheme.IsDark);
        ApplyMode(ViewGridMode.DashboardCard);
    }

    private void AddModeButton(ViewGridMode mode)
    {
        _tool.Items.Add(new ToolStripButton(ViewGridControl.GetViewModeDisplayName(mode), null, (_, __) => ApplyMode(mode)));
    }

    private void ApplyMode(ViewGridMode mode)
    {
        if (mode != ViewGridMode.GroupedList)
            _grid.ClearGrouping();

        _grid.TilePosterMode = false;
        _grid.AutoSizeTileWidthToContent = false;

        switch (mode)
        {
            case ViewGridMode.DenseList:
                _grid.AllowMultilineCells = false;
                _grid.RowHeight = 22;
                break;

            case ViewGridMode.Tile:
                _grid.TilePreferredWidth = 250;
                _grid.TilePreferredHeight = 104;
                _grid.TileMaxTextLines = 4;
                break;

            case ViewGridMode.LargeCard:
                _grid.LargeCardPreferredWidth = 560;
                _grid.LargeCardPreferredHeight = 172;
                _grid.LargeCardMaxTextLines = 8;
                break;

            case ViewGridMode.DashboardCard:
                _grid.TilePreferredWidth = 360;
                _grid.LargeCardPreferredHeight = 158;
                _grid.LargeCardMaxTextLines = 6;
                break;

            case ViewGridMode.RowCard:
                _grid.LargeCardPreferredWidth = 900;
                _grid.TilePreferredHeight = 128;
                _grid.LargeCardMaxTextLines = 7;
                break;

            case ViewGridMode.MediaTile:
                _grid.TilePosterMode = true;
                _grid.TilePreferredWidth = 190;
                _grid.TilePreferredHeight = 236;
                _grid.TilePosterImageHeight = 128;
                _grid.TileMaxTextLines = 4;
                break;

            case ViewGridMode.FilmStrip:
                _grid.TilePosterMode = true;
                _grid.TilePreferredWidth = 900;
                _grid.TilePreferredHeight = 164;
                _grid.TilePosterImageHeight = 116;
                _grid.TileMaxTextLines = 7;
                break;

            case ViewGridMode.DetailCard:
                _grid.LargeCardPreferredWidth = 980;
                _grid.LargeCardMaxTextLines = 12;
                break;

            case ViewGridMode.IconGrid:
                _grid.TilePreferredWidth = 180;
                _grid.TilePreferredHeight = 96;
                _grid.TileMaxTextLines = 2;
                break;

            case ViewGridMode.GroupedList:
                _grid.SetGroupBy(nameof(ShowcaseTicket.Status));
                break;

            case ViewGridMode.Kanban:
                _grid.TilePreferredWidth = 330;
                _grid.LargeCardPreferredHeight = 174;
                _grid.LargeCardMaxTextLines = 6;
                _grid.SetGroupBy(nameof(ShowcaseTicket.Status));
                break;

            case ViewGridMode.Timeline:
                _grid.LargeCardPreferredWidth = 900;
                _grid.TilePreferredHeight = 136;
                _grid.LargeCardMaxTextLines = 8;
                break;

            case ViewGridMode.MasterDetail:
                _grid.AllowMultilineCells = true;
                _grid.MaxCellTextLines = 3;
                break;
        }

        _grid.SetViewMode(mode);
        _info.Text = GetModeDescription(mode);
    }

    private static string GetModeDescription(ViewGridMode mode)
    {
        return mode switch
        {
            ViewGridMode.Details => "Detay Liste: klasik tablo; SAP pozisyonları, BOM, SQL kayıtları ve kolon bazlı filtreleme için en net görünüm.",
            ViewGridMode.DenseList => "Yoğun Liste: çok fazla satırı aynı anda görmek için Excel benzeri sıkı satır yüksekliği.",
            ViewGridMode.Tile => "Kart Görünümü: ticket, operatör veya makine kayıtlarını yan yana özet kartlarla gösterir.",
            ViewGridMode.LargeCard => "Geniş Kart: 4-5 satır okunabilir açıklama isteyen ticket/mesaj ekranları için.",
            ViewGridMode.DashboardCard => "Dashboard Kart: durum/öncelik odaklı, üst vurgu çizgili modern kart görünümü.",
            ViewGridMode.RowCard => "Satır Kart: her kaydı tam genişlikte kart olarak gösterir; destek mesajları ve son cevap akışında rahat okunur.",
            ViewGridMode.MediaTile => "MediaTile: albüm kapağı, film afişi, öğrenci/makine fotoğrafı gibi kompakt medya katalogları için.",
            ViewGridMode.FilmStrip => "FilmStrip: solda büyük görsel, sağda açıklama olan yatay medya şeridi; ticket eki, sahne karesi ve arşiv listeleri için.",
            ViewGridMode.DetailCard => "DetailCard: her kaydı tam genişlikte kart yapar ve tüm görünür kolonları etiket/değer olarak satır satır gösterir.",
            ViewGridMode.IconGrid => "İkon Grid: hat, makine ve operatör seçimi gibi görsel seçim ekranları için.",
            ViewGridMode.GroupedList => "Gruplu Liste: durum, makine veya hat bazlı bölümleyerek yönetim ekranlarını okunur hale getirir.",
            ViewGridMode.Kanban => "Kanban: açık/bekliyor/çözüldü gibi durumlara göre ticket takibi için kart + grup mantığı.",
            ViewGridMode.Timeline => "Zaman Akışı: mesaj, olay ve ticket geçmişini kronolojik takip etmek için.",
            ViewGridMode.MasterDetail => "Master-Detail: üstte kayıt, altta/yan tarafta detay paneli kullanacak formlar için liste odaklı temel görünüm.",
            _ => ViewGridControl.GetViewModeDisplayName(mode)
        };
    }

    private void LoadRows()
    {
        string[] statuses = { "Açık", "Bekliyor", "İşlemde", "Çözüldü" };
        string[] priorities = { "Info", "Warning", "Action" };
        string[] machines = { "LINE01YASREW", "LINE02YASREW", "VEMRKD15061", "AOIQX250I" };

        _rows.Clear();
        for (int i = 1; i <= 64; i++)
        {
            string status = statuses[i % statuses.Length];
            _rows.Add(new ShowcaseTicket(
                $"AOI-{i:000000}",
                i % 4 == 0 ? "AOI durdu, destek gerekiyor" : i % 3 == 0 ? "Çok hata var, kontrol eder misiniz?" : "Program / faskal isteği",
                machines[i % machines.Length],
                status,
                priorities[i % priorities.Length],
                (24022000 + i * 37).ToString(),
                "Operatörden gelen mesaj, makine bilgisi, son teknisyen cevabı ve SAP/OtoKod bilgisi bu alanda görünür. Pencere genişliğine göre kart/liste görünümü daha okunabilir olur.",
                DateTime.Now.AddMinutes(-i * 9).ToString("dd.MM HH:mm")));
        }
        _grid.SetObjects(_rows);
    }

    private void ApplyTheme(bool dark)
    {
        BackColor = dark ? Color.FromArgb(28, 29, 33) : Color.FromArgb(246, 248, 252);
        ForeColor = dark ? Color.White : Color.FromArgb(28, 31, 36);
        _info.BackColor = BackColor;
        _info.ForeColor = ForeColor;
        var theme = ViewGridTheme.FromParentColor(BackColor, ForeColor);
        _grid.ApplyTheme(theme);
        SmartMenuRenderer.ApplyTo(_tool, theme);
    }

    private sealed record ShowcaseTicket(string TicketNo, string Title, string Machine, string Status, string Priority, string OtoCode, string LastMessage, string TimeText);
}
