# 缓存服务使用文档

## 概述

本项目提供了两种缓存服务实现：

1. **`FileCacheService`**：基于本地文件的缓存实现，提供了应用重启后缓存数据持久化的功能。
2. **`TieredCacheService`**：分级缓存实现，提供内存缓存和文件缓存两级缓存，支持仅内存缓存模式。

两种实现都支持 `ICacheService` 接口，提供同步和异步操作，默认缓存过期时间为24小时。

## 特点

### FileCacheService 特点

- **持久化存储**：缓存数据保存在本地文件中，应用重启后仍然可用
- **内存与文件双重缓存**：先从内存读取，提高访问速度；内存中不存在时再从文件读取
- **自动过期**：支持设置缓存项的过期时间，默认24小时
- **线程安全**：使用 `ConcurrentDictionary` 和 `SemaphoreSlim` 确保多线程环境下的安全访问
- **同步和异步API**：提供同步和异步方法，适应不同的使用场景
- **JSON序列化**：使用 `System.Text.Json` 进行数据序列化，支持复杂对象的缓存

### TieredCacheService 特点

- **分级缓存**：提供内存缓存和文件缓存两级缓存
- **仅内存缓存模式**：支持仅内存缓存模式，适用于频繁访问但不需要持久化的数据
- **自动清理**：定期清理过期的内存缓存项
- **差异化过期时间**：仅内存缓存和持久化缓存可以设置不同的默认过期时间
- **所有 FileCacheService 特点**：包含 FileCacheService 的所有特点

## 使用方法

### 1. 服务注册

服务已在 `ServiceCollectionExtensions.cs` 中注册为单例模式：

```csharp
// 注册文件缓存服务（单例模式）
services.AddSingleton<ICacheService>(provider => 
{
    var logService = provider.GetRequiredService<ILogService>();
    return new FileCacheService(logService);
});

// 或者注册分级缓存服务
/*
services.AddSingleton<ICacheService>(provider => 
{
    var logService = provider.GetRequiredService<ILogService>();
    return new TieredCacheService(logService);
});
*/
```

### 2. 依赖注入

在需要使用缓存的类中通过构造函数注入：

```csharp
private readonly ICacheService _cacheService;

public YourClass(ICacheService cacheService)
{
    _cacheService = cacheService;
}
```

### 3. 基本用法

#### 存储数据

```csharp
// 使用默认过期时间（24小时）
_cacheService.Set("key", value);

// 使用自定义过期时间
_cacheService.Set("key", value, TimeSpan.FromMinutes(30));
```

#### 获取数据

```csharp
var value = _cacheService.Get<YourType>("key");

// 检查是否存在后再获取
if (_cacheService.Exists("key"))
{
    var value = _cacheService.Get<YourType>("key");
    // 使用value...
}
```

#### 移除数据

```csharp
_cacheService.Remove("key");
```

#### 清空所有缓存

```csharp
_cacheService.Clear();
```

### 4. 异步用法

```csharp
// 异步存储
await _cacheService.SetAsync("key", value);

// 异步获取
var value = await _cacheService.GetAsync<YourType>("key");

// 异步检查存在
if (await _cacheService.ExistsAsync("key"))
{
    // ...
}

// 异步移除
await _cacheService.RemoveAsync("key");

// 异步清空
await _cacheService.ClearAsync();
```

## 性能优化建议

1. **缓存频繁访问的数据**：将经常需要访问但不经常变化的数据放入缓存
2. **合理设置过期时间**：根据数据的更新频率设置合适的过期时间
3. **避免缓存过大对象**：大对象的序列化和反序列化可能会影响性能
4. **使用有意义的键名**：采用有规律的键名命名方式，便于管理和调试
5. **使用分级缓存**：对于非常频繁访问的数据，使用 `TieredCacheService` 的仅内存缓存模式

## 选择合适的缓存服务

- **FileCacheService**：适用于需要持久化的数据，如用户设置、配置信息等
- **TieredCacheService**：适用于需要更灵活缓存策略的场景，特别是有大量频繁访问但不需要持久化的临时数据

## 缓存位置

缓存文件默认保存在应用程序目录下的 `Cache` 文件夹中，每个缓存项对应一个 `.cache` 文件。

## 示例代码

### 基本使用示例

完整的基本使用示例可以参考 `Examples/CacheServiceExample.cs` 文件。

### 分级缓存使用示例

```csharp
// 使用分级缓存服务
var tieredCache = new TieredCacheService(logService);

// 持久化缓存（默认24小时过期）
tieredCache.Set("persistentData", new UserSettings { Username = "admin", Theme = "dark" });

// 仅内存缓存（默认30分钟过期）
tieredCache.SetMemoryOnly("temporaryData", "临时数据");

// 仅内存缓存（自定义过期时间）
tieredCache.SetMemoryOnly("quickExpireData", "快速过期数据", TimeSpan.FromMinutes(5));

// 获取数据（会自动从合适的缓存层获取）
var settings = tieredCache.Get<UserSettings>("persistentData");
var tempData = tieredCache.Get<string>("temporaryData");
```

### 使用扩展方法

为了在不修改 `ICacheService` 接口的情况下使用 `TieredCacheService` 的特殊功能，项目提供了扩展方法：

```csharp
using TimerCheckApp.Services.Extensions;

// 在任何使用 ICacheService 的地方
public class SomeService
{
    private readonly ICacheService _cacheService;

    public SomeService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public void SomeMethod()
    {
        // 检查是否支持分级缓存
        if (_cacheService.SupportsTieredCache())
        {
            Console.WriteLine("使用的是分级缓存服务");
        }

        // 使用扩展方法设置仅内存缓存
        // 如果底层是 TieredCacheService，则使用专用的内存缓存
        // 如果是其他实现，则使用普通缓存但设置较短的过期时间
        _cacheService.SetMemoryOnly("tempKey", "临时数据");

        // 异步版本
        await _cacheService.SetMemoryOnlyAsync("asyncTempKey", "异步临时数据");
    }
}
```