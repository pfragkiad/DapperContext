﻿using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DapperContext.Contexts;

public class SqlServerContext : DbContext
{
    private readonly string _connectionString;

    public SqlServerContext(string connectionString)
    {
        _connectionString = connectionString;
        SqlMapper.Settings.CommandTimeout = 150;
    }

    public override IDbConnection Connect() => new SqlConnection(_connectionString);

 
}
