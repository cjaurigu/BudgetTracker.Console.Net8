// File: Data/BudgetChangeLogRepository.cs
// Namespace: BudgetTracker.Console.Net8.Data
//
// Purpose:
// ADO.NET operations for BudgetChangeLog (audit trail of monthly budget changes).

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Data
{
    public sealed class BudgetChangeLogRepository
    {
        private readonly string _connectionString;

        public BudgetChangeLogRepository()
        {
            _connectionString = DatabaseConfig.ConnectionString
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        public void Add(BudgetChangeLog item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            const string sql = @"
                INSERT INTO BudgetChangeLog (CategoryId, [Year], [Month], OldAmount, NewAmount, Action)
                VALUES (@CategoryId, @Year, @Month, @OldAmount, @NewAmount, @Action);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@CategoryId", item.CategoryId);
            command.Parameters.AddWithValue("@Year", item.Year);
            command.Parameters.AddWithValue("@Month", item.Month);
            command.Parameters.AddWithValue("@OldAmount", item.OldAmount);
            command.Parameters.AddWithValue("@NewAmount", item.NewAmount);
            command.Parameters.AddWithValue("@Action", item.Action ?? "Update");

            connection.Open();
            command.ExecuteNonQuery();
        }

        public List<BudgetChangeLog> GetForCategoryMonth(int categoryId, int year, int month, int maxRows = 50)
        {
            var results = new List<BudgetChangeLog>();

            const string sql = @"
                SELECT TOP (@MaxRows)
                    Id, CategoryId, [Year], [Month], OldAmount, NewAmount, ChangedAtUtc, Action
                FROM BudgetChangeLog
                WHERE CategoryId = @CategoryId AND [Year] = @Year AND [Month] = @Month
                ORDER BY ChangedAtUtc DESC, Id DESC;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@MaxRows", maxRows);
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@Year", year);
            command.Parameters.AddWithValue("@Month", month);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new BudgetChangeLog
                {
                    Id = reader.GetInt32(0),
                    CategoryId = reader.GetInt32(1),
                    Year = reader.GetInt32(2),
                    Month = reader.GetInt32(3),
                    OldAmount = reader.GetDecimal(4),
                    NewAmount = reader.GetDecimal(5),
                    ChangedAtUtc = reader.GetDateTime(6),
                    Action = reader["Action"]?.ToString() ?? "Update"
                });
            }

            return results;
        }
    }
}
