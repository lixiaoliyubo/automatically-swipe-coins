using TimerCheckApp.enums;
using TimerCheckApp.Extensions;
using PaddleOCRSharp;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TimerCheckApp.Services.Interfaces;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.DeviceCommands;
using TimerCheckApp.Models;

namespace TimerCheckApp.AdWatching;

/// <summary>
/// 盒马广告观看自动化实现
/// </summary>
public class HeMaWatching : AdWatchingBase
{

    public HeMaWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
        : base(logService, deviceService, cacheService, sqlSugarService)
    {        
        // 初始化任务列表
        arrTask = new List<TaskItem>
        {
            new TaskItem("任务"),
            new TaskItem("开宝箱")
        };
        
        // 初始化用餐时间
        mealTimes = new ConcurrentDictionary<string, (TimeSpan start, TimeSpan end)>
        {
            ["早餐"] = (TimeSpan.Parse("07:00"), TimeSpan.Parse("09:00")),
            ["午餐"] = (TimeSpan.Parse("11:00"), TimeSpan.Parse("13:00")),
            ["晚餐"] = (TimeSpan.Parse("17:00"), TimeSpan.Parse("19:00")),
            ["夜宵"] = (TimeSpan.Parse("22:00"), TimeSpan.Parse("23:59"))
        };
    }

    public override async Task AutomateAdWatching(DeviceModel device, CancellationToken cancellationToken)
    {
        var _device = new DeviceData { Serial = device.Id };
        arrTask.ResetTaskStatus();
        int StopTimeSpan = 0;
        int MaxY = 0;
        OCRPoint? closePos = null;
        try
        {
            List<string> PagePosChange = new List<string>();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    OCRResult? text = await _deviceService.GetOCRText(device);//免20分钟广告  上滑继续观看短剧   VIP免广告看全集
                    if (text?.Text?.Length > 1)
                    {
                        #region 识别本文逻辑

                        if (closePos == null && text.TextBlocks.Any(a => a.Text == "跳过"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "跳过");
                            if (BoxPoint != null)
                            {
                                var pos = BoxPoint.BoxPoints.OrderByDescending(a => a.X).Skip(0).Take(1).Select(a => new OCRPoint(a.X, a.Y)).ToList();
                                closePos = pos[0];
                            }
                        }

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
                        else if (text.TextBlocks.Any(a => a.Text == "关闭应用") && text.TextBlocks.Any(a => a.Text == "等待"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "等待");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "禁止") && text.TextBlocks.Any(a => a.Text == "允许"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "禁止");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "仅此一次") && text.TextBlocks.Any(a => a.Text == "始终"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "仅此一次");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "立即下载") && text.TextBlocks.Any(a => a.Text == "功能") && text.TextBlocks.Any(a => a.Text == "隐私") && closePos != null)
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "仅此一次");
                            if (BoxPoint != null)
                            {
                                closePos.X = closePos.X + 50;
                                await ClickAsync(device, _device, new List<OCRPoint>() { closePos }, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "稍后安装") && text.TextBlocks.Any(a => a.Text == "立即安装"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "稍后安装");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "禁止") && text.TextBlocks.Any(a => a.Text == "允许") && text.TextBlocks.Any(a => a.Text.Contains("安装应用")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "禁止");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "拒绝") && text.TextBlocks.Any(a => a.Text.EndsWith("正在尝试安装应用")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "拒绝");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "立即下载") && text.TextBlocks.Any(a => a.Text == "应用详情") && text.TextBlocks.Any(a => a.Text == "X" || a.Text == "x" || a.Text == "×"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x" || a.Text == "×");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"立即签到\S+(\d+)金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"立即签到\S+(\d+)金币"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"再获取(\d+)分钟免广")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"再获取(\d+)分钟免广"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "继续观看") && text.TextBlocks.Any(a => a.Text == "放弃奖励"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "继续观看") && text.TextBlocks.Any(a => a.Text == "残忍离开"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "留下看看") && text.TextBlocks.Any(a => a.Text == "残忍离开"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "留下看看");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "点击或滑动免看本次广告"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "点击或滑动免看本次广告");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.EndsWith("上滑继续观看短剧")))
                        {
                            // 执行小幅度上滑
                            await SwipeAsync(device, _device, 4000, "上滑继续观看短剧", 500, 1500, 500, 500, 200);
                        }
                        else if (!Regex.IsMatch(text.TextBlocks[0].Text, @"^\d+$") && text.TextBlocks.Any(a => a.Text == "跳过") && text.TextBlocks.Any(a => a.Text.StartsWith("立即获取")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "跳过");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "立即下载") && text.TextBlocks.Any(a => a.Text == "残忍离开"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "残忍离开");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "添加到主屏幕") && text.TextBlocks.Any(a => a.Text == "取消") && text.TextBlocks.Any(a => a.Text == "添加"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "抓住奖励机会") && text.TextBlocks.Any(a => a.Text == "放弃奖励离开"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "抓住奖励机会");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "恭喜获得奖励") && text.TextBlocks.Any(a => a.Text == "下载应用"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "下载应用");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("恭喜获得奖励")) && !text.TextBlocks.Any(a => a.Text == "立即查看"))
                        {

                            if (text.TextBlocks.Any(a => a.Text == "X" || a.Text == "x" || a.Text == "×"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x" || a.Text == "×");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                }
                            }
                            else
                            {

                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("恭喜获得奖励"));
                                if (BoxPoint != null)
                                {
                                    var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(pos.X + 115, pos.Y) }, 3000, BoxPoint.Text);
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "继续观看") && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"再看(\d+)秒可得奖励")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (Regex.IsMatch(text.Text, @"去看(\d+)秒可直接拿奖励") && text.TextBlocks.Any(a => a.Text == "立即下载"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即下载");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "查看福利") && text.TextBlocks.Any(a => a.Text == "继续观看"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "放弃福利") && text.TextBlocks.Any(a => a.Text == "立即领取"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "放弃福利");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 1000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "返回继续体验") && Regex.IsMatch(text.Text, @"继续看(\d+)秒可直接拿奖励"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "返回继续体验");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "立即下载领取") && Regex.IsMatch(text.Text, @"恭喜获得(\d+)元红包"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即下载领取");
                            if (BoxPoint != null)
                            {
                                var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();

                                closePos = new OCRPoint(pos.X, pos.Y + 80);
                                await ClickAsync(device, _device, new List<OCRPoint>() { closePos }, 2000, "推测关闭");
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "继续观看") && text.TextBlocks.Any(a => a.Text == "坚持退出"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看小视频最高再领(\d+)金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看小视频最高再领(\d+)金币"));
                            if (BoxPoint != null)
                            {
                                device.CurPage = AppPageEnum.Ad;
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 8000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("我要直接拿奖励")))
                        {
                            int second = 9;
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("继续播放视频内容"));
                            if (BoxPoint != null)
                            {
                                var m = Regex.Match(text.Text, @"去看(\d)秒");
                                if (m.Success)
                                {
                                    second = m.Groups[1].Value.ToInt();
                                }
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, second * 1000, BoxPoint.Text);
                                continue;
                            }
                            BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("我要直接拿奖励"));
                            if (BoxPoint != null)
                            {

                                await ClickAsync(device, _device, BoxPoint.BoxPoints, second * 1000, BoxPoint.Text);
                                continue;
                            }

                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"继续看(\d+)秒可直接拿奖励")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"继续看(\d+)秒可直接拿奖励"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"再逛(\d+)秒后可领奖")))
                        {
                            device.CurPage = AppPageEnum.GuangJie;
                            PagePosChange.Add("逛街赚钱");
                            arrTask.UpdateTaskItem("逛街赚钱", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                            // 执行小幅度上滑
                            await SwipeAsync(device, _device, 4000, "逛街得金币", 500, 1500, 500, 500, 200);
                            // 执行小幅度下滑
                            await SwipeAsync(device, _device, 4000, "逛街得金币", 500, 500, 500, 1500, 200);
                            continue;
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"(\d+)秒") && a.BoxPoints.Any(x => x.Y > 1000 && x.X > 600)))
                        {
                            // 执行小幅度上滑
                            await SwipeAsync(device, _device, 4000, "逛街得金币", 500, 1500, 500, 500, 200);
                            // 执行小幅度下滑
                            await SwipeAsync(device, _device, 4000, "逛街得金币", 500, 500, 500, 1500, 200);
                            continue;
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("奖励已领取")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("奖励已领取"));
                            if (BoxPoint != null)
                            {
                                var pos = BoxPoint.BoxPoints.OrderByDescending(a => a.X).Skip(0).Take(1).Select(a => new OCRPoint(a.X, a.Y)).ToList();

                                await ClickAsync(device, _device, pos, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("领取成功")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("领取成功"));
                            if (BoxPoint != null)
                            {
                                var pos = BoxPoint.BoxPoints.OrderByDescending(a => a.X).OrderByDescending(a => a.Y).Skip(0).Take(1).Select(a => new OCRPoint(a.X, a.Y)).ToList();

                                await ClickAsync(device, _device, pos, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "反馈" || a.Text == "加入购物车"))
                        {
                            if (text.TextBlocks.Any(a => a.Text == "X" || a.Text == "x" || a.Text == "×"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x" || a.Text == "×");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 1000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else
                            {

                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("反馈"));
                                if (BoxPoint != null)
                                {
                                    var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(pos.X + 115, pos.Y) }, 3000, "推测关闭");
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "关闭") && !text.TextBlocks.Any(a => a.Text.StartsWith("奖励发放还有")) && (text.Text.Contains("滑动或点击下载应用") || text.TextBlocks.Any(a => a.Text == "点击安装")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "关闭");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }



                        if (text.TextBlocks.Any(a => a.Text == "福利中心"))
                        {
                            if (MaxY == 0)
                            {
                                MaxY = text.TextBlocks.Last().BoxPoints.CalculateTextBlockCenter().Y;
                            }
                            var currentTime = DateTime.Now.TimeOfDay;
                            var key = mealTimes.FirstOrDefault(a => currentTime >= a.Value.start && currentTime < a.Value.end).Key;
                            #region 首页
                            if (device.CurPage != AppPageEnum.MoneyHome)
                            {
                                device.CurPage = AppPageEnum.MoneyHome;
                                var Last = PagePosChange.LastOrDefault();
                                if (PagePosChange.Count > 0 && Last != null)
                                {
                                    _logService.LogWarning("页面没有跳转，清空重试", device.Id);
                                    var array = arrTask.GetTaskItemValue(Last).GetArray().ToList();
                                    if (array.Count > 0)
                                    {
                                        array.RemoveAt(array.Count - 1);
                                    }
                                    arrTask.UpdateTaskItem(Last, string.Join("|", array));
                                    PagePosChange.Clear();
                                }
                            }



                            if (text.TextBlocks.Any(a => a.Text == "双倍领取"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "双倍领取");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "去观看"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去观看");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (!text.TextBlocks.Any(a => a.Text == "去领取") && text.TextBlocks.Any(a => a.Text == "开宝箱") && (DateTime.Now - arrTask.GetTaskItemValue("开宝箱").ToDateTime(DateTime.Now.AddMinutes(-11))).TotalMinutes > 10)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.OpenTreasure;
                                    PagePosChange.Add("开宝箱");
                                    arrTask.UpdateTaskItem("开宝箱", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "去领取"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去领取");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #region 看指定视频得金币
                            #endregion

                            if (device.CurPage != AppPageEnum.MoneyHome)
                            {
                                //_deviceService.UpdateTaskData();
                            }
                            #endregion

                            StopTimeSpan++;
                        }
                        else
                        {
                            StopTimeSpan = 0;
                            #region 非首页
                            if (text.TextBlocks.Any(a => a.Text == "腾讯元宝") && text.TextBlocks.Any(a => a.Text == "X" || a.Text == "x" || a.Text == "×"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x" || a.Text == "×");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "点击立即下载") && text.TextBlocks.Any(a => a.Text == "反馈"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "点击立即下载");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么") || a.Text == "关注" || a.Text.Contains("本场点赞")))
                            {
                                await ClickAsync(device, _device, null, 3000, "直播间返回");
                            }
                            else if (closePos == null && text.TextBlocks.Any(a => a.Text == "反馈" && a.BoxPoints.OrderByDescending(x => x.X).FirstOrDefault()?.X > 700))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "反馈");
                                if (BoxPoint != null)
                                {
                                    var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                    if (closePos == null)
                                    {
                                        closePos = new OCRPoint(pos.X + 180, pos.Y);
                                    }
                                    await ClickAsync(device, _device, new List<OCRPoint>() { closePos }, 2000, "推测关闭");
                                }
                            }
                            else if (closePos != null && (DateTime.Now - device.LastClickTime).TotalSeconds > 20)
                            {
                                await ClickAsync(device, _device, new List<OCRPoint>() { closePos }, 3000, "关闭");
                            }
                            #endregion
                            //当上面没有操作时就执行下面
                            if (!IsOperate)
                            {
                                if (text.TextBlocks.Any(a => a.Text.Trim() == "首页") && text.TextBlocks.Any(a => a.Text.Trim() == "福利") && text.TextBlocks.Any(a => a.Text.Trim() == "我的"))
                                {
                                    string[] values = arrTask.GetTaskItemValue("任务").GetArray();

                                    var recentCount = values.Count(x => (DateTime.Now - DateTime.ParseExact(x, "yyyy-MM-dd HH:mm:ss", null)).TotalMinutes <= 5);
                                    if (recentCount >= 15)
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("视频"));
                                        if (BoxPoint != null)
                                        {
                                            arrTask.UpdateTaskItem("任务", string.Format("{0}|{1}", string.Join("|", values), $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}"));
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text.StartsWith("看完视频最高")))
                                    {
                                        await Task.Delay(30000);
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "视频") || text.TextBlocks.Any(a => a.Text == "视频") || text.TextBlocks.Any(a => a.Text == "首页"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "视频");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else
                                    {
                                        await SwipeAsync(device, _device, new Random().Next(10000, 25000), "赚钱首页刷视频", 540, 1500, 540, 1000, 200);
                                    }
                                }
                                #region 特殊页面
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("立即下载")) && text.TextBlocks.Any(a => a.Text == "侵权投诉"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-立即下载");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("综合")) && text.TextBlocks.Any(a => a.Text == "销量"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-立即下载");
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "已发放"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-立即下载");
                                }
                                else if (_deviceService.CurAdbClient.GetAppStatus(_device, device.PackageName) != AdvancedSharpAdbClient.Models.AppStatus.Foreground)
                                {
                                    await StartAppAsync(device, device.PackageName);
                                    await Task.Delay(3000);
                                }
                                #endregion
                                #region 页面停留超时
                                else if ((DateTime.Now - device.LastClickTime).TotalSeconds > 60)
                                {
                                    await SwipeAsync(device, _device, 1000, "返回-停留页面超过60秒", 100, 1000, 500, 1000, 200);
                                    await ClickAsync(device, _device, null, 2000, "返回-停留页面超过60秒");
                                }
                                #endregion
                            }

                        }

                        #endregion
                        IsOperate = false;
                    }
                    else
                    {
                        _logService.LogWarning($"OCR失败结果为空", device.Id);
                        await Task.Delay(3000);
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"自动化过程出错: {ex.Message}", device.Id);
                    await Task.Delay(3000);
                }
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