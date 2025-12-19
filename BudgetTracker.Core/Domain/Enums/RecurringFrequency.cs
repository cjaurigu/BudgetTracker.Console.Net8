// File: Domain/Enums/RecurringFrequency.cs
// Namespace: BudgetTracker.Console.Net8.Domain.Enums
//
// Purpose:
// Strongly-typed frequency options for recurring transaction templates.

namespace BudgetTracker.Console.Net8.Domain.Enums
{
    /// <summary>
    /// Frequency options supported by recurring transactions.
    /// </summary>
    public enum RecurringFrequency
    {
        Weekly = 1,
        BiWeekly = 2,
        Monthly = 3
    }
}
