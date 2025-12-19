// File: Data/RecurringTransactionRepository.cs
// Namespace: BudgetTracker.Console.Net8.Data
//
// Purpose:
// ADO.NET repository for RecurringTransactions table.

using System;
using System.Collections.Generic;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace BudgetTracker.Console.Net8.Data
{
    /// <summary>
    /// Data access for recurring transaction templates.
    /// </summary>
    public class RecurringTransactionRepository
    {
        private readonly string _connectionString;

        public RecurringTransactionRepository()
        {
            _connectionString = DatabaseConfig.ConnectionString
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        public int Add(RecurringTransaction template)
        {
            const string sql = @"
INSERT INTO RecurringTransactions
(
    Description, Amount, Type, Category, CategoryId, StartDate, Frequency, DayOfMonth, NextRunDate, IsActive
)
OUTPUT INSERTED.Id
VALUES
(
    @Description, @Amount, @Type, @Category, @CategoryId, @StartDate, @Frequency, @DayOfMonth, @NextRunDate, @IsActive
);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Description", template.Description);
            command.Parameters.AddWithValue("@Amount", template.Amount);
            command.Parameters.AddWithValue("@Type", template.Type);
            command.Parameters.AddWithValue("@Category", template.Category);

            if (template.CategoryId.HasValue)
                command.Parameters.AddWithValue("@CategoryId", template.CategoryId.Value);
            else
                command.Parameters.AddWithValue("@CategoryId", DBNull.Value);

            command.Parameters.AddWithValue("@StartDate", template.StartDate.Date);
            command.Parameters.AddWithValue("@Frequency", template.Frequency.ToString());

            if (template.DayOfMonth.HasValue)
                command.Parameters.AddWithValue("@DayOfMonth", template.DayOfMonth.Value);
            else
                command.Parameters.AddWithValue("@DayOfMonth", DBNull.Value);

            command.Parameters.AddWithValue("@NextRunDate", template.NextRunDate.Date);
            command.Parameters.AddWithValue("@IsActive", template.IsActive);

            connection.Open();
            return (int)command.ExecuteScalar();
        }

        public List<RecurringTransaction> GetAll()
        {
            const string sql = @"
SELECT Id, Description, Amount, Type, Category, CategoryId, StartDate, Frequency, DayOfMonth, NextRunDate, IsActive
FROM RecurringTransactions
ORDER BY IsActive DESC, NextRunDate ASC, Id ASC;";

            var results = new List<RecurringTransaction>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(Map(reader));
            }

            return results;
        }

        public List<RecurringTransaction> GetDue(DateTime asOfDate)
        {
            const string sql = @"
SELECT Id, Description, Amount, Type, Category, CategoryId, StartDate, Frequency, DayOfMonth, NextRunDate, IsActive
FROM RecurringTransactions
WHERE IsActive = 1
  AND NextRunDate <= @AsOf
ORDER BY NextRunDate ASC, Id ASC;";

            var results = new List<RecurringTransaction>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@AsOf", asOfDate.Date);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(Map(reader));
            }

            return results;
        }

        public void UpdateNextRunDate(int id, DateTime nextRunDate)
        {
            const string sql = @"
UPDATE RecurringTransactions
SET NextRunDate = @NextRunDate
WHERE Id = @Id;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@NextRunDate", nextRunDate.Date);

            connection.Open();
            command.ExecuteNonQuery();
        }

        public void Deactivate(int id)
        {
            const string sql = @"
UPDATE RecurringTransactions
SET IsActive = 0
WHERE Id = @Id;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", id);

            connection.Open();
            command.ExecuteNonQuery();
        }

        private static RecurringTransaction Map(SqlDataReader reader)
        {
            var frequencyText = reader["Frequency"]?.ToString() ?? "Monthly";
            if (!Enum.TryParse<RecurringFrequency>(frequencyText, ignoreCase: true, out var freq))
                freq = RecurringFrequency.Monthly;

            int? categoryId = null;
            if (reader["CategoryId"] != DBNull.Value)
                categoryId = Convert.ToInt32(reader["CategoryId"]);

            int? dayOfMonth = null;
            if (reader["DayOfMonth"] != DBNull.Value)
                dayOfMonth = Convert.ToInt32(reader["DayOfMonth"]);

            return new RecurringTransaction
            {
                Id = Convert.ToInt32(reader["Id"]),
                Description = reader["Description"]?.ToString() ?? string.Empty,
                Amount = Convert.ToDecimal(reader["Amount"]),
                Type = reader["Type"]?.ToString() ?? "Expense",
                Category = reader["Category"]?.ToString() ?? string.Empty,
                CategoryId = categoryId,
                StartDate = Convert.ToDateTime(reader["StartDate"]),
                Frequency = freq,
                DayOfMonth = dayOfMonth,
                NextRunDate = Convert.ToDateTime(reader["NextRunDate"]),
                IsActive = Convert.ToBoolean(reader["IsActive"])
            };
        }
    }
}
