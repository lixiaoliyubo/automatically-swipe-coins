using TimerCheckApp.enums;
using TimerCheckApp.Extensions;
using PaddleOCRSharp;
using System.Collections.Concurrent;
using TimerCheckApp.Services.Interfaces;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.DeviceCommands;
using System.Text.RegularExpressions;
using System;
using TimerCheckApp.Services.Extensions;
using TimerCheckApp.Models;
using OpenCvSharp;
using System.IO;

namespace TimerCheckApp.AdWatching;

/// <summary>
/// 支付宝自动化实现
/// </summary>
public class AliPayAdWatching : IAdWatching
{
    private readonly ILogService _logService;
    private readonly IDeviceService _deviceService;
    private readonly ICacheService _cacheService;
    private readonly ISqlSugarService _sqlSugarService;
    private bool IsOperate = false;
    public ConcurrentDictionary<string, string> arrTask { get; set; } = new()
    {

    };

    public AliPayAdWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
    {
        _logService = logService;
        _deviceService = deviceService;
        _cacheService = cacheService;
        _sqlSugarService = sqlSugarService;
    }

    public async Task AutomateAdWatching(DeviceModel device, CancellationToken cancellationToken)
    {
        bool isDesktop = false;
        var _device = new DeviceData { Serial = device.Id };
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    OCRResult? text = await _deviceService.GetOCRText(device);
                    if (text?.Text?.Length > 1)
                    {
                        if (device.CurPage == AppPageEnum.Data)
                        {
                            // 获取当前时间
                            DateTime now = DateTime.Now;
                            //_logService.LogInfo($"当前时间: {now:yyyy-MM-dd HH:mm:ss}", device.Id);


                            // 检测图片中显示的应用图标组合
                            if (text.TextBlocks.Any(a => a.Text.Contains("支付宝")) && text.TextBlocks.Any(a => a.Text.Contains("支付宝1")))
                            {
                                isDesktop = true;
                            }

                            if (isDesktop)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "支付宝1");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 10000, BoxPoint.Text);
                                }
                            }

                        }

                        #region 识别本文逻辑


                        #region 系统层面
                        if (text.TextBlocks.Any(a => a.Text == "仅充电") && text.TextBlocks.Any(a => a.Text == "忽略"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "忽略");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        #endregion

                        #region 弹出层面
                        else if (text.TextBlocks.Any(a => a.Text.Contains("明日继续来领")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "明日继续来领");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "领取今日红包"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领取今日红包");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("外卖红包") || a.Text.Contains("闪购外卖")) && text.TextBlocks.Any(a => a.Text == "去领取" || a.Text == "去使用"))
                        {

                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去领取" || a.Text == "去使用");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        #endregion


                        #region 页面层面

                        else if (
                            text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^([\d-\.]+)元$")) || text.TextBlocks.Any(a => a.Text.Contains("上滑继续看视频")) || text.TextBlocks.Any(a => a.Text == "不喜欢") || text.TextBlocks.Any(a => a.Text == "点击进入直播间" || a.Text == "点击进人直播间") || 
                            (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^(\d+)/(\d+)$")) && text.TextBlocks.Any(a => a.Text == "关注") && !text.TextBlocks.Any(a => a.Text.Contains("更多直播"))) ||
                            (text.TextBlocks.Any(a => a.Text == "关注" && a.BoxPoints.Max(x => x.X) > 500) && text.TextBlocks.Any(a => a.Text == "发现"))
                            )
                        {
                            if (text.TextBlocks.Any(a => a.Text.Contains("我的红包")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "我的红包");
                                if (BoxPoint != null)
                                {
                                    var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(pos.X, pos.Y - 200) }, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "明日可领"))
                            {
                                // 如果是凌晨00:01-04:00且当前在支付宝1中，返回桌面并打开普通支付宝
                                _logService.LogInfo("凌晨时段检测到支付宝1，准备切换到普通支付宝", device.Id);
                                arrTask.TryAdd("支付宝1", DateTime.Now.ToString());
                                _deviceService.CleanDeviceRunningApps(device.Id);
                                await Task.Delay(15000, cancellationToken);
                                continue;
                            }
                            else
                            {
                                var r = new Random().Next(5000, 15000); 
                                if (!text.TextBlocks.Any(a => a.Text == "点击进入直播间"))
                                {
                                    await Task.Delay(r, cancellationToken);
                                }
                                if (device.Width > 0 && device.Height > 0)
                                {
                                    var l = new Random().Next((device.Width / 2) - 100, (device.Width / 2) + 100);
                                    await SwipeAsync(device, _device, 0, "滑动刷视频", l, (device.Height - 300), l, (device.Height - 1000), 400);
                                }
                                else
                                {
                                    await SwipeAsync(device, _device, 0, "滑动刷视频", 300, 1000, 300, 600, 400);
                                }
                            }
                        }
                        #endregion
                        else
                        {
                            #region 非首页

                            if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么") || a.Text == "立即关注"|| a.Text == "已关闭评论" || a.Text.Contains("更多直播") || a.Text == "粉丝团"))// || text.TextBlocks.Any(a => a.Text == "关注")
                            {
                                await ClickAsync(device, _device, null, 3000, "返回到刷视频");
                                continue;
                            }
                            else if (!text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^([\d-\.]+)元$")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "视频");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "闪购") && text.TextBlocks.Any(a => a.Text == "订单"))
                            {
                                await ClickAsync(device, _device, null, 2000, "返回-退出闪购");
                            }
                            else if (text.TextBlocks.Any(a => a.Text.Contains("看了这么久")) && text.TextBlocks.Any(a => a.Text == "关注"))
                            {
                                await ClickAsync(device, _device, null, 2000, "返回-直播间");
                                await ClickAsync(device, _device, null, 2000, "返回-直播间");
                            }
                            #endregion
                        }
                        if (device.CurrentApp != device.PackageName)
                        {
                            //await StartAppAsync(device, _device, 10000);
                        }
                        #endregion

                        IsOperate = false;
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"自动化过程出错: {ex.Message}", device.Id);
                }

                // 等待1秒
                await Task.Delay(3000, cancellationToken).ContinueWith(t =>
                {
                    if (t.IsCanceled)
                        _logService.LogWarning($"任务被取消", device.Id);
                });
            }
        }
        catch (OperationCanceledException)
        {
            // 操作被取消
        }
        catch (Exception ex)
        {
            _logService.LogError($"自动化过程出错: {ex.Message}", device.Id);
        }
    }
    /// <summary>
    /// 执行点击操作
    /// </summary>
    /// <param name="points">点击坐标</param>
    /// <param name="delay">延迟时间</param>
    /// <param name="text">点击文本描述</param>
    /// <returns>异步任务</returns>
    private async Task ClickAsync(DeviceModel device, DeviceData deviceData, List<OCRPoint>? points = null, int delay = 0, string text = "")
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
    /// <param name="delay">延迟时间</param>
    /// <param name="text">滑动文本描述</param>
    /// <param name="startX">起始X坐标</param>
    /// <param name="startY">起始Y坐标</param>
    /// <param name="endX">结束X坐标</param>
    /// <param name="endY">结束Y坐标</param>
    /// <param name="duration">持续时间</param>
    /// <param name="duration">持续时间</param>
    /// <returns>异步任务</returns>
    private async Task SwipeAsync(DeviceModel device, DeviceData deviceData, int delay, string text, int startX, int startY, int endX, int endY, int duration)
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
    /// 运行App
    /// </summary>
    /// <param name="delay"></param>
    /// <returns></returns>
    private async Task StartAppAsync(DeviceModel device, DeviceData deviceData, int delay = 0)
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
    /// 返回首页
    /// </summary>
    /// <param name="delay"></param>
    private async Task ClickHomeButtonAsync(DeviceModel device, DeviceData deviceData, int delay = 0)
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
}