using TimerCheckApp.enums;
using System.Collections.Concurrent;
using TimerCheckApp.Models;

namespace TimerCheckApp.AdWatching
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
        public Task AutomateAdWatching(DeviceModel device, CancellationToken cancellationToken);

    }
}