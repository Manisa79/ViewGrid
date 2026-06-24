using Taylan.Pano.Columns;
using Taylan.Pano.Core;
using Taylan.Pano.Theming;

namespace Taylan.Pano.TestApp;

public sealed class PanoV276DesignTimeThemeSampleForm : Form
{
    private readonly PanoControl _grid;
    private readonly Label _info;

    public PanoV276DesignTimeThemeSampleForm()
    {
        Text = "Pano v27.6 - Design-Time Theme Sync";
        Width = 1120;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(248, 249, 252);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        _info = new Label
        {
            Dock = DockStyle.Fill,
            Text = "v27.6 Design-Time Theme Sync\nDesigner'da Pano açık/VS uyumlu görünür; runtime tema butonlarıyla Light/Dark/Fluent test edilebilir.",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(32, 32, 32),
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };
        root.Controls.Add(_info, 0, 0);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        root.Controls.Add(toolbar, 0, 1);

        _grid = new PanoControl
        {
            Dock = DockStyle.Fill,
            Name = "panoDesignTimeThemePreview",
            ThemePreset = PanoThemePreset.System,
            EnableDesignTimeThemeSync = true,
            DesignTimeThemePreview = PanoDesignTimeThemePreview.Auto,
            DesignTimeFollowParentTheme = false,
            DesignTimeThemeSyncMenus = true,
            DesignTimeSampleData = true,
            ShowHeader = true,
            ShowGridLines = true,
            AlternateRows = true,
            FullRowSelect = true
        };
        AddButton(toolbar, "Runtime Light", () => _grid.ApplyTheme(PanoTheme.LightTheme()));
        AddButton(toolbar, "Runtime Dark", () => _grid.ApplyTheme(PanoTheme.DarkTheme()));
        AddButton(toolbar, "Runtime Fluent Light", () => _grid.ApplyTheme(PanoTheme.FluentLightTheme()));
        AddButton(toolbar, "Designer Sync Aç", () => { _grid.EnableDesignTimeThemeSync = true; _grid.RefreshThemeFromParent(); });
        AddButton(toolbar, "Parent Theme İzle", () => { _grid.DesignTimeFollowParentTheme = !_grid.DesignTimeFollowParentTheme; _grid.RefreshThemeFromParent(); });

        root.Controls.Add(_grid, 0, 2);

        _grid.Columns.Add(new GLVColumn("Alan", nameof(Row.Name), 220) { FillFreeSpace = true });
        _grid.Columns.Add(new GLVColumn("Durum", nameof(Row.Status), 130));
        _grid.Columns.Add(new GLVColumn("Açıklama", nameof(Row.Description), 420));
        _grid.Columns.Add(new GLVColumn("Oran", nameof(Row.Rate), 90) { TextAlign = ContentAlignment.MiddleRight });

        _grid.SetObjects(new[]
        {
            new Row("Designer açık tema", "OK", "Visual Studio form tasarım zamanı görünümüyle uyumlu açık yüzey.", "100%"),
            new Row("Runtime bağımsız", "Info", "Program çalışırken uygulama teması veya ThemePreset davranışı korunur.", "95%"),
            new Row("Menü teması", "OK", "ContextMenuStrip ve Pano built-in menüleri designer sync ile okunabilir kalır.", "90%"),
            new Row("Parent takip", "Optional", "İstenirse designer Auto teması üst form/panel renginden üretilebilir.", "80%")
        });
    }

    private static void AddButton(Control parent, string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            Width = 145,
            Height = 34,
            Margin = new Padding(0, 6, 8, 6),
            FlatStyle = FlatStyle.System
        };
        button.Click += (_, _) => action();
        parent.Controls.Add(button);
    }

    private sealed record Row(string Name, string Status, string Description, string Rate);
}

