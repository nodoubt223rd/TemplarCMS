using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TemplarCMS.Persistence;

public static class SqlServerSchemaVerifier
{
    public const string RequiredVersion = "2026-09-tighten-columns-and-indexes-v1";
    public const string Collation = "SQL_Latin1_General_CP1_CI_AS";

    public static async Task VerifyAsync(TemplarCmsDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsSqlServer()) throw new ArgumentException("Schema verification requires SQL Server.", nameof(db));
        try
        {
            var versions = await db.Database.SqlQueryRaw<string>("SELECT VersionKey AS Value FROM dbo.SchemaVersions").ToListAsync(cancellationToken);
            if (!versions.Contains(RequiredVersion)) Fail("Required deployment version is missing.");
            var collation = await db.Database.SqlQueryRaw<string>("SELECT CONVERT(nvarchar(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation')) AS Value").SingleAsync(cancellationToken);
            if (collation != Collation) Fail("Database collation does not match the approved baseline.");
            var columns = await db.Database.SqlQueryRaw<ColumnShape>("""
                SELECT t.name AS TableName, c.name AS ColumnName, ty.name AS TypeName,
                       CONVERT(int,c.max_length) AS MaxLength, c.is_nullable AS IsNullable,
                       CONVERT(int,c.scale) AS Scale, c.collation_name AS CollationName
                FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
                JOIN sys.columns c ON c.object_id=t.object_id
                JOIN sys.types ty ON ty.user_type_id=c.user_type_id
                WHERE s.name='dbo'
                """).ToListAsync(cancellationToken);
            var indexes = await db.Database.SqlQueryRaw<IndexShape>("""
                SELECT t.name AS TableName, i.name AS IndexName, i.is_unique AS IsUnique,
                       i.is_disabled AS IsDisabled, i.is_primary_key AS IsPrimaryKey,
                       i.filter_definition AS FilterDefinition,
                       STUFF((SELECT ',' + c.name FROM sys.index_columns ic
                           JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                           WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0
                           ORDER BY ic.key_ordinal FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,'') AS KeyColumns
                FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id
                JOIN sys.schemas s ON s.schema_id=t.schema_id
                WHERE s.name='dbo' AND i.name IS NOT NULL
                """).ToListAsync(cancellationToken);
            Validate(db.Model, columns, indexes);
        }
        catch (Microsoft.Data.SqlClient.SqlException exception)
        {
            throw new InvalidOperationException("SQL Server schema verification failed. Have the deployment operator apply the reviewed schema scripts and confirm runtime read permissions; the application does not create or migrate SQL schemas.", exception);
        }
    }

    public static void Validate(IModel model, IReadOnlyList<ColumnShape> columns, IReadOnlyList<IndexShape> indexes)
    {
        foreach (var entity in model.GetEntityTypes())
        {
            var table = entity.GetTableName()!;
            var identifier = StoreObjectIdentifier.Table(table, entity.GetSchema());
            foreach (var property in entity.GetProperties())
            {
                var name = property.GetColumnName(identifier)!;
                var column = columns.SingleOrDefault(c => c.TableName == table && c.ColumnName == name);
                if (column is null) Fail($"Missing column {table}.{name}.");
                var actualType = column!.TypeName == "nvarchar"
                    ? $"nvarchar({(column.MaxLength == -1 ? "max" : (column.MaxLength / 2).ToString())})"
                    : column.TypeName;
                if (!string.Equals(actualType, property.GetColumnType(), StringComparison.OrdinalIgnoreCase) || column.IsNullable != property.IsNullable)
                    Fail($"Unexpected type, length or nullability for {table}.{name}.");
                if (column.TypeName == "nvarchar" && column.CollationName != Collation)
                    Fail($"Unexpected collation for {table}.{name}.");
                if (column.TypeName == "datetimeoffset" && column.Scale != 7)
                    Fail($"Unexpected timestamp precision for {table}.{name}.");
            }
            var primary = entity.FindPrimaryKey()!;
            if (!indexes.Any(i => i.TableName == table && i.IsPrimaryKey && i.IsUnique && !i.IsDisabled &&
                i.KeyColumns == string.Join(',', primary.Properties.Select(p => p.GetColumnName(identifier)))))
                Fail($"Missing or incompatible primary key for {table}.");
            foreach (var expected in entity.GetIndexes())
            {
                var index = indexes.SingleOrDefault(i => i.TableName == table && i.IndexName == expected.GetDatabaseName());
                if (index is null || index.IsDisabled || index.IsUnique != expected.IsUnique ||
                    index.KeyColumns != string.Join(',', expected.Properties.Select(p => p.GetColumnName(identifier))) ||
                    Normalize(index.FilterDefinition) != Normalize(expected.GetFilter()))
                    Fail($"Missing or incompatible index {table}.{expected.GetDatabaseName()}.");
            }
        }
    }

    private static string Normalize(string? filter) => new((filter ?? "").Where(c => !char.IsWhiteSpace(c) && c is not '[' and not ']' and not '(' and not ')').Select(char.ToLowerInvariant).ToArray());
    private static void Fail(string detail) => throw new InvalidOperationException($"SQL Server schema is not ready: {detail} Apply the reviewed deployment scripts before starting the application. No runtime DDL is permitted.");

    public sealed class ColumnShape
    {
        public string TableName { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public string TypeName { get; set; } = "";
        public int MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public int Scale { get; set; }
        public string? CollationName { get; set; }
    }
    public sealed class IndexShape
    {
        public string TableName { get; set; } = "";
        public string IndexName { get; set; } = "";
        public string KeyColumns { get; set; } = "";
        public bool IsUnique { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsPrimaryKey { get; set; }
        public string? FilterDefinition { get; set; }
    }
}
