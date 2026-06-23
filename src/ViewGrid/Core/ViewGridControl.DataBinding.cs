using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using ViewGrid.Columns;
using ViewGrid.Editing;
using ViewGrid.Virtualization;
using ViewGrid.Data;

namespace ViewGrid.Core;

public partial class ViewGridControl
{
    private object? _dataSource;
    private DataTable? _boundDataTable;
    private DbDataAdapter? _boundDataAdapter;
    private DbCommandBuilder? _boundCommandBuilder;

    [Category("ViewGrid - Database")]
    [DefaultValue(null)]
    public object? DataSource
    {
        get => _dataSource;
        set
        {
            _dataSource = value;
            BindDataSource(value);
        }
    }

    [Category("ViewGrid - Database")]
    [DefaultValue(true)]
    public bool AutoGenerateColumns { get; set; } = true;

    [Category("ViewGrid - Database")]
    [DefaultValue("")]
    public string PrimaryKey { get; set; } = string.Empty;

    [Category("ViewGrid - Database")]
    [DefaultValue(true)]
    public bool AllowUserToAddRows { get; set; } = true;

    [Category("ViewGrid - Database")]
    [DefaultValue(true)]
    public bool AllowUserToDeleteRows { get; set; } = true;

    [Browsable(false)]
    public DataTable? BoundDataTable => _boundDataTable;

    [Browsable(false)]
    public bool HasChanges => _boundDataTable?.GetChanges() != null;

    [Browsable(false)]
    public int ChangedRowCount => _boundDataTable?.GetChanges()?.Rows.Count ?? 0;

    public void BindDataTable(DataTable table, bool autoGenerateColumns = true, string primaryKey = "")
    {
        _boundDataTable = table ?? throw new ArgumentNullException(nameof(table));
        _mode = ViewGridDataMode.DataTable;
        _dataSource = table;
        AutoGenerateColumns = autoGenerateColumns;
        if (!string.IsNullOrWhiteSpace(primaryKey)) PrimaryKey = primaryKey;
        if (Columns.Count == 0)
        {
            GenerateColumnsFromDataTable(table);
        }
        else if (AutoGenerateColumns)
        {
            MergeColumnsFromDataTable(table);
        }
        SetProviderCore(DataTableRowProviderFactory.FromDataTable(table));
        InvalidateDataCaches();
        BuildViewIndex();
    }

    public void BindDatabase(DataTable table, DbDataAdapter adapter, DbCommandBuilder? commandBuilder = null, bool autoGenerateColumns = true, string primaryKey = "")
    {
        _boundDataAdapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _boundCommandBuilder = commandBuilder;
        BindDataTable(table, autoGenerateColumns, primaryKey);
    }

    public DataRow AddNewRow(params (string Column, object? Value)[] values)
    {
        if (!AllowUserToAddRows) throw new InvalidOperationException("AllowUserToAddRows=false.");
        if (_boundDataTable == null) throw new InvalidOperationException("DataTable bağlı değil.");
        var row = _boundDataTable.NewRow();
        foreach (var (column, value) in values)
        {
            if (_boundDataTable.Columns.Contains(column)) row[column] = value ?? DBNull.Value;
        }
        _boundDataTable.Rows.Add(row);
        RefreshObjects();
        return row;
    }

    public int DeleteSelectedRows()
    {
        if (!AllowUserToDeleteRows) return 0;
        int deleted = 0;
        foreach (var row in SelectedObjects.OfType<DataRow>().ToList())
        {
            if (row.RowState != DataRowState.Deleted)
            {
                row.Delete();
                deleted++;
            }
        }
        if (deleted > 0) RefreshObjects();
        return deleted;
    }

    public void RejectChanges()
    {
        _boundDataTable?.RejectChanges();
        RefreshObjects();
    }

    public int SaveChanges()
    {
        if (_boundDataTable == null) return 0;
        if (_boundDataAdapter == null)
        {
            _boundDataTable.AcceptChanges();
            RefreshObjects();
            return 0;
        }

        if (_boundCommandBuilder != null)
        {
            _boundDataAdapter.InsertCommand ??= _boundCommandBuilder.GetInsertCommand(true);
            _boundDataAdapter.UpdateCommand ??= _boundCommandBuilder.GetUpdateCommand(true);
            _boundDataAdapter.DeleteCommand ??= _boundCommandBuilder.GetDeleteCommand(true);
        }

        int affected = _boundDataAdapter.Update(_boundDataTable);
        RefreshObjects();
        return affected;
    }

    private void BindDataSource(object? source)
    {
        switch (source)
        {
            case null:
                _boundDataTable = null;
                SetObjects(Array.Empty<object>());
                break;
            case DataTable table:
                BindDataTable(table, AutoGenerateColumns, PrimaryKey);
                break;
            case DataView view:
                BindDataTable(view.ToTable(), AutoGenerateColumns, PrimaryKey);
                break;
            case BindingSource bs when bs.DataSource is DataTable table:
                BindDataTable(table, AutoGenerateColumns, PrimaryKey);
                break;
            case IEnumerable rows:
                _boundDataTable = null;
                SetObjects(rows);
                break;
            default:
                _boundDataTable = null;
                SetObjects(new[] { source });
                break;
        }
    }

    private void GenerateColumnsFromDataTable(DataTable table)
    {
        Columns.Clear();
        foreach (DataColumn dc in table.Columns)
        {
            Columns.Add(CreateColumnFromDataColumn(dc));
        }
    }

    /// <summary>
    /// Runtime DataSource bağlanırken designer'da ayarlanmış kolonları korur.
    /// Eski davranış Columns.Clear() yaptığı için Visible=false olan kolonlar runtime'da
    /// tekrar görünüyordu. ViewGrid/ListView benzeri davranışta mevcut kolonlar korunur,
    /// sadece DataTable'da olup designer'da hiç tanımlanmamış kolonlar sona eklenir.
    /// </summary>
    private void MergeColumnsFromDataTable(DataTable table)
    {
        foreach (DataColumn dc in table.Columns)
        {
            var existing = Columns.FirstOrDefault(c =>
                string.Equals(c.AspectName, dc.ColumnName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Header, dc.ColumnName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                // Kullanıcının designer'da verdiği Visible, Width, Text, DisplayIndex gibi
                // görsel tercihlerini kesinlikle ezmeyelim; sadece veri/edit metadata güncellenir.
                existing.Kind = existing.Kind == ViewGridColumnKind.Text ? GetKindForDataColumn(dc) : existing.Kind;
                existing.EditorType = existing.EditorType == ViewGridCellEditorKind.Auto ? GetEditorForDataColumn(dc) : existing.EditorType;
                existing.ReadOnly = dc.ReadOnly || dc.AutoIncrement || existing.ReadOnly;
                existing.Required = !dc.AllowDBNull;
                existing.MaxLength = dc.MaxLength > 0 ? dc.MaxLength : existing.MaxLength;
                if (string.IsNullOrWhiteSpace(existing.AspectName)) existing.AspectName = dc.ColumnName;
                continue;
            }

            Columns.Add(CreateColumnFromDataColumn(dc));
        }
    }

    private static ViewGridColumn CreateColumnFromDataColumn(DataColumn dc)
    {
        var kind = GetKindForDataColumn(dc);
        var col = new ViewGridColumn(dc.Caption == dc.ColumnName ? dc.ColumnName : dc.Caption, dc.ColumnName, GetDefaultWidth(dc.DataType))
        {
            Kind = kind,
            EditorType = GetEditorForDataColumn(dc),
            Editable = !dc.ReadOnly && !dc.AutoIncrement,
            ReadOnly = dc.ReadOnly || dc.AutoIncrement,
            Required = !dc.AllowDBNull,
            MaxLength = dc.MaxLength > 0 ? dc.MaxLength : 0,
            TextAlign = IsNumericType(dc.DataType) ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft
        };
        if (kind == ViewGridColumnKind.CheckBox) col.Width = 80;
        return col;
    }

    private static ViewGridColumnKind GetKindForDataColumn(DataColumn dc)
    {
        var type = Nullable.GetUnderlyingType(dc.DataType) ?? dc.DataType;
        if (type == typeof(bool)) return ViewGridColumnKind.CheckBox;
        if (type == typeof(DateTime)) return ViewGridColumnKind.Date;
        if (IsNumericType(type)) return ViewGridColumnKind.Numeric;
        return ViewGridColumnKind.Text;
    }

    private static ViewGridCellEditorKind GetEditorForDataColumn(DataColumn dc)
    {
        var type = Nullable.GetUnderlyingType(dc.DataType) ?? dc.DataType;
        if (type == typeof(bool)) return ViewGridCellEditorKind.CheckBox;
        if (type == typeof(DateTime)) return ViewGridCellEditorKind.DateTime;
        if (IsNumericType(type)) return ViewGridCellEditorKind.Numeric;
        return ViewGridCellEditorKind.TextBox;
    }

    private static bool IsNumericType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) ||
               type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }

    private static int GetDefaultWidth(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(bool)) return 80;
        if (type == typeof(DateTime)) return 150;
        if (IsNumericType(type)) return 100;
        return 160;
    }
}
