
using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Windows.Data;
using Yuandl.Common.Helper;

namespace TimerCheckApp.Converters;


public class ObjectToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return "";
        }
        if (IsEntityClass(value))
        {
            return JsonSerializer.Serialize(value);
        }
        if (IsCollection(value))
        {
            return value.ToJson();
        }
        
        if (value is string _value)
        {
            return (int)Math.Round(System.Convert.ToDouble(_value), 0);
        }
        else if (value is double _double)
        {

            return (int)Math.Round(System.Convert.ToDouble(_double), 0);
        }
        return value.ObjToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
    public bool IsEntityClass(object obj)
    {
        if (obj == null) return false;

        var type = obj.GetType();
        return type.IsClass && !type.IsAbstract && !type.IsGenericType &&
               type.GetProperties().Any();
    }
    static bool IsCollection(object obj)
    {
        if (obj == null) return false;

        Type type = obj.GetType();

        // 检查是否实现 IEnumerable 或是数组
        if (typeof(IEnumerable).IsAssignableFrom(type) || type.IsArray)
        {
            return true;
        }

        // 检查是否是已构造的泛型类型
        if (type.IsGenericType && type.IsConstructedGenericType)
        {
            // 检查是否是 IEnumerable<T>
            if (typeof(IEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                return true;
            }
        }

        return false;
    }
}
