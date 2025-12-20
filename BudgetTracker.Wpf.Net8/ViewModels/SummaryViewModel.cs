using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Services;

namespace BudgetTracker.Wpf.Net8.ViewModels
{
    /// <summary>
    /// ViewModel for the Summary tab.
    /// - Tracks selected Month/Year
    /// - Calculates Total Income / Total Expense / Net Total for that month
    /// - Loads category breakdown for the selected month
    /// - Auto-refreshes when Month/Year changes
    /// </summary>
    public sealed class SummaryViewModel : INotifyPropertyChanged
    {
        private readonly BudgetService _budgetService;

        // Prevent refresh from running while we are still setting up defaults.
        private bool _isInitializing;

        public event PropertyChangedEventHandler? PropertyChanged;

        // ------------------------------------
        // Month/Year pickers
        // ------------------------------------

        public ObservableCollection<int> Months { get; } =
            new ObservableCollection<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });

        public ObservableCollection<int> Years { get; } = new ObservableCollection<int>();

        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (_selectedMonth == value)
                    return;

                _selectedMonth = value;
                OnPropertyChanged();

                if (!_isInitializing)
                    RefreshSummary();
            }
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear == value)
                    return;

                _selectedYear = value;
                OnPropertyChanged();

                if (!_isInitializing)
                    RefreshSummary();
            }
        }

        // ------------------------------------
        // Totals strip values
        // ------------------------------------

        private decimal _totalIncome;
        public decimal TotalIncome
        {
            get => _totalIncome;
            private set { _totalIncome = value; OnPropertyChanged(); }
        }

        private decimal _totalExpense;
        public decimal TotalExpense
        {
            get => _totalExpense;
            private set { _totalExpense = value; OnPropertyChanged(); }
        }

        public decimal NetTotal => TotalIncome - TotalExpense;

        // ------------------------------------
        // Category breakdown grid
        // ------------------------------------

        public ObservableCollection<CategorySummary> CategorySummaries { get; } =
            new ObservableCollection<CategorySummary>();

        public SummaryViewModel()
        {
            _budgetService = new BudgetService(new TransactionRepository());

            _isInitializing = true;

            // Build year list (current year +/- 5)
            var now = DateTime.Today;
            for (int y = now.Year - 5; y <= now.Year + 5; y++)
                Years.Add(y);

            // IMPORTANT:
            // Set backing fields directly so we DON'T trigger RefreshSummary with month=0.
            _selectedYear = now.Year;
            _selectedMonth = now.Month;

            // Notify UI of initial values
            OnPropertyChanged(nameof(SelectedYear));
            OnPropertyChanged(nameof(SelectedMonth));

            _isInitializing = false;

            // Now it's safe to refresh once with valid Month/Year
            RefreshSummary();
        }

        private void RefreshSummary()
        {
            // Defensive guard (extra safety)
            if (SelectedMonth < 1 || SelectedMonth > 12)
                return;

            // 1) Totals strip
            var (income, expenses) = _budgetService.GetMonthlyTotals(SelectedYear, SelectedMonth);
            TotalIncome = income;
            TotalExpense = expenses;
            OnPropertyChanged(nameof(NetTotal));

            // 2) Category breakdown
            CategorySummaries.Clear();
            var summaries = _budgetService.GetCategorySummariesByMonth(SelectedYear, SelectedMonth);
            foreach (var s in summaries)
                CategorySummaries.Add(s);
        }

        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
