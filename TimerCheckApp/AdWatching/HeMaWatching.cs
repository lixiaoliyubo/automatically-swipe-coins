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
/// 抖音广告观看自动化实现
/// </summary>
public class HeMaWatching : IAdWatching
{
    private readonly ILogService _logService;
    private readonly IDeviceService _deviceService;
    private readonly ICacheService _cacheService;
    private readonly ISqlSugarService _sqlSugarService;
    private bool IsOperate = false;
    public ConcurrentDictionary<string, string> arrTask { get; set; } = new()
    {
        ["任务"] = string.Empty,
        ["开宝箱"] = string.Empty,

    };
    private ConcurrentDictionary<string, (TimeSpan start, TimeSpan end)> mealTimes = new()
    {
        ["早餐"] = (TimeSpan.Parse("07:00"), TimeSpan.Parse("09:00")),
        ["午餐"] = (TimeSpan.Parse("11:00"), TimeSpan.Parse("13:00")),
        ["晚餐"] = (TimeSpan.Parse("17:00"), TimeSpan.Parse("19:00")),
        ["夜宵"] = (TimeSpan.Parse("22:00"), TimeSpan.Parse("23:59"))
    };


    public HeMaWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
    {
        _logService = logService;
        _deviceService = deviceService;
        _cacheService = cacheService;
        _sqlSugarService = sqlSugarService;
    }

    public async Task AutomateAdWatching(DeviceModel device, CancellationToken cancellationToken)
    {
        var _device = new DeviceData { Serial = device.Id };
        foreach (var key in arrTask.Keys)
        {
            if (key != "切换账号")
            {
                arrTask[key] = string.Empty;
            }
        }
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
                            arrTask["逛街赚钱"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
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
                                    var array = arrTask[Last].GetArray().ToList();
                                    array.RemoveAt(array.Count - 1);
                                    arrTask[Last] = string.Join("|", array);
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
                            else if (!text.TextBlocks.Any(a => a.Text == "去领取") && text.TextBlocks.Any(a => a.Text == "开宝箱") && (DateTime.Now - arrTask["开宝箱"].ToDateTime(DateTime.Now.AddMinutes(-11))).TotalMinutes > 10)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.OpenTreasure;
                                    PagePosChange.Add("开宝箱");
                                    arrTask["开宝箱"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
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
                                    string[] values = arrTask["任务"].GetArray();

                                    var recentCount = values.Count(x => (DateTime.Now - DateTime.ParseExact(x, "yyyy-MM-dd HH:mm:ss", null)).TotalMinutes <= 5);
                                    if (recentCount >= 15)
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("视频"));
                                        if (BoxPoint != null)
                                        {
                                            arrTask["任务"] = string.Format("{0}|{1}", string.Join("|", values), $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
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
                                    await StartAppAsync(device, _device, 3000);
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