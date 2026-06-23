using System.Data;
using System.Data.Common;
using ViewGrid.Core;

namespace ViewGrid.Data;

/// <summary>
/// Non-visual database binding helper. It lets ViewGrid work like a light DataGridView layer:
/// Fill a DataTable, bind it to ViewGrid, then push Insert/Update/Delete changes with SaveChanges().
/// SQL Server usage is provider-package dependent: create SqlConnection/SqlDataAdapter/SqlCommandBuilder
/// in the host app and pass them here through DbDataAdapter/DbCommandBuilder.
/// </summary>
public sealed class ViewGridDatabaseBindingAdapter
{
    private readonly DbConnection _connection;
    private readonly DbDataAdapter _adapter;
    private readonly DbCommandBuilder _commandBuilder;
    private DataTable? _table;

    public ViewGridDatabaseBindingAdapter(DbConnection connection, DbDataAdapter adapter, DbCommandBuilder commandBuilder)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
    }

    public DataTable Fill()
    {
        _table = new DataTable();
        if (_connection.State != ConnectionState.Open) _connection.Open();
        _adapter.Fill(_table);
        return _table;
    }

    public void BindTo(ViewGridControl grid, bool autoGenerateColumns = true, string primaryKey = "")
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var table = _table ?? Fill();
        grid.BindDatabase(table, _adapter, _commandBuilder, autoGenerateColumns, primaryKey);
    }

    public int SaveChanges()
    {
        if (_table == null) return 0;
        _adapter.InsertCommand ??= _commandBuilder.GetInsertCommand(true);
        _adapter.UpdateCommand ??= _commandBuilder.GetUpdateCommand(true);
        _adapter.DeleteCommand ??= _commandBuilder.GetDeleteCommand(true);
        return _adapter.Update(_table);
    }
}
