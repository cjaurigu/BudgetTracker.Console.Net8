using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using BudgetTracker.Console.Net8.Data;
using BudgetTracker.Console.Net8.Domain;
using BudgetTracker.Console.Net8.Services;
using BudgetTracker.Wpf.Net8.Views;

namespace BudgetTracker.Wpf.Net8.ViewModels
{
    /// <summary>
    /// ViewModel for Categories tab.
    /// Uses existing CategoryRepository + CategoryService from Console/Core layer.
    /// </summary>
    public sealed class CategoriesViewModel : INotifyPropertyChanged
    {
        private readonly CategoryService _categoryService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Category> Categories { get; } = new();

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (!ReferenceEquals(_selectedCategory, value))
                {
                    _selectedCategory = value;
                    OnPropertyChanged();

                    EditSelectedCommand.RaiseCanExecuteChanged();
                    DeleteSelectedCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value ?? string.Empty;
                    OnPropertyChanged();

                    ClearSearchCommand.RaiseCanExecuteChanged();
                    ApplySearch();
                }
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand AddCategoryCommand { get; }
        public RelayCommand EditSelectedCommand { get; }
        public RelayCommand DeleteSelectedCommand { get; }
        public RelayCommand ClearSearchCommand { get; }

        public CategoriesViewModel()
        {
            var repo = new CategoryRepository();
            _categoryService = new CategoryService(repo);

            RefreshCommand = new RelayCommand(Refresh);
            AddCategoryCommand = new RelayCommand(OpenAddDialog);
            EditSelectedCommand = new RelayCommand(OpenEditDialog, () => SelectedCategory != null);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedCategory != null);
            ClearSearchCommand = new RelayCommand(ClearSearch, () => !string.IsNullOrWhiteSpace(SearchText));

            Refresh();
        }

        private void Refresh()
        {
            ApplySearch();
        }

        private void ApplySearch()
        {
            var keyword = (SearchText ?? string.Empty).Trim();

            var list = _categoryService.GetAllCategories();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                list = list
                    .Where(c =>
                        (c.Name ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        c.Id.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            Categories.Clear();
            foreach (var c in list)
                Categories.Add(c);

            if (SelectedCategory != null && Categories.All(c => c.Id != SelectedCategory.Id))
                SelectedCategory = null;
        }

        private void ClearSearch()
        {
            SearchText = string.Empty; // triggers ApplySearch
        }

        private void OpenAddDialog()
        {
            var win = new CategoryEditWindow("Add Category", "")
            {
                Owner = Application.Current?.MainWindow
            };

            var result = win.ShowDialog();
            if (result != true)
                return;

            try
            {
                _categoryService.AddCategory(win.CategoryName);
                ApplySearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Add Category Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenEditDialog()
        {
            if (SelectedCategory == null)
                return;

            var win = new CategoryEditWindow("Rename Category", SelectedCategory.Name)
            {
                Owner = Application.Current?.MainWindow
            };

            var result = win.ShowDialog();
            if (result != true)
                return;

            try
            {
                _categoryService.RenameCategory(SelectedCategory.Id, win.CategoryName);
                ApplySearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Rename Category Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSelected()
        {
            if (SelectedCategory == null)
                return;

            var confirm = MessageBox.Show(
                $"Delete category #{SelectedCategory.Id}?\n\n{SelectedCategory.Name}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                _categoryService.DeleteCategory(SelectedCategory.Id);
                ApplySearch();
            }
            catch (Exception ex)
            {
                // If there is a FK relationship from Transactions -> Categories, SQL may block delete.
                MessageBox.Show(
                    ex.Message + "\n\nTip: If this category is used by transactions, delete/update those transactions first.",
                    "Delete Category Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
