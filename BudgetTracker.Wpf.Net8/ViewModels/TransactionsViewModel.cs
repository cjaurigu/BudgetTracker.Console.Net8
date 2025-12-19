using System.Collections.ObjectModel;
using System.ComponentModel;
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
            var repo = new TransactionRepository();
            _budgetService = new BudgetService(repo);

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
                _budgetService.AddTransaction(vm.CreatedTransaction);
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

                vm.SelectedType = SelectedTransaction.Type.Equals("Income", System.StringComparison.OrdinalIgnoreCase)
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
                        c.Name.Equals(SelectedTransaction.Category, System.StringComparison.OrdinalIgnoreCase));

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

        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
