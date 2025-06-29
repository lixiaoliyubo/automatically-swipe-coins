# 内存缓存和SQLite数据库使用文档

## 概述

本项目提供了两种重要的数据存储服务：

1. **`MemoryCacheService`**：基于内存的缓存实现，适用于临时数据的快速存取。
2. **`SqlSugarService`**：基于SQLite的数据库服务，使用SQLSugar ORM框架，适用于需要持久化的结构化数据。

## 内存缓存服务

### 特点

- **纯内存存储**：数据仅保存在内存中，应用重启后数据会丢失
- **高性能**：无磁盘IO操作，读写速度极快
- **自动过期**：支持设置缓存项的过期时间，默认24小时
- **自动清理**：定期清理过期的缓存项
- **线程安全**：使用 `ConcurrentDictionary` 和 `SemaphoreSlim` 确保多线程环境下的安全访问
- **同步和异步API**：提供同步和异步方法，适应不同的使用场景

### 使用方法

#### 1. 依赖注入

在需要使用缓存的类中通过构造函数注入：

```csharp
private readonly ICacheService _cacheService;

public SomeService(ICacheService cacheService)
{
    _cacheService = cacheService;
}
```

#### 2. 基本用法

##### 存储数据

```csharp
// 使用默认过期时间（24小时）
_cacheService.Set("key", value);

// 使用自定义过期时间
_cacheService.Set("key", value, TimeSpan.FromMinutes(30));
```

##### 获取数据

```csharp
// 同步获取
var value = _cacheService.Get<string>("key");

// 异步获取
var value = await _cacheService.GetAsync<string>("key");
```

##### 检查数据是否存在

```csharp
if (_cacheService.Exists("key"))
{
    // 数据存在
}
```

##### 移除数据

```csharp
_cacheService.Remove("key");
```

##### 清空所有缓存

```csharp
_cacheService.Clear();
```

## SQLite数据库服务

### 特点

- **持久化存储**：数据保存在SQLite数据库文件中，应用重启后数据仍然可用
- **ORM支持**：使用SQLSugar ORM框架，简化数据库操作
- **代码优先**：支持通过实体类自动创建数据库表
- **完整的CRUD操作**：提供完整的增删改查操作
- **异步支持**：所有操作都提供异步版本
- **SQL日志**：记录执行的SQL语句，方便调试

### 使用方法

#### 1. 依赖注入

在需要使用数据库服务的类中通过构造函数注入：

```csharp
private readonly ISqlSugarService _sqlSugarService;

public SomeService(ISqlSugarService sqlSugarService)
{
    _sqlSugarService = sqlSugarService;
}
```

#### 2. 创建实体类

使用SQLSugar特性标记实体类：

```csharp
[SugarTable("TableName")]
public class MyEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    
    [SugarColumn(Length = 100)]
    public string Name { get; set; }
    
    public DateTime CreateTime { get; set; }
}
```

#### 3. 创建数据库和表

```csharp
// 创建数据库（SQLite会自动创建）
_sqlSugarService.CreateDatabase();

// 创建表
_sqlSugarService.CreateTable<MyEntity>();
```

#### 4. 基本用法

##### 插入数据

```csharp
// 插入单条数据
var entity = new MyEntity { Name = "测试", CreateTime = DateTime.Now };
_sqlSugarService.Insert(entity);

// 异步插入
await _sqlSugarService.InsertAsync(entity);

// 批量插入
var entities = new List<MyEntity> { ... };
_sqlSugarService.InsertRange(entities);
```

##### 查询数据

```csharp
// 查询单个实体
var entity = _sqlSugarService.GetSingle<MyEntity>(e => e.Id == 1);

// 查询列表
var list = _sqlSugarService.GetList<MyEntity>(e => e.CreateTime > DateTime.Today);

// 查询所有数据
var allList = _sqlSugarService.GetList<MyEntity>();
```

##### 更新数据

```csharp
entity.Name = "更新后的名称";
_sqlSugarService.Update(entity);
```

##### 删除数据

```csharp
// 删除实体
_sqlSugarService.Delete(entity);

// 根据条件删除
_sqlSugarService.DeleteByCondition<MyEntity>(e => e.CreateTime < DateTime.Today.AddDays(-30));
```

#### 5. 高级用法

如果需要更复杂的查询或操作，可以直接获取SQLSugar客户端：

```csharp
var client = _sqlSugarService.GetSqlSugarClient();

// 使用SQLSugar的完整功能
var result = client.Queryable<MyEntity>()
    .Where(e => e.Name.Contains("测试"))
    .OrderBy(e => e.CreateTime, OrderByType.Desc)
    .ToList();
```

## 选择合适的存储服务

- **MemoryCacheService**：适用于临时数据、频繁访问的数据、不需要持久化的数据
- **SqlSugarService**：适用于需要持久化的数据、结构化数据、需要查询和统计的数据

## 最佳实践

1. **合理使用缓存**：将频繁访问但不常变化的数据放入内存缓存
2. **数据分层**：临时数据使用内存缓存，重要数据使用SQLite数据库
3. **设置合理的过期时间**：根据数据的更新频率设置合适的缓存过期时间
4. **异常处理**：处理数据库操作可能出现的异常
5. **使用事务**：对于多个相关的数据库操作，使用事务确保数据一致性

## 示例代码

### 结合使用内存缓存和SQLite数据库

```csharp
public class TaskService
{
    private readonly ICacheService _cacheService;
    private readonly ISqlSugarService _sqlSugarService;
    private readonly ILogService _logService;
    
    public TaskService(ICacheService cacheService, ISqlSugarService sqlSugarService, ILogService logService)
    {
        _cacheService = cacheService;
        _sqlSugarService = sqlSugarService;
        _logService = logService;
        
        // 确保数据库表已创建
        _sqlSugarService.CreateTable<TaskRecord>();
    }
    
    public async Task<TaskRecord> GetTaskRecordAsync(int id)
    {
        // 先从缓存获取
        var cacheKey = $"TaskRecord:{id}";
        var taskRecord = await _cacheService.GetAsync<TaskRecord>(cacheKey);
        
        if (taskRecord == null)
        {
            // 缓存中不存在，从数据库获取
            taskRecord = await _sqlSugarService.GetSingleAsync<TaskRecord>(t => t.Id == id);
            
            if (taskRecord != null)
            {
                // 存入缓存，设置10分钟过期
                await _cacheService.SetAsync(cacheKey, taskRecord, TimeSpan.FromMinutes(10));
            }
        }
        
        return taskRecord;
    }
    
    public async Task SaveTaskRecordAsync(TaskRecord taskRecord)
    {
        try
        {
            // 保存到数据库
            bool success;
            if (taskRecord.Id > 0)
            {
                success = await _sqlSugarService.UpdateAsync(taskRecord);
            }
            else
            {
                success = await _sqlSugarService.InsertAsync(taskRecord);
            }
            
            if (success)
            {
                // 更新缓存
                var cacheKey = $"TaskRecord:{taskRecord.Id}";
                await _cacheService.SetAsync(cacheKey, taskRecord, TimeSpan.FromMinutes(10));
                
                _logService.LogInfo($"任务记录 {taskRecord.Id} 已保存", "TaskService");
            }
        }
        catch (Exception ex)
        {
            _logService.LogError($"保存任务记录失败: {ex.Message}", "TaskService");
            throw;
        }
    }
}
```