// File: Data/CategoryBudgetRepository.cs
// Namespace: BudgetTracker.Console.Net8.Data
//
// Purpose:
// Provides all ADO.NET operations for the CategoryBudgets table.
// This supports "one budget per category" (Phase 1).

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Data
{
    /// <summary>
    /// Repository responsible for CRUD operations on CategoryBudgets.
    /// </summary>
    public class CategoryBudgetRepository
    {
        private readonly string _connectionString;

        public CategoryBudgetRepository()
        {
            // Uses your central connection string configuration.
            _connectionString = DatabaseConfig.ConnectionString;
        }

        // -----------------------------------------------------------
        // GET BUDGET BY CATEGORY ID
        // -----------------------------------------------------------
        /// <summary>
        /// Returns the budget record for a specific category, or null if none exists.
        /// </summary>
        public CategoryBudget? GetByCategoryId(int categoryId)
        {
            const string sql = @"
                SELECT Id, CategoryId, BudgetAmount
                FROM CategoryBudgets
                WHERE CategoryId = @CategoryId;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CategoryId", categoryId);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new CategoryBudget
            {
                Id = reader.GetInt32(0),
                CategoryId = reader.GetInt32(1),
                BudgetAmount = reader.GetDecimal(2)
            };
        }

        // -----------------------------------------------------------
        // GET ALL BUDGETS (WITH CATEGORY NAMES)
        // -----------------------------------------------------------
        /// <summary>
        /// Returns all budgets joined to Categories so the UI can display names.
        /// </summary>
        public List<CategoryBudgetView> GetAllWithCategoryNames()
        {
            var results = new List<CategoryBudgetView>();

            const string sql = @"
                SELECT 
                    cb.CategoryId,
                    c.Name AS CategoryName,
                    cb.BudgetAmount
                FROM CategoryBudgets cb
                INNER JOIN Categories c ON c.Id = cb.CategoryId
                ORDER BY c.Name;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

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

        // -----------------------------------------------------------
        // INSERT
        // -----------------------------------------------------------
        /// <summary>
        /// Adds a new budget record for a category.
        /// One budget per category is enforced by a UNIQUE constraint on CategoryId.
        /// </summary>
        public void Add(CategoryBudget budget)
        {
            if (budget == null)
                throw new ArgumentNullException(nameof(budget));

            const string sql = @"
                INSERT INTO CategoryBudgets (CategoryId, BudgetAmount)
                VALUES (@CategoryId, @BudgetAmount);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@CategoryId", budget.CategoryId);
            command.Parameters.AddWithValue("@BudgetAmount", budget.BudgetAmount);

            connection.Open();
            command.ExecuteNonQuery();
        }

        // -----------------------------------------------------------
        // UPDATE
        // -----------------------------------------------------------
        /// <summary>
        /// Updates an existing budget amount by CategoryId.
        /// </summary>
        public void UpdateByCategoryId(int categoryId, decimal newAmount)
        {
            const string sql = @"
                UPDATE CategoryBudgets
                SET BudgetAmount = @BudgetAmount
                WHERE CategoryId = @CategoryId;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@BudgetAmount", newAmount);

            connection.Open();
            command.ExecuteNonQuery();
        }

        // -----------------------------------------------------------
        // DELETE
        // -----------------------------------------------------------
        /// <summary>
        /// Deletes a budget row by CategoryId (if it exists).
        /// </summary>
        public void DeleteByCategoryId(int categoryId)
        {
            const string sql = "DELETE FROM CategoryBudgets WHERE CategoryId = @CategoryId;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@CategoryId", categoryId);

            connection.Open();
            command.ExecuteNonQuery();
        }
    }
}
