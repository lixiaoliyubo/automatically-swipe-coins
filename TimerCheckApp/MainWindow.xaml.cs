using System;
using System.Windows;
using TimerCheckApp.Services.Interfaces;
using TimerCheckApp.ViewModels;

namespace TimerCheckApp
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        private readonly IDeviceService _deviceService;
        private readonly ILogService _logService;
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow(IDeviceService deviceService, ILogService logService, MainViewModel viewModel)
        {
            // 获取服务
            _deviceService = deviceService;
            _logService = logService;
            _viewModel = viewModel;
            
            InitializeComponent();
            
            // 设置数据上下文
            DataContext = _viewModel;
            
            // 窗口关闭时释放资源
            Closed += MainWindow_Closed;
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            // 资源释放由依赖注入容器管理
        }

        private void Flyout_Closed(Flyout sender, RoutedEventArgs args)
        {
            _viewModel.IsFlyoutOpen = false;
        }
    }
}