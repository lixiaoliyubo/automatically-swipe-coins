using System;
using System.IO;
using System.Threading.Tasks;
using TimerCheckApp.Services;
using TimerCheckApp.Services.Extensions;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Tests
{
    /// <summary>
    /// 缓存服务测试类
    /// </summary>
    public class CacheServiceTests
    {
        private readonly ILogService _logService;
        private readonly string _testCacheDir;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        public CacheServiceTests(ILogService logService)
        {
            _logService = logService;
            _testCacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestCache");
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public async Task RunAllTests()
        {
            _logService.LogInfo("开始缓存服务测试", "Test");

            try
            {
                // 清理测试目录
                CleanupTestDirectory();

                // 测试文件缓存服务
                await TestFileCacheService();

                // 测试分级缓存服务
                await TestTieredCacheService();

                // 测试扩展方法
                await TestExtensionMethods();

                _logService.LogInfo("缓存服务测试完成", "Test");
            }
            catch (Exception ex)
            {
                _logService.LogError($"缓存服务测试失败: {ex.Message}", "Test");
            }
            finally
            {
                // 清理测试目录
                CleanupTestDirectory();
            }
        }

        /// <summary>
        /// 清理测试目录
        /// </summary>
        private void CleanupTestDirectory()
        {
            try
            {
                if (Directory.Exists(_testCacheDir))
                {
                    Directory.Delete(_testCacheDir, true);
                    _logService.LogInfo($"已删除测试缓存目录: {_testCacheDir}", "Test");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"清理测试目录失败: {ex.Message}", "Test");
            }
        }

        /// <summary>
        /// 测试文件缓存服务
        /// </summary>
        private async Task TestFileCacheService()
        {
            _logService.LogInfo("测试文件缓存服务", "Test");

            // 创建缓存服务实例
            var cacheService = new FileCacheService(_logService, _testCacheDir, 24);

            // 测试基本操作
            await TestBasicOperations(cacheService, "FileCacheService");

            // 测试持久化
            await TestPersistence(cacheService, "FileCacheService");
        }

        /// <summary>
        /// 测试分级缓存服务
        /// </summary>
        private async Task TestTieredCacheService()
        {
            _logService.LogInfo("测试分级缓存服务", "Test");

            // 创建缓存服务实例
            var cacheService = new TieredCacheService(_logService, _testCacheDir, 24, 30);

            // 测试基本操作
            await TestBasicOperations(cacheService, "TieredCacheService");

            // 测试持久化
            await TestPersistence(cacheService, "TieredCacheService");

            // 测试仅内存缓存
            await TestMemoryOnlyCache(cacheService);
        }

        /// <summary>
        /// 测试扩展方法
        /// </summary>
        private async Task TestExtensionMethods()
        {
            _logService.LogInfo("测试缓存服务扩展方法", "Test");

            // 创建文件缓存服务实例
            var fileCacheService = new FileCacheService(_logService, _testCacheDir, 24);

            // 创建分级缓存服务实例
            var tieredCacheService = new TieredCacheService(_logService, _testCacheDir, 24, 30);

            // 测试 SupportsTieredCache 扩展方法
            var fileSupports = fileCacheService.SupportsTieredCache();
            var tieredSupports = tieredCacheService.SupportsTieredCache();

            _logService.LogInfo($"FileCacheService 支持分级缓存: {fileSupports}", "Test");
            _logService.LogInfo($"TieredCacheService 支持分级缓存: {tieredSupports}", "Test");

            // 测试 SetMemoryOnly 扩展方法
            var fileResult = fileCacheService.SetMemoryOnly("extTest1", "文件缓存测试");
            tieredCacheService.SetMemoryOnly("extTest2", "分级缓存测试");

            _logService.LogInfo($"FileCacheService SetMemoryOnly 结果: {fileResult}", "Test");

            // 测试 SetMemoryOnlyAsync 扩展方法
            var fileAsyncResult = await fileCacheService.SetMemoryOnlyAsync("extTestAsync1", "文件缓存异步测试");
           await tieredCacheService.SetMemoryOnlyAsync("extTestAsync2", "分级缓存异步测试");

            _logService.LogInfo($"FileCacheService SetMemoryOnlyAsync 结果: {fileAsyncResult}", "Test");

            // 验证数据是否可以获取
            var fileValue = fileCacheService.Get<string>("extTest1");
            var tieredValue = tieredCacheService.Get<string>("extTest2");
            var fileAsyncValue = await fileCacheService.GetAsync<string>("extTestAsync1");
            var tieredAsyncValue = await tieredCacheService.GetAsync<string>("extTestAsync2");

            _logService.LogInfo($"FileCacheService 获取值: {fileValue}", "Test");
            _logService.LogInfo($"TieredCacheService 获取值: {tieredValue}", "Test");
            _logService.LogInfo($"FileCacheService 异步获取值: {fileAsyncValue}", "Test");
            _logService.LogInfo($"TieredCacheService 异步获取值: {tieredAsyncValue}", "Test");
        }

        /// <summary>
        /// 测试基本操作
        /// </summary>
        /// <param name="cacheService">缓存服务</param>
        /// <param name="serviceName">服务名称</param>
        private async Task TestBasicOperations(ICacheService cacheService, string serviceName)
        {
            // 测试设置和获取
            cacheService.Set("testKey1", "测试值1");
            var value1 = cacheService.Get<string>("testKey1");
            _logService.LogInfo($"{serviceName} 设置和获取测试: {value1 == "测试值1"}", "Test");

            // 测试异步设置和获取
            await cacheService.SetAsync("testKey2", "测试值2");
            var value2 = await cacheService.GetAsync<string>("testKey2");
            _logService.LogInfo($"{serviceName} 异步设置和获取测试: {value2 == "测试值2"}", "Test");

            // 测试存在性检查
            var exists1 = cacheService.Exists("testKey1");
            var exists3 = cacheService.Exists("nonExistentKey");
            _logService.LogInfo($"{serviceName} 存在性检查测试: {exists1 && !exists3}", "Test");

            // 测试异步存在性检查
            var existsAsync1 = await cacheService.ExistsAsync("testKey2");
            var existsAsync3 = await cacheService.ExistsAsync("nonExistentKey");
            _logService.LogInfo($"{serviceName} 异步存在性检查测试: {existsAsync1 && !existsAsync3}", "Test");

            // 测试移除
            cacheService.Remove("testKey1");
            var afterRemove = cacheService.Exists("testKey1");
            _logService.LogInfo($"{serviceName} 移除测试: {!afterRemove}", "Test");

            // 测试异步移除
            await cacheService.RemoveAsync("testKey2");
            var afterAsyncRemove = await cacheService.ExistsAsync("testKey2");
            _logService.LogInfo($"{serviceName} 异步移除测试: {!afterAsyncRemove}", "Test");

            // 测试清空
            cacheService.Set("testKey4", "测试值4");
            cacheService.Clear();
            var afterClear = cacheService.Exists("testKey4");
            _logService.LogInfo($"{serviceName} 清空测试: {!afterClear}", "Test");

            // 测试自定义过期时间
            cacheService.Set("shortLived", "短期缓存", TimeSpan.FromMilliseconds(50));
            await Task.Delay(100); // 等待过期
            var expiredValue = cacheService.Get<string>("shortLived");
            _logService.LogInfo($"{serviceName} 过期测试: {expiredValue == null}", "Test");
        }

        /// <summary>
        /// 测试持久化
        /// </summary>
        /// <param name="cacheService">缓存服务</param>
        /// <param name="serviceName">服务名称</param>
        private async Task TestPersistence(ICacheService cacheService, string serviceName)
        {
            // 设置一些测试数据
            cacheService.Set("persistTest", "持久化测试数据");
            await cacheService.SetAsync("asyncPersistTest", "异步持久化测试数据");

            // 验证数据已写入磁盘
            var filePath1 = Path.Combine(_testCacheDir, "persistTest.cache");
            var filePath2 = Path.Combine(_testCacheDir, "asyncPersistTest.cache");

            var fileExists1 = File.Exists(filePath1);
            var fileExists2 = File.Exists(filePath2);

            _logService.LogInfo($"{serviceName} 文件写入测试: {fileExists1 && fileExists2}", "Test");

            // 创建新的缓存服务实例（模拟应用重启）
            ICacheService newCacheService;
            if (serviceName == "FileCacheService")
            {
                newCacheService = new FileCacheService(_logService, _testCacheDir, 24);
            }
            else
            {
                newCacheService = new TieredCacheService(_logService, _testCacheDir, 24, 30);
            }

            // 验证数据是否能从磁盘加载
            var loadedValue1 = newCacheService.Get<string>("persistTest");
            var loadedValue2 = await newCacheService.GetAsync<string>("asyncPersistTest");

            _logService.LogInfo($"{serviceName} 持久化测试: {loadedValue1 == "持久化测试数据" && loadedValue2 == "异步持久化测试数据"}", "Test");
        }

        /// <summary>
        /// 测试仅内存缓存
        /// </summary>
        /// <param name="cacheService">分级缓存服务</param>
        private async Task TestMemoryOnlyCache(TieredCacheService cacheService)
        {
            // 设置仅内存缓存
            cacheService.SetMemoryOnly("memoryOnly", "仅内存缓存测试数据");
            await cacheService.SetMemoryOnlyAsync("asyncMemoryOnly", "异步仅内存缓存测试数据");

            // 验证数据可以从内存获取
            var value1 = cacheService.Get<string>("memoryOnly");
            var value2 = await cacheService.GetAsync<string>("asyncMemoryOnly");

            _logService.LogInfo($"TieredCacheService 仅内存缓存测试: {value1 == "仅内存缓存测试数据" && value2 == "异步仅内存缓存测试数据"}", "Test");

            // 验证文件未写入磁盘
            var filePath1 = Path.Combine(_testCacheDir, "memoryOnly.cache");
            var filePath2 = Path.Combine(_testCacheDir, "asyncMemoryOnly.cache");

            var fileNotExists1 = !File.Exists(filePath1);
            var fileNotExists2 = !File.Exists(filePath2);

            _logService.LogInfo($"TieredCacheService 仅内存缓存文件不存在测试: {fileNotExists1 && fileNotExists2}", "Test");

            // 创建新的缓存服务实例（模拟应用重启）
            var newCacheService = new TieredCacheService(_logService, _testCacheDir, 24, 30);

            // 验证仅内存缓存数据在重启后不可用
            var afterRestartValue1 = newCacheService.Get<string>("memoryOnly");
            var afterRestartValue2 = await newCacheService.GetAsync<string>("asyncMemoryOnly");

            _logService.LogInfo($"TieredCacheService 仅内存缓存重启后不可用测试: {afterRestartValue1 == null && afterRestartValue2 == null}", "Test");
        }

        /// <summary>
        /// 测试数据类
        /// </summary>
        private class TestData
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}