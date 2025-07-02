using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Linq;
using TimerCheckApp.Models;
using TimerCheckApp.Services.Interfaces;

#if WPF
using System.Windows;
#endif

namespace TimerCheckApp.Services;
/// <summary>
/// 日志服务实现
/// </summary>
public class LogService : ILogService, INotifyPropertyChanged
{
    private readonly Logger _logger;
    private readonly ObservableCollection<LogEntry> _logEntries = new();
    private readonly ObservableCollection<LogEntry> _webSocketLogEntries = new();
    private const int MaxLogEntries = 1000;

    /// <summary>
    /// 属性变更事件
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// 触发属性变更通知
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 日志文本，用于在TextBox中显示，从设备日志条目生成
    /// </summary>
    public string LogsText => GenerateLogText(_logEntries);

    /// <summary>
    /// WebSocket日志文本，用于在TextBox中显示，从WebSocket日志条目生成
    /// </summary>
    public string WebSocketLogsText => GenerateLogText(_webSocketLogEntries);

    /// <summary>
    /// 从日志条目集合生成日志文本
    /// </summary>
    /// <param name="entries">日志条目集合</param>
    /// <returns>格式化的日志文本</returns>
    public string GenerateLogText(ObservableCollection<LogEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var entry in entries)
        {
            sb.AppendLine(FormatLogEntry(entry));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 日志文本最大长度
    /// </summary>
    private const int MaxLogTextLength = 100000; // 限制日志文本长度，防止内存无限增长

    /// <summary>
    /// 格式化日志条目为文本
    /// </summary>
    /// <param name="entry">日志条目</param>
    /// <returns>格式化后的文本</returns>
    private string FormatLogEntry(LogEntry entry)
    {
        return $"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] [{entry.DeviceId}] {entry.Message}";
    }

    /// <summary>
    /// 日志条目集合
    /// </summary>
    public ObservableCollection<LogEntry> LogEntries => _logEntries;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LogService()
    {
        _logger = InitializeLogger();
    }

    /// <summary>
    /// 初始化NLog日志记录器
    /// </summary>
    /// <returns>配置好的Logger实例</returns>
    private Logger InitializeLogger()
    {
        var config = new LoggingConfiguration();

        // 创建日志目录
        var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);

        // 文件目标 - 按日期分割日志文件
        var fileTarget = new FileTarget("file")
        {
            FileName = Path.Combine(logDirectory, "${shortdate}.log"),
            Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=tostring}"
        };

        // 控制台目标 - 用于调试
        var consoleTarget = new ConsoleTarget("console")
        {
            Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}"
        };

        // 添加规则
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget);
        config.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);

        // 应用配置
        LogManager.Configuration = config;

        return LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// 记录信息级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    public void LogInfo(string message, string deviceId, bool isWebSocketLog = false)
    {
        if (isWebSocketLog)
        {
            _logger.Info($"[WebSocket][{deviceId}] {message}");
        }
        else
        {
            _logger.Info($"[{deviceId}] {message}");
        }
        AddLogEntry("Info", message, deviceId, isWebSocketLog);
    }

    /// <summary>
    /// 记录警告级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    public void LogWarning(string message, string deviceId, bool isWebSocketLog = false)
    {
        if (isWebSocketLog)
        {
            _logger.Warn($"[WebSocket][{deviceId}] {message}");
        }
        else
        {
            _logger.Warn($"[{deviceId}] {message}");
        }
        AddLogEntry("Warning", message, deviceId, isWebSocketLog);
    }

    /// <summary>
    /// 记录错误级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    public void LogError(string message, string deviceId, bool isWebSocketLog = false)
    {
        if (isWebSocketLog)
        {
            _logger.Error($"[WebSocket][{deviceId}] {message}");
        }
        else
        {
            _logger.Error($"[{deviceId}] {message}");
        }
        AddLogEntry("Error", message, deviceId, isWebSocketLog);
    }
    /// <summary>
    /// 在UI线程上执行操作（WPF环境）或直接执行操作（非WPF环境）
    /// </summary>
    /// <param name="action">要执行的操作</param>
    private void ExecuteOnUIThread(Action action)
    {
#if WPF
        // 在WPF环境中，检查App.Current是否为null
        if (Application.Current != null)
        {
            Application.Current.Dispatcher.InvokeAsync(action);
            return;
        }
#endif
        // 在非WPF环境中或App.Current为null时，直接执行操作
        action();
    }

    /// <summary>
    /// 添加日志条目到集合
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志</param>
    private void AddLogEntry(string level, string message, string deviceId, bool isWebSocketLog = false)
    {
        try
        {
            ExecuteOnUIThread(() =>
            {
                // 创建新日志条目
                var logEntry = new LogEntry(level, message, deviceId);

                // 根据日志类型添加到相应的集合
                if (isWebSocketLog)
                {
                    _webSocketLogEntries.Add(logEntry);

                    // 限制WebSocket日志条目数量
                    //while (_webSocketLogEntries.Count > MaxLogEntries)
                    //{
                    //    _webSocketLogEntries.RemoveAt(0);
                    //}
                    if (_webSocketLogEntries.Count > MaxLogEntries) {

                        _webSocketLogEntries.Clear();
                    }
                    // 触发WebSocket日志文本变更通知
                    OnPropertyChanged(nameof(WebSocketLogsText));
                }
                else
                {
                    _logEntries.Add(logEntry);

                    // 限制设备日志条目数量
                    //while (_logEntries.Count > MaxLogEntries)
                    //{
                    //    _logEntries.RemoveAt(0);
                    //}
                    if (_logEntries.Count > MaxLogEntries)
                    {

                        _logEntries.Clear();
                    }

                    // 触发设备日志文本变更通知
                    OnPropertyChanged(nameof(LogsText));
                }
            });
        }
        catch (Exception ex)
        {
            // 记录异常到文件，避免UI线程异常导致程序崩溃
            _logger.Error($"添加日志条目时发生异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理设备日志记录
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    /// </summary>
    public void ClearLogs(string deviceId = "", bool isWebSocketLog = false)
    {
        ExecuteOnUIThread(() =>
        {
            if (isWebSocketLog)
            {
                _webSocketLogEntries.Clear();
                OnPropertyChanged(nameof(WebSocketLogsText));
            }
            else
            {
                _logEntries.Clear();
                OnPropertyChanged(nameof(LogsText));
            }
        });
        _logger.Info("设备日志已清理");
    }

    /// <summary>
    /// 获取指定设备的日志
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    /// <returns>设备日志集合</returns>
    public ObservableCollection<LogEntry> GetLogs(string deviceId="", bool isWebSocketLog = false)
    {
        var deviceLogs = new ObservableCollection<LogEntry>();

        // 获取日志不需要在UI线程上执行，因为我们只是读取数据
        if (isWebSocketLog)
        {
            deviceLogs.AddRange(_webSocketLogEntries.Where(log => string.IsNullOrEmpty(deviceId) || log.DeviceId == deviceId).ToList());
        }
        else
        {
            deviceLogs.AddRange(_logEntries.Where(log => string.IsNullOrEmpty(deviceId) || log.DeviceId == deviceId).ToList());
        }

        return deviceLogs;
    }

}