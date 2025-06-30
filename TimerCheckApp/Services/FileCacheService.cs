using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Services
{
    /// <summary>
    /// 基于文件的缓存服务实现
    /// </summary>
    public class FileCacheService : ICacheService
    {
        private readonly string _cacheDirectory;
        private readonly ConcurrentDictionary<string, CacheItem> _memoryCache;
        private readonly TimeSpan _defaultExpirationTime;
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogService _logService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="cacheDirectory">缓存目录名称（相对于应用程序目录）</param>
        /// <param name="defaultExpirationHours">默认过期时间（小时）</param>
        public FileCacheService(ILogService logService, string cacheDirectory = "Cache", int defaultExpirationHours = 24)
        {
            _logService = logService;
            _cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cacheDirectory);
            _memoryCache = new ConcurrentDictionary<string, CacheItem>();
            _defaultExpirationTime = TimeSpan.FromHours(defaultExpirationHours);
            _semaphore = new SemaphoreSlim(1, 1);

            // 确保缓存目录存在
            EnsureCacheDirectoryExists();
            
            // 加载现有缓存
            LoadCacheFromDisk();
        }

        /// <summary>
        /// 确保缓存目录存在
        /// </summary>
        private void EnsureCacheDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                    _logService.LogInfo($"创建缓存目录: {_cacheDirectory}", "System");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"创建缓存目录失败: {ex.Message}", "System");
            }
        }

        /// <summary>
        /// 从磁盘加载缓存
        /// </summary>
        private void LoadCacheFromDisk()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                    return;

                foreach (var file in Directory.GetFiles(_cacheDirectory, "*.cache"))
                {
                    try
                    {
                        var key = Path.GetFileNameWithoutExtension(file);
                        var json = File.ReadAllText(file);
                        var cacheItem = JsonSerializer.Deserialize<CacheItem>(json);

                        // 检查是否过期
                        if (cacheItem != null && !IsCacheItemExpired(cacheItem))
                        {
                            _memoryCache[key] = cacheItem;
                            _logService.LogInfo($"从磁盘加载缓存: {key}", "System");
                        }
                        else
                        {
                            // 删除过期的缓存文件
                            File.Delete(file);
                            _logService.LogInfo($"删除过期缓存: {key}", "System");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"加载缓存文件失败: {Path.GetFileName(file)}, {ex.Message}", "System");
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"从磁盘加载缓存失败: {ex.Message}", "System");
            }
        }

        /// <summary>
        /// 检查缓存项是否过期
        /// </summary>
        /// <param name="cacheItem">缓存项</param>
        /// <returns>是否过期</returns>
        private bool IsCacheItemExpired(CacheItem cacheItem)
        {
            return cacheItem.ExpirationTime < DateTime.UtcNow;
        }

        /// <summary>
        /// 获取缓存文件路径
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>文件路径</returns>
        private string GetCacheFilePath(string key)
        {
            return Path.Combine(_cacheDirectory, $"{key}.cache");
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
                return default;

            if (_memoryCache.TryGetValue(key, out var cacheItem) && !IsCacheItemExpired(cacheItem))
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(cacheItem.Value);
                }
                catch (Exception ex)
                {
                    _logService.LogError($"反序列化缓存项失败: {key}, {ex.Message}", "System");
                    return default;
                }
            }

            // 如果内存中没有或已过期，尝试从磁盘加载
            var filePath = GetCacheFilePath(key);
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var loadedItem = JsonSerializer.Deserialize<CacheItem>(json);

                    if (loadedItem != null && !IsCacheItemExpired(loadedItem))
                    {
                        _memoryCache[key] = loadedItem;
                        return JsonSerializer.Deserialize<T>(loadedItem.Value);
                    }
                    else
                    {
                        // 删除过期的缓存文件
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"从磁盘加载缓存项失败: {key}, {ex.Message}", "System");
                }
            }

            return default;
        }

        /// <summary>
        /// 异步获取缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存项，如果不存在则返回默认值</returns>
        public async Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default;

            if (_memoryCache.TryGetValue(key, out var cacheItem) && !IsCacheItemExpired(cacheItem))
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(cacheItem.Value);
                }
                catch (Exception ex)
                {
                    _logService.LogError($"反序列化缓存项失败: {key}, {ex.Message}", "System");
                    return default;
                }
            }

            // 如果内存中没有或已过期，尝试从磁盘加载
            var filePath = GetCacheFilePath(key);
            if (File.Exists(filePath))
            {
                try
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        var json = await File.ReadAllTextAsync(filePath);
                        var loadedItem = JsonSerializer.Deserialize<CacheItem>(json);

                        if (loadedItem != null && !IsCacheItemExpired(loadedItem))
                        {
                            _memoryCache[key] = loadedItem;
                            return JsonSerializer.Deserialize<T>(loadedItem.Value);
                        }
                        else
                        {
                            // 删除过期的缓存文件
                            File.Delete(filePath);
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"从磁盘异步加载缓存项失败: {key}, {ex.Message}", "System");
                }
            }

            return default;
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
            if (string.IsNullOrEmpty(key) || value == null)
                return;

            var expiration = expirationTime ?? _defaultExpirationTime;
            var serializedValue = JsonSerializer.Serialize(value);
            var cacheItem = new CacheItem
            {
                Value = serializedValue,
                ExpirationTime = DateTime.UtcNow.Add(expiration)
            };

            // 更新内存缓存
            _memoryCache[key] = cacheItem;

            // 保存到磁盘
            try
            {
                var filePath = GetCacheFilePath(key);
                var json = JsonSerializer.Serialize(cacheItem);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                _logService.LogError($"保存缓存项到磁盘失败: {key}, {ex.Message}", "System");
            }
        }

        /// <summary>
        /// 异步设置缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expirationTime">过期时间（可选），默认为24小时</param>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            if (string.IsNullOrEmpty(key) || value == null)
                return;

            var expiration = expirationTime ?? _defaultExpirationTime;
            var serializedValue = JsonSerializer.Serialize(value);
            var cacheItem = new CacheItem
            {
                Value = serializedValue,
                ExpirationTime = DateTime.UtcNow.Add(expiration)
            };

            // 更新内存缓存
            _memoryCache[key] = cacheItem;

            // 保存到磁盘
            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    var filePath = GetCacheFilePath(key);
                    var json = JsonSerializer.Serialize(cacheItem);
                    await File.WriteAllTextAsync(filePath, json);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"异步保存缓存项到磁盘失败: {key}, {ex.Message}", "System");
            }
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否成功移除</returns>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            // 从内存中移除
            _memoryCache.TryRemove(key, out _);

            // 从磁盘移除
            try
            {
                var filePath = GetCacheFilePath(key);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"从磁盘移除缓存项失败: {key}, {ex.Message}", "System");
            }

            return false;
        }

        /// <summary>
        /// 异步移除缓存项
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否成功移除</returns>
        public async Task<bool> RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            // 从内存中移除
            _memoryCache.TryRemove(key, out _);

            // 从磁盘移除
            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    var filePath = GetCacheFilePath(key);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        return true;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"异步从磁盘移除缓存项失败: {key}, {ex.Message}", "System");
            }

            return false;
        }

        /// <summary>
        /// 检查缓存项是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否存在</returns>
        public bool Exists(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            // 检查内存缓存
            if (_memoryCache.TryGetValue(key, out var cacheItem) && !IsCacheItemExpired(cacheItem))
                return true;

            // 检查磁盘缓存
            var filePath = GetCacheFilePath(key);
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var loadedItem = JsonSerializer.Deserialize<CacheItem>(json);

                    if (loadedItem != null && !IsCacheItemExpired(loadedItem))
                    {
                        // 更新内存缓存
                        _memoryCache[key] = loadedItem;
                        return true;
                    }
                    else
                    {
                        // 删除过期的缓存文件
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"检查缓存项存在性失败: {key}, {ex.Message}", "System");
                }
            }

            return false;
        }

        /// <summary>
        /// 异步检查缓存项是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否存在</returns>
        public async Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            // 检查内存缓存
            if (_memoryCache.TryGetValue(key, out var cacheItem) && !IsCacheItemExpired(cacheItem))
                return true;

            // 检查磁盘缓存
            var filePath = GetCacheFilePath(key);
            if (File.Exists(filePath))
            {
                try
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        var json = await File.ReadAllTextAsync(filePath);
                        var loadedItem = JsonSerializer.Deserialize<CacheItem>(json);

                        if (loadedItem != null && !IsCacheItemExpired(loadedItem))
                        {
                            // 更新内存缓存
                            _memoryCache[key] = loadedItem;
                            return true;
                        }
                        else
                        {
                            // 删除过期的缓存文件
                            File.Delete(filePath);
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"异步检查缓存项存在性失败: {key}, {ex.Message}", "System");
                }
            }

            return false;
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            // 清空内存缓存
            _memoryCache.Clear();

            // 清空磁盘缓存
            try
            {
                if (Directory.Exists(_cacheDirectory))
                {
                    foreach (var file in Directory.GetFiles(_cacheDirectory, "*.cache"))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"清空磁盘缓存失败: {ex.Message}", "System");
            }
        }

        /// <summary>
        /// 异步清空所有缓存
        /// </summary>
        public async Task ClearAsync()
        {
            // 清空内存缓存
            _memoryCache.Clear();

            // 清空磁盘缓存
            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (Directory.Exists(_cacheDirectory))
                    {
                        foreach (var file in Directory.GetFiles(_cacheDirectory, "*.cache"))
                        {
                            File.Delete(file);
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"异步清空磁盘缓存失败: {ex.Message}", "System");
            }
        }

        /// <summary>
        /// 缓存项类
        /// </summary>
        private class CacheItem
        {
            /// <summary>
            /// 序列化后的值
            /// </summary>
            public string Value { get; set; }

            /// <summary>
            /// 过期时间（UTC）
            /// </summary>
            public DateTime ExpirationTime { get; set; }
        }
    }
}