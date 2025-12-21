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
    public sealed class TransactionsViewModel : INotifyPropertyChanged
    {
        private readonly BudgetService _budgetService;
        private readonly BudgetPlanService _budgetPlanService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Transaction> Transactions { get; } = new();

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

        private string _searchText = string.Empty;

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

        public RelayCommand RefreshCommand { get; }
        public RelayCommand AddTransactionCommand { get; }
        public RelayCommand EditSelectedCommand { get; }
        public RelayCommand DeleteSelectedCommand { get; }
        public RelayCommand ClearSearchCommand { get; }

        public TransactionsViewModel()
        {
            var txRepo = new TransactionRepository();
            _budgetService = new BudgetService(txRepo);

            var categoryRepo = new CategoryRepository();
            _budgetPlanService = new BudgetPlanService(
                new CategoryBudgetRepository(),
                new MonthlyCategoryBudgetRepository(),
                categoryRepo);

            RefreshCommand = new RelayCommand(Refresh);
            AddTransactionCommand = new RelayCommand(OpenAddDialog);
            EditSelectedCommand = new RelayCommand(OpenEditDialog, () => SelectedTransaction != null);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedTransaction != null);
            ClearSearchCommand = new RelayCommand(ClearSearch, () => !string.IsNullOrWhiteSpace(SearchText));

            Refresh();
        }

        private void Refresh() => ApplySearch();

        private void ApplySearch()
        {
            var keyword = (SearchText ?? string.Empty).Trim();

            var list = string.IsNullOrWhiteSpace(keyword)
                ? _budgetService.GetAllTransactions()
                : _budgetService.SearchTransactions(keyword);

            Transactions.Clear();
            foreach (var t in list)
                Transactions.Add(t);

            if (SelectedTransaction != null && Transactions.All(t => t.Id != SelectedTransaction.Id))
                SelectedTransaction = null;
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

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

                if (!ConfirmOverspendIfNeeded(pending, excludeTransactionId: null))
                    return;

                _budgetService.AddTransaction(pending);
                ApplySearch();
            }
        }

        private void OpenEditDialog()
        {
            if (SelectedTransaction == null)
                return;

            var window = new AddTransactionWindow
            {
                Owner = Application.Current?.MainWindow
            };

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
                updated.Id = SelectedTransaction.Id;

                if (!ConfirmOverspendIfNeeded(updated, excludeTransactionId: SelectedTransaction.Id))
                    return;

                _budgetService.UpdateTransaction(updated);
                ApplySearch();
            }
        }

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
            ApplySearch();
        }

        private bool ConfirmOverspendIfNeeded(Transaction pending, int? excludeTransactionId)
        {
            if (pending == null)
                return true;

            if (pending.Type == null || !pending.Type.Equals("Expense", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!pending.CategoryId.HasValue || pending.CategoryId.Value <= 0)
                return true;

            var categoryId = pending.CategoryId.Value;

            var year = pending.Date.Year;
            var month = pending.Date.Month;

            // PHASE B CHANGE: budget is now month-aware
            if (!_budgetPlanService.TryGetMonthlyBudgetAmount(categoryId, year, month, out var budgetAmount))
                return true;

            var spentSoFar = _budgetService.GetTotalExpensesForCategoryMonth(categoryId, year, month, excludeTransactionId);
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
