using TimerCheckApp.enums;
using TimerCheckApp.Extensions;
using PaddleOCRSharp;
using TimerCheckApp.Services.Interfaces;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.DeviceCommands;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using TimerCheckApp.Models;
using System.Diagnostics;
using System;

namespace TimerCheckApp.AdWatching;

/// <summary>
/// 支付宝自动化实现
/// </summary>
public class AliPayAdWatching : AdWatchingBase
{
    public AliPayAdWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
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
    {;
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
                        DateTime now = DateTime.Now;
                        if (device.CurPage == AppPageEnum.Data)
                        {
                            // 获取当前时间
                            //_logService.LogInfo($"当前时间: {now:yyyy-MM-dd HH:mm:ss}", device.Id);

                            if (now.Hour >= 22)
                            {
                                await Task.Delay(15000, cancellationToken);
                                continue;
                            }
                            else {
                                device.CurPage = AppPageEnum.Node;
                            }

                        }

                        if (text.TextBlocks.Any(a => a.Text.Contains("抖音极速版")) && text.TextBlocks.Any(a => a.Text.Contains("支付宝")))
                        {
                            device.PackageName = PlatformType.AliPay.GetPackageName();
                            await StartAppAsync(device, device.PackageName);
                            await Task.Delay(10000);
                            continue;
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

                        else if (text.TextBlocks.Any(a => a.Text == "明日可领") && now.Hour < 9)
                        {
                            //var DeviceCount = _cacheService.Get<string>($"DeviceCount").ToList<string>() ?? new();
                            //DeviceCount.Remove(device.Id);
                            //await _cacheService.SetAsync($"DeviceCount", DeviceCount.ToJson());
                            _deviceService.StopDeviceCheck(device);

                            await Task.Delay(5000, cancellationToken);

                            _logService.LogInfo("完成支付宝刷视频", device.Id);
                            _deviceService.CleanDeviceRunningApps(device.Id);
                            await Task.Delay(15000, cancellationToken);

                            device.CurPlatform = PlatformType.Kuaishou;
                            device.CurPage = AppPageEnum.SlideUpVideo;
                            _deviceService.StartDeviceCheck(device, "");
                        }
                        else if (
                            (text.TextBlocks.Any(a => a.Text == "软件商店") && text.TextBlocks.Any(a => a.Text == "主题商店")) ||
                            (text.TextBlocks.Any(a => a.Text == "华为商城") && text.TextBlocks.Any(a => a.Text == "应用市场"))
                        )
                        {
                            await SwipeAsync(device, _device, SwipeDirection.Left, 0, "左滑", 200);
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "视频" || a.Text == "收付款") && text.TextBlocks.Any(a => a.Text == "我的") && !text.TextBlocks.Any(a => a.Text == "发现"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "视频");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                            }
                            else
                            {
                                var pos = new List<OCRPoint>() { new OCRPoint(device.Width / 2, device.Height - 50) };
                                await ClickAsync(device, _device, pos, 3000, "支付宝位置识别");
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "分词" || a.Text.Contains("中/英")) && text.TextBlocks.Any(a => a.Text == "符" || a.Text == "换行"))
                        {
                            await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint() { X = (device.Height * 0.5).ToInt(), Y = (device.Height * 0.3).ToInt() } }, 3000, "评论收起");
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("我的红包")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "我的红包");
                            if (BoxPoint != null)
                            {
                                var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(pos.X, pos.Y - 200) }, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (
                           (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^([\d-\.]+)元$")) && text.TextBlocks.Any(a => a.Text == "关注" || a.Text == "视频" || a.Text == "发现")) ||
                           (text.TextBlocks.Any(a => a.Text == "关注") && text.TextBlocks.Any(a => a.Text == "发现") && text.TextBlocks.Any(a => a.Text == "支")) ||
                           (text.TextBlocks.Any(a => a.Text == "发首评" || a.Text == "发条评论" || a.Text == "评论区已关闭" || a.Text.Contains("作者期待你的评论")) && text.TextBlocks.Any(a => a.Text == "支")) ||
                           (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^@([\s\S]{3,})$") && a.BoxPoints.OrderBy(a => a.X).FirstOrDefault()?.X < 50 && a.BoxPoints.OrderByDescending(a => a.Y).FirstOrDefault()?.Y > (device.Height * 0.8)) && text.TextBlocks.Any(a => a.Text == "支"))
                           )
                        {

                            //var BoxPointx = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"^@([\s\S]+)$"));
                            //var posx = BoxPointx.BoxPoints.CalculateTextBlockCenter();
                            //Debug.WriteLine($"设备：{_device.Serial},位置：X={BoxPointx.BoxPoints.OrderBy(a => a.X).FirstOrDefault().X},Y={BoxPointx.BoxPoints.OrderByDescending(a => a.X).FirstOrDefault().Y}");


                            int Max = 13000;
                            var m = Regex.Match(text.Text, @"(\d{1,2})/(\d{1,2})支");
                            if (m.Success)
                            {
                                Max = GetDynamicMax(m.Groups[2].Value.ToInt());
                            }
                            var r = new Random().Next(5000, Max);
                            if (!text.TextBlocks.Any(a => a.Text == "点击进入直播间" || a.Text.Contains("上滑继续观看视频")))
                            {
                                await Task.Delay(r, cancellationToken);
                            }
                            await SwipeAsync(device, _device, SwipeDirection.Up, 0, "滑动刷视频", 400);
                        }
                        #endregion
                        else
                        {
                            #region 非首页

                            if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么") || a.Text == "立即关注" || a.Text == "已关闭评论" || a.Text.Contains("更多直播") || a.Text.Contains("欢迎各位小伙伴来到直播间") || a.Text == "粉丝团" || a.Text == "分享直播间"))// || text.TextBlocks.Any(a => a.Text == "关注")
                            {
                                await ClickAsync(device, _device, null, 3000, "返回到刷视频");
                                continue;
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
                            else if ((DateTime.Now - device.LastClickTime).TotalSeconds > 60)
                            {
                                _deviceService.CleanDeviceRunningApps(device.Id);
                                await Task.Delay(15000, cancellationToken);

                                await StartAppAsync(device, device.PackageName);
                                await Task.Delay(10000);
                                continue;
                            }
                            else if ((DateTime.Now - device.LastClickTime).TotalSeconds > 30)
                            {
                                if (_deviceService.CurAdbClient.GetAppStatus(_device, device.PackageName) != AppStatus.Foreground)
                                {
                                    await StartAppAsync(device, device.PackageName);
                                    await Task.Delay(3000);
                                }
                                else await ClickAsync(device, _device, null, 2000, "返回-停留页面超过60秒");
                                continue;
                            }
                            #endregion
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

}