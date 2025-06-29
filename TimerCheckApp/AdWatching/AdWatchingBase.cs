using System;
using System.Collections.Generic;
using System.Linq;
using TimerCheckApp.Models;

namespace TimerCheckApp.AdWatching
{
    /// <summary>
    /// 广告观看自动化基类
    /// </summary>
    public abstract class AdWatchingBase : IAdWatching
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

        /// <summary>
        /// 根据任务名称获取任务项
        /// </summary>
        /// <param name="taskName">任务名称</param>
        /// <returns>任务项</returns>
        public TaskItem GetTask(string taskName)
        {
            var task = Tasks.FirstOrDefault(t => t.Name == taskName);
            if (task == null)
            {
                task = new TaskItem(taskName);
                Tasks.Add(task);
            }
            return task;
        }

        /// <summary>
        /// 更新任务值
        /// </summary>
        /// <param name="taskName">任务名称</param>
        /// <param name="value">任务值</param>
        public void UpdateTask(string taskName, string value)
        {
            var task = GetTask(taskName);
            task.UpdateValue(value);
        }

        /// <summary>
        /// 初始化任务列表
        /// </summary>
        /// <param name="taskNames">任务名称数组</param>
        protected void InitializeTasks(params string[] taskNames)
        {
            foreach (var taskName in taskNames)
            {
                if (!Tasks.Any(t => t.Name == taskName))
                {
                    Tasks.Add(new TaskItem(taskName));
                }
            }
        }

        /// <summary>
        /// 重置任务状态
        /// </summary>
        /// <param name="excludeTaskNames">排除的任务名称</param>
        protected void ResetTaskStatus(params string[] excludeTaskNames)
        {
            foreach (var task in Tasks)
            {
                if (!excludeTaskNames.Contains(task.Name))
                {
                    task.UpdateValue(string.Empty);
                }
            }
        }
    }
}