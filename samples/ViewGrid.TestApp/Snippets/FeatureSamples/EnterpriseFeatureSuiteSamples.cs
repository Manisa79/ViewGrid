using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ViewGrid.Columns;
using ViewGrid.Core;
using ViewGrid.Layout;

namespace ViewGrid.FeatureSamples;

public sealed class EnterpriseDemoRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Machine { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int Messages { get; set; }
    public bool Locked { get; set; }
}

public static class EnterpriseFeatureSuiteSamples
{
    public static ViewGridControl CreateEnterpriseDashboardSample()
    {
        ViewGridControl grid = new ViewGridControl
        {
            Dock = DockStyle.Fill,
            ViewMode = ViewGridMode.DashboardCard,
            CardVisualAdornments = true,
            CardDefaultAccentMode = ViewGridCardAccentMode.TopBar,
            UltraFastMode = true,
            FuzzySearchEnabled = true,
            ColumnQualifiedSearchEnabled = true,
            LiveUpdateMode = ViewGridLiveUpdateMode.Off,
            PersistColumnFilters = false,
            UserLayoutKey = "ViewGrid.Samples.EnterpriseDashboard"
        };

        grid.Columns.Add(new ViewGridColumn("Id", nameof(EnterpriseDemoRow.Id), 70)
        {
            Frozen = true,
            Aggregate = ViewGridAggregateMode.Count,
            AutoFilterMode = ViewGridAutoFilterMode.Number,
            SearchAlias = "id"
        });
        grid.Columns.Add(new ViewGridColumn("Ad", nameof(EnterpriseDemoRow.Name), 180)
        {
            FillFreeSpace = true,
            AutoFilterMode = ViewGridAutoFilterMode.Text,
            SearchAlias = "name"
        });
        grid.Columns.Add(new ViewGridColumn("Durum", nameof(EnterpriseDemoRow.Status), 110)
        {
            Kind = ViewGridColumnKind.Badge,
            AutoFilterMode = ViewGridAutoFilterMode.ValueList,
            ShowTopValuesInFilter = true,
            SearchAlias = "status"
        });
        grid.Columns.Add(new ViewGridColumn("Makine", nameof(EnterpriseDemoRow.Machine), 130)
        {
            AutoFilterMode = ViewGridAutoFilterMode.Text,
            SearchAlias = "machine"
        });
        grid.Columns.Add(new ViewGridColumn("İlerleme", nameof(EnterpriseDemoRow.Progress), 90)
        {
            Kind = ViewGridColumnKind.ProgressBar,
            Aggregate = ViewGridAggregateMode.Average,
            AutoFilterMode = ViewGridAutoFilterMode.Number,
            SearchAlias = "progress"
        });

        grid.CardVisualInfoGetter = rowObj =>
        {
            EnterpriseDemoRow row = (EnterpriseDemoRow)rowObj;
            Color color = row.Status switch
            {
                "Yeni" => Color.Goldenrod,
                "Bakılıyor" => Color.DodgerBlue,
                "Tamamlandı" => Color.SeaGreen,
                "Kapalı" => Color.Gray,
                _ => Color.SlateGray
            };

            ViewGridCardVisualInfo info = new ViewGridCardVisualInfo
            {
                AccentColor = color,
                DotColor = color,
                AccentMode = ViewGridCardAccentMode.TopBar
            };

            if (row.Messages > 0)
            {
                info.Badges.Add(new ViewGridCardBadge
                {
                    Text = row.Messages.ToString(),
                    Glyph = ViewGridCardGlyph.Message,
                    BackColor = Color.Orange,
                    Placement = ViewGridCardBadgePlacement.TopRight
                });
            }

            if (row.Locked)
            {
                info.Badges.Add(new ViewGridCardBadge
                {
                    Glyph = ViewGridCardGlyph.Lock,
                    BackColor = Color.DimGray,
                    Placement = ViewGridCardBadgePlacement.TopLeft
                });
            }

            info.Actions.Add(new ViewGridCardAction
            {
                Key = "view",
                Glyph = ViewGridCardGlyph.Info,
                ToolTipText = "Detay aç",
                Placement = ViewGridCardActionPlacement.BottomRight
            });

            info.Actions.Add(new ViewGridCardAction
            {
                Key = "pin",
                Glyph = ViewGridCardGlyph.Pin,
                ToolTipText = "Sabitle",
                Placement = ViewGridCardActionPlacement.BottomRight
            });

            return info;
        };

        grid.CardActionClick += (sender, e) =>
        {
            MessageBox.Show($"{e.Action.Key} -> {((EnterpriseDemoRow)e.RowObject).Name}", "ViewGrid Card Action");
        };

        grid.AddConditionalRule(new ViewGridConditionalRule
        {
            Name = "Gecikmiş / düşük ilerleme",
            Priority = 10,
            Condition = (row, column) => row is EnterpriseDemoRow r && r.Progress < 30,
            BackColor = Color.FromArgb(70, Color.OrangeRed),
            ForeColor = Color.White
        });

        grid.SetObjects(CreateRows());
        return grid;
    }

    public static void ConfigureProfiles(ViewGridControl grid)
    {
        ViewGridLayoutProfile technician = grid.CaptureLayoutProfile("Technician");
        technician.DisplayName = "Teknisyen görünümü";
        grid.ApplyLayoutProfile(technician);
        grid.SaveLayoutProfile("Technician");
    }

    private static List<EnterpriseDemoRow> CreateRows()
    {
        string[] statuses = { "Yeni", "Bakılıyor", "Tamamlandı", "Kapalı" };
        List<EnterpriseDemoRow> rows = new List<EnterpriseDemoRow>();
        for (int i = 1; i <= 250; i++)
        {
            rows.Add(new EnterpriseDemoRow
            {
                Id = i,
                Name = "AOI kayıt " + i,
                Status = statuses[i % statuses.Length],
                Machine = "LINE" + (10 + i % 35) + "REW",
                Progress = (i * 7) % 101,
                Messages = i % 9 == 0 ? i % 5 + 1 : 0,
                Locked = i % 17 == 0
            });
        }
        return rows;
    }
}
