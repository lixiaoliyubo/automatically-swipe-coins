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
                            text.TextBlocks.Any(a => a.Text == "明日可领") ||
                            text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^([\d-\.]+)元$")) || text.TextBlocks.Any(a => a.Text.Contains("上滑继续看视频")) || text.TextBlocks.Any(a => a.Text == "不喜欢") || text.TextBlocks.Any(a => a.Text == "点击进入直播间" || a.Text == "点击进人直播间") || 
                            (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^(\d+)/(\d+)$")) && text.TextBlocks.Any(a => a.Text == "关注") && !text.TextBlocks.Any(a => a.Text.Contains("更多直播"))) ||
                            (text.TextBlocks.Any(a => a.Text == "关注" && a.BoxPoints.Max(x => x.X) > 500) && text.TextBlocks.Any(a => a.Text == "发现"))
                            )
                        {

                            //var BoxPointx = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"^@([\s\S]+)$"));
                            //var posx = BoxPointx.BoxPoints.CalculateTextBlockCenter();
                            //Debug.WriteLine($"设备：{_device.Serial},位置：X={BoxPointx.BoxPoints.OrderBy(a => a.X).FirstOrDefault().X},Y={BoxPointx.BoxPoints.OrderByDescending(a => a.X).FirstOrDefault().Y}");
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
                                arrTask.UpdateTaskItem("支付宝1", DateTime.Now.ToString());
                                _deviceService.CleanDeviceRunningApps(device.Id);
                                await Task.Delay(15000, cancellationToken);
                                continue;
                            }
                            else
                            {
                                int Max = 10000;
                                var m = Regex.Match(text.Text, @"(\d{1,2})/(\d{1,2})支");
                                if (m.Success) {
                                    Max = GetDynamicMax(m.Groups[2].Value.ToInt());
                                }
                                var r = new Random().Next(5000, Max); 
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

                            if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么") || a.Text == "立即关注"|| a.Text == "已关闭评论" || a.Text.Contains("更多直播") || a.Text.Contains("欢迎各位小伙伴来到直播间") ||a.Text == "粉丝团" ||a.Text == "分享直播间"))// || text.TextBlocks.Any(a => a.Text == "关注")
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
                            else if ((DateTime.Now - device.LastClickTime).TotalSeconds > 60)
                            {
                                if (_deviceService.CurAdbClient.GetAppStatus(_device, device.PackageName) != AppStatus.Foreground)
                                {
                                    await StartAppAsync(device, device.PackageName);
                                    await Task.Delay(3000);
                                }
                                else await ClickAsync(device, _device, null, 2000, "返回-停留页面超过60秒");
                                continue;
                            }
                            else if ((DateTime.Now - device.LastClickTime).TotalSeconds > 30)
                            {
                                if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么")))
                                {
                                    await ClickAsync(device, _device, null, 2000, "返回-停留页面超过30秒");
                                    continue;
                                }
                            }
                            if (device.CurPage != AppPageEnum.MoneyHome)
                            {
                                //_deviceService.UpdateTaskData();
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