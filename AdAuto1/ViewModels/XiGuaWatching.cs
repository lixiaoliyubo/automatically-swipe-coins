using AdAuto.enums;
using AdAuto.Extensions;
using AdAuto.Helper;
using PaddleOCRSharp;
using System.Collections.Concurrent;
using System.Drawing;
using System.Text.RegularExpressions;
using Yuandl.Common.Helper;

namespace AdAuto.ViewModels
{
    /// <summary>
    /// 抖音广告观看自动化实现
    /// </summary>
    public class XiGuaWatching : IAdWatching
    {
        private readonly IMainViewModel _mainViewModel;
        private AppPageEnum CurPage = AppPageEnum.Node;
        public DateTime lastClickTime = DateTime.Now;
        private bool IsOperate = false;
        private string packageName = "com.ss.android.article.video";
        private int MaxY = 0;
        private ConcurrentDictionary<string, string> PageChange = new();

        public ConcurrentDictionary<string, string> arrTask { get; set; } = new()
        {
            ["吃饭打卡赚金币"] = string.Empty,
            ["天天领金币"] = string.Empty,
            ["预约领金币"] = string.Empty,
            ["一键领音符"] = string.Empty,
            ["一键领金币"] = string.Empty,
            ["看广告赚金币"] = string.Empty,
            ["看指定视频得金币"] = string.Empty,
            ["限时任务赚音符"] = string.Empty,
            ["赚钱首页"] = string.Empty,
            ["看直播开宝箱"] = string.Empty,
            ["做任务领奖励"] = string.Empty,
            ["开宝箱得金币"] = string.Empty,
            ["逛街赚金币"] = string.Empty,
            ["逛商城赚金币"] = string.Empty,
            ["签到"] = string.Empty,
            ["切换账号"] = "星月飞天"
        };
        private ConcurrentDictionary<string, (TimeSpan start, TimeSpan end)> mealTimes = new()
        {
            ["早餐"] = (TimeSpan.Parse("05:00"), TimeSpan.Parse("09:00")),
            ["午餐"] = (TimeSpan.Parse("11:00"), TimeSpan.Parse("14:00")),
            ["晚餐"] = (TimeSpan.Parse("17:00"), TimeSpan.Parse("20:00")),
            ["夜宵"] = (TimeSpan.Parse("21:00"), TimeSpan.Parse("23:59"))
        };


        public XiGuaWatching(IMainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public async Task AutomateAdWatching(CancellationToken cancellationToken)
        {
            foreach (var key in arrTask.Keys)
            {
                if (key != "切换账号")
                {
                    arrTask[key] = string.Empty;
                }
            }
            int StopTimeSpan = 0;
            int AdTime = 0;
            bool isSwipeTop = false;
            bool isCompele = false; //是否强制结束
            bool isOthar = false; //是否进入番茄小说
            bool isAd = false; //是否完成了广告
            try
            {
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
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "服务器开小差了") && text.TextBlocks.Any(a => a.Text == "重试"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "重试");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "立即升级") && text.TextBlocks.Any(a => a.Text == "以后再说"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "以后再说");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "关闭应用") && text.TextBlocks.Any(a => a.Text == "等待"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "等待");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "添加到主屏幕") && text.TextBlocks.Any(a => a.Text == "取消") && text.TextBlocks.Any(a => a.Text == "添加"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "当前无新视频"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x");
                                if (BoxPoint != null)
                                {
                                    isCompele = true;
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("到底了")))
                            {
                                isCompele = true;
                                await ClickAsync(null, 3000, "到底了~下滑浏览领取金币");
                                continue;
                            }
                            //else if (text.TextBlocks.Any(a => a.Text == "首页") && text.TextBlocks.Any(a => a.Text == "我") && text.TextBlocks.Any(a => a.Text.Contains("开心收下") || a.Text.Contains("评价并收下") ))
                            //{
                            //    await SwipeAsync(3000, "虚假广告视频", 500, 1500, 500, 500, 200);
                            //    continue;
                            //}
                            else if (text.TextBlocks.Any(a => a.Text == "评价并收下金币"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("评价并收下金币"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "评价并关闭"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("评价并关闭"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"立即签到([\s\S]*)(\d+)金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"立即签到([\s\S]*)(\d+)金币"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看广告签到领([\s\S]*)(\d+)")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看广告签到领([\s\S]*)(\d+)"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看广告视频再赚([\s\S]*)(\d+)")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看广告视频再赚([\s\S]*)(\d+)"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看视频赚([\d\.]+)元")) && text.TextBlocks.Any(a => a.Text.Contains("老用户专享福利")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看视频赚([\d\.]+)元"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("立即签到")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("立即签到"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
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
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    }
                                }
                                else
                                {

                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"(领取奖励|继续观看|继续互动|继续领金币)"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("看广告视频再赚")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看广告视频再赚"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.Ad;
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("看广告再赚")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看广告再赚"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.Ad;
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "开心收下"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("开心收下"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.EndsWith("回任务页")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(A => A.Text.EndsWith("回任务页"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
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
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("获得开宝箱奖励")) && text.TextBlocks.Any(a => a.Text.StartsWith("我知道了")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("我知道了"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("领今日奖励")) && text.TextBlocks.Any(a => a.Text.StartsWith("签到领")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("领今日奖励"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
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
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else
                                {

                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "好的");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("签到成功")) && text.TextBlocks.Any(a => a.Text.StartsWith("好的")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("好的"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "去签到") && text.TextBlocks.Any(a => a.Text == "天天赚金币"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("去签到"));
                                if (BoxPoint != null)
                                {
                                    isOthar = true;
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "今天签到") && text.TextBlocks.Any(a => a.Text == "签到领金币"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("签到领金币"));
                                if (BoxPoint != null)
                                {
                                    isOthar = true;
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "明天继续领现金"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "明天继续领现金");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "待领取"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "待领取");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^签到\d+$")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"^签到\d+$"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
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
                            else if (text.TextBlocks.Any(a => a.Text == "开宝箱") && text.TextBlocks.Any(a => a.Text.Contains("做任务领奖励") || a.Text.Contains("做任务赚金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text =="立即领取") && !text.TextBlocks.Any(a => a.Text == "广告"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即领取");
                                if (BoxPoint != null)
                                {
                                    isOthar = true;
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.Contains("s秒后进入下场直播") || a.Text == "直播已结束"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("X"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"(\d+)s秒后进入直播间")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.EndsWith("直播已结束")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("x"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }

                            if (text.TextBlocks.Any(a => a.Text == "乐活") && text.TextBlocks.Any(a => a.Text == "抖音极速版") && text.TextBlocks.Any(a => a.Text == "今日头条") && text.TextBlocks.Any(a => a.Text == "番茄畅听"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "抖音极速版");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => new string[] { "推荐", "赚钱", "首页", "我" }.Any(x => x == a.Text)) && text.TextBlocks.Any(a => new string[] { "可领取", "去领取", "去完成", "换现金" }.Contains(a.Text)))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "赚钱");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            if ((text.TextBlocks.Any(a => a.Text.StartsWith("金币收益")) && text.TextBlocks.Any(a => a.Text.Contains("现金收益") || a.Text == "详情")) || (text.TextBlocks.Any(a => a.Text.StartsWith("集音符") || a.Text.StartsWith("限时任务") || a.Text.StartsWith("日常任务")) && text.TextBlocks.Any(a => a.Text.Contains("已预约") || a.Text == "去领取")))
                            {
                                isOthar = false;
                                if (MaxY == 0)
                                {
                                    MaxY = text.TextBlocks.Last().BoxPoints.CalculateTextBlockCenter().Y;
                                }
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
                                        var array = arrTask[Last].GetArray().ToList();
                                        array.RemoveAt(array.Count - 1);
                                        arrTask[Last] = string.Join("|", array);
                                        PagePosChange.Clear();
                                    }
                                    else
                                    {

                                        await SwipeAsync(4000, "滑动到看指定视频得金币", 100, 1200, 540, 1000, 200);
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
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }

                                #region 天天领金币
                                if (CurPage == AppPageEnum.MoneyHome && arrTask["天天领金币"].IsNotEmptyOrNull())
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("天天领金币"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.DayMoney;
                                        PagePosChange.Add("天天领金币");
                                        arrTask["天天领金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);

                                    }
                                }
                                #endregion

                                #region 一键领金币
                                if (CurPage == AppPageEnum.MoneyHome && arrTask["一键领金币"].IsNotEmptyOrNull())
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("一键领金币") || a.Text.EndsWith("一键领金币"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.OneClickMoney;
                                        PagePosChange.Add("一键领金币");
                                        arrTask["一键领金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);

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
                                        arrTask["开宝箱得金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    }
                                }
                                #endregion


                                #region 逛街得金币
                                if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"浏览低价商品(\d+)秒即得")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("逛街赚金币") || a.Text.StartsWith("逛街赚音符") || a.Text.StartsWith("逛街赚金币"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.GuangJie;
                                        PagePosChange.Add("逛街赚金币");
                                        arrTask["逛街赚金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    }
                                }
                                #endregion

                                #region 吃饭打卡赚金币
                                //早餐：05:00-09:00、午餐：11:00-14:00 、晚餐：17:00-20:00 、夜宵：21:00-24:00、
                                if (CurPage == AppPageEnum.MoneyHome && (!arrTask["吃饭打卡赚金币"].GetArray().Contains(key) && mealTimes.Any(a => currentTime >= a.Value.start && currentTime < a.Value.end) && text.TextBlocks.Any(a => a.Text.Contains("吃饭打卡赚金币"))))
                                {
                                    var array = arrTask["吃饭打卡赚金币"].GetArray().ToList();
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("吃饭打卡赚金币"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.Dining;
                                        array.Add(key);
                                        arrTask["吃饭打卡赚金币"] = string.Join("|", array);
                                        PagePosChange.Add("吃饭打卡赚金币");
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    }
                                }
                                #endregion

                                #region 看广告赚金币
                                if (CurPage == AppPageEnum.MoneyHome && AdTime > 0 && text.TextBlocks.Any(a => a.Text.StartsWith("看广告赚金币")) && (DateTime.Now - arrTask["看广告赚金币"].ToDateTime(DateTime.Now.AddMinutes(-25))).TotalMinutes >= AdTime)
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看广告赚金币"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.ViewAdMoney;
                                        PagePosChange.Add("看广告赚金币");
                                        arrTask["看广告赚金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    }
                                }
                                #endregion

                                #region 早安小岛
                                if (CurPage == AppPageEnum.MoneyHome && arrTask["早安小岛"].IsNotEmptyOrNull())
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("早安小岛"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.ZaoAnXiaoDao;
                                        PagePosChange.Add("早安小岛");
                                        arrTask["早安小岛"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);

                                    }
                                }
                                #endregion


                                #region 看指定视频得金币
                                if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, "看指定视频得(金币|音符)")) && (DateTime.Now - arrTask["看指定视频得金币"].ToDateTime(DateTime.Now.AddMinutes(-22))).TotalMinutes > 20)
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "看指定视频得(金币|音符)"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.AppointVideo;
                                        PagePosChange.Add("看指定视频得金币");
                                        arrTask["看指定视频得金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    }
                                }
                                #endregion


                                #region 看直播开宝箱
                                if (CurPage == AppPageEnum.MoneyHome && (DateTime.Now - (arrTask["看直播开宝箱"].GetArray().LastOrDefault() ?? "").ToDateTime(DateTime.Now.AddMinutes(-15))).TotalMinutes > 10)
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看直播开宝箱"));
                                    if (BoxPoint != null)
                                    {
                                        var array = arrTask["看直播开宝箱"].GetArray().ToList();
                                        int Index = text.TextBlocks.FindIndex(a => Regex.IsMatch(a.Text, @"开宝箱最高可得(\d+)金币"));
                                        if (Index > -1)
                                        {
                                            Match match = Regex.Match(text.TextBlocks[Index + 2].Text, @"(\d+)/10");
                                            if (match.Success)
                                            {
                                                for (int i = 0; i < match.Groups[1].Value.ToInt(); i++)
                                                {
                                                    array.Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                                }
                                            }
                                        }
                                        CurPage = AppPageEnum.ViewLiveRoom;
                                        PagePosChange.Add("看直播开宝箱");
                                        array.Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                        arrTask["看直播开宝箱"] = string.Join("|", array);

                                        await ClickAsync(BoxPoint.BoxPoints, 10000, BoxPoint.Text);
                                    }
                                }
                                #endregion

                                if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("免费送红包最高得")))
                                {
                                    await ClickAsync(null, 3000, "返回-免费送红包最高得");
                                }

                                if(CurPage == AppPageEnum.MoneyHome && StopTimeSpan > 60)
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
                                else if (CurPage == AppPageEnum.ZaoAnXiaoDao)
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
                                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);

                                            }
                                        }
                                        else if (text.TextBlocks.Any(a => a.Text == arrZt))
                                        {

                                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == arrZt);
                                            if (BoxPoint != null)
                                            {
                                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                                BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "发布动态");
                                                if (BoxPoint != null)
                                                {
                                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            await ClickAsync(null, 3000, "返回-首页");
                                        }
                                    }
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.Dining)
                                {
                                    #region 吃饭打卡领金币
                                    if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, "看(指定|广告)视频领(早餐|午餐|晚餐|夜宵)补贴")))
                                    {
                                        PagePosChange.Clear();
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "看(指定|广告)视频领(早餐|午餐|晚餐|夜宵)补贴"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "开心收下"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开心收下");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "去领取"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去领取");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text.StartsWith("再赚最高")))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("再赚最高"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else if (!text.TextBlocks.Any(a => a.Text.StartsWith("任务升级")) && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, "领取(早餐|午餐|晚餐|夜宵)补贴")))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "领取(早餐|午餐|晚餐|夜宵)补贴"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
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
                                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            }
                                        }
                                        else
                                        {
                                            await ClickAsync(null, 3000, "返回-吃饭打卡领金币");
                                        }
                                    }
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.DayMoney)
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
                                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                                if (text.TextBlocks.Any(a => a.Text == "抖音支付积分"))
                                                {
                                                    BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("抖音支付积分"));
                                                    if (BoxPoint != null)
                                                    {
                                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
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
                                else if (CurPage == AppPageEnum.ListenBooks)
                                {
                                    #region 听书赚金币
                                    if (text.TextBlocks.Any(a => a.Text == "免费小说"))
                                    {
                                        PagePosChange.Clear();
                                        if (text.TextBlocks.Any(a => a.Text.Contains("时长不足")))
                                        {
                                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("看广告免费领取"));
                                            if (BoxPoint != null)
                                            {
                                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            }
                                        }
                                        else if (text.TextBlocks.Any(a => a.Text == "书架") && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"([\d-\.]+)分")))
                                        {

                                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"([\d-\.]+)分"));
                                            if (BoxPoint != null)
                                            {
                                                await ClickAsync(BoxPoint.BoxPoints, 10000, BoxPoint.Text);

                                            }
                                        }
                                        else if ((DateTime.Now - arrTask["听书赚金币"].ToDateTime(DateTime.Now.AddMinutes(-22))).TotalMinutes > 1)
                                        {
                                            await ClickAsync(null, 3000, "返回-首页");
                                            await ClickAsync(null, 3000, "返回-首页");
                                        }
                                    }
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.FqTingShu)
                                {
                                    #region 去番茄畅听听书*S秒
                                    if (text.TextBlocks.Any(a => a.Text == "小说"))
                                    {
                                        PagePosChange.Clear();
                                        if (text.TextBlocks.Any(a => a.Text == "去领奖"))
                                        {
                                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("去领奖"));
                                            if (BoxPoint != null)
                                            {
                                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            }
                                        }
                                        else if ((DateTime.Now - arrTask["去番茄畅听听书"].ToDateTime(DateTime.Now.AddSeconds(-22))).TotalSeconds > 10)
                                        {

                                            await ClickAsync(null, 3000, "返回-抖音极速版App");
                                        }
                                        else
                                        {
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
                                    if (!text.TextBlocks.Any(a => a.Text.Contains("来商城")) || text.TextBlocks.Any(a => a.Text == "下单返金币"))
                                    {
                                        if (text.TextBlocks.Any(a => a.Text == "赚钱") || text.TextBlocks.Any(a => a.Text == "赚钱") || text.TextBlocks.Any(a => a.Text == "首页"))
                                        {
                                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "赚钱");
                                            if (BoxPoint != null)
                                            {
                                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            }
                                        }
                                        else
                                        {
                                            await ClickAsync(null, 3000, "浏览低价商品返回");
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "商城") && text.TextBlocks.Any(a => a.Text == "去领取"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去领取");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else if ((DateTime.Now - arrTask["逛商城赚金币"].ToDateTime(DateTime.Now.AddMinutes(-7))).TotalMinutes >= 1)
                                    {
                                        await ClickAsync(null, 3000, "逛商城赚金币返回首页");
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
                                    if (text.TextBlocks.Any(a => a.Text == "还可做任务赚金币"))
                                    {
                                    }
                                    else if ((DateTime.Now - (arrTask["看直播开宝箱"].GetArray().LastOrDefault() ?? "").ToDateTime(DateTime.Now.AddMinutes(-15))).TotalMinutes > 10)
                                    {

                                        await ClickAsync(null, 2000, "看直播开宝箱超过10分钟返回首页");
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
                                    if (text.TextBlocks.Any(a => a.Text.StartsWith("设备异常")))
                                    {
                                        await ClickAsync(null, 3000, "设备异常-返回首页");
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "一键领取"))
                                    {
                                        if (text.TextBlocks.Any(a => a.Text.Contains("明日可一键领取")) || text.TextBlocks.Any(a => a.Text == "倒计时结束后才能领取哦"))
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
                                    else if (text.TextBlocks.Any(a => a.Text.Contains("明日可一键领取")))
                                    {
                                        await ClickAsync(null, 3000, "返回-已经预约");
                                    }
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.ViewRenDian)
                                {
                                    #region 看热点3分钟赚钱
                                    if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"\d{2,2}:\d{2,2}")))
                                    {
                                        PagePosChange.Clear();
                                    }
                                    if (text.TextBlocks.Any(a => a.Text.StartsWith("再看")))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("再看"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "开宝箱" || a.Text == "领金币"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱" || a.Text == "领金币");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text == "去领取"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去领取");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text.StartsWith("看视频再得")))
                                    {

                                        await ClickAsync(null, 3000, "关闭看视频再得");
                                    }
                                    else if ((DateTime.Now - arrTask["看热点3分钟赚钱"].ToDateTime(DateTime.Now.AddMinutes(-15))).TotalMinutes > 3)
                                    {
                                        await ClickAsync(null, 3000, "看热点3分钟赚钱返回");
                                    }
                                    else
                                    {
                                        // 执行小幅度上滑
                                        await SwipeAsync(new Random().Next(2000, 10000), "逛商城赚金币", 500, 1500, 500, 500, 200);
                                    }
                                    #endregion
                                }

                                else if (CurPage == AppPageEnum.ToutiaoVideo)
                                {
                                    #region 去今日头条极速版看视频
                                    if (text.TextBlocks.Any(a => a.Text.StartsWith("@")))
                                    {
                                        PagePosChange.Clear();
                                    }
                                    int i = 0;
                                    while (i < arrTask["去今日头条极速版看视频"].ToInt(3))
                                    {
                                        int r = new Random().Next(8000, 13000);
                                        i = i + r;
                                        await SwipeAsync(r, "看短剧赚更多金币刷视频", 540, 1500, 540, 1000, 200);
                                    }
                                    await ClickAsync(null, 3000, "返回");
                                    await ClickAsync(null, 3000, "返回");
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.FanqieBook)
                                {
                                    #region 去番茄小说看书
                                    PagePosChange.Clear();
                                    await ClickAsync(null, 3000, "返回");
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.ShortDramaCoins)
                                {
                                    #region 看短剧赚更多金币
                                    if (text.TextBlocks.Any(a => a.Text.StartsWith("首页")))
                                    {
                                        PagePosChange.Clear();
                                    }
                                    if ((DateTime.Now - arrTask["看短剧赚更多金币"].ToDateTime(DateTime.Now.AddMinutes(-20))).TotalMinutes > 10)
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "赚钱");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else
                                    {
                                        await SwipeAsync(new Random().Next(8000, 13000), "看短剧赚更多金币刷视频", 540, 1500, 540, 1000, 200);
                                    }
                                    #endregion
                                }



                                //当上面没有操作时就执行下面
                                if (!IsOperate)
                                {
                                    if (text.TextBlocks.Any(a => a.Text.Trim() == "首页") && text.TextBlocks.Any(a => a.Text.Trim() == "消息") && text.TextBlocks.Any(a => a.Text.Trim() == "我"))
                                    {
                                        string[] values = arrTask["赚钱首页"].GetArray();

                                        var recentCount = values.Count(x => (DateTime.Now - DateTime.ParseExact(x, "yyyy-MM-dd HH:mm:ss", null)).TotalMinutes <= 5);
                                        if (recentCount >= 15)
                                        {
                                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("赚钱"));
                                            if (BoxPoint != null)
                                            {
                                                arrTask["赚钱首页"] = string.Format("{0}|{1}", string.Join("|", values), $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            }
                                        }
                                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("看完视频最高")))
                                        {
                                            await Task.Delay(30000);
                                        }
                                        else if (text.TextBlocks.Any(a => a.Text == "赚钱") || text.TextBlocks.Any(a => a.Text == "赚钱") || text.TextBlocks.Any(a => a.Text == "首页"))
                                        {
                                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "赚钱");
                                            if (BoxPoint != null)
                                            {
                                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            }
                                        }
                                        else
                                        {
                                            await SwipeAsync(new Random().Next(10000, 23000), "赚钱首页刷视频", 540, 1500, 540, 1000, 200);
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
                                        await ClickAsync(null, 3000, "返回-直播间");
                                    }
                                    else if (text.TextBlocks.Any(a => a.Text.Contains("逛街得金币")) && text.TextBlocks.Any(a => a.Text == "0秒"))
                                    {
                                        await ClickAsync(null, 3000, "逛街得金币-返回");
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

                                    else if (!isOthar && _mainViewModel.CurDevice.GetAppStatus(packageName) != AdvancedSharpAdbClient.Models.AppStatus.Foreground)
                                    {
                                        await StartAppAsync(3000);
                                    }
                                    else if (CurPage == AppPageEnum.GuangJie)
                                    {
                                        await SwipeAsync(2000, "返回-停留页面超过60秒", 500, 500, 500, 1500, 200);
                                    }
                                    #region 页面停留超时
                                    else if ((DateTime.Now - lastClickTime).TotalSeconds > 60)
                                    {
                                        await ClickAsync(null, 2000, "返回-停留页面超过60秒");
                                    }
                                    #endregion
                                }

                                #endregion
                            }

                            IsOperate = false;
                        }
                        else
                        {
                            LogMessage($"OCR失败结果为空");
                            await Task.Delay(3000);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"自动化过程出错: {ex.Message}");
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
        private async Task ClickAsync(List<OCRPoint>? points = null, int delay = 0, string text = "")
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
        private async Task SwipeAsync(int delay, string text, int startX, int startY, int endX, int endY, int duration)
        {
            IsOperate = true;
            lastClickTime = DateTime.Now;
            await _mainViewModel.SwipeAsync(delay, text, startX, startY, endX, endY, duration);
        }

        /// <summary>
        /// 运行App
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        private async Task StartAppAsync(int delay = 0)
        {
            IsOperate = true;
            lastClickTime = DateTime.Now;
            await _mainViewModel.CurDevice.StartAppAsync(packageName);
            if (delay > 0)
            {
                await Task.Delay(delay);
            }
        }

        /// <summary>
        /// 返回首页
        /// </summary>
        /// <param name="delay"></param>
        private async Task ClickHomeButtonAsync(int delay = 0)
        {
            IsOperate = true;
            lastClickTime = DateTime.Now;
            await _mainViewModel.ClickHomeButtonAsync(delay);
        }

        /// <summary>
        /// 记录日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        private void LogMessage(string message)
        {
            _mainViewModel.LogMessage(message);
        }

        /// <summary>
        /// 设置/更新当前页面
        /// </summary>
        /// <param name="appPage"></param>
        public void UpdateAppPageEnum(AppPageEnum appPage)
        {
            CurPage = appPage;
        }
    }
}