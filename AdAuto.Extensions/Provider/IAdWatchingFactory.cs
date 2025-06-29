using AdAuto.Extensions.enums;
using System;

namespace AdAuto.Extensions.Provider;

/// <summary>
/// 广告观看实例工厂接口
/// </summary>
public interface IAdWatchingFactory
{
    /// <summary>
    /// 根据平台类型创建广告观看实例
    /// </summary>
    /// <param name="platform">平台类型</param>
    /// <param name="mainViewModel">主视图模型实例</param>
    /// <returns>广告观看实例</returns>
    IAdWatching CreateAdWatching(PlatformType platform, IMainViewModel mainViewModel);
}

/// <summary>
/// 广告观看实例工厂实现
/// </summary>
public class AdWatchingFactory : IAdWatchingFactory
{
    /// <summary>
    /// 根据平台类型创建广告观看实例
    /// </summary>
    /// <param name="platform">平台类型</param>
    /// <param name="mainViewModel">主视图模型实例</param>
    /// <returns>广告观看实例</returns>
    public IAdWatching CreateAdWatching(PlatformType platform, IMainViewModel mainViewModel)
    {
        return platform switch
        {
            PlatformType.Douyin => new DouyinAdWatching(mainViewModel),
            PlatformType.Kuaishou => new KuaishouAdWatching(mainViewModel),
            _ => throw new ArgumentException($"不支持的平台类型：{platform}", nameof(platform))
        };
    }
}