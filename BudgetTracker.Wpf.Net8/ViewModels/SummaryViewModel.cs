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
    /// ViewModel for the Monthly / Category Summary tab.
    /// </summary>
    public sealed class SummaryViewModel : INotifyPropertyChanged
    {
        private readonly BudgetService _budgetService;

        public event PropertyChangedEventHandler? PropertyChanged;

        // -----------------------------
        // UI-bound collections
        // -----------------------------
        public ObservableCollection<int> Years { get; } = new();
        public ObservableCollection<int> Months { get; } = new();
        public ObservableCollection<CategorySummary> Summaries { get; } = new();

        // -----------------------------
        // Selected values
        // -----------------------------
        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged();
                LoadSummary();
            }
        }

        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                _selectedMonth = value;
                OnPropertyChanged();
                LoadSummary();
            }
        }

        public SummaryViewModel()
        {
            var repo = new TransactionRepository();
            _budgetService = new BudgetService(repo);

            LoadYearMonthOptions();

            // Default to current month/year
            SelectedYear = DateTime.Today.Year;
            SelectedMonth = DateTime.Today.Month;
        }

        // -----------------------------
        // Loaders
        // -----------------------------
        private void LoadYearMonthOptions()
        {
            Years.Clear();
            Months.Clear();

            // Simple range: last 5 years → next year
            var currentYear = DateTime.Today.Year;
            for (int y = currentYear - 5; y <= currentYear + 1; y++)
                Years.Add(y);

            for (int m = 1; m <= 12; m++)
                Months.Add(m);
        }

        private void LoadSummary()
        {
            if (SelectedYear <= 0 || SelectedMonth <= 0)
                return;

            Summaries.Clear();

            var results = _budgetService.GetCategorySummariesByMonth(
                SelectedYear,
                SelectedMonth);

            foreach (var item in results)
                Summaries.Add(item);
        }

        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
