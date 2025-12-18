using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Data
{
    /// <summary>
    /// Handles ALL SQL operations for the Categories table.
    /// This includes:
    /// - Reading categories
    /// - Adding new categories
    /// - Updating category names
    /// - Deleting categories
    /// 
    /// It acts as the "Data Access Layer" for Categories.
    /// </summary>
    public class CategoryRepository
    {
        private readonly string _connectionString;

        public CategoryRepository()
        {
            // Reads the SQL connection string from your DatabaseConfig class.
            _connectionString = DatabaseConfig.ConnectionString;
        }

        /// <summary>
        /// Retrieves ALL categories from the database, sorted alphabetically.
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

            // Read one row at a time and convert it into a Category object.
            while (reader.Read())
            {
                var category = new Category
                {
                    Id = reader.GetInt32(0),     // Column 0 = Id
                    Name = reader.GetString(1),  // Column 1 = Name
                };

                results.Add(category);
            }

            return results;
        }

        /// <summary>
        /// Retrieves a single category by its ID (or null if not found).
        /// </summary>
        public Category? GetById(int id)
        {
            const string sql = "SELECT Id, Name FROM Categories WHERE Id = @Id;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null; // No match → return null

            return new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            };
        }

        /// <summary>
        /// Retrieves a category by name. Useful for checking duplicates.
        /// </summary>
        public Category? GetByName(string name)
        {
            const string sql = "SELECT Id, Name FROM Categories WHERE Name = @Name;";

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
        /// Adds a new category to the database.
        /// </summary>
        public void Add(Category category)
        {
            const string sql = "INSERT INTO Categories (Name) VALUES (@Name);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Name", category.Name);

            connection.Open();
            command.ExecuteNonQuery(); // Execute the INSERT
        }

        /// <summary>
        /// Updates the name of an existing category.
        /// </summary>
        public void Update(Category category)
        {
            const string sql = "UPDATE Categories SET Name = @Name WHERE Id = @Id;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", category.Id);
            command.Parameters.AddWithValue("@Name", category.Name);

            connection.Open();
            command.ExecuteNonQuery(); // Execute UPDATE
        }

        /// <summary>
        /// Deletes a category by ID.
        /// WARNING: If CategoryId in Transactions has FK, this may fail without cascade rules.
        /// </summary>
        public void Delete(int id)
        {
            const string sql = "DELETE FROM Categories WHERE Id = @Id;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", id);

            connection.Open();
            command.ExecuteNonQuery(); // Execute DELETE
        }
    }
}
