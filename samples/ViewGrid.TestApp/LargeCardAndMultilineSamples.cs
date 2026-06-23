using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Filtering;
using ViewGrid.Theming;

namespace ViewGrid.TestApp;

public sealed class LargeCardSampleForm : Form
{
    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = ViewGridDataMode.Object,
        ViewMode = ViewGridMode.LargeCard,
        LargeCardPreferredWidth = 560,
        LargeCardPreferredHeight = 176,
        LargeCardMaxTextLines = 8,
        TilePreferredHeight = 120,
        TilePreferredWidth = 260,
        TileMaxTextLines = 5,
        EnforceTilePreferredHeight = true,
        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full,
        FilterMenuMode = ViewGridFilterMenuMode.Both,
        EmptyListMessage = "Büyük kart örneği için kayıt yok"
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Top,
        Height = 58,
        Padding = new Padding(12),
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "Geniş Kart modu: ticket, mesaj, makine ve operatör listeleri gibi daha geniş ve 4-5 satırlık okunabilir kart görünümü için kullanılabilir. Seçim/refresh sonrası yükseklik korunur."
    };

    public LargeCardSampleForm()
    {
        Text = "ViewGrid Geniş Kart / Ticket Görünümü Örneği";
        Width = 1180;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;

        _grid.Columns.Add(new ViewGridColumn("Ticket", nameof(TicketCard.TicketNo), 120));
        _grid.Columns.Add(new ViewGridColumn("Başlık", nameof(TicketCard.Title), 260) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 2 });
        _grid.Columns.Add(new ViewGridColumn("Makine", nameof(TicketCard.Machine), 160));
        _grid.Columns.Add(new ViewGridColumn("Oto Kod", nameof(TicketCard.OtoCode), 100));
        _grid.Columns.Add(new ViewGridColumn("Durum", nameof(TicketCard.Status), 95));
        _grid.Columns.Add(new ViewGridColumn("Mesaj", nameof(TicketCard.LastMessage), 360) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 5 });

        _tool.Items.Add(new ToolStripButton("Geniş Kart", null, (_,__) => SetLargeCard()));
        _tool.Items.Add(new ToolStripButton("Details", null, (_,__) => _grid.SetViewMode(ViewGridMode.Details)));
        _tool.Items.Add(new ToolStripButton("Tile", null, (_,__) => _grid.SetViewMode(ViewGridMode.Tile)));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Geniş kart", null, (_,__) => { _grid.LargeCardPreferredWidth = 520; _grid.TilePreferredWidth = 520; _grid.Invalidate(); }));
        _tool.Items.Add(new ToolStripButton("Kompakt kart", null, (_,__) => { _grid.LargeCardPreferredWidth = 420; _grid.Invalidate(); }));
        _tool.Items.Add(new ToolStripButton("Geniş Kart Satır +", null, (_,__) => { _grid.LargeCardMaxTextLines = Math.Min(12, _grid.LargeCardMaxTextLines + 1); _grid.Invalidate(); }));
        _tool.Items.Add(new ToolStripButton("Geniş Kart Satır -", null, (_,__) => { _grid.LargeCardMaxTextLines = Math.Max(1, _grid.LargeCardMaxTextLines - 1); _grid.Invalidate(); }));
        _tool.Items.Add(new ToolStripButton("Boş liste imzası", null, (_,__) => _grid.SetObjects(Array.Empty<TicketCard>())));
        _tool.Items.Add(new ToolStripButton("Veriyi yükle", null, (_,__) => LoadData()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Açık tema", null, (_,__) => ApplyTheme(false)));
        _tool.Items.Add(new ToolStripButton("Koyu tema", null, (_,__) => ApplyTheme(true)));

        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);

        LoadData();
        ApplyTheme(Program.AppTheme.IsDark);
    }

    private void SetLargeCard()
    {
        _grid.LargeCardPreferredHeight = 176;
        _grid.LargeCardPreferredWidth = 560;
        _grid.LargeCardMaxTextLines = 8;
        _grid.TilePreferredHeight = 120;
        _grid.TilePreferredWidth = 260;
        _grid.TileMaxTextLines = 5;
        _grid.SetViewMode(ViewGridMode.LargeCard);
    }

    private void LoadData()
    {
        _grid.SetObjects(Enumerable.Range(1, 40).Select(i => new TicketCard(
            $"AOI-{i:000000}",
            i % 4 == 0 ? "AOI durdu / ilerlemiyor, destek gerekiyor" : i % 3 == 0 ? "Çok hata var, kontrol eder misiniz?" : "Faskal istiyorum",
            i % 3 == 0 ? "LINE02YASREW" : i % 2 == 0 ? "LINE01YASREW" : "VEMRKD15061",
            (24022000 + i * 37).ToString(),
            i % 5 == 0 ? "Çözüldü" : i % 4 == 0 ? "Bekliyor" : "Açık",
            "Operatörden gelen uzun mesaj burada 4-5 satıra kadar okunabilir şekilde gösterilir. OtoKod, hat/makine, son işlem ve teknisyen cevabı gibi bilgiler kart içinde geniş alanda rahat okunur.")));
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

    private sealed record TicketCard(string TicketNo, string Title, string Machine, string OtoCode, string Status, string LastMessage);
}

public sealed class MultilineCellsSampleForm : Form
{
    private readonly ViewGridControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = ViewGridDataMode.Object,
        ViewMode = ViewGridMode.Details,
        RowHeight = 30,
        AllowMultilineCells = true,
        MaxCellTextLines = 5,
        AutoRowHeightForMultilineCells = true,
        HeaderContextMenuBehavior = ViewGridHeaderContextMenuBehavior.Full,
        FilterMenuMode = ViewGridFilterMenuMode.Both,
        EmptyListMessage = "Çok satırlı hücre örneği için kayıt yok"
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Top,
        Height = 58,
        Padding = new Padding(12),
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "Details modunda WordWrap kolonları hücre genişliğine göre 4-5 satıra bölünür. Mesaj, açıklama, log ve hata detayı kolonlarında kullanılabilir."
    };

    public MultilineCellsSampleForm()
    {
        Text = "ViewGrid Details Çok Satırlı Hücre Örneği";
        Width = 1180;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;

        _grid.Columns.Add(new ViewGridColumn("Id", nameof(MessageRow.Id), 70));
        _grid.Columns.Add(new ViewGridColumn("Makine", nameof(MessageRow.Machine), 150));
        _grid.Columns.Add(new ViewGridColumn("Kısa Durum", nameof(MessageRow.Status), 120));
        _grid.Columns.Add(new ViewGridColumn("Mesaj / Açıklama", nameof(MessageRow.Message), 420) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 5 });
        _grid.Columns.Add(new ViewGridColumn("Teknisyen Notu", nameof(MessageRow.TechnicianNote), 360) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 4 });
        _grid.Columns.Add(new ViewGridColumn("Zaman", nameof(MessageRow.TimeText), 150));

        _tool.Items.Add(new ToolStripButton("Wrap Aç/Kapat", null, (_,__) => { _grid.AllowMultilineCells = !_grid.AllowMultilineCells; }));
        _tool.Items.Add(new ToolStripButton("Max 3 satır", null, (_,__) => _grid.MaxCellTextLines = 3));
        _tool.Items.Add(new ToolStripButton("Max 5 satır", null, (_,__) => _grid.MaxCellTextLines = 5));
        _tool.Items.Add(new ToolStripButton("Max 8 satır", null, (_,__) => _grid.MaxCellTextLines = 8));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Boş liste imzası", null, (_,__) => _grid.SetObjects(Array.Empty<MessageRow>())));
        _tool.Items.Add(new ToolStripButton("Veriyi yükle", null, (_,__) => LoadData()));
        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Açık tema", null, (_,__) => ApplyTheme(false)));
        _tool.Items.Add(new ToolStripButton("Koyu tema", null, (_,__) => ApplyTheme(true)));

        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);

        LoadData();
        ApplyTheme(Program.AppTheme.IsDark);
    }

    private void LoadData()
    {
        _grid.SetObjects(Enumerable.Range(1, 80).Select(i => new MessageRow(
            i,
            i % 3 == 0 ? "LINE02YASREW" : i % 2 == 0 ? "LINE01YASREW" : "VEMRKD15061",
            i % 5 == 0 ? "ActionRequired" : i % 2 == 0 ? "Warning" : "Info",
            "Bu hücredeki uzun içerik kolon genişliğine göre otomatik sarılır. Kullanıcı pencereyi büyütüp küçülttüğünde metin yeniden ölçülür; sığmayan kısım tek satırda kaybolmak yerine düzgün şekilde alt satırlara bölünür.",
            "Program kontrol ediliyor, SAP/PCB bilgisi karşılaştırılıyor. Gerekirse operatör tekrar denemeden önce teknisyen sonucu bu not alanından takip edebilir.",
            DateTime.Now.AddMinutes(-i * 7).ToString("dd.MM.yyyy HH:mm"))));
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

    private sealed record MessageRow(int Id, string Machine, string Status, string Message, string TechnicianNote, string TimeText);
}
