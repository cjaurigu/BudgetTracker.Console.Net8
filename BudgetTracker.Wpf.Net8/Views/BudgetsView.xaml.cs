using System.Windows.Controls;
using System.Windows.Input;
using BudgetTracker.Wpf.Net8.ViewModels;

namespace BudgetTracker.Wpf.Net8.Views
{
    public partial class BudgetsView : UserControl
    {
        public BudgetsView()
        {
            InitializeComponent();
        }

        private void BudgetsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not BudgetsViewModel vm) return;
            if (vm.SelectedRow == null) return;

            if (vm.EditSelectedCommand.CanExecute(null))
                vm.EditSelectedCommand.Execute(null);
        }
    }
}
