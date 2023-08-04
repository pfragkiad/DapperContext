using Dapper;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Data;

namespace DapperContext.Contexts;

public class MySqlContext : DbContext
{
    private readonly string _connectionString;

    public MySqlContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public override IDbConnection Connect() => new MySqlConnection(_connectionString);


}
