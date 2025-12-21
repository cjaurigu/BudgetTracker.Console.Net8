namespace BudgetTracker.Wpf.Net8.ViewModels
{
    /// <summary>
    /// UI model for the Budgets tab grid.
    /// </summary>
    public sealed class BudgetRow
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public decimal BudgetAmount { get; set; }
        public decimal SpentAmount { get; set; }

        public decimal RemainingAmount => BudgetAmount - SpentAmount;
    }
}
