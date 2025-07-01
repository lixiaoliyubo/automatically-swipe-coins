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

namespace TimerCheckApp.AdWatching;

/// <summary>
/// 抖音广告观看自动化实现
/// </summary>
public class DouyinAdWatching : IAdWatching
{
    private readonly ILogService _logService;
    private readonly IDeviceService _deviceService;
    private readonly ICacheService _cacheService;
    private readonly ISqlSugarService _sqlSugarService;
    /// <summary>
    /// 当前是否有操作
    /// </summary>
    private bool IsOperate = false;


    public List<TaskItem> arrTask { get; set; } = new()
    {
        new TaskItem("吃饭打卡赚金币"),
        new TaskItem("天天领金币"),
        new TaskItem("预约领金币"),
        new TaskItem("一键领音符"),
        new TaskItem("一键领金币"),
        new TaskItem("看广告赚金币"),
        new TaskItem("看指定视频得金币"),
        new TaskItem("限时任务赚音符"),
        new TaskItem("赚钱首页"),
        new TaskItem("看直播开宝箱"),
        new TaskItem("做任务领奖励"),
        new TaskItem("开宝箱得金币"),
        new TaskItem("逛街赚钱"),
        new TaskItem("签到"),
        new TaskItem("早安小岛"),
        new TaskItem("听书赚金币"),
        new TaskItem("上下滑动")
    };
    ConcurrentDictionary<string, string> IAdWatching.arrTask { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    private readonly ConcurrentDictionary<string, (TimeSpan start, TimeSpan end)> mealTimes = new()
    {
        ["早餐"] = (TimeSpan.Parse("05:00"), TimeSpan.Parse("09:00")),
        ["午餐"] = (TimeSpan.Parse("11:00"), TimeSpan.Parse("14:00")),
        ["晚餐"] = (TimeSpan.Parse("17:00"), TimeSpan.Parse("20:00")),
        ["夜宵"] = (TimeSpan.Parse("21:00"), TimeSpan.Parse("23:59"))
    };


    public DouyinAdWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
    {
        _logService = logService;
        _deviceService = deviceService;
        _cacheService = cacheService;
        _sqlSugarService = sqlSugarService;
    }

    /// <summary>
    /// 自动化广告观看主方法
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AutomateAdWatching(DeviceModel device, CancellationToken cancellationToken)
    {
        var _device = new DeviceData { Serial = device.Id };
        // 重置任务状态
        arrTask.ResetTaskStatus();

        // 记录开始时间用于性能分析
        var stopwatch = Stopwatch.StartNew();
        int StopTimeSpan = 0;
        int AdTime = 0;
        bool isCompele = false; //是否强制结束
        try
        {
            List<string> PagePosChange = new List<string>();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (device.CurPage == AppPageEnum.SlideUp && DateTime.Now.Hour > 23 && DateTime.Now.Minute > 30)
                    {
                        await Task.Delay(5000);
                        continue;
                    }
                    OCRResult? text = await _deviceService.GetOCRText(device);
                    if (text?.Text?.Length > 1)
                    {
                        #region 识别本文逻辑

                        if (device.CurPage == AppPageEnum.Data && DateTime.Now.Hour > 23)
                        {
                            _logService.LogWarning($"准备刷短剧", device.Id);
                            await Task.Delay(10000);
                            continue;
                        }
                        else if (device.CurPage == AppPageEnum.Data && DateTime.Now.Hour < 23)
                        {
                            _deviceService.CleanDeviceRunningApps(device.Id);
                            await Task.Delay(10000);
                            device.CurPage = AppPageEnum.SlideUp;
                            _logService.LogWarning($"开始刷短剧", device.Id);
                            await Task.Delay(1000);
                            continue;
                        }
                        else if (device.CurPage == AppPageEnum.KanXiaoShuo)
                        {
                            // 处理小说页面的广告和奖励
                            await HandleNovelPageInteractions(device, _device, text);

                            await Task.Delay(1000);
                            continue;
                        }
                        else if (device.CurPage == AppPageEnum.SlideUp)
                        {
                            // 刷短剧
                            await HandleSlideUpPageInteractions(device, _device, text, PagePosChange);
                            await Task.Delay(2000);
                            continue;
                        }

                        #region 位置识别
                        if (text.TextBlocks.Any(a => a.Text == "天天预约领金币"))
                        {
                            device.CurPage = AppPageEnum.OneClickMoney;
                        }
                        else if (text.TextBlocks.Any(a => a.Text.EndsWith("的钱包")))
                        {
                            device.CurPage = AppPageEnum.DayMoney;
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "逛街得金币"))
                        {
                            device.CurPage = AppPageEnum.GuangJie;
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "早安小岛" || a.Text == "晚安小岛"))
                        {
                            device.CurPage = AppPageEnum.ZaoAnXiaoDao;
                        }
                        else if(text.TextBlocks.Any(a => a.Text == "已暂停") && text.TextBlocks.Any(a => a.Text == "金币" || a.Text == "已到账"))
                        {
                            device.CurPage = AppPageEnum.AppointVideo;
                        }
                        #endregion

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

                        #region  弹出层面
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("签到奖励已翻倍")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("签到奖励已翻倍"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("签到成功")) && text.TextBlocks.Any(a => a.Text == "好的"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "好的");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "打开签到提醒"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "打开签到提醒");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "当前无新视频"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x");
                            if (BoxPoint != null)
                            {
                                isCompele = true;
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("到底了")))
                        {
                            isCompele = true;
                            await ClickAsync(device, _device, null, 3000, "到底了~下滑浏览领取金币");
                            continue;
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "评价并收下金币"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("评价并收下金币"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "评价并关闭"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("评价并关闭"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("番茄免费小说")) && text.TextBlocks.Any(a => a.Text == "取消"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"立即签到([\s\S]*)(\d+)金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"立即签到([\s\S]*)(\d+)金币"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看广告签到领([\s\S]*)(\d+)")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看广告签到领([\s\S]*)(\d+)"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }

                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("看广告视频再赚")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看广告视频再赚"));
                            if (BoxPoint != null)
                            {
                                device.CurPage = AppPageEnum.Ad;
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看视频赚([\d\.]+)元")) && text.TextBlocks.Any(a => a.Text.Contains("老用户专享福利")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看视频赚([\d\.]+)元"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("立即签到")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("立即签到"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"(领取奖励|继续观看|继续互动|继续领金币)")) && text.TextBlocks.Any(a => a.Text.Contains("坚持退出")))
                        {
                            if (isCompele)
                            {
                                isCompele = false;
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("坚持退出"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else
                            {

                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"(领取奖励|继续观看|继续互动|继续领金币)"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "开心收下"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("开心收下"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("领取成功")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("领取成功"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("获得开宝箱奖励")) && text.TextBlocks.Any(a => a.Text.StartsWith("我知道了")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("我知道了"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("领今日奖励")) && text.TextBlocks.Any(a => a.Text.StartsWith("签到领")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("领今日奖励"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("每日签到") || a.Text.StartsWith("明日签到")) && text.TextBlocks.Any(a => a.Text.StartsWith("签到提醒")))
                        {
                            if (text.TextBlocks.Any(a => a.Text.StartsWith("立即签到")))
                            {

                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("立即签到"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else
                            {

                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "好的");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("签到成功")) && text.TextBlocks.Any(a => a.Text.StartsWith("好的")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("好的"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "去签到") && text.TextBlocks.Any(a => a.Text == "天天赚金币"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("去签到"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "今天签到") && text.TextBlocks.Any(a => a.Text == "签到领金币"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("签到领金币"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "明天继续领现金"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "明天继续领现金");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^签到\d+$")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"^签到\d+$"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "领金币") && !text.TextBlocks.Any(a => a.Text == "首页" || a.Text == "今日已领" || a.Text.EndsWith("的钱包")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领金币");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "开宝箱") && text.TextBlocks.Any(a => a.Text.Contains("做任务领奖励") || a.Text.Contains("做任务赚金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "立即领取") && !text.TextBlocks.Any(a => a.Text == "广告") && device.CurPage != AppPageEnum.GuangJie)
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即领取");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("s秒后进入下场直播") || a.Text == "直播已结束"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("X"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"(\d+)s秒后进入直播间")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.EndsWith("直播已结束")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("x"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }

                        if (text.TextBlocks.Any(a => a.Text == "抖音极速版") && text.TextBlocks.Any(a => a.Text == "快手极速版"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "抖音极速版");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => new string[] { "推荐", "赚钱", "首页", "我" }.Any(x => x == a.Text)) && text.TextBlocks.Any(a => new string[] { "可领取", "去领取", "去完成", "换现金" }.Contains(a.Text)))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "赚钱");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }

                        #endregion

                        if ((text.TextBlocks.Any(a => a.Text.StartsWith("金币收益")) && text.TextBlocks.Any(a => a.Text.Contains("现金收益") || a.Text == "详情")) || (text.TextBlocks.Any(a => a.Text.StartsWith("集音符") || a.Text.StartsWith("限时任务") || a.Text.StartsWith("日常任务")) && text.TextBlocks.Any(a => a.Text.Contains("已预约") || a.Text == "去领取")))
                        {
                            var currentTime = DateTime.Now.TimeOfDay;
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
                                else
                                {

                                    await SwipeAsync(device, _device, 4000, "滑动到看指定视频得金币", 100, 1200, 540, 1000, 200);
                                    continue;
                                }
                            }
                            if (AdTime == 0 && text.Text.Contains("完成一次广告任务"))
                            {
                                AdTime = Regex.Match(text.Text, @"每(\d+)分钟完成一次广告任务").Groups[1].Value.ToInt32();
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"点击领(\d+)")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"点击领(\d+)"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "看视频赚金币") && text.TextBlocks.Any(a => a.Text == "待领取"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "待领取");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    continue;
                                }
                            }

                            #region 天天领金币
                            if (device.CurPage == AppPageEnum.MoneyHome && arrTask.GetTaskEmptyOrNull("天天领金币"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("天天领金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.DayMoney;
                                    PagePosChange.Add("天天领金币");
                                    arrTask.UpdateTaskItem("天天领金币", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 一键领金币
                            if (device.CurPage == AppPageEnum.MoneyHome && arrTask.GetTaskEmptyOrNull("一键领金币"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("一键领金币") || a.Text.EndsWith("一键领金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.OneClickMoney;
                                    PagePosChange.Add("一键领金币");
                                    arrTask.UpdateTaskItem("一键领金币", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;

                                }
                            }
                            #endregion

                            #region 开宝箱得金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text == "开宝箱得金币" || a.Text.StartsWith("点击领金币") || Regex.IsMatch(a.Text, @"点击领(\d+)")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱得金币" || a.Text.StartsWith("点击领金币") || Regex.IsMatch(a.Text, @"点击领(\d+)"));
                                if (BoxPoint != null)
                                {
                                    PagePosChange.Add(BoxPoint.Text);
                                    device.CurPage = AppPageEnum.OpenTreasure;
                                    arrTask.UpdateTaskItem("开宝箱得金币", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 逛街得金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"浏览低价商品(\d+)秒即得")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("逛街赚钱") || a.Text.StartsWith("逛街赚音符") || a.Text.StartsWith("逛街赚金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.GuangJie;
                                    PagePosChange.Add("逛街赚钱");
                                    arrTask.UpdateTaskItem("逛街赚钱", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 吃饭打卡赚金币
                            //早餐：05:00-09:00、午餐：11:00-14:00 、晚餐：17:00-20:00 、夜宵：21:00-24:00、
                            var keyValue = mealTimes.FirstOrDefault(a => currentTime >= a.Value.start && currentTime < a.Value.end);
                            if (device.CurPage == AppPageEnum.MoneyHome && !arrTask.GetTaskArrayContains("吃饭打卡赚金币", keyValue.Key) && text.TextBlocks.Any(a => a.Text.Contains("吃饭打卡赚金币")))
                            {
                                var array = arrTask.GetTaskItemValue("吃饭打卡赚金币").GetArray().ToList();
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("吃饭打卡赚金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Dining;
                                    array.Add(keyValue.Key);
                                    arrTask.UpdateTaskItem("吃饭打卡赚金币", string.Join("|", array));
                                    PagePosChange.Add("吃饭打卡赚金币");
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 看广告赚金币
                            if (device.CurPage == AppPageEnum.MoneyHome && AdTime > 0 && text.TextBlocks.Any(a => a.Text.StartsWith("看广告赚金币")) && (DateTime.Now - arrTask.GetTaskItemValue("看广告赚金币").ToDateTime(DateTime.Now.AddMinutes(-25))).TotalMinutes >= AdTime)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看广告赚金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.ViewAdMoney;
                                    PagePosChange.Add("看广告赚金币");
                                    arrTask.UpdateTaskItem("看广告赚金币", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                }
                            }
                            #endregion

                            #region 早安小岛
                            if (device.CurPage == AppPageEnum.MoneyHome && arrTask.GetTaskEmptyOrNull("早安小岛"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("早安小岛"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.ZaoAnXiaoDao;
                                    PagePosChange.Add("早安小岛");
                                    arrTask.UpdateTaskItem("早安小岛", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;

                                }
                            }
                            #endregion

                            #region 看指定视频得金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"(看|刷)指定视频(得|赚)(金币|音符)")) && (DateTime.Now - arrTask.GetTaskItemValue("看指定视频得金币").GetArray().ToDateTime(DateTime.Now.AddMinutes(-22))).TotalMinutes > 20)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"(看|刷)指定视频(得|赚)(金币|音符)"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.AppointVideo;
                                    PagePosChange.Add("看指定视频得金币");
                                    arrTask.UpdateTaskItem("看指定视频得金币", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("免费送红包最高得")))
                            {
                                await ClickAsync(device, _device, null, 3000, "返回-免费送红包最高得");
                                continue;
                            }
                            else if (!IsOperate && (DateTime.Now - device.LastClickTime).TotalSeconds > 10)
                            {
                                var taskSlide = arrTask.GetTaskItemValue("上下滑动");
                                if (taskSlide.ToInt32() > 1500)
                                {
                                    arrTask.UpdateTaskItem("上下滑动", $"{(taskSlide.ToInt32() - 100)}");
                                    await SwipeAsync(device, _device, 3000, "在首页停留时间超过30秒自动下滑", 500, 900, 500, 1000, 200);
                                }
                                else
                                {
                                    arrTask.UpdateTaskItem("上下滑动", $"{(taskSlide.ToInt32() + 100)}");
                                    await SwipeAsync(device, _device, 3000, "在首页停留时间超过30秒自动上滑", 500, 1000, 500, 900, 200);
                                }
                                continue;
                            }
                            if (device.CurPage != AppPageEnum.MoneyHome)
                            {
                                ////_deviceService.UpdateTaskData();
                            }
                            #endregion

                            StopTimeSpan++;
                        }
                        else
                        {
                            StopTimeSpan = 0;

                            #region 非首页
                            if (device.CurPage == AppPageEnum.GuangJie)
                            {
                                #region 逛街赚钱、逛街得金币
                                if (text.TextBlocks.Any(a => a.Text == "逛街得金币"))
                                {
                                    PagePosChange.Clear();
                                    if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"浏览(\d+)秒可领")))
                                    {
                                        // 执行小幅度上滑
                                        await SwipeAsync(device, _device, 4000, "逛街得金币", 500, 1500, 500, 500, 200);
                                        // 执行小幅度下滑
                                        await SwipeAsync(device, _device, 2000, "逛街得金币", 500, 500, 500, 1500, 200);
                                        continue;
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "明日浏览可领取奖励" || a.Text.StartsWith("恭喜获得")))
                                    {
                                        await ClickAsync(device, _device, null, 3000, "返回-逛街得金币恭喜获得");
                                        continue;
                                    }
                                }
                                #endregion
                            }
                            else if (device.CurPage == AppPageEnum.AppointVideo)
                            {
                                PagePosChange.Clear();
                                #region 看指定视频得金币
                                if (text.TextBlocks.Any(a => a.Text == "已暂停") && text.TextBlocks.Any(a => a.Text == "金币") && !text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"观看([\d\.]+)秒")))
                                {
                                    //if (text.TextBlocks.Any(a => a.Text == "广告"))
                                    //{
                                    //    var BoxPoint = text.TextBlocks.FirstOrDefault(A => A.Text == "广告");
                                    //    if (BoxPoint != null)
                                    //    {
                                    //        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    //        await ClickAsync(device, _device, null, 5000, "返回-停留页面超过60秒");
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //}
                                    await SwipeAsync(device, _device, 2000, "金币暂停", 500, 1500, 500, 500, 200);
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text.EndsWith("回任务页")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(A => A.Text.EndsWith("回任务页"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                /*
                                if (text.TextBlocks.Any(a => a.Text.StartsWith("有爱评论") || a.Text == "直播中") && text.TextBlocks.Any(a => a.Text == "金币"))
                                {
                                    _logService.LogInfo("检测到'看指定视频得金币'文本");
                                    if (text.TextBlocks.Any(a => a.Text.EndsWith("回任务页")))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(A => A.Text.EndsWith("回任务页"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        }
                                    }
                                    else if (
                                        (text.TextBlocks.Any(a => a.Text == "已暂停") && (text.TextBlocks.Any(a => a.Text == "已到账") || text.TextBlocks.Any(a => a.Text == "购买商品"))) ||
                                        (text.TextBlocks.Any(a => a.Text == "已暂停") && text.TextBlocks.Any(a => a.Text == "金币") && !text.TextBlocks.Any(a => a.Text.StartsWith("观看"))))
                                    {
                                        await SwipeAsync(device, _device, 3000, "暂停金币", 500, 1500, 500, 500, 200);
                                    }

                                }
                                else if (text.TextBlocks.Any(a => a.Text.EndsWith("回任务页")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(A => A.Text.EndsWith("回任务页"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    }
                                    else
                                    {
                                        await ClickAsync(device, _device, null, 3000, "返回-回任务页");
                                    }
                                }*/
                                #endregion
                            }
                            else if (device.CurPage == AppPageEnum.ViewAdMoney)
                            {
                                #region 看广告赚金币
                                if (text.TextBlocks.Any(a => a.Text == "反馈"))
                                {
                                    PagePosChange.Clear();
                                }
                                #endregion
                            }
                            else if (device.CurPage == AppPageEnum.ZaoAnXiaoDao)
                            {
                                #region 早安小岛
                                if (text.TextBlocks.Any(a => a.Text == "规则"))
                                {
                                    PagePosChange.Clear();
                                    string[] arr = new string[] { "我起床了", "我出门了", "记录此刻活动" };
                                    string arrZt = new string[] { "想聊天", "逛街买菜", "在吃饭", "在午休", "在摸鱼", "在上班" }.GetRandomElement();
                                    if (text.TextBlocks.Any(a => arr.Contains(a.Text)))
                                    {

                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => arr.Contains(a.Text));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            continue;

                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == arrZt))
                                    {

                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == arrZt);
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "发布动态");
                                            if (BoxPoint != null)
                                            {
                                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            }
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        await ClickAsync(device, _device, null, 3000, "返回-首页");
                                        continue;
                                    }
                                }
                                #endregion
                            }
                            else if (device.CurPage == AppPageEnum.Dining)
                            {
                                #region 吃饭打卡领金币
                                if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, "看(指定|广告)视频领(早餐|午餐|晚餐|夜宵)补贴")))
                                {
                                    PagePosChange.Clear();
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "看(指定|广告)视频领(早餐|午餐|晚餐|夜宵)补贴"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
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
                                else if (text.TextBlocks.Any(a => a.Text == "去领取"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去领取");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("再赚最高")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("再赚最高"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (!text.TextBlocks.Any(a => a.Text.StartsWith("任务升级")) && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, "领取(早餐|午餐|晚餐|夜宵)补贴")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "领取(早餐|午餐|晚餐|夜宵)补贴"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, "记得领(早餐|午餐|晚餐|夜宵)补贴")))
                                {
                                    PagePosChange.Clear();
                                    if (text.TextBlocks.Any(a => a.Text == "点击补领"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("点击补领"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        await ClickAsync(device, _device, null, 3000, "返回-吃饭打卡领金币");
                                        continue;
                                    }
                                }
                                #endregion
                            }
                            else if (device.CurPage == AppPageEnum.DayMoney)
                            {
                                #region 天天领金币/钱包
                                if (text.TextBlocks.Any(a => a.Text.EndsWith("的钱包")))
                                {
                                    PagePosChange.Clear();
                                    if (text.TextBlocks.Any(a => a.Text == "领取今日奖励") && text.TextBlocks.Any(a => a.Text == "领取"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领取");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            if (text.TextBlocks.Any(a => a.Text == "抖音支付积分"))
                                            {
                                                BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("抖音支付积分"));
                                                if (BoxPoint != null)
                                                {
                                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                                }
                                            }
                                            continue;
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "X"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 4000, BoxPoint.Text);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        await ClickAsync(device, _device, null, 3000, "返回-立即签到X");
                                        continue;
                                    }
                                }
                                #endregion
                            }
                            else if (device.CurPage == AppPageEnum.OpenTreasure)
                            {
                                #region 开宝箱得金币
                                if (text.TextBlocks.Any(a => a.Text == "开宝箱奖励"))
                                {
                                    PagePosChange.Clear();
                                }
                                #endregion
                            }
                            else if (device.CurPage == AppPageEnum.OneClickMoney)
                            {
                                #region 一键领金币
                                if (text.TextBlocks.Any(a => a.Text == "领全币") || text.TextBlocks.Any(a => a.Text == "一键领取"))
                                {
                                    PagePosChange.Clear();
                                }
                                if (text.TextBlocks.Any(a => a.Text.StartsWith("设备异常")))
                                {
                                    await ClickAsync(device, _device, null, 3000, "设备异常-返回首页");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "一键领取"))
                                {
                                    if (text.TextBlocks.Any(a => a.Text.Contains("明日可一键领取")) || text.TextBlocks.Any(a => a.Text == "倒计时结束后才能领取哦"))
                                    {
                                        await ClickAsync(device, _device, null, 3000, "返回-已预约，明日可一键领");
                                        continue;
                                    }
                                    else
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("一键领取"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                            continue;
                                        }
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "恭喜预约成功") && text.TextBlocks.Any(a => a.Text.Contains("不用提醒")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("不用提醒"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        await ClickAsync(device, _device, null, 3000, "返回-不用提醒我也能来");
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "立即预约领取"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("立即预约领取"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "金币已领取"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "金币已领取");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        await ClickAsync(device, _device, null, 3000, "返回-金币已领取");
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text.Contains("明日可一键领取")))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-已经预约");
                                    continue;
                                }
                                #endregion
                            }


                            //当上面没有操作时就执行下面
                            if (!IsOperate)
                            {
                                if (text.TextBlocks.Any(a => a.Text.Trim() == "首页") && text.TextBlocks.Any(a => a.Text.Trim() == "消息") && text.TextBlocks.Any(a => a.Text.Trim() == "我"))
                                {
                                    var values = arrTask.GetTaskItemValue("赚钱首页").GetArray();
                                    var recentCount = values.Count(x => (DateTime.Now - DateTime.ParseExact(x, "yyyy-MM-dd HH:mm:ss", null)).TotalMinutes <= 5);
                                    if (recentCount >= 15)
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("赚钱"));
                                        if (BoxPoint != null)
                                        {
                                            arrTask.UpdateTaskItem("赚钱首页", string.Format("{0}|{1}", string.Join("|", values), $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}"));
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text.StartsWith("看完视频最高")))
                                    {
                                        await Task.Delay(30000);
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "赚钱")|| text.TextBlocks.Any(a => a.Text == "首页"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "赚钱");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            continue;
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
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "加入购物车") && text.TextBlocks.Any(a => a.Text == "购物车"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-加入购物车");
                                    await SwipeAsync(device, _device, 2000, "加入购物车", 100, 1000, 540, 1100, 200);
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("广告")) && text.TextBlocks.Any(a => a.Text == "免费领取"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-广告免费领取");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("收藏")) && text.TextBlocks.Any(a => a.Text == "立即抢购"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-收藏立即抢购");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("导航")) && text.TextBlocks.Any(a => a.Text == "电话"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-导航电话");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("提现说明")) && text.TextBlocks.Any(a => a.Text.StartsWith("去兑换")))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-提现说明去兑换");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "刚刚浏览了该商品"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-刚刚浏览了该商品");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "关注") && text.TextBlocks.Any(a => a.Text.StartsWith("赠送额外获得")))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-关注赠送额外获得");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text.Trim() == "搜索") && text.TextBlocks.Any(a => a.Text.StartsWith("历史记录")))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-搜索历史记录");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text.Trim() == "书城") && text.TextBlocks.Any(a => a.Text.StartsWith("书架")))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-书城书架");
                                    continue;
                                }
                                else if ((text.TextBlocks.LastOrDefault()?.Text ?? "").Contains("立即下载") && !text.TextBlocks.Any(a => a.Text == "反馈") && !text.TextBlocks.Any(a => a.Text == "领取成功"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-立即下载反馈");
                                    continue;
                                }
                                else if ((text.TextBlocks.LastOrDefault()?.Text ?? "").Contains("立即打开") && !text.TextBlocks.Any(a => a.Text == "反馈"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-立即打开反馈");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "服务") && text.TextBlocks.Any(a => a.Text == "须知") && text.TextBlocks.Any(a => a.Text == "活动"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-服务须知活动");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("有爱评论")) && text.TextBlocks.Any(a => a.Text == "发送"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-收起键盘");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text.Contains("逛街得金币")) && text.TextBlocks.Any(a => a.Text == "0秒"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "逛街得金币-返回");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "收支统计") && text.TextBlocks.Any(a => a.Text == "全部"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "账单列表-返回");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "账单详情") && text.TextBlocks.Any(a => a.Text == "删除"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "账单详情-返回");
                                    await ClickAsync(device, _device, null, 3000, "账单列表-返回");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "账单详情") && text.TextBlocks.Any(a => a.Text == "删除"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "账单详情-返回");
                                    await ClickAsync(device, _device, null, 3000, "账单列表-返回");
                                    continue;
                                }

                                #endregion

                                else if (device.CurPage == AppPageEnum.GuangJie)
                                {
                                    await SwipeAsync(device, _device, 2000, "返回-停留页面超过60秒", 500, 500, 500, 1500, 200);
                                }
                                #region 页面停留超时
                                else if ((DateTime.Now - device.LastClickTime).TotalSeconds > 60)
                                {
                                    if (_deviceService.CurAdbClient.GetAppStatus(_device, device.PackageName) != AppStatus.Foreground)
                                    {
                                        await StartAppAsync(device, _device, 3000);
                                    }
                                    else await ClickAsync(device, _device, null, 2000, "返回-停留页面超过60秒");
                                    continue;
                                }
                                #endregion
                            }

                            #endregion
                        }

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
    private async Task HandleSlideUpPageInteractions(DeviceModel device, DeviceData adbDevice, OCRResult ocrResult, List<string> PagePosChange)
    {
        // 限制PagePosChange列表大小不超过50条
        if (PagePosChange.Count >= 50)
        {
            PagePosChange.RemoveAt(0);
        }

        // 添加当前OCR结果到列表
        PagePosChange.Add(string.Join(" ", ocrResult.TextBlocks.Select(a => a.Text)));


        if (ocrResult.TextBlocks.Any(a => a.Text == "抖音极速版") && ocrResult.TextBlocks.Any(a => a.Text == "支付宝" || a.Text == "快手极速版"))
        {
            var BoxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text == "抖音极速版");
            if (BoxPoint != null)
            {
                var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                await ClickAsync(device, adbDevice, new List<OCRPoint>() { new OCRPoint(pos.X, pos.Y - 50) }, 6000, BoxPoint.Text);
            }
        }

        else if (ocrResult.TextBlocks.Any(a => a.Text == "更多推荐") && ocrResult.TextBlocks.Any(a => a.Text == "选集"))
        {
            var BoxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text == "更多推荐");
            if (BoxPoint != null)
            {
                var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                await ClickAsync(device, adbDevice, new List<OCRPoint>() { new OCRPoint(pos.X, pos.Y - 300) }, 2000, BoxPoint.Text);
            }
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text.StartsWith("立即签到")) && ocrResult.TextBlocks.Any(a => a.Text == "签到提醒"))
        {
            var BoxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text == "签到提醒");
            if (BoxPoint != null)
            {
                var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                await ClickAsync(device, adbDevice, new List<OCRPoint>() { new OCRPoint(pos.X, pos.Y + 100) }, 3000, BoxPoint.Text);
            }
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text == "点击重播"))
        {
            var BoxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text == "点击重播");
            if (BoxPoint != null)
            {
                await ClickAsync(device, adbDevice, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
            }
        }
        else if (
            ocrResult.TextBlocks.Any(a => a.Text.Contains("信息保护") || a.Text.Contains("备案号") || a.Text == "领金币" || a.Text.StartsWith("说点什么")) ||
            ocrResult.TextBlocks.Any(a => a.Text.Contains("查看兑换规则"))
            )
        {
            await ClickAsync(device, adbDevice, null, 5000, "返回继续看短剧");
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text == "应用市场") && ocrResult.TextBlocks.Any(a => a.Text == "华为商城"))
        {
            await SwipeAsync(device, adbDevice, 5000, "左滑查找抖音", 600, 1000, 100, 1000, 300);
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text == "短剧") && ocrResult.TextBlocks.Any(a => a.Text == "首页") && !ocrResult.TextBlocks.Any(a => a.Text.Contains("进入看剧金币更多")))
        {
            var BoxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text == "短剧");
            if (BoxPoint != null)
            {
                await ClickAsync(device, adbDevice, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
            }
        }
        else if (ocrResult.Text.Contains("免费看全集最高赚"))
        {
            await SwipeAsync(device, adbDevice, 2000, "上滑滑继续看短剧", 500, 1500, 500, 500, 300);
            var BoxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text.Contains("免费看全集"));
            if (BoxPoint != null)
            {
                await ClickAsync(device, adbDevice, BoxPoint.BoxPoints, 4000, BoxPoint.Text);
            }
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text == "收藏短剧") && ocrResult.TextBlocks.Any(a => a.Text == "购买短剧卡"))
        {
            await ClickAsync(device, adbDevice, new List<OCRPoint>() { new OCRPoint(100, 300) }, 2000, "返回继续看短剧");
            await ClickAsync(device, adbDevice, new List<OCRPoint>() { new OCRPoint(300, 800) }, 2000, "返回继续看短剧");
        }
        else if (
            !ocrResult.TextBlocks.Any(a => a.Text.Contains("秒后可继续上滑观看")) &&
            !ocrResult.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"第([\d]+)集")) &&
            ocrResult.TextBlocks.Any(a => a.Text.Length == 4 && Regex.IsMatch(a.Text, @"^[\u4e00-\u9fa5]+$")) || ocrResult.TextBlocks.Any(a => a.Text == "点击重播" || a.Text.Contains("点击进入直播间") || a.Text.EndsWith("广告") || a.Text == "广告" || a.Text == "点击查询" || a.Text == "免费在线诊股" || a.Text == "获取资料" || a.Text == "点击了解" || a.Text == "获取报价" || a.Text == "查看详情" || a.Text == "立即咨询" || a.Text == "上滑继续观看视频" || a.Text.StartsWith("立即查看") || a.Text.StartsWith("获取底价") || a.Text.StartsWith("查看详情") || a.Text.StartsWith("预约试驾"))

            )
        {
            if (device.Width > 500 && device.Height > 1000)
            {
                await SwipeAsync(device, adbDevice, 15000, "上滑滑继续看短剧", 50, (device.Height - 400), 50, (device.Height - 1000), 300);
            }
            else
            {
                await SwipeAsync(device, adbDevice, 15000, "上滑滑继续看短剧", 500, 1500, 500, 500, 300);
            }
        }
        // 判断最后10条数据内容是否90%相同
        if (!IsOperate && PagePosChange.Count >= 20)
        {
            var lastTenItems = PagePosChange.Skip(PagePosChange.Count - 20).ToList();
            bool isSimilar = CheckContentSimilarity(lastTenItems, 0.9, device);

            if (isSimilar)
            {
                if (ocrResult.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"第([\d]+)集")))
                {
                    await ClickAsync(device, adbDevice, new List<OCRPoint>() { new OCRPoint(device.Width / 2, device.Height / 2) }, 3000, "点击-内容相似");
                }
                else
                {

                    await SwipeAsync(device, adbDevice, 2000, "上滑-内容相似", 500, 1500, 500, 500, 300);
                }
                PagePosChange.Clear();
                return;
            }
            else if (ocrResult.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"第([\d]+)集")))
            {
                lastTenItems = PagePosChange.Skip(PagePosChange.Count - 30).ToList();
                try
                {
                    var arr = lastTenItems.Select(a =>
                    {
                        var m = Regex.Match(a, @"(\d/\d)圈([\s\S]+)(第([\d]+)集)");
                        return m.Groups[1].Value + m.Groups[3].Value;
                    }).ToList();
                    if (arr.GroupBy(a => a).Count() == 1)
                    {
                        await ClickAsync(device, adbDevice, new List<OCRPoint>() { new OCRPoint(device.Width / 2, device.Height / 2) }, 5000, "点击-内容相似");
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogInfo($"匹配本文出错: {ex.Message}", device.Id);
                }
            }
        }
        IsOperate = false;

    }

    /// <summary>
    /// 检查内容相似度
    /// </summary>
    /// <param name="contents">要检查的内容列表</param>
    /// <param name="similarityThreshold">相似度阈值(0.0-1.0)</param>
    /// <returns>如果内容相似度超过阈值则返回true</returns>
    private bool CheckContentSimilarity(List<string> contents, double similarityThreshold, DeviceModel device)
    {
        if (contents.Count <= 1) return false;

        // 如果所有内容都完全相同，直接返回true
        if (contents.All(c => c == contents[0]))
        {
            return true;
        }

        // 计算每个内容与其他所有内容的相似度
        int similarPairsCount = 0;
        int totalPairsCount = 0;

        // 比较每一对内容
        for (int i = 0; i < contents.Count; i++)
        {
            for (int j = i + 1; j < contents.Count; j++)
            {
                totalPairsCount++;

                // 使用更简单的方法计算相似度：相同字符的长度比例
                string content1 = contents[i];
                string content2 = contents[j];

                // 如果两个内容长度相差太大，认为它们不相似
                if (Math.Abs(content1.Length - content2.Length) > content1.Length * 0.3)
                {
                    continue;
                }

                // 计算最长公共子序列长度作为相似度指标
                int commonLength = 0;
                int minLength = Math.Min(content1.Length, content2.Length);

                for (int k = 0; k < minLength; k++)
                {
                    if (content1[k] == content2[k])
                    {
                        commonLength++;
                    }
                }

                double similarity = (double)commonLength / minLength;

                if (similarity >= similarityThreshold)
                {
                    similarPairsCount++;
                }
            }
        }

        // 如果没有比较对，返回false
        if (totalPairsCount == 0) return false;

        // 计算相似对的比例
        double similarRatio = (double)similarPairsCount / totalPairsCount;

        // 调试日志
        _logService.LogInfo($"相似度检查: 相似对数量={similarPairsCount}, 总对数={totalPairsCount}, 相似比例={similarRatio}", device.Id);

        return similarRatio >= 0.8; // 80%的内容对相似就认为整体相似
    }

    private bool isAd = false; //是否完成了广告
    // 预编译正则表达式以提高性能
    private static readonly Regex RegexAdTime = new Regex(@"看广告免费领取(\d+)分钟时长", RegexOptions.Compiled);
    private static readonly Regex RegexCoinPattern = new Regex(@"(\d+)金币", RegexOptions.Compiled);
    private static readonly Regex RegexRewardPattern = new Regex(@"(领取奖励|继续观看|继续互动|继续领金币)", RegexOptions.Compiled);
    /// <summary>
    /// 处理小说页面的交互
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <param name="adbDevice">ADB设备</param>
    /// <param name="ocrResult">OCR识别结果</param>
    /// <param name="isAd">广告状态引用</param>
    /// <returns>元组(是否已处理, 更新后的isAd值)</returns>
    private async Task<bool> HandleNovelPageInteractions(DeviceModel device, DeviceData adbDevice, OCRResult ocrResult)
    {
        // 检查是否有看广告免费领取时长的提示
        if (ocrResult.TextBlocks.Any(a => RegexAdTime.IsMatch(a.Text)))
        {
            var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => RegexAdTime.IsMatch(a.Text));
            if (boxPoint != null)
            {
                await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 1000, boxPoint.Text);
            }
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text == "看广告领取听书时长"))
        {
            var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text == "看广告领取听书时长");
            if (boxPoint != null)
            {
                await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 3000, boxPoint.Text);
            }
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text.StartsWith("看广告免费解锁")))
        {
            var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看广告免费解锁"));
            if (boxPoint != null)
            {
                await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 3000, boxPoint.Text);
            }
        }
        // 检查是否有领取成功的提示
        else if (ocrResult.TextBlocks.Any(a => a.Text.StartsWith("看小说赚金币")))
        {
            var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看小说赚金币"));
            if (boxPoint != null)
            {
                await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 3000, boxPoint.Text);
            }
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text.StartsWith("继续阅读")))
        {
            var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("继续阅读"));
            if (boxPoint != null)
            {
                await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 1000, boxPoint.Text);

            }
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text == "设置") && ocrResult.TextBlocks.Any(a => a.Text.EndsWith("听") && a.BoxPoints.Any(x => x.Y > 900 && x.X > 300)))
        {
            var BoxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text.EndsWith("听") && a.BoxPoints.Any(x => x.Y > 900 && x.X > 300));
            if (BoxPoint != null)
            {
                byte[]? bytes = await _deviceService.CaptureWithAdb(device);
                if (bytes != null)
                {

                    string iconTemp = Path.Combine(Environment.CurrentDirectory, $"Resources\\template\\play.png");
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
                        await ClickAsync(device, adbDevice, new List<OCRPoint>() { new OCRPoint(maxLoc.X + 15, maxLoc.Y + 15) }, 5000, "匹配播放按钮");
                        return true;
                    }

                }
                var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                await ClickAsync(device, adbDevice, new List<OCRPoint>() { new OCRPoint(pos.X - 100, pos.Y) }, 3000, BoxPoint.Text);
            }
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text == "天天赚金币"))
        {
            var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text == "天天赚金币");
            if (boxPoint != null)
            {
                var pos = boxPoint.BoxPoints.CalculateTextBlockCenter();
                await ClickAsync(device, adbDevice, new List<OCRPoint>() { new OCRPoint(pos.X, pos.Y - 100) }, 3000, boxPoint.Text);

            }
        }
        // 检查是否有领取成功的提示
        else if (ocrResult.TextBlocks.Any(a => a.Text.StartsWith("领取成功")))
        {
            var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("领取成功"));
            if (boxPoint != null)
            {
                isAd = true;
                await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 3000, boxPoint.Text);

            }
        }
        // 检查是否有领取奖励或继续观看的提示
        else if (ocrResult.TextBlocks.Any(a => RegexRewardPattern.IsMatch(a.Text)) && ocrResult.TextBlocks.Any(a => a.Text.Contains("坚持退出")))
        {
            var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => RegexRewardPattern.IsMatch(a.Text));
            if (boxPoint != null)
            {
                await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 3000, boxPoint.Text);

            }
        }
        // 检查是否有金币显示，如果有则左滑继续看小说
        else if (!ocrResult.TextBlocks.Any(a => a.Text == "赚钱任务") && ocrResult.TextBlocks.Any(a => RegexCoinPattern.IsMatch(a.Text)))
        {
            isAd = false;
            await SwipeAsync(device, adbDevice, new Random().Next(10000, 15000), "左滑继续看小说", 300, 1500, 100, 1500, 200);

        }
        else if (ocrResult.TextBlocks.Any(a => a.Text.Contains("个人信息保护声明") || a.Text.Contains("点击下方链接")))
        {
            isAd = false;
            await ClickAsync(device, adbDevice, null, 2000, "返回");
        }
        // 检查是否停留在页面超过30秒
        else if (isAd && !ocrResult.TextBlocks.Any(a => RegexCoinPattern.IsMatch(a.Text)) && (DateTime.Now - device.LastClickTime).TotalSeconds > 30)
        {
            isAd = false;
            await ClickAsync(device, adbDevice, null, 2000, "返回-停留页面超过30秒");
        }
        else if (ocrResult.TextBlocks.Any(a => RegexCoinPattern.IsMatch(a.Text)) && (DateTime.Now - device.LastClickTime).TotalSeconds > 60)
        {
            isAd = false;
            await ClickAsync(device, adbDevice, null, 2000, "返回-停留页面超过30秒");
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text.Contains("秒后可领奖励")))
        {
            var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text.Contains("秒后可领奖励"));
            if (boxPoint != null)
            {
                var s = boxPoint.Text.Substring(0, boxPoint.Text.IndexOf("秒")).ToInt() - 2;
                if (s > 1)
                {
                    await Task.Delay(s.ToInt());
                }
            }
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text == "赚钱") && ocrResult.TextBlocks.Any(a => a.Text == "商城") && ocrResult.TextBlocks.Any(a => a.Text == "我"))
        {
            var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text == "赚钱");
            if (boxPoint != null)
            {
                await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 3000, boxPoint.Text);
            }
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text == "赚钱任务") && ocrResult.TextBlocks.Any(a => a.Text == "去听书"))
        {
            var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text == "去听书");
            if (boxPoint != null)
            {
                await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 5000, boxPoint.Text);
            }
        }
        else if (ocrResult.TextBlocks.Any(a => a.Text == "书城") && ocrResult.TextBlocks.Any(a => a.Text == "我的"))
        {
            if (ocrResult.TextBlocks.Any(a => a.Text == "继续阅读"))
            {

                var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text == "继续阅读");
                if (boxPoint != null)
                {
                    await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 5000, boxPoint.Text);
                }
            }
            else if (ocrResult.TextBlocks.Any(a => a.Text == "推荐榜"))
            {

                var boxPoints = ocrResult.TextBlocks.Where(a => a.Text.EndsWith("万热度"));
                if (boxPoints.Count() > 0)
                {
                    var boxPoint = boxPoints.Skip(new Random().Next(0, boxPoints.Count() - 1)).FirstOrDefault();
                    await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 5000, boxPoint.Text);
                }
            }
            else
            {
                var boxPoint = ocrResult.TextBlocks.FirstOrDefault(a => a.Text == "书城");
                if (boxPoint != null)
                {
                    await ClickAsync(device, adbDevice, boxPoint.BoxPoints, 5000, boxPoint.Text);
                }
            }
        }

        return true;
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