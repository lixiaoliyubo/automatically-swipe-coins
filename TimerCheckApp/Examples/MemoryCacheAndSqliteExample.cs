using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimerCheckApp.Models;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Examples
{
    /// <summary>
    /// 内存缓存和SQLite数据库使用示例
    /// </summary>
    public class MemoryCacheAndSqliteExample
    {
        private readonly ICacheService _cacheService;
        private readonly ISqlSugarService _sqlSugarService;
        private readonly ILogService _logService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cacheService">缓存服务</param>
        /// <param name="sqlSugarService">SQLite数据库服务</param>
        /// <param name="logService">日志服务</param>
        public MemoryCacheAndSqliteExample(ICacheService cacheService, ISqlSugarService sqlSugarService, ILogService logService)
        {
            _cacheService = cacheService;
            _sqlSugarService = sqlSugarService;
            _logService = logService;

            // 确保数据库表已创建
            _sqlSugarService.CreateTable<TaskRecord>();
        }

        /// <summary>
        /// 运行示例
        /// </summary>
        public async Task RunExampleAsync()
        {
            _logService.LogInfo("开始运行内存缓存和SQLite数据库示例", "Example");

            // 内存缓存示例
            await RunMemoryCacheExampleAsync();

            // SQLite数据库示例
            await RunSqliteExampleAsync();

            // 结合使用示例
            await RunCombinedExampleAsync();

            _logService.LogInfo("内存缓存和SQLite数据库示例运行完成", "Example");
        }

        /// <summary>
        /// 运行内存缓存示例
        /// </summary>
        private async Task RunMemoryCacheExampleAsync()
        {
            _logService.LogInfo("===== 内存缓存示例 =====", "Example");

            // 存储简单类型
            _cacheService.Set("simpleKey", "这是一个字符串值");
            var simpleValue = _cacheService.Get<string>("simpleKey");
            _logService.LogInfo($"从缓存获取简单值: {simpleValue}", "Example");

            // 存储复杂对象
            var settings = new { Username = "admin", Theme = "dark", IsAdmin = true };
            _cacheService.Set("settingsKey", settings, TimeSpan.FromMinutes(30));
            var cachedSettings = _cacheService.Get<dynamic>("settingsKey");
            _logService.LogInfo($"从缓存获取设置: Username={cachedSettings.Username}, Theme={cachedSettings.Theme}", "Example");

            // 异步操作
            await _cacheService.SetAsync("asyncKey", "异步存储的值");
            var asyncValue = await _cacheService.GetAsync<string>("asyncKey");
            _logService.LogInfo($"从缓存异步获取值: {asyncValue}", "Example");

            // 检查存在性和移除
            if (_cacheService.Exists("simpleKey"))
            {
                _cacheService.Remove("simpleKey");
                _logService.LogInfo("已移除缓存项 'simpleKey'", "Example");
            }

            _logService.LogInfo("内存缓存示例完成", "Example");
        }

        /// <summary>
        /// 运行SQLite数据库示例
        /// </summary>
        private async Task RunSqliteExampleAsync()
        {
            _logService.LogInfo("===== SQLite数据库示例 =====", "Example");

            // 插入数据
            var taskRecord = new TaskRecord
            {
                TaskName = "示例任务",
                PlatformType = "抖音",
                DeviceId = "device123",
                Status = "完成",
                StartTime = DateTime.Now.AddMinutes(-10),
                EndTime = DateTime.Now,
                Duration = 600,
                Result = "任务执行成功",
                Remark = "这是一个示例任务记录"
            };

            var insertSuccess = await _sqlSugarService.InsertAsync(taskRecord);
            _logService.LogInfo($"插入任务记录 {(insertSuccess ? "成功" : "失败")}, ID: {taskRecord.Id}", "Example");

            // 查询单个记录
            var record = _sqlSugarService.GetSingle<TaskRecord>(t => t.Id == taskRecord.Id);
            if (record != null)
            {
                _logService.LogInfo($"查询到任务记录: {record.TaskName}, 状态: {record.Status}", "Example");
            }

            // 更新记录
            if (record != null)
            {
                record.Status = "已验证";
                record.Remark = "已更新的备注信息";
                var updateSuccess = await _sqlSugarService.UpdateAsync(record);
                _logService.LogInfo($"更新任务记录 {(updateSuccess ? "成功" : "失败")}", "Example");
            }

            // 插入多条记录
            var taskRecords = new List<TaskRecord>();
            for (int i = 0; i < 5; i++)
            {
                taskRecords.Add(new TaskRecord
                {
                    TaskName = $"批量任务 {i+1}",
                    PlatformType = "快手",
                    DeviceId = $"device{i+100}",
                    Status = "进行中",
                    StartTime = DateTime.Now,
                    Duration = 0,
                    Result = "执行中",
                    Remark = $"批量创建的任务 {i+1}"
                });
            }

            var batchInsertSuccess = await _sqlSugarService.InsertRangeAsync(taskRecords);
            _logService.LogInfo($"批量插入任务记录 {(batchInsertSuccess ? "成功" : "失败")}", "Example");

            // 查询列表
            var list = _sqlSugarService.GetList<TaskRecord>(t => t.PlatformType == "快手");
            _logService.LogInfo($"查询到 {list.Count} 条快手平台的任务记录", "Example");

            // 删除记录
            if (list.Count > 0)
            {
                var deleteSuccess = await _sqlSugarService.DeleteAsync(list[0]);
                _logService.LogInfo($"删除任务记录 {(deleteSuccess ? "成功" : "失败")}", "Example");
            }

            _logService.LogInfo("SQLite数据库示例完成", "Example");
        }

        /// <summary>
        /// 运行结合使用示例
        /// </summary>
        private async Task RunCombinedExampleAsync()
        {
            _logService.LogInfo("===== 结合使用示例 =====", "Example");

            // 从数据库获取数据并缓存
            var records = _sqlSugarService.GetList<TaskRecord>();
            if (records.Count > 0)
            {
                // 将第一条记录放入缓存
                var record = records[0];
                var cacheKey = $"TaskRecord:{record.Id}";
                await _cacheService.SetAsync(cacheKey, record, TimeSpan.FromMinutes(5));
                _logService.LogInfo($"已将任务记录 ID: {record.Id} 放入缓存", "Example");

                // 从缓存获取
                var cachedRecord = await _cacheService.GetAsync<TaskRecord>(cacheKey);
                if (cachedRecord != null)
                {
                    _logService.LogInfo($"从缓存获取任务记录: {cachedRecord.TaskName}, 状态: {cachedRecord.Status}", "Example");
                }

                // 模拟更新操作
                _logService.LogInfo("模拟更新操作，先更新数据库，再更新缓存", "Example");
                record.Status = "已完成并验证";
                record.EndTime = DateTime.Now;
                record.Duration = (int)(record.EndTime.Value - record.StartTime).TotalSeconds;

                // 更新数据库
                var updateSuccess = await _sqlSugarService.UpdateAsync(record);
                if (updateSuccess)
                {
                    // 更新缓存
                    await _cacheService.SetAsync(cacheKey, record, TimeSpan.FromMinutes(5));
                    _logService.LogInfo("数据库和缓存都已更新", "Example");
                }
            }

            _logService.LogInfo("结合使用示例完成", "Example");
        }
    }
}