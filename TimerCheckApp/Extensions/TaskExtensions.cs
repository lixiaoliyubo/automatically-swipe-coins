using System;

namespace TimerCheckApp.Extensions;

public static class TaskExtensions
{
    /// <summary>
    /// 重置任务状态，将所有任务的值设置为空字符串
    /// </summary>
    /// <param name="arrTask">任务列表</param>
    public static void ResetTaskStatus(this List<TaskItem> arrTask)
    {
        foreach (var task in arrTask)
        {
            task.Value = string.Empty;
        }
    }

    /// <summary>
    /// 检查指定任务的值是否不为空
    /// </summary>
    /// <param name="arrTask">任务列表</param>
    /// <param name="taskName">任务名称</param>
    /// <returns>如果任务存在且值不为空，则返回true；否则返回false</returns>
    public static bool GetTaskEmptyOrNull(this List<TaskItem> arrTask, string taskName)
    {
        return arrTask.Any(t => t.Name == taskName && !t.Value.IsNotEmptyOrNull());
    }
    /// <summary>
    /// 根据任务名称获取任务值
    /// </summary>
    /// <param name="arrTask">任务列表</param>
    /// <param name="taskName">任务名称</param>
    /// <returns>任务的值，如果任务不存在则返回空字符串</returns>
    public static string GetTaskItemValue(this List<TaskItem> arrTask, string taskName)
    {
        var model = arrTask.FirstOrDefault(t => t.Name == taskName);
        return model?.Value ?? string.Empty;
    }

    /// <summary>
    /// 根据任务名称获取TaskItem对象
    /// </summary>
    /// <param name="arrTask">任务列表</param>
    /// <param name="taskName">任务名称</param>
    /// <returns>对应的TaskItem对象，如果不存在则返回null</returns>
    public static TaskItem? GetTaskItem(this List<TaskItem> arrTask, string taskName)
    {
        return arrTask.FirstOrDefault(t => t.Name == taskName);
    }
    /// <summary>
    /// 检查指定任务的值数组是否包含指定值
    /// </summary>
    /// <param name="arrTask">任务列表</param>
    /// <param name="taskName">任务名称</param>
    /// <param name="taskValue">要检查的值</param>
    /// <returns>如果任务存在且其值数组包含指定值，则返回true；否则返回false</returns>
    public static bool GetTaskArrayContains(this List<TaskItem> arrTask, string taskName, string? taskValue)
    {
        var model = arrTask.FirstOrDefault(t => t.Name == taskName);
        if (model != null && !taskValue.IsNotEmptyOrNull())
        {
            return model.Value.GetArray().Contains(taskValue);
        }
        return false;
    }
    /// <summary>
    /// 检查指定任务的值数组是否包含指定值
    /// </summary>
    /// <param name="arrTask">任务列表</param>
    /// <param name="taskName">任务名称</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>如果任务存在且其值数组包含指定值，则返回string；否则返回null</returns>
    public static string? GetTaskArrayLast(this List<TaskItem> arrTask, string taskName, string? defaultValue = null)
    {
        var model = arrTask.FirstOrDefault(t => t.Name == taskName);

        return model?.Value.GetArray().LastOrDefault() ?? defaultValue;
    }
    /// <summary>
    /// 更新任务项
    /// </summary>
    /// <param name="arrTask">任务列表</param>
    /// <param name="taskName">任务名称</param>
    /// <param name="value">任务值</param>
    /// <returns>是否更新成功</returns>
    public static bool UpdateTaskItem(this List<TaskItem> arrTask, string taskName, string value)
    {
        if (arrTask == null) return false;

        var taskItem = arrTask.FirstOrDefault(t => t.Name == taskName);
        if (taskItem != null)
        {
            taskItem.Value = value;
            taskItem.UpdateTime = DateTime.Now;
            return true;
        }
        else
        {
            arrTask.Add(new TaskItem(taskName, value));
            return true;
        }
    }

    /// <summary>
    /// 检查任务列表是否包含指定任务
    /// </summary>
    /// <param name="arrTask">任务列表</param>
    /// <param name="taskName">任务名称</param>
    /// <returns>是否包含任务</returns>
    public static bool ContainsTaskItem(this List<TaskItem> arrTask, string taskName)
    {
        if (arrTask == null) return false;
        return arrTask.Any(t => t.Name == taskName);
    }
    
    /// <summary>
    /// 移除任务项
    /// </summary>
    /// <param name="arrTask">任务列表</param>
    /// <param name="taskName">任务名称</param>
    /// <returns>是否移除成功</returns>
    public static bool RemoveTaskItem(this List<TaskItem> arrTask, string taskName)
    {
        if (arrTask == null) return false;

        var taskItem = arrTask.FirstOrDefault(t => t.Name == taskName);
        if (taskItem != null)
        {
            return arrTask.Remove(taskItem);
        }
        return false;
    }

}
