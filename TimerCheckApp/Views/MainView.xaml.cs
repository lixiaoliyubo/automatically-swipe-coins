using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using TimerCheckApp.Services.Interfaces;
using TimerCheckApp.ViewModels;

namespace TimerCheckApp.Views
{
    /// <summary>
    /// MainView.xaml 的交互逻辑
    /// </summary>
    public partial class MainView : UserControl
    {
        private readonly ILogService? _logService;
        private readonly MainViewModel? _mainViewModel;
        
        // 日志消息队列
        private readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();
        
        // 处理线程取消标记
        private CancellationTokenSource? _cancellationTokenSource;
        
        // 处理任务
        private Task? _processingTask;
        
        // 线程同步锁
        private readonly object _updateLock = new object();
        
        // 批处理大小
        private const int BatchSize = 10;
        
        // 处理间隔（毫秒）
        private const int ProcessingInterval = 100;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public MainView()
        {
            InitializeComponent();
            // 获取日志服务实例
            _logService = App.Current.Services.GetService<ILogService>();

            _mainViewModel = App.Current.Services?.GetService<MainViewModel>();

            // 订阅日志条目添加事件
            if (_logService != null)
            {
                _logService.LogPropertyChanged += LogService_PropertyChanged;
            }
            
            // 启动处理线程
            StartProcessingThread();

            // 在控件卸载时取消订阅事件和停止处理线程
            Unloaded += (s, e) => {
                if (_logService != null)
                {
                    _logService.LogPropertyChanged -= LogService_PropertyChanged;
                }
                
                // 停止处理线程
                StopProcessingThread();
            };
        }
        
        /// <summary>
        /// 启动日志处理任务
        /// </summary>
        private void StartProcessingThread()
        {
            // 如果已有任务在运行，先停止它
            StopProcessingThread();
            
            // 创建新的取消标记
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            
            // 创建并启动处理任务
            _processingTask = Task.Run(() => ProcessLogQueueAsync(token));
        }
        
        /// <summary>
        /// 停止日志处理任务
        /// </summary>
        private void StopProcessingThread()
        {
            // 发送取消信号
            _cancellationTokenSource?.Cancel();
            
            try
            {
                // 等待任务结束（最多等待1秒）
                if (_processingTask != null && !_processingTask.IsCompleted)
                {
                    _processingTask.Wait(1000);
                }
            }
            catch (AggregateException ex)
            {
                // 任务取消异常可以忽略
                if (!(ex.InnerException is TaskCanceledException))
                {
                    System.Diagnostics.Debug.WriteLine($"停止日志处理任务异常: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止日志处理任务异常: {ex.Message}");
            }
            finally
            {
                // 清理资源
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _processingTask = null;
            }
        }
        
        /// <summary>
        /// 处理日志队列的异步方法（在后台任务中运行）
        /// </summary>
        private async Task ProcessLogQueueAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 检查队列中是否有日志需要处理
                    if (_logQueue.Count > 0)
                    {
                        // 批量处理日志
                        ProcessLogBatch();
                    }
                    
                    // 等待一段时间再继续处理
                    await Task.Delay(ProcessingInterval, token);
                }
                catch (OperationCanceledException)
                {
                    // 正常取消，不需处理
                    break;
                }
                catch (Exception ex)
                {
                    // 记录异常但不中断任务
                    System.Diagnostics.Debug.WriteLine($"日志处理任务异常: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 批量处理日志队列中的条目
        /// </summary>
        private void ProcessLogBatch()
        {
            // 一次最多处理的日志数量
            const int batchSize = 50;
            int processedCount = 0;
            LogEntry? lastEntry = null;
            bool isWebSocketLog = false;
            
            // 从队列中取出日志并处理
            while (processedCount < batchSize && _logQueue.TryDequeue(out var entry))
            {
                lastEntry = entry;
                isWebSocketLog = entry.IsWebSocketLog;
                processedCount++;
            }
            
            // 只更新最后一个日志条目的显示，以提高性能
            if (lastEntry != null)
            {
                // 使用异步方法更新UI，但不等待其完成
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    lock (_updateLock)
                    {
                        // 为了向后兼容，仍然调用同步方法
                        UpdateLogDisplay(lastEntry);
                        
                        // 也可以使用异步方法（未来可以完全替换为异步方法）
                        // await UpdateLogDisplayAsync(lastEntry);
                    }
                });
            }
        }

        /// <summary>
        /// 日志服务属性变更事件处理方法
        /// </summary>
        private void LogService_PropertyChanged(object? sender, LogEntry logEntry)
        {
            // 将日志条目添加到队列中，而不是直接更新UI
            // 这样可以避免频繁的UI更新，提高性能
            if (logEntry != null)
            {
                _logQueue.Enqueue(logEntry);
            }
            else
            {
                // 如果日志条目为空，可能是清理操作，直接更新UI
                // 使用异步方法更新UI，但不等待其完成
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    // 为了向后兼容，仍然调用同步方法
                    UpdateLogDisplay(new LogEntry());
                    
                    // 也可以使用异步方法（未来可以完全替换为异步方法）
                    // await UpdateLogDisplayAsync(new LogEntry() { IsWebSocketLog = false });
                    // await UpdateLogDisplayAsync(new LogEntry() { IsWebSocketLog = true });
                });
            }
        }
        
        /// <summary>
        /// 更新日志显示
        /// </summary>
        private void UpdateLogDisplay(LogEntry logEntry)
        {
            // 确保在UI线程上执行
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.InvokeAsync(() => UpdateLogDisplay(logEntry));
                return;
            }
            
            try
            {
                // 根据日志类型和当前设置决定更新哪个日志显示
                if (!logEntry.IsWebSocketLog)
                {
                    // 更新普通日志
                    if (_mainViewModel?.ShowAllLogs == true)
                    {
                        LogsTextBox.Text = _logService?.LogsText ?? string.Empty;
                    }
                    else if (_mainViewModel?.SelectedDevice != null)
                    {
                        var logs = _logService?.GetLogs(_mainViewModel.SelectedDevice.Id) ?? new ObservableCollection<LogEntry>();
                        LogsTextBox.Text =  _logService?.GenerateLogText(logs) ?? string.Empty;
                    }
                }
                else
                {
                    // 更新WebSocket日志文本框（如果存在）
                    if (WebSocketLogsTextBox != null)
                    {
                        WebSocketLogsTextBox.Text = _logService?.WebSocketLogsText ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录异常但不中断UI更新
                System.Diagnostics.Debug.WriteLine($"更新日志显示异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步更新日志显示
        /// </summary>
        private async Task UpdateLogDisplayAsync(LogEntry logEntry)
        {
            // 确保在UI线程上执行
            if (!Dispatcher.CheckAccess())
            {
                await Dispatcher.InvokeAsync(() => UpdateLogDisplay(logEntry));
                return;
            }
            
            try
            {
                // 根据日志类型和当前设置决定更新哪个日志显示
                if (!logEntry.IsWebSocketLog)
                {
                    // 更新普通日志
                    if (_mainViewModel?.ShowAllLogs == true)
                    {
                        LogsTextBox.Text = _logService?.LogsText ?? string.Empty;
                    }
                    else if (_mainViewModel?.SelectedDevice != null)
                    {
                        var logs = _logService?.GetLogs(_mainViewModel.SelectedDevice.Id) ?? new ObservableCollection<LogEntry>();
                        LogsTextBox.Text = _logService?.GenerateLogText(logs) ?? string.Empty;
                    }
                }
                else
                {
                    // 更新WebSocket日志文本框（如果存在）
                    if (WebSocketLogsTextBox != null)
                    {
                        WebSocketLogsTextBox.Text = _logService?.WebSocketLogsText ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录异常但不中断UI更新
                System.Diagnostics.Debug.WriteLine($"异步更新日志显示异常: {ex.Message}");
            }
        }
    }
}