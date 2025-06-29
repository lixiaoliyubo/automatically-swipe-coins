using AdvancedSharpAdbClient;
using Microsoft.Extensions.DependencyInjection;
using System;
using TimerCheckApp.AdWatching;
using TimerCheckApp.Services.Interfaces;
using TimerCheckApp.ViewModels;

namespace TimerCheckApp.Services.DependencyInjection
{
    /// <summary>
    /// 依赖注入服务扩展类
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册应用服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            // 注册日志服务（单例模式）
            services.AddSingleton<ILogService, LogService>();

            // 注册ADB服务（单例模式）
            services.AddSingleton<AdbServer>(provider => 
            {
                var adbServer = new AdbServer();
                //var serverStarted = adbServer.StartServer("adb.exe", restartServerIfNewer: false);
                var logService = provider.GetRequiredService<ILogService>();
               // logService.LogInfo($"ADB服务器启动状态: {serverStarted}", "System");
                return adbServer;
            });
            services.AddSingleton<AdbClient>();

            // 注册设备服务（单例模式）
            services.AddSingleton<IDeviceService, DeviceService>();
            
            // 注册广告观看工厂（单例模式）
            services.AddSingleton<IAdWatchingFactory, AdWatchingFactory>();

            // 注册WebSocket服务（单例模式）
            services.AddSingleton<IWebSocketService>(provider => 
            {
                var logService = provider.GetRequiredService<ILogService>();
                return new WebSocketService(logService, "ws://127.0.0.1:33332");
            });

            // 注册视图模型（单例模式）
            services.AddSingleton<MainViewModel>();

            // 注册缓存服务（单例模式）- 使用内存缓存
            services.AddSingleton<ICacheService>(provider => 
            {
                var logService = provider.GetRequiredService<ILogService>();
                return new MemoryCacheService(logService);
            });
            
            // 注册SQLSugar SQLite服务（单例模式）
            services.AddSingleton<ISqlSugarService>(provider => 
            {
                var logService = provider.GetRequiredService<ILogService>();
                return new SqlSugarService(logService);
            });
            
            // 注册音频服务（单例模式）
            services.AddSingleton<IAudioService>(provider => 
            {
                var logService = provider.GetRequiredService<ILogService>();
                return new AudioService(logService);
            });

            return services;
        }
    }
}