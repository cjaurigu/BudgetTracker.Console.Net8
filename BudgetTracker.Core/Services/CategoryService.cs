// File: Services/CategoryService.cs
// Namespace: BudgetTracker.Console.Net8.Services
//
// Purpose:
// Business logic for category management.
// Ensures names are valid, prevents duplicates, and calls the repository correctly.

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
        private readonly CategoryRepository _repository;

        public CategoryService(CategoryRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Returns all categories sorted by name.
        /// </summary>
        public List<Category> GetAllCategories()
        {
            return _repository.GetAll();
        }

        /// <summary>
        /// Returns a category by Id, or null if not found.
        /// </summary>
        public Category? GetCategoryById(int id)
        {
            if (id <= 0) return null;
            return _repository.GetById(id);
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
            var existing = _repository.GetByName(name);
            if (existing != null)
                return existing;

            // IMPORTANT: Repository Add expects a string name (not a Category object).
            _repository.Add(name);

            // Return the inserted row (simple re-query by name).
            return _repository.GetByName(name)
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
            var existing = _repository.GetById(id);
            if (existing == null)
                throw new InvalidOperationException($"Category not found (Id: {id}).");

            // Prevent renaming into a duplicate name.
            var nameCheck = _repository.GetByName(newName);
            if (nameCheck != null && nameCheck.Id != id)
                throw new InvalidOperationException("A category with that name already exists.");

            // IMPORTANT: CategoryRepository does not have Update() in your project.
            // We use Rename(id, newName) which we implement below in the repository.
            _repository.Rename(id, newName);
        }

        /// <summary>
        /// Deletes a category by Id.
        /// </summary>
        public void DeleteCategory(int id)
        {
            if (id <= 0)
                return;

            _repository.Delete(id);
        }
    }
}
