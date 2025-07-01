



// 导入 Windows API 函数
using AdAuto.Extensions.Helper;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using TimerCheckApp.enums;
using TimerCheckApp.Extensions;
using TimerCheckApp.Services;
using TimerCheckApp.Services.Interfaces;
using Yuandl.ThemeUI.Controls;
using Point = OpenCvSharp.Point;
namespace ConsoleApp1;
public class ConsoleApp1
{
    static int GetDynamicMax(int value, int baseMax = 15000)
    {
        const int minValue = 5;
        const int maxValue = 20;

        // 确保value在5~20之间
        value = Math.Clamp(value, minValue, maxValue);

        // 线性反比
        int step = 500; // 每增加1个Value减少500ms
        return baseMax - (value - minValue) * step;

        /* 或者使用非线性：
        double decay = 1.0 / (1 + 0.1 * (value - minValue));
        return (int)(baseMax * decay);
        */
    }
    /// <summary>
    /// 原始Main方法
    /// </summary>
    static void Main1(string[] args)
    {
        ConcurrentDictionary<string, string> arrTask = new()
        {
            ["快手"] = string.Empty,
            ["抖音"] = string.Empty,
            ["快手极速版"] = string.Empty,
        };

        _ = arrTask.Remove("快手极速版", out _);

        var PackageName = PlatformType.KuaishouJs.GetPackageName();


        // 初始化ADB连接
        var adbClient = new AdbClient();


        // 获取第一个连接的设备
        var devices = adbClient.GetDevices();

        foreach (var device in devices)
        {

            Task.Run(async () =>
            {
                var status = adbClient.GetAppStatus(device, PackageName);
                Console.WriteLine($"App运行状态: {status}");

                // 获取设备上安装的所有包
                var receiver = new ConsoleOutputReceiver();
                adbClient.ExecuteRemoteCommand("pm list packages -3", device, receiver);
                var output = receiver.ToString().Trim();
                var packages = output.Split('\n').Select(p => p.Replace("package:", "").Trim()).Where(p => !string.IsNullOrEmpty(p));

                Console.WriteLine($"设备 {device.Serial} 上共有 {packages.Count()} 个应用");

                // 逐个强制停止应用
                foreach (var package in packages)
                {
                    try
                    {
                        adbClient.ExecuteRemoteCommand($"am force-stop {package}", device, new ConsoleOutputReceiver());
                        Console.WriteLine($"已停止应用: {package}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"停止应用 {package} 失败: {ex.Message}");
                    }
                }
                await Task.Delay(10000);
                await adbClient.StartAppAsync(device, PackageName);
            });


        }
        Console.WriteLine("完成...");

        Console.ReadKey();
    }
    
    /// <summary>
     /// 缓存服务示例 - 展示FileCacheService和MemoryCacheService的使用
     /// </summary>
     static void Main(string[] args)
     {
        int BaseMax = GetDynamicMax(18); 
        Console.WriteLine("===== 缓存服务示例 =====\n");
#if WPF
    // WPF 专用代码
    Console.WriteLine("当前是 WPF 应用程序");
#else
        // 其他环境代码
        Console.WriteLine("非 WPF 环境");
#endif
        // 创建日志服务（缓存服务依赖）
        ILogService logService = new LogService();
         
         // 创建内存缓存服务
         Console.WriteLine("1. 创建内存缓存服务 (MemoryCacheService)...");
         //ICacheService memoryCache = new MemoryCacheService(logService);
         
         // 创建文件缓存服务
         Console.WriteLine("2. 创建文件缓存服务 (FileCacheService)...");
         ICacheService fileCache = new FileCacheService(logService, "TestCache");
         
         // 测试内存缓存
         //TestCacheService(memoryCache, "内存缓存");
         
         // 测试文件缓存
         TestCacheService(fileCache, "文件缓存");
         
         
         Console.WriteLine("\n按任意键退出...");
         Console.ReadKey();
     }
    
    /// <summary>
     /// 测试缓存服务的通用方法
     /// </summary>
     /// <param name="cacheService">缓存服务实例</param>
     /// <param name="cacheName">缓存名称（用于显示）</param>
     private static void TestCacheService(ICacheService cacheService, string cacheName)
     {
         Console.WriteLine($"\n===== 测试{cacheName} =====");
         
         // 1. 设置字符串缓存
         string stringKey = $"{cacheName}_test_string";
         string stringValue = "这是一个测试字符串";
         Console.WriteLine($"设置字符串缓存: {stringKey} = {stringValue}");
         cacheService.Set(stringKey, stringValue);
         
         // 2. 设置对象缓存
         string objectKey = $"{cacheName}_test_object";
         var objectValue = new TestCacheObject
         {
             Id = 1,
             Name = "测试对象",
             CreateTime = DateTime.Now
         };
         Console.WriteLine($"设置对象缓存: {objectKey} = {objectValue.Name}");
         cacheService.Set(objectKey, objectValue);
         
         // 3. 设置带过期时间的缓存
         string expireKey = $"{cacheName}_test_expire";
         string expireValue = "这个缓存将在5秒后过期";
         Console.WriteLine($"设置带过期时间的缓存: {expireKey} = {expireValue} (5秒)");
         cacheService.Set(expireKey, expireValue, TimeSpan.FromSeconds(5));
         
         // 4. 检查缓存是否存在
         Console.WriteLine($"检查缓存是否存在: {stringKey} = {cacheService.Exists(stringKey)}");
         Console.WriteLine($"检查缓存是否存在: 不存在的键 = {cacheService.Exists("not_exist")}");
         
         // 5. 获取缓存
         string retrievedString = cacheService.Get<string>(stringKey);
         Console.WriteLine($"获取字符串缓存: {stringKey} = {retrievedString}");
         
         var retrievedObject = cacheService.Get<TestCacheObject>(objectKey);
         Console.WriteLine($"获取对象缓存: {objectKey} = {retrievedObject?.Name}, 创建时间: {retrievedObject?.CreateTime}");
         
         // 6. 等待过期缓存过期
         Console.WriteLine("等待5秒让过期缓存失效...");
         Thread.Sleep(5500);
         string expiredValue = cacheService.Get<string>(expireKey);
         Console.WriteLine($"获取过期缓存: {expireKey} = {(expiredValue == null ? "已过期" : expiredValue)}");
         
         // 7. 移除缓存
         Console.WriteLine($"移除缓存: {stringKey}");
         cacheService.Remove(stringKey);
         Console.WriteLine($"检查缓存是否存在: {stringKey} = {cacheService.Exists(stringKey)}");
         
         // 8. 清空所有缓存
         Console.WriteLine("清空所有缓存");
         cacheService.Clear();
         Console.WriteLine($"检查缓存是否存在: {objectKey} = {cacheService.Exists(objectKey)}");
     }
     
    /// <summary>
    /// 用于缓存测试的示例类
    /// </summary>
    private class TestCacheObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 音频输出
    /// </summary>
    /// <param name="args"></param>
    static void Main308(string[] args)
    {
        using (var synthesizer = new SpeechSynthesizer())
        {
            // 设置输出为WAV文件
            synthesizer.SetOutputToWaveFile(@"E:\AdAuto\TimerCheckApp\Resources\audio\error.wav");

            // 选择语音（可选）
            foreach (var voice in synthesizer.GetInstalledVoices())
            {
                if (voice.VoiceInfo.Culture.Name == "zh-CN")
                {
                    synthesizer.SelectVoice(voice.VoiceInfo.Name);
                    break;
                }
            }

            // 设置语音速率（-10到10）
            synthesizer.Rate = 0;
            synthesizer.Volume = 100; // 音量（0-100）

            // 开始转换
            synthesizer.Speak("主人，任务出现问题，请及时查看");
        }
        Console.ReadKey();
    }
    /// <summary>
    /// 检测当前设备活动的App
    /// </summary>
    /// <param name="args"></param>
    static void Main307(string[] args)
    {
        // 初始化ADB连接
        var adbClient = new AdbClient();

        try
        {
            // 获取第一个连接的设备
            var device = adbClient.GetDevices().ToList()[0];

            if (device == null)
            {
                Console.WriteLine("错误：未检测到连接的设备");
                return;
            }
            var sss = adbClient.GetAppStatus(device, "com.eg.android.AlipayGphone");
            Console.WriteLine($"正在检测设备: {device.Serial}");

            // 方案2：备用检测方法
            Console.WriteLine("标准方法未获取到焦点，尝试备用方案...");
            var focusInfo = GetAlternativeFocusInfo(adbClient, device);
            Console.WriteLine(string.IsNullOrEmpty(focusInfo) ?
                "无法获取当前窗口焦点信息" :
                $"当前活动窗口: {focusInfo}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生异常: {ex.Message}");
        }
    }

    // 方法2：备用活动检测方案
    static string GetAlternativeFocusInfo(AdbClient adbClient, DeviceData device)
    {
        var receiver = new ConsoleOutputReceiver();

        // 尝试多种备用命令
        string[] altCommands = {
            "dumpsys activity activities | grep ResumedActivity",
            "dumpsys activity top | grep ACTIVITY",
            "dumpsys window policy | grep isStatusBarKeyguard"
        };

        foreach (var cmd in altCommands)
        {
            adbClient.ExecuteRemoteCommand(cmd, device, receiver);
            var output = receiver.ToString().Trim();

            if (!string.IsNullOrEmpty(output))
                return output;

            receiver = new ConsoleOutputReceiver();
        }

        return null;
    }
    static async Task Main306()
    {
        var stopwatch = new Stopwatch();
        var _adbClient = new AdbClient();
        var adbClients = _adbClient.GetDevices();

        foreach (var _device in adbClients.Where(a => a.Serial.Contains("f41f8502")))//OB8HDMK7R8EQEE95//KWFRX19412002479
        {
            // 执行 dumpsys window windows 命令
            var receiver = new ConsoleOutputReceiver();
            _adbClient.ExecuteRemoteCommand("dumpsys window windows", _device, receiver);
            var sizeReceiver = GetDeviceSize(_adbClient, _device.Serial);
            await _adbClient.SendKeyEventAsync(_device, "KEYCODE_APP_SWITCH", CancellationToken.None);

            await Task.Delay(2000);
            await _adbClient.SwipeAsync(_device, new System.Drawing.Point(100, 500), new System.Drawing.Point(300, 500), 1000, CancellationToken.None);

            Console.WriteLine($"");
            Console.WriteLine($"-----------------------------------");
        }
        Console.ReadLine();
    }
    static async Task Main208()
    {
        var _adbClient = new AdbClient();
        var adbClients = _adbClient.GetDevices();
        (int width, int height) size = new(0, 0);
        foreach (var _device in adbClients.Where(a => a.Name.Contains("OP5627")))
        {
            int i = 0;
            size = GetDeviceSize(_adbClient, _device.Serial);
            while (true)
            {
                int sy = 0;
                int ey = 0;
                if ((i % 2) == 0)
                {
                    sy = (size.height / 2) - 200;
                    ey = (size.height / 2) - 600;

                }
                else
                {
                    sy = size.height - 50;
                    ey = size.height - 500;
                }
                await _adbClient.SwipeAsync(_device, new System.Drawing.Point((size.width / 2), sy), new System.Drawing.Point((size.width / 2), ey), 200);
                Console.WriteLine($"");
                Console.WriteLine($"-----------------------------------");
                Console.WriteLine($"");
                var r = new Random().Next(3000, 10000);
                await Task.Delay(r);
                i++;
            }
        }
    }
    /// <summary>
    /// 获取设备屏幕尺寸
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>尺寸元组 (宽度, 高度)</returns>
    public static (int width, int height) GetDeviceSize(AdbClient _adbClient, string deviceId)
    {
        try
        {
            var device = new DeviceData { Serial = deviceId };
            var receiver = new ConsoleOutputReceiver();
            _adbClient.ExecuteRemoteCommand("wm size", device, receiver);
            var output = receiver.ToString().Trim();

            if (Regex.IsMatch(output, @"(\d+)x(\d+)"))
            {
                var match = Regex.Match(output, @"(\d+)x(\d+)");
                if (match.Success && match.Groups.Count >= 3)
                {
                    int width = int.Parse(match.Groups[1].Value);
                    int height = int.Parse(match.Groups[2].Value);
                    return (width, height);
                }
            }

            return (0, 0);
        }
        catch (Exception ex)
        {
            return (0, 0);
        }
    }
    /// <summary>
    /// 测试截图
    /// </summary>
    /// <returns></returns>
    static async Task Main305()
    {
        var stopwatch = new Stopwatch();
        var _adbClient = new AdbClient();
        var adbClients = _adbClient.GetDevices();

        foreach (var _device in adbClients.Where(a => a.Serial.Contains("KWFRX19412002479")))
        {
            while (true)
            {

                var androidScreenCapturer = new AndroidScreenCapturer(_device.Serial);

                // 测试方法1：GetFrameBufferAsync + ToBitmapAsync
                stopwatch.Restart();
                // 使用using语句确保资源正确释放
                try
                {
                    using (Framebuffer? framebuffer = await _adbClient.GetFrameBufferAsync(_device, CancellationToken.None))
                    {
                        // 使用扩展方法将Framebuffer转换为Bitmap
                        byte[]? imageBytes1 = framebuffer.ToBitmapAsync();
                        androidScreenCapturer.SaveScreenToDesktop(imageBytes1);
                        stopwatch.Stop();
                        Console.WriteLine($"方法1（Framebuffer）耗时: {stopwatch.ElapsedMilliseconds} ms");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"方法1发生错误: {ex.Message}");
                }

                // 测试方法2：直接 CaptureScreen
                stopwatch.Restart();
                byte[]? imageBytes2 = androidScreenCapturer.CaptureScreen();
                androidScreenCapturer.SaveScreenToDesktop(imageBytes2);
                stopwatch.Stop();
                Console.WriteLine($"方法2（ADB命令）耗时: {stopwatch.ElapsedMilliseconds} ms");

                Console.WriteLine($"");
                Console.WriteLine($"-----------------------------------");
                Console.WriteLine($"");
                await Task.Delay(1000);
            }
        }
    }
    /// <summary>
    /// 截取所有设备尺寸
    /// </summary>
    /// <returns></returns>
    static async Task Main304()
    {
        var _adbClient = new AdbClient();
        var adbClients = _adbClient.GetDevices();

        foreach (var _device in adbClients)
        {

            var receiver = new ConsoleOutputReceiver();
            _adbClient.ExecuteRemoteCommand("wm size", _device, receiver);
            var output = receiver.ToString().Trim();

            // 解析输出内容，示例输出如 "Physical size: 1080x2340"
            if (output.Contains(":"))
            {
                string sizePart = output.Split(':')[1].Trim();
                string[] dimensions = sizePart.Split('x');
                if (dimensions.Length == 2 && int.TryParse(dimensions[0], out int width) && int.TryParse(dimensions[1], out int height))
                {
                    Console.WriteLine($"设备屏幕分辨率：{width}x{height}");
                }
                else
                {
                    Console.WriteLine("解析屏幕大小失败，输出内容：" + output);
                }
            }
            else
            {
                Console.WriteLine("解析屏幕大小失败，输出内容：" + output);
            }
        }
    }

    /// <summary>
    /// OpenCvSharp原图匹配后标注位置并显示窗口
    /// </summary>
    static void Main303()
    {
        // 1. 加载原始图片和模板
        Mat src = Cv2.ImRead(@$"D:\quickmirror\11111.png", ImreadModes.Color);  // 原始截图
        Mat template = Cv2.ImRead(@$"D:\quickmirror\im_push_hongbao.png", ImreadModes.Color); // 菜单图标模板

        if (src.Empty() || template.Empty())
        {
            Console.WriteLine("图片加载失败！");
            return;
        }

        // 2. 转换为灰度图（提升匹配效率）
        Mat srcGray = new Mat();
        Mat templateGray = new Mat();
        Cv2.CvtColor(src, srcGray, ColorConversionCodes.BGR2GRAY);
        Cv2.CvtColor(template, templateGray, ColorConversionCodes.BGR2GRAY);


        // 3. 模板匹配（使用归一化相关系数）
        Mat result = new Mat();
        Cv2.MatchTemplate(src, template, result, TemplateMatchModes.CCoeffNormed);

        // 4. 获取最佳匹配位置
        Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

        // 5. 设定匹配阈值（通常 > 0.8 认为有效）
        double threshold = 0.2;
        if (maxVal >= threshold)
        {
            // 标记匹配区域
            Rect matchRect = new Rect(maxLoc, new OpenCvSharp.Size(template.Width, template.Height));
            Cv2.Rectangle(src, matchRect, new Scalar(0, 255, 0), 2); // 绿色框

            // 5. 添加文字标注（位置+置信度）
            string infoText = $"Position:X={maxLoc.X}, Y={maxLoc.Y}  Confidence: {maxVal:F2}";
            Cv2.PutText(
                src,
                infoText,
                new Point(maxLoc.X, maxLoc.Y - 50), // 文字显示在框上方
                HersheyFonts.HersheyDuplex,
                0.8,
                Scalar.Red,
                1
            );
            Console.WriteLine($"匹配成功！位置：X={maxLoc.X}, Y={maxLoc.Y}");
        }
        else
        {
            Console.WriteLine("未找到匹配的图标！");
        }

        // 7. 仅显示裁剪后的区域（验证是否裁剪正确）
        Cv2.NamedWindow("裁剪后区域", WindowFlags.Normal);
        Cv2.ResizeWindow("裁剪后区域", srcGray.Width, srcGray.Height);
        Cv2.ImShow("裁剪后区域", src);
        Cv2.WaitKey(0);
    }
    /// <summary>
    /// 截取所有设备屏幕图
    /// </summary>
    /// <returns></returns>
    static async Task Main302()
    {
        var _adbClient = new AdbClient();
        var adbClients = _adbClient.GetDevices();

        foreach (var _device in adbClients)
        {

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{_device.Name}.png");

            // 使用using语句确保资源正确释放
            using (Framebuffer? framebuffer = await _adbClient.GetFrameBufferAsync(_device, CancellationToken.None))
            {
                if (framebuffer != null)
                {
                    // 使用扩展方法将Framebuffer转换为Bitmap
                    byte[]? imageBytes = null;
                    try
                    {
                        imageBytes = framebuffer.ToBitmapAsync();
                        if (imageBytes != null)
                        {
                            File.WriteAllBytes(path, imageBytes);
                        }
                    }
                    finally
                    {
                        // 立即释放大对象，帮助GC回收
                        imageBytes = null;
                    }
                }
            }
        }
    }

    public static string GetLastNLines(string logText, int n)
    {
        if (string.IsNullOrEmpty(logText))
        {
            return string.Empty; // 如果输入为空，直接返回空字符串
        }

        // 按换行符分割字符串
        var lines = logText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // 提取最后 N 行
        var lastNLines = lines.Length > n ? lines.TakeLast(n) : lines;

        // 将结果合并为字符串并返回
        return string.Join(Environment.NewLine, lastNLines);
    }
    /// <summary>
    /// 生成指定行数的模拟数据
    /// </summary>
    /// <param name="lineCount">需要生成的行数</param>
    /// <returns>包含模拟数据的字符串</returns>
    public static string GenerateSimulatedData(int lineCount)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 1; i <= lineCount; i++)
        {
            sb.AppendLine($"Line{i}");
        }

        return sb.ToString();
    }
    static void Main301()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        // 1. 加载原始图片和模板
        Mat src = Cv2.ImRead($"{desktopPath}\\RE584B.png", ImreadModes.Color);  // 原始截图
        Mat template = Cv2.ImRead($"{desktopPath}\\play.png", ImreadModes.Color); // 菜单图标模板

        if (src.Empty() || template.Empty())
        {
            Console.WriteLine("图片加载失败！");
            return;
        }



        // 2. 转换为灰度图（提升匹配效率）
        Mat srcGray = new Mat();
        Mat templateGray = new Mat();
        Cv2.CvtColor(src, srcGray, ColorConversionCodes.BGR2GRAY);
        Cv2.CvtColor(template, templateGray, ColorConversionCodes.BGR2GRAY);



        // 3. 模板匹配（使用归一化相关系数）
        Mat result = new Mat();
        Cv2.MatchTemplate(srcGray, templateGray, result, TemplateMatchModes.CCoeffNormed);

        // 4. 获取最佳匹配位置
        Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

        // 5. 设定匹配阈值（通常 > 0.8 认为有效）
        double threshold = 0.8;
        if (maxVal >= threshold)
        {
            // 标记匹配区域
            Rect matchRect = new Rect(maxLoc, new OpenCvSharp.Size(template.Width, template.Height));
            Cv2.Rectangle(srcGray, matchRect, new Scalar(0, 255, 0), 2); // 绿色框
            Console.WriteLine($"匹配成功！位置：X={maxLoc.X}, Y={maxLoc.Y}");
        }
        else
        {
            Console.WriteLine("未找到匹配的图标！");
        }

        // 7. 仅显示裁剪后的区域（验证是否裁剪正确）
        Cv2.NamedWindow("裁剪后区域", WindowFlags.Normal);
        Cv2.ResizeWindow("裁剪后区域", srcGray.Width, srcGray.Height);
        Cv2.ImShow("裁剪后区域", srcGray);
        Cv2.WaitKey(0);
    }

    /// <summary>
    /// 裁剪后
    /// </summary>
    static void Main333()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        // 1. 加载原始图片和模板
        Mat src = Cv2.ImRead($"{desktopPath}\\RE584B.png", ImreadModes.Color);  // 原始截图
        Mat template = Cv2.ImRead($"{desktopPath}\\play.png", ImreadModes.Color); // 菜单图标模板

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

        // 5. 设定匹配阈值（通常 > 0.8 认为有效）
        double threshold = 0.8;
        if (maxVal >= threshold)
        {
            // 标记匹配区域
            Rect matchRect = new Rect(maxLoc, new OpenCvSharp.Size(template.Width, template.Height));
            Cv2.Rectangle(srcGray, matchRect, new Scalar(0, 255, 0), 2); // 绿色框
            Console.WriteLine($"匹配成功！位置：X={maxLoc.X}, Y={maxLoc.Y}");
        }
        else
        {
            Console.WriteLine("未找到匹配的图标！");
        }

        // 7. 仅显示裁剪后的区域（验证是否裁剪正确）
        Cv2.NamedWindow("裁剪后区域", WindowFlags.Normal);
        Cv2.ResizeWindow("裁剪后区域", srcGray.Width, srcGray.Height);
        Cv2.ImShow("裁剪后区域", srcGray);
        Cv2.WaitKey(0);
    }
    static void Main334()
    {
        // 1. 加载图像
        Mat src = Cv2.ImRead("C:\\Users\\Administrator\\Desktop\\image.png", ImreadModes.Color);
        Mat gray = new Mat();
        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

        // 2. 边缘检测（Canny）
        Mat edges = new Mat();
        Cv2.Canny(gray, edges, 50, 150);

        // 3. 查找轮廓
        OpenCvSharp.Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(edges, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

        // 4. 筛选右上角轮廓（假设菜单是独立的小图标）
        Rect menuRect = new Rect();
        double maxX = 0;
        double minY = src.Height;

        foreach (var contour in contours)
        {
            Rect rect = Cv2.BoundingRect(contour);
            // 筛选条件：小矩形（图标大小）且靠近右上角
            if (rect.Width < 50 && rect.Height < 50 && rect.X > src.Width * 0.7 && rect.Y < src.Height * 0.3)
            {
                if (rect.X > maxX || rect.Y < minY)
                {
                    maxX = rect.X;
                    minY = rect.Y;
                    menuRect = rect;
                }
            }
        }

        // 5. 标记菜单位置
        if (menuRect.Width > 0)
        {
            Cv2.Rectangle(src, menuRect, new Scalar(0, 255, 0), 2);
            Console.WriteLine($"菜单位置: X={menuRect.X}, Y={menuRect.Y}");
        }

        // 显示结果
        Cv2.ImShow("Detected Menu", src);
        Cv2.WaitKey(0);
    }

    // 定义线程安全的字典来存储用餐时间段
    static ConcurrentDictionary<string, (TimeSpan start, TimeSpan end)> mealTimes = new()
    {
        ["早餐"] = (TimeSpan.Parse("05:00"), TimeSpan.Parse("09:00")),
        ["午餐"] = (TimeSpan.Parse("11:00"), TimeSpan.Parse("14:00")),
        ["晚餐"] = (TimeSpan.Parse("17:00"), TimeSpan.Parse("20:00")),
        ["夜宵"] = (TimeSpan.Parse("21:00"), TimeSpan.Parse("23:59"))
    };


    static void Main222(string[] args)
    {
        // 启动scrcpy进程
        Process scrcpyProcess = new Process();
        scrcpyProcess.StartInfo.FileName = "scrcpy";
        scrcpyProcess.StartInfo.Arguments = "--no-control --stay-awake --video-bit-rate 2M -s <device_serial> - |";
        scrcpyProcess.StartInfo.RedirectStandardOutput = true;
        scrcpyProcess.StartInfo.UseShellExecute = false;
        scrcpyProcess.Start();

        // 启动ffmpeg进程
        Process ffmpegProcess = new Process();
        ffmpegProcess.StartInfo.FileName = "ffmpeg";
        ffmpegProcess.StartInfo.Arguments = "-i pipe:0 -f rawvideo -pix_fmt rgb24 -";
        ffmpegProcess.StartInfo.RedirectStandardInput = true;
        ffmpegProcess.StartInfo.RedirectStandardOutput = true;
        ffmpegProcess.StartInfo.UseShellExecute = false;
        ffmpegProcess.Start();

        // 获取scrcpy的输出流和ffmpeg的输入流
        Stream scrcpyOutputStream = scrcpyProcess.StandardOutput.BaseStream;
        Stream ffmpegInputStream = ffmpegProcess.StandardInput.BaseStream;
        Stream ffmpegOutputStream = ffmpegProcess.StandardOutput.BaseStream;

        // 假设分辨率为1920x1080
        int width = 1920;
        int height = 1080;
        int frameSize = width * height * 3; // RGB24 format

        byte[] buffer = new byte[frameSize];

        while (true)
        {
            int bytesRead = scrcpyOutputStream.Read(buffer, 0, buffer.Length);
            if (bytesRead <= 0)
            {
                break;
            }

            ffmpegInputStream.Write(buffer, 0, bytesRead);

            bytesRead = ffmpegOutputStream.Read(buffer, 0, buffer.Length);
            if (bytesRead <= 0)
            {
                break;
            }

            // 处理每一帧图像数据
            Console.WriteLine("Received a frame of video data.");

            // 这里可以添加对每一帧的处理逻辑，例如显示在窗体上
        }

        scrcpyProcess.WaitForExit();
        ffmpegProcess.WaitForExit();
    }
    static void Main00()
    {
        // 启动 scrcpy 进程
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "scrcpy", // 确保 scrcpy 已添加到系统 PATH
            Arguments = "--no-display --raw-video -", // 输出裸视频流到 stdout
            RedirectStandardOutput = true, // 捕获标准输出
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = new Process())
        {
            process.StartInfo = startInfo;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // 处理视频流数据
                    Console.WriteLine("Received video data: " + e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine(); // 开始异步读取输出

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();

            process.Kill(); // 停止进程
        }
        while (true)
        {
            Thread.Sleep(1000); // 每秒检查一次
            Console.Clear(); // 清除控制台以获得更好的显示效果
        }
    }

    static void CheckTimeInMealPeriod(TimeSpan time)
    {
        foreach (var meal in mealTimes)
        {
            var (start, end) = meal.Value;
            if (time >= start && time < end)
            {
                Console.WriteLine($"时间 {time:hh\\:mm} 处于 {meal.Key} 时间段内。");
                return;
            }
        }
        Console.WriteLine($"时间 {time:hh\\:mm} 不在任何用餐时间段内。");
    }


}


