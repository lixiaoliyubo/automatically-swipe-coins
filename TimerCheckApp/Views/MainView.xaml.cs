using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Views
{
    /// <summary>
    /// MainView.xaml 的交互逻辑
    /// </summary>
    public partial class MainView : UserControl
    {
        private readonly ILogService? _logService;
        /// <summary>
        /// 构造函数
        /// </summary>
        public MainView()
        {
            InitializeComponent();
            // 获取日志服务实例
            _logService = App.Current.Services.GetService<ILogService>();
        }
    }
}