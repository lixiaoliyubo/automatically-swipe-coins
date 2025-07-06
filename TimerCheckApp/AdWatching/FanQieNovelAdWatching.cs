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
public class FanQieNovelAdWatching : AdWatchingBase
{

    public FanQieNovelAdWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
        : base(logService, deviceService, cacheService, sqlSugarService)
    {
        // 初始化任务列表
        arrTask = new()
        {
            new TaskItem("看广告领金币")
        };

        // 初始化用餐时间（如果需要）
        mealTimes = new()
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
        // 重置任务状态
        arrTask.ResetTaskStatus();
        int StopTimeSpan = 0;
        int MaxY = 0;
        bool isSwipeTop = false;
        try
        {
            List<string> PagePosChange = new List<string>();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
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
                        else if (text.TextBlocks.Any(a => a.Text == "恭喜你获得奖励权益") && text.TextBlocks.Any(a => a.Text == "我知道了"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "我知道了");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 1000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "开启推送提醒") && text.TextBlocks.Any(a => a.Text == "取消"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "无法关闭") && text.TextBlocks.Any(a => a.Text == "举报广告"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "举报广告");
                            if (BoxPoint != null)
                            {
                                var pos = BoxPoint.BoxPoints.OrderByDescending(a => a.Y).FirstOrDefault();
                                await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(pos.X, pos.Y + 800) }, 2000, BoxPoint.Text);
                                continue;
                            }
                        }

                        #endregion


                        if (device.CurPage == AppPageEnum.SlideUp)
                        {
                            if (text.TextBlocks.Any(a => a.Text.Contains("上滑继续观看短剧")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("上滑继续观看短剧"));
                                if (BoxPoint != null)
                                {
                                    await SwipeAsync(device, _device, SwipeDirection.Up, 0, "上滑继续观看短剧", 200);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.Contains("上滑继续看短剧")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("上滑继续看短剧"));
                                if (BoxPoint != null)
                                {
                                    await SwipeAsync(device, _device, SwipeDirection.Up, 0, "上滑继续看短剧", 200);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "看视频") && text.TextBlocks.Any(a => a.Text == "退出自动阅读"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("看视频"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 0, BoxPoint.Text);
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看到(\d+)集")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看到(\d+)集"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "剧场") && text.TextBlocks.Any(a => a.Text == "我的"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("我的"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 0, BoxPoint.Text);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "点评此书") && text.TextBlocks.Any(a => a.Text == "取消"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 0, BoxPoint.Text);
                                }
                            }
                            //else if (isCompele)
                            //{
                            //    var BoxText = text.TextBlocks.LastOrDefault(a => a.Text.Length > 10);
                            //    if (BoxText != null && text.TextBlocks.Count(a => a.Text.Length > 10) > 2)
                            //    {
                            //        await ClickAsync(device, _device, BoxText.BoxPoints, 0, BoxText.Text);
                            //        isCompele = !text.TextBlocks.Any(a => a.Text == "听");
                            //    }
                            //}
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("领取成功")))
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
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么")))
                            {
                                await ClickAsync(device, _device, null, 0, "返回-退出直播间");
                            }
                            await Task.Delay(3000);
                            continue;
                        }


                        #region 弹出层面
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"立即签到([\s\S]*)(\d+)金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"立即签到([\s\S]*)(\d+)金币"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("再看一个继续领取") || a.Text.StartsWith("看视频再领") || a.Text.StartsWith("看视频签到") || a.Text.StartsWith("看视频最高可领") ||a.Text.StartsWith("看视频领最高") || a.Text.StartsWith("看视频最多") || a.Text.StartsWith("看视频最高再领") || a.Text.StartsWith("看视频最高再得") || a.Text.StartsWith("看视频再得")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("再看一个继续领取") || a.Text.StartsWith("看视频再领") || a.Text.StartsWith("看视频签到") || a.Text.StartsWith("看视频最高可领") || a.Text.StartsWith("看视频领最高") || a.Text.StartsWith("看视频最多") || a.Text.StartsWith("看视频最高再领") || a.Text.StartsWith("看视频最高再得") || a.Text.StartsWith("看视频再得"));
                            if (BoxPoint != null)
                            {
                                device.CurPage = AppPageEnum.Ad;
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看广告再领(\d+)金币") || Regex.IsMatch(a.Text, @"看视频领最多(\d+)金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看广告再领(\d+)金币") || Regex.IsMatch(a.Text, @"看视频领最多(\d+)金币"));
                            if (BoxPoint != null)
                            {
                                device.CurPage = AppPageEnum.Ad;
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"预约最高领(\d+)金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"预约最高领(\d+)金币"));
                            if (BoxPoint != null)
                            {
                                device.CurPage = AppPageEnum.DayMoney;
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "继续观看") && text.TextBlocks.Any(a => a.Text == "坚持退出"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "继续互动") && text.TextBlocks.Any(a => a.Text == "坚持退出"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续互动");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "点击领取") && text.TextBlocks.Any(a => a.Text == "已完成"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "点击领取");
                            if (BoxPoint != null)
                            {
                                device.CurPage = AppPageEnum.Ad;
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                await SwipeAsync(device, _device, SwipeDirection.Up, 20000, "赚钱首页刷视频", 200);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "开心收下"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开心收下");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "领取奖励") && text.TextBlocks.Any(a => a.Text == "坚持退出"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领取奖励");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("领取成功")))
                        {
                            if (text.TextBlocks.Any(a => a.Text == "X" || a.Text == "x" || a.Text == "×"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x" || a.Text == "×");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else
                            {

                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("领取成功"));
                                if (BoxPoint != null)
                                {
                                    var pos = BoxPoint.BoxPoints.OrderByDescending(a => a.X).FirstOrDefault();
                                    await ClickAsync(device, _device,  new List<OCRPoint>() { new OCRPoint(pos.X + 50, pos.Y)  }, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                        }

                        #endregion


                        if (text.TextBlocks.Any(a => a.Text == "福利中心") && text.TextBlocks.Any(a => a.Text == "福利" || a.Text == "金币" || a.Text == "书城" || a.Text == "书架"))
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

                            if (text.TextBlocks.Any(a => a.Text == "开宝箱得金币"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱得金币");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Ad;
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"点击领(\d+)金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"点击领(\d+)金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Ad;
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if(text.TextBlocks.Any(a => a.Text == "翻倍领取"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "翻倍领取");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Ad;
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("看视频加倍")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看视频加倍"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Ad;
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if(text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看视频多赚(\d+)金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("去看视频"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Ad;
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("立即领取")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("立即领取"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Ad;
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "领取"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领取");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Ad;
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "看视频补领一次补贴"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "看视频补领一次补贴");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Ad;
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"领(早饭|午饭|晚饭|夜宵)补贴(\d+)金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"领(早饭|午饭|晚饭|夜宵)补贴(\d+)金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Ad;
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }

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
                            if (text.TextBlocks.Any(a => a.Text == "看视频补领一次补贴"))
                            {

                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "看视频补领一次补贴");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, "领(早餐|午餐|晚餐|夜宵)补贴(\\d+)金币")))
                            {
                                PagePosChange.Clear();
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "领(早餐|午餐|晚餐|夜宵)补贴(\\d+)金币"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "待领取"))
                            {

                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "待领取");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }

                            if (device.CurPage == AppPageEnum.GuangJie)
                            {
                                #region 逛街赚钱、逛街得金币
                                if (text.TextBlocks.Any(a => a.Text == "搜索"))
                                {
                                    PagePosChange.Clear();
                                    if (text.TextBlocks.FindIndex(a => Regex.IsMatch(a.Text, @"^(\d+):(\d+)$")) > 5 && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^(\d+):(\d+)$")))
                                    {
                                        await ClickAsync(device, _device, null, 3000, "返回-赚钱首页");
                                    }
                                    else
                                    {
                                        await SwipeAsync(device, _device, SwipeDirection.Up, new Random().Next(3000, 3000), "赚钱首页刷视频", 200);
                                    }
                                }
                                #endregion
                            }
                            else if (device.CurPage == AppPageEnum.DayMoney)
                            {
                                #region 天天预约领金币
                                if (text.TextBlocks.Any(a => a.Text == "立即预约领取"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即预约领取");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "提醒我来领金币"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "提醒我来领金币");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        await ClickAsync(device, _device, null, 2000, "立即预约领取返回");
                                    }
                                }
                                #endregion
                            }
                            else if (device.CurPage == AppPageEnum.Ad)
                            {
                                #region 广告
                                if (text.TextBlocks.Any(a => a.Text == "关注"))
                                {
                                    await ClickAsync(device, _device, null, 2000, "直播间返回-关注");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么")))
                                {
                                    await ClickAsync(device, _device, null, 2000, "直播间返回-说点什么");
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "已到账"))
                                {
                                    if (text.TextBlocks.Any(a => a.Text == "查看详情"))
                                    {

                                        await ClickAsync(device, _device, null, 2000, "返回首页");
                                    }
                                    else
                                    {

                                        await SwipeAsync(device, _device, SwipeDirection.Up, 1000, "上滑继续看广告", 200);
                                    }
                                }
                                #endregion
                            }
                            else
                            {

                            }

                            //当上面没有操作时就执行下面
                            if (!IsOperate)
                            {
                                #region 页面停留超时
                                if (text.TextBlocks.Any(a => a.Text.Contains("说点什么")) && text.TextBlocks.Any(a => a.Text.Contains("直播")))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-直播间");
                                }
                                else if (_deviceService.CurAdbClient.GetAppStatus(_device, device.PackageName) != AdvancedSharpAdbClient.Models.AppStatus.Foreground)
                                {
                                    await StartAppAsync(device, device.PackageName);
                                    await Task.Delay(3000);
                                }
                                else if ((DateTime.Now - device.LastClickTime).TotalSeconds > 60)
                                {
                                    await SwipeAsync(device, _device, SwipeDirection.Down, 1000, "返回-停留页面超过60秒", 200);
                                    await ClickAsync(device, _device, null, 2000, "返回-停留页面超过60秒");
                                }
                                #endregion
                            }

                            #endregion
                        }

                        IsOperate = false;

                        #endregion
                    }
                    else
                    {
                        _logService.LogWarning($"OCR失败结果为空", device.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"自动化过程出错: {ex.Message}", device.Id);
                }
                await Task.Delay(1000, cancellationToken).ContinueWith(t => {

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