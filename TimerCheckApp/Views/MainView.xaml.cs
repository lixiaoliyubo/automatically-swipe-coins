using System.ComponentModel;
using System.Speech.Recognition.SrgsGrammar;
using System.Windows.Controls;
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
                _logService.LogEntryAdded += OnLogEntryAdded;
            }

            // 在控件卸载时取消订阅事件
            Unloaded += (s, e) => {
                if (_logService != null)
                {
                    _logService.LogEntryAdded -= OnLogEntryAdded;
                }
            };
        }

        private void OnLogEntryAdded(object? sender, LogEntry logEntry)
        {
            //// 确保在UI线程上执行
            //if (!Dispatcher.CheckAccess())
            //{
            //    Dispatcher.InvokeAsync(() => OnLogEntryAdded(sender, logEntry));
            //    return;
            //}

            //// 根据日志级别设置不同的视觉提示
            //switch (logEntry.Level.ToLower())
            //{
            //    case "error":
            //        // 错误日志可以显示红色提示
            //        ShowLogNotification(logEntry, Colors.Red);
            //        break;
            //    case "warning":
            //        // 警告日志可以显示黄色提示
            //        ShowLogNotification(logEntry, Colors.Orange);
            //        break;
            //    default:
            //        // 普通日志可以显示绿色或蓝色提示
            //        ShowLogNotification(logEntry, Colors.Green);
            //        break;
            //}
        }

        /// <summary>
        /// 日志服务属性变更事件处理
        /// </summary>
        private void LogService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILogService.LogsText))
            {
                if (_mainViewModel.ShowAllLogs)
                {
                    _mainViewModel.LogsText = _logService.LogsText;
                }
                else
                {
                    _mainViewModel.Logs = _logService.GetLogs(_mainViewModel.SelectedDevice.Id);
                    _mainViewModel.LogsText = _logService.GenerateLogText(_mainViewModel.Logs);
                }
            }
            else if (e.PropertyName == nameof(ILogService.WebSocketLogsText))
            {
                _mainViewModel.WebSocketLogsText = _logService.WebSocketLogsText;
            }
        }
    }
}