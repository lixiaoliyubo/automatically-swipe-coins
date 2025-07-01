using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using AdvancedSharpAdbClient;
using Microsoft.Extensions.DependencyInjection;
using TimerCheckApp.Services.DependencyInjection;
using TimerCheckApp.Services.Interfaces;
using TimerCheckApp.ViewModels;
using MessageBox = Yuandl.ThemeUI.Controls.MessageBox;

namespace TimerCheckApp
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {

        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        private IServiceProvider Services { get; }
        public App()
        {
            // 创建必要的目录
            CreateRequiredDirectories();
            Services = ConfigureServices();

        }
        /// <summary>
        /// 应用程序启动事件处理
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            
            // 设置全局异常处理
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;


            // 显示主窗口
            var mainWindow = Services!.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        
        /// <summary>
        /// 创建应用程序所需的目录
        /// </summary>
        private void CreateRequiredDirectories()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                Directory.CreateDirectory(Path.Combine(baseDir, "Logs"));
                Directory.CreateDirectory(Path.Combine(baseDir, "Screenshots"));
            }
            catch (Exception ex)
            {
                var uiMessageBox = new MessageBox
                {
                    Title = "错误",
                    Content = $"创建应用程序目录失败: {ex.Message}",
                };
                _ = uiMessageBox.ShowDialogAsync();
            }
        }
        
        /// <summary>
        /// UI线程未处理异常处理
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var uiMessageBox = new MessageBox
            {
                Title = "未处理的异常",
                Content = $"应用程序发生错误: {e.Exception.Message}", //\n\n{e.Exception.StackTrace}
            };
            _ = uiMessageBox.ShowDialogAsync();
            e.Handled = true;
        }
        
        /// <summary>
        /// 非UI线程未处理异常处理
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            var uiMessageBox = new MessageBox
            {
                Title = "严重错误",
                Content = $"应用程序发生严重错误: {exception?.Message}\n\n{exception?.StackTrace}",
            };
            _ = uiMessageBox.ShowDialogAsync();
        }

        /// <summary>
        /// 配置依赖注入服务
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // 注册应用服务
            services.AddAppServices();

            // 注册主窗口
            services.AddSingleton<MainWindow>();

            // 构建服务提供者
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// 应用程序退出事件处理
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 清理MainViewModel资源
                var mainViewModel = Services?.GetService<MainViewModel>();
                mainViewModel?.Cleanup();
                
                // 清理其他资源
                var logService = Services?.GetService<ILogService>();
                logService?.LogInfo("应用程序正在退出", "System");
            }
            catch (Exception ex)
            {
                // 记录清理过程中的异常，但不阻止应用程序退出
                Console.WriteLine($"清理资源时发生错误: {ex.Message}");
            }
            
            base.OnExit(e);
        }
    }
}
