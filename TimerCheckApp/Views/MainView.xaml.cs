using System;
using System.Windows.Controls;
using System.Windows.Threading;
using TimerCheckApp.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace TimerCheckApp.Views
{
    /// <summary>
    /// MainView.xaml 的交互逻辑
    /// </summary>
    public partial class MainView : UserControl
    {
        private readonly ILogService _logService;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public MainView()
        {
            InitializeComponent();
            
            // 获取日志服务实例
            _logService = App.Current.Services.GetService<ILogService>();
            
            // 订阅日志更新事件
            if (_logService != null)
            {
                // 使用反射获取LogsTextUpdated和WebSocketLogsTextUpdated事件
                var logsTextUpdatedEvent = _logService.GetType().GetEvent("LogsTextUpdated");
                var webSocketLogsTextUpdatedEvent = _logService.GetType().GetEvent("WebSocketLogsTextUpdated");
                
                if (logsTextUpdatedEvent != null)
                {
                    // 创建并添加事件处理程序
                    var handler = Delegate.CreateDelegate(
                        logsTextUpdatedEvent.EventHandlerType,
                        this,
                        GetType().GetMethod(nameof(OnLogsTextUpdated), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
                    
                    logsTextUpdatedEvent.AddEventHandler(_logService, handler);
                }
                
                if (webSocketLogsTextUpdatedEvent != null)
                {
                    // 创建并添加事件处理程序
                    var handler = Delegate.CreateDelegate(
                        webSocketLogsTextUpdatedEvent.EventHandlerType,
                        this,
                        GetType().GetMethod(nameof(OnWebSocketLogsTextUpdated), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
                    
                    webSocketLogsTextUpdatedEvent.AddEventHandler(_logService, handler);
                }
            }
        }
        
        /// <summary>
        /// 处理日志文本更新事件
        /// </summary>
        private void OnLogsTextUpdated(object sender, string logsText)
        {
            // 在UI线程上更新日志文本框
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (LogsTextBox != null)
                {
                    LogsTextBox.Text = logsText;
                    LogsTextBox.ScrollToEnd();
                }
            }));
        }
        
        /// <summary>
        /// 处理WebSocket日志文本更新事件
        /// </summary>
        private void OnWebSocketLogsTextUpdated(object sender, string logsText)
        {
            // 在UI线程上更新WebSocket日志文本框
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (WebSocketLogsTextBox != null)
                {
                    WebSocketLogsTextBox.Text = logsText;
                    WebSocketLogsTextBox.ScrollToEnd();
                }
            }));
        }
    }
}