// File: Services/BudgetPlanService.cs
// Namespace: BudgetTracker.Console.Net8.Services
//
// Purpose:
// Business logic for managing budgets.
// - Phase 1: default budget per category (CategoryBudgets)
// - Phase B: monthly budget per category (MonthlyCategoryBudgets)
// - Phase C: change history logging (BudgetChangeLog)

using System;
using System.Collections.Generic;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Services
{
    public class BudgetPlanService
    {
        private readonly CategoryBudgetRepository _budgetRepo;                 // Phase 1 (defaults)
        private readonly MonthlyCategoryBudgetRepository _monthlyBudgetRepo;   // Phase B (per-month)
        private readonly BudgetChangeLogRepository _changeLogRepo;             // Phase C (history)
        private readonly CategoryRepository _categoryRepo;

        // Backward-compatible constructor (used by your WPF code today)
        public BudgetPlanService(
            CategoryBudgetRepository budgetRepo,
            MonthlyCategoryBudgetRepository monthlyBudgetRepo,
            CategoryRepository categoryRepo)
            : this(budgetRepo, monthlyBudgetRepo, new BudgetChangeLogRepository(), categoryRepo)
        {
        }

        public BudgetPlanService(
            CategoryBudgetRepository budgetRepo,
            MonthlyCategoryBudgetRepository monthlyBudgetRepo,
            BudgetChangeLogRepository changeLogRepo,
            CategoryRepository categoryRepo)
        {
            _budgetRepo = budgetRepo ?? throw new ArgumentNullException(nameof(budgetRepo));
            _monthlyBudgetRepo = monthlyBudgetRepo ?? throw new ArgumentNullException(nameof(monthlyBudgetRepo));
            _changeLogRepo = changeLogRepo ?? throw new ArgumentNullException(nameof(changeLogRepo));
            _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        }

        // ---------------------------------------------------------
        // PHASE 1 (Defaults) - kept for later features
        // ---------------------------------------------------------
        public void SetBudget(int categoryId, decimal amount)
        {
            if (categoryId <= 0)
                throw new ArgumentException("CategoryId must be greater than zero.", nameof(categoryId));

            if (amount < 0)
                throw new ArgumentException("Budget amount must be zero or positive.", nameof(amount));

            var category = _categoryRepo.GetById(categoryId);
            if (category == null)
                throw new InvalidOperationException($"Category does not exist (Id: {categoryId}).");

            var existing = _budgetRepo.GetByCategoryId(categoryId);

            if (existing == null)
            {
                _budgetRepo.Add(new CategoryBudget
                {
                    CategoryId = categoryId,
                    BudgetAmount = amount
                });
            }
            else
            {
                _budgetRepo.UpdateByCategoryId(categoryId, amount);
            }
        }

        public void DeleteBudget(int categoryId)
        {
            if (categoryId <= 0)
                return;

            _budgetRepo.DeleteByCategoryId(categoryId);
        }

        public CategoryBudget? GetBudget(int categoryId)
        {
            if (categoryId <= 0)
                return null;

            return _budgetRepo.GetByCategoryId(categoryId);
        }

        public List<CategoryBudgetView> GetAllBudgetsWithNames()
            => _budgetRepo.GetAllWithCategoryNames();

        public bool TryGetBudgetAmount(int categoryId, out decimal budgetAmount)
        {
            budgetAmount = 0m;

            if (categoryId <= 0)
                return false;

            var budget = _budgetRepo.GetByCategoryId(categoryId);
            if (budget == null)
                return false;

            budgetAmount = budget.BudgetAmount;
            return true;
        }

        // ---------------------------------------------------------
        // PHASE B (Monthly Budgets)
        // ---------------------------------------------------------
        public List<CategoryBudgetView> GetMonthlyBudgetsWithNames(int year, int month)
        {
            ValidateYearMonth(year, month);
            return _monthlyBudgetRepo.GetAllWithCategoryNames(year, month);
        }

        public void SetMonthlyBudget(int categoryId, int year, int month, decimal amount)
        {
            ValidateYearMonth(year, month);

            if (categoryId <= 0)
                throw new ArgumentException("CategoryId must be greater than zero.", nameof(categoryId));

            if (amount < 0)
                throw new ArgumentException("Budget amount must be zero or positive.", nameof(amount));

            var category = _categoryRepo.GetById(categoryId);
            if (category == null)
                throw new InvalidOperationException($"Category does not exist (Id: {categoryId}).");

            // Capture old value (if any) before writing
            var existing = _monthlyBudgetRepo.Get(categoryId, year, month);
            var oldAmount = existing?.BudgetAmount ?? 0m;

            // Write
            _monthlyBudgetRepo.Upsert(categoryId, year, month, amount);

            // Log only if it truly changed
            if (oldAmount != amount)
            {
                _changeLogRepo.Add(new BudgetChangeLog
                {
                    CategoryId = categoryId,
                    Year = year,
                    Month = month,
                    OldAmount = oldAmount,
                    NewAmount = amount,
                    Action = "Update"
                });
            }
        }

        public void DeleteMonthlyBudget(int categoryId, int year, int month)
        {
            ValidateYearMonth(year, month);

            if (categoryId <= 0)
                return;

            // Capture old before delete
            var existing = _monthlyBudgetRepo.Get(categoryId, year, month);
            var oldAmount = existing?.BudgetAmount ?? 0m;

            _monthlyBudgetRepo.Delete(categoryId, year, month);

            // Only log if there was something to clear
            if (existing != null && oldAmount != 0m)
            {
                _changeLogRepo.Add(new BudgetChangeLog
                {
                    CategoryId = categoryId,
                    Year = year,
                    Month = month,
                    OldAmount = oldAmount,
                    NewAmount = 0m,
                    Action = "Clear"
                });
            }
        }

        public bool TryGetMonthlyBudgetAmount(int categoryId, int year, int month, out decimal budgetAmount)
        {
            budgetAmount = 0m;

            ValidateYearMonth(year, month);

            if (categoryId <= 0)
                return false;

            var row = _monthlyBudgetRepo.Get(categoryId, year, month);
            if (row == null)
                return false;

            budgetAmount = row.BudgetAmount;
            return true;
        }

        // Phase C: expose history for later UI
        public List<BudgetChangeLog> GetMonthlyBudgetHistory(int categoryId, int year, int month, int maxRows = 50)
        {
            ValidateYearMonth(year, month);

            if (categoryId <= 0)
                return new List<BudgetChangeLog>();

            return _changeLogRepo.GetForCategoryMonth(categoryId, year, month, maxRows);
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
