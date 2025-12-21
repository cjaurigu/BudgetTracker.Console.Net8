// File: Domain/MonthlyCategoryBudget.cs
// Namespace: BudgetTracker.Console.Net8.Domain
//
// Purpose:
// Represents a budget assigned to a category for a specific year+month.

namespace BudgetTracker.Console.Net8.Domain
{
    public sealed class MonthlyCategoryBudget
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal BudgetAmount { get; set; }
    }
}
