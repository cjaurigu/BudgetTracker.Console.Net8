// File: Domain/RecurringTransaction.cs
// Namespace: BudgetTracker.Console.Net8.Domain
//
// Purpose:
// Represents a recurring transaction template stored in RecurringTransactions table.

using System;
using BudgetTracker.Console.Net8.Domain.Enums;

namespace BudgetTracker.Console.Net8.Domain
{
    /// <summary>
    /// A recurring template that can generate normal Transactions on a schedule.
    /// </summary>
    public class RecurringTransaction
    {
        public int Id { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        /// <summary>
        /// Stored as "Income" or "Expense" to match the Transactions table.
        /// </summary>
        public string Type { get; set; } = "Expense";

        public string Category { get; set; } = string.Empty;

        public int? CategoryId { get; set; }

        public DateTime StartDate { get; set; }

        public RecurringFrequency Frequency { get; set; }

        /// <summary>
        /// Used for Monthly frequency. Recommended range: 1–28.
        /// </summary>
        public int? DayOfMonth { get; set; }

        public DateTime NextRunDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
