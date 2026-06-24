using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Layout;

namespace Taylan.Pano.FeatureSamples;

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
    public static PanoControl CreateEnterpriseDashboardSample()
    {
        PanoControl grid = new PanoControl
        {
            Dock = DockStyle.Fill,
            ViewMode = PanoViewMode.DashboardCard,
            CardVisualAdornments = true,
            CardDefaultAccentMode = PanoCardAccentMode.TopBar,
            UltraFastMode = true,
            FuzzySearchEnabled = true,
            ColumnQualifiedSearchEnabled = true,
            LiveUpdateMode = PanoLiveUpdateMode.Off,
            PersistColumnFilters = false,
            UserLayoutKey = "Pano.Samples.EnterpriseDashboard"
        };

        grid.Columns.Add(new PanoColumn("Id", nameof(EnterpriseDemoRow.Id), 70)
        {
            Frozen = true,
            Aggregate = PanoAggregateMode.Count,
            AutoFilterMode = PanoAutoFilterMode.Number,
            SearchAlias = "id"
        });
        grid.Columns.Add(new PanoColumn("Ad", nameof(EnterpriseDemoRow.Name), 180)
        {
            FillFreeSpace = true,
            AutoFilterMode = PanoAutoFilterMode.Text,
            SearchAlias = "name"
        });
        grid.Columns.Add(new PanoColumn("Durum", nameof(EnterpriseDemoRow.Status), 110)
        {
            Kind = PanoColumnKind.Badge,
            AutoFilterMode = PanoAutoFilterMode.ValueList,
            ShowTopValuesInFilter = true,
            SearchAlias = "status"
        });
        grid.Columns.Add(new PanoColumn("Makine", nameof(EnterpriseDemoRow.Machine), 130)
        {
            AutoFilterMode = PanoAutoFilterMode.Text,
            SearchAlias = "machine"
        });
        grid.Columns.Add(new PanoColumn("İlerleme", nameof(EnterpriseDemoRow.Progress), 90)
        {
            Kind = PanoColumnKind.ProgressBar,
            Aggregate = PanoAggregateMode.Average,
            AutoFilterMode = PanoAutoFilterMode.Number,
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

            PanoCardVisualInfo info = new PanoCardVisualInfo
            {
                AccentColor = color,
                DotColor = color,
                AccentMode = PanoCardAccentMode.TopBar
            };

            if (row.Messages > 0)
            {
                info.Badges.Add(new PanoCardBadge
                {
                    Text = row.Messages.ToString(),
                    Glyph = PanoCardGlyph.Message,
                    BackColor = Color.Orange,
                    Placement = PanoCardBadgePlacement.TopRight
                });
            }

            if (row.Locked)
            {
                info.Badges.Add(new PanoCardBadge
                {
                    Glyph = PanoCardGlyph.Lock,
                    BackColor = Color.DimGray,
                    Placement = PanoCardBadgePlacement.TopLeft
                });
            }

            info.Actions.Add(new PanoCardAction
            {
                Key = "view",
                Glyph = PanoCardGlyph.Info,
                ToolTipText = "Detay aç",
                Placement = PanoCardActionPlacement.BottomRight
            });

            info.Actions.Add(new PanoCardAction
            {
                Key = "pin",
                Glyph = PanoCardGlyph.Pin,
                ToolTipText = "Sabitle",
                Placement = PanoCardActionPlacement.BottomRight
            });

            return info;
        };

        grid.CardActionClick += (sender, e) =>
        {
            MessageBox.Show($"{e.Action.Key} -> {((EnterpriseDemoRow)e.RowObject).Name}", "Pano Card Action");
        };

        grid.AddConditionalRule(new PanoConditionalRule
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

    public static void ConfigureProfiles(PanoControl grid)
    {
        PanoLayoutProfile technician = grid.CaptureLayoutProfile("Technician");
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

