using System.Data;
using System.Data.Common;
using ViewGrid.Virtualization;

namespace ViewGrid.Data;

public sealed class DatabaseAdapter
{
    private readonly DbConnection _connection;
    private readonly DbDataAdapter _adapter;
    private readonly DbCommandBuilder _builder;
    private DataTable? _table;

    public DatabaseAdapter(DbConnection connection, DbDataAdapter adapter, DbCommandBuilder builder)
    {
        _connection = connection; _adapter = adapter; _builder = builder;
    }

    public DataTable FillTable()
    {
        _table = new DataTable();
        if (_connection.State != ConnectionState.Open) _connection.Open();
        _adapter.Fill(_table);
        return _table;
    }

    public int SaveChanges()
    {
        if (_table == null) return 0;
        _adapter.InsertCommand ??= _builder.GetInsertCommand(true);
        _adapter.UpdateCommand ??= _builder.GetUpdateCommand(true);
        _adapter.DeleteCommand ??= _builder.GetDeleteCommand(true);
        return _adapter.Update(_table);
    }
}

public static class DataTableRowProviderFactory
{
    public static IRowProvider FromDataTable(DataTable table)
        => new DataTableRowProvider(table);
}

public sealed class DataTableRowProvider : IRowProvider
{
    private readonly DataTable _table;
    public DataTableRowProvider(DataTable table) => _table = table;
    public int Count => _table.Rows.Cast<DataRow>().Count(r => r.RowState != DataRowState.Deleted);
    public object? GetRow(int index)
    {
        int visible = 0;
        foreach (DataRow row in _table.Rows)
        {
            if (row.RowState == DataRowState.Deleted) continue;
            if (visible == index) return row;
            visible++;
        }
        return null;
    }
}
