using System;
using System.Collections.Concurrent;
using TimerCheckApp.Extensions;
using TimerCheckApp.Services.Interfaces;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.DeviceCommands;
using PaddleOCRSharp;
using System.ComponentModel;

namespace TimerCheckApp.AdWatching;

/// <summary>
/// 滑动方向枚举
/// </summary>
public enum SwipeDirection
{
    /// <summary>
    /// 上滑（从屏幕下方向上滑动）
    /// </summary>
    [Description("上滑")]
    Up,
    
    /// <summary>
    /// 下滑（从屏幕上方向下滑动）
    /// </summary>
    [Description("下滑")]
    Down,
    
    /// <summary>
    /// 左滑（从屏幕右侧向左滑动）
    /// </summary>
    [Description("左滑")]
    Left,
    
    /// <summary>
    /// 右滑（从屏幕左侧向右滑动）
    /// </summary>
    [Description("右滑")]
    Right
}

/// <summary>
/// 广告观看自动化基类
/// </summary>
public abstract class AdWatchingBase : IAdWatching
{
    /// <summary>
    /// 日志服务
    /// </summary>
    protected readonly ILogService _logService;
    
    /// <summary>
    /// 设备服务
    /// </summary>
    protected readonly IDeviceService _deviceService;
    
    /// <summary>
    /// 缓存服务
    /// </summary>
    protected readonly ICacheService _cacheService;
    
    /// <summary>
    /// SQL Sugar服务
    /// </summary>
    protected readonly ISqlSugarService _sqlSugarService;
    
    /// <summary>
    /// 当前是否有操作
    /// </summary>
    protected bool IsOperate = false;

    /// <summary>
    /// 任务列表
    /// </summary>
    public List<TaskItem> arrTask { get; set; } = new();

    /// <summary>
    /// 用餐时间字典
    /// </summary>
    protected ConcurrentDictionary<string, (TimeSpan start, TimeSpan end)> mealTimes = new()
    {
        ["早餐"] = (TimeSpan.Parse("07:00"), TimeSpan.Parse("09:00")),
        ["午餐"] = (TimeSpan.Parse("11:00"), TimeSpan.Parse("13:00")),
        ["晚餐"] = (TimeSpan.Parse("17:00"), TimeSpan.Parse("19:00")),
        ["夜宵"] = (TimeSpan.Parse("22:00"), TimeSpan.Parse("23:59"))
    };

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logService">日志服务</param>
    /// <param name="deviceService">设备服务</param>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="sqlSugarService">SQL Sugar服务</param>
    protected AdWatchingBase(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
    {
        _logService = logService;
        _deviceService = deviceService;
        _cacheService = cacheService;
        _sqlSugarService = sqlSugarService;
    }

    /// <summary>
    /// 执行广告观看自动化
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public abstract Task AutomateAdWatching(DeviceModel device, CancellationToken cancellationToken);

    /// <summary>
    /// 执行点击操作
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <param name="deviceData">设备数据</param>
    /// <param name="points">点击坐标</param>
    /// <param name="delay">延迟时间</param>
    /// <param name="text">点击文本描述</param>
    /// <returns>异步任务</returns>
    protected async Task ClickAsync(DeviceModel device, DeviceData deviceData, List<OCRPoint>? points = null, int delay = 0, string text = "")
    {
        IsOperate = true;
        device.LastClickTime = DateTime.Now;

        // 检查是否重复点击
        //if (lastClickInfo.IsSameClick(points, text))
        //{
        //    _logService.LogInfo($"跳过重复点击：{text}");
        //    return;
        //}
        if (points != null)
        {
            _logService.LogInfo($"位置：{device.CurPage}{device.CurPage.GetDescription()}，点击了：{text}", device.Id);
            if (points.Count == 1)
            {
                System.Drawing.Point center = new System.Drawing.Point() { X = points[0].X, Y = points[0].Y };
                await _deviceService.CurAdbClient.ClickAsync(deviceData, center);
            }
            else
            {
                System.Drawing.Point center = points.CalculateTextBlockCenter();
                await _deviceService.CurAdbClient.ClickAsync(deviceData, center);
            }
        }
        else
        {
            _logService.LogInfo($"位置：{device.CurPage}{device.CurPage.GetDescription()}，点击了：{text}", device.Id);
            await _deviceService.CurAdbClient.ClickBackButtonAsync(deviceData);
        }

        if (delay > 0)
        {
            await Task.Delay(delay);
        }
    }

    /// <summary>
    /// 执行滑动操作
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <param name="deviceData">设备数据</param>
    /// <param name="delay">延迟时间</param>
    /// <param name="text">滑动文本描述</param>
    /// <param name="startX">起始X坐标</param>
    /// <param name="startY">起始Y坐标</param>
    /// <param name="endX">结束X坐标</param>
    /// <param name="endY">结束Y坐标</param>
    /// <param name="duration">持续时间</param>
    /// <returns>异步任务</returns>
    protected async Task SwipeAsync(DeviceModel device, DeviceData deviceData, int delay, string text, int startX, int startY, int endX, int endY, int duration)
    {
        IsOperate = true;
        device.LastClickTime = DateTime.Now;//.ToString("yyyy-MM-dd HH:mm:ss")
        _logService.LogInfo($"位置：{device.CurPage}{device.CurPage.GetDescription()}，滑动了：{text}", device.Id);
        await _deviceService.CurAdbClient.SwipeAsync(deviceData, new System.Drawing.Point(startX, startY), new System.Drawing.Point(endX, endY), duration);

        if (delay > 0)
        {
            await Task.Delay(delay);
        }
    }


    /// <summary>
    /// 启动应用
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <param name="deviceData">设备数据</param>
    /// <param name="delay">延迟时间</param>
    /// <returns>异步任务</returns>
    protected async Task StartAppAsync(DeviceModel device, DeviceData deviceData, int delay = 0)
    {
        IsOperate = true;
        device.LastClickTime = DateTime.Now;
        await _deviceService.CurAdbClient.StartAppAsync(deviceData, device.PackageName);
        if (delay > 0)
        {
            await Task.Delay(delay);
        }
    }

    /// <summary>
    /// 启动应用
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <param name="packageName">包名</param>
    /// <param name="delay">持续时间</param>
    /// <returns>异步任务</returns>
    protected async Task StartAppAsync(DeviceModel device, string packageName, int delay = 0)
    {
        IsOperate = true;
        device.LastClickTime = DateTime.Now;
        device.PackageName = packageName;
        var _device = new DeviceData { Serial = device.Id };
        await _deviceService.CurAdbClient.StartAppAsync(_device, device.PackageName);
        if (delay > 0)
        {
            await Task.Delay(delay);
        }
    }

    /// <summary>
    /// 点击Home按钮
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <param name="deviceData">设备数据</param>
    /// <param name="delay">延迟时间</param>
    /// <returns>异步任务</returns>
    protected async Task ClickHomeButtonAsync(DeviceModel device, DeviceData deviceData, int delay = 0)
    {
        IsOperate = true;
        device.LastClickTime = DateTime.Now;
        _logService.LogInfo($"位置：{device.CurPage}{device.CurPage.GetDescription()}，点击返回首页", device.Id);
        await _deviceService.CurAdbClient.ClickHomeButtonAsync(deviceData);

        if (delay > 0)
        {
            await Task.Delay(delay);
        }
    }


    protected int GetDynamicMax(int value, int baseMax = 15000)
    {
        const int minValue = 5;
        const int maxValue = 15;

        // 确保value在5~20之间
        value = Math.Clamp(value, minValue, maxValue);

        // 线性反比
        int step = 500; // 每增加1个Value减少500ms
        return baseMax - (value - minValue) * step;
    }
    
    /// <summary>
    /// 根据方向执行滑动操作
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <param name="deviceData">设备数据</param>
    /// <param name="direction">滑动方向</param>
    /// <param name="delay">延迟时间（毫秒）</param>
    /// <param name="text">滑动文本描述</param>
    /// <param name="duration">滑动持续时间（毫秒），默认200</param>
    /// <returns>异步任务</returns>
    protected async Task SwipeAsync(DeviceModel device, DeviceData deviceData, SwipeDirection direction, int delay, string text, int duration = 200)
    {
        int startX, startY, endX, endY;
        
        // 根据滑动方向计算起始和结束坐标
        switch (direction)
        {
            case SwipeDirection.Up: // 上滑：从屏幕下方向上滑动（Y坐标从大到小）
                startX = device.Width / 2;
                startY = (int)(device.Height * 0.7); // 屏幕70%位置
                endX = startX;
                endY = (int)(device.Height * 0.3);   // 屏幕30%位置
                break;
                
            case SwipeDirection.Down: // 下滑：从屏幕上方向下滑动（Y坐标从小到大）
                startX = device.Width / 2;
                startY = (int)(device.Height * 0.3); // 屏幕30%位置
                endX = startX;
                endY = (int)(device.Height * 0.7);   // 屏幕70%位置
                break;
                
            case SwipeDirection.Left: // 左滑：从屏幕右侧向左滑动（X坐标从大到小）
                startX = (int)(device.Width * 0.8);  // 屏幕80%位置
                startY = device.Height / 2;
                endX = (int)(device.Width * 0.2);    // 屏幕20%位置
                endY = startY;
                break;
                
            case SwipeDirection.Right: // 右滑：从屏幕左侧向右滑动（X坐标从小到大）
                startX = (int)(device.Width * 0.2);  // 屏幕20%位置
                startY = device.Height / 2;
                endX = (int)(device.Width * 0.8);    // 屏幕80%位置
                endY = startY;
                break;
                
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "不支持的滑动方向");
        }
        
        // 调用基础滑动方法执行滑动操作
        await SwipeAsync(device, deviceData, delay, text, startX, startY, endX, endY, duration);
    }
}