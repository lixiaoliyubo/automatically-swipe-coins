using AdAuto.Helper;
using System.Globalization;
using System.Windows.Data;

namespace AdAuto.Converters;

public class Base64ToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var base64String = (String)value;
        if (string.IsNullOrEmpty(base64String) || base64String == "data:image/jpg;base64,")
        {

            return null;
        }
        else if (base64String.Contains("\\"))
        {
            return base64String;
        }

        if (base64String.StartsWith("data:image/"))
        {

            // 去除 data:image/gif;base64, 前缀
            base64String = base64String.Substring(base64String.IndexOf(',') + 1);
        }


        return ImageHelper.Base64ToBitmapSource(base64String);
    }
    public object? ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
