using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BudgetTracker.Wpf.Net8.Converters
{
    public sealed class DecimalSignToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d)
                return d < 0 ? Brushes.IndianRed : Brushes.SeaGreen;

            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
