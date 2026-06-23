// ViewGrid v25 FeatureSamples.cs
// Bu dosya tek başına proje değildir; WinForms projenize ekleyip çağırabileceğiniz örnek kurulum metotları içerir.
// Namespace ve model adlarını kendi projenize göre değiştirebilirsiniz.

using ViewGrid.Core;
using ViewGrid.Columns;
using ViewGrid.Filtering;
using ViewGrid.Exporting;
using ViewGrid.Theming;
using ViewGrid.Analytics;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace ViewGrid.FeatureSamples;

public sealed class CardVisualDemoRow
{
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public int Messages { get; set; }
    public bool HasAttachment { get; set; }
    public bool IsPinned { get; set; }
}

public sealed class DemoRow
{
    public bool Checked { get; set; }
    public string Barcode { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string AssemblyName { get; set; } = "";
    public string State { get; set; } = "";
    public int Score { get; set; }
    public int Progress { get; set; }
    public string Description { get; set; } = "";
    public string Url { get; set; } = "";
}

public static class FeatureSamples
{
    public static List<DemoRow> CreateDemoRows(int count = 1000)
    {
        var states = new[] { "OK", "FAIL", "WAIT", "REPAIR" };
        var machines = new[] { "QX250i", "QX500", "Flex", "Flex Ultra" };
        var rows = new List<DemoRow>(count);
        for (int i = 0; i < count; i++)
        {
            rows.Add(new DemoRow
            {
                Checked = i % 5 == 0,
                Barcode = "BC" + i.ToString("000000"),
                MachineName = machines[i % machines.Length],
                AssemblyName = "BOARD-" + (i % 42).ToString("000"),
                State = states[i % states.Length],
                Score = i % 100,
                Progress = i % 101,
                Description = "Açıklama " + i,
                Url = "https://example.com/" + i
            });
        }
        return rows;
    }

    public static ViewGridControl CreateBasicAspectNameExample()
    {
        var grid = CreateBaseGrid();
        grid.Columns.Add(new ViewGridColumn("Barkod", "Barcode", 160));
        grid.Columns.Add(new ViewGridColumn("Makine", "MachineName", 140));
        grid.Columns.Add(new ViewGridColumn { HeaderText = "Durum", FieldName = "State", Width = 100 });
        grid.SetObjects(CreateDemoRows(100));
        return grid;
    }

    public static ViewGridControl CreateGLVMigrationExample()
    {
        var grid = CreateBasicAspectNameExample();
        grid.ClearObjects();
        grid.AddObjects(CreateDemoRows(25));
        grid.TextFilter = "BC00001";
        grid.CheckAll();
        grid.UncheckAll();
        return grid;
    }

    public static ViewGridControl CreateAspectGetterPutterExample()
    {
        var grid = CreateBaseGrid();
        grid.Columns.Add(new ViewGridColumn("Seç", "Checked", 70)
        {
            Kind = ViewGridColumnKind.CheckBox,
            AspectGetter = row => ((DemoRow)row).Checked,
            AspectPutter = (row, value) => ((DemoRow)row).Checked = Convert.ToBoolean(value)
        });
        grid.Columns.Add(new ViewGridColumn("Özet", "", 260)
        {
            AspectGetter = row =>
            {
                var r = (DemoRow)row;
                return $"{r.Barcode} / {r.MachineName} / {r.State}";
            }
        });
        grid.SetObjects(CreateDemoRows(100));
        return grid;
    }

    public static ViewGridControl CreateFilterHighlightExample(TextBox searchBox)
    {
        var grid = CreateBasicAspectNameExample();
        grid.EnableHighlightEngine = true;
        grid.HighlightGlobalFilterText = true;
        grid.FastFilterMenuForHugeLists = true;
        grid.AsyncLoadFullFilterValues = true;
        grid.TypedFilterSearchesAllRows = true;
        searchBox.TextChanged += (_, _) =>
        {
            grid.SetGlobalFilter(searchBox.Text);
            if (!string.IsNullOrWhiteSpace(searchBox.Text))
                grid.JumpToFirstMatch(searchBox.Text);
        };
        return grid;
    }

    public static ViewGridControl CreateProgressButtonHyperlinkExample()
    {
        var grid = CreateBaseGrid();
        grid.EnableModernProgressBar = true;
        grid.ProgressBarShowText = true;
        grid.ProgressBarUseGradient = true;
        grid.Columns.Add(new ViewGridColumn("Barkod", "Barcode", 150));
        grid.Columns.Add(new ViewGridColumn("Progress", "Progress", 130) { Kind = ViewGridColumnKind.ProgressBar });
        grid.Columns.Add(new ViewGridColumn("Aç", "", 80) { Kind = ViewGridColumnKind.Button, AspectGetter = _ => "Aç" });
        grid.Columns.Add(new ViewGridColumn("Link", "Url", 180) { Kind = ViewGridColumnKind.Hyperlink });
        grid.ButtonClick += (_, e) => MessageBox.Show($"Buton: {((DemoRow)e.RowObject).Barcode}");
        grid.HyperlinkClick += (_, e) => MessageBox.Show($"Link: {((DemoRow)e.RowObject).Url}");
        grid.SetObjects(CreateDemoRows(100));
        return grid;
    }

    public static ViewGridControl CreateEditingExample()
    {
        var grid = CreateBaseGrid();
        grid.EnableCellEditing = true;
        grid.CellEditActivationOnDoubleClick = true;
        grid.CellEditActivationKey = Keys.F2;
        grid.Columns.Add(new ViewGridColumn("Barkod", "Barcode", 150));
        grid.Columns.Add(new ViewGridColumn("Açıklama", "Description", 260)
        {
            Editable = true,
            AspectPutter = (row, value) => ((DemoRow)row).Description = Convert.ToString(value) ?? ""
        });
        grid.CellValueChanged += (_, e) => MessageBox.Show($"Güncellendi: {e.Column.Header} = {e.NewValue}");
        grid.SetObjects(CreateDemoRows(100));
        return grid;
    }

    public static ViewGridControl CreateGroupingExample()
    {
        var grid = CreateBasicAspectNameExample();
        grid.EnableGrouping = true;
        grid.SetGroupBy("MachineName");
        grid.AllowGroupCollapse = true;
        grid.DrawGroupCollapseGlyph = true;
        return grid;
    }

    public static ViewGridControl CreateLayoutManagerExample()
    {
        var grid = CreateBasicAspectNameExample();
        grid.AllowColumnReorder = true;
        grid.EnableColumnAutoResizeOnDoubleClick = true;
        grid.AutoSaveColumnLayout = true;
        grid.ColumnLayoutStorageKey = "ViewGrid.FeatureSamples.Layout";
        grid.SaveColumnLayoutProfile("Default");
        return grid;
    }

    public static ViewGridControl CreateViewModesExample()
    {
        var grid = CreateBasicAspectNameExample();
        grid.TileMinWidth = 220;
        grid.TilePreferredHeight = 90;
        grid.SetViewMode(ViewGridMode.Tile);
        return grid;
    }


    public static ViewGridControl CreateCardVisualAdornmentsExample()
    {
        var grid = CreateBaseGrid();
        grid.SetViewMode(ViewGridMode.DashboardCard);
        grid.TilePreferredWidth = 340;
        grid.TilePreferredHeight = 150;
        grid.CardVisualAdornments = true;
        grid.CardDefaultAccentMode = ViewGridCardAccentMode.TopBar;

        grid.Columns.Add(new ViewGridColumn("Başlık", nameof(CardVisualDemoRow.Title), 220));
        grid.Columns.Add(new ViewGridColumn("Durum", nameof(CardVisualDemoRow.Status), 110));
        grid.Columns.Add(new ViewGridColumn("Öncelik", nameof(CardVisualDemoRow.Priority), 90) { Kind = ViewGridColumnKind.Badge });
        grid.Columns.Add(new ViewGridColumn("Mesaj", nameof(CardVisualDemoRow.Messages), 80));

        grid.CardVisualInfoGetter = row =>
        {
            var r = (CardVisualDemoRow)row;
            Color statusColor = r.Status switch
            {
                "Yeni" => Color.FromArgb(240, 190, 55),
                "Bakılıyor" => Color.FromArgb(54, 158, 245),
                "Tamamlandı" => Color.FromArgb(74, 190, 118),
                _ => Color.FromArgb(112, 122, 135)
            };

            var info = new ViewGridCardVisualInfo
            {
                AccentColor = statusColor,
                DotColor = statusColor,
                AccentMode = ViewGridCardAccentMode.TopBar
            };

            if (r.Messages > 0)
            {
                info.Badges.Add(new ViewGridCardBadge
                {
                    Text = r.Messages.ToString(),
                    Glyph = ViewGridCardGlyph.Message,
                    BackColor = Color.FromArgb(245, 158, 11),
                    Placement = ViewGridCardBadgePlacement.TopRight
                });
            }

            if (r.HasAttachment)
            {
                info.Badges.Add(new ViewGridCardBadge
                {
                    Glyph = ViewGridCardGlyph.Attachment,
                    BackColor = Color.FromArgb(99, 102, 241),
                    Placement = ViewGridCardBadgePlacement.BottomRight
                });
            }

            if (r.IsPinned)
            {
                info.Badges.Add(new ViewGridCardBadge
                {
                    Glyph = ViewGridCardGlyph.Pin,
                    BackColor = Color.FromArgb(100, 116, 139),
                    Placement = ViewGridCardBadgePlacement.TopLeft
                });
            }

            return info;
        };

        grid.SetObjects(new List<CardVisualDemoRow>
        {
            new() { Title = "AOI durdu", Status = "Yeni", Priority = "Acil", Messages = 3, HasAttachment = true, IsPinned = true },
            new() { Title = "Program isteği", Status = "Bakılıyor", Priority = "Normal", Messages = 1 },
            new() { Title = "False call kontrol", Status = "Tamamlandı", Priority = "Düşük", Messages = 0, HasAttachment = true },
            new() { Title = "Kapatılmış kayıt", Status = "Kapalı", Priority = "Arşiv", Messages = 0 }
        });

        return grid;
    }

    public static ViewGridControl CreateThemeExample()
    {
        var grid = CreateBasicAspectNameExample();
        grid.FollowWindowsTheme = true;
        grid.EnableAnimatedSelection = true;
        grid.EnableRoundedCells = true;
        grid.EnableSoftShadows = true;
        grid.ApplyThemePreset(ViewGridThemePreset.Dark);
        return grid;
    }

    public static ViewGridControl CreateRowColorExample()
    {
        var grid = CreateBasicAspectNameExample();
        grid.RowColorAspectName = "State";
        grid.RowColorStrength = 0.18;
        grid.RowBackColorGetter = row => ((DemoRow)row).State == "FAIL" ? Color.FromArgb(50, Color.Red) : null;
        return grid;
    }

    public static void ExportVisibleRows(ViewGridControl grid, string folder)
    {
        Directory.CreateDirectory(folder);
        var rows = grid.GetVisibleObjects();
        ViewGridExporter.SaveCsv(Path.Combine(folder, "viewgrid.csv"), grid.Columns, rows);
        ViewGridExporter.SaveJson(Path.Combine(folder, "viewgrid.json"), grid.Columns, rows);
        ViewGridExporter.SaveExcelWorkbook(Path.Combine(folder, "viewgrid.xlsx"), grid.Columns, rows);
        ViewGridExporter.SavePdf(Path.Combine(folder, "viewgrid.pdf"), grid.Columns, rows, "ViewGrid Export");
    }

    public static void ConfigurePrint(ViewGridControl grid)
    {
        grid.PrintTitle = "ViewGrid v25 Print";
        grid.PrintOnlyVisibleColumns = true;
        grid.PrintSelectedRowsOnly = false;
        grid.PrintMaxRows = 5000;
        grid.PrintFitToPageWidth = true;
        grid.PrintShowGrid = true;
        grid.PrintZebraRows = true;
    }

    public static void ShowMiniAnalytics(ViewGridControl grid)
    {
        var stateColumn = grid.Columns["State"];
        if (stateColumn == null) return;
        var analytics = ViewGridColumnAnalytics.From(stateColumn, grid.GetVisibleObjects());
        MessageBox.Show($"Satır: {analytics.RowCount}\nDistinct: {analytics.DistinctCount}\nBoş: {analytics.BlankCount}");
    }

    private static ViewGridControl CreateBaseGrid()
    {
        return new ViewGridControl
        {
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            MultiSelect = true,
            ShowHeader = true,
            ShowGridLines = true,
            EmptyListMessage = "Kayıt bulunamadı",
            EnableModernEmptyState = true,
            EnableIncrementalSearch = true,
            EnableClipboard = true
        };
    }
}
