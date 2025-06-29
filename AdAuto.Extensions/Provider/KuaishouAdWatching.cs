using System.Text.RegularExpressions;
using PaddleOCRSharp;
using Yuandl.Common.Helper;
using AdAuto.Extensions.Helper;
using System.Collections.Concurrent;
using AdAuto.Extensions.enums;

namespace AdAuto.Extensions.Provider;

/// <summary>
/// 快手广告观看自动化实现
/// </summary>
public class KuaishouAdWatching : IAdWatching
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
        ["看广告得金币"] = new List<string>(),
        ["看指定视频赚金币"] = new List<string>(),
        ["切换账号"] = new List<string>() { "星月飞天" }
    };

    public KuaishouAdWatching(IMainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }
    public async Task AutomateAdWatching(CancellationToken cancellationToken)
    {

        try
        {
            int StopTimeSpan = 0;
            List<string> PagePosChange = new List<string>();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    OCRResult? text = await _mainViewModel.RecognizeText();
                    if (text?.Text?.Length > 1)
                    {
                        if (text.TextBlocks.Any(a => a.Text == "领取奖励") && text.TextBlocks.Any(a => a.Text == "坚持退出" || a.Text == "放弃奖励"))
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
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("已成功领取")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("已成功领取"));
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

                        if (text.TextBlocks.Any(a => a.Text == "任务中心") || text.TextBlocks.Any(a => a.Text.StartsWith("金币")) && text.TextBlocks.Any(a => a.Text.StartsWith("现金")))
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
                                    arrTask[Last].RemoveAt(arrTask[Last].Count - 1);
                                    PagePosChange.Clear();
                                }
                                else
                                {

                                    await SwipeAsync(4000, "滑动到看指定视频得金币", 100, 1200, 540, 1000, 200);
                                    continue;
                                }

                            }

                            #region 看指定视频得金币
                            if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("看指定视频赚金币")) && (DateTime.Now - (arrTask["看视频赚金币"].LastOrDefault() ?? "").ToDateTime(DateTime.Now.AddMinutes(-22))).TotalMinutes > 20)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "看指定视频赚金币"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.AppointVideo;
                                    PagePosChange.Add("看指定视频赚金币");
                                    arrTask["看指定视频赚金币"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                }
                            }
                            #endregion

                            #region 看广告得金币
                            if (CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("看广告得金币")) && arrTask["看广告得金币"].Count < 10)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "看广告得金币"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.AppointVideo;
                                    PagePosChange.Add("看广告得金币");
                                    arrTask["看广告得金币"].Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion
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

                                var recentCount = (DateTime.Now - (arrTask["看指定视频赚金币"].LastOrDefault() ?? "").ToDateTime(DateTime.Now.AddMinutes(18))).TotalMinutes;
                                if (recentCount >= 15)
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("去赚钱"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                    }
                                }
                                else
                                {
                                    await SwipeAsync(new Random().Next(10000, 25000), "赚钱首页刷视频", 540, 1500, 540, 1000, 200);
                                }
                                #endregion
                            }

                            if (CurPage == AppPageEnum.Ad)
                            {
                                PagePosChange.Clear();
                                #region 看广告得金币 

                                #endregion
                            }

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
                            else if (CurPage == AppPageEnum.GuangJie)
                            {
                                await SwipeAsync(2000, "返回-停留页面超过60秒", 500, 500, 500, 1500, 200);
                            }
                            #region 页面停留超时
                            if ((DateTime.Now - lastClickTime).TotalSeconds > 60)
                            {
                                await ClickAsync(null, 2000, "返回-停留页面超过60秒");
                                //else if ((DateTime.Now - lastClickTime).TotalSeconds > 120 && CurPage == AppPageEnum.MoneyHome)
                                //{
                                //    SwitchAccount = true;
                                //}
                            }
                            #endregion
                        }



                        IsOperate = false;
                    }
                }
                catch (Exception ex)
                {
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
    protected async Task ClickAsync(List<OCRPoint>? points = null, int delay = 0, string pos = "")
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