using System.Windows.Controls;
using System.Windows.Input;
using BudgetTracker.Wpf.Net8.ViewModels;

namespace BudgetTracker.Wpf.Net8.Views
{
    public partial class TransactionsView : UserControl
    {
        public TransactionsView()
        {
            InitializeComponent();
        }

        private void TransactionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not TransactionsViewModel vm) return;
            if (vm.SelectedTransaction == null) return;

            if (vm.EditSelectedCommand.CanExecute(null))
                vm.EditSelectedCommand.Execute(null);
        }
    }
}
