// File: Services/BudgetCarryOverService.cs
// Namespace: BudgetTracker.Console.Net8.Services
//
// Purpose:
// Computes and applies carry-over of unused monthly budgets into "Savings" next month.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Services
{
    public sealed class BudgetCarryOverService
    {
        private readonly BudgetPlanService _budgetPlanService;
        private readonly BudgetService _budgetService;
        private readonly CategoryRepository _categoryRepo;
        private readonly BudgetCarryOverRunRepository _runRepo;

        public BudgetCarryOverService(
            BudgetPlanService budgetPlanService,
            BudgetService budgetService,
            CategoryRepository categoryRepo,
            BudgetCarryOverRunRepository runRepo)
        {
            _budgetPlanService = budgetPlanService ?? throw new ArgumentNullException(nameof(budgetPlanService));
            _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
            _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
            _runRepo = runRepo ?? throw new ArgumentNullException(nameof(runRepo));
        }

        /// <summary>
        /// Returns a per-category preview of what will roll over into Savings.
        /// Only categories with a positive remaining amount are included.
        /// </summary>
        public List<CarryOverPreviewItem> PreviewCarryOver(int fromYear, int fromMonth)
        {
            ValidateYearMonth(fromYear, fromMonth);

            var categories = _categoryRepo.GetAll();
            var results = new List<CarryOverPreviewItem>();

            foreach (var c in categories)
            {
                // Only carry over if a monthly budget exists for that category
                if (!_budgetPlanService.TryGetMonthlyBudgetAmount(c.Id, fromYear, fromMonth, out var budgetAmount))
                    continue;

                if (budgetAmount <= 0m)
                    continue;

                var spent = _budgetService.GetTotalExpensesForCategoryMonth(c.Id, fromYear, fromMonth);
                var remaining = budgetAmount - spent;

                if (remaining > 0m)
                {
                    results.Add(new CarryOverPreviewItem
                    {
                        CategoryId = c.Id,
                        CategoryName = c.Name,
                        CarryOverAmount = remaining
                    });
                }
            }

            return results
                .OrderByDescending(x => x.CarryOverAmount)
                .ThenBy(x => x.CategoryName)
                .ToList();
        }

        public decimal PreviewCarryOverTotal(int fromYear, int fromMonth)
        {
            var lines = PreviewCarryOver(fromYear, fromMonth);
            return lines.Sum(x => x.CarryOverAmount);
        }

        /// <summary>
        /// Applies carry-over once per month:
        /// - Creates an Income transaction in "Savings" on the first day of next month
        /// - Writes a run record in BudgetCarryOverRuns to prevent duplicates
        /// </summary>
        public decimal ApplyCarryOverToSavings(int fromYear, int fromMonth)
        {
            ValidateYearMonth(fromYear, fromMonth);

            if (_runRepo.Exists(fromYear, fromMonth))
            {
                throw new InvalidOperationException(
                    $"Carry-over has already been applied for {fromYear}/{fromMonth:00}.");
            }

            var lines = PreviewCarryOver(fromYear, fromMonth);
            var total = lines.Sum(x => x.CarryOverAmount);

            // Nothing to carry over? Record nothing and do nothing.
            if (total <= 0m)
                return 0m;

            var nextMonthDate = new DateTime(fromYear, fromMonth, 1).AddMonths(1);

            var tx = new Transaction
            {
                Date = nextMonthDate,
                Description = $"Budget carry-over from {fromYear}/{fromMonth:00}",
                Amount = total,
                Type = "Income",
                Category = "Savings",
                CategoryId = null // repo will resolve/create category by name
            };

            _budgetService.AddTransaction(tx);

            // Safety marker (prevents double-apply)
            _runRepo.Add(fromYear, fromMonth, total);

            return total;
        }

        private static void ValidateYearMonth(int year, int month)
        {
            if (year < 1)
                throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");

            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }
    }
}
