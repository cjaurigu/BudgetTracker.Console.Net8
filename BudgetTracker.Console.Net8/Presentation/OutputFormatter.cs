// File: Presentation/OutputFormatter.cs
// Namespace: BudgetTracker.Console.Net8.Presentation
//
// Purpose:
// Central place for formatting output to the console so the UI looks
// consistent and easy to read.

using System;
using System.Collections.Generic;
using System.Linq;
using BudgetTracker.Console.Net8.Domain;
using SystemConsole = System.Console;

namespace BudgetTracker.Console.Net8.Presentation
{
    /// <summary>
    /// Static helper class for printing tables and summaries.
    /// </summary>
    public static class OutputFormatter
    {
        public static void PrintTransactions(List<Transaction> transactions)
        {
            SystemConsole.WriteLine();

            if (transactions == null || transactions.Count == 0)
            {
                SystemConsole.WriteLine("No transactions found.");
                SystemConsole.WriteLine();
                return;
            }

            SystemConsole.WriteLine("ID   Date         Type     Category            Description                      Amount");
            SystemConsole.WriteLine("---- ------------ -------- ------------------- -------------------------------- ----------");

            foreach (var t in transactions)
            {
                SystemConsole.WriteLine(
                    "{0,-4} {1:yyyy-MM-dd} {2,-8} {3,-19} {4,-32} {5,10:C}",
                    t.Id,
                    t.Date,
                    t.Type,
                    (t.Category ?? string.Empty).Length > 19 ? t.Category!.Substring(0, 19) : t.Category,
                    (t.Description ?? string.Empty).Length > 32 ? t.Description!.Substring(0, 32) : t.Description,
                    t.Amount);
            }

            SystemConsole.WriteLine();
        }

        public static void PrintTotals(decimal income, decimal expenses)
        {
            var net = income - expenses;

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("=== Totals ===");
            SystemConsole.WriteLine($"Total Income : {income,10:C}");
            SystemConsole.WriteLine($"Total Expense: {expenses,10:C}");
            SystemConsole.WriteLine($"Net Balance  : {net,10:C}");
            SystemConsole.WriteLine();
        }

        public static void PrintCategorySummaries(List<CategorySummary> summaries, string title)
        {
            SystemConsole.WriteLine();
            SystemConsole.WriteLine(title);
            SystemConsole.WriteLine(new string('=', title.Length));
            SystemConsole.WriteLine();

            if (summaries == null || summaries.Count == 0)
            {
                SystemConsole.WriteLine("No category data available for this period.");
                SystemConsole.WriteLine();
                return;
            }

            SystemConsole.WriteLine("{0,-25} {1,15} {2,15} {3,15}",
                "Category",
                "Total Income",
                "Total Expense",
                "Net");

            SystemConsole.WriteLine(new string('-', 25 + 15 + 15 + 15 + 3));

            foreach (var s in summaries)
            {
                SystemConsole.WriteLine("{0,-25} {1,15:C} {2,15:C} {3,15:C}",
                    s.CategoryName,
                    s.TotalIncome,
                    s.TotalExpense,
                    s.NetAmount);
            }

            SystemConsole.WriteLine();
        }

        // ---------------------------------------------------------
        // BUDGET OUTPUT
        // ---------------------------------------------------------

        /// <summary>
        /// Prints all saved budgets (Category + BudgetAmount).
        /// </summary>
        public static void PrintAllBudgets(List<CategoryBudgetView> budgets)
        {
            SystemConsole.WriteLine();

            if (budgets == null || budgets.Count == 0)
            {
                SystemConsole.WriteLine("No budgets have been set yet.");
                SystemConsole.WriteLine();
                return;
            }

            SystemConsole.WriteLine("{0,-6} {1,-25} {2,12}", "Id", "Category", "Budget");
            SystemConsole.WriteLine(new string('-', 6 + 25 + 12 + 4));

            foreach (var b in budgets)
            {
                SystemConsole.WriteLine("{0,-6} {1,-25} {2,12:C}",
                    b.CategoryId,
                    b.CategoryName,
                    b.BudgetAmount);
            }

            SystemConsole.WriteLine();
        }

        /// <summary>
        /// Prints a monthly "Budget vs Actual" report.
        /// Actual = TotalExpense from the category summary.
        /// </summary>
        public static void PrintMonthlyBudgetReport(
            List<CategorySummary> categorySummaries,
            List<CategoryBudgetView> budgets,
            string title)
        {
            SystemConsole.WriteLine();
            SystemConsole.WriteLine(title);
            SystemConsole.WriteLine(new string('=', title.Length));
            SystemConsole.WriteLine();

            if (categorySummaries == null || categorySummaries.Count == 0)
            {
                SystemConsole.WriteLine("No transactions for this month, so no budget report can be created.");
                SystemConsole.WriteLine();
                return;
            }

            // Join budgets by CategoryName (Phase 1).
            // Later, we can join by CategoryId once summaries include CategoryId.
            var budgetLookup = (budgets ?? new List<CategoryBudgetView>())
                .ToDictionary(b => b.CategoryName, b => b.BudgetAmount, StringComparer.OrdinalIgnoreCase);

            SystemConsole.WriteLine("{0,-25} {1,12} {2,12} {3,12} {4,8}",
                "Category",
                "Budget",
                "Actual",
                "Remaining",
                "% Used");

            SystemConsole.WriteLine(new string('-', 25 + 12 + 12 + 12 + 8 + 4));

            foreach (var s in categorySummaries.OrderBy(x => x.CategoryName))
            {
                var actual = s.TotalExpense;

                decimal? budget = null;
                if (budgetLookup.TryGetValue(s.CategoryName, out var budgetAmount))
                    budget = budgetAmount;

                string budgetText = budget.HasValue ? budget.Value.ToString("C") : "-";

                decimal? remaining = budget.HasValue ? budget.Value - actual : null;
                string remainingText = remaining.HasValue ? remaining.Value.ToString("C") : "-";

                decimal? percentUsed = null;
                if (budget.HasValue && budget.Value > 0)
                    percentUsed = (actual / budget.Value) * 100m;

                string percentText = percentUsed.HasValue ? $"{Math.Round(percentUsed.Value, 0)}%" : "-";

                SystemConsole.WriteLine("{0,-25} {1,12} {2,12:C} {3,12} {4,8}",
                    s.CategoryName,
                    budgetText,
                    actual,
                    remainingText,
                    percentText);
            }

            SystemConsole.WriteLine();
        }
    }
}
