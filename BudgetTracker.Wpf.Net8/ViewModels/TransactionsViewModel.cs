using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public RelayCommand AddTransactionCommand { get; }
        public RelayCommand DeleteSelectedCommand { get; }

        public TransactionsViewModel()
        {
            var repo = new TransactionRepository();
            _budgetService = new BudgetService(repo);

            RefreshCommand = new RelayCommand(Refresh);
            AddTransactionCommand = new RelayCommand(OpenAddDialog);
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

        private void OpenAddDialog()
        {
            // Owner = current main window so dialog centers nicely
            var window = new AddTransactionWindow
            {
                Owner = Application.Current?.MainWindow
            };

            var result = window.ShowDialog();
            if (result != true)
                return;

            // Read the created transaction from the dialog VM
            if (window.DataContext is AddTransactionViewModel vm && vm.CreatedTransaction != null)
            {
                _budgetService.AddTransaction(vm.CreatedTransaction);
                Refresh();
            }
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
