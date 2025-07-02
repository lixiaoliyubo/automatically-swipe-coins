using TimerCheckApp.enums;
using TimerCheckApp.Extensions;
using PaddleOCRSharp;
using System.Text.RegularExpressions;
using TimerCheckApp.Services.Interfaces;
using AdvancedSharpAdbClient.Models;
using System.IO;
using OpenCvSharp;
using System.Collections.Concurrent;

namespace TimerCheckApp.AdWatching;

/// <summary>
/// 数据准备自动化实现
/// </summary>
public class DataAdWatching : AdWatchingBase
{

    public DataAdWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
        : base(logService, deviceService, cacheService, sqlSugarService)
    {
        // 初始化任务列表
        arrTask = new()
        {
            // 支付宝相关任务可以在这里添加
        };

        // 初始化用餐时间（如果需要）
        mealTimes = new ConcurrentDictionary<string, (TimeSpan start, TimeSpan end)>();
    }

    public override async Task AutomateAdWatching(DeviceModel device, CancellationToken cancellationToken)
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
            _ = arrTask.RemoveTaskItem("快手");
            await Task.Delay(5000, cancellationToken);
        }
        else if (isDesktop && dateTime.Hour >= 4 && dateTime.Hour < 6)
        {
            _ = arrTask.RemoveTaskItem("快手极速版");
            await Task.Delay(5000, cancellationToken);
        }
        else if (isDesktop && dateTime.Hour >= 0 && dateTime.Hour < 4)
        {
            _ = arrTask.RemoveTaskItem("抖音");
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
        else if (dateTime.Hour >= 6 && !arrTask.ContainsTaskItem("快手"))
        {
            _logService.LogInfo($"打开快手", device.Id);
            _ = arrTask.UpdateTaskItem("快手", DateTime.Now.ToString());

            _deviceService.CleanDeviceRunningApps(device.Id);
            await Task.Delay(15000, cancellationToken);

            device.PackageName = PlatformType.Kuaishou.GetPackageName();
            await StartAppAsync(device, device.PackageName);
            await Task.Delay(5000);
        }
        else if (dateTime.Hour >= 4 && !arrTask.ContainsTaskItem("快手极速版") && dateTime.Hour < 6)
        {
            _logService.LogInfo($"打开快手极速版", device.Id);
            _ = arrTask.UpdateTaskItem("快手极速版", DateTime.Now.ToString());

            _deviceService.CleanDeviceRunningApps(device.Id);
            await Task.Delay(15000, cancellationToken);

            device.PackageName = PlatformType.KuaishouJs.GetPackageName();
            await StartAppAsync(device, device.PackageName);
            await Task.Delay(5000);
        }
        else if (dateTime.Hour >= 0 && !arrTask.ContainsTaskItem("抖音") && dateTime.Hour < 4)
        {
            _logService.LogInfo($"打开抖音", device.Id);
            _ = arrTask.UpdateTaskItem("抖音", DateTime.Now.ToString());

            _deviceService.CleanDeviceRunningApps(device.Id);
            await Task.Delay(15000, cancellationToken);

            device.PackageName = PlatformType.Douyin.GetPackageName();
            await StartAppAsync(device, device.PackageName);
            await Task.Delay(5000);
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

}