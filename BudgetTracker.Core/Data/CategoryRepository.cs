// File: Data/CategoryRepository.cs
// Namespace: BudgetTracker.Console.Net8.Data
//
// Purpose:
// ADO.NET repository for Categories table.

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Data
{
    /// <summary>
    /// Data access layer for Categories.
    /// </summary>
    public class CategoryRepository
    {
        private readonly string _connectionString;

        public CategoryRepository()
        {
            _connectionString = DatabaseConfig.ConnectionString
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        /// <summary>
        /// Returns all categories sorted by name.
        /// </summary>
        public List<Category> GetAll()
        {
            const string sql = @"
                SELECT Id, Name
                FROM Categories
                ORDER BY Name;";

            var results = new List<Category>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }

            return results;
        }

        /// <summary>
        /// Returns a category by Id, or null if not found.
        /// </summary>
        public Category? GetById(int id)
        {
            const string sql = @"
                SELECT Id, Name
                FROM Categories
                WHERE Id = @Id;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", id);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            };
        }

        /// <summary>
        /// Returns a category by name, or null if not found.
        /// </summary>
        public Category? GetByName(string name)
        {
            const string sql = @"
                SELECT Id, Name
                FROM Categories
                WHERE Name = @Name;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Name", name);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            };
        }

        /// <summary>
        /// Inserts a new category row.
        /// </summary>
        public void Add(string name)
        {
            const string sql = @"
                INSERT INTO Categories (Name)
                VALUES (@Name);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Name", name);

            connection.Open();
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Renames a category by Id.
        /// </summary>
        public void Rename(int id, string newName)
        {
            const string sql = @"
                UPDATE Categories
                SET Name = @Name
                WHERE Id = @Id;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", newName);

            connection.Open();
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes a category by Id.
        /// </summary>
        public void Delete(int id)
        {
            const string sql = @"
                DELETE FROM Categories
                WHERE Id = @Id;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", id);

            connection.Open();
            command.ExecuteNonQuery();
        }
    }
}
