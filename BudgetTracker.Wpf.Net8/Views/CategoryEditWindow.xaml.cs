using System.Windows;

namespace BudgetTracker.Wpf.Net8.Views
{
    public partial class CategoryEditWindow : Window
    {
        public string CategoryName => NameTextBox.Text.Trim();

        public CategoryEditWindow(string title, string initialName)
        {
            InitializeComponent();

            Title = title;
            NameTextBox.Text = initialName ?? string.Empty;

            Loaded += (_, __) =>
            {
                NameTextBox.Focus();
                NameTextBox.SelectAll();
            };
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                MessageBox.Show("Category name cannot be empty.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                NameTextBox.Focus();
                return;
            }

            DialogResult = true;
        }
    }
}
