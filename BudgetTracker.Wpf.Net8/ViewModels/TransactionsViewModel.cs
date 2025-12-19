using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Services;

namespace BudgetTracker.Wpf.Net8.ViewModels
{
    public sealed class TransactionsViewModel : INotifyPropertyChanged
    {
        private readonly BudgetService _budgetService;

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
                }
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand AddQuickTransactionCommand { get; }
        public RelayCommand DeleteSelectedCommand { get; }

        public TransactionsViewModel()
        {
            // Uses your existing ADO.NET repository + service
            var repo = new TransactionRepository();
            _budgetService = new BudgetService(repo);

            RefreshCommand = new RelayCommand(Refresh);
            AddQuickTransactionCommand = new RelayCommand(AddQuickTransaction);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedTransaction != null);

            Refresh();
        }

        private void Refresh()
        {
            Transactions.Clear();
            var all = _budgetService.GetAllTransactions();

            foreach (var t in all)
                Transactions.Add(t);
        }

        private void AddQuickTransaction()
        {
            var tx = new Transaction
            {
                Description = "WPF Quick Add",
                Amount = 1.00m,
                Type = "Expense",
                Category = "Uncategorized",
                Date = DateTime.Today
            };

            _budgetService.AddTransaction(tx);
            Refresh();
        }

        private void DeleteSelected()
        {
            if (SelectedTransaction == null)
                return;

            _budgetService.DeleteTransaction(SelectedTransaction.Id);
            Refresh();
        }

        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
