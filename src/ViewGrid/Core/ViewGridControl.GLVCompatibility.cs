using System.ComponentModel;
using System.Collections;
using ViewGrid.Columns;
using ViewGrid.Presets;
using ViewGrid.Theming;

namespace ViewGrid.Core;

/// <summary>
/// ViewGrid migration presets kept for source compatibility with older MasterData/ViewGrid screens.
/// New code can still use ViewGrid.Presets.ViewGridPresets directly.
/// </summary>
public enum ViewGridPreset
{
    Default,
    ClassicViewGrid,
    LogView,
    LineOverview,
    CompareList,
    AoiFailList,
    DatabaseEditor
}

public sealed class FormatRowEventArgs : EventArgs
{
    public FormatRowEventArgs(int rowIndex, object rowObject, ListViewItem item)
    {
        RowIndex = rowIndex;
        RowObject = rowObject;
        Model = rowObject;
        Item = item;
    }

    public int RowIndex { get; }
    public object RowObject { get; }
    public object Model { get; }
    public ListViewItem Item { get; }
}


public sealed class FormatCellEventArgs : EventArgs
{
    public FormatCellEventArgs(int rowIndex, int columnIndex, object rowObject, ViewGridColumn column, object? cellValue, ListViewItem.ListViewSubItem subItem)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        RowObject = rowObject;
        Model = rowObject;
        Column = column;
        CellValue = cellValue;
        SubItem = subItem;
    }

    public int RowIndex { get; }
    public int ColumnIndex { get; }
    public object RowObject { get; }
    public object Model { get; }
    public ViewGridColumn Column { get; }
    public object? CellValue { get; }
    public ListViewItem.ListViewSubItem SubItem { get; }
}

public partial class ViewGridControl
{
    /// <summary>
    /// ViewGrid compatible row formatting event. Set e.Item.BackColor / ForeColor / Font.
    /// ViewGrid applies BackColor and ForeColor while painting the visible row.
    /// </summary>
    [Category("ViewGrid - Compatibility")]
    public event EventHandler<FormatRowEventArgs>? FormatRow;

    [Category("ViewGrid - Compatibility")]
    public event EventHandler<FormatCellEventArgs>? FormatCell;

    [Category("ViewGrid - Compatibility")]
    [DefaultValue(true)]
    [Description("Right-clicking a body row selects that row before showing BodyContextMenuStrip.")]
    public bool SelectRowOnBodyContextMenu { get; set; } = true;

    [Category("ViewGrid - Compatibility")]
    [DefaultValue(true)]
    [Description("When true ViewGrid shows its built-in body context menu if BodyContextMenuStrip is null.")]
    public bool UseBuiltInBodyContextMenu { get; set; } = true;

    [Category("ViewGrid - Compatibility")]
    [DefaultValue(null)]
    [Description("ViewGrid style body context menu. Header menus remain ViewGrid's filter/sort menu.")]
    public ContextMenuStrip? BodyContextMenuStrip { get; set; }

    [Category("ViewGrid - Compatibility")]
    [DefaultValue(true)]
    public bool ShowImagesOnSubItems { get; set; } = true;

    [Category("ViewGrid - Compatibility")]
    [DefaultValue(true)]
    [Description("True ise Text kolonlarında da ImageGetter/StateImageGetter sonucu ViewGrid gibi metnin solunda çizilir.")]
    public bool ImageGetterAppliesToTextColumns { get; set; } = true;

    [Category("ViewGrid - Compatibility")]
    [DefaultValue(true)]
    public bool UseCellFormatEvents { get; set; } = true;

    [Category("ViewGrid - Compatibility")]
    [DefaultValue(true)]
    public bool UseNotifyPropertyChanged { get; set; } = true;

    [Category("ViewGrid - Compatibility")]
    [DefaultValue(false)]
    public bool OwnerDraw
    {
        get => false;
        set { /* ViewGrid zaten owner-draw çalışır; ViewGrid migration için no-op. */ }
    }

    [Category("ViewGrid - Compatibility")]
    [DefaultValue(false)]
    public bool UseAlternatingBackColors
    {
        get => AlternateRows;
        set => AlternateRows = value;
    }

    [Category("ViewGrid - Compatibility")]
    public Color AlternateRowBackColor
    {
        get => CustomAlternateRowBackColor;
        set => CustomAlternateRowBackColor = value;
    }

    [Category("ViewGrid - Compatibility")]
    [DefaultValue(false)]
    public bool GridLines
    {
        get => ShowGridLines;
        set => ShowGridLines = value;
    }


    [Category("ViewGrid - Compatibility")]
    [DefaultValue(false)]
    public bool HideSelection { get; set; }

    [Browsable(false)]
    public ViewGridColumnCollection AllColumns => Columns;

    public void ApplyPreset(ViewGridPreset preset)
    {
        switch (preset)
        {
            case ViewGridPreset.AoiFailList:
                ViewGridPresets.ApplyAoiFailList(this);
                break;
            case ViewGridPreset.DatabaseEditor:
                ViewGridPresets.ApplyDatabaseEditorDefaults(this);
                break;
            case ViewGridPreset.LogView:
                ApplyClassicViewGridPreset();
                RowHeight = Math.Max(RowHeight, 24);
                AlternateRows = false;
                ShowGridLines = false;
                EmptyListMessage = "Log kaydı yok";
                break;
            case ViewGridPreset.LineOverview:
                ApplyClassicViewGridPreset();
                RowHeight = Math.Max(RowHeight, 34);
                RowColorPreset = ViewGrid.Theming.ViewGridRowColorPreset.StatusPills;
                break;
            case ViewGridPreset.CompareList:
                ApplyClassicViewGridPreset();
                ShowGridLines = true;
                AlternateRows = true;
                FastFilterMenuForHugeLists = true;
                AsyncLoadFullFilterValues = true;
                break;
            case ViewGridPreset.ClassicViewGrid:
            case ViewGridPreset.Default:
            default:
                ApplyClassicViewGridPreset();
                break;
        }

        RefreshView();
        Invalidate();
    }

    public void ApplyPreset(string? presetName)
    {
        if (Enum.TryParse<ViewGridPreset>(presetName ?? string.Empty, true, out var preset))
            ApplyPreset(preset);
        else
            ApplyPreset(ViewGridPreset.ClassicViewGrid);
    }

    private void ApplyClassicViewGridPreset()
    {
        ShowHeader = true;
        ShowGridLines = true;
        AlternateRows = true;
        HotTracking = true;
        MultiSelect = true;
        EnableClipboard = true;
        EnableColumnResize = true;
        AllowColumnReorder = true;
        ShowFilterMenu = true;
        ShowColumnFilterButtons = true;
        FastFilterMenuForHugeLists = true;
        AsyncLoadFullFilterValues = true;
        EnableModernEmptyState = true;
        EnableColumnAutoResizeOnDoubleClick = true;
        AutoResizeIncludeHeader = true;
        UseBuiltInBodyContextMenu = true;
        SelectRowOnBodyContextMenu = true;
        if (RowHeight < 24) RowHeight = 24;
    }

    private void ApplyFormatRowCompatibility(int viewIndex, object row, ref Color rowBack, ref Color rowFore)
    {
        if (!UseCellFormatEvents) return;
        var handler = FormatRow;
        if (handler == null) return;

        var item = new ListViewItem(Convert.ToString(row) ?? string.Empty)
        {
            Tag = row,
            BackColor = rowBack,
            ForeColor = rowFore,
            Font = Font
        };

        try
        {
            Color beforeBack = rowBack;
            Color beforeFore = rowFore;

            handler(this, new FormatRowEventArgs(viewIndex, row, item));

            bool backChanged = item.BackColor != Color.Empty && item.BackColor.ToArgb() != beforeBack.ToArgb();
            bool foreChanged = item.ForeColor != Color.Empty && item.ForeColor.ToArgb() != beforeFore.ToArgb();

            if (item.BackColor != Color.Empty) rowBack = item.BackColor;
            if (item.ForeColor != Color.Empty) rowFore = item.ForeColor;

            if (backChanged && !foreChanged)
                rowFore = EnsureReadableTextOn(rowBack, rowFore);
        }
        catch
        {
            // Formatting code must never break painting.
        }
    }

    private void ApplyFormatCellCompatibility(int viewIndex, object row, ViewGridColumn col, object? value, ref Color cellBack, ref Color cellFore)
    {
        if (!UseCellFormatEvents) return;
        var handler = FormatCell;
        if (handler == null) return;
        int columnIndex = Columns.VisibleColumns.ToList().IndexOf(col);
        if (columnIndex < 0) columnIndex = 0;
        var subItem = new ListViewItem.ListViewSubItem
        {
            Text = Convert.ToString(value) ?? string.Empty,
            Tag = row,
            BackColor = cellBack,
            ForeColor = cellFore,
            Font = Font
        };
        try
        {
            Color beforeBack = cellBack;
            Color beforeFore = cellFore;

            handler(this, new FormatCellEventArgs(viewIndex, columnIndex, row, col, value, subItem));

            bool backChanged = subItem.BackColor != Color.Empty && subItem.BackColor.ToArgb() != beforeBack.ToArgb();
            bool foreChanged = subItem.ForeColor != Color.Empty && subItem.ForeColor.ToArgb() != beforeFore.ToArgb();

            if (subItem.BackColor != Color.Empty) cellBack = subItem.BackColor;
            if (subItem.ForeColor != Color.Empty) cellFore = subItem.ForeColor;

            if (backChanged && !foreChanged)
                cellFore = EnsureReadableTextOn(cellBack, cellFore);
        }
        catch
        {
            // ViewGrid migration formatting code must never break painting.
        }
    }

    /// <summary>
    /// ViewGrid compatible helper. Scrolls the row that owns the given model into view.
    /// Usage: grid.EnsureModelVisible(newLog);
    /// </summary>
    public void EnsureModelVisible(object? model) => EnsureObjectVisible(model);

    /// <summary>ViewGrid compatible alias for EnsureModelVisible.</summary>
    public void EnsureObjectVisible(object? model)
    {
        int index = IndexOfObject(model);
        if (index >= 0) EnsureVisible(index);
    }

    /// <summary>Returns true when the model is currently visible after filtering/sorting/grouping.</summary>
    public bool IsModelVisible(object? model) => IndexOfObject(model) >= 0;

    /// <summary>ViewGrid style selection check by model object.</summary>
    public bool IsSelected(object? model)
    {
        int index = IndexOfObject(model);
        return index >= 0 && (_selectedRow == index || _selectedRows.Contains(index));
    }

    // GetItemCount() is implemented in ViewGridControl.GLVCompat.cs and returns ViewCount.
    // Keep a single implementation to avoid partial-class duplicate member conflicts.

    /// <summary>GLV/ListView compatible column auto-size by zero based visible column index.</summary>
    public void AutoResizeColumn(int columnIndex, ColumnHeaderAutoResizeStyle headerAutoResize)
    {
        var visible = Columns.VisibleColumns.ToList();
        if (columnIndex < 0 || columnIndex >= visible.Count) return;
        AutoResizeColumn(visible[columnIndex], headerAutoResize);
    }

    /// <summary>GLV/ListView compatible column auto-size by column key/name/aspect/text.</summary>
    public void AutoResizeColumn(string columnNameOrAspectName, ColumnHeaderAutoResizeStyle headerAutoResize)
    {
        var col = Columns.ByName(columnNameOrAspectName)
                  ?? Columns.ByAspectName(columnNameOrAspectName)
                  ?? Columns.FirstOrDefault(c => string.Equals(c.Header, columnNameOrAspectName, StringComparison.CurrentCultureIgnoreCase));
        if (col != null) AutoResizeColumn(col, headerAutoResize);
    }

    /// <summary>GLV/ListView compatible column auto-size.</summary>
    public void AutoResizeColumn(ViewGridColumn col, ColumnHeaderAutoResizeStyle headerAutoResize)
    {
        if (col == null || !Columns.Contains(col)) return;

        switch (headerAutoResize)
        {
            case ColumnHeaderAutoResizeStyle.HeaderSize:
                AutoResizeColumnToHeader(col);
                break;
            case ColumnHeaderAutoResizeStyle.ColumnContent:
                AutoResizeColumnToContentOnly(col);
                break;
            case ColumnHeaderAutoResizeStyle.None:
            default:
                AutoResizeColumnToContent(col);
                break;
        }
    }

    /// <summary>GLV/ListView compatible all-column auto-size.</summary>
    public void AutoResizeColumns(ColumnHeaderAutoResizeStyle headerAutoResize)
    {
        foreach (var col in Columns.VisibleColumns.ToList())
        {
            switch (headerAutoResize)
            {
                case ColumnHeaderAutoResizeStyle.HeaderSize:
                    AutoResizeColumnToHeader(col, refresh: false);
                    break;
                case ColumnHeaderAutoResizeStyle.ColumnContent:
                    AutoResizeColumnToContentOnly(col, refresh: false);
                    break;
                case ColumnHeaderAutoResizeStyle.None:
                default:
                    AutoResizeColumnToContent(col, -1, 24, refresh: false);
                    break;
            }
        }
        RefreshView();
        OnColumnLayoutChanged();
    }

    private void AutoResizeColumnToHeader(ViewGridColumn col, bool refresh = true)
    {
        int maxWidth = Math.Max(80, AutoResizeMaxWidth);
        int width = TextRenderer.MeasureText(col.Header ?? string.Empty, Font).Width + 24;
        col.Width = Math.Clamp(width, Math.Max(20, col.MinimumWidth > 0 ? col.MinimumWidth : 40), maxWidth);
        if (refresh) { RefreshView(); OnColumnLayoutChanged(); }
    }

    private void AutoResizeColumnToContentOnly(ViewGridColumn col, bool refresh = true)
    {
        if (col == null || !Columns.Contains(col)) return;
        int sampleRows = Math.Max(1, AutoResizeSampleRows);
        int maxWidth = Math.Max(80, AutoResizeMaxWidth);
        int minWidth = Math.Max(20, col.MinimumWidth > 0 ? col.MinimumWidth : 40);
        int imagePadding = IncludeCellImagesInAutoResizeWidth && ColumnMayDrawCellImage(col)
            ? Math.Max(0, CellImageTextPadding)
            : 0;
        int width = minWidth;
        int rows = Math.Min(ViewCount, sampleRows);

        for (int i = 0; i < rows; i++)
        {
            if (IsGroupRow(i)) continue;
            var row = GetViewRow(i);
            if (row == null) continue;
            string text = Convert.ToString(col.GetValue(row)) ?? string.Empty;
            if (text.Length == 0) continue;
            width = Math.Max(width, TextRenderer.MeasureText(text, Font).Width + 24 + imagePadding);
            if (width >= maxWidth) { width = maxWidth; break; }
        }

        col.Width = Math.Clamp(width, minWidth, maxWidth);
        if (refresh) { RefreshView(); OnColumnLayoutChanged(); }
    }


    /// <summary>ViewGrid compatible alias: replaces current objects and rebuilds visible rows.</summary>
    public void BuildList(IEnumerable rows)
    {
        SetObjects(rows == null ? Array.Empty<object>() : rows.Cast<object>());
        RefreshObjects();
    }

    // SelectObject, SelectObjects and DeselectObject are implemented in ViewGridControl.cs.
    // This file keeps only additional GLV compatibility helpers.

    public void DeselectAll()
    {
        _selectedRows.Clear();
        _selectedRow = -1;
        _selectionAnchorRow = -1;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public object? ModelToItem(object? model) => model;
    public object? ItemToModel(object? item) => item;

    private bool TryShowGLVBodyContextMenu(Point location)
    {
        if (ShowHeader && location.Y < HeaderHeight) return false;

        int rowIndex = HitRow(location.Y);
        if (SelectRowOnBodyContextMenu && rowIndex >= 0 && !IsGroupRow(rowIndex) && !_selectedRows.Contains(rowIndex))
            SelectRow(rowIndex);

        if (BodyContextMenuStrip != null)
        {
            ApplyViewGridThemeToMenu(BodyContextMenuStrip);
            BodyContextMenuStrip.Show(this, location);
            return true;
        }

        return !UseBuiltInBodyContextMenu;
    }
}
