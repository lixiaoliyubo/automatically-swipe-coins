using AdAuto.enums;
using System.Collections.Concurrent;

namespace AdAuto.ViewModels
{
    /// <summary>
    /// 广告观看自动化接口
    /// </summary>
    public interface IAdWatching
    {
        /// <summary>
        /// 执行广告观看自动化
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        public Task AutomateAdWatching(CancellationToken cancellationToken);

        public ConcurrentDictionary<string, string> arrTask { get; set; }



        /// <summary>
        /// 设置/更新当前页面
        /// </summary>
        /// <param name="appPage"></param>
        public void UpdateAppPageEnum(AppPageEnum  appPage);
    }
}