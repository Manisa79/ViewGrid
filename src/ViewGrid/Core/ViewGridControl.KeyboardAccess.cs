using System.ComponentModel;
using ViewGrid.Columns;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    [Category("ViewGrid - Keyboard")]
    [DefaultValue(true)]
    [Description("ViewGrid içindeki kolon, filtre, kolon seçici, auto-size, gruplama ve header checkbox işlemlerini klavye kısayolları ile erişilebilir yapar.")]
    public bool EnableKeyboardAccessibilityShortcuts { get; set; } = true;

    [Category("ViewGrid - Keyboard")]
    [DefaultValue(true)]
    [Description("Ctrl+Left/Ctrl+Right ile aktif kolonu değiştirir. Aktif kolon filtre, edit, auto-size ve header checkbox kısayollarında kullanılır.")]
    public bool KeyboardColumnNavigationEnabled { get; set; } = true;

    [Category("ViewGrid - Keyboard")]
    [DefaultValue(true)]
    [Description("Ctrl+Shift+F ile aktif kolonun filtre penceresini klavyeden açar.")]
    public bool KeyboardColumnFilterShortcutEnabled { get; set; } = true;

    [Category("ViewGrid - Keyboard")]
    [DefaultValue(true)]
    [Description("Ctrl+Shift+L ile kolon seçici penceresini klavyeden açar.")]
    public bool KeyboardColumnChooserShortcutEnabled { get; set; } = true;

    [Category("ViewGrid - Keyboard")]
    [DefaultValue(true)]
    [Description("Ctrl+Shift+R ile aktif kolonu, Ctrl+Shift+Plus ile tüm kolonları içerik genişliğine göre ayarlar.")]
    public bool KeyboardAutoSizeShortcutEnabled { get; set; } = true;

    [Category("ViewGrid - Keyboard")]
    [DefaultValue(true)]
    [Description("Ctrl+Shift+Space ile aktif kolon HeaderCheckBox ise tüm görünen satırları işaretler/kaldırır.")]
    public bool KeyboardHeaderCheckBoxShortcutEnabled { get; set; } = true;

    [Category("ViewGrid - Keyboard")]
    [DefaultValue(true)]
    [Description("Ctrl+G ile aktif kolona göre gruplar, Ctrl+Shift+G ile gruplamayı temizler.")]
    public bool KeyboardGroupingShortcutsEnabled { get; set; } = true;

    [Category("ViewGrid - Keyboard")]
    [DefaultValue(true)]
    [Description("F3 / Shift+F3 ile son incremental/global arama metninde sonraki/önceki eşleşmeye gider.")]
    public bool KeyboardFindNextShortcutEnabled { get; set; } = true;

    private bool HandleViewGridKeyboardAccessibilityShortcut(Keys keyData)
    {
        if (!EnableKeyboardAccessibilityShortcuts) return false;

        if (KeyboardColumnNavigationEnabled)
        {
            if (keyData == (Keys.Control | Keys.Left)) { MoveActiveColumnByKeyboard(-1); return true; }
            if (keyData == (Keys.Control | Keys.Right)) { MoveActiveColumnByKeyboard(1); return true; }
        }

        if (KeyboardColumnFilterShortcutEnabled && keyData == (Keys.Control | Keys.Shift | Keys.F))
        {
            ShowKeyboardFilterForActiveColumn();
            return true;
        }

        if (KeyboardColumnChooserShortcutEnabled && keyData == (Keys.Control | Keys.Shift | Keys.L))
        {
            ShowColumnChooser();
            return true;
        }

        if (KeyboardAutoSizeShortcutEnabled)
        {
            if (keyData == (Keys.Control | Keys.Shift | Keys.R))
            {
                var col = GetKeyboardActiveColumn();
                if (col != null) AutoResizeColumnToContent(col);
                return true;
            }

            if (keyData == (Keys.Control | Keys.Shift | Keys.Oemplus) || keyData == (Keys.Control | Keys.Shift | Keys.Add))
            {
                AutoResizeAllColumnsToContent();
                return true;
            }
        }

        if (KeyboardHeaderCheckBoxShortcutEnabled && keyData == (Keys.Control | Keys.Shift | Keys.Space))
        {
            var col = GetKeyboardActiveColumn();
            if (col != null && col.HeaderCheckBox && !col.HeaderCheckBoxDisabled)
            {
                ToggleHeaderCheckBox(col);
                return true;
            }
        }

        if (KeyboardGroupingShortcutsEnabled)
        {
            if (keyData == (Keys.Control | Keys.G))
            {
                var col = GetKeyboardActiveColumn();
                if (col != null && !string.IsNullOrWhiteSpace(col.AspectName))
                    ToggleGroupBy(col.AspectName);
                return true;
            }

            if (keyData == (Keys.Control | Keys.Shift | Keys.G))
            {
                ClearGrouping();
                return true;
            }
        }

        if (KeyboardFindNextShortcutEnabled)
        {
            if (keyData == Keys.F3)
            {
                FindNextFromKeyboard();
                return true;
            }

            if (keyData == (Keys.Shift | Keys.F3))
            {
                FindPreviousFromKeyboard();
                return true;
            }
        }

        return false;
    }

    private ViewGridColumn? GetKeyboardActiveColumn()
    {
        var visible = Columns.VisibleColumns.ToList();
        if (visible.Count == 0) return null;
        if (_activeColumn != null && visible.Contains(_activeColumn)) return _activeColumn;
        if (_sortColumn != null && visible.Contains(_sortColumn)) return _sortColumn;
        _activeColumn = visible[0];
        return _activeColumn;
    }

    private void MoveActiveColumnByKeyboard(int delta)
    {
        var visible = Columns.VisibleColumns.ToList();
        if (visible.Count == 0) return;
        int index = _activeColumn != null ? visible.IndexOf(_activeColumn) : -1;
        if (index < 0) index = _sortColumn != null ? visible.IndexOf(_sortColumn) : 0;
        index = Math.Clamp(index + delta, 0, visible.Count - 1);
        _activeColumn = visible[index];
        EnsureColumnVisibleByKeyboard(_activeColumn);
        Invalidate();
    }

    private void EnsureColumnVisibleByKeyboard(ViewGridColumn col)
    {
        int left = GetColumnLeft(col);
        int right = left + col.Width;
        int viewportLeft = 0;
        int viewportRight = Math.Max(0, Width - (_vbar.Visible ? _vbar.Width : 0));

        if (left < viewportLeft)
            ScrollHorizontal(Math.Max(0, _scrollX + left - 8));
        else if (right > viewportRight)
            ScrollHorizontal(_scrollX + (right - viewportRight) + 8);
    }

    private void ShowKeyboardFilterForActiveColumn()
    {
        var col = GetKeyboardActiveColumn();
        if (col == null) return;
        int x = Math.Max(0, GetColumnLeft(col));
        ShowConfiguredFilterMenuForColumn(col, new Point(x, HeaderHeight));
    }

    private void FindNextFromKeyboard()
    {
        var text = !string.IsNullOrWhiteSpace(_searchHighlightText) ? _searchHighlightText : _incrementalSearchBuffer;
        if (string.IsNullOrWhiteSpace(text)) return;
        FindNext(text, wrap: true);
    }

    private void FindPreviousFromKeyboard()
    {
        var text = !string.IsNullOrWhiteSpace(_searchHighlightText) ? _searchHighlightText : _incrementalSearchBuffer;
        if (string.IsNullOrWhiteSpace(text)) return;
        FindPrevious(text, wrap: true);
    }
}
