using System.Data;
using System.Transactions;

namespace DapperContext.Interfaces;

public interface IDbContext
{

    IDbConnection Connect();

    #region Generic data operations

    Task<int> Execute(string sql, object? parameters = null, IDbTransaction? transaction = null);
    Task<T> ExecuteScalar<T>(string sql, object? parameters = null, IDbTransaction? transaction = null);
    Task<T> QueryFirst<T>(string sql, object? parameters = null, IDbTransaction? transaction = null);
    Task<IEnumerable<T>> Query<T>(string sql, object? parameters = null, IDbTransaction? transaction = null);
    Task<IEnumerable<T>> QueryProcedure<T>(string sql, object? parameters = null, IDbTransaction? transaction = null);
    Task<T?> QueryProcedureScalar<T>(string sql, object? parameters = null, IDbTransaction? transaction = null) where T : struct;
  
    Task<int> ExecuteProcedure(string sql, object? parameters = null, IDbTransaction? transaction = null);

    Task<IEnumerable<T>> QueryTable<T>(string tableName,IDbTransaction? transaction = null);

    #endregion


    #region CRUD

    Task<bool> Delete(int id, string table, IDbTransaction? transaction = null);
    Task<TEntity?> GetById<TEntity>(int id, string table);
    Task<IEnumerable<TEntity>> GetAll<TEntity>(string table);

    #endregion

    #region DataTable and Tables
    string PrefixForTempTables { get; init; }
    bool TableExists(string tableName, string? schema = null, IDbConnection? connection = null);
    Task<string> InitializeTempTable<T>(IEnumerable<T> items, string createTableSqlBody);

    DataTable InitializeDatatable<TEntity>(IEnumerable<TEntity> items);
    string GetNewTempTableName(IDbConnection connection, string tableNamePrefix);
    string GetNewTempTableName(string tableNamePrefix);
    Task<(int Updated, int Added)> UpdateOldAndAddNew<T>(IEnumerable<T> items, string mainTable, string createTableSqlBody, string addAndUpdateSql,
        Func<IEnumerable<T>, string, Task>? PostAction = null);

    #endregion
}
