// File: Data/TransactionRepository.cs
// Namespace: BudgetTracker.Console.Net8.Data
//
// Purpose:
// Handles all SQL operations for the Transactions table using ADO.NET.

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Data
{
    /// <summary>
    /// Repository responsible for all SQL operations on the Transactions table.
    /// Uses ADO.NET and a normalized CategoryId foreign key.
    /// </summary>
    public class TransactionRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes the repository using the global DatabaseConfig connection string.
        /// </summary>
        public TransactionRepository()
        {
            _connectionString = DatabaseConfig.ConnectionString
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        // -----------------------------------------------------------
        // ADD TRANSACTION
        // -----------------------------------------------------------

        /// <summary>
        /// Inserts a new transaction into the Transactions table.
        /// Ensures CategoryId is resolved (creating the category if needed).
        /// </summary>
        public void Add(Transaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            int? categoryId = null;
            if (!string.IsNullOrWhiteSpace(transaction.Category))
            {
                categoryId = GetOrCreateCategoryId(connection, transaction.Category);
            }

            const string sql = @"
                INSERT INTO Transactions (Description, Amount, Type, Category, CategoryId, Date)
                VALUES (@Description, @Amount, @Type, @Category, @CategoryId, @Date);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Description", transaction.Description);
            command.Parameters.AddWithValue("@Amount", transaction.Amount);
            command.Parameters.AddWithValue("@Type", transaction.Type);
            command.Parameters.AddWithValue("@Category", transaction.Category ?? string.Empty);

            if (categoryId.HasValue)
                command.Parameters.AddWithValue("@CategoryId", categoryId.Value);
            else
                command.Parameters.AddWithValue("@CategoryId", DBNull.Value);

            command.Parameters.AddWithValue("@Date", transaction.Date);

            command.ExecuteNonQuery();
        }

        // -----------------------------------------------------------
        // UPDATE TRANSACTION
        // -----------------------------------------------------------

        /// <summary>
        /// Updates an existing transaction (matched by Id).
        /// </summary>
        public void Update(Transaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            int? categoryId = null;
            if (!string.IsNullOrWhiteSpace(transaction.Category))
            {
                categoryId = GetOrCreateCategoryId(connection, transaction.Category);
            }

            const string sql = @"
                UPDATE Transactions
                SET Description = @Description,
                    Amount      = @Amount,
                    Type        = @Type,
                    Category    = @Category,
                    CategoryId  = @CategoryId,
                    Date        = @Date
                WHERE Id = @Id;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", transaction.Id);
            command.Parameters.AddWithValue("@Description", transaction.Description);
            command.Parameters.AddWithValue("@Amount", transaction.Amount);
            command.Parameters.AddWithValue("@Type", transaction.Type);
            command.Parameters.AddWithValue("@Category", transaction.Category ?? string.Empty);

            if (categoryId.HasValue)
                command.Parameters.AddWithValue("@CategoryId", categoryId.Value);
            else
                command.Parameters.AddWithValue("@CategoryId", DBNull.Value);

            command.Parameters.AddWithValue("@Date", transaction.Date);

            command.ExecuteNonQuery();
        }

        // -----------------------------------------------------------
        // DELETE TRANSACTION
        // -----------------------------------------------------------

        /// <summary>
        /// Deletes a transaction by Id.
        /// </summary>
        public void Delete(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string sql = "DELETE FROM Transactions WHERE Id = @Id;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            command.ExecuteNonQuery();
        }

        // -----------------------------------------------------------
        // GET BY ID
        // -----------------------------------------------------------

        /// <summary>
        /// Returns a single transaction by Id, or null if not found.
        /// Uses Categories join so Category text reflects current CategoryId.
        /// </summary>
        public Transaction? GetById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string sql = @"
                SELECT
                    t.Id,
                    t.Description,
                    t.Amount,
                    t.Type,
                    ISNULL(c.Name, t.Category) AS Category,
                    t.CategoryId,
                    t.Date
                FROM Transactions t
                LEFT JOIN Categories c ON t.CategoryId = c.Id
                WHERE t.Id = @Id;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapTransaction(reader);
        }

        // -----------------------------------------------------------
        // GET ALL TRANSACTIONS
        // -----------------------------------------------------------

        /// <summary>
        /// Returns all transactions ordered by Date DESC, then Id DESC.
        /// Uses Categories join so Category text reflects current CategoryId.
        /// </summary>
        public List<Transaction> GetAll()
        {
            var results = new List<Transaction>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string sql = @"
                SELECT
                    t.Id,
                    t.Description,
                    t.Amount,
                    t.Type,
                    ISNULL(c.Name, t.Category) AS Category,
                    t.CategoryId,
                    t.Date
                FROM Transactions t
                LEFT JOIN Categories c ON t.CategoryId = c.Id
                ORDER BY t.Date DESC, t.Id DESC;";

            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(MapTransaction(reader));
            }

            return results;
        }

        // -----------------------------------------------------------
        // CATEGORY SUMMARY QUERIES
        // -----------------------------------------------------------

        /// <summary>
        /// Returns category summaries for a specific month and year.
        /// </summary>
        public List<CategorySummary> GetCategorySummariesByMonth(int year, int month)
        {
            var results = new List<CategorySummary>();

            const string sql = @"
                SELECT 
                    ISNULL(c.Name, t.Category) AS CategoryName,
                    SUM(CASE WHEN t.Type = 'Income'  THEN t.Amount ELSE 0 END) AS TotalIncome,
                    SUM(CASE WHEN t.Type = 'Expense' THEN t.Amount ELSE 0 END) AS TotalExpense
                FROM Transactions t
                LEFT JOIN Categories c ON t.CategoryId = c.Id
                WHERE YEAR(t.[Date]) = @Year
                  AND MONTH(t.[Date]) = @Month
                GROUP BY ISNULL(c.Name, t.Category)
                ORDER BY ISNULL(c.Name, t.Category);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Year", year);
            command.Parameters.AddWithValue("@Month", month);

            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var summary = new CategorySummary
                {
                    CategoryName = reader["CategoryName"]?.ToString() ?? string.Empty,
                    TotalIncome = reader.GetDecimal(reader.GetOrdinal("TotalIncome")),
                    TotalExpense = reader.GetDecimal(reader.GetOrdinal("TotalExpense"))
                };

                results.Add(summary);
            }

            return results;
        }

        /// <summary>
        /// Returns category summaries across all transactions (no date filter).
        /// </summary>
        public List<CategorySummary> GetOverallCategorySummaries()
        {
            var results = new List<CategorySummary>();

            const string sql = @"
                SELECT 
                    ISNULL(c.Name, t.Category) AS CategoryName,
                    SUM(CASE WHEN t.Type = 'Income'  THEN t.Amount ELSE 0 END) AS TotalIncome,
                    SUM(CASE WHEN t.Type = 'Expense' THEN t.Amount ELSE 0 END) AS TotalExpense
                FROM Transactions t
                LEFT JOIN Categories c ON t.CategoryId = c.Id
                GROUP BY ISNULL(c.Name, t.Category)
                ORDER BY ISNULL(c.Name, t.Category);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var summary = new CategorySummary
                {
                    CategoryName = reader["CategoryName"]?.ToString() ?? string.Empty,
                    TotalIncome = reader.GetDecimal(reader.GetOrdinal("TotalIncome")),
                    TotalExpense = reader.GetDecimal(reader.GetOrdinal("TotalExpense"))
                };

                results.Add(summary);
            }

            return results;
        }

        // -----------------------------------------------------------
        // CATEGORY REASSIGNMENT
        // -----------------------------------------------------------

        /// <summary>
        /// Reassigns all transactions from one CategoryId to another.
        /// Also updates the legacy Category text field to match the new category name.
        /// </summary>
        public void ReassignCategory(int fromCategoryId, int toCategoryId)
        {
            if (fromCategoryId <= 0)
                throw new ArgumentException("fromCategoryId must be > 0.", nameof(fromCategoryId));

            if (toCategoryId <= 0)
                throw new ArgumentException("toCategoryId must be > 0.", nameof(toCategoryId));

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            const string sql = @"
                UPDATE t
                SET
                    t.CategoryId = @ToCategoryId,
                    t.Category   = ISNULL(cTo.Name, t.Category)
                FROM Transactions t
                LEFT JOIN Categories cTo ON cTo.Id = @ToCategoryId
                WHERE t.CategoryId = @FromCategoryId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FromCategoryId", fromCategoryId);
            command.Parameters.AddWithValue("@ToCategoryId", toCategoryId);

            command.ExecuteNonQuery();
        }

        // -----------------------------------------------------------
        // HELPER: MAP TRANSACTION
        // -----------------------------------------------------------

        /// <summary>
        /// Maps a SqlDataReader row to a Transaction object.
        /// </summary>
        private static Transaction MapTransaction(SqlDataReader reader)
        {
            var transaction = new Transaction
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Description = reader["Description"]?.ToString() ?? string.Empty,
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                Type = reader["Type"]?.ToString() ?? string.Empty,
                Category = reader["Category"]?.ToString() ?? string.Empty,
                Date = reader.GetDateTime(reader.GetOrdinal("Date"))
            };

            int categoryIdOrdinal = reader.GetOrdinal("CategoryId");
            if (!reader.IsDBNull(categoryIdOrdinal))
            {
                transaction.CategoryId = reader.GetInt32(categoryIdOrdinal);
            }

            return transaction;
        }

        // -----------------------------------------------------------
        // CATEGORY SUPPORT (PRIVATE)
        // -----------------------------------------------------------

        /// <summary>
        /// Ensures the given category name exists in the Categories table and
        /// returns its Id. If the category does not exist, it is inserted.
        /// </summary>
        private int GetOrCreateCategoryId(SqlConnection connection, string categoryName)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentException("Category name is required.", nameof(categoryName));

            var trimmed = categoryName.Trim();

            const string selectSql = @"
                SELECT Id
                FROM Categories
                WHERE Name = @Name;";

            using (var selectCommand = new SqlCommand(selectSql, connection))
            {
                selectCommand.Parameters.AddWithValue("@Name", trimmed);

                var existingId = selectCommand.ExecuteScalar();
                if (existingId != null && existingId != DBNull.Value)
                {
                    return Convert.ToInt32(existingId);
                }
            }

            const string insertSql = @"
                INSERT INTO Categories (Name)
                VALUES (@Name);
                SELECT SCOPE_IDENTITY();";

            using (var insertCommand = new SqlCommand(insertSql, connection))
            {
                insertCommand.Parameters.AddWithValue("@Name", trimmed);

                var newIdObj = insertCommand.ExecuteScalar();
                return Convert.ToInt32(newIdObj);
            }
        }
    }
}
