using TimerCheckApp.enums;
using TimerCheckApp.Extensions;
using PaddleOCRSharp;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TimerCheckApp.Services.Interfaces;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.DeviceCommands;
using System.IO;
using OpenCvSharp;

namespace TimerCheckApp.AdWatching;

/// <summary>
/// 数据准备自动化实现
/// </summary>
public class DataAdWatching : IAdWatching
{
    private readonly ILogService _logService;
    private readonly IDeviceService _deviceService;
    private readonly ICacheService _cacheService;
    private readonly ISqlSugarService _sqlSugarService;
    private bool IsOperate = false;
    public ConcurrentDictionary<string, string> arrTask { get; set; } = new()
    {

    };

    public DataAdWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
    {
        _logService = logService;
        _deviceService = deviceService;
        _cacheService = cacheService;
        _sqlSugarService = sqlSugarService;
    }

    public async Task AutomateAdWatching(DeviceModel device, CancellationToken cancellationToken)
    {
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



                        if (device.CurPage == AppPageEnum.DyAndKsAddKsJs)
                        {
                            // 刷短剧
                            await HandleSlideUpPageInteractions(device, _device, text, cancellationToken);

                        }
                        else
                        {
                            #region 识别本文逻辑
                            if (text.TextBlocks.Any(a => a.Text == "取消") && !text.TextBlocks.Any(a => a.Text == "打开消息通知"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 1000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "剧单") && text.TextBlocks.Any(a => a.Text == "首页") && text.TextBlocks.Any(a => a.Text == "福利") && text.TextBlocks.Any(a => a.Text == "我的"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "剧场");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 1000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.EndsWith("万播放")))//3181.1万播放
                            {
                                var BoxPoints = text.TextBlocks.Where(a => a.Text.EndsWith("万播放")).ToList();
                                if (BoxPoints.Count > 0)
                                {
                                    var r = new Random().Next(0, BoxPoints.Count - 1);
                                    var BoxPoint = BoxPoints[r];
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 0, BoxPoint.Text);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.Contains("上滑继续观看短剧")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("上滑继续观看短剧"));
                                if (BoxPoint != null)
                                {
                                    var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                    await SwipeAsync(device, _device, 0, "上滑继续观看短剧", pos.X, pos.Y - 50, pos.X, pos.Y - 500, 200);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.Contains("上滑继续看短剧")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("上滑继续看短剧"));
                                if (BoxPoint != null)
                                {
                                    var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                    await SwipeAsync(device, _device, 0, "上滑继续看短剧", pos.X, pos.Y - 50, pos.X, pos.Y - 500, 200);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.Contains("上滑继续观看视频")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("上滑继续观看视频"));
                                if (BoxPoint != null)
                                {
                                    var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                    await SwipeAsync(device, _device, 0, "上滑继续观看视频", pos.X, pos.Y - 500, pos.X, pos.Y - 1000, 200);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("继续阅读下一页")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("继续阅读下一页"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "听" && a.BoxPoints.Any(x => x.Y > 900 && x.X > 300)))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "听");
                                if (BoxPoint != null)
                                {
                                    var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(pos.X - 100, pos.Y) }, 0, BoxPoint.Text);
                                }
                            }
                            else if (
                                text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^(\d{1,})/(\d{4,})$")) ||
                                text.TextBlocks.Any(a => a.Text.EndsWith("点击关闭广告并退出小说")) ||
                                text.TextBlocks.Any(a => a.Text.EndsWith("左滑开始阅读")))
                            {
                                await SwipeAsync(device, _device, new Random().Next(12000, 22000), "左滑看小说", device.Width - 100, device.Height - 100, device.Width - 500, device.Height - 100, 300);
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("看视频最高得")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看视频最高得"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 0, BoxPoint.Text);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "领取成功"))
                            {

                                if (text.TextBlocks.Any(a => a.Text == "X" || a.Text == "x" || a.Text == "×"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x" || a.Text == "×");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 0, BoxPoint.Text);
                                    }
                                }
                                else
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("领取成功"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 0, BoxPoint.Text);
                                    }
                                }
                            }

                            //当上面没有操作时就执行下面
                            if (!IsOperate)
                            {
                                #region 特殊页面
                                if (text.TextBlocks.Any(a => a.Text.StartsWith("立即下载")) && text.TextBlocks.Any(a => a.Text == "侵权投诉"))
                                {
                                    await ClickAsync(device, _device, null, 0, "返回-立即下载");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么")) || text.TextBlocks.Any(a => a.Text == "关注"))
                                {
                                    await ClickAsync(device, _device, null, 0, "返回-直播间返回");
                                }
                                #endregion

                                #region 页面停留超时
                                //else if (_deviceService.CurAdbClient.GetAppStatus(_device, packageName) != AdvancedSharpAdbClient.Models.AppStatus.Foreground)
                                //{
                                //    await StartAppAsync(device, _device, 3000);
                                //}
                                else if ((DateTime.Now - device.LastClickTime).TotalSeconds > 60)
                                {
                                    await SwipeAsync(device, _device, 1000, "返回-停留页面超过60秒", 100, 1000, 500, 1000, 200);
                                    await ClickAsync(device, _device, null, 2000, "返回-停留页面超过60秒");
                                }
                                #endregion
                            }

                            if (_deviceService.GetCurrentApp(device.Id) != device.PackageName)
                            {
                                //await StartAppAsync(device, _device, 10000);
                            }

                            #endregion

                        }
                        IsOperate = false;
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"自动化过程出错: {ex.Message}", device.Id);
                    await Task.Delay(3000);
                }

                // 等待1秒
                await Task.Delay(5000, cancellationToken).ContinueWith(t =>
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

    private async Task HandleSlideUpPageInteractions(DeviceModel device, DeviceData _device, OCRResult text, CancellationToken cancellationToken)
    {
        bool isDesktop = false;
        DateTime dateTime = DateTime.Now;
        if (text.TextBlocks.Any(a => a.Text.Contains("抖音极速版")) && text.TextBlocks.Any(a => a.Text.Contains("快手极速版")))
        {
            isDesktop = true;
        }

        if (isDesktop && dateTime.Hour >= 6)
        {
            _ = arrTask.Remove("快手", out _);
            await Task.Delay(5000, cancellationToken);
        }
        else if (isDesktop && dateTime.Hour >= 4 && dateTime.Hour < 6)
        {
            _ = arrTask.Remove("快手极速版", out _);
            await Task.Delay(5000, cancellationToken);
        }
        else if (isDesktop && dateTime.Hour >= 0 && dateTime.Hour < 4)
        {
            _ = arrTask.Remove("抖音", out _);
            await Task.Delay(5000, cancellationToken);
        }

        if (
            (text.TextBlocks.Any(a => a.Text == "软件商店") && text.TextBlocks.Any(a => a.Text == "主题商店")) ||
            (text.TextBlocks.Any(a => a.Text == "华为商城") && text.TextBlocks.Any(a => a.Text == "应用市场"))
            )
        {
            await SwipeAsync(device, _device, 2000, "左滑", 500, 500, 100, 500, 400);
        }
        else if (text.TextBlocks.Any(a => a.Text == "看视频领取"))
        {
            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "看视频领取");
            if (BoxPoint != null)
            {
                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
            }
        }
        else if (text.TextBlocks.Any(a => a.Text == "看点") && text.TextBlocks.Any(a => a.Text == "关注"))
        {
            await ClickHomeButtonAsync(device, _device, 3000);
        }
        else if (text.TextBlocks.Any(a => a.Text.Contains("新人福利")) && text.TextBlocks.Any(a => a.Text == "立即领取"))
        {

            byte[]? bytes = await _deviceService.CaptureWithAdb(device);
            if (bytes != null)
            {
                string iconTemp = Path.Combine(Environment.CurrentDirectory, $"Resources\\template\\ks_close.png");
                // 1. 加载原始图片和模板
                using Mat src = Cv2.ImDecode(bytes, ImreadModes.Color);  // 原始截图
                using Mat template = Cv2.ImRead(iconTemp, ImreadModes.Color); // 菜单图标模板

                // 3. 模板匹配（使用归一化相关系数）
                using Mat result = new Mat();
                Cv2.MatchTemplate(src, template, result, TemplateMatchModes.CCoeffNormed);

                // 4. 获取最佳匹配位置
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);
                src?.Dispose();
                template?.Dispose();
                result?.Dispose();
                if (maxVal > 0.7)
                {
                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(maxLoc.X + 5, maxLoc.Y + 5) }, 1000, "匹配关闭按钮");
                }
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
        else if (text.TextBlocks.Any(a => a.Text.Contains("外卖红包") || a.Text.Contains("闪购外卖")) && text.TextBlocks.Any(a => a.Text == "去领取" || a.Text == "去使用"))
        {
            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去领取" || a.Text == "去使用");
            if (BoxPoint != null)
            {
                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
            }
        }
        else if (dateTime.Hour >= 6 && !arrTask.ContainsKey("快手"))
        {
            _logService.LogInfo($"打开快手", device.Id);
            _ = arrTask.TryAdd("快手", DateTime.Now.ToString());

            _deviceService.CleanDeviceRunningApps(device.Id);
            await Task.Delay(15000, cancellationToken);

            device.PackageName = PlatformType.Kuaishou.GetPackageName();
            await StartAppAsync(device, _device, 5000);
        }
        else if (dateTime.Hour >= 4 && !arrTask.ContainsKey("快手极速版") && dateTime.Hour < 6)
        {
            _logService.LogInfo($"打开快手极速版", device.Id);
            _ = arrTask.TryAdd("快手极速版", DateTime.Now.ToString());

            _deviceService.CleanDeviceRunningApps(device.Id);
            await Task.Delay(15000, cancellationToken);

            device.PackageName = PlatformType.KuaishouJs.GetPackageName();
            await StartAppAsync(device, _device, 5000);
        }
        else if (dateTime.Hour >= 0 && !arrTask.ContainsKey("抖音") && dateTime.Hour < 4)
        {
            _logService.LogInfo($"打开抖音", device.Id);
            _ = arrTask.TryAdd("抖音", DateTime.Now.ToString());

            _deviceService.CleanDeviceRunningApps(device.Id);
            await Task.Delay(15000, cancellationToken);

            device.PackageName = PlatformType.Douyin.GetPackageName();
            await StartAppAsync(device, _device, 5000);
        }
        else if (text.TextBlocks.Any(a => a.Text == "我") && text.TextBlocks.Any(a => a.Text.StartsWith("首页")))
        {
            await _cacheService.RemoveAsync($"DataAdWatching_{device.CurPage.ToString()}");
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
        else
        {
            int count = await _cacheService.GetAsync<int>($"DataAdWatching_{device.CurPage.ToString()}");
            count++;
            await _cacheService.SetAsync($"DataAdWatching_{device.CurPage.ToString()}", count);
            if (count > 10)
            {
                await _deviceService.PlayAudioAsync("error.wav");
                await Task.Delay(10000, cancellationToken);
            }
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