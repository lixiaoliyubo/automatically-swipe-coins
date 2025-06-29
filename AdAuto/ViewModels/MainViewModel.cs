using System.Collections.ObjectModel;
using System.Windows;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdAuto.Extensions.enums;
using Yuandl.Common.Helper;
using PaddleOCRSharp;
using AdAuto.Extensions.Helper;
using System.Runtime.InteropServices;
using Yuandl.Common.LogClass;
using Microsoft.Extensions.Logging;
using DynamicData.Binding;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using AdAuto.Extensions.Provider;
using System.Net.Sockets;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.DeviceCommands;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
namespace AdAuto.ViewModels
{
    /// <summary>
    /// 主视图模型类，负责管理应用程序的主要业务逻辑和UI状态
    /// </summary>
    public partial class MainViewModel : ObservableObject, IMainViewModel, IDisposable
    {
        #region Properties

        private static readonly ILogger log = LogFactory.CreateLogger<MainViewModel>();




        [ObservableProperty]
        private AppPageEnum curPage;

        [ObservableProperty]
        private string lastClickTime;
        /// <summary>
        /// 获取或设置可用的Android设备列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollectionExtended<DeviceData> devices = new();

        /// <summary>
        /// 获取或设置当前选中的设备
        /// </summary>
        [ObservableProperty]
        private DeviceData? selectedDevice;

        /// <summary>
        /// 获取或设置当前状态文本
        /// </summary>
        [ObservableProperty]
        private string statusText = "未连接设备";

        /// <summary>
        /// 获取或设置日志文本内容
        /// </summary>
        [ObservableProperty]
        private string logText = "";

        /// <summary>
        /// 获取或设置是否显示任务数据
        /// </summary>
        [ObservableProperty]
        private bool showTaskData = false;

        /// <summary>
        /// 获取或设置任务数据文本
        /// </summary>
        [ObservableProperty]
        private string taskDataText = "";

        /// <summary>
        /// 获取或设置任务数据类型列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> taskDataTypes = new();

        private string selectedTaskDataType;
        public string SelectedTaskDataType
        {
            get => selectedTaskDataType;
            set
            {
                SetProperty(ref selectedTaskDataType, value);
                UpdateTaskData();
            }
        }

        /// <summary>
        /// 获取或设置是否运行
        /// </summary>
        [ObservableProperty]
        private bool isRunning = false;

        /// <summary>
        /// 获取或设置是否启用屏幕捕获功能
        /// </summary>
        [ObservableProperty]
        private bool isCaptureScreenEnabled = true;


        /// <summary>
        /// 获取或设置当前选择的平台类型
        /// </summary>
        [ObservableProperty]
        private PlatformType selectedPlatform = PlatformType.Douyin;

        #endregion

        #region Private Fields
        private CancellationTokenSource? cancellationTokenSource;
        private PaddleOCREngine? paddleOCREngine;
        /// <summary>
        /// 获取或设置屏幕截图图像源
        /// </summary>
        private string? _screenImageSource;
        private Dictionary<string, OCRResult> _ocrCache = new();
        private readonly IAdWatchingFactory _adWatchingFactory;
        private IAdWatching? _currentAdWatching;
        public AdbClient? _adbClient;
        public DeviceClient? _deviceClient;

        /// <summary>
        /// 初始化主视图模型的新实例
        /// </summary>
        #endregion

        #region Constructor
        public MainViewModel()
        {
            InitializeTesseract();

            // 初始化广告观看工厂
            _adWatchingFactory = new AdWatchingFactory();
            _adbClient = new AdbClient();
            InitializeAdb();
        }

        #endregion

        #region Initialization Methods
        private void InitializeTesseract()
        {
            // 使用默认参数
            var oCRParameter = new OCRParameter
            {
                cpu_math_library_num_threads = 10, // 预测并发线程数
                enable_mkldnn = true, // web部署该值建议设置为0, 否则出错，内存如果使用很大，建议该值也设置为0.
                cls = false, // 是否执行文字方向分类；默认false
                det = true, // 是否开启方向检测，用于检测识别180旋转
                use_angle_cls = false, // 是否开启方向检测，用于检测识别180旋转
                det_db_score_mode = true // 是否使用多段线，即文字区域是用多段线还是用矩形
            };

            // 建议程序全局初始化一次即可，不必每次识别都初始化，容易报错。
            paddleOCREngine = new PaddleOCREngine(null, oCRParameter);
        }

        /// <summary>
        /// 初始化ADB服务和客户端
        /// </summary>
        private void InitializeAdb()
        {
            try
            {
                // Start ADB server if needed
                var srv = new AdbServer();
                if (!srv.GetStatus().IsRunning)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var adbPath = FindAdbPath();
                        srv.StartServer(adbPath, false);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        srv.StartServer("/usr/bin/adb", false);
                    }
                    else
                    {
                        log.LogWarning("Can't automatically start the ADB server on this platform.");
                    }
                }
                OnRefreshDevices();
            }
            catch (Exception ex)
            {
                LogMessage($"ADB初始化失败: {ex.Message}");
                MessageBox.Show($"ADB初始化失败: {ex.Message}\n请确保ADB已安装并添加到系统路径中。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 更新任务数据显示
        /// </summary>
        public void UpdateTaskData()
        {
            if (ShowTaskData && _currentAdWatching is not null)
            {
                var sb = new System.Text.StringBuilder();
                if (string.IsNullOrEmpty(SelectedTaskDataType))
                {
                    sb.AppendLine("当前任务执行情况：");
                    foreach (var task in _currentAdWatching.arrTask)
                    {
                        sb.AppendLine($"\n{task.Key}：");
                        foreach (var time in task.Value)
                        {
                            sb.AppendLine($"  - {time}");
                        }
                    }
                }
                else
                {
                    if (_currentAdWatching.arrTask.TryGetValue(SelectedTaskDataType, out var times))
                    {
                        sb.AppendLine($"当前任务：{SelectedTaskDataType}");
                        sb.AppendLine();
                        foreach (var time in times)
                        {
                            sb.AppendLine($"  - {time}");
                        }
                    }
                }
                TaskDataText = sb.ToString();
            }
        }



        /// <summary>
        /// 清空选中的任务数据类型
        /// </summary>
        #endregion

        #region Command Methods

        /// <summary>
        /// 当ScrcpyDisplay控件加载完成时调用
        /// </summary>
        [RelayCommand]
        private void OnScrcpyDisplayLoaded(RoutedEventArgs args)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), @"ScreenImageSource.png");
            // 释放旧的ScreenImageSource

        }

        [RelayCommand]
        private void OnClearSelectedTaskDataType()
        {
            SelectedTaskDataType = string.Empty;
        }

        /// <summary>
        /// 启动Scrcpy投屏
        /// </summary>
        [RelayCommand]
        private void OnScrcpyStartRun()
        {
            if (SelectedDevice == null)
            {
                MessageBox.Show("请选择设备和文件路径", "友情提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            //scrcpy --tcpip=192.168.1.1:5555
            var path = "./scrcpy/scrcpy.exe";
            Task.Run(async () =>
            {
                var result = await ProcessHelper.StartExE(path, $"-s {SelectedDevice.Value.Serial} --no-audio --max-fps=60");//, $"{AdbPath} start-server");
                LogMessage($" {result}");
            });
        }
        /// <summary>
        /// 刷新可用的Android设备列表
        /// </summary>
        [RelayCommand]
        private void OnRefreshDevices()
        {
            try
            {
                Devices.Clear();
                Devices.Load(_adbClient.GetDevices());
                if (Devices.Count == 0)
                {
                    LogMessage("未找到设备");
                    StatusText = "未找到设备";
                }
                else
                {
                    LogMessage($"找到 {Devices.Count} 个设备");
                    SelectedDevice = Devices[0];
                }
            }
            catch (Exception ex)
            {
                LogMessage($"刷新设备列表失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 开始自动化操作
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        [RelayCommand]
        private void OnStartAdnStop()
        {
            if (IsRunning)
            {
                cancellationTokenSource?.Cancel();
                StopAutomation();
                return;
            }

            if (_adbClient == null)
            {
                MessageBox.Show("AdbClient没有初始化", "友情提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (SelectedDevice == null)
            {
                MessageBox.Show("请选择设备和文件路径", "友情提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _deviceClient = new DeviceClient(_adbClient, SelectedDevice.Value);

            try
            {
                StatusText = "已连接设备";
                IsRunning = true;

                cancellationTokenSource = new CancellationTokenSource();
                _currentAdWatching = _adWatchingFactory.CreateAdWatching(SelectedPlatform, this);
                // 更新任务数据类型列表
                if (!TaskDataTypes.Any())
                {
                    foreach (var task in _currentAdWatching.arrTask.Keys)
                    {
                        TaskDataTypes.Add(task);
                    }
                    if (TaskDataTypes.Any())
                    {
                        SelectedTaskDataType = TaskDataTypes.First();
                    }
                }


                _ = _currentAdWatching.AutomateAdWatching(cancellationTokenSource.Token);

            }
            catch (Exception ex)
            {
                LogMessage($"发生错误: {ex.Message}");
                cancellationTokenSource?.Cancel();
                StopAutomation();
            }
            finally
            {
            }
        }


        /// <summary>
        /// 停止自动化过程并重置UI状态
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        public void StopAutomation()
        {
            cancellationTokenSource?.Dispose();

            IsRunning = false;
            _deviceClient = null;
            LogMessage("已停止自动刷广告");
            StatusText = $"已连接: {SelectedDevice?.Serial}";
        }


        #endregion

        #region Device Operation Methods


        public async Task ClickAsync(List<OCRPoint>? points = null, int delay = 0, string text = "", AppPageEnum curPage = AppPageEnum.Node)
        {

            if (_deviceClient != null)
            {
                CurPage = curPage;
                LastClickTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (points != null)
                {
                    LogMessage($"位置：{CurPage}{CurPage.GetDescription()}，点击了：{text}");
                    if (points.Count == 1)
                    {
                        System.Drawing.Point center = points.CalculateTextBlockCenter();
                        await _deviceClient.ClickAsync(center);
                    }
                    else
                    {
                        System.Drawing.Point center = points.CalculateTextBlockCenter();
                        await _deviceClient.ClickAsync(center);
                    }
                }
                else
                {
                    LogMessage($"位置：{CurPage}{CurPage.GetDescription()}，点击了：{text}");
                    await _deviceClient.ClickBackButtonAsync();
                }

                if (delay > 0)
                {
                    await Task.Delay(delay);
                }
            }
        }
        public async Task SwipeAsync(int delay, string text = "", params int[] points)
        {
            if (_deviceClient != null)
            {
                LastClickTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                LogMessage($"位置：{CurPage}{CurPage.GetDescription()}，滑动了：{text}");
                await _deviceClient.SwipeAsync(points[0], points[1], points[2], points[3], points[4]);

                if (delay > 0)
                {
                    await Task.Delay(delay);
                }
            }
        }
        public async Task ClickHomeButtonAsync(int delay = 0, string text = "")
        {
            if (_deviceClient != null)
            {
                LastClickTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                LogMessage($"位置：{CurPage}{CurPage.GetDescription()}，滑动了：{text}");
                await _deviceClient.ClickHomeButtonAsync();

                if (delay > 0)
                {
                    await Task.Delay(delay);
                }
            }
        }
        /// <summary>
        /// 启动屏幕捕获功能
        /// </summary>
        #endregion



        #region Screen Capture Methods


        /// <summary>
        /// 捕获设备屏幕截图
        /// </summary>
        /// <returns>设备屏幕的位图，如果捕获失败则返回null</returns>
        private async Task<System.Drawing.Bitmap?> CaptureDeviceScreen()
        {
            if (SelectedDevice != null && _adbClient != null)
            {
                try
                {
                    using Framebuffer? framebuffer = await _adbClient.GetFrameBufferAsync(SelectedDevice.Value, CancellationToken.None);
                    if (framebuffer != null)
                    {
                        //return framebuffer.ToImage();
                        return ImageHelper.ByteArrayToBitmap(framebuffer.Data, (int)framebuffer.Header.Width, (int)framebuffer.Header.Height);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"屏幕捕获失败: {ex.Message}");
                    if (ex is SocketException)
                    {
                        StopAutomation();
                    }
                }
            }

            return null;
        }
        #endregion


        #region Helper Methods
        // 添加OCR结果缓存，避免重复识别相同图像);

        public async Task<OCRResult?> RecognizeText()
        {
            if (paddleOCREngine == null)
            {
                LogMessage("OCR引擎未初始化");
                return null;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), @"ScreenImageSource.png");
            if (_adbClient == null || SelectedDevice == null) return null;
            try
            {
                using var framebuffer = await _adbClient.GetFrameBufferAsync(SelectedDevice.Value);
                if (framebuffer != null) {
                    var arr = framebuffer.Data?.ToArray();
                    // 1. 使用 ImageSharp 加载原始数据（假设为 BGRA32 格式）
                    using var image = Image.LoadPixelData<Bgra32>(arr, (int)framebuffer.Header.Width, (int)framebuffer.Header.Height);

                    // 2. 转换为 PNG byte[]
                    using var memoryStream = new MemoryStream();
                    await image.SaveAsBmpAsync(memoryStream);
                    var ocrResult = paddleOCREngine.DetectText(memoryStream.ToArray());
                    if (ocrResult != null)
                    {
                        image.Dispose();
                        return ocrResult;
                    }

                }


                //using Bitmap? bitmap = await CaptureDeviceScreen();
                //if (bitmap != null)
                //{
                //    var ocrResult = paddleOCREngine.DetectText(bitmap);
                //    bitmap.Dispose();
                //    if (ocrResult != null)
                //    {
                //        return ocrResult;
                //    }
                //}
            }
            catch (Exception ex)
            {
                LogMessage($"文字识别失败: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 记录日志消息
        /// </summary>
        /// <param name="message">要记录的消息</param>
        public void LogMessage(string message)
        {
            string logLines = GetLastNLines(LogText, 100);

            LogText = logLines + Environment.NewLine + $"[{DateTime.Now:HH:mm:ss}] {message}";
        }

        /// <summary>
        /// 查找ADB工具的路径
        /// </summary>
        public static string FindAdbPath()
        {
            // 从环境变量中查找
            var androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
            if (!androidHome.IsNotEmptyOrNull())
            {
                var adbPath = System.IO.Path.Combine(androidHome, "platform-tools", "adb.exe");
                if (File.Exists(adbPath))
                {
                    return adbPath;
                }
            }

            // 在默认安装目录中查找
            var defaultPaths = new[]
            {
            @"D:\Android\android-sdk\platform-tools",
            @"C:\Program Files\Android\android-sdk\platform-tools",
            @"C:\Program Files (x86)\Android\android-sdk\platform-tools",
            @"C:\Program Files\Android\Android Studio\platform-tools",
            @"C:\Users\%USERNAME%\AppData\Local\Android\Sdk\platform-tools"
        };

            foreach (var path in defaultPaths)
            {
                var expandedPath = Environment.ExpandEnvironmentVariables(path);
                var adbPath = System.IO.Path.Combine(expandedPath, "adb.exe");
                if (File.Exists(adbPath))
                {
                    return adbPath;
                }
            }

            return "adb";
        }


        /// <summary>
        /// 获取字符串中的最后 N 行
        /// </summary>
        /// <param name="logText">日志文本</param>
        /// <param name="n">要提取的行数</param>
        /// <returns>最后 N 行组成的字符串</returns>
        public static string GetLastNLines(string logText, int n)
        {
            if (string.IsNullOrEmpty(logText))
            {
                return string.Empty; // 如果输入为空，直接返回空字符串
            }

            // 按换行符分割字符串
            var lines = logText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            // 提取最后 N 行
            var lastNLines = lines.Length > n ? lines.TakeLast(n) : lines;

            // 将结果合并为字符串并返回
            return string.Join(Environment.NewLine, lastNLines);
        }
        #endregion




        public void Dispose()
        {
            try
            {
                // 停止所有运行中的任务
                cancellationTokenSource?.Cancel();
                StopAutomation();

                if (paddleOCREngine != null)
                {
                    paddleOCREngine.Dispose();
                    paddleOCREngine = null;
                }


                // 建议垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                LogMessage($"资源释放时发生错误: {ex.Message}");
            }
        }
    }
}