using System;
using Microsoft.Extensions.DependencyInjection;
using TimerCheckApp.enums;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.AdWatching
{
    /// <summary>
    /// 广告观看工厂实现
    /// </summary>
    public class AdWatchingFactory : IAdWatchingFactory
    {
        private readonly ILogService _logService;
        private readonly ICacheService _cacheService;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="cacheService">缓存服务</param>
        /// <param name="serviceProvider">服务提供者</param>
        public AdWatchingFactory(ILogService logService, ICacheService cacheService, IServiceProvider serviceProvider)
        {
            _logService = logService;
            _cacheService = cacheService;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 创建广告观看实例
        /// </summary>
        /// <param name="platformType">平台类型</param>
        /// <returns>广告观看实例</returns>
        public IAdWatching Create(PlatformType platformType)
        {
            // 在需要时才获取 IDeviceService 实例，避免循环依赖
            var deviceService = _serviceProvider.GetRequiredService<IDeviceService>();
            
            return platformType switch
            {
                PlatformType.Douyin => new DouyinAdWatching(_logService, deviceService, _cacheService),
                PlatformType.Kuaishou => new KuaishouAdWatching(_logService, deviceService, _cacheService),
                PlatformType.KuaishouJs => new KuaishouAdWatching(_logService, deviceService, _cacheService),
                PlatformType.FanQie => new FanQieMusicAdWatching(_logService, deviceService, _cacheService),
                PlatformType.FanQieMusic => new FanQieMusicAdWatching(_logService, deviceService, _cacheService),
                PlatformType.HongGuo => new HongGuoWatching(_logService, deviceService, _cacheService),
                PlatformType.HeMa => new HeMaWatching(_logService, deviceService, _cacheService),
                PlatformType.XiGua => new XiGuaWatching(_logService, deviceService, _cacheService),
                PlatformType.Novel => new FanQieNovelAdWatching(_logService, deviceService, _cacheService),
                PlatformType.AliPay => new AliPayAdWatching(_logService, deviceService, _cacheService),
                PlatformType.Data => new DataAdWatching(_logService, deviceService, _cacheService),
                _ => throw new ArgumentException($"不支持的平台类型：{platformType}", nameof(platformType))
            };
        }
    }
}