using AdAuto.enums;
using AdAuto.Extensions;
using AdAuto.Helper;
using DynamicData;
using OpenCvSharp;
using PaddleOCRSharp;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using Yuandl.Common.Helper;

namespace AdAuto.ViewModels
{
    /// <summary>
    /// 抖音广告观看自动化实现
    /// </summary>
    public class TouTiaoWatching : IAdWatching
    {
        private readonly IMainViewModel _mainViewModel;
        private AppPageEnum CurPage = AppPageEnum.Node;
        public DateTime lastClickTime = DateTime.Now;
        private bool IsOperate = false;
        private string packageName = "com.ss.android.article.lite";

        public ConcurrentDictionary<string, string> arrTask { get; set; } = new()
        {
            ["喝水打卡领金币"] = string.Empty,
            ["每日三餐补贴"] = string.Empty,
            ["任务赚"] = string.Empty,
            ["百度智慧回答"] = string.Empty,
            ["搜索领金币"] = string.Empty,
            ["逛商城赚金币"] = string.Empty,
            ["看视频文章领金币"] = string.Empty,
            ["每日签到领现金"] = string.Empty,
            ["看看广告领金币奖励"] = string.Empty,

        };
        private ConcurrentDictionary<string, (TimeSpan start, TimeSpan end)> mealTimes = new()
        {
            ["早餐"] = (TimeSpan.Parse("07:00"), TimeSpan.Parse("09:00")),
            ["午餐"] = (TimeSpan.Parse("11:00"), TimeSpan.Parse("13:00")),
            ["晚餐"] = (TimeSpan.Parse("17:00"), TimeSpan.Parse("19:00")),
            ["夜宵"] = (TimeSpan.Parse("22:00"), TimeSpan.Parse("23:59"))
        };


        public TouTiaoWatching(IMainViewModel mainViewModel)
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
            int MaxY = 0;
            bool isSwipeTop = false;
            bool isCompele = false; //是否强制结束
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
                            else if (text.TextBlocks.Any(a => a.Text == "关闭应用") && text.TextBlocks.Any(a => a.Text == "等待"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "等待");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "立即下载") && text.TextBlocks.Any(a => a.Text == "残忍离开"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "残忍离开");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("开心收下")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("开心收下"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "继续观看") && text.TextBlocks.Any(a => a.Text == "换一个"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("看视频再得") || a.Text.StartsWith("看视频再领")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看视频再得") || a.Text.StartsWith("看视频再领"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.Ad;
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看视频领(\d+)金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看视频领(\d+)金币"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.Ad;
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
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
                            else if (text.TextBlocks.Any(a => a.Text == "开宝箱") && text.TextBlocks.Any(a => a.Text.Contains("关注")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "开宝箱得金币"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开宝箱得金币");
                                if (BoxPoint != null)
                                {
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
                            else if (text.TextBlocks.Any(a => a.Text == "浏览更多直播") && text.TextBlocks.Any(a => a.Text == "退出不看了"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "退出不看了");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => new string[] { "现金", "积分" }.Any(x => x == a.Text)) || (text.TextBlocks.Any(a => a.Text.StartsWith("积分收益")) && text.TextBlocks.Any(a => a.Text.StartsWith("现金收益"))))
                            {
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

                                //当上面没有操作时就执行下面
                                if (!IsOperate)
                                {
                                    if (text.TextBlocks.Any(a => a.Text.Trim() == "首页") && text.TextBlocks.Any(a => a.Text.Trim() == "任务") && text.TextBlocks.Any(a => a.Text.Trim() == "我的"))
                                    {
                                        string[] values = arrTask["任务"].GetArray();

                                        var recentCount = values.Count(x => (DateTime.Now - DateTime.ParseExact(x, "yyyy-MM-dd HH:mm:ss", null)).TotalMinutes <= 5);
                                        if (recentCount >= 15)
                                        {
                                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("视频"));
                                            if (BoxPoint != null)
                                            {
                                                arrTask["任务"] = string.Format("{0}|{1}", string.Join("|", values), $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
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
                                    #endregion

                                    //else if (CurPage == AppPageEnum.GuangJie)
                                    //{
                                    //    await SwipeAsync(2000, "返回-停留页面超过60秒", 500, 500, 500, 1500, 200);
                                    //}
                                    #region 页面停留超时
                                    else if (_mainViewModel.CurDevice.GetAppStatus(packageName) != AdvancedSharpAdbClient.Models.AppStatus.Foreground)
                                    {
                                        await StartAppAsync(3000);
                                    }
                                    else if ((DateTime.Now - lastClickTime).TotalSeconds > 60)
                                    {
                                        await SwipeAsync(1000, "返回-停留页面超过60秒", 100, 1000, 500, 1000, 200);
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