using TimerCheckApp.enums;
using TimerCheckApp.Extensions;
using PaddleOCRSharp;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TimerCheckApp.Services.Interfaces;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.DeviceCommands;
using OpenCvSharp;
using System.IO;

namespace TimerCheckApp.AdWatching;

/// <summary>
/// 快手广告观看自动化实现
/// </summary>
public class KuaishouAdWatching : IAdWatching
{
    private readonly ILogService _logService;
    private readonly IDeviceService _deviceService;
    private readonly ICacheService _cacheService;
    private readonly ISqlSugarService _sqlSugarService;
    private bool IsOperate = false;

    public ConcurrentDictionary<string, string> arrTask { get; set; } = new()
    {
        ["看广告得金币"] = string.Empty,
        ["看视频赚金币"] = string.Empty,
        ["每天打卡拿奖励"] = string.Empty,
        ["每天睡觉起床"] = string.Empty,
        ["刷广告视频赚金币"] = string.Empty,
        ["看指定视频赚金币"] = string.Empty,
        ["预约领金币"] = string.Empty,
        ["走路赚金币"] = string.Empty,
        ["看直播赚金币"] = string.Empty,
        ["搜索赚金币"] = string.Empty,
        ["吃饭打卡赚金币"] = string.Empty,
        ["逛街领金币"] = string.Empty,
        ["逛快手商城赚金币"] = string.Empty,
        ["上下滑动"] = string.Empty,
        ["上次滑动时间"] = string.Empty
    };

    public KuaishouAdWatching(ILogService logService, IDeviceService deviceService, ICacheService cacheService, ISqlSugarService sqlSugarService)
    {
        _logService = logService;
        _deviceService = deviceService;
        _cacheService = cacheService;
        _sqlSugarService = sqlSugarService;
    }
    public async Task AutomateAdWatching(DeviceModel device, CancellationToken cancellationToken)
    {

        var _device = new DeviceData { Serial = device.Id };
        try
        {
            int StopTimeSpan = 0;
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
                        if (text.TextBlocks.Any(a => a.Text == "添加到主屏幕" || a.Text == "添加至桌面") && text.TextBlocks.Any(a => a.Text == "取消") && text.TextBlocks.Any(a => a.Text == "添加"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "取消") && text.TextBlocks.Any(a => a.Text == "开启快手时默认静音"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "允许") && text.TextBlocks.Any(a => a.Text.Contains("访问您的日历")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "允许");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "允许") && text.TextBlocks.Any(a => a.Text.Contains("访问您的照片")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "允许");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "取消") && text.TextBlocks.Any(a => a.Text.Contains("安装应用吗")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "取消") && text.TextBlocks.Any(a => a.Text.Contains("开启快手时默认静音")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "点击重试") && text.TextBlocks.Any(a => a.Text == "点击返回"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "点击重试");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "点击重试") && text.TextBlocks.Any(a => a.Text.Contains("网络错误")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "点击重试");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "添加至桌面") && text.TextBlocks.Any(a => a.Text == "取消"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "取消");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "知道了") && text.TextBlocks.Any(a => a.Text == "立即前往"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "知道了");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("使用位置信息")) && text.TextBlocks.Any(a => a.Text == "使用时允许"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "使用时允许");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("访问日历")) && text.TextBlocks.Any(a => a.Text == "允许"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "允许");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("向你发送通知")) && text.TextBlocks.Any(a => a.Text == "禁止"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "禁止");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (device.CurPage == AppPageEnum.Data)
                        {
                            // 刷短剧
                            try
                            {

                                await HandleSlideUpPageInteractions(device, _device, text, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logService.LogError($"自动化过程出错: {ex.Message}", device.Id);
                                await Task.Delay(5000);
                            }
                            await Task.Delay(2000);
                            continue;
                        }
                        #endregion


                        #region 弹出层面
                        else if (text.TextBlocks.Any(a => a.Text == "立即签到"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即签到");
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
                        else if (text.TextBlocks.Any(a => a.Text == "提醒我每天来领"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "提醒我每天来领");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "点我领iPhone" || a.Text == "每日打卡白拿手机"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "点我领iPhone" || a.Text == "每日打卡白拿手机");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "仍要离开") && text.TextBlocks.Any(a => a.Text == "我知道了"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "仍要离开");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "关注并退出") && text.TextBlocks.Any(a => a.Text == "退出直播间"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "退出直播间");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "去微信邀请好友"))
                        {
                            byte[]? bytes = await _deviceService.CaptureWithAdb(device);
                            if (bytes != null)
                            {
                                string iconTemp = Path.Combine(Environment.CurrentDirectory, $"Resources\\template\\ks_close.png");
                                //await File.WriteAllBytesAsync(Path.Combine(Environment.CurrentDirectory, $"Resources\\template\\ks_closexxxxxx.png"), bytes);
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
                                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(maxLoc.X + 5, maxLoc.Y + 5) }, 1000, "匹配播放按钮");
                                    continue;
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("新人福利")) && text.TextBlocks.Any(a => a.Text == "立即领取"))
                        {

                            byte[]? bytes = await _deviceService.CaptureWithAdb(device);
                            if (bytes != null)
                            {
                                string iconTemp = Path.Combine(Environment.CurrentDirectory, $"Resources\\template\\ks_close.png");
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
                                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(maxLoc.X + 5, maxLoc.Y + 5) }, 1000, "匹配关闭按钮");
                                    continue;
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("邀请")) && text.TextBlocks.Any(a => a.Text.Contains("个新用户必得")))
                        {

                            byte[]? bytes = await _deviceService.CaptureWithAdb(device);
                            if (bytes != null)
                            {
                                string iconTemp = Path.Combine(Environment.CurrentDirectory, $"Resources\\template\\ks_close2.png");


                                // 1. 加载原始图片和模板
                                using Mat src = Cv2.ImDecode(bytes, ImreadModes.Color);  // 原始截图
                                using Mat template = Cv2.ImRead(iconTemp, ImreadModes.Color); // 菜单图标模板


                                // 2. 转换为灰度图（提升匹配效率）
                                using Mat srcGray = new Mat();
                                using Mat templateGray = new Mat();
                                Cv2.CvtColor(src, srcGray, ColorConversionCodes.BGR2GRAY);
                                Cv2.CvtColor(template, templateGray, ColorConversionCodes.BGR2GRAY);


                                // 3. 模板匹配（使用归一化相关系数）
                                using Mat result = new Mat();
                                Cv2.MatchTemplate(srcGray, templateGray, result, TemplateMatchModes.CCoeffNormed);

                                // 4. 获取最佳匹配位置
                                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                                src?.Dispose();
                                srcGray?.Dispose();
                                templateGray.Dispose();
                                template?.Dispose();
                                result?.Dispose();
                                if (maxVal > 0.7)
                                {
                                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(maxLoc.X + 5, maxLoc.Y + 5) }, 1000, "匹配关闭按钮");
                                    continue;
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "领取额外金币"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领取额外金币");
                            if (BoxPoint != null)
                            {
                                if (text.TextBlocks.Any(a => a.Text.Contains("打开并体验") || a.Text.Contains("点击并体验")))
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 10000, BoxPoint.Text);
                                    await _deviceService.CurAdbClient.SendKeyEventAsync(_device, "KEYCODE_APP_SWITCH", CancellationToken.None);
                                    await Task.Delay(2000);
                                    text = await _deviceService.GetOCRText(device);
                                    if (!text.TextBlocks.Any(a => a.Text == device.CurPlatform.GetDescription()))
                                    {

                                        for (int i = 0; i < 7; i++)
                                        {
                                            if (i <= 3)
                                            {
                                                await SwipeAsync(device, _device, 1000, BoxPoint.Text, 100, device.Height / 2, 300, device.Height / 2, 500);
                                                text = await _deviceService.GetOCRText(device);
                                                if (text.TextBlocks.Any(a => a.Text == device.CurPlatform.GetDescription()))
                                                {
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                await SwipeAsync(device, _device, 1000, BoxPoint.Text, device.Width - 100, device.Height / 2, 100, device.Height / 2, 500);
                                            }
                                        }
                                    }
                                    //await SwipeAsync(device, _device, 2000, BoxPoint.Text, 100, 500, 300, 500, 500);
                                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(device.Width / 2, device.Height / 2) }, 5000, BoxPoint.Text);
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text.Contains("下载并体验") || a.Text.Contains("安装并体验")))
                                {
                                    byte[]? bytes = await _deviceService.CaptureWithAdb(device);
                                    if (bytes != null)
                                    {
                                        string iconTemp = Path.Combine(Environment.CurrentDirectory, $"Resources\\template\\ks_close1.png");
                                        //await File.WriteAllBytesAsync(Path.Combine(Environment.CurrentDirectory, $"Resources\\template\\ks_closexxxxxx.png"), bytes);
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
                                            await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(maxLoc.X + 5, maxLoc.Y + 5) }, 1000, "匹配关闭按钮");
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("打开并体验")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("打开并体验"));
                            if (BoxPoint != null)
                            {
                                var m = Regex.Match(text.Text, @"(点击|打开)并体验(\d+)");
                                if (m.Success)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, (m.Groups[1].Value.ToInt() + 5) * 1000, BoxPoint.Text);
                                    await _deviceService.CurAdbClient.SendKeyEventAsync(_device, "KEYCODE_APP_SWITCH", CancellationToken.None);
                                    await Task.Delay(2000);
                                    await SwipeAsync(device, _device, 2000, BoxPoint.Text, 100, 500, 300, 500, 500);
                                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(device.Width / 2, device.Height / 2) }, 5000, BoxPoint.Text);
                                    continue;
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "领取奖励") && text.TextBlocks.Any(a => a.Text.StartsWith("再看1个广告")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领取奖励");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "继续观看") && text.TextBlocks.Any(a => a.Text.StartsWith("再看")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "翻倍任务开启") && text.TextBlocks.Any(a => a.Text == "去看内容"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去看内容");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("看内容最高可得") || a.Text.StartsWith("看广告最高赚")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看内容最高可得") || a.Text.StartsWith("看广告最高赚"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("看视频立得") || a.Text.StartsWith("看视频最高可得") || a.Text.StartsWith("看视频最高得") || a.Text.StartsWith("去看广告得最高") || a.Text.StartsWith("看广告最多再得") || a.Text == "金币奖励"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看视频立得") || a.Text.StartsWith("看视频最高可得") || a.Text.StartsWith("看视频最高得") || a.Text.StartsWith("去看广告得最高") || a.Text.StartsWith("看广告最多再得"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "金币奖励"))
                            {
                                BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "金币奖励");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    continue;
                                }
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "领取奖励") && text.TextBlocks.Any(a => a.Text == "坚持退出" || a.Text == "放弃奖励"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领取奖励");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "继续观看") && text.TextBlocks.Any(a => a.Text == "放弃奖励"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "继续观看") && text.TextBlocks.Any(a => a.Text.StartsWith("换一个广告")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续观看");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "继续赚钱") && text.TextBlocks.Any(a => a.Text == "任务完成奖励"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "继续赚钱");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("去分享视频赚更多")))
                        {
                            byte[]? bytes = await _deviceService.CaptureWithAdb(device);
                            if (bytes != null)
                            {
                                string iconTemp = Path.Combine(Environment.CurrentDirectory, $"Resources\\template\\ks_close.png");
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
                                if (maxVal > 0.8)
                                {
                                    await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(maxLoc.X + 5, maxLoc.Y + 5) }, 1000, "匹配播放按钮");
                                    continue;
                                }
                            }
                        }
                        #endregion

                        #region 页面层
                        else if (text.TextBlocks.Any(a => a.Text == "精选") && text.TextBlocks.Any(a => a.Text == "我") && !text.TextBlocks.Any(a => a.Text == "去领取"))
                        {
                            if (text.TextBlocks.Any(a => a.Text == "点击进入直播间"))
                            {
                                await SwipeAsync(device, _device, 1000, "赚钱首页刷视频", 540, 1500, 540, 1000, 200);
                            }
                            else
                            {
                                byte[]? bytes = await _deviceService.CaptureWithAdb(device);
                                if (bytes != null)
                                {
                                    string iconTemp = Path.Combine(Environment.CurrentDirectory, $"Resources\\template\\red.png");
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
                                    if (maxVal > 0.8)
                                    {
                                        await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(maxLoc.X + 30, maxLoc.Y + 30) }, 1000, "匹配关闭按钮");
                                        continue;
                                    }
                                }
                            }
                        }
                        else if (device.CurPage != AppPageEnum.KSShopMall && !text.TextBlocks.Any(a => a.Text == "去领取") && text.TextBlocks.Any(a => a.Text == "首页") && text.TextBlocks.Any(a => a.Text == "去赚钱") && !text.TextBlocks.Any(a => a.Text == "任务中心") && text.TextBlocks.Any(a => a.Text == "发现" || a.Text == "关注"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去赚钱");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 5000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "领取成功") && text.TextBlocks.Any(a => a.Text == "X" || a.Text == "x" || a.Text == "×"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "X" || a.Text == "x" || a.Text == "×");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "去完成任务") && text.TextBlocks.Any(a => a.Text == "放弃奖励"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "放弃奖励");
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("已成功领取") || Regex.IsMatch(a.Text, @"成功领取(\d+)金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("已成功领取") || Regex.IsMatch(a.Text, @"成功领取(\d+)金币"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("已可继续看剧")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("已可继续看剧"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"点可领(\d+)金币")))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"点可领(\d+)金币"));
                            if (BoxPoint != null)
                            {
                                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                continue;
                            }
                        }
                        else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"(\d+)/5")) && text.TextBlocks.Any(a => a.Text == "立即观看"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"(\d+)/5"));
                            if (BoxPoint?.Text != "5/5")
                            {
                                // 执行小幅度上滑
                                await SwipeAsync(device, _device, 10000, "滑动-逛街得金币", 500, 1500, 500, 500, 200);
                            }
                            else
                            {
                                await ClickAsync(device, _device, null, 3000);
                            }
                            continue;
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("有爱评论")) && text.TextBlocks.Any(a => a.Text == "分词"))
                        {
                            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.Contains("有爱评论"));
                            if (BoxPoint != null)
                            {
                                var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(pos.X, pos.Y - 250) }, 1000, BoxPoint.Text);
                                continue;
                            }
                        }
                        #endregion


                        #region 位置识别
                        if (text.TextBlocks.Any(a => a.Text == "睡觉赚金币"))
                        {
                            device.CurPage = AppPageEnum.DaySawu;
                        }
                        else if (text.TextBlocks.Any(a => a.Text.StartsWith("先领饭补再下单") || a.Text.StartsWith("到点领饭补金币") || a.Text.StartsWith("领早饭补贴啦") || a.Text.StartsWith("领午饭补贴啦") || a.Text.StartsWith("领晚饭补贴啦") || a.Text.StartsWith("领夜宵补贴啦")))
                        {
                            device.CurPage = AppPageEnum.Dining;
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "逛街赚金币"))
                        {
                            device.CurPage = AppPageEnum.GuangJie;
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "走路赚金币"))
                        {
                            device.CurPage = AppPageEnum.KSWalkEarn;
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "看直播领金币"))
                        {
                            device.CurPage = AppPageEnum.ViewLive;
                        }
                        else if (text.TextBlocks.Any(a => a.Text.Contains("每日打卡任务") || a.Text.Contains("请使用原设备") || a.Text.Contains("当前设备已绑定账号") || a.Text.Contains("打卡任务白拿好礼")))
                        {
                            device.CurPage = AppPageEnum.DayClockIn;
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "搜索") && text.TextBlocks.Any(a => a.Text == "历史搜索" || a.Text == "综合"))
                        {
                            device.CurPage = AppPageEnum.SearchKey;
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "看直播领金币"))
                        {
                            device.CurPage = AppPageEnum.ViewLive;
                        }
                        else if (text.TextBlocks.Any(a => a.Text == "商城") && text.TextBlocks.Any(a => a.Text == "去领取" || a.Text == "我的订单"))
                        {
                            device.CurPage = AppPageEnum.KSShopMall;
                        }
                        #endregion

                        if (text.TextBlocks.Any(a => a.Text.Contains("任务中心")) || text.TextBlocks.Any(a => a.Text.Contains("金币") && a.Text.Contains("现金")) || (text.TextBlocks.Any(a => a.Text.StartsWith("金币")) && text.TextBlocks.Any(a => a.Text.StartsWith("现金"))))
                        {

                            #region 首页
                            if (device.CurPage != AppPageEnum.MoneyHome)
                            {
                                //await SwipeAsync(device, _device, 2000, "滑动到首页顶部", 500, 500, 500, 1500, 200);
                                device.CurPage = AppPageEnum.MoneyHome;
                                var Last = PagePosChange.LastOrDefault();
                                if (PagePosChange.Count > 0 && Last != null)
                                {
                                    _logService.LogWarning("页面没有跳转，清空重试", device.Id);
                                    var array = arrTask[Last].GetArray().ToList();
                                    array.RemoveAt(array.Count - 1);
                                    arrTask[Last] = string.Join("|", array);
                                    PagePosChange.Clear();
                                }
                                else
                                {
                                }

                            }

                            #region 每天睡觉起床
                            if (device.CurPage == AppPageEnum.MoneyHome && ((text.TextBlocks.Any(a => a.Text == "去起床") && DateTime.Now.Hour <= 10) || (text.TextBlocks.Any(a => a.Text == "去睡觉") && DateTime.Now.Hour >= 20)) && arrTask["每天睡觉起床"].IsNotEmptyOrNull())
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去起床" || a.Text == "去睡觉");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.DaySawu;
                                    PagePosChange.Add("每天睡觉起床");
                                    arrTask["每天睡觉起床"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 走路赚金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text == "去走路") && (DateTime.Now - arrTask["走路赚金币"].ToDateTime(DateTime.Now.AddHours(-11))).TotalHours >= 10)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去走路");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.KSWalkEarn;
                                    PagePosChange.Add("走路赚金币");
                                    arrTask["走路赚金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 吃饭打卡赚金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text == "到饭点领饭补") && (DateTime.Now - arrTask["吃饭打卡赚金币"].ToDateTime(DateTime.Now.AddHours(-11))).TotalHours >= 2)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "到饭点领饭补");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.Dining;
                                    PagePosChange.Add("吃饭打卡赚金币");
                                    arrTask["吃饭打卡赚金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 搜索赚金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text == "搜索赚金币" || Regex.IsMatch(a.Text, @"搜索\S{1}([\s\S]*?)\S{1}赚金币") || Regex.IsMatch(a.Text, @"搜索\S{1}([\s\S]*?)\S{1}")) && arrTask["搜索赚金币"].GetArray().Length < 3)
                            {
                                var array = arrTask["搜索赚金币"].GetArray().ToList();
                                if (text.TextBlocks.Any(a => a.Text == "搜索赚金币") || (DateTime.Now - array.LastOrDefault().ToDateTime(DateTime.Now.AddMinutes(-20))).TotalMinutes >= 20)
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去搜索");
                                    if (BoxPoint != null)
                                    {
                                        device.CurPage = AppPageEnum.SearchKey;
                                        PagePosChange.Add("搜索赚金币");
                                        array.Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                        arrTask["搜索赚金币"] = string.Join("|", array);
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                            }
                            #endregion

                            #region 看广告得金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("看广告得金币")) && !text.TextBlocks.Any(a => a.Text == "冷却中") && arrTask["看广告得金币"].GetArray().Length < 10 && (DateTime.Now - arrTask["看广告得金币"].GetArray().LastOrDefault().ToDateTime(DateTime.Now.AddMinutes(-11))).TotalMinutes > 10)
                            {
                                var array = arrTask["看广告得金币"].GetArray().ToList();
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "看广告得金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.ViewAdMoney;
                                    PagePosChange.Add("看广告得金币");
                                    array.Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    arrTask["看广告得金币"] = string.Join("|", array);
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 看直播赚金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"看(\d+)次直播领金币")) && (DateTime.Now - arrTask["看直播赚金币"].ToDateTime(DateTime.Now.AddHours(-11))).TotalHours >= 1)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"看(\d+)次直播领金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.ViewLive;
                                    PagePosChange.Add("看直播赚金币");
                                    arrTask["看直播赚金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 逛快手商城赚金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text == "逛快手商城赚金币") && arrTask["逛快手商城赚金币"].IsNotEmptyOrNull())
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "逛快手商城赚金币");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.KSShopMall;
                                    PagePosChange.Add("逛快手商城赚金币");
                                    arrTask["逛快手商城赚金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 预约领金币
                            if (device.CurPage == AppPageEnum.MoneyHome && arrTask["预约领金币"].IsNotEmptyOrNull())
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("一键领金币") || a.Text.EndsWith("预约领金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.OneClickMoney;
                                    PagePosChange.Add("预约领金币");
                                    arrTask["预约领金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;

                                }
                            }
                            #endregion

                            #region 刷广告视频赚金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("刷广告视频赚金币")) && arrTask["刷广告视频赚金币"].GetArray().Length < 50 && (DateTime.Now - arrTask["刷广告视频赚金币"].GetArray().LastOrDefault().ToDateTime(DateTime.Now.AddMinutes(-11))).TotalMinutes >= 10)
                            {
                                var array = arrTask["刷广告视频赚金币"].GetArray().ToList();
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, "刷广告视频赚金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.ReLoadAd;
                                    PagePosChange.Add("刷广告视频赚金币");
                                    array.Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    arrTask["刷广告视频赚金币"] = string.Join("|", array);
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 看指定视频赚金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("看指定视频赚金币")) && (DateTime.Now - arrTask["看指定视频赚金币"].ToDateTime(DateTime.Now.AddMinutes(-7))).TotalMinutes >= 5)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看指定视频赚金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.AppointVideo;
                                    PagePosChange.Add("看指定视频赚金币");
                                    arrTask["看指定视频赚金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion


                            #region 逛街领金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text == "去逛街") && !text.TextBlocks.Any(a => a.Text.Contains("浏览商品赚金币10/10")) && arrTask["逛街领金币"].IsNotEmptyOrNull())
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去逛街");
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.GuangJie;
                                    PagePosChange.Add("逛街领金币");
                                    arrTask["逛街领金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 每天打卡拿奖励
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("连续打卡白拿")) && arrTask["每天打卡拿奖励"].IsNotEmptyOrNull())
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("连续打卡白拿"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.DayClockIn;
                                    PagePosChange.Add("每天打卡拿奖励");
                                    arrTask["每天打卡拿奖励"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            #region 看视频赚金币
                            if (device.CurPage == AppPageEnum.MoneyHome && text.TextBlocks.Any(a => a.Text.StartsWith("看视频赚金币")) && (DateTime.Now - arrTask["看视频赚金币"].ToDateTime(DateTime.Now.AddMinutes(-35))).TotalMinutes >= 30)
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看视频赚金币"));
                                if (BoxPoint != null)
                                {
                                    device.CurPage = AppPageEnum.ViewVideo;
                                    PagePosChange.Add("看视频赚金币");
                                    arrTask["看视频赚金币"] = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            #endregion

                            if (!IsOperate && (DateTime.Now - device.LastClickTime).TotalSeconds > 10)
                            {
                                if (arrTask["上下滑动"].ToInt32() > 500)
                                {
                                   //arrTask["上下滑动"] = $"{(arrTask["上下滑动"].ToInt32() - 100)}";
                                   //await SwipeAsync(device, _device, 3000, "在首页停留时间超过30秒自动下滑", 500, 900, 500, 1000, 200);
                                }
                                else
                                {
                                    //arrTask["上下滑动"] = $"{(arrTask["上下滑动"].ToInt32() + 100)}";
                                    //await SwipeAsync(device, _device, 3000, "在首页停留时间超过30秒自动上滑", 500, 1000, 500, 900, 200);
                                }
                                continue;
                            }
                            if (device.CurPage != AppPageEnum.MoneyHome)
                            {
                                //_deviceService.UpdateTaskData();
                            }
                            StopTimeSpan++;
                            #endregion
                        }
                        else
                        {
                            #region 非首页
                            StopTimeSpan = 0;
                            if (device.CurPage == AppPageEnum.DaySawu)
                            {
                                PagePosChange.Clear();
                                #region 每天睡觉起床
                                if (text.TextBlocks.Any(a => a.Text == "开始睡觉" || a.Text == "开始起床" || a.Text == "看广告领睡觉补贴" || a.Text == "看广告领起床补贴"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "开始睡觉" || a.Text == "开始起床" || a.Text == "看广告领睡觉补贴" || a.Text == "看广告领起床补贴");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "已起床" || a.Text == "已入睡" || a.Text == "已人睡"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-首页");
                                    continue;
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.Dining)
                            {
                                PagePosChange.Clear();
                                #region 吃饭打卡赚金币
                                if (text.TextBlocks.Any(a => a.Text == "补签"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "补签");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"(早饭|午饭|晚饭|夜宵)待补签")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"(早饭|午饭|晚饭|夜宵)待补签"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("领饭补") || a.Text.StartsWith("领取饭补") || a.Text == "看广告领饭补"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("领饭补") || a.Text.StartsWith("领取饭补") || a.Text == "看广告领饭补");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("浏览团购再赚")))
                                {
                                    for (int i = 0; i < 30; i++)
                                    {
                                        await SwipeAsync(device, _device, 2000, "滑动-逛街得金币", 500, 1500, 500, 1000, 200);
                                    }
                                    for (int i = 0; i < 10; i++)
                                    {
                                        await SwipeAsync(device, _device, 1000, "滑动-逛街得金币", 500, 100, 500, 1500, 200);
                                    }
                                    await ClickAsync(device, _device, null, 3000, "返回-首页");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"后领(早饭|午饭|晚饭|夜宵)补贴") || a.Text.StartsWith("明日")))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-首页");
                                    continue;
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.KSWalkEarn)
                            {
                                PagePosChange.Clear();
                                #region 走路赚金币
                                if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"领取(\d+)金币")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"领取(\d+)金币"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("再走")))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-首页");
                                    continue;
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.SearchKey)
                            {
                                PagePosChange.Clear();
                                #region 搜索赚金币
                                if (text.TextBlocks.Any(a => a.Text == "搜索") && !text.TextBlocks.Any(a => a.Text.Contains("任务已完成")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "搜索");
                                    if (BoxPoint != null)
                                    {
                                        if (arrTask["搜索赚金币"].IsNotEmptyOrNull())
                                        {
                                            var array = new List<string>() { $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}" };
                                            arrTask["搜索赚金币"] = string.Join("|", array);
                                        }
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 20000, BoxPoint.Text);
                                        await ClickAsync(device, _device, null, 3000, "返回-首页");
                                        await ClickAsync(device, _device, null, 3000, "返回-首页");
                                        continue;
                                    }
                                }
                                else
                                {
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.AppointVideo)
                            {
                                PagePosChange.Clear();
                                #region 看指定视频赚金币 

                                var Minutes = (DateTime.Now - (arrTask["看指定视频赚金币"].GetArray().LastOrDefault() ?? "").ToDateTime(DateTime.Now.AddMinutes(18))).TotalMinutes;
                                if (Minutes >= 15)
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回首页");
                                    continue;
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "没有更多作品了" || a.Text == "查看收益"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回首页");
                                    continue;
                                }
                                else
                                {
                                    if (text.TextBlocks.Any(a => a.Text == "看视频" && a.BoxPoints.Max(x => x.X) > (device.Width - 300)))
                                    {
                                        await Task.Delay(new Random().Next(5000, 10000));
                                    }
                                    //arrTask["上次滑动时间"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    await SwipeAsync(device, _device, 1000, "赚钱首页刷视频", 540, 1500, 540, 1000, 200);
                                    continue;
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.ViewAdMoney)
                            {
                                PagePosChange.Clear();
                                #region 看广告得金币 
                                if (text.TextBlocks.Any(a => a.Text == "关注"))
                                { //text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"00:\d{2}"))
                                    await Task.Delay(30000);
                                    await ClickAsync(device, _device, null, 3000, "返回-赚钱首页");
                                    continue;
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.ViewLive)
                            {
                                PagePosChange.Clear();
                                #region 看直播赚金币 
                                for (int i = 5; i < 11; i++)
                                {
                                    if (text.TextBlocks.Any(a => a.Text == $"已完成{i}/{i}" && a.Text != $"已完成1/1"))
                                    {
                                        await ClickAsync(device, _device, null, 3000, "返回-首页");
                                        continue;
                                    }
                                }
                                if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"今日还剩(\d+)次赚钱机会")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text ==  @"继续赚金币");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 20000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if(text.TextBlocks.Any(a => a.Text == $"已完成0/1"))
                                {
                                    await _cacheService.RemoveAsync("ViewLive_Timer");
                                }
                                int count = await _cacheService.GetAsync<int>("ViewLive_Count");
                                DateTime? Timer = await _cacheService.GetAsync<DateTime>("ViewLive_Timer");
                                if (Timer != DateTime.MinValue)
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-首页");
                                    await SwipeAsync(device, _device, 10000, "滑动-逛街得金币", 500, 1500, 500, 500, 300);
                                    continue;
                                }
                                else
                                {

                                    var Lives = text.TextBlocks.Where(a => a.Text == "直播中").ToList();
                                    if (Lives.Count > 1)
                                    {
                                        var BoxPoint = Lives[new Random().Next(0, Lives.Count)];
                                        if (BoxPoint != null)
                                        {
                                            await _cacheService.SetAsync("ViewLive_Count", count + 1);
                                            await _cacheService.SetAsync("ViewLive_Timer", DateTime.Now);
                                            var pos = BoxPoint.BoxPoints.CalculateTextBlockCenter();
                                            await ClickAsync(device, _device, new List<OCRPoint>() { new OCRPoint(pos.X + 100, pos.Y + 50) }, 40000, BoxPoint.Text);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.OneClickMoney)
                            {
                                PagePosChange.Clear();
                                #region 预约领金币
                                if (text.TextBlocks.Any(a => a.Text == "一键领取"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "一键领取");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "立即预约"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即预约");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "不开提醒也能来领"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "不开提醒也能来领");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-首页");
                                    continue;
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.KSShopMall)
                            {
                                PagePosChange.Clear();
                                #region 逛快手商城赚金币
                                if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"再逛(\d+)秒")))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => Regex.IsMatch(a.Text, @"再逛(\d+)秒"));
                                    if (BoxPoint != null)
                                    {
                                        Match match = Regex.Match(BoxPoint.Text, @"再逛(\d+)秒");
                                        int Delay = match.Groups[1].Value.ToInt() + 5;
                                        while (Delay >= 0)
                                        {
                                            await SwipeAsync(device, _device, 1000, "滑动-逛快手商城赚金币", 500, 800, 500, 500, 200);
                                            Delay -= 1;
                                        }
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "商城" && a.BoxPoints.Max(x => x.Y) < 300) && text.TextBlocks.Any(a => (a.Text == "精选" || a.Text == "去赚钱") && a.BoxPoints.Max(x => x.Y) > (device.Height - 300)))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去赚钱" || a.Text == "精选");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-首页");
                                    continue;
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.ReLoadAd)
                            {
                                PagePosChange.Clear();
                                #region 刷广告视频赚金币 

                                var recentCount = (DateTime.Now - (arrTask["刷广告视频赚金币"].GetArray().LastOrDefault() ?? "").ToDateTime(DateTime.Now.AddMinutes(18))).TotalMinutes;
                                if (text.TextBlocks.Any(a => a.Text == "50/50"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-赚钱首页");
                                }
                                else if (recentCount <= 5)
                                {
                                    await SwipeAsync(device, _device, new Random().Next(10000, 25000), "赚钱首页", 540, 1500, 540, 1000, 200);
                                    continue;
                                }
                                else
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-赚钱首页");
                                    continue;
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.GuangJie)
                            {
                                PagePosChange.Clear();
                                #region 逛街领金币
                                if(text.TextBlocks.Any(a => a.Text == "领取奖励"))
                                {
                                    bool isError = await _cacheService.GetAsync<DateTime>("GuangJie_Error") != DateTime.MinValue;
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == (isError ? "放弃" : "领取奖励"));
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if(text.TextBlocks.Any(a => a.Text == "点击领取"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "点击领取");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text.Contains("10/10") || a.Text.Contains("明天再来吧") || a.Text.Contains("服务器开小差了")))
                                {
                                    if (text.TextBlocks.Any(a => a.Text.Contains("服务器开小差了"))) 
                                    {
                                        await _cacheService.SetAsync("GuangJie_Error", DateTime.Now);
                                    }
                                    await ClickAsync(device, _device, null, 3000, "返回-逛街得金币恭喜获得");
                                    continue;
                                }
                                else
                                {
                                    // 执行小幅度上滑
                                    await SwipeAsync(device, _device, 4000, "逛街得金币", 500, 1500, 500, 500, 200);
                                    // 执行小幅度下滑
                                    await SwipeAsync(device, _device, 2000, "逛街得金币", 500, 500, 500, 1500, 200);
                                    await SwipeAsync(device, _device, 2000, "逛街得金币", 500, 500, 500, 1500, 200);
                                    continue;
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.DayClockIn)
                            {
                                PagePosChange.Clear();
                                #region 每天打卡拿奖励
                                if (text.TextBlocks.Any(a => a.Text == "去签到"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去签到");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "立即领取"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "立即领取");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "领取奖励") && text.TextBlocks.Any(a => a.Text == "精选" || a.Text == "去赚钱"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "领取奖励");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("观看精彩视频")) && text.TextBlocks.Any(a => a.Text == "去观看"))
                                {
                                    var BoxPoint = text.TextBlocks.LastOrDefault(a => a.Text == "去观看");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("观看广告")) && text.TextBlocks.Any(a => a.Text == "去观看"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "去观看");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => a.Text == "朋友") && text.TextBlocks.Any(a => a.Text == "消息") && text.TextBlocks.Any(a => a.Text == "精选" || a.Text == "去赚钱"))
                                {
                                    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "精选" || a.Text == "去赚钱");
                                    if (BoxPoint != null)
                                    {
                                        await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                        continue;
                                    }
                                }
                                else if (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"倒计时\d+:\d+")))
                                {
                                    await SwipeAsync(device, _device, new Random().Next(5000, 10000), "赚钱首页", 400, 1000, 400, 500, 200);
                                    continue;
                                }
                                else if (!text.TextBlocks.Any(a => a.Text == "去观看") && text.TextBlocks.Any(a => a.Text == "已完成" || a.Text.Contains("当前用户已在") || a.Text.Contains("当前设备已绑定")))
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-首页");
                                    continue;
                                }
                                else { 
                                
                                }
                                #endregion
                            }

                            if (device.CurPage == AppPageEnum.ViewVideo)
                            {
                                PagePosChange.Clear();
                                #region 看视频赚金币

                                DateTime startTime = arrTask["看视频赚金币"].ToDateTime();
                                DateTime endTime = DateTime.Now;

                                if ((endTime - startTime).TotalMinutes > 30)
                                {
                                    await ClickAsync(device, _device, null, 3000, "返回-首页");
                                    continue;
                                }
                                else
                                {
                                    await SwipeAsync(device, _device, new Random().Next(5000, 15000), "赚钱首页刷视频", 540, 1500, 540, 1000, 200);
                                    continue;
                                }
                                #endregion
                            }

                            if (text.TextBlocks.Any(a => a.Text.StartsWith("看广告得额外金币")))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text.StartsWith("看广告得额外金币"));
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "点击重播"))
                            {
                                var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "点击重播");
                                if (BoxPoint != null)
                                {
                                    await ClickAsync(device, _device, BoxPoint.BoxPoints, 2000, BoxPoint.Text);
                                    continue;
                                }
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "拼多多App下载"))
                            {
                                await ClickAsync(device, _device, null, 3000, "直播间返回");
                                continue;
                            }
                            else if (text.TextBlocks.Any(a => a.Text == "已领取") && text.TextBlocks.Any(a => a.Text.Contains("更多直播")))
                            {
                                await ClickAsync(device, _device, null, 3000, "直播间返回");
                                continue;
                            }

                            #endregion

                            //当上面没有操作时就执行下面
                            if (!IsOperate)
                            {
                                #region 页面停留超时
                                if (text.TextBlocks.Any(a => a.Text.StartsWith("立即下载")) && text.TextBlocks.Any(a => a.Text == "侵权投诉"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "立即下载返回");
                                    continue;
                                }
                                //else if (text.TextBlocks.Slice(0, 5).Any(a => a.Text == "关注"))
                                //{
                                //    await ClickAsync(device, _device, null, 3000, "直播间返回");
                                //    continue;
                                //}
                                //else if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么")) || text.TextBlocks.Any(a => a.Text == "关注"))
                                //{
                                //    await ClickAsync(device, _device, null, 3000, "直播间返回");
                                //}
                                else if (text.TextBlocks.Any(a => a.Text.StartsWith("更多直播")) || text.TextBlocks.Any(a => a.Text == "退出直播间"))
                                {
                                    await ClickAsync(device, _device, null, 3000, "退出直播间");
                                    continue;
                                }
                                else if ((DateTime.Now - device.LastClickTime).TotalSeconds > 60)
                                {
                                    if (_deviceService.CurAdbClient.GetAppStatus(_device, device.PackageName) != AppStatus.Foreground)
                                    {
                                        await StartAppAsync(device, _device, 3000);
                                    }
                                    else await ClickAsync(device, _device, null, 2000, "返回-停留页面超过60秒");
                                    continue;
                                }
                                else if ((DateTime.Now - device.LastClickTime).TotalSeconds > 30)
                                {
                                    if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么")))
                                    {
                                        await ClickAsync(device, _device, null, 2000, "返回-停留页面超过30秒");
                                        continue;
                                    }
                                }
                                if (device.CurPage != AppPageEnum.MoneyHome)
                                {
                                    //_deviceService.UpdateTaskData();
                                }
                                #endregion
                            }

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
                // 等待1秒
                await Task.Delay(3000, cancellationToken).ContinueWith(t =>
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
            _logService.LogError($"自动化过程出错: {ex.Message}", device.Id);
        }
    }

    private async Task HandleSlideUpPageInteractions(DeviceModel device, DeviceData _device, OCRResult text, CancellationToken cancellationToken)
    {
        bool isDesktop = false;
        DateTime dateTime = DateTime.Now;
        if (text.TextBlocks.Any(a => a.Text == "快手极速版") && text.TextBlocks.Any(a => a.Text == "快手"))
        {
            isDesktop = true;
        }
        if (isDesktop && dateTime.Hour >= 5)
        {
            _ = arrTask.Remove("支付宝", out _);
            await Task.Delay(5000, cancellationToken);
        }
        else if (isDesktop && dateTime.Hour >= 3 && dateTime.Hour < 5)
        {
            _ = arrTask.Remove("快手极速版", out _);
            await Task.Delay(5000, cancellationToken);
        }
        else if (isDesktop && dateTime.Hour >= 0 && dateTime.Hour < 3)
        {
            _ = arrTask.Remove("快手", out _);
            await Task.Delay(5000, cancellationToken);
        }

        if (
            (text.TextBlocks.Any(a => a.Text == "软件商店") && text.TextBlocks.Any(a => a.Text == "主题商店")) ||
            (text.TextBlocks.Any(a => a.Text == "华为商城") && text.TextBlocks.Any(a => a.Text == "应用市场"))
            )
        {
            await SwipeAsync(device, _device, 2000, "左滑", 500, 500, 100, 500, 400);
        }
        else if (text.TextBlocks.Any(a => a.Text == "任务中心" || a.Text.StartsWith("说点什么")))
        {
            await ClickAsync(device, _device, null, 2000, "返回-首页");
        }
        else if (dateTime.Hour >= 5 && !arrTask.ContainsKey("支付宝"))
        {
            _logService.LogInfo($"打开支付宝", device.Id);
            _ = arrTask.TryAdd("支付宝", DateTime.Now.ToString());

            _deviceService.CleanDeviceRunningApps(device.Id);
            await Task.Delay(15000, cancellationToken);

            device.PackageName = PlatformType.AliPay.GetPackageName();
            await StartAppAsync(device, _device, 10000);
        }
        else if (dateTime.Hour >= 3 && dateTime.Hour < 5 && !arrTask.ContainsKey("快手极速版"))
        {
            _logService.LogInfo($"打开快手极速版", device.Id);
            _ = arrTask.TryAdd("快手极速版", DateTime.Now.ToString());

            _deviceService.CleanDeviceRunningApps(device.Id);
            await Task.Delay(15000, cancellationToken);

            device.PackageName = PlatformType.KuaishouJs.GetPackageName();
            await StartAppAsync(device, _device, 10000);
        }
        else if (dateTime.Hour >= 0 && dateTime.Hour < 3 && !arrTask.ContainsKey("快手"))
        {
            _logService.LogInfo($"准备打开快手", device.Id);
            _ = arrTask.TryAdd("快手", DateTime.Now.ToString());

            _deviceService.CleanDeviceRunningApps(device.Id);
            await Task.Delay(15000, cancellationToken);

            device.PackageName = PlatformType.Kuaishou.GetPackageName();
            await StartAppAsync(device, _device, 10000);
        }
        else if (text.TextBlocks.Any(a => a.Text == "明日可领"))
        {
            _logService.LogInfo("完成支付宝刷视频", device.Id);
            _deviceService.StopDeviceCheck(device);
        }

        else if (
            (text.TextBlocks.Any(a => a.Text == "首页" || a.Text == "我") && text.TextBlocks.Any(a => a.Text == "去赚钱" || a.Text == "精选")) ||
            (text.TextBlocks.Any(a => Regex.IsMatch(a.Text, @"^([\d-\.]+)元$")) && text.TextBlocks.Any(a => a.Text == "关注" || a.Text == "视频" || a.Text == "发现"))
            )
        {
            await _cacheService.RemoveAsync($"KuaishouAdWatching_{device.CurPage.ToString()}");


            int Max = 13000;
            var m = Regex.Match(text.Text, @"(\d{1,2})/(\d{1,2})支");
            if (m.Success)
            {
                Max = GetDynamicMax(m.Groups[2].Value.ToInt());
            }
            var r = new Random().Next(5000, Max);
            if (!text.TextBlocks.Any(a => a.Text == "点击进入直播间"))
            {
                await Task.Delay(r, cancellationToken);
            }
            if (device.Width > 0 && device.Height > 0)
            {
                var t = new Random().Next(330, 400);
                var l = new Random().Next((device.Width / 2) - 100, (device.Width / 2) + 100);
                await SwipeAsync(device, _device, 0, "滑动刷视频", l, (device.Height / 2) + 200, l, ((device.Height / 2) - 300), t);
            }
            else
            {
                await SwipeAsync(device, _device, 0, "滑动刷视频", 300, 1000, 300, 600, 400);
            }
        }
        else if (text.TextBlocks.Any(a => a.Text.StartsWith("说点什么") || a.Text == "立即关注" || a.Text == "已关闭评论" || a.Text.Contains("更多直播") || a.Text == "粉丝团"))// || text.TextBlocks.Any(a => a.Text == "关注")
        {
            await ClickAsync(device, _device, null, 3000, "返回到刷视频");
        }
        else if (device.PackageName == PlatformType.AliPay.GetPackageName() && text.TextBlocks.Any(a => a.Text == "视频") && text.TextBlocks.Any(a => a.Text == "我的") && !text.TextBlocks.Any(a => a.Text == "发现"))
        {
            var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "视频");
            if (BoxPoint != null)
            {
                await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
            }
        }
        //else if ((device.PackageName == PlatformType.Kuaishou.GetPackageName() || device.PackageName == PlatformType.KuaishouJs.GetPackageName()) && text.TextBlocks.Any(a => a.Text == "视频") && text.TextBlocks.Any(a => a.Text == "我的") && !text.TextBlocks.Any(a => a.Text == "发现"))
        //{
        //    var BoxPoint = text.TextBlocks.FirstOrDefault(a => a.Text == "视频");
        //    if (BoxPoint != null)
        //    {
        //        await ClickAsync(device, _device, BoxPoint.BoxPoints, 3000, BoxPoint.Text);
        //    }
        //}
        else
        {
            int count = await _cacheService.GetAsync<int>($"KuaishouAdWatching_{device.CurPage.ToString()}");
            count++;
            await _cacheService.SetAsync($"DataAdWatching_{device.CurPage.ToString()}", count);
            if (count > 10)
            {
                await _deviceService.PlayAudioAsync("error.wav");
                await Task.Delay(10000, cancellationToken);
            }
        }
    }

    private int GetDynamicMax(int value, int baseMax = 15000)
    {
        const int minValue = 5;
        const int maxValue = 13;

        // 确保value在5~20之间
        value = Math.Clamp(value, minValue, maxValue);

        // 线性反比
        int step = 500; // 每增加1个Value减少500ms
        return baseMax - (value - minValue) * step;
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