using System.Globalization;
using System.Windows.Data;

namespace TimerCheckApp.Converters;

/// <summary>
/// Convert a <see cref="bool"/> to a <see cref="Visibility"/> value.
/// </summary>
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets a value that defines whether the converter behavior is inverted or not
    /// </summary>
    public bool IsInverted { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool? valueBool = value as bool?;
        if (valueBool == null)
        {
            return Visibility.Collapsed;
        }

        if (valueBool.Value)
        {
            if (!IsInverted)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }
        else
        {
            if (!IsInverted)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
