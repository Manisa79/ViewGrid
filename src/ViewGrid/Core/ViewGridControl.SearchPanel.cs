using System.ComponentModel;
using ViewGrid.Notifications;
using ViewGrid.Theming;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    private Form? _modernSearchPanel;

    [Category("ViewGrid - Search"), DefaultValue(true)]
    [Description("Ctrl+F ile açılabilen modern arama panelini etkinleştirir.")]
    public bool EnableModernSearchPanel { get; set; } = true;

    [Category("ViewGrid - Search"), DefaultValue(false)]
    [Description("Modern arama panelinde arama metnini global filtre olarak uygular.")]
    public bool SearchPanelCanFilterResults { get; set; } = false;

    public void ShowModernSearchPanel()
    {
        if (!EnableModernSearchPanel) return;
        if (_modernSearchPanel is { IsDisposed: false })
        {
            _modernSearchPanel.Activate();
            return;
        }

        var form = new Form
        {
            Text = ViewGrid.Localization.ViewGridText.T("Find"),
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(460, 170),
            MinimumSize = new Size(420, 150),
            Font = Font,
            KeyPreview = true
        };
        ViewGridDialogChrome.ConfigureStandardDialog(form, _theme, new Size(420, 150), sizeable: true, iconKind: ViewGridDialogIconKind.Search);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 3, ColumnCount = 3 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var search = new TextBox { Dock = DockStyle.Fill, PlaceholderText = ViewGrid.Localization.ViewGridText.SearchPlaceholder };
        var next = new Button { Text = "Sonraki", Dock = DockStyle.Fill };
        var prev = new Button { Text = "Önceki", Dock = DockStyle.Fill };
        var filter = new CheckBox { Text = "Sonuçları filtrele", Dock = DockStyle.Left, AutoSize = true, Checked = SearchPanelCanFilterResults };
        var clear = new Button { Text = "Temizle", Width = 90, Anchor = AnchorStyles.Right };
        var status = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };

        root.Controls.Add(search, 0, 0);
        root.Controls.Add(prev, 1, 0);
        root.Controls.Add(next, 2, 0);
        root.Controls.Add(filter, 0, 1);
        root.Controls.Add(clear, 2, 1);
        root.Controls.Add(status, 0, 2);
        root.SetColumnSpan(status, 3);
        form.Controls.Add(root);
        ViewGridDialogThemeApplier.Apply(form, _theme);

        void ApplySearch(bool forward)
        {
            string text = search.Text.Trim();
            if (filter.Checked)
            {
                _filters.GlobalText = text;
                _searchHighlightText = text;
                BuildViewIndex();
                status.Text = string.IsNullOrWhiteSpace(text) ? "Filtre temizlendi." : $"Filtre uygulandı: {text}";
                return;
            }

            bool found = forward ? FindNext(text, true) : FindPrevious(text, true);
            status.Text = found ? "Eşleşme bulundu." : "Eşleşme bulunamadı.";
        }

        next.Click += (_, _) => ApplySearch(true);
        prev.Click += (_, _) => ApplySearch(false);
        clear.Click += (_, _) => { search.Clear(); ClearSearchHighlight(); status.Text = "Arama temizlendi."; };
        search.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; ApplySearch(true); } };
        form.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) form.Close(); };
        form.FormClosed += (_, _) => _modernSearchPanel = null;
        _modernSearchPanel = form;
        form.Show(FindForm());
        search.Focus();
    }

    public void ShowToast(string message, ViewGridToastKind kind = ViewGridToastKind.Info, int milliseconds = 3500)
        => ViewGridToast.Show(this, message, kind, _theme, milliseconds);
}
