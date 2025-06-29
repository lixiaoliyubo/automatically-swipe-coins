using System.Globalization;
using System.Windows.Data;
using TimerCheckApp.Models;

namespace TimerCheckApp.Converters;

public class MultiParamsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return new CommandParameters(values[0], new RoutedEventArgs() {Source = values[1] });
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
