// File: Presentation/MainMenu.cs
// Namespace: BudgetTracker.Console.Net8.Presentation
//
// Purpose:
// Orchestrates the console UI:
//  - Shows the main menu
//  - Reads user input
//  - Calls into services (BudgetService, CategoryService)

using System;
using System.Linq;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Domain.Enums;
using BudgetTracker.Console.Net8.Services;
using BudgetTracker.Console.Net8.Data;
using SystemConsole = System.Console;

namespace BudgetTracker.Console.Net8.Presentation
{
    /// <summary>
    /// Main entry-point class for the console UI.
    /// </summary>
    public class MainMenu
    {
        private readonly BudgetService _budgetService;
        private readonly CategoryService _categoryService;

        /// <summary>
        /// Constructs the main menu and wires up repositories + services.
        /// </summary>
        public MainMenu()
        {
            // Build the repositories (data layer).
            var transactionRepo = new TransactionRepository();
            var categoryRepo = new CategoryRepository();

            // Build services (business layer).
            _budgetService = new BudgetService(transactionRepo);
            _categoryService = new CategoryService(categoryRepo);
        }

        /// <summary>
        /// Starts the main menu loop and keeps showing options
        /// until the user chooses to exit.
        /// </summary>
        public void Run()
        {
            bool exit = false;

            while (!exit)
            {
                SystemConsole.Clear();

                SystemConsole.WriteLine("=== Budget Tracker ===");
                SystemConsole.WriteLine("1. Add Transaction");
                SystemConsole.WriteLine("2. View All Transactions");
                SystemConsole.WriteLine("3. View Income Only");
                SystemConsole.WriteLine("4. View Expenses Only");
                SystemConsole.WriteLine("5. View Monthly Summary");
                SystemConsole.WriteLine("6. View Overall Totals");
                SystemConsole.WriteLine("7. Edit Transaction");
                SystemConsole.WriteLine("8. Delete Transaction");
                SystemConsole.WriteLine("9. Manage Categories");
                SystemConsole.WriteLine("10. Search Transactions");
                SystemConsole.WriteLine("11. Exit");
                SystemConsole.WriteLine();

                var choice = InputHelper.ReadInt("Choose an option (1-11): ", 1, 11);

                switch (choice)
                {
                    case 1:
                        AddTransactionFlow();
                        break;
                    case 2:
                        ViewAllFlow();
                        break;
                    case 3:
                        ViewByTypeFlow(TransactionType.Income);
                        break;
                    case 4:
                        ViewByTypeFlow(TransactionType.Expense);
                        break;
                    case 5:
                        ViewMonthlySummaryFlow();
                        break;
                    case 6:
                        ViewOverallTotalsFlow();
                        break;
                    case 7:
                        EditTransactionFlow();
                        break;
                    case 8:
                        DeleteTransactionFlow();
                        break;
                    case 9:
                        ManageCategoriesFlow();
                        break;
                    case 10:
                        SearchTransactionsFlow();
                        break;
                    case 11:
                        exit = true;
                        break;
                }

                if (!exit)
                {
                    SystemConsole.WriteLine();
                    SystemConsole.WriteLine("Press any key to return to the main menu...");
                    SystemConsole.ReadKey();
                }
            }
        }

        // -----------------------------------------------------------
        // OPTION 1: ADD TRANSACTION
        // -----------------------------------------------------------

        /// <summary>
        /// Guides the user through entering a new transaction.
        /// </summary>
        private void AddTransactionFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Add Transaction ===");

            var description = InputHelper.ReadString("Description: ");
            var amount = InputHelper.ReadDecimal("Amount: ");
            var type = InputHelper.ReadTransactionType("Type (1 = Income, 2 = Expense): ");
            var date = InputHelper.ReadDate("Date (YYYY-MM-DD) or blank for today: ");

            // Load all existing categories from the service.
            var categories = _categoryService.GetAllCategories();

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Available categories:");

            if (categories.Count == 0)
            {
                SystemConsole.WriteLine("(No categories yet. You can define some in 'Manage Categories'.)");
            }
            else
            {
                foreach (var c in categories)
                {
                    SystemConsole.WriteLine($"{c.Id}: {c.Name}");
                }
            }

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Options:");
            SystemConsole.WriteLine(" - Enter an existing Category ID to use it");
            SystemConsole.WriteLine(" - Enter 0 to type a custom category");

            var categoryId = InputHelper.ReadInt("Category ID (or 0 for custom): ", 0, int.MaxValue);

            string categoryName;

            if (categoryId == 0)
            {
                SystemConsole.Write("Enter category name: ");
                categoryName = SystemConsole.ReadLine() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    _categoryService.AddCategory(categoryName);
                }
            }
            else
            {
                var selected = categories.FirstOrDefault(c => c.Id == categoryId);

                if (selected == null)
                {
                    SystemConsole.WriteLine("Invalid category ID. Defaulting to 'Uncategorized'.");
                    categoryName = "Uncategorized";
                }
                else
                {
                    categoryName = selected.Name;
                }
            }

            var transaction = new Transaction
            {
                Description = description,
                Amount = amount,
                Type = type == TransactionType.Income ? "Income" : "Expense",
                Category = categoryName,
                Date = date
            };

            _budgetService.AddTransaction(transaction);

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Transaction saved successfully.");
        }

        // -----------------------------------------------------------
        // OPTION 2: VIEW ALL TRANSACTIONS
        // -----------------------------------------------------------

        /// <summary>
        /// Shows all transactions in a table.
        /// </summary>
        private void ViewAllFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== All Transactions ===");

            var all = _budgetService.GetAllTransactions();
            OutputFormatter.PrintTransactions(all);
        }

        // -----------------------------------------------------------
        // OPTION 3–4: VIEW BY TYPE (INCOME / EXPENSE)
        // -----------------------------------------------------------

        /// <summary>
        /// Displays only income or only expenses, based on the TransactionType.
        /// </summary>
        private void ViewByTypeFlow(TransactionType type)
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine(type == TransactionType.Income
                ? "=== Income Transactions ==="
                : "=== Expense Transactions ===");

            var items = _budgetService.GetByType(type);
            OutputFormatter.PrintTransactions(items);
        }

        // -----------------------------------------------------------
        // OPTION 10: SEARCH TRANSACTIONS
        // -----------------------------------------------------------

        /// <summary>
        /// Allows the user to search transactions by a keyword that appears
        /// in the description or category (case-insensitive).
        /// </summary>
        private void SearchTransactionsFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Search Transactions ===");

            var keyword = InputHelper.ReadString("Enter keyword to search in description or category: ");

            var results = _budgetService.SearchTransactions(keyword);

            if (results.Count == 0)
            {
                SystemConsole.WriteLine();
                SystemConsole.WriteLine("No transactions found matching that search.");
                SystemConsole.WriteLine();
            }
            else
            {
                OutputFormatter.PrintTransactions(results);
            }
        }

        // -----------------------------------------------------------
        // OPTION 5: MONTHLY SUMMARY (NOW WITH CATEGORY BREAKDOWN)
        // -----------------------------------------------------------

        /// <summary>
        /// Shows a specific month’s transactions, category breakdown,
        /// and totals (income, expenses, net).
        /// </summary>
        private void ViewMonthlySummaryFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Monthly Summary ===");

            var year = InputHelper.ReadInt("Year (e.g., 2025): ", 2000, 2100);
            var month = InputHelper.ReadInt("Month (1-12): ", 1, 12);

            // 1) All transactions for that month.
            var monthlyItems = _budgetService.GetByMonth(year, month);

            // 2) Totals for that month.
            var (income, expenses) = _budgetService.GetMonthlyTotals(year, month);

            // 3) Category-level breakdown for that month.
            var categorySummaries = _budgetService.GetCategorySummariesByMonth(year, month);

            // Raw transactions.
            OutputFormatter.PrintTransactions(monthlyItems);

            // Category breakdown.
            var monthName = new DateTime(year, month, 1).ToString("MMMM");
            var title = $"Category Summary – {monthName} {year}";
            OutputFormatter.PrintCategorySummaries(categorySummaries, title);

            // Overall totals.
            OutputFormatter.PrintTotals(income, expenses);
        }

        // -----------------------------------------------------------
        // OPTION 6: OVERALL TOTALS (NOW WITH CATEGORY BREAKDOWN)
        // -----------------------------------------------------------

        /// <summary>
        /// Shows overall category breakdown and totals across ALL time.
        /// </summary>
        private void ViewOverallTotalsFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Overall Totals ===");

            var (income, expenses) = _budgetService.GetTotals();
            var categorySummaries = _budgetService.GetOverallCategorySummaries();

            OutputFormatter.PrintCategorySummaries(categorySummaries, "Overall Category Summary (All Time)");
            OutputFormatter.PrintTotals(income, expenses);
        }

        // -----------------------------------------------------------
        // OPTION 7: EDIT TRANSACTION
        // -----------------------------------------------------------

        /// <summary>
        /// Allows user to edit a specific transaction by Id.
        /// </summary>
        private void EditTransactionFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Edit Transaction ===");

            var id = InputHelper.ReadInt("Enter the ID of the transaction to edit: ", 1, int.MaxValue);

            var existing = _budgetService.GetTransactionById(id);
            if (existing is null)
            {
                SystemConsole.WriteLine($"No transaction found with ID {id}.");
                return;
            }

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Current values:");
            SystemConsole.WriteLine($"Description: {existing.Description}");
            SystemConsole.WriteLine($"Amount     : {existing.Amount}");
            SystemConsole.WriteLine($"Type       : {existing.Type}");
            SystemConsole.WriteLine($"Category   : {existing.Category}");
            SystemConsole.WriteLine($"Date       : {existing.Date:yyyy-MM-dd}");
            SystemConsole.WriteLine();

            SystemConsole.WriteLine("Press Enter to KEEP the existing value.");
            SystemConsole.WriteLine();

            SystemConsole.Write($"New description (current: {existing.Description}): ");
            var descInput = SystemConsole.ReadLine();
            if (!string.IsNullOrWhiteSpace(descInput))
                existing.Description = descInput;

            SystemConsole.Write($"New amount (current: {existing.Amount}): ");
            var amountInput = SystemConsole.ReadLine();
            if (!string.IsNullOrWhiteSpace(amountInput) && decimal.TryParse(amountInput, out var newAmount))
                existing.Amount = newAmount;

            SystemConsole.Write($"New type (Income/Expense, current: {existing.Type}): ");
            var typeInput = SystemConsole.ReadLine();
            if (!string.IsNullOrWhiteSpace(typeInput))
            {
                if (typeInput.Equals("Income", StringComparison.OrdinalIgnoreCase) ||
                    typeInput.Equals("Expense", StringComparison.OrdinalIgnoreCase))
                {
                    existing.Type = char.ToUpper(typeInput[0]) + typeInput[1..].ToLower();
                }
                else
                {
                    SystemConsole.WriteLine("Invalid type entered. Keeping existing value.");
                }
            }

            SystemConsole.Write($"New category (current: {existing.Category}): ");
            var categoryInput = SystemConsole.ReadLine();
            if (!string.IsNullOrWhiteSpace(categoryInput))
                existing.Category = categoryInput;

            SystemConsole.Write($"New date (YYYY-MM-DD, current: {existing.Date:yyyy-MM-dd}): ");
            var dateInput = SystemConsole.ReadLine();
            if (!string.IsNullOrWhiteSpace(dateInput) &&
                DateTime.TryParse(dateInput, out var newDate))
            {
                existing.Date = newDate;
            }

            _budgetService.UpdateTransaction(existing);

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Transaction updated successfully.");
        }

        // -----------------------------------------------------------
        // OPTION 8: DELETE TRANSACTION
        // -----------------------------------------------------------

        /// <summary>
        /// Deletes a transaction by ID (with confirmation).
        /// </summary>
        private void DeleteTransactionFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Delete Transaction ===");

            var id = InputHelper.ReadInt("Enter the ID of the transaction to delete: ", 1, int.MaxValue);

            var existing = _budgetService.GetTransactionById(id);
            if (existing is null)
            {
                SystemConsole.WriteLine($"No transaction found with ID {id}.");
                return;
            }

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Transaction to delete:");
            SystemConsole.WriteLine($"ID         : {existing.Id}");
            SystemConsole.WriteLine($"Description: {existing.Description}");
            SystemConsole.WriteLine($"Amount     : {existing.Amount}");
            SystemConsole.WriteLine($"Type       : {existing.Type}");
            SystemConsole.WriteLine($"Category   : {existing.Category}");
            SystemConsole.WriteLine($"Date       : {existing.Date:yyyy-MM-dd}");
            SystemConsole.WriteLine();

            SystemConsole.Write("Are you sure you want to delete this transaction? (Y/N): ");
            var confirm = (SystemConsole.ReadLine() ?? string.Empty).Trim();

            if (confirm.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                confirm.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                _budgetService.DeleteTransaction(id);
                SystemConsole.WriteLine("Transaction deleted.");
            }
            else
            {
                SystemConsole.WriteLine("Delete cancelled.");
            }
        }

        // -----------------------------------------------------------
        // OPTION 9: MANAGE CATEGORIES
        // -----------------------------------------------------------

        /// <summary>
        /// Shows the "Manage Categories" submenu and loops within it
        /// until the user chooses to go back to the main menu.
        /// </summary>
        private void ManageCategoriesFlow()
        {
            bool back = false;

            while (!back)
            {
                SystemConsole.Clear();
                SystemConsole.WriteLine("=== Manage Categories ===");
                SystemConsole.WriteLine("1. View Categories");
                SystemConsole.WriteLine("2. Add Category");
                SystemConsole.WriteLine("3. Rename Category");
                SystemConsole.WriteLine("4. Delete Category");
                SystemConsole.WriteLine("5. Back");
                SystemConsole.WriteLine();

                var choice = InputHelper.ReadInt("Choose an option (1-5): ", 1, 5);

                switch (choice)
                {
                    case 1:
                        ViewCategories();
                        break;
                    case 2:
                        AddCategory();
                        break;
                    case 3:
                        RenameCategory();
                        break;
                    case 4:
                        DeleteCategory();
                        break;
                    case 5:
                        back = true;
                        break;
                }

                if (!back)
                {
                    SystemConsole.WriteLine();
                    SystemConsole.WriteLine("Press any key to continue...");
                    SystemConsole.ReadKey();
                }
            }
        }

        /// <summary>
        /// Displays all categories.
        /// </summary>
        private void ViewCategories()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Categories ===");

            var categories = _categoryService.GetAllCategories();

            if (categories.Count == 0)
            {
                SystemConsole.WriteLine("No categories defined yet.");
            }
            else
            {
                foreach (var c in categories)
                {
                    SystemConsole.WriteLine($"{c.Id}: {c.Name}");
                }
            }
        }

        /// <summary>
        /// Adds a new category.
        /// </summary>
        private void AddCategory()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Add Category ===");

            var name = InputHelper.ReadString("Category name: ");
            _categoryService.AddCategory(name);

            SystemConsole.WriteLine("Category added.");
        }

        /// <summary>
        /// Renames an existing category by Id.
        /// </summary>
        private void RenameCategory()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Rename Category ===");

            var categories = _categoryService.GetAllCategories();
            ViewCategories();

            var id = InputHelper.ReadInt("Enter the ID of the category to rename: ", 1, int.MaxValue);
            var existing = categories.FirstOrDefault(c => c.Id == id);

            if (existing == null)
            {
                SystemConsole.WriteLine($"No category found with ID {id}.");
                return;
            }

            var newName = InputHelper.ReadString($"New name for '{existing.Name}': ");
            _categoryService.RenameCategory(id, newName);

            SystemConsole.WriteLine("Category renamed.");
        }

        /// <summary>
        /// Deletes a category by Id (no transaction remap yet).
        /// </summary>
        private void DeleteCategory()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Delete Category ===");

            var categories = _categoryService.GetAllCategories();
            ViewCategories();

            var id = InputHelper.ReadInt("Enter the ID of the category to delete: ", 1, int.MaxValue);
            var existing = categories.FirstOrDefault(c => c.Id == id);

            if (existing == null)
            {
                SystemConsole.WriteLine($"No category found with ID {id}.");
                return;
            }

            SystemConsole.Write($"Are you sure you want to delete '{existing.Name}'? (Y/N): ");
            var confirm = (SystemConsole.ReadLine() ?? string.Empty).Trim();

            if (confirm.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                confirm.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                _categoryService.DeleteCategory(id);
                SystemConsole.WriteLine("Category deleted.");
            }
            else
            {
                SystemConsole.WriteLine("Delete cancelled.");
            }
        }
    }
}
