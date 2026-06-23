using System.ComponentModel;
using System.Reflection;
using ViewGrid.Columns;
using ViewGrid.Core;

namespace ViewGrid.TestApp;

public sealed class ViewGridColumnDesignerEditorSmokeForm : ViewGridSampleFormBase
{
    private readonly Panel _testPanel = new()
    {
        Dock = DockStyle.Top,
        Height = 112,
        Padding = new Padding(12, 10, 12, 10)
    };

    private readonly Label _testTitle = new()
    {
        Dock = DockStyle.Top,
        Height = 24,
        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
        Text = "Kolon editor test akışı"
    };

    private readonly Label _testSteps = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "1) Audix Kolonu Ekle veya Designer Property Seti uygula.  2) Büyük 'Kolon Editor Aç' düğmesine bas.  3) Açılan ViewGrid kolon editoründe ekle/sil/değiştir ve Tamam ya da İptal ile dön.  4) Sağdaki raporda propertylerin korunup korunmadığını kontrol et."
    };

    private readonly FlowLayoutPanel _testButtons = new()
    {
        Dock = DockStyle.Bottom,
        Height = 38,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false
    };

    private readonly TextBox _report = new()
    {
        Dock = DockStyle.Right,
        Width = 430,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Font = new Font("Consolas", 9F)
    };

    private string _lastSnapshot = string.Empty;

    public ViewGridColumnDesignerEditorSmokeForm()
        : base(
            "ViewGrid Kolon Designer Editor Smoke Test",
            "Gerçek ViewGridColumnCollectionEditor penceresini açar. Tamam/İptal, kolon ekle/sil ve property korunmasını burada hızlıca test edebilirsin.")
    {
        Width = 1280;
        Height = 760;

        Tool.Items.Add(new ToolStripSeparator());
        Tool.Items.Add(new ToolStripButton("Kolon Editor Aç", null, (_, __) => OpenColumnEditor()));
        Tool.Items.Add(new ToolStripButton("Audix Kolonu Ekle", null, (_, __) => AddAudixLikeColumn()));
        Tool.Items.Add(new ToolStripButton("Designer Property Seti", null, (_, __) => ApplyDesignerStressProperties()));
        Tool.Items.Add(new ToolStripButton("Snapshot Al", null, (_, __) => CaptureSnapshot("Manuel snapshot")));
        Tool.Items.Add(new ToolStripButton("Reset", null, (_, __) => ResetColumns()));

        BuildVisibleTestPanel();
        Controls.Add(_report);
        Controls.Add(_testPanel);
        ResetColumns();
    }

    private void BuildVisibleTestPanel()
    {
        _testPanel.BackColor = Program.AppTheme.PanelBackColor;
        _testPanel.ForeColor = Program.AppTheme.ForeColor;
        _testTitle.BackColor = _testPanel.BackColor;
        _testTitle.ForeColor = _testPanel.ForeColor;
        _testSteps.BackColor = _testPanel.BackColor;
        _testSteps.ForeColor = Program.AppTheme.MutedForeColor;

        _testButtons.Controls.Add(CreateActionButton("1  Audix Kolonu Ekle", AddAudixLikeColumn, 150));
        _testButtons.Controls.Add(CreateActionButton("2  Designer Property Seti", ApplyDesignerStressProperties, 180));
        _testButtons.Controls.Add(CreateActionButton("3  Kolon Editor Aç", OpenColumnEditor, 170, bold: true));
        _testButtons.Controls.Add(CreateActionButton("4  Snapshot Al", () => CaptureSnapshot("Manuel snapshot"), 120));
        _testButtons.Controls.Add(CreateActionButton("Reset", ResetColumns, 90));

        _testPanel.Controls.Add(_testSteps);
        _testPanel.Controls.Add(_testButtons);
        _testPanel.Controls.Add(_testTitle);
    }

    private static Button CreateActionButton(string text, Action action, int width, bool bold = false)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 30,
            Margin = new Padding(0, 2, 8, 2),
            FlatStyle = FlatStyle.System,
            Font = bold ? new Font("Segoe UI", 9F, FontStyle.Bold) : new Font("Segoe UI", 9F)
        };
        button.Click += (_, __) => action();
        return button;
    }

    private void ResetColumns()
    {
        ViewGrid.Columns.Clear();
        ViewGrid.CheckBoxes = true;
        ViewGrid.ShowGridLines = true;
        ViewGrid.FullRowSelect = true;
        ViewGrid.ViewMode = ViewGridMode.Details;

        ViewGrid.Columns.Add(new GLVColumn("Person", nameof(DemoRow.Name), 190)
        {
            Name = "glvPerson",
            HeaderCheckBox = true,
            HeaderCheckBoxUpdatesRowCheckBoxes = true,
            ToolTipText = "Designer editor smoke test ana kolonu",
            AllowGroup = true,
            Filterable = true,
            Sortable = true
        });

        ViewGrid.Columns.Add(new GLVColumn("Status", nameof(DemoRow.State), 120)
        {
            Name = "glvStatus",
            Kind = ViewGridColumnKind.Badge,
            AllowGroup = true,
            SearchAlias = "status"
        });

        ViewGrid.Columns.Add(new GLVColumn("Progress", nameof(DemoRow.Progress), 105)
        {
            Name = "glvProgress",
            Kind = ViewGridColumnKind.ProgressBar,
            TextAlign = ContentAlignment.MiddleRight,
            DefaultWidth = 105
        });

        ViewGrid.Columns.Add(new GLVColumn("Notes", nameof(DemoRow.Notes), 300)
        {
            Name = "glvNotes",
            WordWrap = true,
            FillFreeSpace = true,
            AllowCellScroll = true,
            CellScrollMaxVisibleLines = 3,
            CellOverflowDetailsOnDoubleClick = true,
            CardShowCaption = true,
            CardOrder = 20
        });

        ViewGrid.SetObjects(CreateRows(80));
        ViewGrid.RebuildColumns();
        CaptureSnapshot("Reset sonrası başlangıç");
    }

    private void OpenColumnEditor()
    {
        var before = CreateSnapshotText();
        bool changed = InvokeViewGridColumnEditor();
        ViewGrid.RebuildColumns();
        var after = CreateSnapshotText();

        var status = changed
            ? (string.Equals(before, after, StringComparison.Ordinal) ? "Tamam: görünür fark yok" : "Tamam: kolon farkı var")
            : (string.Equals(before, after, StringComparison.Ordinal) ? "İptal/Kapat: değişiklik yok" : "İptal/Kapat: fark algılandı");

        _lastSnapshot = after;
        _report.Text = status + Environment.NewLine +
                       new string('=', 72) + Environment.NewLine +
                       after;
        Info.Text = status + ". Sağdaki raporda Name, AspectName, Kind, Visible, PrivateColumn, Width ve özel propertyler izlenir.";
    }

    private bool InvokeViewGridColumnEditor()
    {
        var editorType = typeof(ViewGridControl).Assembly.GetType("ViewGrid.Design.ViewGridColumnCollectionEditor", throwOnError: true);
        var method = editorType!.GetMethod("EditColumns", BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
            throw new MissingMethodException(editorType.FullName, "EditColumns");

        var columnsProperty = TypeDescriptor.GetProperties(ViewGrid)[nameof(ViewGrid.Columns)];
        var result = method.Invoke(null, new object?[]
        {
            ViewGrid.Columns,
            ViewGrid,
            columnsProperty,
            null,
            this,
            false
        });

        return result is bool value && value;
    }

    private void AddAudixLikeColumn()
    {
        var column = new GLVColumn("Audix Cover", nameof(DemoRow.Description), 150)
        {
            Name = CreateUniqueColumnName("glvAudixCover"),
            Kind = ViewGridColumnKind.Image,
            PrivateColumn = false,
            DefaultWidth = 150,
            HeaderToolTipText = "Audix/media senaryosu için designer property korunma testi",
            ToolTipText = "Image/Media kolon ayarları editor turundan sonra kaybolmamalı",
            CardRole = "Image",
            CardOrder = 0,
            VisibleInCard = true,
            HeaderImageSize = 18,
            AllowColumnChooser = true,
            CanBeHidden = true,
            SearchAlias = "cover",
            ImageAspectName = nameof(DemoRow.Description)
        };

        ViewGrid.Columns.Add(column);
        ViewGrid.RebuildColumns();
        CaptureSnapshot("Audix benzeri kolon eklendi");
    }

    private void ApplyDesignerStressProperties()
    {
        var notes = ViewGrid.Columns.ByName("glvNotes") ?? ViewGrid.Columns.ByAspectName(nameof(DemoRow.Notes));
        if (notes != null)
        {
            notes.PrivateColumn = !notes.PrivateColumn;
            notes.Kind = ViewGridColumnKind.Text;
            notes.WordWrap = true;
            notes.AllowCellScroll = true;
            notes.CellOverflowFade = true;
            notes.CardShowCaption = true;
            notes.CardMaxLines = 4;
            notes.FillFreeSpace = true;
            notes.SearchAlias = "notes";
        }

        var status = ViewGrid.Columns.ByName("glvStatus") ?? ViewGrid.Columns.ByAspectName(nameof(DemoRow.State));
        if (status != null)
        {
            status.Kind = ViewGridColumnKind.Badge;
            status.HeaderBackColor = Color.FromArgb(40, 96, 170);
            status.HeaderForeColor = Color.White;
            status.AllowGroup = true;
            status.Filterable = true;
            status.Sortable = true;
        }

        ViewGrid.RebuildColumns();
        CaptureSnapshot("Designer stress property seti uygulandı");
    }

    private void CaptureSnapshot(string title)
    {
        _lastSnapshot = CreateSnapshotText();
        _report.Text = title + Environment.NewLine +
                       new string('=', 72) + Environment.NewLine +
                       _lastSnapshot;
        Info.Text = title + ". Kolon Editor Aç ile aynı koleksiyonu gerçek editor penceresinde düzenleyebilirsin.";
    }

    private string CreateSnapshotText()
    {
        var lines = new List<string>
        {
            "TEST:",
            "  1) Audix Kolonu Ekle veya Designer Property Seti uygula",
            "  2) Kolon Editor Aç",
            "  3) Açılan editor içinde Ekle/Sil veya property değiştir",
            "  4) Tamam/İptal sonrası bu rapordaki kolon propertylerini karşılaştır",
            string.Empty,
            "Count=" + ViewGrid.Columns.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "Visible=" + string.Join(", ", ViewGrid.Columns.VisibleColumns.Select(c => c.Name)),
            string.Empty
        };

        for (int i = 0; i < ViewGrid.Columns.Count; i++)
        {
            var c = ViewGrid.Columns[i];
            lines.Add($"[{i}] {c.Name}");
            lines.Add($"  Header={c.Header} | Aspect={c.AspectName} | Kind={c.Kind}");
            lines.Add($"  Width={c.Width} Default={c.DefaultWidth} Visible={c.Visible} Private={c.PrivateColumn}");
            lines.Add($"  Fill={c.FillFreeSpace} Sort={c.Sortable} Filter={c.Filterable} Group={c.AllowGroup}");
            lines.Add($"  HeaderCheckBox={c.HeaderCheckBox} WordWrap={c.WordWrap} CellScroll={c.AllowCellScroll}");
            lines.Add($"  CardRole={c.CardRole} CardOrder={c.CardOrder} Caption={c.CardShowCaption} MaxLines={c.CardMaxLines}");
            lines.Add($"  SearchAlias={c.SearchAlias} ImageAspect={c.ImageAspectName} Tooltip={c.ToolTipText}");
            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string CreateUniqueColumnName(string baseName)
    {
        if (ViewGrid.Columns.ByName(baseName) == null)
            return baseName;

        int i = 2;
        while (ViewGrid.Columns.ByName(baseName + i.ToString(System.Globalization.CultureInfo.InvariantCulture)) != null)
            i++;

        return baseName + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
