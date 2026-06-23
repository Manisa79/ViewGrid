using ViewGrid.Columns;
using ViewGrid.Filtering;
using ViewGrid.Theming;

namespace ViewGrid.Core;

internal sealed class ViewGridCommandPaletteForm : Form
{
    private readonly ViewGridControl _grid;
    private readonly TextBox _search = new() { BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11f), Dock = DockStyle.Top, Margin = new Padding(10), PlaceholderText = "Komut ara... örn: filtre, kolon, export" };
    private readonly ListBox _list = new() { Dock = DockStyle.Fill, IntegralHeight = false, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10f) };
    private readonly List<PaletteCommand> _commands = new();

    public ViewGridCommandPaletteForm(ViewGridControl grid, string initialSearch)
    {
        _grid = grid;
        Text = "ViewGrid Komut Paleti";
        StartPosition = FormStartPosition.CenterParent;
        Width = 520;
        Height = 430;
        var theme = WindowsThemeService.CurrentTheme();
        ViewGridDialogChrome.ConfigureStandardDialog(this, theme, new Size(420, 320), sizeable: true, iconKind: ViewGridDialogIconKind.Command);
        KeyPreview = true;

        BackColor = theme.PanelBackColor;
        ForeColor = theme.ForeColor;
        _search.BackColor = theme.ControlBackColor;
        _search.ForeColor = theme.ForeColor;
        _list.BackColor = theme.ControlBackColor;
        _list.ForeColor = theme.ForeColor;

        var panel = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 12, 12, 6), BackColor = theme.PanelBackColor, ForeColor = theme.ForeColor };
        panel.Controls.Add(_search);
        Controls.Add(_list);
        Controls.Add(panel);

        BuildCommands();
        _search.TextChanged += (_, _) => RefreshList();
        _search.KeyDown += SearchKeyDown;
        _list.DoubleClick += (_, _) => ExecuteSelected();
        _list.KeyDown += ListKeyDown;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        _search.Text = initialSearch ?? string.Empty;
        RefreshList();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _search.Focus();
        _search.SelectAll();
    }

    private void BuildCommands()
    {
        _commands.Clear();
        Add("Filtreleri temizle", "filter clear temizle", () => _grid.ClearFilters());
        Add("Global filtreyi temizle", "global filter", () => _grid.SetGlobalFilter(string.Empty));
        Add("Sıralamayı temizle", "sort sıralama", () => _grid.ClearSort());
        Add("Kolon seçici", "column chooser kolon görünürlük", () => _grid.ShowColumnChooser());
        Add("Gruplamayı temizle", "group grouping grup", () => _grid.ClearGrouping());
        Add("Tüm grupları aç", "group expand", () => _grid.ExpandAllGroups());
        Add("Tüm grupları kapat", "group collapse", () => _grid.CollapseAllGroups());
        Add("Kullanıcı düzenini kaydet", "layout save profil", () => _grid.SaveUserLayout());
        Add("Kullanıcı düzenini yükle", "layout load profil", () => _grid.LoadUserLayout());
        Add("Kullanıcı düzenini dışa aktar", "layout export profil", () => _grid.ExportUserLayoutWithDialog());
        Add("Kullanıcı düzenini içe aktar", "layout import profil", () => _grid.ImportUserLayoutWithDialog());
        Add("Görünenleri CSV dışa aktar", "export csv", () => _grid.ExportVisibleCsvWithDialog());
        Add("Görünenleri Excel dışa aktar", "export excel", () => _grid.ExportVisibleExcelWithDialog());
        Add("Seçili kayıtları CSV dışa aktar", "export selected csv", () => _grid.ExportSelectedCsvWithDialog());
        Add("Seçimi panoya kopyala", "copy clipboard", () => _grid.CopySelectionToClipboard());
        Add("Seçimi JSON kopyala", "copy json", () => _grid.CopySelectionAsJsonToClipboard());

        foreach (var col in _grid.Columns)
        {
            if (col.PrivateColumn) continue;
            var local = col;
            Add($"Kolonu göster/gizle: {local.Header}", "column visible hide show", () => { local.ApplyRuntimeVisible(!local.Visible); _grid.RefreshView(); });
            if (!string.IsNullOrWhiteSpace(local.AspectName))
            {
                Add($"Bu kolona göre grupla: {local.Header}", "group column", () => _grid.SetGroupBy(local.AspectName));
                Add($"Artan sırala: {local.Header}", "sort asc", () => _grid.SortBy(local, false));
                Add($"Azalan sırala: {local.Header}", "sort desc", () => _grid.SortBy(local, true));
            }
        }
    }

    private void Add(string title, string keywords, Action action) => _commands.Add(new PaletteCommand(title, keywords, action));

    private void RefreshList()
    {
        var q = _search.Text.Trim();
        IEnumerable<PaletteCommand> query = _commands;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var parts = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = query.Where(c => parts.All(p => c.SearchText.Contains(p, StringComparison.CurrentCultureIgnoreCase)));
        }
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var cmd in query.Take(80)) _list.Items.Add(cmd);
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        _list.EndUpdate();
    }

    private void SearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down && _list.Items.Count > 0) { _list.Focus(); _list.SelectedIndex = Math.Min(_list.Items.Count - 1, Math.Max(0, _list.SelectedIndex) + 1); e.SuppressKeyPress = true; }
        else if (e.KeyCode == Keys.Enter) { ExecuteSelected(); e.SuppressKeyPress = true; }
    }

    private void ListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter) { ExecuteSelected(); e.SuppressKeyPress = true; }
        else if (e.KeyCode == Keys.Back) { _search.Focus(); _search.SelectionStart = _search.TextLength; }
    }

    private void ExecuteSelected()
    {
        if (_list.SelectedItem is not PaletteCommand cmd) return;
        Close();
        BeginInvoke(cmd.Action);
    }

    private sealed class PaletteCommand
    {
        public PaletteCommand(string title, string keywords, Action action)
        {
            Title = title;
            SearchText = title + " " + keywords;
            Action = action;
        }
        public string Title { get; }
        public string SearchText { get; }
        public Action Action { get; }
        public override string ToString() => Title;
    }
}
