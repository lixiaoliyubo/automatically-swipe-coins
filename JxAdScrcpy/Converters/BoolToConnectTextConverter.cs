using System.Globalization;
using System.Windows.Data;

namespace JxAdScrcpy.Converters
{
    public class BoolToConnectTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isConnected && isConnected ? "断开连接" : "连接服务";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}