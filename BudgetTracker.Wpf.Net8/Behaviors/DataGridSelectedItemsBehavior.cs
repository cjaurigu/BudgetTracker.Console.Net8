using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace BudgetTracker.Wpf.Net8.Behaviors
{
    /// <summary>
    /// Enables binding DataGrid.SelectedItems to a ViewModel IList.
    /// </summary>
    public static class DataGridSelectedItemsBehavior
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(IList),
                typeof(DataGridSelectedItemsBehavior),
                new PropertyMetadata(null, OnSelectedItemsChanged));

        public static IList GetSelectedItems(DependencyObject obj)
            => (IList)obj.GetValue(SelectedItemsProperty);

        public static void SetSelectedItems(DependencyObject obj, IList value)
            => obj.SetValue(SelectedItemsProperty, value);

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid grid)
                return;

            grid.SelectionChanged -= Grid_SelectionChanged;
            grid.SelectionChanged += Grid_SelectionChanged;

            Sync(grid);
        }

        private static void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid grid)
                Sync(grid);
        }

        private static void Sync(DataGrid grid)
        {
            var bound = GetSelectedItems(grid);
            if (bound == null)
                return;

            bound.Clear();
            foreach (var item in grid.SelectedItems)
                bound.Add(item);
        }
    }
}
