using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Domain.Enums;
using BudgetTracker.Console.Net8.Services;

namespace BudgetTracker.Wpf.Net8.ViewModels
{
    /// <summary>
    /// ViewModel for the Add Transaction dialog.
    /// Holds form fields, loads categories, validates input, and builds a Transaction to save.
    /// </summary>
    public sealed class AddTransactionViewModel : INotifyPropertyChanged
    {
        private readonly CategoryService _categoryService;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Window listens to this to close itself.
        /// true = DialogResult true, false = DialogResult false.
        /// </summary>
        public event Action<bool>? RequestClose;

        // -----------------------------
        // Form Fields (bound to UI)
        // -----------------------------

        private DateTime _date = DateTime.Today;
        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TransactionType> Types { get; } =
            new ObservableCollection<TransactionType>((TransactionType[])Enum.GetValues(typeof(TransactionType)));

        private TransactionType _selectedType = TransactionType.Expense;
        public TransactionType SelectedType
        {
            get => _selectedType;
            set { _selectedType = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Category> Categories { get; } = new();

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        // Keep Amount as string in the UI so we can validate/parse safely (no crashes on bad typing)
        private string _amountText = string.Empty;
        public string AmountText
        {
            get => _amountText;
            set { _amountText = value; OnPropertyChanged(); }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// On Save, we build this Transaction so the caller (TransactionsViewModel) can persist it.
        /// </summary>
        public Transaction? CreatedTransaction { get; private set; }

        // Commands
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public AddTransactionViewModel()
        {
            // Load categories using your existing service + repository (ADO.NET)
            _categoryService = new CategoryService(new CategoryRepository());

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);

            LoadCategories();
        }

        private void LoadCategories()
        {
            Categories.Clear();

            var all = _categoryService.GetAllCategories();
            foreach (var c in all)
                Categories.Add(c);

            // Optional: pre-select first category if available
            if (Categories.Count > 0)
                SelectedCategory = Categories[0];
        }

        private void Save()
        {
            ErrorMessage = string.Empty;

            // Basic validation (keep it simple for milestone 1)
            var desc = (Description ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(desc))
            {
                ErrorMessage = "Description is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(AmountText))
            {
                ErrorMessage = "Amount is required.";
                return;
            }

            if (!decimal.TryParse(AmountText.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var amount))
            {
                ErrorMessage = "Amount must be a valid number (example: 12.34).";
                return;
            }

            if (amount <= 0)
            {
                ErrorMessage = "Amount must be greater than zero.";
                return;
            }

            // Build Transaction matching your domain model expectations
            var categoryName = SelectedCategory?.Name ?? "Uncategorized";

            CreatedTransaction = new Transaction
            {
                Date = Date,
                Description = desc,
                Amount = amount,
                Type = SelectedType.ToString(),        // "Income" or "Expense"
                Category = categoryName,
                CategoryId = SelectedCategory?.Id
            };

            // Tell the Window to close with DialogResult=true
            RequestClose?.Invoke(true);
        }

        private void Cancel()
        {
            ErrorMessage = string.Empty;
            CreatedTransaction = null;

            // Close dialog with DialogResult=false
            RequestClose?.Invoke(false);
        }

        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}