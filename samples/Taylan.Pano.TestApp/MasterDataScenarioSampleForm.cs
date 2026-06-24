using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Filtering;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed class MasterDataScenarioSampleForm : Form
{
    private readonly PanoControl _grid = new()
    {
        Dock = DockStyle.Fill,
        Mode = PanoDataMode.Object,
        HeaderContextMenuBehavior = PanoHeaderContextMenuBehavior.Full,
        FilterMenuMode = PanoFilterMenuMode.Both,
        EmptyListMessage = "MasterData senaryosu için kayıt yok"
    };

    private readonly ToolStrip _tool = new() { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
    private readonly Label _info = new()
    {
        Dock = DockStyle.Top,
        Height = 72,
        Padding = new Padding(12),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly List<MasterDataRow> _rows = new();

    public MasterDataScenarioSampleForm()
    {
        Text = "Pano MasterData Görünüm Senaryoları";
        Width = 1280;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;

        ConfigureColumns();
        ConfigureToolbar();

        Controls.Add(_grid);
        Controls.Add(_info);
        Controls.Add(_tool);

        ApplyTheme(Program.AppTheme.IsDark);
        ApplyScenario(PanoScenario.DataTable);
    }

    private void ConfigureColumns()
    {
        _grid.Columns.Add(new PanoColumn("Tür", nameof(MasterDataRow.Type), 110) { Kind = PanoColumnKind.Badge });
        _grid.Columns.Add(new PanoColumn("Kod", nameof(MasterDataRow.Code), 150));
        _grid.Columns.Add(new PanoColumn("Açıklama", nameof(MasterDataRow.Name), 280) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 3 });
        _grid.Columns.Add(new PanoColumn("Makine", nameof(MasterDataRow.Machine), 120));
        _grid.Columns.Add(new PanoColumn("Katman", nameof(MasterDataRow.Layer), 80));
        _grid.Columns.Add(new PanoColumn("RefDes", nameof(MasterDataRow.RefDes), 120));
        _grid.Columns.Add(new PanoColumn("Miktar", nameof(MasterDataRow.Quantity), 70));
        _grid.Columns.Add(new PanoColumn("Durum", nameof(MasterDataRow.Status), 100) { Kind = PanoColumnKind.Badge });
        _grid.Columns.Add(new PanoColumn("Detay", nameof(MasterDataRow.Detail), 420) { FillFreeSpace = true, WordWrap = true, MaxTextLines = 5 });
    }

    private void ConfigureToolbar()
    {
        foreach (PanoScenario scenario in Enum.GetValues<PanoScenario>())
        {
            PanoScenario captured = scenario;
            _tool.Items.Add(new ToolStripButton(captured.GetDisplayName(), null, (_, __) => ApplyScenario(captured)));
        }

        _tool.Items.Add(new ToolStripSeparator());
        _tool.Items.Add(new ToolStripButton("Koyu tema", null, (_, __) => ApplyTheme(true)));
        _tool.Items.Add(new ToolStripButton("Açık tema", null, (_, __) => ApplyTheme(false)));
    }

    private void ApplyScenario(PanoScenario scenario)
    {
        _grid.ClearGrouping();
        LoadRows(scenario);
        _grid.ApplyScenario(scenario);

        if (scenario == PanoScenario.ProductTree)
            _grid.SetGroupBy(nameof(MasterDataRow.Type));
        else if (scenario == PanoScenario.TicketBoard)
            _grid.SetGroupBy(nameof(MasterDataRow.Status));
        else if (scenario == PanoScenario.ProgramFiles)
            _grid.SetGroupBy(nameof(MasterDataRow.Machine));

        _info.Text = GetScenarioDescription(scenario);
    }

    private void LoadRows(PanoScenario scenario)
    {
        _rows.Clear();

        switch (scenario)
        {
            case PanoScenario.ProductTree:
                AddProductTreeRows();
                break;
            case PanoScenario.BomPositions:
                AddBomRows(120);
                break;
            case PanoScenario.ProgramFiles:
                AddProgramRows();
                break;
            case PanoScenario.MachineOrLinePicker:
                AddMachineRows();
                break;
            case PanoScenario.TicketBoard:
                AddTicketRows();
                break;
            case PanoScenario.Timeline:
                AddTimelineRows();
                break;
            default:
                AddBomRows(scenario == PanoScenario.DenseData ? 260 : 80);
                break;
        }

        _grid.SetObjects(_rows);
    }

    private static string GetScenarioDescription(PanoScenario scenario)
        => scenario switch
        {
            PanoScenario.DataTable => "Standart Tablo: MasterData içinde SAP sonuçları, ürün ağaçları, SQL kayıtları ve kolon filtreleri için güvenli varsayılan görünüm.",
            PanoScenario.DenseData => "Yoğun Veri Tablosu: çok fazla BOM/pozisyon satırında maksimum kayıt görmek için kısa satır yüksekliği.",
            PanoScenario.ProductTree => "Ürün Ağacı: yarı mamul, alt ürün, komponent ve operasyon kırılımlarını gruplu liste olarak gösterir.",
            PanoScenario.BomPositions => "BOM / Pozisyon Listesi: RefDes, malzeme kodu, katman ve miktar gibi teknik satırlar için Excel benzeri görünüm.",
            PanoScenario.ProgramFiles => "Program Dosyaları: makine bazlı çıktı dosyaları, dizgi programları ve kontrol adımları için kart + grup kullanımı.",
            PanoScenario.MachineOrLinePicker => "Makine / Hat Seçimi: Flex, QX, AX, OIB, KnS gibi hat/makine seçimlerinde ikon grid mantığı.",
            PanoScenario.TicketBoard => "Ticket Dashboard: AOI Support Desk tarzı açık/bekliyor/çözüldü durumlarını kartlarla izlemek için.",
            PanoScenario.Timeline => "İşlem Geçmişi: program oluşturma, SAP okuma, kullanıcı kararı ve hata loglarını kronolojik kartlarla göstermek için.",
            PanoScenario.MasterDetail => "MasterData Detay: üstte ana kayıt, altta/yan tarafta pozisyon-detay gridleri olan bakım formları için.",
            _ => scenario.GetDisplayName()
        };

    private void AddBomRows(int count)
    {
        string[] layers = { "TOP", "BOT" };
        string[] status = { "OK", "Eksik", "Kontrol", "Yeni" };
        for (int i = 1; i <= count; i++)
        {
            _rows.Add(new MasterDataRow(
                "BOM",
                $"17MB{i % 90 + 100:000}FRR{i % 7}",
                i % 5 == 0 ? "Stoklaşmış yarı mamul / alt ürün" : "SMD komponent / üretim malzemesi",
                i % 3 == 0 ? "AXIAL" : i % 3 == 1 ? "RADIAL" : "SIPLACE",
                layers[i % 2],
                $"R{i},C{i + 4},U{i % 18 + 1}",
                (i % 12 + 1).ToString(),
                status[i % status.Length],
                "MasterData senaryosunda SAP/BOM satırı, filtreleme, gruplama, çok satırlı açıklama ve kolon düzeni birlikte denenebilir."));
        }
    }

    private void AddProductTreeRows()
    {
        for (int i = 1; i <= 48; i++)
        {
            string type = i % 8 == 0 ? "Yarı Mamul" : i % 5 == 0 ? "Operasyon" : "Komponent";
            _rows.Add(new MasterDataRow(type, $"2402{i:0000}", $"Ürün ağacı kırılımı {i}", "MASTER", i % 2 == 0 ? "TOP" : "BOT", $"P{i}", "1", i % 6 == 0 ? "Kontrol" : "OK", "Ürün ağacı görünümünde type/durum bazlı gruplama ve hiyerarşik okuma amaçlanır."));
        }
    }

    private void AddProgramRows()
    {
        string[] machines = { "Flex", "FlexUltra", "QX100", "QX250i", "OIB", "KnS" };
        for (int i = 1; i <= 54; i++)
        {
            string machine = machines[i % machines.Length];
            _rows.Add(new MasterDataRow("Program", $"{machine}-{24022000 + i}", $"{machine} dizgi programı", machine, i % 2 == 0 ? "TOP" : "BOT", "-", "1", i % 7 == 0 ? "Uyarı" : "Hazır", "Program klasörü, çıktı yolu, tarih, operatör ve kontrol sonucunu kart üzerinde okunur şekilde göstermek için."));
        }
    }

    private void AddMachineRows()
    {
        string[] machines = { "Flex", "FlexUltra", "FlexHR", "QX100", "QX150i", "QX250i", "AXIAL", "RADIAL", "OIB", "KnS" };
        for (int i = 0; i < machines.Length; i++)
        {
            _rows.Add(new MasterDataRow("Makine", machines[i], $"{machines[i]} hattı / program klasörü", machines[i], "-", "-", "-", i % 4 == 0 ? "Pasif" : "Aktif", "Makine eşleşmesi pc adı yerine klasör/makine adı üzerinden okunacak senaryolar için."));
        }
    }

    private void AddTicketRows()
    {
        string[] status = { "Açık", "İşlemde", "Bekliyor", "Çözüldü" };
        for (int i = 1; i <= 56; i++)
        {
            _rows.Add(new MasterDataRow("Ticket", $"AOI-{i:000000}", i % 4 == 0 ? "AOI durdu" : "Program / false call talebi", $"LINE{i % 6 + 1:00}REW", "-", "-", "1", status[i % status.Length], "AOI Support Desk tarafındaki ticket ve mesaj listeleri için dashboard kart görünümü."));
        }
    }

    private void AddTimelineRows()
    {
        for (int i = 1; i <= 42; i++)
        {
            _rows.Add(new MasterDataRow("Log", DateTime.Now.AddMinutes(-i * 7).ToString("dd.MM HH:mm"), i % 3 == 0 ? "SAP ürün ağacı okundu" : i % 3 == 1 ? "Program dosyası oluşturuldu" : "Kullanıcı karar verdi", "MASTER", "-", "-", "-", i % 5 == 0 ? "Uyarı" : "OK", "Kronolojik işlem geçmişinde uzun mesajlar kart içinde 4-5 satıra kadar okunabilir."));
        }
    }

    private void ApplyTheme(bool dark)
    {
        BackColor = dark ? Color.FromArgb(28, 29, 33) : Color.FromArgb(246, 248, 252);
        ForeColor = dark ? Color.White : Color.FromArgb(28, 31, 36);
        _info.BackColor = BackColor;
        _info.ForeColor = ForeColor;
        var theme = PanoTheme.FromParentColor(BackColor, ForeColor);
        _grid.ApplyTheme(theme);
        SmartMenuRenderer.ApplyTo(_tool, theme);
    }

    private sealed record MasterDataRow(string Type, string Code, string Name, string Machine, string Layer, string RefDes, string Quantity, string Status, string Detail);
}

