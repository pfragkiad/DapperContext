using Dapper;
using DapperContext.Attributes;
using DapperContext.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace DapperContext.Contexts;

public abstract class DbContext : IDbContext
{
    public abstract IDbConnection Connect();


    #region Generic data operations

    public async Task<int> Execute(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null)
    {
        if (transaction is null)
        {
            using IDbConnection connection = Connect();
            return await connection.ExecuteAsync(sql, parameters, commandTimeout: commandTimeout);
        }
        return await transaction.Connection!.ExecuteAsync(sql, parameters, transaction, commandTimeout);
    }

    public async Task<T?> ExecuteScalar<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null)
    {
        if (transaction is null)
        {
            using IDbConnection connection = Connect();
            return await connection.ExecuteScalarAsync<T>(sql, parameters, commandTimeout: commandTimeout);
        }
        return await transaction.Connection!.ExecuteScalarAsync<T>(sql, parameters, transaction, commandTimeout);
    }

    public async Task<T?> QueryFirst<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null)
    {
        if (transaction is null)
        {
            using IDbConnection connection = Connect();

            return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters, commandTimeout: commandTimeout);
        }
        return await transaction.Connection!.QueryFirstOrDefaultAsync<T>(sql, parameters, transaction, commandTimeout: commandTimeout);
    }

    public async Task<IEnumerable<T>> Query<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null)
    {
        if (transaction is null)
        {
            using IDbConnection connection = Connect();
            return await connection.QueryAsync<T>(sql, parameters, commandTimeout: commandTimeout);
        }
        return await transaction.Connection!.QueryAsync<T>(sql, parameters, transaction, commandTimeout: commandTimeout);
    }

    public async Task<IEnumerable<T>> QueryProcedure<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null)
    {
        if (transaction is null)
        {
            using IDbConnection connection = Connect();
            return await connection.QueryAsync<T>(sql, parameters, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout);
        }
        return await transaction.Connection!.QueryAsync<T>(sql, parameters, transaction, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout);
    }
    public async Task<T?> QueryProcedureScalar<T>(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null) where T : struct
    {
        if (transaction is null)
        {
            using IDbConnection connection = Connect();
            return await connection.ExecuteScalarAsync<T?>(sql, parameters, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout);
        }
        return await transaction.Connection!.ExecuteScalarAsync<T?>(sql, parameters, transaction, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout);
    }
    public async Task<int> ExecuteProcedure(string sql, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null)
    {
        if (transaction is null)
        {
            using IDbConnection connection = Connect();
            return await connection.ExecuteAsync(sql, parameters, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout);
        }
        return await transaction.Connection!.ExecuteAsync(sql, parameters, transaction, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout);
    }

    public virtual async Task<IEnumerable<T>> QueryTable<T>(string tableName, IDbTransaction? transaction = null, int? commandTimeout = null) =>
         await Query<T>($"select * from [{tableName}]", null, transaction);

    #endregion

    #region CRUD

    public virtual async Task<bool> Delete(int id, string table, IDbTransaction? transaction = null)
    {
        string sql = $"delete t from [{table}] where t.id = @id";
        int rowsAffected = await Execute(sql, new { id }, transaction);
        return rowsAffected > 0;
    }

    public virtual async Task<TEntity?> GetById<TEntity>(int id, string table)
    {
        string sql = $"select top 1 * from [{table}] where id = @id";
        var entity = await QueryFirst<TEntity>(sql, new { id });
        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> GetAll<TEntity>(string table, int? commandTimeout = null) =>
        await QueryTable<TEntity>(table, commandTimeout: commandTimeout);

    #endregion

    #region DataTable and Tables
    public string PrefixForTempTables { get; init; } = "_tmp";

    private static object ToObject<TMember>(TMember? v)
    {
        if (v is null) return DBNull.Value;

        if (v is string)
        {
            string s = (v as string)!.Trim();
            if (!string.IsNullOrEmpty(s))
                return s;

            return DBNull.Value;
        }

        return v;
    }

    public virtual bool TableExists(string tableName, string? schema = null, IDbConnection? connection = null)
    {
        connection ??= Connect();

        string sql = $"SELECT count(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";

        if (!string.IsNullOrWhiteSpace(schema)) sql += $" and table_schema = '{schema}'";

        int rows = connection.ExecuteScalar<int>(sql);
        return rows > 0;
    }

    public DataTable InitializeDatatable<T>(IEnumerable<T> items)
    {
        Type t = typeof(T);

        DataTable table = new();
        var properties = t.GetProperties()
           //ignore those that both have the attribute [BulkIgnore]
           //do not Ignore those with attribute [BulkIgnore(false)]
           .Where(p => !p.GetCustomAttribute<DataRowIgnoreAttribute>()?.Ignore ?? true);

        foreach (var p in properties)
        {
            string columnName = p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name;
            table.Columns.Add(columnName, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);
        }

        if (t.GetInterfaces().Any(i => i == typeof(IDataRow)))
            //if the interface is implemented then this speeds up operations 
            foreach (T item in items)
            {
                var values = (item as IDataRow)!.ToDataRow();
                table.Rows.Add(values);
            }
        else
            foreach (var item in items)
            {
                var values = properties.Select(p => ToObject(p.GetValue(item))).ToArray();
                table.Rows.Add(values);
            }


        return table;
    }

    public string GetNewTempTableName(IDbConnection connection, string tableNamePrefix)
    {
        while (true)
        {
            int i = Random.Shared.Next();
            if (!TableExists($"{tableNamePrefix}_{i}", connection: connection))
                return $"{tableNamePrefix}_{i}";
        }
    }

    public string GetNewTempTableName(string tableNamePrefix)
    {
        var connection = Connect();

        while (true)
        {
            int i = Random.Shared.Next();
            if (!TableExists($"{tableNamePrefix}_{i}", connection: connection))
            {
                connection.Close();
                return $"{tableNamePrefix}_{i}";
            }
        }
    }

    public async Task<string> InitializeTempTable<T>(
        IEnumerable<T> items, string createTableSql, int? bulkTimeout=null)
    {
        DataTable table = InitializeDatatable(items);

        using IDbConnection connection = Connect(); //new SqlConnection(_connectionString);
        SqlBulkCopy copier = new SqlBulkCopy(connection as SqlConnection);
        copier.BulkCopyTimeout = bulkTimeout?? 0;

        foreach (var c in table.Columns.Cast<DataColumn>())
            copier.ColumnMappings.Add(c.ColumnName, c.ColumnName);

        connection.Open();
        string prefix = string.IsNullOrWhiteSpace(PrefixForTempTables) ? "_tmp" : PrefixForTempTables;
        string tempTable = GetNewTempTableName(connection, prefix);
        copier.DestinationTableName = tempTable;

        createTableSql = createTableSql.Replace("{tempTable}", tempTable);
        await connection.ExecuteAsync(createTableSql);
        await copier.WriteToServerAsync(table);
        connection.Close();

        return tempTable;
    }

    public async Task<(int Updated, int Added)> UpdateOldAndAddNew<T>(
        List<T> items, string mainTable, string createTableSqlBody, string addAndUpdateSql,
        Func<List<T>, string, Task>? PostAction = null, int? commandTimeout = null)
    {
        string tempTable = await InitializeTempTable(items, createTableSqlBody);

        string tempOld = GetNewTempTableName("_old");

        //add, update and return report
        addAndUpdateSql = addAndUpdateSql.Replace("{tempTable}", tempTable).Replace("{Table}", mainTable).Replace("{tempOld}", tempOld);

        var connection = Connect();
        int[] counts = (await connection.QueryAsync<int>(addAndUpdateSql,commandTimeout:commandTimeout)).ToArray();

        if (PostAction is not null) await PostAction(items, tempTable);

        await Execute($"drop table {tempTable}");
        await Execute($"drop table if exists {tempOld}");


        return counts.Length == 1 ? (counts[0], items.Count - counts[0]) : (counts[0], counts[1]);

    }


    #endregion



}
