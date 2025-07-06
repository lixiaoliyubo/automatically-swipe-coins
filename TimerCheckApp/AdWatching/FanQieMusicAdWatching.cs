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
public class FanQieMusicAdWatching : AdWatchingBase
{
   
    public FanQieMusicAdWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
        : base(logService, deviceService, cacheService, sqlSugarService)
    {
        // 初始化任务列表
        arrTask = new()
        {
            new TaskItem("喝水打卡领金币"),
            new TaskItem("每日三餐补贴"),
            new TaskItem("任务赚"),
            new TaskItem("百度智慧回答"),
            new TaskItem("搜索领金币"),
            new TaskItem("阅读赚金币"),
            new TaskItem("逛商城赚金币"),
            new TaskItem("看视频文章领金币"),
            new TaskItem("每日签到领现金"),
            new TaskItem("看看广告领金币奖励")
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
                        else if (text.TextBlocks.Any(a => a.Text == "我要反馈") && text.TextBlocks.Any(a => a.Text == "五星好评"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "五星好评");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "优先体验") && text.TextBlocks.Any(a => a.Text == "以后再说"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "以后再说");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "添加到主屏幕" || a.Text == "添加至桌面") && text.TextBlocks.Any(a => a.Text == "取消") && text.TextBlocks.Any(a => a.Text == "添加"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "开启推送通知") && text.TextBlocks.Any(a => a.Text == "取消"))
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

                        #region 弹出层面
                        else if (text.TextBlocks.Any(a => a.Text == "天天抽奖") && text.TextBlocks.Any(a => a.Text == "立即前往"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即前往");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "你有大额金币奖励可领取") && text.TextBlocks.Any(a => a.Text == "立即领取"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即领取");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, "立即签到\\+(\\d+)金币")) && text.TextBlocks.Any(a => a.Text.StartsWith("今日签到")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "立即签到\\+(\\d+)金币"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "看视频免费领时长" || Regex.IsMatch(a.Text, @"看视频领(\d+)分钟时长") || a.Text.Contains("再看1个享全天免费听")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "看视频免费领时长" || Regex.IsMatch(a.Text, @"看视频领(\d+)分钟时长") || a.Text.Contains("再看1个享全天免费听"));
                            if (BoxPoint != null)
                            {
                                device.CurPage = AppPageEnum.Ad;
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("再看一个继续领取") || a.Text.StartsWith("看视频再领") || a.Text.StartsWith("看视频签到") || a.Text.StartsWith("看视频最高可领") || a.Text.StartsWith("看视频领最高") || a.Text.StartsWith("看视频最多") || a.Text.StartsWith("看视频最高再领") || a.Text.StartsWith("看视频最高再得") || a.Text.StartsWith("看视频再得")))
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
                        else if (text.TextBlocks.Any(a => a.Text == "去逛街继续领取"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a =>  a.Text == "去逛街继续领取");
                            if (BoxPoint != null)
                            {
                                device.CurPage = AppPageEnum.GuangJie;
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
                        else if (text.TextBlocks.Any(a => a.Text == "看视频立即领取") && text.TextBlocks.Any(a => a.Text == "恭喜你"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "看视频立即领取");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
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
                        else if (text.TextBlocks.Any(a => a.Text == "天天抽奖") && text.TextBlocks.Any(a => a.Text == "立即前往" || a.Text == "你有一次机会"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即前往" || a.Text == "你有一次机会");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 10000, BoxPoint.Text);
                                continue;
                            }
                            else {

                                await ClickAsync(device, _device, null, 3000, "返回-首页");
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
                        else if (text.TextBlocks.Any(a => a.Text == "领取奖励") && text.TextBlocks.Any(a => a.Text == "以后再说"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "以后再说");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
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
                                    var pos = BoxPoint.BoxPoints.OrderByDescending(a => a.X).FirstOrDefault();
                                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(pos.X + 50, pos.Y) }, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("本次免费")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("本次免费"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("免费领时长") || a.Text.EndsWith("时长已用完")) && text.TextBlocks.Any(a => a.Text.StartsWith("看视频领")) && !text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"05:(\d{2}):(\d{2})")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看视频领"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        #endregion

                        if (text.TextBlocks.Any(a => a.Text == "福利中心" ||  a.Text == "日常福利" || a.Text == "阅读赚金币" || a.Text == "时长赚金币" || a.Text == "每日听歌赚钱" || a.Text == "看视频赚金币")  || text.TextBlocks.Any(a => a.Text == "去提现"))
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


                            if (text.TextBlocks.Any(a => a.Text.StartsWith("开宝箱得金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("开宝箱得金币"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"领(\d+)金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"领(\d+)金币"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "翻倍领取" || a.Text == "加倍领取" || a.Text == "去加倍"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "翻倍领取" || a.Text == "加倍领取" || a.Text == "去加倍");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("待领取") || a.Text == "立即领取"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("待领取") || a.Text == "立即领取");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看视频领(\d+)次奖励")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去观看");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Ad;
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "看视频赚金币"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即观看");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }



                            #region 阅读赚金币
                            if (device.CurPage == AppPageEnum.MoneyHome && (DateTime.Now - arrTask.GetTaskItemValue("阅读赚金币").ToDateTime(DateTime.Now.AddMinutes(-25))).TotalMinutes >= 20)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("阅读赚金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.ReadLoad;
                                    PagePosChange.Add("阅读赚金币");
                                    arrTask.UpdateTaskItem("阅读赚金币", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;

                                }
                            }
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
                                    if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^([3-9]+):(\d+)$")))
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
                                #endregion
                            }

                            else if (device.CurPage == AppPageEnum.ReadLoad)
                            {
                                #region 阅读赚金币

                                await SwipeAsync(device, _device, new Random().Next(10000, 15000), "阅读赚金币左滑", 800, 1000, 100, 1000, 200);
                                #endregion
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"领(早饭|午饭|晚饭|夜宵)补贴(\d+)金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"领(早饭|午饭|晚饭|夜宵)补贴(\d+)金币"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }


                            //当上面没有操作时就执行下面
                            if (!IsOperate)
                            {
                                #region 特殊页面
                                if (text.TextBlocks.Any(a => a.Text.StartsWith("立即下载")) && text.TextBlocks.Any(a => a.Text == "侵权投诉"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-立即下载");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么")) || text.TextBlocks.Any(a => a.Text == "关注"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-直播间返回");
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("我的收益")) || text.TextBlocks.Any(a => a.Text == "立即提现"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-首页");
                                }
                                #endregion

                                #region 页面停留超时
                                else if (_deviceService.CurAdbClient.GetAppStatus(_device, device.PackageName) != AdvancedSharpAdbClient.Models.AppStatus.Foreground)
                                {
                                    await StartAppAsync(device, device.PackageName);
                                    await Task.Delay(3000);
                                }
                                else if ((DateTime.Now - device.LastClickTime).TotalSeconds > 60)
                                {
                                    await SwipeAsync(device, _device, 1000, "返回-停留页面超过60秒", 100, 1000, 500, 1000, 200);
                                    await ClickAsync(device, _device, null, 2000, "返回-停留页面超过60秒");
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