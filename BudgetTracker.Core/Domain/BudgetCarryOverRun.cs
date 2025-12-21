// File: Domain/BudgetCarryOverRun.cs
// Namespace: BudgetTracker.Console.Net8.Domain
//
// Purpose:
// Records that a given month’s carry-over was applied (prevents duplicates).

using System;

namespace BudgetTracker.Console.Net8.Domain
{
    public sealed class BudgetCarryOverRun
    {
        public int Id { get; set; }
        public int FromYear { get; set; }
        public int FromMonth { get; set; }
        public DateTime AppliedAtUtc { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
