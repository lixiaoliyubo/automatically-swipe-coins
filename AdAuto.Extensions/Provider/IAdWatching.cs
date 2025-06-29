using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AdAuto.Extensions.Provider;

/// <summary>
/// 广告观看自动化接口
/// </summary>
public interface IAdWatching : IDisposable
{
    /// <summary>
    /// 执行广告观看自动化
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task AutomateAdWatching(CancellationToken cancellationToken);

    public ConcurrentDictionary<string, List<string>> arrTask { get; set; }
}