using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleDotnet.RepositoryFactory.Tests
{
    public static class DbContextExtensions
    {
        public static async Task CLEAN_TABLES_DO_NOT_USE_PRODUCTION(this DbContext dbContext)
        {
            using var connection = new SqlConnection(dbContext.Database.GetConnectionString());
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = @"
            DECLARE @Sql NVARCHAR(MAX) = N'';

            -- Generate a TRUNCATE statement for each user-defined table
            SELECT @Sql += 'TRUNCATE TABLE ' + QUOTENAME(TABLE_SCHEMA) + '.' + QUOTENAME(TABLE_NAME) + ';'
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
              AND TABLE_SCHEMA <> 'sys'  -- Exclude system tables
              AND TABLE_SCHEMA <> 'INFORMATION_SCHEMA'  -- Exclude information schema tables
              AND OBJECTPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), 'IsMsShipped') = 0;  -- Exclude system tables shipped with SQL Server

            -- Execute the generated SQL
            EXEC sp_executesql @Sql;
        ";

            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }
}
