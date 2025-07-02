using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TimerCheckApp.Models;
using TimerCheckApp.Services.Interfaces;

#if WPF
using System.Windows;
#endif

namespace TimerCheckApp.Services;

/// <summary>
/// 日志消息类型
/// </summary>
public enum LogMessageType
{
    Info,
    Warning,
    Error
}

/// <summary>
/// 日志消息
/// </summary>
public class LogMessage
{
    public LogMessageType Type { get; set; }
    public string Message { get; set; }
    public string DeviceId { get; set; }
    public bool IsWebSocketLog { get; set; }
    public DateTime Timestamp { get; set; }

    public LogMessage(LogMessageType type, string message, string deviceId, bool isWebSocketLog)
    {
        Type = type;
        Message = message;
        DeviceId = deviceId;
        IsWebSocketLog = isWebSocketLog;
        Timestamp = DateTime.Now;
    }
}

/// <summary>
/// 日志服务实现 - 使用消息队列和发布订阅模式
/// </summary>
public class LogService : ILogService, INotifyPropertyChanged, IDisposable
{
    private readonly Logger _logger;
    private readonly ObservableCollection<LogEntry> _logEntries = new();
    private readonly ObservableCollection<LogEntry> _webSocketLogEntries = new();
    private const int MaxLogEntries = 1000;
    
    // 日志消息队列
    private readonly BlockingCollection<LogMessage> _logQueue = new(new ConcurrentQueue<LogMessage>());
    
    // 日志处理线程
    private readonly Thread _logProcessingThread;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    // 日志更新事件
    public event EventHandler<string> LogsTextUpdated;
    public event EventHandler<string> WebSocketLogsTextUpdated;

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
    public string LogsText { get; private set; } = string.Empty;

    /// <summary>
    /// WebSocket日志文本，用于在TextBox中显示，从WebSocket日志条目生成
    /// </summary>
    public string WebSocketLogsText { get; private set; } = string.Empty;

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
        
        // 初始化日志文本
        LogsText = GenerateLogText(_logEntries);
        WebSocketLogsText = GenerateLogText(_webSocketLogEntries);
        
        // 启动日志处理线程
        _logProcessingThread = new Thread(ProcessLogQueue)
        {
            IsBackground = true,
            Name = "LogProcessingThread"
        };
        _logProcessingThread.Start();
    }
    
    /// <summary>
    /// 处理日志队列的方法，在后台线程中运行
    /// </summary>
    private void ProcessLogQueue()
    {
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // 从队列中取出日志消息（阻塞操作，直到有新消息或被取消）
                if (_logQueue.TryTake(out LogMessage logMessage, 100, _cancellationTokenSource.Token))
                {
                    ProcessLogMessage(logMessage);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 线程被取消，正常退出
        }
        catch (Exception ex)
        {
            // 记录异常，但不中断线程
            _logger.Error($"日志处理线程异常: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 处理单个日志消息
    /// </summary>
    /// <param name="logMessage">日志消息</param>
    private void ProcessLogMessage(LogMessage logMessage)
    {
        try
        {
            // 根据日志类型记录到NLog
            string logPrefix = logMessage.IsWebSocketLog ? $"[WebSocket][{logMessage.DeviceId}]" : $"[{logMessage.DeviceId}]";
            string fullMessage = $"{logPrefix} {logMessage.Message}";
            
            switch (logMessage.Type)
            {
                case LogMessageType.Info:
                    _logger.Info(fullMessage);
                    break;
                case LogMessageType.Warning:
                    _logger.Warn(fullMessage);
                    break;
                case LogMessageType.Error:
                    _logger.Error(fullMessage);
                    break;
            }
            
            // 创建日志条目
            var logEntry = new LogEntry(
                logMessage.Type.ToString(), 
                logMessage.Message, 
                logMessage.DeviceId)
            {
                Timestamp = logMessage.Timestamp
            };
            
            // 在UI线程上更新集合和文本
            UpdateLogCollections(logEntry, logMessage.IsWebSocketLog);
        }
        catch (Exception ex)
        {
            // 记录异常，但不中断处理
            _logger.Error($"处理日志消息异常: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 更新日志集合和文本
    /// </summary>
    /// <param name="logEntry">日志条目</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志</param>
    private void UpdateLogCollections(LogEntry logEntry, bool isWebSocketLog)
    {
        ExecuteOnUIThread(() =>
        {
            // 根据日志类型添加到相应的集合
            if (isWebSocketLog)
            {
                _webSocketLogEntries.Add(logEntry);
                
                // 限制WebSocket日志条目数量
                if (_webSocketLogEntries.Count > MaxLogEntries)
                {
                    // 移除最旧的条目而不是清空集合
                    for (int i = 0; i < _webSocketLogEntries.Count - MaxLogEntries; i++)
                    {
                        _webSocketLogEntries.RemoveAt(0);
                    }
                }
                
                // 更新WebSocket日志文本
                WebSocketLogsText = GenerateLogText(_webSocketLogEntries);
                
                // 触发WebSocket日志文本变更通知
                OnPropertyChanged(nameof(WebSocketLogsText));
                WebSocketLogsTextUpdated?.Invoke(this, WebSocketLogsText);
            }
            else
            {
                _logEntries.Add(logEntry);
                
                // 限制设备日志条目数量
                if (_logEntries.Count > MaxLogEntries)
                {
                    // 移除最旧的条目而不是清空集合
                    for (int i = 0; i < _logEntries.Count - MaxLogEntries; i++)
                    {
                        _logEntries.RemoveAt(0);
                    }
                }
                
                // 更新设备日志文本
                LogsText = GenerateLogText(_logEntries);
                
                // 触发设备日志文本变更通知
                OnPropertyChanged(nameof(LogsText));
                LogsTextUpdated?.Invoke(this, LogsText);
            }
        });
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
        // 将日志消息添加到队列中，而不是直接处理
        _logQueue.Add(new LogMessage(LogMessageType.Info, message, deviceId, isWebSocketLog));
    }

    /// <summary>
    /// 记录警告级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    public void LogWarning(string message, string deviceId, bool isWebSocketLog = false)
    {
        // 将日志消息添加到队列中，而不是直接处理
        _logQueue.Add(new LogMessage(LogMessageType.Warning, message, deviceId, isWebSocketLog));
    }

    /// <summary>
    /// 记录错误级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isWebSocketLog">是否为WebSocket日志，默认为false（设备日志）</param>
    public void LogError(string message, string deviceId, bool isWebSocketLog = false)
    {
        // 将日志消息添加到队列中，而不是直接处理
        _logQueue.Add(new LogMessage(LogMessageType.Error, message, deviceId, isWebSocketLog));
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

    // AddLogEntry方法已被移除，使用消息队列和ProcessLogMessage方法代替

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
                WebSocketLogsText = string.Empty;
                OnPropertyChanged(nameof(WebSocketLogsText));
                WebSocketLogsTextUpdated?.Invoke(this, WebSocketLogsText);
            }
            else
            {
                _logEntries.Clear();
                LogsText = string.Empty;
                OnPropertyChanged(nameof(LogsText));
                LogsTextUpdated?.Invoke(this, LogsText);
            }
        });
        
        // 记录清理日志
        LogInfo("日志已清理", deviceId, isWebSocketLog);
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        try
        {
            // 取消日志处理线程
            _cancellationTokenSource.Cancel();
            
            // 等待线程结束，但最多等待1秒
            if (_logProcessingThread.IsAlive)
            {
                _logProcessingThread.Join(1000);
            }
            
            // 清空日志队列
            _logQueue.CompleteAdding();
            _logQueue.Dispose();
            _cancellationTokenSource.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Error($"释放日志服务资源时发生异常: {ex.Message}");
        }
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