using Taylan.Pano;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Filtering;

namespace Taylan.Pano.FeatureSamples;

/// <summary>
/// v25.18: eski listeden GLV'ye taşınan kolon başlığı özellikleri.
/// - Header checkbox
/// - 90 derece dik başlık yazısı
/// - Başlık yazı/arka plan rengi
/// - Başlık ikonu
/// - GLVColumn ile temiz kullanım
/// </summary>
public static class GLVHeaderVisualSamples
{
    public static FastPanoControl CreateHeaderFeatureSample()
    {
        var grid = new FastPanoControl
        {
            Dock = DockStyle.Fill,
            RowHeight = 30,
            ShowFilterMenu = true,
            FilterMenuMode = Taylan.Pano.Filtering.PanoFilterMenuMode.Both
        };

        var jobColumn = new GLVColumn("Jobs?", nameof(PersonRow.Selected), 42)
        {
            Kind = PanoColumnKind.CheckBox,
            HeaderCheckBox = true,
            HeaderTextVertical = true,
            HeaderForeColor = Color.Black,
            HeaderBackColor = Color.FromArgb(245, 248, 255),
            Filterable = false,
            Sortable = false
        };

        var personColumn = new GLVColumn("Person", nameof(PersonRow.Person), 210)
        {
            HeaderImage = CreateHeaderIcon(Color.FromArgb(240, 180, 60)),
            HeaderForeColor = Color.FromArgb(40, 40, 40),
            ImageGetter = row => row is PersonRow p ? CreateHeaderIcon(p.AccentColor) : null
        };

        var skillColumn = new GLVColumn("Cooking\nSkill", nameof(PersonRow.CookingSkill), 80)
        {
            HeaderTextVertical = true,
            HeaderForeColor = Color.Green,
            HeaderBackColor = Color.FromArgb(220, 240, 255),
            TextAlign = ContentAlignment.MiddleRight
        };

        var birthColumn = new GLVColumn("Year of birth", nameof(PersonRow.YearOfBirth), 110);
        var commentColumn = new GLVColumn("Comments", nameof(PersonRow.Comment), 260)
        {
            HeaderForeColor = Color.Red,
            HeaderImage = CreateHeaderIcon(Color.Red),
            HeaderImageAlign = ContentAlignment.MiddleRight,
            HeaderImageBeforeText = false
        };

        grid.Columns.Add(jobColumn);
        grid.Columns.Add(personColumn);
        grid.Columns.Add(skillColumn);
        grid.Columns.Add(birthColumn);
        grid.Columns.Add(commentColumn);

        grid.SetObjects(BuildRows());
        return grid;
    }

    private static List<PersonRow> BuildRows() => new()
    {
        new PersonRow(true, "🎵 WILHELM FRAT", 21, 1984, "Aggressive, belligerent, pacifically challenged", Color.FromArgb(245, 179, 66)),
        new PersonRow(true, "⭐ ALANA RODERICK", 21, 1974, "Beautiful, exquisite", Color.FromArgb(255, 214, 75)),
        new PersonRow(true, "👩 FRANK PRICE", 30, 1965, "Competitive, spirited, timidically challenged", Color.FromArgb(82, 180, 118)),
        new PersonRow(false, "⭐ ERIC", 1, 1966, "Diminutive, vertically challenged", Color.FromArgb(255, 214, 75)),
        new PersonRow(false, "👩 NICOLA SCOTTS", 42, 1965, "Wise, fun, lovely", Color.FromArgb(82, 180, 118)),
    };

    private static Image CreateHeaderIcon(Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var b = new SolidBrush(color);
        using var p = new Pen(Color.FromArgb(90, Color.Black));
        g.FillEllipse(b, 2, 2, 12, 12);
        g.DrawEllipse(p, 2, 2, 12, 12);
        return bmp;
    }

    private sealed record PersonRow(bool Selected, string Person, int CookingSkill, int YearOfBirth, string Comment, Color AccentColor);
}

