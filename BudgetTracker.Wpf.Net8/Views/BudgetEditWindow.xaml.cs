using System.Globalization;
using System.Windows;

namespace BudgetTracker.Wpf.Net8.Views
{
    public partial class BudgetEditWindow : Window
    {
        public decimal BudgetAmount { get; private set; }

        public BudgetEditWindow(string title, decimal initialAmount)
        {
            InitializeComponent();

            Title = title;
            AmountTextBox.Text = initialAmount.ToString("0.##", CultureInfo.InvariantCulture);

            Loaded += (_, __) =>
            {
                AmountTextBox.Focus();
                AmountTextBox.SelectAll();
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var raw = (AmountTextBox.Text ?? string.Empty).Trim();

            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                MessageBox.Show("Enter a valid number (example: 250 or 250.00).", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                AmountTextBox.Focus();
                return;
            }

            if (amount < 0)
            {
                MessageBox.Show("Budget amount cannot be negative.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                AmountTextBox.Focus();
                return;
            }

            BudgetAmount = amount;
            DialogResult = true;
        }
    }
}
