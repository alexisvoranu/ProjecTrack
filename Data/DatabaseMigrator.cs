using System;
using System.Data;
using System.Data.SqlClient;
using Npgsql;

namespace Licenta3.Data
{
    public class DatabaseMigrator
    {
        private readonly string sqlServerConnectionString;
        private readonly string postgresConnectionString;

        public DatabaseMigrator(string sqlServerConnectionString, string postgresConnectionString)
        {
            this.sqlServerConnectionString = sqlServerConnectionString;
            this.postgresConnectionString = postgresConnectionString;
        }

        public void MigrateTable(string tableName)
        {
            using var sqlConnection = new SqlConnection(sqlServerConnectionString);
            sqlConnection.Open();

            using var selectCommand = new SqlCommand($"SELECT * FROM {tableName}", sqlConnection);
            using var reader = selectCommand.ExecuteReader();

            using var postgresConnection = new NpgsqlConnection(postgresConnectionString);
            postgresConnection.Open();

            using var transaction = postgresConnection.BeginTransaction();

            var schemaTable = reader.GetSchemaTable();
            var columnNames = string.Join(", ", schemaTable.Rows.Cast<DataRow>().Select(r => $"\"{r["ColumnName"]}\""));
            var paramNames = string.Join(", ", schemaTable.Rows.Cast<DataRow>().Select((r, i) => $"@p{i}"));

            var insertCommand = postgresConnection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = $"INSERT INTO \"{tableName}\" ({columnNames}) VALUES ({paramNames})";

            for (int i = 0; i < reader.FieldCount; i++)
                insertCommand.Parameters.Add(new NpgsqlParameter($"@p{i}", DbType.Object));

            int rowCount = 0;
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                    insertCommand.Parameters[i].Value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);

                insertCommand.ExecuteNonQuery();
                rowCount++;
            }

            transaction.Commit();
            Console.WriteLine($"✅ Migrated {rowCount} rows from {tableName}");
        }
    }
}
