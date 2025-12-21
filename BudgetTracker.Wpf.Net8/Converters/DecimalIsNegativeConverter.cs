using System;
using System.Globalization;
using System.Windows.Data;

namespace BudgetTracker.Wpf.Net8.Converters
{
    /// <summary>
    /// Returns true when a decimal value is negative (value < 0).
    /// Used for row highlighting in the Budgets grid.
    /// </summary>
    public sealed class DecimalIsNegativeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d)
                return d < 0m;

            // If null or not a decimal, treat as not negative
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
