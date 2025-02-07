using System.Data;
using System.Transactions;

namespace DapperContext.Interfaces;

public interface IDbContext
{

    IDbConnection Connect();

    #region Generic data operations

    Task<int> Execute(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = 0);
    Task<T?> ExecuteScalar<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = 0);
    Task<T?> QueryFirst<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout=null);
    Task<IEnumerable<T>> Query<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = 0);
    Task<IEnumerable<T>> QueryProcedure<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = 0);
    Task<T?> QueryProcedureScalar<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = 0) where T : struct;

    Task<int> ExecuteProcedure(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = 0);

    Task<IEnumerable<T>> QueryTable<T>(string tableName,IDbTransaction? transaction = null, int? commandTimeout = 0);

    #endregion


    #region CRUD

    Task<bool> Delete(int id, string table, IDbTransaction? transaction = null);
    Task<TEntity?> GetById<TEntity>(int id, string table);
    Task<IEnumerable<TEntity>> GetAll<TEntity>(string table, int? commandTimeout = null);

    #endregion

    #region DataTable and Tables
    string PrefixForTempTables { get; init; }
    bool TableExists(string tableName, string? schema = null, IDbConnection? connection = null);
    Task<string> InitializeTempTable<T>(IEnumerable<T> items, string createTableSqlBody, int? bulkTimeout = 0);

    DataTable InitializeDatatable<TEntity>(IEnumerable<TEntity> items);
    string GetNewTempTableName(IDbConnection connection, string tableNamePrefix);
    string GetNewTempTableName(string tableNamePrefix);
    Task<(int Updated, int Added)> UpdateOldAndAddNew<T>(List<T> items, string mainTable, string createTableSqlBody, string addAndUpdateSql,
        Func<List<T>, string, Task>? PostAction = null, int? commandTimeout = 0);

    #endregion
}
