using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AdAuto.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            bool inverse = parameter is bool p && p;

            if (parameter != null)
            {
                return !boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}