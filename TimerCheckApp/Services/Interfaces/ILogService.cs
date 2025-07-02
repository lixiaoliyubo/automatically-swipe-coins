using System;
using System.Collections.ObjectModel;

namespace TimerCheckApp.Services.Interfaces;

/// <summary>
/// 日志服务接口
/// </summary>
public interface ILogService : IDisposable
{
    /// <summary>
    /// 日志条目集合
    /// </summary>
    ObservableCollection<LogEntry> LogEntries { get; }

    /// <summary>
    /// 日志文本，用于在TextBox中显示
    /// </summary>
    string LogsText { get; }

    /// <summary>
    /// WebSocket日志文本，用于在TextBox中显示
    /// </summary>
    string WebSocketLogsText { get; }

    /// <summary>
    /// 从日志条目集合生成日志文本
    /// </summary>
    /// <param name="entries">日志条目集合</param>
    /// <returns>格式化的日志文本</returns>
    string GenerateLogText(ObservableCollection<LogEntry> entries);
    /// <summary>
    /// 记录信息级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    void LogInfo(string message, string deviceId, bool isWebSocketLog = false);

    /// <summary>
    /// 记录警告级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    void LogWarning(string message, string deviceId, bool isWebSocketLog = false);

    /// <summary>
    /// 记录错误级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    void LogError(string message, string deviceId, bool isWebSocketLog = false);

    /// <summary>
    /// 清理设备日志记录
    /// </summary>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    void ClearLogs(string deviceId = "", bool isWebSocketLog = false);


    /// <summary>
    /// 获取指定设备的日志
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    /// <returns>设备日志集合</returns>
    ObservableCollection<LogEntry> GetLogs(string deviceId = "", bool isWebSocketLog = false);
    
}