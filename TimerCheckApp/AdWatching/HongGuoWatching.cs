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
/// 红果广告观看自动化实现
/// </summary>
public class HongGuoWatching : AdWatchingBase
{
    public HongGuoWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
        : base(logService, deviceService, cacheService, sqlSugarService)
    {
        // 初始化任务列表
        arrTask = new List<TaskItem>
        {
            // 初始化任务列表，可以根据需要添加任务项
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
        bool isSwipeTop = false;
        try
        {
            List<string> PagePosChange = new List<string>();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    OCRResult? text = await _deviceService.GetOCRText(device);
                    if (text == null) throw new Exception("OCR识别识别");
                    if (text?.Text?.Length > 1)
                    {

                        #region 识别本文逻辑
                        if (device.CurPage == AppPageEnum.SlideUp)
                        {
                            if (text.TextBlocks.Any(a => a.Text.Contains("上滑继续观看短剧")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("上滑继续观看短剧"));
                                if (BoxPoint != null)
                                {
                                    var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                    await SwipeAsync(device, _device, 0, "上滑继续观看短剧", pos.X, pos.Y - 50, pos.X, pos.Y - 500, 200);
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.Contains("上滑继续看短剧")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("上滑继续看短剧"));
                                if (BoxPoint != null)
                                {
                                    var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                    await SwipeAsync(device, _device, 0, "上滑继续看短剧", pos.X, pos.Y - 20, pos.X, pos.Y - 500, 200);
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
                        else if (text.TextBlocks.Any(a => a.Text == "立即下载") && text.TextBlocks.Any(a => a.Text == "残忍离开"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "残忍离开");
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
                        else if (text.TextBlocks.Any(a => a.Text == "添加到主屏幕") && text.TextBlocks.Any(a => a.Text == "取消") && text.TextBlocks.Any(a => a.Text == "添加"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "下次再说") && text.TextBlocks.Any(a => a.Text == "加入收藏"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "下次再说");
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
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("开心收下")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("开心收下"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("上滑继续观看短剧")))
                        {
                            // 执行小幅度上滑
                            await SwipeAsync(device, _device, SwipeDirection.Up, 4000, "上滑继续观看短剧");
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("看视频再得") || a.Text.StartsWith("看视频再领") || a.Text.StartsWith("看视频最高再得")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看视频再得") || a.Text.StartsWith("看视频再领") || a.Text.StartsWith("看视频最高再得"));
                            if (BoxPoint != null)
                            {
                                device.CurPage = AppPageEnum.Ad;
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("s后进入直播间|取消")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("s后进入直播间|取消"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints.OrderByDescending(a => a.X).Skip(0).Take(1).ToList(), 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看视频最高再领(\d+)金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看视频最高再领(\d+)金币"));
                            if (BoxPoint != null)
                            {
                                device.CurPage = AppPageEnum.Ad;
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看视频继续翻(千}百|十|个)位卡")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看视频继续翻(千}百|十|个)位卡"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "开宝箱得金币"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱得金币");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("立即翻卡")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("立即翻卡"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("继续赚钱")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("继续赚钱"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
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
                        else if (text.TextBlocks.Any(a => a.Text == "继续观看") && text.TextBlocks.Any(a => a.Text == "坚持退出"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }

                        #endregion

                        #region 页面层面
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
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"点击领(\d+)金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"点击领(\d+)金币"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("立即领取")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("立即领取"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "看视频补领一次补贴"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "看视频补领一次补贴");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
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

                        #endregion


                        /*
                        else if (text.TextBlocks.Any(a => a.Text == "开宝箱") && text.TextBlocks.Any(a => a.Text.Contains("关注")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱");
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
                        else if (text.TextBlocks.Any(a => a.Text == "浏览更多直播") && text.TextBlocks.Any(a => a.Text == "退出不看了"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "退出不看了");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }*/
                        else if (text.TextBlocks.Any(a => new string[]{ "现金" , "积分" }.Any(x => x == a.Text)) || (text.TextBlocks.Any(a => a.Text.StartsWith("积分收益")) && text.TextBlocks.Any(a => a.Text.StartsWith("现金收益"))))
                        {
                            if (MaxY == 0)
                            {
                                MaxY = text.TextBlocks.Last().BoxPoints.OrderByDescending(a => a.Y).ToList().CalculateTextBlockCenter().Y;
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
                                else
                                {

                                    await SwipeAsync(device, _device, SwipeDirection.Up, 4000, "滑动到看指定视频得金币", 200);
                                    continue;
                                }
                            }




                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("免费送红包最高得")))
                            {
                                await ClickAsync(device, _device, null, 3000, "返回-免费送红包最高得");
                            }

                            if (device.CurPage == AppPageEnum.MoneyHome && StopTimeSpan > 60)
                            {
                                await SwipeAsync(device, _device, SwipeDirection.Down, 3000, "在首页停留时间超过60秒自动下滑第一次");
                            }
                            else if (device.CurPage == AppPageEnum.MoneyHome && StopTimeSpan > 30)
                            {
                                if (isSwipeTop)
                                {
                                    await SwipeAsync(device, _device, SwipeDirection.Up, 3000, "在首页停留时间超过30秒自动上滑");
                                }
                                else
                                {
                                    await SwipeAsync(device, _device, SwipeDirection.Down, 3000, "在首页停留时间超过30秒自动下滑");
                                }
                                isSwipeTop = !isSwipeTop;

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

                            if (device.CurPage == AppPageEnum.Ad)
                            {
                                #region 广告
                                 if (text.TextBlocks.Any(a => a.Text == "关注"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回");
                                }

                                #endregion
                            }
                            //当上面没有操作时就执行下面
                            if (!IsOperate)
                            {
                                #region 特殊页面
                                if (text.TextBlocks.Any(a => a.Text.StartsWith("立即下载")) && text.TextBlocks.Any(a => a.Text == "侵权投诉"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-立即下载");
                                }
                                else if (text.TextBlocks.Count > 2 && text.TextBlocks[2].Text == "拼多多")
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-立即下载");
                                }
                                #endregion

                                //else if (device.CurPage == AppPageEnum.GuangJie)
                                //{
                                //    await SwipeAsync(device, _device, SwipeDirection.Down, 2000, "返回-停留页面超过60秒");
                                //}
                                #region 页面停留超时
                                else if (_deviceService.CurAdbClient.GetAppStatus(_device, device.PackageName) != AdvancedSharpAdbClient.Models.AppStatus.Foreground)
                                {
                                    await StartAppAsync(device, device.PackageName);
                                    await Task.Delay(3000);
                                }
                                else if((DateTime.Now - device.LastClickTime).TotalSeconds > 60)
                                {
                                    if (text.TextBlocks.Any(a => a.Text.Trim() == "首页") && text.TextBlocks.Any(a => a.Text.Trim() == "领现金") && text.TextBlocks.Any(a => a.Text.Trim() == "我的"))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领现金");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else
                                    {

                                        await SwipeAsync(device, _device, SwipeDirection.Down, 1000, "返回-停留页面超过60秒", 200);
                                        await ClickAsync(device, _device, null, 2000, "返回-停留页面超过60秒");
                                    }
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