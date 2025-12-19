// File: Services/BudgetService.cs
// Namespace: BudgetTracker.Console.Net8.Services
//
// Purpose:
// Contains the BUSINESS LOGIC for transactions.
// UI should talk to this class instead of the repository directly.

using System;
using System.Collections.Generic;
using System.Linq;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Domain.Enums;

namespace BudgetTracker.Console.Net8.Services
{
    /// <summary>
    /// This service contains the BUSINESS LOGIC for transactions.
    /// UI talks to this class instead of talking directly to SQL or the repository.
    /// </summary>
    public class BudgetService
    {
        private readonly TransactionRepository _repository;

        /// <summary>
        /// Creates a new BudgetService with a TransactionRepository dependency.
        /// </summary>
        /// <param name="repository">Repository used to read/write transactions in SQL Server.</param>
        public BudgetService(TransactionRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        // -----------------------------------------------------------
        // BASIC CRUD OPERATIONS
        // -----------------------------------------------------------

        /// <summary>
        /// Adds a new transaction after performing basic validation.
        /// </summary>
        public void AddTransaction(Transaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            if (transaction.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(transaction.Amount));

            if (string.IsNullOrWhiteSpace(transaction.Description))
                throw new ArgumentException("Description is required.", nameof(transaction.Description));

            if (string.IsNullOrWhiteSpace(transaction.Type))
                throw new ArgumentException("Type (Income/Expense) is required.", nameof(transaction.Type));

            _repository.Add(transaction);
        }

        /// <summary>
        /// Updates an existing transaction.
        /// </summary>
        public void UpdateTransaction(Transaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            _repository.Update(transaction);
        }

        /// <summary>
        /// Deletes a transaction by its ID.
        /// </summary>
        public void DeleteTransaction(int id)
        {
            _repository.Delete(id);
        }

        /// <summary>
        /// Gets a single transaction by ID, or null if it does not exist.
        /// </summary>
        public Transaction? GetTransactionById(int id)
        {
            return _repository.GetById(id);
        }

        /// <summary>
        /// Returns ALL transactions from the database.
        /// </summary>
        public List<Transaction> GetAllTransactions()
        {
            return _repository.GetAll();
        }

        // -----------------------------------------------------------
        // FILTERING HELPERS
        // -----------------------------------------------------------

        /// <summary>
        /// Returns only income or only expense transactions, using TransactionType.
        /// </summary>
        public List<Transaction> GetByType(TransactionType type)
        {
            var all = _repository.GetAll();
            var targetType = type == TransactionType.Income ? "Income" : "Expense";

            return all
                .Where(t => string.Equals(t.Type, targetType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToList();
        }

        /// <summary>
        /// Returns all transactions for a specific year and month.
        /// </summary>
        public List<Transaction> GetByMonth(int year, int month)
        {
            var all = _repository.GetAll();

            return all
                .Where(t => t.Date.Year == year && t.Date.Month == month)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToList();
        }

        /// <summary>
        /// Searches transactions by keyword in Description and Category (case-insensitive).
        /// Returns matching transactions ordered by Date desc, then Id desc.
        /// </summary>
        /// <param name="keyword">Keyword to search for (part of description or category).</param>
        public List<Transaction> SearchTransactions(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                // If user provides blank or whitespace, just return an empty list
                // so that the UI can show "no results" instead of dumping everything.
                return new List<Transaction>();
            }

            var all = _repository.GetAll();
            var trimmed = keyword.Trim();

            return all
                .Where(t =>
                    (!string.IsNullOrEmpty(t.Description) &&
                     t.Description.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(t.Category) &&
                     t.Category.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToList();
        }

        // -----------------------------------------------------------
        // TOTALS AND SUMMARIES
        // -----------------------------------------------------------

        /// <summary>
        /// Returns overall income and expenses across ALL transactions.
        /// </summary>
        public (decimal income, decimal expenses) GetTotals()
        {
            var all = _repository.GetAll();

            var income = all
                .Where(t => t.Type == "Income")
                .Sum(t => t.Amount);

            var expenses = all
                .Where(t => t.Type == "Expense")
                .Sum(t => t.Amount);

            return (income, expenses);
        }

        /// <summary>
        /// Returns income and expenses for a specific month.
        /// </summary>
        public (decimal income, decimal expenses) GetMonthlyTotals(int year, int month)
        {
            var monthItems = GetByMonth(year, month);

            var income = monthItems
                .Where(t => t.Type == "Income")
                .Sum(t => t.Amount);

            var expenses = monthItems
                .Where(t => t.Type == "Expense")
                .Sum(t => t.Amount);

            return (income, expenses);
        }

        // -----------------------------------------------------------
        // CATEGORY SUMMARIES
        // -----------------------------------------------------------

        /// <summary>
        /// Returns category summaries for a specific month and year.
        /// Used for "Category Summary – Month Year" reports.
        /// </summary>
        public List<CategorySummary> GetCategorySummariesByMonth(int year, int month)
        {
            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

            if (year < 1)
                throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");

            return _repository.GetCategorySummariesByMonth(year, month);
        }

        /// <summary>
        /// Returns category summaries across ALL transactions.
        /// </summary>
        public List<CategorySummary> GetOverallCategorySummaries()
        {
            return _repository.GetOverallCategorySummaries();
        }
    }
}
