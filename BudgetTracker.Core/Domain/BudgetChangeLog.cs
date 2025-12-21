// File: Domain/BudgetChangeLog.cs
// Namespace: BudgetTracker.Console.Net8.Domain
//
// Purpose:
// Stores a history record each time a monthly budget changes.

using System;

namespace BudgetTracker.Console.Net8.Domain
{
    public sealed class BudgetChangeLog
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }

        public decimal OldAmount { get; set; }
        public decimal NewAmount { get; set; }

        public DateTime ChangedAtUtc { get; set; }

        /// <summary>
        /// Example values: "Update", "Clear"
        /// </summary>
        public string Action { get; set; } = "Update";
    }
}
