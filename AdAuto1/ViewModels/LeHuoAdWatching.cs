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
    public class LeHuoAdWatching : IAdWatching
    {
        private readonly IMainViewModel _mainViewModel;
        private AppPageEnum CurPage = AppPageEnum.Node;
        public DateTime lastClickTime = DateTime.Now;
        private bool IsOperate = false;
        private string packageName = "";

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


        public LeHuoAdWatching(IMainViewModel mainViewModel)
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
                            else if (text.TextBlocks.Any(a => a.Text == "禁止") && text.TextBlocks.Any(a => a.Text == "允许") && text.TextBlocks.Any(a => a.Text.Contains("安装应用")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "禁止");
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
                            else if (text.TextBlocks.Any(a => a.Text == "去领取奖励") && text.TextBlocks.Any(a => a.Text == "坚持退出"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "坚持退出");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.Contains("看视频再领")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("看视频再领"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.Ad;
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "恭喜获得金币"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "x" || a.Text == "×");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "放弃福利" || a.Text == "放弃奖励离开"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a =>  a.Text == "放弃福利" || a.Text == "放弃奖励离开");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看广告再领(\d+)金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看广告再领(\d+)金币"));
                                if (BoxPoint != null)
                                {
                                    CurPage = AppPageEnum.Ad;
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"喝水打卡领(\d+)金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"喝水打卡领(\d+)金币"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text.Contains("我要提前拿奖励")) && text.TextBlocks.Any(a => a.Text.Contains("继续播放视频")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("继续播放视频"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }


                            if (text.TextBlocks.Any(a => a.Text == "乐活") || text.TextBlocks.Any(a => a.Text == "抖音极速版") || text.TextBlocks.Any(a => a.Text == "今日头条") || text.TextBlocks.Any(a => a.Text == "番茄畅听"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "乐活");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => new string[]{ "任务" , "视频" , "首页" , "我的" }.Any(x => x == a.Text)) && text.TextBlocks.Any(a => new string[] { "可领取", "去领取", "去完成", "换现金" }.Contains(a.Text)))
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

                                #region 喝水打卡领金币
                                if (CurPage == AppPageEnum.MoneyHome && (DateTime.Now - arrTask["喝水打卡领金币"].ToDateTime(DateTime.Now.AddMinutes(-15))).TotalMinutes >= 12)
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("喝水打卡领金币"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.DrinkWaterPunchIn;
                                        PagePosChange.Add("喝水打卡领金币");
                                        arrTask["喝水打卡领金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);

                                    }
                                }
                                #endregion


                                #region 每日三餐补贴
                                //早餐：07:00-09:00、午餐：11:00-13:00 、晚餐：17:00-19:00 、夜宵：22:00-24:00、
                                if (CurPage == AppPageEnum.MoneyHome && (arrTask["每日三餐补贴"].IsNotEmptyOrNull() || (!arrTask["每日三餐补贴"].GetArray().Contains(key) && mealTimes.Any(a => currentTime >= a.Value.start && currentTime < a.Value.end) && text.TextBlocks.Any(a => a.Text.Contains("每日三餐补贴")))))
                                {
                                    var array = arrTask["每日三餐补贴"].GetArray().ToList();
                                    var keyValue = mealTimes.FirstOrDefault(a => DateTime.Now.TimeOfDay > a.Value.start);
                                    array.Add(keyValue.Key);
                                    arrTask["每日三餐补贴"] = string.Join("|", array);
                                    PagePosChange.Add("每日三餐补贴");
                                    if (text.TextBlocks.Any(a => a.Text.Contains("领取错过补贴")))
                                    {
                                        CurPage = AppPageEnum.ThreeMealsSubsidy;
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("领取错过补贴"));
                                        if (BoxPoint != null)
                                        {
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        }
                                    }
                                    else {
                                        //await ClickAsync(null,2000, null);
                                        //CurPage = AppPageEnum.ThreeMealsSubsidy;
                                        //var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("每日三餐补贴"));
                                        //if (BoxPoint != null)
                                        //{
                                        //    array.Add(key);
                                        //    arrTask["每日三餐补贴"] = string.Join("|", array);
                                        //    PagePosChange.Add("每日三餐补贴");
                                        //    await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        //}
                                    }

                                }
                                #endregion


                                #region 看视频文章领金币
                                if (CurPage == AppPageEnum.MoneyHome && (DateTime.Now - arrTask["看视频文章领金币"].ToDateTime(DateTime.Now.AddMinutes(-15))).TotalMinutes >= 5)
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看视频文章领金币"));
                                    if (BoxPoint != null)
                                    {
                                        CurPage = AppPageEnum.ReadVideoOrArticle;
                                        PagePosChange.Add("看视频文章领金币");
                                        arrTask["看视频文章领金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                        await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);

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
                                if (CurPage == AppPageEnum.DrinkWaterPunchIn)
                                {
                                    #region 喝水打卡领金币
                                    if (text.TextBlocks.Any(a => a.Text == "喝水打卡"))
                                    {
                                        PagePosChange.Clear();

                                    }
                                    if (!text.TextBlocks.Any(a => a.Text == "免费获取" || Regex.IsMatch(a.Text, @"^\d+\s*s$")))
                                    {
                                        var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x");
                                        if (BoxPoint != null)
                                        {
                                            isCompele = true;
                                            await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            continue;
                                        }
                                        else
                                        {
                                            if (text.TextBlocks.Any(a => a.Text.Contains("金币")))
                                            {

                                                await SwipeAsync(2000, "返回-首页", 100, 1000, 800, 1000, 300);
                                            }
                                            await ClickAsync(null, 2000, "返回-首页");
                                        }
                                    }
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.ThreeMealsSubsidy)
                                {
                                    #region 每日三餐补贴
                                    PagePosChange.Clear();
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.TaskEarning)
                                {
                                    #region 任务赚
                                    PagePosChange.Clear();
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.BaiduSmartAnswer)
                                {
                                    #region 百度智慧回答
                                    PagePosChange.Clear();
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.SearchForCoins)
                                {
                                    #region 搜索领金币
                                    PagePosChange.Clear();
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.ReadArticleOrWatchVideo)
                                {
                                    #region 看一个文章或视频
                                    PagePosChange.Clear();
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.ShopMallEarning)
                                {
                                    #region 逛商城赚金币
                                    PagePosChange.Clear();
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.DailySignInForCash)
                                {
                                    #region 每日签到领现金
                                    PagePosChange.Clear();
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.WatchAdsForCoins)
                                {
                                    #region 看看广告领金币奖励
                                    PagePosChange.Clear();
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.ReadVideoOrArticle)
                                {
                                    #region 看视频文章领金币
                                    PagePosChange.Clear();
                                    if (arrTask["看视频文章领金币"].ToDateTime().TimeOfDay.TotalMinutes > 5)
                                    {
                                        await ClickAsync(null, 2000, "返回-停留页面超过60秒");
                                    }
                                    else
                                    {

                                        await SwipeAsync(new Random().Next(3000, 6000), "看视频文章领金币自动上滑", 500, 1000, 500, 500, 200);
                                    }
                                    #endregion
                                }
                                else if (CurPage == AppPageEnum.Ad)
                                {
                                    #region 广告
                                    if (text.TextBlocks.Any(a => a.Text.StartsWith("免费获取") || a.Text.StartsWith("立即获取")) && !text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^\d+$")))
                                    {
                                        if (text.TextBlocks.Any(a => a.Text == "跳过"))
                                        {
                                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "跳过");
                                            if (BoxPoint != null)
                                            {
                                                await ClickAsync(BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                            }
                                        }
                                        else {

                                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("免费获取"));
                                            if (BoxPoint != null)
                                            {
                                                var pos = BoxPoint.BoxPoints.OrderByDescending(a => a.X).Skip(0).Take(1).Select(a => new OCRPoint(a.X + 220, a.Y)).ToList();
                                                await ClickAsync(pos, 3000, "关闭广告");
                                            }
                                            else 
                                            {
                                                try
                                                {
                                                    var screen = await _mainViewModel.CaptureDeviceScreen();
                                                    await File.WriteAllBytesAsync("screen.png", screen);
                                                    /*
                                                    // 1. 加载原始图片和模板
                                                    Mat src = Cv2.ImDecode(screen, ImreadModes.Color);  // 原始截图
                                                    Mat template = Cv2.ImRead("E:\\AdWPFAndroid\\ConsoleApp1\\close.png", ImreadModes.Color); // 菜单图标模板

                                                    if (src.Empty() || template.Empty())
                                                    {
                                                        Console.WriteLine("图片加载失败！");
                                                        return;
                                                    }
                                                    // 2. 裁剪顶部15%区域
                                                    int cropHeight = (int)(src.Height * 0.15);
                                                    Mat cropped = new Mat(src, new Rect(0, 0, src.Width, cropHeight));

                                                    // 2. 转换为灰度图（提升匹配效率）
                                                    Mat srcGray = new Mat();
                                                    Mat templateGray = new Mat();
                                                    Cv2.CvtColor(cropped, srcGray, ColorConversionCodes.BGR2GRAY);
                                                    Cv2.CvtColor(template, templateGray, ColorConversionCodes.BGR2GRAY);



                                                    // 3. 模板匹配（使用归一化相关系数）
                                                    Mat result = new Mat();
                                                    Cv2.MatchTemplate(srcGray, templateGray, result, TemplateMatchModes.CCoeffNormed);

                                                    // 4. 获取最佳匹配位置
                                                    Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                                                    if (maxVal >= 0.8)
                                                    {
                                                        await ClickAsync(new List<OCRPoint>() { new OCRPoint(maxLoc.X + 50, maxLoc.Y + 50) }, 3000, "OpenCvSharp关闭广告");
                                                    }*/
                                                }
                                                catch (Exception ex)
                                                {
                                                }
                                            }

                                        }
                                    }
                                    #endregion
                                }


                                //当上面没有操作时就执行下面
                                if (!IsOperate)
                                {
                                    if (text.TextBlocks.Any(a => a.Text.Trim() == "首页") && text.TextBlocks.Any(a => a.Text.Trim() == "任务") && text.TextBlocks.Any(a => a.Text.Trim() == "我"))
                                    {
                                        string[] values = arrTask["赚钱首页"].GetArray();

                                        var recentCount = values.Count(x => (DateTime.Now - DateTime.ParseExact(x, "yyyy-MM-dd HH:mm:ss", null)).TotalMinutes <= 5);
                                        if (recentCount >= 15)
                                        {
                                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("视频"));
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