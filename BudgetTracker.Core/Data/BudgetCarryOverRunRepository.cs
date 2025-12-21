// File: Data/BudgetCarryOverRunRepository.cs
// Namespace: BudgetTracker.Console.Net8.Data
//
// Purpose:
// ADO.NET operations for BudgetCarryOverRuns (prevents double-apply).

using System;
using Microsoft.Data.SqlClient;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Data
{
    public sealed class BudgetCarryOverRunRepository
    {
        private readonly string _connectionString;

        public BudgetCarryOverRunRepository()
        {
            _connectionString = DatabaseConfig.ConnectionString
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        public bool Exists(int fromYear, int fromMonth)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM BudgetCarryOverRuns
                WHERE FromYear = @FromYear AND FromMonth = @FromMonth;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FromYear", fromYear);
            command.Parameters.AddWithValue("@FromMonth", fromMonth);

            connection.Open();
            var countObj = command.ExecuteScalar();
            var count = Convert.ToInt32(countObj);

            return count > 0;
        }

        public void Add(int fromYear, int fromMonth, decimal totalAmount)
        {
            const string sql = @"
                INSERT INTO BudgetCarryOverRuns (FromYear, FromMonth, TotalAmount)
                VALUES (@FromYear, @FromMonth, @TotalAmount);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FromYear", fromYear);
            command.Parameters.AddWithValue("@FromMonth", fromMonth);
            command.Parameters.AddWithValue("@TotalAmount", totalAmount);

            connection.Open();
            command.ExecuteNonQuery();
        }
    }
}
