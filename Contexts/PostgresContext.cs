using Dapper;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using System.Data;

namespace DapperContext.Contexts;

public class PostgresContext : DbContext
{
    private readonly string _connectionString;

    public PostgresContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override IDbConnection Connect() => new NpgsqlConnection(_connectionString);


}
