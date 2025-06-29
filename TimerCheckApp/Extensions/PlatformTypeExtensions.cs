using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using TimerCheckApp.enums;

namespace TimerCheckApp.Extensions;

/// <summary>
/// PlatformType的扩展方法
/// </summary>
public static class PlatformTypeExtensions
{
    /// <summary>
    /// 获取枚举值的包名
    /// </summary>
    /// <param name="value">枚举值</param>
    /// <returns>包名</returns>
    public static string GetPackageName(this PlatformType value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo == null) return string.Empty;

        var attribute = fieldInfo.GetCustomAttribute<AppPackageAttribute>();
        return attribute?.PackageName ?? string.Empty;
    }

    /// <summary>
    /// 获取枚举值的描述
    /// </summary>
    /// <param name="value">枚举值</param>
    /// <returns>描述</returns>
    public static string GetDescription(this PlatformType value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo == null) return string.Empty;

        var attribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }

    /// <summary>
    /// 获取枚举值的所有属性
    /// </summary>
    /// <param name="value">枚举值</param>
    /// <returns>属性字典</returns>
    public static Dictionary<string, object> GetAllAttributes(this PlatformType value)
    {
        var result = new Dictionary<string, object>();
        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo == null) return result;

        // 获取所有自定义属性
        var attributes = fieldInfo.GetCustomAttributes(false);
        foreach (var attribute in attributes)
        {
            if (attribute is DescriptionAttribute descAttr)
            {
                result["Description"] = descAttr.Description;
            }
            else if (attribute is AppPackageAttribute packageAttr)
            {
                result["PackageName"] = packageAttr.PackageName;
            }
            // 可以添加其他属性类型的处理
        }

        return result;
    }

    /// <summary>
    /// 获取所有平台类型及其属性
    /// </summary>
    /// <returns>平台类型及其属性的字典</returns>
    public static Dictionary<PlatformType, Dictionary<string, object>> GetAllPlatformAttributes()
    {
        var result = new Dictionary<PlatformType, Dictionary<string, object>>();
        var values = Enum.GetValues(typeof(PlatformType)).Cast<PlatformType>();

        foreach (var value in values)
        {
            result[value] = value.GetAllAttributes();
        }

        return result;
    }
}