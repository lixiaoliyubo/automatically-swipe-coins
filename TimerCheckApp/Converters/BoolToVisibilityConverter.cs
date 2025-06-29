using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TimerCheckApp.Converters
{
    /// <summary>
    /// 将布尔值转换为可见性的转换器
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值转换为可见性
        /// </summary>
        /// <param name="value">布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数，如果为"Inverse"则反转结果</param>
        /// <param name="culture">区域信息</param>
        /// <returns>可见性枚举值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool bValue = false;
            if (value is bool)
            {
                bValue = (bool)value;
            }
            else if (value is bool?)
            {
                bool? tmp = (bool?)value;
                bValue = tmp.HasValue ? tmp.Value : false;
            }
            
            // 检查是否需要反转结果
            if (parameter != null && parameter.ToString() == "Inverse")
            {
                bValue = !bValue;
            }
            
            return bValue ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 将可见性转换回布尔值
        /// </summary>
        /// <param name="value">可见性枚举值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数，如果为"Inverse"则反转结果</param>
        /// <param name="culture">区域信息</param>
        /// <returns>布尔值</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            bool result = visibility == Visibility.Visible;
            
            // 检查是否需要反转结果
            if (parameter != null && parameter.ToString() == "Inverse")
            {
                result = !result;
            }
            
            return result;
        }
    }
}