// File: Domain/CarryOverPreviewItem.cs
// Namespace: BudgetTracker.Console.Net8.Domain
//
// Purpose:
// Represents a single category's carry-over amount for a month.

namespace BudgetTracker.Console.Net8.Domain
{
    public sealed class CarryOverPreviewItem
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// Positive amount that will roll into savings.
        /// </summary>
        public decimal CarryOverAmount { get; set; }
    }
}
    