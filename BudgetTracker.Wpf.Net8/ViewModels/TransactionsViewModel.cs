using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Services;
using BudgetTracker.Wpf.Net8.Views;

namespace BudgetTracker.Wpf.Net8.ViewModels
{
    public sealed class TransactionsViewModel : INotifyPropertyChanged
    {
        private readonly BudgetService _budgetService;
        private readonly CategoryService _categoryService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Transaction> Transactions { get; } = new();

        // Filters
        public ObservableCollection<int> Years { get; } = new();
        public ObservableCollection<MonthOption> Months { get; } = new();

        // Includes "All Categories" for filtering
        public ObservableCollection<Category> Categories { get; } = new();

        // Excludes "All Categories" for inline edit dropdown
        public ObservableCollection<Category> EditableCategories { get; } = new();

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear != value)
                {
                    _selectedYear = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        private MonthOption? _selectedMonth;
        public MonthOption? SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (!ReferenceEquals(_selectedMonth, value))
                {
                    _selectedMonth = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (!ReferenceEquals(_selectedCategory, value))
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        // Selection
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
                    EditSelectedCommand.RaiseCanExecuteChanged();
                    DeleteSelectedCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private IList? _selectedTransactions;
        public IList? SelectedTransactions
        {
            get => _selectedTransactions;
            set
            {
                if (!ReferenceEquals(_selectedTransactions, value))
                {
                    _selectedTransactions = value;
                    OnPropertyChanged();
                    EditSelectedCommand.RaiseCanExecuteChanged();
                    DeleteSelectedCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // Commands
        public RelayCommand RefreshCommand { get; }
        public RelayCommand ClearFiltersCommand { get; }
        public RelayCommand AddTransactionCommand { get; }
        public RelayCommand EditSelectedCommand { get; }
        public RelayCommand DeleteSelectedCommand { get; }

        public TransactionsViewModel()
        {
            var txRepo = new TransactionRepository();
            _budgetService = new BudgetService(txRepo);

            var catRepo = new CategoryRepository();
            _categoryService = new CategoryService(catRepo);

            RefreshCommand = new RelayCommand(Refresh);
            ClearFiltersCommand = new RelayCommand(ClearFilters);

            AddTransactionCommand = new RelayCommand(OpenAddDialog);
            EditSelectedCommand = new RelayCommand(OpenEditDialog, () => GetSelectedTransactions().Count == 1);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => GetSelectedTransactions().Count >= 1);

            LoadFilterLists();
            Refresh();
        }

        private void Refresh()
        {
            var all = _budgetService.GetAllTransactions();
            LoadTransactions(all);
            ApplyFilters();
        }

        private void LoadTransactions(System.Collections.Generic.List<Transaction> items)
        {
            Transactions.Clear();
            foreach (var t in items)
                Transactions.Add(t);
        }

        private void LoadFilterLists()
        {
            Years.Clear();
            var currentYear = DateTime.Today.Year;
            for (int y = currentYear; y >= currentYear - 10; y--)
                Years.Add(y);

            Months.Clear();
            Months.Add(new MonthOption(0, "All Months"));
            for (int m = 1; m <= 12; m++)
            {
                var name = new DateTime(2000, m, 1).ToString("MMMM");
                Months.Add(new MonthOption(m, name));
            }

            Categories.Clear();
            Categories.Add(new Category { Id = 0, Name = "All Categories" });

            EditableCategories.Clear();

            var allCats = _categoryService.GetAllCategories()
                .OrderBy(x => x.Name)
                .ToList();

            foreach (var c in allCats)
            {
                Categories.Add(c);
                EditableCategories.Add(c);
            }

            SelectedYear = DateTime.Today.Year;
            SelectedMonth = Months.FirstOrDefault(x => x.Month == DateTime.Today.Month) ?? Months.First();
            SelectedCategory = Categories.FirstOrDefault(); // All Categories
        }

        private void ClearFilters()
        {
            SelectedYear = DateTime.Today.Year;
            SelectedMonth = Months.FirstOrDefault(x => x.Month == DateTime.Today.Month) ?? Months.First();
            SelectedCategory = Categories.FirstOrDefault(); // All Categories
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var all = _budgetService.GetAllTransactions().AsEnumerable();

            all = all.Where(t => t.Date.Year == SelectedYear);

            var month = SelectedMonth?.Month ?? 0;
            if (month >= 1 && month <= 12)
                all = all.Where(t => t.Date.Month == month);

            var cat = SelectedCategory;
            if (cat != null && cat.Id != 0)
            {
                all = all.Where(t =>
                    (t.CategoryId.HasValue && t.CategoryId.Value == cat.Id) ||
                    (!t.CategoryId.HasValue && string.Equals(t.Category, cat.Name, StringComparison.OrdinalIgnoreCase)));
            }

            LoadTransactions(all
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToList());
        }

        // Called by TransactionsView.xaml.cs after inline edits
        public void SaveEditedTransaction(Transaction tx)
        {
            try
            {
                // Keep Category string in sync if CategoryId changed
                if (tx.CategoryId.HasValue)
                {
                    var match = EditableCategories.FirstOrDefault(c => c.Id == tx.CategoryId.Value);
                    if (match != null)
                        tx.Category = match.Name;
                }

                // Basic cleanup: Type should be a plain string "Expense"/"Income"
                // When editing via ComboBoxItem, WPF may hand us "System.Windows.Controls.ComboBoxItem: Expense"
                // so we normalize it.
                tx.Type = NormalizeType(tx.Type);

                _budgetService.UpdateTransaction(tx);

                // Optional: refresh so sorting stays consistent after edits
                // (Keeps it simple and avoids stale computed values)
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not save the edited transaction.\n\n{ex.Message}",
                    "Save Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string NormalizeType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Expense";

            // Handles: "Expense", "Income"
            if (value.Equals("Expense", StringComparison.OrdinalIgnoreCase))
                return "Expense";
            if (value.Equals("Income", StringComparison.OrdinalIgnoreCase))
                return "Income";

            // Handles ComboBoxItem ToString output: "System.Windows.Controls.ComboBoxItem: Expense"
            if (value.Contains("Expense", StringComparison.OrdinalIgnoreCase))
                return "Expense";
            if (value.Contains("Income", StringComparison.OrdinalIgnoreCase))
                return "Income";

            return value.Trim();
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
                _budgetService.AddTransaction(vm.CreatedTransaction);
                Refresh();
            }
        }

        private void OpenEditDialog()
        {
            var selected = GetSelectedTransactions();
            if (selected.Count != 1)
                return;

            var tx = selected[0];

            var window = new AddTransactionWindow
            {
                Owner = Application.Current?.MainWindow
            };

            if (window.DataContext is AddTransactionViewModel vm)
            {
                vm.Date = tx.Date;
                vm.Description = tx.Description;
                vm.AmountText = tx.Amount.ToString();

                vm.SelectedType = tx.Type.Equals("Income", StringComparison.OrdinalIgnoreCase)
                    ? Console.Net8.Domain.Enums.TransactionType.Income
                    : Console.Net8.Domain.Enums.TransactionType.Expense;

                if (tx.CategoryId.HasValue)
                {
                    var matchById = vm.Categories.FirstOrDefault(c => c.Id == tx.CategoryId.Value);
                    if (matchById != null)
                        vm.SelectedCategory = matchById;
                }

                if (vm.SelectedCategory == null && !string.IsNullOrWhiteSpace(tx.Category))
                {
                    var matchByName = vm.Categories.FirstOrDefault(c =>
                        c.Name.Equals(tx.Category, StringComparison.OrdinalIgnoreCase));

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
                updated.Id = tx.Id;

                _budgetService.UpdateTransaction(updated);
                Refresh();
            }
        }

        private void DeleteSelected()
        {
            var selected = GetSelectedTransactions();
            if (selected.Count == 0)
                return;

            string message = selected.Count == 1
                ? $"Delete transaction #{selected[0].Id}?\n\n{selected[0].Description}"
                : $"Delete {selected.Count} transactions?\n\nThis cannot be undone.";

            var confirm = MessageBox.Show(
                message,
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            foreach (var tx in selected)
                _budgetService.DeleteTransaction(tx.Id);

            Refresh();
        }

        private System.Collections.Generic.List<Transaction> GetSelectedTransactions()
        {
            if (SelectedTransactions != null && SelectedTransactions.Count > 0)
            {
                return SelectedTransactions
                    .Cast<object>()
                    .OfType<Transaction>()
                    .ToList();
            }

            if (SelectedTransaction != null)
                return new System.Collections.Generic.List<Transaction> { SelectedTransaction };

            return new System.Collections.Generic.List<Transaction>();
        }

        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public sealed class MonthOption
    {
        public int Month { get; }
        public string Display { get; }

        public MonthOption(int month, string display)
        {
            Month = month;
            Display = display;
        }

        public override string ToString() => Display;
    }
}
