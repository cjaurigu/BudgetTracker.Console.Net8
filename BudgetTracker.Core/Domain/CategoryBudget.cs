// File: Domain/CategoryBudget.cs
// Namespace: BudgetTracker.Console.Net8.Domain
//
// Purpose:
// Represents a budget assigned to a category.
// This table supports "one budget per category" (Phase 1).

namespace BudgetTracker.Console.Net8.Domain
{
    public class CategoryBudget
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the Categories table.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// The budgeted monthly amount for this category.
        /// </summary>
        public decimal BudgetAmount { get; set; }
    }
}
