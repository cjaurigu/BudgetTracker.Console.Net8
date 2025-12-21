using System.Windows.Controls;
using System.Windows.Input;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Wpf.Net8.ViewModels;

namespace BudgetTracker.Wpf.Net8.Views
{
    public partial class TransactionsView : UserControl
    {
        public TransactionsView()
        {
            InitializeComponent();
        }

        // Commit edits when user presses Enter (makes saving feel snappy)
        private void TransactionsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            if (sender is DataGrid grid)
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
        }

        // Auto-save when a row edit is committed
        private void TransactionsGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (DataContext is not TransactionsViewModel vm)
                return;

            if (e.Row.Item is Transaction tx)
            {
                vm.SaveEditedTransaction(tx);
            }
        }
    }
}
