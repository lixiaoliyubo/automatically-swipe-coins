using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TimerCheckApp.Services;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Tests
{
    /// <summary>
    /// 测试运行器类
    /// </summary>
    public class TestRunner
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TestRunner()
        {
            // 创建服务集合
            var services = new ServiceCollection();

            // 注册服务
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<CacheServiceTests>();

            // 构建服务提供者
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// 运行缓存服务测试
        /// </summary>
        public async Task RunCacheServiceTests()
        {
            var cacheTests = _serviceProvider.GetRequiredService<CacheServiceTests>();
            await cacheTests.RunAllTests();
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public async Task RunAllTests()
        {
            Console.WriteLine("开始运行所有测试...");
            
            // 运行缓存服务测试
            await RunCacheServiceTests();
            
            Console.WriteLine("所有测试完成。");
        }
    }
}