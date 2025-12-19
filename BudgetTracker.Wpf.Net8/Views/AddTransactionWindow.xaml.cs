using System.Windows;
using BudgetTracker.Wpf.Net8.ViewModels;

namespace BudgetTracker.Wpf.Net8.Views
{
    /// <summary>
    /// Code-behind stays tiny:
    /// just listens for VM close requests and sets DialogResult.
    /// </summary>
    public partial class AddTransactionWindow : Window
    {
        public AddTransactionWindow()
        {
            InitializeComponent();

            if (DataContext is AddTransactionViewModel vm)
            {
                vm.RequestClose += Vm_RequestClose;
            }
        }

        private void Vm_RequestClose(bool dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }
    }
}
