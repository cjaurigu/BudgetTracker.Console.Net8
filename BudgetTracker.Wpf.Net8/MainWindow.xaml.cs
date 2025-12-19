using System.Windows;
using System.Windows.Input;
using BudgetTracker.Wpf.Net8.ViewModels;

namespace BudgetTracker.Wpf.Net8
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// Note:
    /// We keep code-behind minimal. This handler is purely a UI gesture:
    /// double-clicking a row triggers the ViewModel command.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TransactionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TransactionsViewModel vm && vm.EditSelectedCommand.CanExecute(null))
            {
                vm.EditSelectedCommand.Execute(null);
            }
        }
    }
}
