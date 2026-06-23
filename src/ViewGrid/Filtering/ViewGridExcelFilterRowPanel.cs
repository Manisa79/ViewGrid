using ViewGrid.Columns;
using ViewGrid.Core;

namespace ViewGrid.Filtering;

/// <summary>
/// Excel tarzı başlık-altı filtre satırı. Formlara ek komponent koymadan da kullanılabilir;
/// istenirse gridin üstüne Dock=Top olarak eklenir ve kolonlara göre TextBox üretir.
/// </summary>
public sealed class ViewGridExcelFilterRowPanel : Panel
{
    private readonly ViewGridControl _grid;
    private readonly Dictionary<ViewGridColumn, TextBox> _editors = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 180 };

    public ViewGridExcelFilterRowPanel(ViewGridControl grid)
    {
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        Height = 32;
        Dock = DockStyle.Top;
        Padding = new Padding(0, 3, 0, 3);
        BackColor = SystemColors.Control;
        _timer.Tick += (_,__) => { _timer.Stop(); ApplyFilters(); };
        BuildEditors();
    }

    public void BuildEditors()
    {
        Controls.Clear();
        _editors.Clear();
        int left = 0;
        foreach (var col in _grid.Columns.VisibleColumns)
        {
            var tb = new TextBox
            {
                Left = left,
                Top = 4,
                Width = Math.Max(60, col.Width - 4),
                Height = 24,
                Tag = col,
                PlaceholderText = col.Header
            };
            tb.TextChanged += (_,__) => { _timer.Stop(); _timer.Start(); };
            Controls.Add(tb);
            _editors[col] = tb;
            left += col.Width;
        }
    }

    public void ClearFilterText()
    {
        foreach (var tb in _editors.Values) tb.Text = string.Empty;
        _grid.ClearFilters();
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        int left = 0;
        foreach (var pair in _editors)
        {
            pair.Value.Left = left;
            pair.Value.Width = Math.Max(60, pair.Key.Width - 4);
            left += pair.Key.Width;
        }
    }

    private void ApplyFilters()
    {
        foreach (var pair in _editors)
        {
            var aspect = pair.Key.AspectName;
            if (string.IsNullOrWhiteSpace(aspect)) continue;
            var text = pair.Value.Text;
            if (string.IsNullOrWhiteSpace(text))
                _grid.Filters.Clear(aspect);
            else
                _grid.SetColumnFilter(new ViewGridColumnFilter { AspectName = aspect, Mode = ViewGridFilterMode.Contains, Text = text });
        }
        _grid.RefreshObjects();
    }
}
