using Microsoft.Extensions.Caching.Memory;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Services
{
    /// <summary>
    /// 基于内存的缓存服务实现
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly ILogService _logService;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _defaultExpirationTime;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="memoryCache">内存缓存</param>
        /// <param name="defaultExpirationHours">默认过期时间（小时）</param>
        public MemoryCacheService(ILogService logService, IMemoryCache memoryCache = null, int defaultExpirationHours = 24)
        {
            _logService = logService;
            _memoryCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
            _defaultExpirationTime = TimeSpan.FromHours(defaultExpirationHours);

            _logService.LogInfo("内存缓存服务已初始化", "MemoryCacheService");
        }

        /// <summary>
        /// 获取缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存项，如果不存在则返回默认值</returns>
        public T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                _logService.LogWarning("尝试获取空键的缓存项", "MemoryCacheService");
                return default;
            }

            if (_memoryCache.TryGetValue(key, out T value))
            {
                return value;
            }

            return default;
        }

        /// <summary>
        /// 异步获取缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存项，如果不存在则返回默认值</returns>
        public Task<T> GetAsync<T>(string key)
        {
            return Task.FromResult(Get<T>(key));
        }

        /// <summary>
        /// 设置缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expirationTime">过期时间（可选），默认为24小时</param>
        public void Set<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                _logService.LogWarning("尝试设置空键的缓存项", "MemoryCacheService");
                return;
            }

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime ?? _defaultExpirationTime
            };

            _memoryCache.Set(key, value, cacheEntryOptions);
        }

        /// <summary>
        /// 异步设置缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expirationTime">过期时间（可选），默认为24小时</param>
        public Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            Set(key, value, expirationTime);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否成功移除</returns>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                _logService.LogWarning("尝试移除空键的缓存项", "MemoryCacheService");
                return false;
            }

            _memoryCache.Remove(key);
            return true;
        }

        /// <summary>
        /// 异步移除缓存项
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否成功移除</returns>
        public Task<bool> RemoveAsync(string key)
        {
            return Task.FromResult(Remove(key));
        }

        /// <summary>
        /// 检查缓存项是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否存在</returns>
        public bool Exists(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                _logService.LogWarning("尝试检查空键的缓存项是否存在", "MemoryCacheService");
                return false;
            }

            return _memoryCache.TryGetValue(key, out _);
        }

        /// <summary>
        /// 异步检查缓存项是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否存在</returns>
        public Task<bool> ExistsAsync(string key)
        {
            return Task.FromResult(Exists(key));
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            if (_memoryCache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
            }
            _logService.LogInfo("已清空所有内存缓存", "MemoryCacheService");
        }

        /// <summary>
        /// 异步清空所有缓存
        /// </summary>
        public Task ClearAsync()
        {
            Clear();
            return Task.CompletedTask;
        }
    }
}