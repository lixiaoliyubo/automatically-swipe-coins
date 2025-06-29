using TimerCheckApp.enums;

namespace TimerCheckApp.AdWatching
{
    /// <summary>
    /// 广告观看工厂接口
    /// </summary>
    public interface IAdWatchingFactory
    {
        /// <summary>
        /// 创建广告观看实例
        /// </summary>
        /// <param name="platformType">平台类型</param>
        /// <returns>广告观看实例</returns>
        IAdWatching Create(PlatformType platformType);
    }
}