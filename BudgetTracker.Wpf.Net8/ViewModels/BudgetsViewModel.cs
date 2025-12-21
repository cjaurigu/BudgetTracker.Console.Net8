// File: ViewModels/BudgetsViewModel.cs
// Namespace: BudgetTracker.Wpf.Net8.ViewModels
//
// Purpose:
// Budgets tab ViewModel:
// - Shows budgets + spent + remaining for selected month/year
// - Provides carry-over Preview/Apply commands
//   (PreviewCarryOverCommand, ApplyCarryOverCommand)
// - Exposes CarryOverPreviewText for the UI

using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Services;
using BudgetTracker.Wpf.Net8.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace BudgetTracker.Wpf.Net8.ViewModels
{
    public sealed class BudgetsViewModel : INotifyPropertyChanged
    {
        private readonly BudgetPlanService _budgetPlanService;
        private readonly CategoryRepository _categoryRepo;
        private readonly BudgetService _budgetService;
        private readonly BudgetCarryOverService _carryOverService;

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
                    ClearCarryOverPreview();
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
                    ClearCarryOverPreview();
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

        // -----------------------------
        // Carry-over UI binding
        // -----------------------------
        private string _carryOverPreviewText = string.Empty;
        public string CarryOverPreviewText
        {
            get => _carryOverPreviewText;
            set
            {
                if (_carryOverPreviewText != value)
                {
                    _carryOverPreviewText = value;
                    OnPropertyChanged();
                }
            }
        }

        // -----------------------------
        // Commands
        // -----------------------------
        public RelayCommand RefreshCommand { get; }
        public RelayCommand EditSelectedCommand { get; }
        public RelayCommand ClearBudgetCommand { get; }
        public RelayCommand ClearSearchCommand { get; }

        // Requested bindings
        public RelayCommand PreviewCarryOverCommand { get; }
        public RelayCommand ApplyCarryOverCommand { get; }

        public BudgetsViewModel()
        {
            _categoryRepo = new CategoryRepository();

            // BudgetPlanService supports monthly budgets (Phase B/C)
            _budgetPlanService = new BudgetPlanService(
                new CategoryBudgetRepository(),
                new MonthlyCategoryBudgetRepository(),
                _categoryRepo);

            _budgetService = new BudgetService(new TransactionRepository());

            // Carry-over engine (Phase D)
            _carryOverService = new BudgetCarryOverService(
                _budgetPlanService,
                _budgetService,
                _categoryRepo,
                new BudgetCarryOverRunRepository());

            var now = DateTime.Now;
            _selectedMonth = now.Month;
            _selectedYear = now.Year;

            Years = Enumerable.Range(now.Year - 5, 7).ToList(); // (year-5) .. (year+1)

            RefreshCommand = new RelayCommand(Refresh);
            EditSelectedCommand = new RelayCommand(OpenEditDialog, () => SelectedRow != null);
            ClearBudgetCommand = new RelayCommand(ClearSelectedBudget, () => SelectedRow != null);
            ClearSearchCommand = new RelayCommand(ClearSearch, () => !string.IsNullOrWhiteSpace(SearchText));

            // Requested commands
            PreviewCarryOverCommand = new RelayCommand(PreviewCarryOver);
            ApplyCarryOverCommand = new RelayCommand(ApplyCarryOver);

            Refresh();
        }

        private void Refresh()
        {
            // 1) Monthly budgets for selected month
            var monthlyBudgets = _budgetPlanService.GetMonthlyBudgetsWithNames(SelectedYear, SelectedMonth);
            var budgetByCategoryId = monthlyBudgets.ToDictionary(b => b.CategoryId, b => b.BudgetAmount);

            // 2) Monthly spend summaries
            var summaries = _budgetService.GetCategorySummariesByMonth(SelectedYear, SelectedMonth);
            var spentByCategoryName = summaries.ToDictionary(
                s => s.CategoryName,
                s => s.TotalExpense,
                StringComparer.OrdinalIgnoreCase);

            // 3) Build rows from categories (ensures categories always show)
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

            // 5) Push to collection
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

            // NOTE: Your edit window remains unchanged. We only update monthly budget values.
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
                ClearCarryOverPreview();
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
                ClearCarryOverPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Clear Budget Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -----------------------------
        // Carry-over logic
        // -----------------------------
        private void PreviewCarryOver()
        {
            try
            {
                var lines = _carryOverService.PreviewCarryOver(SelectedYear, SelectedMonth);
                var total = lines.Sum(x => x.CarryOverAmount);

                if (lines.Count == 0 || total <= 0m)
                {
                    CarryOverPreviewText = "No unused budget to carry over for this month.";
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"Carry-over preview for {SelectedYear}/{SelectedMonth:00}:");
                sb.AppendLine();

                foreach (var line in lines.Take(15))
                {
                    sb.AppendLine($"{line.CategoryName}: {line.CarryOverAmount.ToString("C", CultureInfo.CurrentCulture)}");
                }

                if (lines.Count > 15)
                    sb.AppendLine($"…and {lines.Count - 15} more categories.");

                sb.AppendLine();
                sb.AppendLine($"Total to Savings: {total.ToString("C", CultureInfo.CurrentCulture)}");

                CarryOverPreviewText = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Carry-over Preview Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyCarryOver()
        {
            try
            {
                // Always compute total first (and show confirmation)
                var lines = _carryOverService.PreviewCarryOver(SelectedYear, SelectedMonth);
                var total = lines.Sum(x => x.CarryOverAmount);

                if (total <= 0m)
                {
                    MessageBox.Show(
                        "No unused budget to carry over for this month.",
                        "Carry-over",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var nextMonthDate = new DateTime(SelectedYear, SelectedMonth, 1).AddMonths(1);

                var msg =
                    $"This will create an Income transaction in \"Savings\" dated {nextMonthDate:yyyy-MM-dd}.\n\n" +
                    $"Total carry-over: {total.ToString("C", CultureInfo.CurrentCulture)}\n\n" +
                    $"Apply carry-over now?";

                var confirm = MessageBox.Show(
                    msg,
                    "Apply Carry-over",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var appliedTotal = _carryOverService.ApplyCarryOverToSavings(SelectedYear, SelectedMonth);

                MessageBox.Show(
                    $"Carry-over applied!\n\nSavings +{appliedTotal.ToString("C", CultureInfo.CurrentCulture)}",
                    "Carry-over Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Refresh();
                ClearCarryOverPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Carry-over Apply Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearCarryOverPreview()
        {
            CarryOverPreviewText = string.Empty;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
