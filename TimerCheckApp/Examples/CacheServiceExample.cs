using System;
using System.Threading.Tasks;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Examples
{
    /// <summary>
    /// 缓存服务使用示例
    /// </summary>
    public class CacheServiceExample
    {
        private readonly ICacheService _cacheService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cacheService">缓存服务</param>
        public CacheServiceExample(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// 同步缓存示例
        /// </summary>
        public void SyncCacheExample()
        {
            // 存储数据到缓存（默认24小时过期）
            _cacheService.Set("userSettings", new UserSettings { Username = "admin", Theme = "dark" });

            // 存储数据到缓存（自定义过期时间）
            _cacheService.Set("temporaryData", "临时数据", TimeSpan.FromMinutes(30));

            // 检查缓存是否存在
            if (_cacheService.Exists("userSettings"))
            {
                // 从缓存获取数据
                var settings = _cacheService.Get<UserSettings>("userSettings");
                Console.WriteLine($"用户名: {settings.Username}, 主题: {settings.Theme}");
            }

            // 移除缓存项
            _cacheService.Remove("temporaryData");

            // 清空所有缓存
            // _cacheService.Clear();
        }

        /// <summary>
        /// 异步缓存示例
        /// </summary>
        public async Task AsyncCacheExampleAsync()
        {
            // 异步存储数据到缓存（默认24小时过期）
            await _cacheService.SetAsync("asyncUserSettings", new UserSettings { Username = "admin", Theme = "light" });

            // 异步存储数据到缓存（自定义过期时间）
            await _cacheService.SetAsync("asyncTemporaryData", "临时数据", TimeSpan.FromHours(1));

            // 异步检查缓存是否存在
            if (await _cacheService.ExistsAsync("asyncUserSettings"))
            {
                // 异步从缓存获取数据
                var settings = await _cacheService.GetAsync<UserSettings>("asyncUserSettings");
                Console.WriteLine($"用户名: {settings.Username}, 主题: {settings.Theme}");
            }

            // 异步移除缓存项
            await _cacheService.RemoveAsync("asyncTemporaryData");

            // 异步清空所有缓存
            // await _cacheService.ClearAsync();
        }

        /// <summary>
        /// 用户设置类
        /// </summary>
        private class UserSettings
        {
            /// <summary>
            /// 用户名
            /// </summary>
            public string Username { get; set; }

            /// <summary>
            /// 主题
            /// </summary>
            public string Theme { get; set; }
        }
    }
}