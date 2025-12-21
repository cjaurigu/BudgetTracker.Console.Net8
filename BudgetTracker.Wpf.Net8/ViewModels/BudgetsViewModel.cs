using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Services;
using BudgetTracker.Wpf.Net8.Views;

namespace BudgetTracker.Wpf.Net8.ViewModels
{
    public sealed class BudgetsViewModel : INotifyPropertyChanged
    {
        private readonly BudgetPlanService _budgetPlanService;
        private readonly CategoryRepository _categoryRepo;
        private readonly BudgetService _budgetService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<BudgetRow> BudgetRows { get; } = new();

        public List<int> Months { get; } = Enumerable.Range(1, 12).ToList();
        public List<int> Years { get; }

        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (_selectedMonth != value)
                {
                    _selectedMonth = value;
                    OnPropertyChanged();
                    Refresh();
                }
            }
        }

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
                    Refresh();
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
                    Refresh();
                }
            }
        }

        private BudgetRow? _selectedRow;
        public BudgetRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (!ReferenceEquals(_selectedRow, value))
                {
                    _selectedRow = value;
                    OnPropertyChanged();

                    EditSelectedCommand.RaiseCanExecuteChanged();
                    ClearBudgetCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand EditSelectedCommand { get; }
        public RelayCommand ClearBudgetCommand { get; }
        public RelayCommand ClearSearchCommand { get; }

        public BudgetsViewModel()
        {
            _categoryRepo = new CategoryRepository();

            _budgetPlanService = new BudgetPlanService(
                new CategoryBudgetRepository(),
                new MonthlyCategoryBudgetRepository(),
                _categoryRepo);

            _budgetService = new BudgetService(new TransactionRepository());

            var now = DateTime.Now;
            _selectedMonth = now.Month;
            _selectedYear = now.Year;

            Years = Enumerable.Range(now.Year - 5, 7).ToList(); // (year-5) .. (year+1)

            RefreshCommand = new RelayCommand(Refresh);
            EditSelectedCommand = new RelayCommand(OpenEditDialog, () => SelectedRow != null);
            ClearBudgetCommand = new RelayCommand(ClearSelectedBudget, () => SelectedRow != null);
            ClearSearchCommand = new RelayCommand(ClearSearch, () => !string.IsNullOrWhiteSpace(SearchText));

            Refresh();
        }

        private void Refresh()
        {
            // 1) Load monthly budgets for selected month
            var monthlyBudgets = _budgetPlanService.GetMonthlyBudgetsWithNames(SelectedYear, SelectedMonth);
            var budgetByCategoryId = monthlyBudgets.ToDictionary(b => b.CategoryId, b => b.BudgetAmount);

            // 2) Load monthly spending summaries (grouped by name)
            var summaries = _budgetService.GetCategorySummariesByMonth(SelectedYear, SelectedMonth);
            var spentByCategoryName = summaries.ToDictionary(
                s => s.CategoryName,
                s => s.TotalExpense,
                StringComparer.OrdinalIgnoreCase);

            // 3) Build grid rows from categories
            var categories = _categoryRepo.GetAll();

            var rows = new List<BudgetRow>();
            foreach (var c in categories)
            {
                var budget = budgetByCategoryId.TryGetValue(c.Id, out var b) ? b : 0m;
                var spent = spentByCategoryName.TryGetValue(c.Name, out var e) ? e : 0m;

                rows.Add(new BudgetRow
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    BudgetAmount = budget,
                    SpentAmount = spent
                });
            }

            // 4) Search filter
            var keyword = (SearchText ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                rows = rows
                    .Where(r => r.CategoryName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 5) Push to observable collection
            BudgetRows.Clear();
            foreach (var r in rows.OrderBy(r => r.CategoryName))
                BudgetRows.Add(r);

            if (SelectedRow != null && BudgetRows.All(r => r.CategoryId != SelectedRow.CategoryId))
                SelectedRow = null;
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private void OpenEditDialog()
        {
            if (SelectedRow == null)
                return;

            // PHASE C: warn if this category/month already has spending
            if (!ConfirmEditingBudgetIfSpendingExists())
                return;

            var win = new BudgetEditWindow(
                title: $"Set Budget - {SelectedRow.CategoryName} ({SelectedYear}/{SelectedMonth:00})",
                initialAmount: SelectedRow.BudgetAmount)
            {
                Owner = Application.Current?.MainWindow
            };

            var result = win.ShowDialog();
            if (result != true)
                return;

            try
            {
                _budgetPlanService.SetMonthlyBudget(SelectedRow.CategoryId, SelectedYear, SelectedMonth, win.BudgetAmount);
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Set Budget Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearSelectedBudget()
        {
            if (SelectedRow == null)
                return;

            // PHASE C: warn if spending exists
            if (!ConfirmEditingBudgetIfSpendingExists())
                return;

            var confirm = MessageBox.Show(
                $"Clear monthly budget for:\n\n{SelectedRow.CategoryName}\n({SelectedYear}/{SelectedMonth:00}) ?",
                "Confirm Clear Budget",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                _budgetPlanService.DeleteMonthlyBudget(SelectedRow.CategoryId, SelectedYear, SelectedMonth);
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Clear Budget Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ConfirmEditingBudgetIfSpendingExists()
        {
            if (SelectedRow == null)
                return true;

            var spent = _budgetService.GetTotalExpensesForCategoryMonth(
                SelectedRow.CategoryId,
                SelectedYear,
                SelectedMonth);

            if (spent <= 0m)
                return true;

            var msg =
                $"This category already has spending for {SelectedYear}/{SelectedMonth:00}.\n\n" +
                $"Spent so far: {spent.ToString("C", CultureInfo.CurrentCulture)}\n\n" +
                $"Changing the budget will not change transactions — it will only change reporting.\n\n" +
                $"Do you want to continue?";

            var choice = MessageBox.Show(
                msg,
                "Edit Past Budget Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return choice == MessageBoxResult.Yes;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
