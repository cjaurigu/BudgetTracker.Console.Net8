// File: Presentation/OutputFormatter.cs
// Namespace: BudgetTracker.Console.Net8.Presentation
//
// Purpose:
// Central place for formatting output to the console so the UI looks
// consistent and easy to read.

using System.Collections.Generic;
using BudgetTracker.Console.Net8.Domain;
using SystemConsole = System.Console;

namespace BudgetTracker.Console.Net8.Presentation
{
    /// <summary>
    /// Static helper class for printing tables and summaries.
    /// </summary>
    public static class OutputFormatter
    {
        /// <summary>
        /// Prints a list of transactions in a table with columns:
        /// Id, Date, Type, Category, Description, Amount.
        /// </summary>
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
                    (t.Category ?? string.Empty).Length > 19
                        ? t.Category!.Substring(0, 19)
                        : t.Category,
                    (t.Description ?? string.Empty).Length > 32
                        ? t.Description!.Substring(0, 32)
                        : t.Description,
                    t.Amount);
            }

            SystemConsole.WriteLine();
        }

        /// <summary>
        /// Prints summary totals: total income, total expenses, and net.
        /// </summary>
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

        /// <summary>
        /// Prints a table of category summaries:
        ///   Category | Total Income | Total Expense | Net
        /// 
        /// This is used for:
        ///   - Monthly category breakdowns
        ///   - Overall (all-time) category breakdowns
        /// </summary>
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
    }
}
