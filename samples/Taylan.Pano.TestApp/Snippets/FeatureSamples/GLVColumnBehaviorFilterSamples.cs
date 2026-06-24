using Taylan.Pano;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Filtering;

namespace Taylan.Pano.FeatureSamples;

public static class GLVColumnBehaviorFilterSamples
{
    public sealed class DemoRow
    {
        public bool Select { get; set; }
        public string Barcode { get; set; } = "";
        public string Machine { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime TestTime { get; set; }
        public int FailCount { get; set; }
    }

    public static FastPanoControl CreateDefaultFilterButtonSample()
    {
        var grid = new FastPanoControl
        {
            Dock = DockStyle.Fill,
            ShowColumnFilterButtons = true,      // default zaten true
            FilterMenuMode = PanoFilterMenuMode.PopupMenu,
            UseEmbeddedHeaderFilterMenu = true,
            ShowFilterMenu = true
        };

        grid.Columns.Add(new GLVColumn("Seç", "Select", 54)
        {
            Kind = PanoColumnKind.CheckBox,
            HeaderCheckBox = true,
            AllowFilter = false,
            AllowSort = false,
            AllowResize = false
        });

        grid.Columns.Add(new GLVColumn("Barcode", "Barcode", 170)
        {
            AllowFilter = true,
            AllowSort = true,
            AllowResize = true,
            HeaderImage = SystemIcons.Information.ToBitmap(),
            HeaderImageSize = 16
        });

        grid.Columns.Add(new GLVColumn("Machine", "Machine", 120)
        {
            AllowFilter = true,
            AllowGroup = true
        });

        grid.Columns.Add(new GLVColumn("Status", "Status", 120)
        {
            AllowFilter = true,
            HeaderForeColor = Color.White,
            HeaderBackColor = Color.FromArgb(64, 124, 214)
        });

        grid.Columns.Add(new GLVColumn("Dikey\nBaşlık", "FailCount", 72)
        {
            HeaderTextVertical = true,
            HeaderTextAngle = -90,
            TextAlign = ContentAlignment.MiddleRight,
            AllowFilter = false
        });

        grid.Columns.Add(new GLVColumn("Test Time", "TestTime", 155)
        {
            AllowFilter = true,
            AllowSort = true
        });

        grid.SetObjects(CreateRows(250));
        return grid;
    }

    public static FastPanoControl CreateDesignerLikeConfigurationSample()
    {
        var grid = CreateDefaultFilterButtonSample();

        // Grid genelinde kapatmak istersek:
        // grid.ShowColumnFilterButtons = false;

        // Sadece belirli kolonda filtre kapatma/açma:
        var failColumn = grid.Columns.FirstOrDefault(c => c.AspectName == "FailCount");
        if (failColumn != null)
        {
            failColumn.AllowFilter = false;
            failColumn.AllowSort = true;
            failColumn.AllowResize = true;
        }

        // Kodla hızlı filtre uygulama:
        grid.SetColumnFilter(new PanoColumnFilter
        {
            AspectName = "Status",
            Mode = PanoFilterMode.ValueList,
            SelectedValues = new HashSet<string>(new[] { "FAIL", "REVIEW" }, StringComparer.CurrentCultureIgnoreCase),
            Enabled = true
        });

        return grid;
    }

    private static List<DemoRow> CreateRows(int count)
    {
        string[] machines = { "QX250i", "Flex", "Flex Ultra", "QX500" };
        string[] statuses = { "PASS", "FAIL", "REVIEW", "WAIT" };
        var rnd = new Random(42);
        return Enumerable.Range(1, count).Select(i => new DemoRow
        {
            Select = i % 7 == 0,
            Barcode = $"BC{i:000000}",
            Machine = machines[i % machines.Length],
            Status = statuses[i % statuses.Length],
            TestTime = DateTime.Today.AddMinutes(-i * 3),
            FailCount = rnd.Next(0, 12)
        }).ToList();
    }
}

