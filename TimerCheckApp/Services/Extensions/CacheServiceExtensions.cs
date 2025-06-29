using System;
using System.Threading.Tasks;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Services.Extensions
{
    /// <summary>
    /// 缓存服务扩展方法
    /// </summary>
    public static class CacheServiceExtensions
    {
        /// <summary>
        /// 设置仅内存缓存项（不持久化到磁盘）
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="cacheService">缓存服务</param>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expirationTime">过期时间（可选），默认为30分钟</param>
        /// <returns>是否成功设置</returns>
        public static bool SetMemoryOnly<T>(this ICacheService cacheService, string key, T value, TimeSpan? expirationTime = null)
        {
            if (cacheService is TieredCacheService tieredCache)
            {
                tieredCache.SetMemoryOnly(key, value, expirationTime);
                return true;
            }
            else
            {
                // 对于非TieredCacheService实现，使用普通的Set方法，但使用较短的过期时间
                var expiration = expirationTime ?? TimeSpan.FromMinutes(30);
                cacheService.Set(key, value, expiration);
                return false; // 返回false表示未使用专用的内存缓存
            }
        }

        /// <summary>
        /// 异步设置仅内存缓存项（不持久化到磁盘）
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="cacheService">缓存服务</param>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expirationTime">过期时间（可选），默认为30分钟</param>
        /// <returns>是否成功设置的任务</returns>
        public static async Task<bool> SetMemoryOnlyAsync<T>(this ICacheService cacheService, string key, T value, TimeSpan? expirationTime = null)
        {
            if (cacheService is TieredCacheService tieredCache)
            {
                await tieredCache.SetMemoryOnlyAsync(key, value, expirationTime);
                return true;
            }
            else
            {
                // 对于非TieredCacheService实现，使用普通的SetAsync方法，但使用较短的过期时间
                var expiration = expirationTime ?? TimeSpan.FromMinutes(30);
                await cacheService.SetAsync(key, value, expiration);
                return false; // 返回false表示未使用专用的内存缓存
            }
        }

        /// <summary>
        /// 检查缓存服务是否支持分级缓存
        /// </summary>
        /// <param name="cacheService">缓存服务</param>
        /// <returns>是否支持分级缓存</returns>
        public static bool SupportsTieredCache(this ICacheService cacheService)
        {
            return cacheService is TieredCacheService;
        }
    }
}