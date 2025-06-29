using System.Text.RegularExpressions;
using PaddleOCRSharp;
using System.Collections.Concurrent;
using AdAuto.Extensions.Helper;
using Yuandl.Common.Helper;
using AdAuto.Extensions.enums;

namespace AdAuto.Extensions.Provider;

/// <summary>
/// 抖音广告观看自动化实现
/// </summary>
public class DouyinAdWatching : IAdWatching
{
    private readonly IMainViewModel _mainViewModel;
    private bool SwitchAccount = false;
    private AppPageEnum CurPage = AppPageEnum.Node;
    public DateTime lastClickTime = DateTime.Now;
    private bool IsOperate = false;
    private bool _disposed = false;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    public ConcurrentDictionary<string, List<string>> arrTask { get; set; } = new()
    {
        ["吃饭打卡赚金币"] = new List<string>(),
        ["天天领金币"] = new List<string>(),
        ["预约领金币"] = new List<string>(),
        ["一键领音符"] = new List<string>(),
        ["一键领金币"] = new List<string>(),
        ["看广告赚金币"] = new List<string>(),
        ["看指定视频得金币"] = new List<string>(),
        ["限时任务赚音符"] = new List<string>(),
        ["赚钱首页"] = new List<string>(),
        ["看直播开宝箱"] = new List<string>(),
        ["做任务领奖励"] = new List<string>(),
        ["开宝箱得金币"] = new List<string>(),
        ["逛街赚钱"] = new List<string>(),
        ["逛商城赚金币"] = new List<string>(),
        ["切换账号"] = new List<string>() { "星月飞天" }
    };
    private ConcurrentDictionary<string, (TimeSpan start, TimeSpan end)> mealTimes = new()
    {
        ["早餐"] = (TimeSpan.Parse("05:00"), TimeSpan.Parse("09:00")),
        ["午餐"] = (TimeSpan.Parse("11:00"), TimeSpan.Parse("14:00")),
        ["晚餐"] = (TimeSpan.Parse("17:00"), TimeSpan.Parse("20:00")),
        ["夜宵"] = (TimeSpan.Parse("21:00"), TimeSpan.Parse("23:59"))
    };


    public DouyinAdWatching(IMainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public async Task AutomateAdWatching(CancellationToken cancellationToken)
    {
        try
        {
            // 启动定时任务，每天凌晨清理arrTask
            TaskCleaner.StartDailyCleanup(arrTask, cancellationToken);

        }
        catch (Exception ex)
        {
            LogMessage($"自动化过程出错: {ex.Message}");
            _mainViewModel.StopAutomation();
        }

        try
        {
            int StopTimeSpan = 0;
            bool isSwipeTop = false;
            List<string> PagePosChange = new List<string>();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    OCRResult? text = await _mainViewModel.RecognizeText();
                    if (text?.Text?.Length > 1)
                    {
                        if (text.TextBlocks.Any(a => a.Text == "网络异常") && text.TextBlocks.Any(a => a.Text == "刷新"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "刷新");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "关闭应用") && text.TextBlocks.Any(a => a.Text == "等待"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "等待");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "当前无新视频"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                await ClickAsync(null, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        //else if (text.TextBlocks.Any(a => a.Text == "首页") && text.TextBlocks.Any(a => a.Text == "我") && text.TextBlocks.Any(a => a.Text.Contains("开心收下") || a.Text.Contains("评价并收下") ))
                        //{
                        //    await SwipeAsync(3000, "虚假广告视频", 500, 1500, 500, 500, 200);
                        //    continue;
                        //}
                        //else if (text.TextBlocks.Any(a => a.Text == "s进入直播间"))
                        //{
                        //    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("评价并收下金币"));
                        //    if (BoxPoint != null)
                        //    {
                        //        await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                        //        continue;
                        //    }
                        //}
                        else if (text.TextBlocks.Any(a => a.Text == "评价并收下金币"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("评价并收下金币"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"(领取奖励|继续观看|继续互动|继续领金币)")) && text.TextBlocks.Any(a => a.Text.Contains("坚持退出")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"(领取奖励|继续观看|继续互动|继续领金币)"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains(@"金币领取成功") || a.Text.Contains(@"获得开宝箱奖励")) && text.TextBlocks.Any(a => a.Text.Contains("开心收下")))
                        {
                            if (text.TextBlocks.Any(a => a.Text.StartsWith("看广告视频再赚")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看广告视频再赚"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.Ad;
                                    await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else
                            {

                                var BoxPoint = text.TextBlocks.FirstOrDefault(A => A.Text.Contains("开心收下"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;
                                }
                            }

                        }
                        else if (text.TextBlocks.Any(a => a.Text.EndsWith("回任务页")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(A => A.Text.EndsWith("回任务页"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "已暂停") && text.TextBlocks.Any(a => a.Text == "金币") && !text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"观看([\d\.]+)秒")))
                        {
                            await SwipeAsync(3000, "金币暂停", 500, 1500, 500, 500, 200);
                            continue;
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("领取成功")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("领取成功"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("获得开宝箱奖励")) && text.TextBlocks.Any(a => a.Text.StartsWith("我知道了")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("我知道了"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("领今日奖励")) && text.TextBlocks.Any(a => a.Text.StartsWith("签到领")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("领今日奖励"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("每日签到")) && text.TextBlocks.Any(a => a.Text.StartsWith("签到提醒")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("立即签到"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("签到成功")) && text.TextBlocks.Any(a => a.Text.StartsWith("好的")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("好的"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "明天继续领现金"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "明天继续领现金");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看视频赚([\d\.]+)元")) && text.TextBlocks.Any(a => a.Text.Contains("老用户专享福利")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看视频赚([\d\.]+)元"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("立即签到")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("立即签到"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "领金币") && !text.TextBlocks.Any(a => a.Text == "首页" || a.Text == "今日已领" || a.Text.EndsWith("的钱包")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领金币");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if(text.TextBlocks.Any(a => a.Text == "开宝箱") && text.TextBlocks.Any(a => a.Text.Contains("做任务领奖励") || a.Text.Contains("做任务赚金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.EndsWith("s秒后进入下场直播")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("X"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.EndsWith("直播已结束")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("x"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        if (text.TextBlocks.Any(a => a.Text.StartsWith("金币收益")) && text.TextBlocks.Any(a => a.Text.Contains("现金收益") || a.Text == "详情"))
                        {
                            var currentTime = DateTime.Now.TimeOfDay;
                            var key = mealTimes.FirstOrDefault(a => currentTime >= a.Value.start && currentTime < a.Value.end).Key;
                            #region 首页
                            if (CurPage != AppPageEnum.MoneyHome)
                            {
                                CurPage = AppPageEnum.MoneyHome;
                                var Last = PagePosChange.LastOrDefault();
                                if (PagePosChange.Count > 0 && Last != null)
                                {
                                    LogMessage("页面没有跳转，清空重试");
                                    arrTask[Last].RemoveAt(arrTask[Last].Count-1);
                                    PagePosChange.Clear();
                                }
                                else {

                                    await SwipeAsync(4000, "滑动到看指定视频得金币", 100, 1200, 540, 1000, 200);
                                    continue;
                                }
                            }

                            #region 天天领金币
                            if (arrTask["天天领金币"].Count == 0)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("天天领金币"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.DayMoney;
                                    PagePosChange.Add("天天领金币");
                                    arrTask["天天领金币"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;

                                }
                            }
                            #endregion

                            #region 一键领金币
                            if (arrTask["一键领金币"].Count == 0)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("一键领金币") || a.Text.EndsWith("一键领金币"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.OneClickMoney;
                                    PagePosChange.Add("一键领金币");
                                    arrTask["一键领金币"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;

                                }
                            }
                            #endregion

                            #region 开宝箱得金币
                            if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text == "开宝箱得金币" || a.Text.StartsWith("点击领金币") || Regex.IsMatch(a.Text, @"点击领(\d+)")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱得金币" || a.Text.StartsWith("点击领金币") || Regex.IsMatch(a.Text, @"点击领(\d+)"));
                                if (BoxPoint != null)
                                {
                                    PagePosChange.Add(BoxPoint.Text);
                                    CurPage = AppPageEnum.OpenTreasure;
                                    arrTask["开宝箱得金币"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 逛街得金币
                            if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"浏览低价商品(\d+)秒即得")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("逛街赚钱") || a.Text.StartsWith("逛街赚音符"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.GuangJie;
                                    PagePosChange.Add("逛街赚钱");
                                    arrTask["逛街赚钱"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 逛商城赚金币
                            if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("逛商城赚金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("逛商城赚金币"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.GuangSC;
                                    PagePosChange.Add("逛商城赚金币");
                                    arrTask["逛商城赚金币"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 吃饭打卡赚金币
                            //早餐：05:00-09:00、午餐：11:00-14:00 、晚餐：17:00-20:00 、夜宵：21:00-24:00、
                            if (CurPage == AppPageEnum.MoneyHome && (arrTask["吃饭打卡赚金币"].Count == 0 || (!arrTask["吃饭打卡赚金币"].Contains(key) && mealTimes.Any(a => currentTime >= a.Value.start && currentTime < a.Value.end) && text.TextBlocks.Any(a => a.Text.Contains("吃饭打卡赚金币")))))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("吃饭打卡赚金币"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.Dining;
                                    arrTask["吃饭打卡赚金币"].Add(key);
                                    PagePosChange.Add("吃饭打卡赚金币");
                                    await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 看指定视频得金币
                            if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, "看指定视频得(金币|音符)")) && (DateTime.Now - (arrTask["看指定视频得金币"].LastOrDefault() ?? "").ToDateTime(DateTime.Now.AddMinutes(-22))).TotalMinutes > 20)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "看指定视频得(金币|音符)"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.AppointVideo;
                                    PagePosChange.Add("看指定视频得金币");
                                    arrTask["看指定视频得金币"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 看广告赚金币
                            if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("看广告赚金币")) && (DateTime.Now - (arrTask["看广告赚金币"].LastOrDefault() ?? "").ToDateTime(DateTime.Now.AddMinutes(-7))).TotalMinutes > 5)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看广告赚金币"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.ViewAdMoney;
                                    PagePosChange.Add("看广告赚金币");
                                    arrTask["看广告赚金币"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion


                            #region 看直播开宝箱
                            if (CurPage == AppPageEnum.MoneyHome && arrTask["看直播开宝箱"].Count < 10 && (DateTime.Now - (arrTask["看直播开宝箱"].LastOrDefault() ?? "").ToDateTime(DateTime.Now.AddMinutes(-15))).TotalMinutes > 10)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看直播开宝箱"));
                                if (BoxPoint != null)
                                {
                                    int Index = text.TextBlocks.FindIndex(a => Regex.IsMatch(a.Text, @"开宝箱最高可得(\d+)金币"));
                                    if (Index > -1)
                                    {
                                        Match match = Regex.Match(text.TextBlocks[Index + 2].Text, @"(\d+)/10");
                                        if (match.Success)
                                        {
                                            arrTask["看直播开宝箱"].Clear();
                                            for (int i = 0; i < match.Groups[1].Value.ToInt(); i++)
                                            {
                                                arrTask["看直播开宝箱"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                            }
                                        }
                                    }
                                    CurPage = AppPageEnum.ViewLiveRoom;
                                    PagePosChange.Add("看直播开宝箱");
                                    arrTask["看直播开宝箱"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(BoxPoint.BoxPoints, 10000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion


                            if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("免费送红包最高得")))
                            {
                                await ClickAsync(null, 3000, "返回-免费送红包最高得");
                            }

                            if (CurPage == AppPageEnum.MoneyHome && StopTimeSpan > 60)
                            {
                                await SwipeAsync(3000, "在首页停留时间超过60秒自动下滑第一次", 500, 500, 500, 1500, 200);
                            }
                            else if (CurPage == AppPageEnum.MoneyHome && StopTimeSpan > 30)
                            {
                                if (isSwipeTop)
                                {
                                    await SwipeAsync(3000, "在首页停留时间超过30秒自动上滑", 500, 1500, 500, 500, 200);
                                }
                                else
                                {
                                    await SwipeAsync(3000, "在首页停留时间超过30秒自动下滑", 500, 500, 500, 1500, 200);
                                }
                                isSwipeTop = !isSwipeTop;

                            }
                            if (CurPage != AppPageEnum.MoneyHome)
                            {
                                _mainViewModel.UpdateTaskData();
                            }
                            #endregion

                            StopTimeSpan++;
                        }
                        else
                        {
                            StopTimeSpan = 0;

                            #region 非首页
                            if (CurPage == AppPageEnum.GuangJie)
                            {
                                #region 逛街赚钱、逛街得金币
                                if (text.TextBlocks.Any(a => a.Text == "逛街得金币"))
                                {
                                    PagePosChange.Clear();
                                    if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"浏览(\d+)秒可领")))
                                    {
                                        // 执行小幅度上滑
                                        await SwipeAsync(4000, "逛街得金币", 500, 1500, 500, 500, 200);
                                        // 执行小幅度下滑
                                        await SwipeAsync(2000, "逛街得金币", 500, 500, 500, 1500, 200);
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "明日浏览可领取奖励" || a.Text.StartsWith("恭喜获得")))
                                    {
                                        await ClickAsync(null, 3000, "返回-逛街得金币恭喜获得");
                                    }
                                }
                                #endregion
                            }
                            else if (CurPage == AppPageEnum.AppointVideo)
                            {
                                PagePosChange.Clear();
                                #region 看指定视频得金币
                                /*
                                if (text.TextBlocks.Any(a => a.Text.StartsWith("有爱评论") || a.Text == "直播中") && text.TextBlocks.Any(a => a.Text == "金币"))
                                {
                                    LogMessage("检测到'看指定视频得金币'文本");
                                    if (text.TextBlocks.Any(a => a.Text.EndsWith("回任务页")))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(A => A.Text.EndsWith("回任务页"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        }
                                    }
                                    else if (
                                        (text.TextBlocks.Any(a => a.Text == "已暂停") && (text.TextBlocks.Any(a => a.Text == "已到账") || text.TextBlocks.Any(a => a.Text == "购买商品"))) ||
                                        (text.TextBlocks.Any(a => a.Text == "已暂停") && text.TextBlocks.Any(a => a.Text == "金币") && !text.TextBlocks.Any(a => a.Text.StartsWith("观看"))))
                                    {
                                        await SwipeAsync(3000, "暂停金币", 500, 1500, 500, 500, 200);
                                    }

                                }
                                else if (text.TextBlocks.Any(a => a.Text.EndsWith("回任务页")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(A => A.Text.EndsWith("回任务页"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    }
                                    else
                                    {
                                        await ClickAsync(null, 3000, "返回-回任务页");
                                    }
                                }*/
                                #endregion
                            }
                            else if (CurPage == AppPageEnum.ViewAdMoney)
                            {
                                #region 看广告赚金币
                                if (text.TextBlocks.Any(a => a.Text == "反馈"))
                                {
                                    PagePosChange.Clear();
                                }
                                #endregion
                            }
                            else if (CurPage == AppPageEnum.Dining)
                            {
                                #region 吃饭打卡领金币
                                if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, "看(指定|广告)视频领(早餐|午餐|晚餐|夜宵)补贴")))
                                {
                                    PagePosChange.Clear();
                                    if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, "看(指定|广告)视频领(早餐|午餐|晚餐|夜宵)补贴")))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "看(指定|广告)视频领(早餐|午餐|晚餐|夜宵)补贴"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                        }
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
                                            await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                        }
                                    }
                                    else
                                    {
                                        await ClickAsync(null, 5000, "返回-吃饭打卡领金币");
                                    }
                                }
                                #endregion
                            }
                            else if (CurPage == AppPageEnum.DayMoney)
                            {
                                #region 天天领金币-钱包
                                if (text.TextBlocks.Any(a => a.Text.EndsWith("的钱包")))
                                {
                                    PagePosChange.Clear();
                                    if (text.TextBlocks.Any(a => a.Text == "领取今日奖励") && text.TextBlocks.Any(a => a.Text == "领取") && DateTime.Now.Hour > 7)
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领取");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                            if (text.TextBlocks.Any(a => a.Text == "抖音支付积分"))
                                            {
                                                BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("抖音支付积分"));
                                                if (BoxPoint != null)
                                                {
                                                    await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                                }
                                            }
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "X"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 4000, BoxPoint.Text);
                                        }
                                    }
                                    else
                                    {
                                        await ClickAsync(null, 3000, "返回-立即签到X");
                                    }
                                }
                                #endregion
                            }
                            else if (CurPage == AppPageEnum.GuangSC)
                            {
                                #region 逛商城赚金币
                                if (text.TextBlocks.Any(a => a.Text == "逛商城"))
                                {
                                    PagePosChange.Clear();
                                }
                                if (text.TextBlocks.Any(a => a.Text == "商城") && text.TextBlocks.Any(a => a.Text == "去领取"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去领取");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "下单返金币")) {

                                    await ClickAsync(null, 5000, "浏览低价商品返回");
                                }
                                else
                                {
                                    // 执行小幅度上滑
                                    await SwipeAsync(2000, "逛商城赚金币", 500, 1500, 500, 500, 200);
                                    // 执行小幅度下滑
                                    await SwipeAsync(2000, "逛商城赚金币", 500, 500, 500, 1500, 200);
                                }
                                #endregion
                            }
                            else if (CurPage == AppPageEnum.OpenTreasure)
                            {
                                #region 开宝箱得金币
                                if (text.TextBlocks.Any(a => a.Text == "开宝箱奖励"))
                                {
                                    PagePosChange.Clear();
                                }
                                #endregion
                            }
                            else if (CurPage == AppPageEnum.ViewLiveRoom)
                            {
                                #region 看直播开宝箱

                                if (text.TextBlocks.Any(a => a.Text == "关注"))
                                {
                                    PagePosChange.Clear();



                                }
                                #endregion
                            }
                            else if (CurPage == AppPageEnum.OneClickMoney)
                            {
                                #region 一键领金币
                                if (text.TextBlocks.Any(a => a.Text == "领全币") || text.TextBlocks.Any(a => a.Text == "一键领取"))
                                {
                                    PagePosChange.Clear();
                                }
                                if (text.TextBlocks.Any(a => a.Text == "一键领取"))
                                {
                                    if (text.TextBlocks.Any(a => a.Text == "已预约，明日可一键领取"))
                                    {
                                        await ClickAsync(null, 3000, "返回-已预约，明日可一键领");
                                    }
                                    else
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("一键领取"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        }
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "恭喜预约成功") && text.TextBlocks.Any(a => a.Text.Contains("不用提醒")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("不用提醒"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        await ClickAsync(null, 3000, "返回-不用提醒我也能来");
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "立即预约领取"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("立即预约领取"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "金币已领取"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "金币已领取");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        await ClickAsync(null, 3000, "返回-金币已领取");
                                    }
                                }
                                #endregion
                            }

                            //当上面没有操作时就执行下面
                            if (!IsOperate)
                            {
                                if (text.TextBlocks.Any(a => a.Text.Trim() == "首页") && text.TextBlocks.Any(a => a.Text.Trim() == "消息") && text.TextBlocks.Any(a => a.Text.Trim() == "我"))
                                {
                                    var recentCount = arrTask["赚钱首页"].Count(x => (DateTime.Now - DateTime.ParseExact(x, "yyyy-MM-dd HH:mm:ss", null)).TotalMinutes <= 5);
                                    if (recentCount >= 15)
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("赚钱"));
                                        if (BoxPoint != null)
                                        {
                                            arrTask["赚钱首页"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text.StartsWith("看完视频最高")))
                                    {
                                        await Task.Delay(30000);
                                    }
                                    else
                                    {
                                        await SwipeAsync(new Random().Next(10000, 25000), "赚钱首页刷视频", 540, 1500, 540, 1000, 200);
                                    }
                                }
                                #region 特殊页面
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("立即下载")) && text.TextBlocks.Any(a => a.Text == "侵权投诉"))
                                {
                                    await ClickAsync(null, 3000, "返回-立即下载");
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "加入购物车") && text.TextBlocks.Any(a => a.Text == "购物车"))
                                {
                                    await ClickAsync(null, 3000, "返回-加入购物车");
                                    await SwipeAsync(2000, "加入购物车", 100, 1000, 540, 1100, 200);
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("广告")) && text.TextBlocks.Any(a => a.Text == "免费领取"))
                                {
                                    await ClickAsync(null, 3000, "返回-广告免费领取");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("收藏")) && text.TextBlocks.Any(a => a.Text == "立即抢购"))
                                {
                                    await ClickAsync(null, 3000, "返回-收藏立即抢购");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("导航")) && text.TextBlocks.Any(a => a.Text == "电话"))
                                {
                                    await ClickAsync(null, 3000, "返回-导航电话");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("提现说明")) && text.TextBlocks.Any(a => a.Text.StartsWith("去兑换")))
                                {
                                    await ClickAsync(null, 3000, "返回-提现说明去兑换");
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "刚刚浏览了该商品"))
                                {
                                    await ClickAsync(null, 3000, "返回-刚刚浏览了该商品");
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "关注") && text.TextBlocks.Any(a => a.Text.StartsWith("赠送额外获得")))
                                {
                                    await ClickAsync(null, 3000, "返回-关注赠送额外获得");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.Trim() == "搜索") && text.TextBlocks.Any(a => a.Text.StartsWith("历史记录")))
                                {
                                    await ClickAsync(null, 3000, "返回-搜索历史记录");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.Trim() == "书城") && text.TextBlocks.Any(a => a.Text.StartsWith("书架")))
                                {
                                    await ClickAsync(null, 3000, "返回-书城书架");
                                }
                                else if ((text.TextBlocks.LastOrDefault()?.Text ?? "").Contains("立即下载") && !text.TextBlocks.Any(a => a.Text == "反馈") && !text.TextBlocks.Any(a => a.Text == "领取成功"))
                                {
                                    await ClickAsync(null, 3000, "返回-立即下载反馈");
                                }
                                else if ((text.TextBlocks.LastOrDefault()?.Text ?? "").Contains("立即打开") && !text.TextBlocks.Any(a => a.Text == "反馈"))
                                {
                                    await ClickAsync(null, 3000, "返回-立即打开反馈");
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "服务") && text.TextBlocks.Any(a => a.Text == "须知") && text.TextBlocks.Any(a => a.Text == "活动"))
                                {
                                    await ClickAsync(null, 3000, "返回-服务须知活动");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("有爱评论")) && text.TextBlocks.Any(a => a.Text == "发送"))
                                {
                                    await ClickAsync(null, 3000, "返回-收起键盘");
                                }
                                else if (CurPage != AppPageEnum.ViewLiveRoom && text.TextBlocks.Any(a => a.Text.Contains("说点什么")) && text.TextBlocks.Any(a => a.Text == "关注"))
                                {
                                    await ClickAsync(null, 5000, "返回-直播间");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.Contains("逛街得金币")) && text.TextBlocks.Any(a => a.Text == "0秒"))
                                {
                                    await ClickAsync(null, 5000, "逛街得金币-返回");
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "收支统计") && text.TextBlocks.Any(a => a.Text == "全部"))
                                {
                                    await ClickAsync(null, 3000, "账单列表-返回");
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "账单详情") && text.TextBlocks.Any(a => a.Text == "删除"))
                                {
                                    await ClickAsync(null, 3000, "账单详情-返回");
                                    await ClickAsync(null, 3000, "账单列表-返回");
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "账单详情") && text.TextBlocks.Any(a => a.Text == "删除"))
                                {
                                    await ClickAsync(null, 3000, "账单详情-返回");
                                    await ClickAsync(null, 3000, "账单列表-返回");
                                }

                                #endregion

                                else if (CurPage == AppPageEnum.GuangJie)
                                {
                                    await SwipeAsync(2000, "返回-停留页面超过60秒", 500, 500, 500, 1500, 200);
                                }
                                #region 页面停留超时
                                else if ((DateTime.Now - lastClickTime).TotalSeconds > 60)
                                {
                                    await ClickAsync(null, 2000, "返回-停留页面超过60秒");
                                    //else if ((DateTime.Now - lastClickTime).TotalSeconds > 120 && CurPage == AppPageEnum.MoneyHome)
                                    //{
                                    //    SwitchAccount = true;
                                    //}
                                }
                                #endregion
                            }

                            #endregion
                        }

                        IsOperate = false;
                    }
                }
                catch (Exception ex)
                {

                    LogMessage($"自动化过程出错: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 操作被取消
        }
        catch (Exception ex)
        {
            LogMessage($"自动化过程出错: {ex.Message}");
            _mainViewModel.StopAutomation();
        }
    }
    /// <summary>
    /// 执行点击操作
    /// </summary>
    /// <param name="points">点击坐标</param>
    /// <param name="delay">延迟时间</param>
    /// <param name="text">点击文本描述</param>
    /// <returns>异步任务</returns>
    protected async Task ClickAsync(List<OCRPoint>? points = null, int delay = 0, string text = "")
    {
        IsOperate = true;
        lastClickTime = DateTime.Now;
        await _mainViewModel.ClickAsync(points, delay, text, CurPage);
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
    protected async Task SwipeAsync(int delay, string text, int startX, int startY, int endX, int endY, int duration)
    {
        IsOperate = true;
        lastClickTime = DateTime.Now;
        await _mainViewModel.SwipeAsync(delay, text, startX, startY, endX, endY, duration);
    }
    /// <summary>
    /// 返回首页
    /// </summary>
    /// <param name="delay"></param>
    protected async Task ClickHomeButtonAsync(int delay = 0)
    {
        IsOperate = true;
        lastClickTime = DateTime.Now;
        await _mainViewModel.ClickHomeButtonAsync(delay);
    }

    /// <summary>
    /// 记录日志消息
    /// </summary>
    /// <param name="message">日志消息</param>
    protected void LogMessage(string message)
    {
        _mainViewModel.LogMessage(message);
    }

}