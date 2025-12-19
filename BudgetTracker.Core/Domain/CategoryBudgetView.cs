// File: Domain/CategoryBudgetView.cs
// Namespace: BudgetTracker.Console.Net8.Domain
//
// Purpose:
// A read/display model used to show budgets WITH category names.
// Returned from CategoryBudgetRepository.GetAllWithCategoryNames().

namespace BudgetTracker.Console.Net8.Domain
{
    /// <summary>
    /// Represents a budget row joined to a category name for display.
    /// </summary>
    public class CategoryBudgetView
    {
        /// <summary>
        /// The category Id (Categories.Id).
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// The category name (Categories.Name).
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// The saved budget amount for this category.
        /// </summary>
        public decimal BudgetAmount { get; set; }
    }
}
