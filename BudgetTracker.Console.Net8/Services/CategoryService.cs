using System;
using System.Collections.Generic;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;

namespace BudgetTracker.Console.Net8.Services
{
    /// <summary>
    /// The CategoryService applies business rules on top of the repository layer.
    /// It does NOT talk directly to SQL — it delegates that to CategoryRepository.
    /// 
    /// Responsibilities:
    /// - Validation
    /// - Avoiding duplicates
    /// - Returning domain models to the UI
    /// - Coordinating input/output flow
    /// </summary>
    public class CategoryService
    {
        private readonly CategoryRepository _repository;

        public CategoryService(CategoryRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Returns every category in the system.
        ///</summary>
        public List<Category> GetAllCategories() => _repository.GetAll();

        /// <summary>
        /// Gets a single category using its ID.
        /// </summary>
        public Category? GetCategoryById(int id) => _repository.GetById(id);

        /// <summary>
        /// Adds a category — but includes business logic:
        /// - Normalize whitespace
        /// - Prevent empty names
        /// - Prevent duplicate names
        /// </summary>
        public Category AddCategory(string name)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name cannot be empty.", nameof(name));

            // Prevent duplicate categories
            var existing = _repository.GetByName(name);
            if (existing != null)
                return existing;

            var category = new Category { Name = name };
            _repository.Add(category);

            // Read the inserted row to get its ID
            return _repository.GetByName(name)!;
        }

        /// <summary>
        /// Renames a category.
        /// </summary>
        public void RenameCategory(int id, string newName)
        {
            newName = newName.Trim();

            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New name cannot be empty.");

            var category = _repository.GetById(id);
            if (category == null)
                return; // No-op if category doesn't exist

            category.Name = newName;

            _repository.Update(category);
        }

        /// <summary>
        /// Deletes a category by ID.
        /// If the category is referenced by transactions, FK rules apply.
        /// </summary>
        public void DeleteCategory(int id)
        {
            _repository.Delete(id);
        }
    }
}
