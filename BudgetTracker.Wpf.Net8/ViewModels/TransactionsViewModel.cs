using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Domain.Enums;
using BudgetTracker.Console.Net8.Services;
using BudgetTracker.Wpf.Net8.Views;

namespace BudgetTracker.Wpf.Net8.ViewModels
{
    /// <summary>
    /// ViewModel for the main Transactions grid screen.
    /// - Loads transactions from SQL via BudgetService
    /// - Supports Refresh / Add / Edit / Delete
    /// - Supports Search (Description + Category)
    /// </summary>
    public sealed class TransactionsViewModel : INotifyPropertyChanged
    {
        private readonly BudgetService _budgetService;
        private readonly BudgetPlanService _budgetPlanService;

        public event PropertyChangedEventHandler? PropertyChanged;

        // -----------------------------
        // Data bound to the DataGrid
        // -----------------------------
        public ObservableCollection<Transaction> Transactions { get; } = new();

        // Selected row in the DataGrid
        private Transaction? _selectedTransaction;
        public Transaction? SelectedTransaction
        {
            get => _selectedTransaction;
            set
            {
                if (!ReferenceEquals(_selectedTransaction, value))
                {
                    _selectedTransaction = value;
                    OnPropertyChanged();

                    DeleteSelectedCommand.RaiseCanExecuteChanged();
                    EditSelectedCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // -----------------------------
        // Search / Filter
        // -----------------------------
        private string _searchText = string.Empty;

        /// <summary>
        /// Search text bound to the Search TextBox.
        /// When it changes, we reload the grid using the search API.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value ?? string.Empty;
                    OnPropertyChanged();

                    ClearSearchCommand.RaiseCanExecuteChanged();
                    ApplySearch();
                }
            }
        }

        // -----------------------------
        // Commands (Buttons)
        // -----------------------------
        public RelayCommand RefreshCommand { get; }
        public RelayCommand AddTransactionCommand { get; }
        public RelayCommand EditSelectedCommand { get; }
        public RelayCommand DeleteSelectedCommand { get; }
        public RelayCommand ClearSearchCommand { get; }

        public TransactionsViewModel()
        {
            // Uses your existing ADO.NET repository + service
            var txRepo = new TransactionRepository();
            _budgetService = new BudgetService(txRepo);

            // Budget read access for overspend warnings (Phase A)
            var categoryRepo = new CategoryRepository();
            _budgetPlanService = new BudgetPlanService(new CategoryBudgetRepository(), categoryRepo);

            RefreshCommand = new RelayCommand(Refresh);
            AddTransactionCommand = new RelayCommand(OpenAddDialog);
            EditSelectedCommand = new RelayCommand(OpenEditDialog, () => SelectedTransaction != null);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedTransaction != null);
            ClearSearchCommand = new RelayCommand(ClearSearch, () => !string.IsNullOrWhiteSpace(SearchText));

            Refresh();
        }

        // -----------------------------
        // Load / Refresh
        // -----------------------------
        private void Refresh()
        {
            // If user is searching, Refresh should re-run the same search.
            // If empty search, show all.
            ApplySearch();
        }

        private void ApplySearch()
        {
            var keyword = (SearchText ?? string.Empty).Trim();

            // Using your existing service method:
            // - if keyword blank, it returns empty list
            // - so we use GetAllTransactions when blank
            var list = string.IsNullOrWhiteSpace(keyword)
                ? _budgetService.GetAllTransactions()
                : _budgetService.SearchTransactions(keyword);

            Transactions.Clear();
            foreach (var t in list)
                Transactions.Add(t);

            // If the selected transaction is no longer in the list, clear selection
            if (SelectedTransaction != null && Transactions.All(t => t.Id != SelectedTransaction.Id))
                SelectedTransaction = null;
        }

        private void ClearSearch()
        {
            SearchText = string.Empty; // triggers ApplySearch()
        }

        // -----------------------------
        // Add
        // -----------------------------
        private void OpenAddDialog()
        {
            var window = new AddTransactionWindow
            {
                Owner = Application.Current?.MainWindow
            };

            var result = window.ShowDialog();
            if (result != true)
                return;

            if (window.DataContext is AddTransactionViewModel vm && vm.CreatedTransaction != null)
            {
                var pending = vm.CreatedTransaction;

                // PHASE A: Overspend warning (Expense only)
                if (!ConfirmOverspendIfNeeded(pending, excludeTransactionId: null))
                    return;

                _budgetService.AddTransaction(pending);
                ApplySearch(); // keep current filter applied
            }
        }

        // -----------------------------
        // Edit
        // -----------------------------
        private void OpenEditDialog()
        {
            if (SelectedTransaction == null)
                return;

            var window = new AddTransactionWindow
            {
                Owner = Application.Current?.MainWindow
            };

            // Pre-fill from selected row
            if (window.DataContext is AddTransactionViewModel vm)
            {
                vm.Date = SelectedTransaction.Date;
                vm.Description = SelectedTransaction.Description;
                vm.AmountText = SelectedTransaction.Amount.ToString();

                vm.SelectedType = SelectedTransaction.Type.Equals("Income", StringComparison.OrdinalIgnoreCase)
                    ? TransactionType.Income
                    : TransactionType.Expense;

                if (SelectedTransaction.CategoryId.HasValue)
                {
                    var matchById = vm.Categories.FirstOrDefault(c => c.Id == SelectedTransaction.CategoryId.Value);
                    if (matchById != null)
                        vm.SelectedCategory = matchById;
                }

                if (vm.SelectedCategory == null && !string.IsNullOrWhiteSpace(SelectedTransaction.Category))
                {
                    var matchByName = vm.Categories.FirstOrDefault(c =>
                        c.Name.Equals(SelectedTransaction.Category, StringComparison.OrdinalIgnoreCase));

                    if (matchByName != null)
                        vm.SelectedCategory = matchByName;
                }
            }

            var result = window.ShowDialog();
            if (result != true)
                return;

            if (window.DataContext is AddTransactionViewModel vmAfter && vmAfter.CreatedTransaction != null)
            {
                var updated = vmAfter.CreatedTransaction;

                // CRITICAL: keep original Id so UPDATE happens
                updated.Id = SelectedTransaction.Id;

                // PHASE A: Overspend warning (Expense only)
                // For edit: exclude this transaction so we don't double-count it in "spent so far"
                if (!ConfirmOverspendIfNeeded(updated, excludeTransactionId: SelectedTransaction.Id))
                    return;

                _budgetService.UpdateTransaction(updated);
                ApplySearch(); // keep current filter applied
            }
        }

        // -----------------------------
        // Delete
        // -----------------------------
        private void DeleteSelected()
        {
            if (SelectedTransaction == null)
                return;

            var confirm = MessageBox.Show(
                $"Delete transaction #{SelectedTransaction.Id}?\n\n{SelectedTransaction.Description}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            _budgetService.DeleteTransaction(SelectedTransaction.Id);
            ApplySearch(); // keep current filter applied
        }

        // -----------------------------
        // PHASE A: Overspend warning helper
        // -----------------------------
        private bool ConfirmOverspendIfNeeded(Transaction pending, int? excludeTransactionId)
        {
            if (pending == null)
                return true;

            // Only warn for Expense
            if (pending.Type == null || !pending.Type.Equals("Expense", StringComparison.OrdinalIgnoreCase))
                return true;

            // Need a CategoryId to check budget reliably
            if (!pending.CategoryId.HasValue || pending.CategoryId.Value <= 0)
                return true;

            var categoryId = pending.CategoryId.Value;

            // If no budget exists for this category, don't warn
            if (!_budgetPlanService.TryGetBudgetAmount(categoryId, out var budgetAmount))
                return true;

            // Determine month/year from transaction date (this matches how your Budgets tab uses SelectedMonth/Year)
            var year = pending.Date.Year;
            var month = pending.Date.Month;

            // Spent so far in this category for this month (excluding the transaction being edited if applicable)
            var spentSoFar = _budgetService.GetTotalExpensesForCategoryMonth(categoryId, year, month, excludeTransactionId);

            // Projected spend after this save
            var projected = spentSoFar + pending.Amount;

            if (projected <= budgetAmount)
                return true;

            var overBy = projected - budgetAmount;

            var msg =
                $"This expense will exceed the budget for \"{pending.Category}\".\n\n" +
                $"Budget: {budgetAmount.ToString("C", CultureInfo.CurrentCulture)}\n" +
                $"Spent (so far): {spentSoFar.ToString("C", CultureInfo.CurrentCulture)}\n" +
                $"After this: {projected.ToString("C", CultureInfo.CurrentCulture)}\n\n" +
                $"Over by: {overBy.ToString("C", CultureInfo.CurrentCulture)}\n\n" +
                $"Do you want to continue?";

            var choice = MessageBox.Show(
                msg,
                "Budget Overspend Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return choice == MessageBoxResult.Yes;
        }

        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
