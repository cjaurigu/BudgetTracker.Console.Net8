// File: Data/MonthlyCategoryBudgetRepository.cs
// Namespace: BudgetTracker.Console.Net8.Data
//
// Purpose:
// Provides ADO.NET operations for MonthlyCategoryBudgets (per CategoryId + Year + Month).

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Data
{
    public sealed class MonthlyCategoryBudgetRepository
    {
        private readonly string _connectionString;

        public MonthlyCategoryBudgetRepository()
        {
            _connectionString = DatabaseConfig.ConnectionString
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        public MonthlyCategoryBudget? Get(int categoryId, int year, int month)
        {
            const string sql = @"
                SELECT Id, CategoryId, [Year], [Month], BudgetAmount
                FROM MonthlyCategoryBudgets
                WHERE CategoryId = @CategoryId AND [Year] = @Year AND [Month] = @Month;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@Year", year);
            command.Parameters.AddWithValue("@Month", month);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new MonthlyCategoryBudget
            {
                Id = reader.GetInt32(0),
                CategoryId = reader.GetInt32(1),
                Year = reader.GetInt32(2),
                Month = reader.GetInt32(3),
                BudgetAmount = reader.GetDecimal(4)
            };
        }

        public List<CategoryBudgetView> GetAllWithCategoryNames(int year, int month)
        {
            var results = new List<CategoryBudgetView>();

            const string sql = @"
                SELECT 
                    c.Id AS CategoryId,
                    c.Name AS CategoryName,
                    ISNULL(mcb.BudgetAmount, 0) AS BudgetAmount
                FROM Categories c
                LEFT JOIN MonthlyCategoryBudgets mcb
                    ON mcb.CategoryId = c.Id
                    AND mcb.[Year] = @Year
                    AND mcb.[Month] = @Month
                ORDER BY c.Name;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Year", year);
            command.Parameters.AddWithValue("@Month", month);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new CategoryBudgetView
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    BudgetAmount = reader.GetDecimal(2)
                });
            }

            return results;
        }

        public void Upsert(int categoryId, int year, int month, decimal amount)
        {
            const string sql = @"
                MERGE MonthlyCategoryBudgets AS target
                USING (SELECT @CategoryId AS CategoryId, @Year AS [Year], @Month AS [Month]) AS src
                ON target.CategoryId = src.CategoryId AND target.[Year] = src.[Year] AND target.[Month] = src.[Month]
                WHEN MATCHED THEN
                    UPDATE SET BudgetAmount = @Amount, UpdatedAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (CategoryId, [Year], [Month], BudgetAmount)
                    VALUES (@CategoryId, @Year, @Month, @Amount);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@Year", year);
            command.Parameters.AddWithValue("@Month", month);
            command.Parameters.AddWithValue("@Amount", amount);

            connection.Open();
            command.ExecuteNonQuery();
        }

        public void Delete(int categoryId, int year, int month)
        {
            const string sql = @"
                DELETE FROM MonthlyCategoryBudgets
                WHERE CategoryId = @CategoryId AND [Year] = @Year AND [Month] = @Month;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@Year", year);
            command.Parameters.AddWithValue("@Month", month);

            connection.Open();
            command.ExecuteNonQuery();
        }
    }
}
