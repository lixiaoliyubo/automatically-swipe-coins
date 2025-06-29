using AdAuto.enums;
using AdAuto.Extensions;
using PaddleOCRSharp;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Yuandl.Common.Helper;

namespace AdAuto.ViewModels
{
    /// <summary>
    /// 快手广告观看自动化实现
    /// </summary>
    public class KuaishouAdWatching : IAdWatching
    {
        private readonly IMainViewModel _mainViewModel;
        private AppPageEnum CurPage = AppPageEnum.Node;
        public DateTime lastClickTime = DateTime.Now;
        private bool IsOperate = false;
        private string packageName = "com.kuaishou.nebula";



        public ConcurrentDictionary<string, string> arrTask { get; set; } = new()
        {
            ["看广告得金币"] = string.Empty,
            ["看视频赚金币"] = string.Empty,
            ["看指定视频赚金币"] = string.Empty,
            ["刷广告视频赚金币"] = string.Empty,
            ["搜索赚金币"] = string.Empty,
            ["切换账号"] = "星月飞天" 
        };

        public KuaishouAdWatching(IMainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }
        public async Task AutomateAdWatching(CancellationToken cancellationToken)
        {

            try
            {
                bool isSwipeTop = false;
                int StopTimeSpan = 0;
                List<string> PagePosChange = new List<string>();
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        OCRResult? text = await _mainViewModel.RecognizeText();
                        if (text?.Text?.Length > 1)
                        {
                            if (text.TextBlocks.Any(a => a.Text == "添加到主屏幕") && text.TextBlocks.Any(a => a.Text == "取消") && text.TextBlocks.Any(a => a.Text == "添加"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "领取成功") && text.TextBlocks.Any(a => a.Text == "X" || a.Text == "x" || a.Text == "×"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x" || a.Text == "×");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "领取奖励") && text.TextBlocks.Any(a => a.Text == "坚持退出" || a.Text == "放弃奖励"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领取奖励");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "继续观看") && text.TextBlocks.Any(a => a.Text == "放弃奖励"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "继续赚钱") && text.TextBlocks.Any(a => a.Text == "任务完成奖励"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续赚钱");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "去完成任务") && text.TextBlocks.Any(a => a.Text == "放弃奖励"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "放弃奖励");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("已成功领取")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("已成功领取"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.StartsWith("已可继续看剧")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("已可继续看剧"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"点可领(\d+)金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"点可领(\d+)金币"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"去看广告得最高(\d+)金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"去看广告得最高(\d+)金币"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.ViewAdMoney;
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"(\d+)/5")) && text.TextBlocks.Any(a => a.Text == "立即观看"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"(\d+)/5"));
                                if (BoxPoint?.Text != "5/5")
                                {
                                    // 执行小幅度上滑
                                    await SwipeAsync(10000, "滑动-逛街得金币", 500, 1500, 500, 500, 200);
                                }
                                else
                                {
                                    await ClickAsync(null, 3000);
                                }
                                continue;
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"再逛(\d+)秒")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"再逛(\d+)秒"));
                                if (BoxPoint != null)
                                {
                                    Match match = Regex.Match(BoxPoint.Text, @"再逛(\d+)秒");
                                    await Task.Run(async () =>
                                    {
                                        int Delay = match.Groups[1].Value.ToInt();
                                        while (Delay > 0)
                                        {
                                            await SwipeAsync(3000, "滑动-逛街得金币", 500, 1500, 500, 500, 200);
                                            await SwipeAsync(3000, "滑动-逛街得金币", 500, 500, 500, 1500, 200);
                                            Delay -= 6;
                                        }
                                    });
                                    await ClickAsync(null, 3000, "逛商城返回");
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Slice(0, 5).Any(a => a.Text == "关注"))
                            {
                                await ClickAsync(null, 3000, "直播间返回");
                                continue;
                            }

                            if (text.TextBlocks.Any(a => a.Text == "任务中心") || (text.TextBlocks.Any(a => a.Text.StartsWith("金币")) && text.TextBlocks.Any(a => a.Text.StartsWith("现金"))))
                            {

                                #region 首页
                                if (CurPage != AppPageEnum.MoneyHome)
                                {
                                    await SwipeAsync(2000, "滑动到首页顶部", 500, 500, 500, 1500, 200);
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

                                        //await SwipeAsync(4000, "滑动到看指定视频得金币", 100, 1200, 540, 1000, 200);
                                        //continue;
                                    }

                                }


                                #region 看广告得金币
                                if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("看广告得金币")) && arrTask["看广告得金币"].GetArray().Length < 10)
                                {
                                    var array = arrTask["看广告得金币"].GetArray().ToList();
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "看广告得金币"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.ViewAdMoney;
                                        PagePosChange.Add("看广告得金币");
                                        array.Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                        arrTask["看广告得金币"] = string.Join("|", array);
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                #endregion

                                #region 刷广告视频赚金币
                                if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("刷广告视频赚金币")) && arrTask["刷广告视频赚金币"].GetArray().Length < 50)
                                {
                                    var array = arrTask["刷广告视频赚金币"].GetArray().ToList();
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "刷广告视频赚金币"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.ReLoadAd;
                                        PagePosChange.Add("刷广告视频赚金币");
                                        array.Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                        arrTask["刷广告视频赚金币"] = string.Join("|", array);
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                #endregion

                                #region 看视频赚金币
                                if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("看视频赚金币")) && (DateTime.Now - arrTask["看视频赚金币"].ToDateTime(DateTime.Now.AddMinutes(-7))).TotalMinutes >= 5)
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看视频赚金币"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.AppointVideo;
                                        PagePosChange.Add("看视频赚金币");
                                        arrTask["看视频赚金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                #endregion

                                #region 看指定视频赚金币
                                if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("看指定视频赚金币")) && (DateTime.Now - arrTask["看指定视频赚金币"].ToDateTime(DateTime.Now.AddMinutes(-7))).TotalMinutes >= 5)
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看指定视频赚金币"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.AppointVideo;
                                        PagePosChange.Add("看指定视频赚金币");
                                        arrTask["看指定视频赚金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                #endregion

                                #region 搜索赚金币
                                if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text == "去搜索"))// && (DateTime.Now - arrTask["搜索赚金币"].ToDateTime(DateTime.Now.AddMinutes(-7))).TotalMinutes >= 5
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"搜索\S{1}([\s\S]*?)\S{1}赚金币"));
                                    if (BoxPoint != null)
                                    {
                                        var m = Regex.Match(BoxPoint.Text, @"搜索\S{1}([\s\S]*?)\S{1}赚金币");
                                        if (m.Success) {
                                            CurPage = AppPageEnum.SearchKey;
                                            PagePosChange.Add(m.Groups[1].Value);
                                            arrTask["搜索赚金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            continue;
                                        }
                                    }
                                }
                                #endregion

                                #region 看指定视频得金币
                                //if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("看指定视频赚金币")) && (DateTime.Now - arrTask["看视频赚金币"].ToDateTime(DateTime.Now.AddMinutes(-22))).TotalMinutes > 20)
                                //{
                                //    var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "去观看"));
                                //    if (BoxPoint != null)
                                //    {
                                //        CurPage = AppPageEnum.AppointVideo;
                                //        PagePosChange.Add("看指定视频赚金币");
                                //        arrTask["看指定视频赚金币"] =$"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                //        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                //        continue;
                                //    }
                                //}
                                #endregion
                                if (CurPage != AppPageEnum.MoneyHome)
                                {
                                    _mainViewModel.UpdateTaskData();
                                }
                                StopTimeSpan++;
                                #endregion
                            }
                            else
                            {
                                StopTimeSpan = 0;



                                if (CurPage == AppPageEnum.AppointVideo)
                                {
                                    PagePosChange.Clear();
                                    #region 看指定视频赚金币 

                                    var recentCount = (DateTime.Now - (arrTask["看指定视频赚金币"].GetArray().LastOrDefault() ?? "").ToDateTime(DateTime.Now.AddMinutes(18))).TotalMinutes;
                                    if (recentCount >= 15)
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("去赚钱"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else
                                    {
                                        await SwipeAsync(new Random().Next(10000, 23000), "赚钱首页刷视频", 540, 1500, 540, 1000, 200);
                                    }
                                    #endregion
                                }

                                if (CurPage == AppPageEnum.ViewAdMoney)
                                {
                                    PagePosChange.Clear();
                                    #region 看广告得金币 
                                    if (text.TextBlocks.Any(a => a.Text == "关注"))
                                    { //text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"00:\d{2}"))
                                        await Task.Delay(30000);
                                        await ClickAsync(null, 3000, "返回-赚钱首页");
                                    }
                                    #endregion
                                }

                                if (CurPage == AppPageEnum.SearchKey)
                                {
                                    #region 搜索赚金币 
                                    if (text.TextBlocks.Any(a => a.Text == "搜索"))
                                    {
                                        var keyword = PagePosChange.Last();
                                        if (keyword != null)
                                        {
                                            PagePosChange.Clear();
                                            await SendTextAsync(3000, keyword);
                                        }

                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "搜索");
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                        await ClickAsync(null, 3000, "返回-赚钱首页");
                                    }
                                    #endregion
                                }
                                if (CurPage == AppPageEnum.ReLoadAd)
                                {
                                    PagePosChange.Clear();
                                    #region 刷广告视频赚金币 

                                    var recentCount = (DateTime.Now - (arrTask["看指定视频赚金币"].GetArray().LastOrDefault() ?? "").ToDateTime(DateTime.Now.AddMinutes(18))).TotalMinutes;
                                    if (text.TextBlocks.Any(a => a.Text == "关注") && text.TextBlocks.Any(a => a.Text == "无更"))
                                    { //text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"00:\d{2}"))

                                    }
                                    else if (recentCount <= 5)
                                    {
                                        await SwipeAsync(new Random().Next(10000, 23000), "赚钱首页", 540, 1500, 540, 1000, 200);
                                    }
                                    else
                                    {
                                        await ClickAsync(null, 3000, "返回-赚钱首页");
                                    }
                                    #endregion
                                }
                            }



                            //当上面没有操作时就执行下面
                            if (!IsOperate)
                            {
                                if (text.TextBlocks.Any(a => a.Text.Trim() == "首页") && text.TextBlocks.Any(a => a.Text.Trim() == "消息") && text.TextBlocks.Any(a => a.Text.Trim() == "我"))
                                {
                                    var recentCount = arrTask["赚钱首页"].GetArray().Count(x => (DateTime.Now - DateTime.ParseExact(x, "yyyy-MM-dd HH:mm:ss", null)).TotalMinutes <= 5);
                                    if (recentCount >= 15)
                                    {
                                        string[] values = arrTask["赚钱首页"].GetArray();
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
                                    else
                                    {
                                        await SwipeAsync(new Random().Next(10000, 23000), "赚钱首页刷视频", 540, 1500, 540, 1000, 200);
                                    }
                                }
                                #region 页面停留超时
                                if (_mainViewModel.CurDevice.GetAppStatus(packageName) != AdvancedSharpAdbClient.Models.AppStatus.Foreground)
                                {
                                    await StartAppAsync(3000);
                                }
                                else if (CurPage == AppPageEnum.MoneyHome && StopTimeSpan > 60)
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
                    await Task.Delay(1000);
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
        /// <param name="pos">点击文本描述</param>
        /// <returns>异步任务</returns>
        private async Task ClickAsync(List<OCRPoint>? points = null, int delay = 0, string pos = "")
        {
            IsOperate = true;
            lastClickTime = DateTime.Now;
            await _mainViewModel.ClickAsync(points, delay, pos, CurPage);
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
        /// 发送本文
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private async Task SendTextAsync(int delay = 0, string text = "")
        {
            IsOperate = true;
            lastClickTime = DateTime.Now;
            await _mainViewModel.SendTextAsync(delay, text);
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