// File: Presentation/MainMenu.cs
// Namespace: BudgetTracker.Console.Net8.Presentation
//
// Purpose:
// Orchestrates the console UI for the BudgetTracker app:
// - Transactions (CRUD + summaries)
// - Categories (CRUD)
// - Budgets (set/view/delete + budget report)
// - Recurring Transactions (templates + run due items)

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
    /// Main console menu for the application.
    /// </summary>
    public class MainMenu
    {
        private readonly BudgetService _budgetService;
        private readonly CategoryService _categoryService;
        private readonly BudgetPlanService _budgetPlanService;

        private readonly RecurringTransactionService _recurringService;

        /// <summary>
        /// Wires up repositories + services.
        /// </summary>
        public MainMenu()
        {
            // Data layer
            var transactionRepo = new TransactionRepository();
            var categoryRepo = new CategoryRepository();
            var budgetRepo = new CategoryBudgetRepository();
            var recurringRepo = new RecurringTransactionRepository();

            // Service layer
            _budgetService = new BudgetService(transactionRepo);
            _categoryService = new CategoryService(categoryRepo);
            _budgetPlanService = new BudgetPlanService(budgetRepo, categoryRepo);

            _recurringService = new RecurringTransactionService(recurringRepo, _budgetService);
        }

        /// <summary>
        /// Starts the main menu loop.
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
                SystemConsole.WriteLine("11. Manage Budgets");
                SystemConsole.WriteLine("12. Recurring Transactions");
                SystemConsole.WriteLine("13. Exit");
                SystemConsole.WriteLine();

                var choice = InputHelper.ReadInt("Choose an option (1-13): ", 1, 13);

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
                        ManageBudgetsFlow();
                        break;
                    case 12:
                        ManageRecurringFlow();
                        break;
                    case 13:
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
        // TRANSACTIONS
        // -----------------------------------------------------------

        private void AddTransactionFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Add Transaction ===");

            var description = InputHelper.ReadString("Description: ");
            var amount = InputHelper.ReadDecimal("Amount: ");
            var type = InputHelper.ReadTransactionType("Type (1 = Income, 2 = Expense): ");
            var date = InputHelper.ReadDate("Date (YYYY-MM-DD) or blank for today: ");

            var (categoryId, categoryName) = PromptForCategorySelection();

            var transaction = new Transaction
            {
                Description = description,
                Amount = amount,
                Type = type == TransactionType.Income ? "Income" : "Expense",
                Category = categoryName,
                CategoryId = categoryId,
                Date = date
            };

            _budgetService.AddTransaction(transaction);

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Transaction saved successfully.");
        }

        private void ViewAllFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== All Transactions ===");

            var all = _budgetService.GetAllTransactions();
            OutputFormatter.PrintTransactions(all);
        }

        private void ViewByTypeFlow(TransactionType type)
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine(type == TransactionType.Income
                ? "=== Income Transactions ==="
                : "=== Expense Transactions ===");

            var items = _budgetService.GetByType(type);
            OutputFormatter.PrintTransactions(items);
        }

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

        private void ViewMonthlySummaryFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Monthly Summary ===");

            var year = InputHelper.ReadInt("Year (e.g., 2025): ", 2000, 2100);
            var month = InputHelper.ReadInt("Month (1-12): ", 1, 12);

            var monthlyItems = _budgetService.GetByMonth(year, month);
            var (income, expenses) = _budgetService.GetMonthlyTotals(year, month);

            OutputFormatter.PrintTransactions(monthlyItems);

            var categorySummaries = _budgetService.GetCategorySummariesByMonth(year, month);
            var monthName = new DateTime(year, month, 1).ToString("MMMM");

            OutputFormatter.PrintCategorySummaries(categorySummaries, $"Category Summary – {monthName} {year}");

            // Phase 1 budget reporting joins on CategoryName (simple and stable for now).
            var budgets = _budgetPlanService.GetAllBudgetsWithNames();
            OutputFormatter.PrintMonthlyBudgetReport(categorySummaries, budgets, $"Budget Report – {monthName} {year}");

            OutputFormatter.PrintTotals(income, expenses);
        }

        private void ViewOverallTotalsFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Overall Totals ===");

            var (income, expenses) = _budgetService.GetTotals();
            var categorySummaries = _budgetService.GetOverallCategorySummaries();

            OutputFormatter.PrintCategorySummaries(categorySummaries, "Overall Category Summary (All Time)");
            OutputFormatter.PrintTotals(income, expenses);
        }

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
            }

            SystemConsole.Write($"New category (current: {existing.Category}): ");
            var categoryInput = SystemConsole.ReadLine();
            if (!string.IsNullOrWhiteSpace(categoryInput))
                existing.Category = categoryInput;

            SystemConsole.Write($"New date (YYYY-MM-DD, current: {existing.Date:yyyy-MM-dd}): ");
            var dateInput = SystemConsole.ReadLine();
            if (!string.IsNullOrWhiteSpace(dateInput) && DateTime.TryParse(dateInput, out var newDate))
                existing.Date = newDate;

            _budgetService.UpdateTransaction(existing);

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Transaction updated successfully.");
        }

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
        // CATEGORIES
        // -----------------------------------------------------------

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

        private void ViewCategories()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Categories ===");

            var categories = _categoryService.GetAllCategories();

            if (categories.Count == 0)
                SystemConsole.WriteLine("No categories defined yet.");
            else
                foreach (var c in categories)
                    SystemConsole.WriteLine($"{c.Id}: {c.Name}");
        }

        private void AddCategory()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Add Category ===");

            var name = InputHelper.ReadString("Category name: ");
            _categoryService.AddCategory(name);

            SystemConsole.WriteLine("Category added.");
        }

        private void RenameCategory()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Rename Category ===");

            var categories = _categoryService.GetAllCategories();
            if (categories.Count == 0)
            {
                SystemConsole.WriteLine("No categories to rename.");
                return;
            }

            foreach (var c in categories)
                SystemConsole.WriteLine($"{c.Id}: {c.Name}");

            var id = InputHelper.ReadInt("Enter the ID of the category to rename: ", 1, int.MaxValue);
            var newName = InputHelper.ReadString("New name: ");

            _categoryService.RenameCategory(id, newName);
            SystemConsole.WriteLine("Category renamed.");
        }

        private void DeleteCategory()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Delete Category ===");

            var categories = _categoryService.GetAllCategories();
            if (categories.Count == 0)
            {
                SystemConsole.WriteLine("No categories to delete.");
                return;
            }

            foreach (var c in categories)
                SystemConsole.WriteLine($"{c.Id}: {c.Name}");

            var id = InputHelper.ReadInt("Enter the ID of the category to delete: ", 1, int.MaxValue);

            SystemConsole.Write("Are you sure? (Y/N): ");
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

        // -----------------------------------------------------------
        // BUDGETS
        // -----------------------------------------------------------

        private void ManageBudgetsFlow()
        {
            bool back = false;

            while (!back)
            {
                SystemConsole.Clear();
                SystemConsole.WriteLine("=== Manage Budgets ===");
                SystemConsole.WriteLine("1. Set/Update Budget for Category");
                SystemConsole.WriteLine("2. View All Budgets");
                SystemConsole.WriteLine("3. Delete Budget");
                SystemConsole.WriteLine("4. Back");
                SystemConsole.WriteLine();

                var choice = InputHelper.ReadInt("Choose an option (1-4): ", 1, 4);

                switch (choice)
                {
                    case 1:
                        SetOrUpdateBudgetFlow();
                        break;
                    case 2:
                        ViewAllBudgetsFlow();
                        break;
                    case 3:
                        DeleteBudgetFlow();
                        break;
                    case 4:
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

        private void SetOrUpdateBudgetFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Set / Update Budget ===");
            SystemConsole.WriteLine();

            var categories = _categoryService.GetAllCategories();
            if (categories.Count == 0)
            {
                SystemConsole.WriteLine("No categories exist yet. Add a category first.");
                return;
            }

            foreach (var c in categories)
                SystemConsole.WriteLine($"{c.Id}: {c.Name}");

            SystemConsole.WriteLine();

            var categoryId = InputHelper.ReadInt("Enter Category ID: ", 1, int.MaxValue);
            var amount = InputHelper.ReadDecimal("Enter monthly budget amount: ");

            _budgetPlanService.SetBudget(categoryId, amount);

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Budget saved.");
        }

        private void ViewAllBudgetsFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== All Budgets ===");

            var budgets = _budgetPlanService.GetAllBudgetsWithNames();
            OutputFormatter.PrintAllBudgets(budgets);
        }

        private void DeleteBudgetFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Delete Budget ===");
            SystemConsole.WriteLine();

            var budgets = _budgetPlanService.GetAllBudgetsWithNames();
            OutputFormatter.PrintAllBudgets(budgets);

            SystemConsole.WriteLine();

            var categoryId = InputHelper.ReadInt("Enter Category ID to delete budget for: ", 1, int.MaxValue);
            _budgetPlanService.DeleteBudget(categoryId);

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Budget deleted (if it existed).");
        }

        // -----------------------------------------------------------
        // RECURRING TRANSACTIONS
        // -----------------------------------------------------------

        private void ManageRecurringFlow()
        {
            bool back = false;

            while (!back)
            {
                SystemConsole.Clear();
                SystemConsole.WriteLine("=== Recurring Transactions ===");
                SystemConsole.WriteLine("1. Add Recurring Template");
                SystemConsole.WriteLine("2. View Templates");
                SystemConsole.WriteLine("3. Deactivate Template");
                SystemConsole.WriteLine("4. Run Due Templates (Generate Transactions)");
                SystemConsole.WriteLine("5. Back");
                SystemConsole.WriteLine();

                var choice = InputHelper.ReadInt("Choose an option (1-5): ", 1, 5);

                switch (choice)
                {
                    case 1:
                        AddRecurringTemplateFlow();
                        break;
                    case 2:
                        ViewRecurringTemplatesFlow();
                        break;
                    case 3:
                        DeactivateRecurringTemplateFlow();
                        break;
                    case 4:
                        RunDueRecurringFlow();
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

        private void AddRecurringTemplateFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Add Recurring Template ===");

            var description = InputHelper.ReadString("Description: ");
            var amount = InputHelper.ReadDecimal("Amount: ");
            var type = InputHelper.ReadTransactionType("Type (1 = Income, 2 = Expense): ");
            var startDate = InputHelper.ReadDate("Start date (YYYY-MM-DD) or blank for today: ");

            var (categoryId, categoryName) = PromptForCategorySelection();

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Frequency:");
            SystemConsole.WriteLine("1. Weekly");
            SystemConsole.WriteLine("2. Bi-Weekly");
            SystemConsole.WriteLine("3. Monthly");

            var freqChoice = InputHelper.ReadInt("Choose frequency (1-3): ", 1, 3);
            var frequency = (RecurringFrequency)freqChoice;

            int? dayOfMonth = null;
            if (frequency == RecurringFrequency.Monthly)
            {
                // Using 1–28 prevents “Feb 30th” style issues.
                dayOfMonth = InputHelper.ReadInt("Day of month (1-28): ", 1, 28);
            }

            var typeText = type == TransactionType.Income ? "Income" : "Expense";

            var id = _recurringService.CreateTemplate(
                description,
                amount,
                typeText,
                categoryName,
                categoryId,
                startDate,
                frequency,
                dayOfMonth);

            SystemConsole.WriteLine();
            SystemConsole.WriteLine($"Recurring template created (Id: {id}).");
        }

        private void ViewRecurringTemplatesFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Recurring Templates ===");
            SystemConsole.WriteLine();

            var templates = _recurringService.GetAllTemplates();

            if (templates.Count == 0)
            {
                SystemConsole.WriteLine("No recurring templates found.");
                return;
            }

            SystemConsole.WriteLine("Id  Active  NextRun     Freq      Category                Description                     Amount   Type");
            SystemConsole.WriteLine("--  ------  ----------  --------  ----------------------  ------------------------------  -------  ------");

            foreach (var t in templates)
            {
                var active = t.IsActive ? "Yes" : "No";
                SystemConsole.WriteLine(
                    "{0,-3} {1,-6}  {2:yyyy-MM-dd}  {3,-8}  {4,-22}  {5,-30}  {6,7:C}  {7,-6}",
                    t.Id,
                    active,
                    t.NextRunDate,
                    t.Frequency,
                    Truncate(t.Category, 22),
                    Truncate(t.Description, 30),
                    t.Amount,
                    t.Type);
            }
        }

        private void DeactivateRecurringTemplateFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Deactivate Template ===");
            SystemConsole.WriteLine();

            ViewRecurringTemplatesFlow();

            SystemConsole.WriteLine();
            var id = InputHelper.ReadInt("Enter template Id to deactivate: ", 1, int.MaxValue);

            _recurringService.DeactivateTemplate(id);

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Template deactivated (if it existed).");
        }

        private void RunDueRecurringFlow()
        {
            SystemConsole.Clear();
            SystemConsole.WriteLine("=== Run Due Templates ===");
            SystemConsole.WriteLine();

            var created = _recurringService.RunDue(DateTime.Today);

            SystemConsole.WriteLine($"Generated {created} transaction(s).");
        }

        // -----------------------------------------------------------
        // SHARED HELPERS
        // -----------------------------------------------------------

        private (int? categoryId, string categoryName) PromptForCategorySelection()
        {
            var categories = _categoryService.GetAllCategories();

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Available categories:");

            if (categories.Count == 0)
            {
                SystemConsole.WriteLine("(No categories yet. You can add one now.)");
            }
            else
            {
                foreach (var c in categories)
                    SystemConsole.WriteLine($"{c.Id}: {c.Name}");
            }

            SystemConsole.WriteLine();
            SystemConsole.WriteLine("Options:");
            SystemConsole.WriteLine(" - Enter an existing Category ID to use it");
            SystemConsole.WriteLine(" - Enter 0 to type a custom category");

            var categoryIdInput = InputHelper.ReadInt("Category ID (or 0 for custom): ", 0, int.MaxValue);

            if (categoryIdInput == 0)
            {
                SystemConsole.Write("Enter category name: ");
                var customName = (SystemConsole.ReadLine() ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(customName))
                    return (null, "Uncategorized");

                var created = _categoryService.AddCategory(customName);
                return (created.Id, created.Name);
            }

            var selected = categories.FirstOrDefault(c => c.Id == categoryIdInput);
            if (selected == null)
                return (null, "Uncategorized");

            return (selected.Id, selected.Name);
        }

        private static string Truncate(string value, int maxLen)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Length <= maxLen) return value;
            return value.Substring(0, maxLen);
        }
    }
}
