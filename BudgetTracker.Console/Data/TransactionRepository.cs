
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using BudgetTracker.Console.Domain;

namespace BudgetTracker.Console.Data
{
    public class TransactionRepository
    {
        private readonly string _connectionString;

        public TransactionRepository()
        {
            _connectionString = DatabaseConfig.ConnectionString;
        }

        public void Add(Transaction transaction)
        {
            const string sql = @"
                INSERT INTO Transactions (Description, Amount, Type, Category, Date)
                VALUES (@Description, @Amount, @Type, @Category, @Date);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Description", transaction.Description);
            command.Parameters.AddWithValue("@Amount", transaction.Amount);
            command.Parameters.AddWithValue("@Type", transaction.Type);
            command.Parameters.AddWithValue("@Category", transaction.Category);
            command.Parameters.AddWithValue("@Date", transaction.Date);

            connection.Open();
            command.ExecuteNonQuery();
        }

        public List<Transaction> GetAll()
        {
            const string sql = @"
                SELECT Id, Description, Amount, Type, Category, Date
                FROM Transactions
                ORDER BY Date DESC;";

            var results = new List<Transaction>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var transaction = new Transaction
                {
                    Id = reader.GetInt32(0),
                    Description = reader.GetString(1),
                    Amount = reader.GetDecimal(2),
                    Type = reader.GetString(3),
                    Category = reader.GetString(4),
                    Date = reader.GetDateTime(5)
                };

                results.Add(transaction);
            }

            return results;
        }
    }
}
