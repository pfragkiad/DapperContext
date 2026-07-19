# DapperContextAlt

*The simplest/fastest alternative to Entity Framework context, based entirely on Dapper.*

`DapperContextAlt` provides a lightweight context/repository layer on top of Dapper for common CRUD, raw SQL, temp-table workflows, and bulk update/import scenarios.

## How to install

Via the Package Manager:
```powershell
Install-Package DapperContextAlt
```

Via the .NET CLI:
```bash
dotnet add package DapperContextAlt
```

## Quick start

```csharp
using DapperContext.Contexts;
using DapperContext.Interfaces;

string cs = "Server=.;Database=AppDb;Trusted_Connection=True;TrustServerCertificate=True";
IDbContext db = new SqlServerContext(cs);

IEnumerable<Product> products = await db.Query<Product>(
    "select Id, Name, Price from dbo.Products where IsActive = @isActive",
    new { isActive = true });

Product? product = await db.GetById<Product>(1, "Products");
int rows = await db.Execute(
    "update dbo.Products set Price = @price where Id = @id",
    new { id = 1, price = 9.99m });
```

## Available context types

Use the provider-specific context that matches your connection string:

- `SqlServerContext`
- `PostgresContext`
- `MySqlContext`

```csharp
var sqlServer = new SqlServerContext(sqlServerConnectionString);
var postgres = new PostgresContext(postgresConnectionString);
var mySql = new MySqlContext(mySqlConnectionString);
```

## Core operations

The base `DbContext` exposes async helpers for the most common Dapper scenarios:

- `Execute(sql, parameters)`
- `ExecuteScalar<T>(sql, parameters)`
- `QueryFirst<T>(sql, parameters)`
- `Query<T>(sql, parameters)`
- `QueryProcedure<T>(name, parameters)`
- `QueryProcedureScalar<T>(name, parameters)`
- `ExecuteProcedure(name, parameters)`
- `QueryTable<T>(tableName)`
- `GetById<T>(id, tableName)`
- `GetAll<T>(tableName)`
- `Delete(id, tableName)`

Stored procedure example:

```csharp
IEnumerable<OrderSummary> orders = await db.QueryProcedure<OrderSummary>(
    "dbo.GetOrdersByCustomer",
    new { customerId = 42 });
```

## Repository base class

`Repository<T>` wraps the temp-table update/import workflow behind a small base abstraction.

```csharp
using DapperContext.Interfaces;
using DapperContext.Repositories;

public sealed class ProductRepository : Repository<ProductImportRow>
{
    public ProductRepository(IDbContext context) : base(context) { }

    protected override string MainTable => "dbo.Products";

    protected override string CreateTempTableCommand =>
        """
        CREATE TABLE [{tempTable}](
            [Id] INT NOT NULL,
            [Name] NVARCHAR(200) NOT NULL,
            [Price] DECIMAL(18,2) NULL
        )
        """;

    protected override string AddAndUpdateSqlCommand =>
        """
        update p
        set p.Name = t.Name,
            p.Price = t.Price
        from dbo.Products p
        inner join [{tempTable}] t on t.Id = p.Id;

        insert into dbo.Products(Id, Name, Price)
        select t.Id, t.Name, t.Price
        from [{tempTable}] t
        left join dbo.Products p on p.Id = t.Id
        where p.Id is null;
        """;
}
```

Then call:

```csharp
var repo = new ProductRepository(db);
(int updated, int added) = await repo.UpdateOldAndAddNew(items, updateIds: false);
```

## Temp tables and bulk-copy helpers

The package includes helpers for building a `DataTable`, creating a temp table, and pushing rows through `SqlBulkCopy`.

```csharp
string tempTable = await db.InitializeTempTable(items, """
    CREATE TABLE [{tempTable}](
        [Id] INT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL
    )
    """);
```

Use `TableExists` and temp-table naming helpers when needed:

```csharp
bool exists = db.TableExists("Products", schema: "dbo");
string tempName = db.GetNewTempTableName("_tmp");
```

## Fast row mapping with `IDataRow`

When a model implements `IDataRow`, `InitializeDatatable` uses `ToDataRow()` directly, which is faster than reflection-based property extraction.

```csharp
using DapperContext.Interfaces;

public sealed class ProductImportRow : IDataRow
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal? Price { get; init; }

    public object[] ToDataRow() => [Id, Name, Price ?? DBNull.Value];
}
```

## Column naming and ignored properties

`InitializeDatatable` respects `System.ComponentModel.DataAnnotations.Schema.ColumnAttribute` and `DataRowIgnoreAttribute`.

```csharp
using DapperContext.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

public sealed class ProductRow
{
    [Column("product_id")]
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    [DataRowIgnore]
    public string? LocalOnlyNote { get; init; }
}
```

## Notes

- The package targets `.NET 10`.
- `SqlServerContext` sets Dapper's global command timeout to `150` seconds.
- The temp-table bulk-copy workflow is SQL Server-oriented because it relies on `SqlBulkCopy`.
- The context layer stays intentionally thin; write raw SQL when you need full control.

## License

See `LICENSE.txt`.
