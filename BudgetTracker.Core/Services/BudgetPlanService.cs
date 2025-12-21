// File: Services/BudgetPlanService.cs
// Namespace: BudgetTracker.Console.Net8.Services
//
// Purpose:
// Business logic for managing category budgets (Phase 1: one budget per category).
// This service validates inputs, checks category existence, and delegates SQL work
// to CategoryBudgetRepository.

using System;
using System.Collections.Generic;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Services
{
    /// <summary>
    /// Handles business rules around budgets (set/update/delete/view).
    /// Phase 1 design: each category can have only one budget amount.
    /// </summary>
    public class BudgetPlanService
    {
        private readonly CategoryBudgetRepository _budgetRepo;
        private readonly CategoryRepository _categoryRepo;

        /// <summary>
        /// Creates the service with the required repositories.
        /// </summary>
        public BudgetPlanService(CategoryBudgetRepository budgetRepo, CategoryRepository categoryRepo)
        {
            _budgetRepo = budgetRepo ?? throw new ArgumentNullException(nameof(budgetRepo));
            _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        }

        // ---------------------------------------------------------
        // SET / UPDATE
        // ---------------------------------------------------------

        /// <summary>
        /// Sets or updates a budget for a category.
        /// If a budget already exists for that category, it updates it.
        /// If not, it inserts a new budget row.
        /// </summary>
        /// <param name="categoryId">The Categories.Id value.</param>
        /// <param name="amount">Budget amount (must be >= 0).</param>
        public void SetBudget(int categoryId, decimal amount)
        {
            if (categoryId <= 0)
                throw new ArgumentException("CategoryId must be greater than zero.", nameof(categoryId));

            if (amount < 0)
                throw new ArgumentException("Budget amount must be zero or positive.", nameof(amount));

            // Important rule: don’t allow budgets for categories that don’t exist.
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
                // Keeps UPDATE logic centralized in the repository.
                _budgetRepo.UpdateByCategoryId(categoryId, amount);
            }
        }

        // ---------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------

        /// <summary>
        /// Deletes the budget for a category (if it exists).
        /// </summary>
        public void DeleteBudget(int categoryId)
        {
            if (categoryId <= 0)
                return;

            _budgetRepo.DeleteByCategoryId(categoryId);
        }

        // ---------------------------------------------------------
        // GETTERS
        // ---------------------------------------------------------

        /// <summary>
        /// Returns the raw budget record for a category, or null if not found.
        /// </summary>
        public CategoryBudget? GetBudget(int categoryId)
        {
            if (categoryId <= 0)
                return null;

            return _budgetRepo.GetByCategoryId(categoryId);
        }

        /// <summary>
        /// Returns all budgets joined with category names (ideal for UI display).
        /// </summary>
        public List<CategoryBudgetView> GetAllBudgetsWithNames()
        {
            return _budgetRepo.GetAllWithCategoryNames();
        }

        // ---------------------------------------------------------
        // PHASE A ADDITIONS (Overspend checks)
        // ---------------------------------------------------------

        /// <summary>
        /// Tries to fetch the saved budget amount for the given category.
        /// Returns true if a budget exists; otherwise false.
        /// </summary>
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
    }
}
