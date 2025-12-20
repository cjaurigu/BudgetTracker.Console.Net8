using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Services;

namespace BudgetTracker.Wpf.Net8.ViewModels
{
    public sealed class SummaryViewModel : INotifyPropertyChanged
    {
        private readonly BudgetService _budgetService;
        private bool _isInitializing;

        public event PropertyChangedEventHandler? PropertyChanged;

        // -------------------------------
        // Month / Year selectors
        // -------------------------------

        public ObservableCollection<int> Months { get; } =
            new ObservableCollection<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });

        public ObservableCollection<int> Years { get; } = new ObservableCollection<int>();

        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (_selectedMonth == value) return;
                _selectedMonth = value;
                OnPropertyChanged();
                if (!_isInitializing) RefreshSummary();
            }
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear == value) return;
                _selectedYear = value;
                OnPropertyChanged();
                if (!_isInitializing) RefreshSummary();
            }
        }

        // -------------------------------
        // Totals
        // -------------------------------

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

        // -------------------------------
        // Category breakdown
        // -------------------------------

        public ObservableCollection<CategorySummary> CategorySummaries { get; } =
            new ObservableCollection<CategorySummary>();

        public SummaryViewModel()
        {
            _budgetService = new BudgetService(new TransactionRepository());

            _isInitializing = true;

            var now = DateTime.Today;
            for (int y = now.Year - 5; y <= now.Year + 5; y++)
                Years.Add(y);

            _selectedYear = now.Year;
            _selectedMonth = now.Month;

            OnPropertyChanged(nameof(SelectedYear));
            OnPropertyChanged(nameof(SelectedMonth));

            _isInitializing = false;

            RefreshSummary();
        }

        private void RefreshSummary()
        {
            if (SelectedMonth < 1 || SelectedMonth > 12)
                return;

            var (income, expense) =
                _budgetService.GetMonthlyTotals(SelectedYear, SelectedMonth);

            TotalIncome = income;
            TotalExpense = expense;
            OnPropertyChanged(nameof(NetTotal));

            CategorySummaries.Clear();
            foreach (var s in _budgetService.GetCategorySummariesByMonth(SelectedYear, SelectedMonth))
                CategorySummaries.Add(s);

            // 🔽 Default sort: highest expense first
            var view = CollectionViewSource.GetDefaultView(CategorySummaries);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(
                new SortDescription(nameof(CategorySummary.TotalExpense),
                ListSortDirection.Descending));
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
