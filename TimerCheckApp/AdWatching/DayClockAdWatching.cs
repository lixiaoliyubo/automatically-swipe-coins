using TimerCheckApp.enums;
using TimerCheckApp.Extensions;
using PaddleOCRSharp;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TimerCheckApp.Services.Interfaces;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.DeviceCommands;
using System.Diagnostics;
using OpenCvSharp;
using System.IO;
using TimerCheckApp.Models;
using DynamicData;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;

namespace TimerCheckApp.AdWatching;

/// <summary>
/// 抖音广告观看自动化实现
/// </summary>
public class DayClockAdWatching : AdWatchingBase
{
    public DayClockAdWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
        : base(logService, deviceService, cacheService, sqlSugarService)
    {
        // 初始化特定于抖音的任务列表
        arrTask = new()
        {
        };

        // 初始化特定于抖音的用餐时间
        mealTimes = new()
        {
        };
    }

    /// <summary>
    /// 自动化广告观看主方法
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <param name="cancellationToken">取消令牌</param>
    public override async Task AutomateAdWatching(DeviceModel device, CancellationToken cancellationToken)
    {
        var _device = new DeviceData { Serial = device.Id };
        // 重置任务状态
        arrTask.ResetTaskStatus();

        // 记录开始时间用于性能分析
        var stopwatch = Stopwatch.StartNew();
        int StopTimeSpan = 0;
        int AdTime = 0;
        bool isCompele = false; //是否强制结束
        // 重复次数计数
        int repeatIndex = 0;
        // 计算间隔延迟毫秒数
        int intervalDelayMs = device.IntervalMinutes * 60 * 1000 + device.IntervalSeconds * 1000;
        if (intervalDelayMs <= 0) intervalDelayMs = 1000; // 默认1秒
        try
        {
            List<string> PagePosChange = new List<string>();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 检查重复次数限制
                    if (device.RepeatCount > 0 && repeatIndex >= device.RepeatCount)
                    {
                        _logService.LogInfo($"达到重复次数限制({device.RepeatCount})，退出自动化", device.Id);
                        break;
                    }
                    repeatIndex++;
                    if (device.CurPage == AppPageEnum.SlideUp)
                    {
                        var r = GetRandomPauseMs(device);
                        await SwipeAsync(device, _device, SwipeDirection.Up, intervalDelayMs, "上滑继续看刷视频", 200);
                        await Task.Delay(r);
                        continue;
                    }
                    OCRResult? text = await _deviceService.GetOCRText(device);
                    if (text?.Text?.Length > 1)
                    {
                        #region 识别本文逻辑

                        #region 系统层面

                        if (text.TextBlocks.Any(a => a.Text == "网络异常") && text.TextBlocks.Any(a => a.Text == "刷新"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "刷新");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "服务器开小差了") && text.TextBlocks.Any(a => a.Text == "重试"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "重试");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "立即升级") && text.TextBlocks.Any(a => a.Text == "以后再说"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "以后再说");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "关闭应用") && text.TextBlocks.Any(a => a.Text == "等待"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "等待");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "添加到主屏幕") && text.TextBlocks.Any(a => a.Text == "取消"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("访问您的日历")) && text.TextBlocks.Any(a => a.Text == "允许"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "允许");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("获取此设备的位置")) && text.TextBlocks.Any(a => a.Text == "仅使用期间允许"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "仅使用期间允许");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        #endregion

                        #region  抖音
                        else if (text.TextBlocks.Any(a => a.Text == "打卡领5000元现金") && text.TextBlocks.Any(a => a.Text == "去打卡"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去打卡");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"做(\d+)个任务完成打卡") || a.Text == "做任务完成打卡") && !text.TextBlocks.Any(a => a.Text.Contains("继续加油吧")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"做(\d+)个任务完成打卡") || a.Text == "做任务完成打卡");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看(\d+)个指定视频")) && text.TextBlocks.Any(a => a.Text == "打卡日程"))
                        {
                            var row = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看(\d+)个指定视频"));
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去观看");
                            if (BoxPoint != null && (BoxPoint.BoxPoints[0].Y - row?.BoxPoints[0].Y) < 50)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                            else {
                                await Task.Delay(10000);
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.EndsWith("广告完成打卡") || a.Text == "看广告完成打卡"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.EndsWith("广告完成打卡") || a.Text == "看广告完成打卡");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 35000, BoxPoint.Text);
                                await ClickBackButtonAsync(device, _device, 3000);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("继续加油吧")) && text.TextBlocks.Any(a => a.Text.EndsWith("个指定视频")) && !text.TextBlocks.Any(a => a.Text == "已完成"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.EndsWith("个指定视频"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 35000, BoxPoint.Text);
                                await ClickBackButtonAsync(device, _device, 3000);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("继续加油吧")) && text.TextBlocks.Any(a => a.Text.EndsWith("分钟视频")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.EndsWith("分钟视频"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                Match a = Regex.Match(BoxPoint.Text, @"(\d+)分钟视频");
                                DateTime dateTime = DateTime.Now.AddMinutes(a.Groups[1].Value.ToInt());
                                while (true)
                                {
                                    if (dateTime < DateTime.Now) break;

                                    var r = new Random().Next(3000, 8000);
                                    await SwipeAsync(device, _device, SwipeDirection.Up, r, "上滑继续看刷视频", 200);
                                }
                                await ClickBackButtonAsync(device, _device, 3000);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("完成任务")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("完成任务"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 35000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("今日打卡已完成")))
                        {
                            await ClickBackButtonAsync(device, _device, 3000);
                            continue;
                        }
                        #endregion

                        #region  快手
                        else if (text.TextBlocks.Any(a => a.Text.Contains("百日心愿之旅")) && text.TextBlocks.Any(a => a.Text == "今日已打卡"))
                        {
                            await ClickBackButtonAsync(device, _device, 3000);
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("快手送你优惠")) && text.TextBlocks.Any(a => a.Text == "立即使用"))
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
                                    continue;
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "开心收下"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开心收下");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }

                        else if (text.TextBlocks.Any(a => a.Text == "向前冲") && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"再冲(\d+)步完成今日打卡")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "向前冲");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("观看广告")) && text.TextBlocks.Any(a => a.Text == "去完成"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去完成");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 40000, BoxPoint.Text);
                                await ClickBackButtonAsync(device, _device, 3000);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("大家还在搜")) && text.TextBlocks.Any(a => a.Text == "退出"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "退出");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("开心收下")) && text.TextBlocks.Any(a => a.Text == "继续冲"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续冲");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("观看内容")) && text.TextBlocks.Any(a => a.Text == "去完成"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去完成");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 35000, BoxPoint.Text);
                                await ClickBackButtonAsync(device, _device, 3000);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("观看视频")) && text.TextBlocks.Any(a => a.Text == "去完成"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去完成");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                Match a = Regex.Match(text.Text, @"/(\d+)分钟");
                                DateTime dateTime = DateTime.Now.AddMinutes(a.Groups[1].Value.ToInt());
                                while (true)
                                {
                                    if (dateTime < DateTime.Now) break;

                                    var r = new Random().Next(3000, 8000);
                                    await SwipeAsync(device, _device, SwipeDirection.Up, r, "上滑继续看刷视频", 200);
                                }
                                await ClickBackButtonAsync(device, _device, 3000);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "今日打卡成功") && text.TextBlocks.Any(a => a.Text == "我知道了"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "我知道了");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "已成功领取奖励" || a.Text == "已成功领取奖励 ×" || a.Text == "已成功领取奖励×"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "已成功领取奖励" || a.Text == "已成功领取奖励 ×" || a.Text == "已成功领取奖励×");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "今日广告任务") && text.TextBlocks.Any(a => a.Text == "去看广告"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去看广告");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        #endregion




                        #endregion

                        IsOperate = false;
                    }
                    else
                    {
                        _logService.LogInfo($"OCR失败结果为空", device.Id);
                        await Task.Delay(3000, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogInfo($"自动化过程出错: {ex.Message}", device.Id);
                    await Task.Delay(3000, cancellationToken);
                }
                // 等待1秒
                await Task.Delay(1000, cancellationToken).ContinueWith(t =>
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
            _logService.LogInfo($"自动化过程出错: {ex.Message}", device.Id);
        }
    }

    /// <summary>
    /// 获取随机暂停毫秒数
    /// </summary>
    private static int GetRandomPauseMs(DeviceModel device)
    {
        int minMs = device.RandomPauseMinSeconds > 0 || device.RandomPauseMaxSeconds > 0
            ? device.RandomPauseMinSeconds * 1000 : 3000;
        int maxMs = device.RandomPauseMinSeconds > 0 || device.RandomPauseMaxSeconds > 0
            ? device.RandomPauseMaxSeconds * 1000 : 8000;
        if (maxMs <= minMs) maxMs = minMs + 1000;
        return new Random().Next(minMs, maxMs);
    }

}