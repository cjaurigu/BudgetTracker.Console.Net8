using System.Windows.Controls;
using System.Windows.Input;
using BudgetTracker.Wpf.Net8.ViewModels;

namespace BudgetTracker.Wpf.Net8.Views
{
    public partial class CategoriesView : UserControl
    {
        public CategoriesView()
        {
            InitializeComponent();
        }

        private void CategoriesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not CategoriesViewModel vm) return;
            if (vm.SelectedCategory == null) return;

            if (vm.EditSelectedCommand.CanExecute(null))
                vm.EditSelectedCommand.Execute(null);
        }
    }
}
