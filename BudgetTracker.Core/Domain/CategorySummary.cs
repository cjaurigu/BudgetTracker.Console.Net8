// File: Domain/CategorySummary.cs
// Namespace: BudgetTracker.Console.Net8.Domain
//
// Purpose:
// This model represents an aggregated view of money by category.
// It is used for reporting (for example, category totals per month).
//
// NOTE:
// This is NOT a table in the database. It is a "view model" used
// by the service and presentation layers for reporting.

using System;

namespace BudgetTracker.Console.Net8.Domain
{
    /// <summary>
    /// Represents a summary of financial activity for a single category.
    /// </summary>
    public class CategorySummary
    {
        /// <summary>
        /// The category name (for example, "Groceries", "Rent", "Salary").
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// Total income amount for this category in the period being reported.
        /// </summary>
        public decimal TotalIncome { get; set; }

        /// <summary>
        /// Total expense amount for this category in the period being reported.
        /// </summary>
        public decimal TotalExpense { get; set; }

        /// <summary>
        /// Net amount = TotalIncome - TotalExpense.
        /// Positive means surplus; negative means overspend.
        /// </summary>
        public decimal NetAmount => TotalIncome - TotalExpense;
    }
}
