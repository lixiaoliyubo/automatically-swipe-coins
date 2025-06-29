using TimerCheckApp.Models;
using System.ComponentModel;
using System.Windows.Markup;

namespace TimerCheckApp.Extensions;


/// <summary>
/// 枚举扩展类，用于获取枚举值及其描述
/// https://blog.csdn.net/kristen_dou/article/details/133675830
/// </summary>
public class EnumerationExtension : MarkupExtension
{
    /// <summary>
    /// 获取或设置枚举类型。
    /// </summary>
    public Type EnumType { get; set; }

    /// <summary>
    /// 初始化 <see cref="EnumerationExtension"/> 类的新实例。
    /// </summary>
    /// <param name="enumType">要使用的枚举类型。</param>
    /// <exception cref="ArgumentNullException">如果 <paramref name="enumType"/> 为 null。</exception>
    /// <exception cref="ArgumentException">如果 <paramref name="enumType"/> 不是枚举类型。</exception>
    public EnumerationExtension(Type? enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);
        if (!enumType.IsEnum)
            throw new ArgumentException("Type must be an Enum.");
        EnumType = enumType;
    }

    /// <summary>
    /// 提供枚举值的数组，包含每个值及其描述。
    /// </summary>
    /// <param name="serviceProvider">服务提供者。</param>
    /// <returns>包含枚举值和描述的数组。</returns>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var enumValues = Enum.GetValues(EnumType).Cast<object>();

        // 检查枚举的底层类型是否为 int
        if (Enum.GetUnderlyingType(EnumType) == typeof(int))
        {
            enumValues = enumValues.OrderBy(e => (int)e);
        }

        return (from object enumValue in enumValues
                select new EnumerationMember { Value = enumValue, Description = GetDescription(enumValue) }).ToArray();
    }

    /// <summary>
    /// 获取指定枚举值的描述。
    /// </summary>
    /// <param name="enumValue">要获取描述的枚举值。</param>
    /// <returns>枚举值的描述，如果没有描述则返回枚举值的字符串表示。</returns>
    private string GetDescription(object enumValue)
    {
        return EnumType.GetField(enumValue.ToString() ?? "")?.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() is DescriptionAttribute descriptionAttribute ? descriptionAttribute.Description : enumValue.ToString() ?? "";
    }
}