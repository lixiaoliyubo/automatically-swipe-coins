using System;
using System.Collections.Generic;
using System.Linq;
using TimerCheckApp.Models;

namespace TimerCheckApp.AdWatching
{
    /// <summary>
    /// 广告观看自动化基类
    /// </summary>
    public abstract class AdWatchingBase 
    {
        /// <summary>
        /// 任务列表
        /// </summary>
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        /// <summary>
        /// 执行广告观看自动化
        /// </summary>
        /// <param name="device">设备模型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        public abstract Task AutomateAdWatching(DeviceModel device, CancellationToken cancellationToken);

    }
}