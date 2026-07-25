using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using PaddleOCRSharp;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using TimerCheckApp.AdWatching;
using TimerCheckApp.Extensions;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Services;

/// <summary>
/// 设备服务类，负责管理Android设备连接和操作
/// </summary>
public class DeviceService : IDeviceService, IDisposable
{
    #region 字段
    private PaddleOCREngine _ocrEngine;
    private readonly AdbClient _adbClient;
    private readonly AdbServer _adbServer;
    private readonly ILogService _logService;
    private readonly ICacheService _cacheService;
    private readonly Dictionary<string, CancellationTokenSource> _deviceTasks;
    private readonly IWebSocketService _webSocketService;
    private readonly IAdWatchingFactory _adWatchingFactory;
    private readonly ISqlSugarService _sqlSugarService;
    private readonly IAudioService _audioService;
    private readonly object _ocrEngineLock = new object();

    public AdbClient CurAdbClient { get => _adbClient; }
    #endregion

    #region 构造函数和初始化

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logService">日志服务</param>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="adbClient">ADB客户端</param>
    /// <param name="adbServer">ADB服务器</param>
    /// <param name="webSocketService">WebSocket服务</param>
    /// <param name="adWatchingFactory">广告观看工厂</param>
    /// <param name="sqlSugarService">数据库服务</param>
    /// <param name="audioService">音频服务</param>
    public DeviceService(ILogService logService, ICacheService cacheService, AdbClient adbClient, AdbServer adbServer, IWebSocketService webSocketService, IAdWatchingFactory adWatchingFactory, ISqlSugarService sqlSugarService, IAudioService audioService)
    {
        _cacheService = cacheService;
        _logService = logService;
        _adbClient = adbClient;
        _adbServer = adbServer;
        _webSocketService = webSocketService;
        _adWatchingFactory = adWatchingFactory;
        _sqlSugarService = sqlSugarService;
        _audioService = audioService;
        _deviceTasks = new Dictionary<string, CancellationTokenSource>();

        // 启动ADB服务器
        StartAdbServer();
    }
    /// <summary>
    /// 获取OCR引擎实例（懒加载）
    /// </summary>
    /// <returns>OCR引擎实例</returns>
    private PaddleOCREngine GetOCREngine()
    {
        if (_ocrEngine == null)
        {
            lock (_ocrEngineLock)
            {
                if (_ocrEngine == null)
                {
                    try
                    {
                        _logService.LogInfo("初始化OCR引擎", "System");
                        var oCRParameter = new OCRParameter
                        {
                            cpu_math_library_num_threads = 20,
                            enable_mkldnn = true,
                            cls = false,
                            det = true,
                            use_angle_cls = false,
                            det_db_score_mode = true
                        };
                        _ocrEngine = new PaddleOCREngine(null, oCRParameter);
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"初始化OCR引擎失败: {ex.Message}", "System");
                        throw;
                    }
                }
            }
        }
        return _ocrEngine;
    }
    /// <summary>
    /// 启动ADB服务器
    /// </summary>
    private void StartAdbServer()
    {
        try
        {
            var result = _adbServer.StartServer("adb", restartServerIfNewer: false);
            _logService.LogInfo("ADB服务器已启动", "System");
        }
        catch (Exception ex)
        {
            _logService.LogError($"启动ADB服务器失败: {ex.Message}", "System");
        }
    }

    #endregion

    #region 设备管理

    /// <summary>
    /// 获取所有连接的设备
    /// </summary>
    /// <returns>设备模型列表</returns>
    public ObservableCollection<DeviceModel> GetDevices()
    {
        var devices = new ObservableCollection<DeviceModel>();
        try
        {
            var adbDevices = _adbClient.GetDevices();
            foreach (var device in adbDevices)
            {
                var deviceModel = new DeviceModel
                {
                    Id = device.Serial,
                    Name = device.Name ?? GetDeviceName(device.Serial),
                    Status = device.State.ToString(),
                    CurPlatform = enums.PlatformType.DayClock,
                    CurPage = enums.AppPageEnum.DayClockIn,
                    IsRunning = _deviceTasks.ContainsKey(device.Serial) && !_deviceTasks[device.Serial].IsCancellationRequested
                };

                // 获取设备屏幕尺寸
                var sizeReceiver = GetDeviceSize(deviceModel.Id);
                if (sizeReceiver.width > 0 && sizeReceiver.height > 0)
                {
                    deviceModel.Width = sizeReceiver.width;
                    deviceModel.Height = sizeReceiver.height;
                }

                devices.Add(deviceModel);
                //_logService.LogInfo($"发现设备: {deviceModel.Id} ({deviceModel.Name})", deviceModel.Id);
            }
        }
        catch (Exception ex)
        {
            _logService.LogError($"获取设备列表失败: {ex.Message}", "System");
        }
        return devices;
    }

    /// <summary>
    /// 获取设备名称
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>设备名称</returns>
    private string GetDeviceName(string deviceId)
    {
        try
        {
            var device = new DeviceData { Serial = deviceId };
            var receiver = new ConsoleOutputReceiver();
            _adbClient.ExecuteRemoteCommand("getprop ro.product.model", device, receiver);
            return receiver.ToString().Trim();
        }
        catch
        {
            return deviceId;
        }
    }

    #endregion

    #region 设备检查任务

    /// <summary>
    /// 启动设备检查任务
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <param name="screenshotSaveDir">保存截图文件夹位置</param>
    public void StartDeviceCheck(DeviceModel device, string screenshotSaveDir)
    {
        if (_deviceTasks.ContainsKey(device.Id) && !_deviceTasks[device.Id].IsCancellationRequested)
        {
            _logService.LogWarning($"设备 {device.Id} 已经在运行检查任务", device.Id);
            return;
        }

        var cts = new CancellationTokenSource();
        _deviceTasks[device.Id] = cts;
        device.PackageName = device.CurPlatform.GetPackageName();
        //执行截图任务
        Task.Run(async () =>
        {
            _logService.LogInfo($"设备 {device.Id} 开始运行检查任务", device.Id);
            device.IsRunning = true;
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    // 获取当前运行的应用信息
                    var currentApp = GetCurrentApp(device.Id);
                    device.CurrentApp = currentApp;
                    // 更新最后检查时间
                    device.LastCheckTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    _logService.LogError($"设备检查失败: {ex.Message}", device.Id);
                    await Task.Delay(4000, cts.Token).ContinueWith(t => { });
                }

                // 等待1秒
                await Task.Delay(2000, cts.Token).ContinueWith(t =>
                {
                    if (t.IsCanceled)
                        _logService.LogWarning($"任务被取消", device.Id);
                });
            }

            device.IsRunning = false;
            _logService.LogInfo($"设备 {device.Id} 停止运行检查任务", device.Id);
        }, cts.Token);


        //执行广告任务
        Task.Run(async () =>
        {
            _logService.LogInfo($"设备 {device.Id} 开始运行广告任务", device.Id);

            // 使用工厂创建广告观看实例
            IAdWatching adWatching = _adWatchingFactory.Create(device.CurPlatform);
            await adWatching.AutomateAdWatching(device, cts.Token);

            _logService.LogInfo($"设备 {device.Id} 停止运行广告任务", device.Id);
        }, cts.Token);
    }

    /// <summary>
    /// 停止设备检查任务
    /// </summary>
    /// <param name="device">设备模型</param>
    public void StopDeviceCheck(DeviceModel device)
    {
        if (_deviceTasks.ContainsKey(device.Id) && !_deviceTasks[device.Id].IsCancellationRequested)
        {
            _deviceTasks[device.Id].Cancel();
            _logService.LogInfo($"设备 {device.Id} 检查任务已停止", device.Id);
        }
    }

    #endregion

    #region 应用管理

    /// <summary>
    /// 获取当前运行的应用信息
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>当前应用包名</returns>
    public string GetCurrentApp(string deviceId)
    {
        try
        {
            var device = new DeviceData { Serial = deviceId };
            var receiver = new ConsoleOutputReceiver();
            _adbClient.ExecuteRemoteCommand("dumpsys window | grep mCurrentFocus", device, receiver);
            var output = receiver.ToString().Trim();

            // 解析输出获取包名
            if (output.Contains("/"))
            {
                var parts = output.Split(' ');
                foreach (var part in parts)
                {
                    if (part.Contains("/"))
                    {
                        return part.Split('/')[0];
                    }
                }
            }
            return output;
        }
        catch (Exception ex)
        {
            _logService.LogError($"获取当前应用失败: {ex.Message}", deviceId);
            return "未知";
        }
    }

    /// <summary>
    /// 清理指定设备上的所有运行中的应用
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    public void CleanDeviceRunningApps(string deviceId)
    {
        try
        {
            _logService.LogInfo($"开始清理设备 {deviceId} 上的所有应用", deviceId);

            // 获取设备上安装的所有包
            var receiver = new ConsoleOutputReceiver();
            _adbClient.ExecuteRemoteCommand("pm list packages -3", new DeviceData { Serial = deviceId }, receiver);
            var output = receiver.ToString().Trim();
            var packages = output.Split('\n').Select(p => p.Replace("package:", "").Trim()).Where(p => !string.IsNullOrEmpty(p));

            _logService.LogInfo($"设备 {deviceId} 上共有 {packages.Count()} 个应用", deviceId);

            // 逐个强制停止应用
            foreach (var package in packages)
            {
                try
                {
                    _adbClient.ExecuteRemoteCommand($"am force-stop {package}", new DeviceData { Serial = deviceId }, new ConsoleOutputReceiver());
                    _logService.LogInfo($"已停止应用: {package}", deviceId);
                }
                catch (Exception ex)
                {
                    _logService.LogError($"停止应用 {package} 失败: {ex.Message}", deviceId);
                }
            }

            _logService.LogInfo($"设备 {deviceId} 上的所有应用已清理完成", deviceId);
        }
        catch (Exception ex)
        {
            _logService.LogError($"清理设备 {deviceId} 上的应用时发生错误: {ex.Message}", deviceId);
        }
    }

    /// <summary>
    /// 清理所有运行中的应用
    /// </summary>
    public void CleanAllRunningApps()
    {
        try
        {
            var devices = _adbClient.GetDevices();
            foreach (var device in devices)
            {
                try
                {
                    var receiver = new ConsoleOutputReceiver();
                    _adbClient.ExecuteRemoteCommand("pm list packages -3", device, receiver);
                    var output = receiver.ToString().Trim();
                    var packages = output.Split('\n')
                        .Select(p => p.Replace("package:", "").Trim())
                        .Where(p => !string.IsNullOrEmpty(p));

                    foreach (var package in packages)
                    {
                        _adbClient.ExecuteRemoteCommand($"am force-stop {package}", device, new ConsoleOutputReceiver());
                        _logService.LogInfo($"已停止应用: {package}", device.Serial);
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"清理设备 {device.Serial} 应用失败: {ex.Message}", device.Serial);
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogError($"清理所有运行应用失败: {ex.Message}", "System");
        }
    }

    #endregion

    #region 截图与OCR

    /// <summary>
    /// 使用ADB截取设备屏幕
    /// </summary>
    /// <param name="device">设备模型</param>
    /// <returns>截图字节数组</returns>
    public async Task<byte[]?> CaptureWithAdb(DeviceModel device)
    {
        try
        {
            var _device = new DeviceData { Serial = device.Id };
            // 使用using语句确保资源正确释放
            using (Framebuffer? framebuffer = await _adbClient.GetFrameBufferAsync(_device, CancellationToken.None))
            {
                if (framebuffer?.Data?.Length > 0)
                {
                    try
                    {                        
                        // 使用扩展方法将Framebuffer转换为Bitmap
                        byte[]? imageBytes = framebuffer.ToBitmapAsync();
                        
                        // 验证图像数据是否有效
                        if (imageBytes == null || imageBytes.Length == 0)
                        {
                            _logService.LogError("获取到的图像数据无效", device.Id);
                            return null;
                        }
                        
                        // 返回有效的图像数据
                        return imageBytes;
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"ADB截图过程中出错: {ex.Message}", device.Id);
                        // 强制进行GC回收
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
                else
                {
                    _logService.LogError("获取到的Framebuffer为空", device.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogError($"ADB截图失败: {ex.Message}", device.Id);
        }
        
        return null;
    }

    /// <summary>
    /// 执行OCR识别获取文本
    /// </summary>
    /// <param name="device">当前设备</param>
    /// <returns>OCR识别结果</returns>
    public async Task<OCRResult?> GetOCRText(DeviceModel device)
    {
        byte[]? imageBytes = null;
        try
        {
            // 读取图片文件
            imageBytes = await CaptureWithAdb(device);

            // 确保图像数据有效
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _logService.LogError("截图数据为空或无效", device.Id);
                return null;
            }

            // 获取OCR引擎并进行识别
            var engine = GetOCREngine();
            
            // 使用try-catch包装OCR识别过程，防止内存访问异常
            try
            {
                var ocrResult = engine.DetectText(imageBytes);

                // 更新设备的最后OCR结果
                device.LastOcrResult = ocrResult?.Text ?? "";

                return ocrResult;
            }
            catch (System.AccessViolationException ex)
            {
                _logService.LogError($"OCR引擎内存访问异常: {ex.Message}", device.Id);
                // 强制进行GC回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return null;
            }
        }
        catch (Exception ex)
        {
            _logService.LogError($"OCR识别失败: {ex.Message}", device.Id);
            return null;
        }
        finally
        {
            // 释放大对象
            imageBytes = null;
            // 建议进行垃圾回收
            GC.Collect();
        }
    }

    #endregion

    #region 音频播放

    /// <summary>
    /// 播放提示音
    /// </summary>
    /// <param name="fileName">音频文件名</param>
    public async Task PlayAudioAsync(string fileName)
    {
        await _audioService.PlayAsync(fileName);
    }
    
    /// <summary>
    /// 播放文本内容（文本转语音）
    /// </summary>
    /// <param name="text">要播放的文本内容</param>
    /// <returns>播放任务</returns>
    public async Task PlayTextAsync(string text)
    {
        await _audioService.PlayTextAsync(text);
    }

    /// <summary>
    /// 停止所有音频播放
    /// </summary>
    public void StopAllAudio()
    {
        _audioService.StopAll();
    }

    #endregion

    #region 资源释放

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 停止所有设备任务
        foreach (var cts in _deviceTasks.Values)
        {
            cts.Cancel();
        }
        _deviceTasks.Clear();

        // 停止所有音频播放
        StopAllAudio();

        // 释放OCR引擎资源
        lock (_ocrEngineLock)
        {
            if (_ocrEngine != null)
            {
                _ocrEngine.Dispose();
                _ocrEngine = null;
            }
        }
    }

    #endregion

    #region 设备扩展方法

    /// <summary>
    /// 获取设备屏幕尺寸
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>尺寸元组 (宽度, 高度)</returns>
    public (int width, int height) GetDeviceSize(string deviceId)
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
            _logService.LogError($"获取设备尺寸失败: {ex.Message}", deviceId);
            return (0, 0);
        }
    }

    #endregion
}