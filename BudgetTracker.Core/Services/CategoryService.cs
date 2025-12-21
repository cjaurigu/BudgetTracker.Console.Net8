// File: Services/CategoryService.cs
// Namespace: BudgetTracker.Console.Net8.Services
//
// Purpose:
// Business logic for category management.
// Ensures names are valid, prevents duplicates, and calls the repository correctly.
// ALSO:
// Implements "safe delete" by reassigning transactions to "Uncategorized" before deleting.

using System;
using System.Collections.Generic;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Services
{
    /// <summary>
    /// Category business rules and validation.
    /// </summary>
    public class CategoryService
    {
        private readonly CategoryRepository _categoryRepo;
        private readonly TransactionRepository _transactionRepo;

        public CategoryService(CategoryRepository repository)
        {
            _categoryRepo = repository ?? throw new ArgumentNullException(nameof(repository));

            // Keep existing call sites working (WPF and Console already call: new CategoryService(new CategoryRepository()))
            // We create the TransactionRepository internally so DeleteCategory can safely reassign transactions.
            _transactionRepo = new TransactionRepository();
        }

        /// <summary>
        /// Returns all categories sorted by name.
        /// </summary>
        public List<Category> GetAllCategories()
        {
            return _categoryRepo.GetAll();
        }

        /// <summary>
        /// Returns a category by Id, or null if not found.
        /// </summary>
        public Category? GetCategoryById(int id)
        {
            if (id <= 0) return null;
            return _categoryRepo.GetById(id);
        }

        /// <summary>
        /// Adds a category if it does not already exist.
        /// Returns the existing category if it already exists.
        /// </summary>
        public Category AddCategory(string name)
        {
            name = (name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name cannot be empty.", nameof(name));

            // Prevent duplicates (case-insensitive behavior depends on DB collation,
            // so we still check here to keep UX clean).
            var existing = _categoryRepo.GetByName(name);
            if (existing != null)
                return existing;

            // IMPORTANT: Repository Add expects a string name (not a Category object).
            _categoryRepo.Add(name);

            // Return the inserted row (simple re-query by name).
            return _categoryRepo.GetByName(name)
                   ?? new Category { Name = name }; // fallback (should rarely happen)
        }

        /// <summary>
        /// Renames a category.
        /// </summary>
        public void RenameCategory(int id, string newName)
        {
            if (id <= 0)
                throw new ArgumentException("Category Id must be greater than zero.", nameof(id));

            newName = (newName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New category name cannot be empty.", nameof(newName));

            // Ensure category exists.
            var existing = _categoryRepo.GetById(id);
            if (existing == null)
                throw new InvalidOperationException($"Category not found (Id: {id}).");

            // Prevent renaming into a duplicate name.
            var nameCheck = _categoryRepo.GetByName(newName);
            if (nameCheck != null && nameCheck.Id != id)
                throw new InvalidOperationException("A category with that name already exists.");

            _categoryRepo.Rename(id, newName);
        }

        /// <summary>
        /// Deletes a category by Id.
        ///
        /// User-friendly rule:
        /// If transactions reference this category, we auto-reassign them to "Uncategorized",
        /// then delete the category safely.
        /// </summary>
        public void DeleteCategory(int id)
        {
            if (id <= 0)
                return;

            var category = _categoryRepo.GetById(id);
            if (category == null)
                return;

            // Optional guard: don't allow deleting the "Uncategorized" bucket.
            if (category.Name.Equals("Uncategorized", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("You cannot delete the 'Uncategorized' category.");

            // 1) Ensure "Uncategorized" exists (create it if not)
            var uncategorized = _categoryRepo.GetByName("Uncategorized");
            if (uncategorized == null)
            {
                _categoryRepo.Add("Uncategorized");
                uncategorized = _categoryRepo.GetByName("Uncategorized");
            }

            if (uncategorized == null)
                throw new InvalidOperationException("Could not create or retrieve the 'Uncategorized' category.");

            // 2) Reassign all transactions that point to this category -> Uncategorized
            // This prevents the FK conflict you saw.
            _transactionRepo.ReassignCategory(id, uncategorized.Id);

            // 3) Now delete the category safely
            _categoryRepo.Delete(id);
        }
    }
}
