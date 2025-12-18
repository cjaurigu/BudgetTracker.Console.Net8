// File: Domain/Transaction.cs
// Namespace: BudgetTracker.Console.Net8.Domain
//
// Purpose:
// Represents a single financial transaction in the system.
// This model is used by the data layer (SQL), service layer, and UI.

using System;

namespace BudgetTracker.Console.Net8.Domain
{
    /// <summary>
    /// Represents an individual financial transaction (income or expense).
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Primary key from the Transactions table.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Short description of the transaction (for example, "Rent", "Groceries").
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Monetary amount of the transaction.
        /// Always stored as a positive value; Type determines direction.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Type of transaction: "Income" or "Expense".
        /// (The UI uses an enum, but we store the string here for simplicity.)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable category name (for example, "Rent", "Groceries").
        /// This is kept for readability and historical reasons.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Normalized foreign key to the Categories table.
        /// Matches Categories.Id when available.
        /// Nullable because older records or imported data might not have it.
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Date the transaction occurred.
        /// </summary>
        public DateTime Date { get; set; }
    }
}
