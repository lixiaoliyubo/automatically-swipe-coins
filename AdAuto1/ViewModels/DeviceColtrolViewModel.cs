using AdAuto.enums;
using AdAuto.Helper;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using PaddleOCRSharp;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Yuandl.Common.Helper;
using Yuandl.Common.LogClass;
using Point = System.Drawing.Point;
namespace AdAuto.ViewModels
{
    /// <summary>
    /// 主视图模型类，负责管理应用程序的主要业务逻辑和UI状态
    /// 快手：com.kuaishou.nebula
    /// 番茄畅听：com.xs.fm
    /// 番茄畅听音乐版：com.xs.fm.lite
    /// 番茄免费小说：com.dragon.read
    /// 抖音极速版：com.ss.android.ugc.aweme.lite
    /// 抖音火山版： com.ss.android.ugc.live
    /// 红果免费短剧：com.phoenix.read
    /// 百度极速版：com.baidu.searchbox.lite
    /// 河马剧场： com.dz.hmjc
    /// 西瓜视频：com.ss.android.article.video
    /// 语空浏览器：com.cat.readall
    /// 西瓜视频：com.ss.android.article.video
    /// 今日头条：com.ss.android.article.news
    /// 今日头条极速版：com.ss.android.article.lite
    /// </summary>
    public partial class DeviceColtrolViewModel : ObservableObject
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

        /// <summary>
        /// 获取或设置选中的任务数据类型
        /// </summary>
        #endregion

        #region Private Fields
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
        /// 获取或设置开始按钮是否可用
        /// </summary>
        [ObservableProperty]
        private bool isStartEnabled = true;

        /// <summary>
        /// 获取或设置停止按钮是否可用
        /// </summary>
        [ObservableProperty]
        private bool isStopEnabled = false;


        /// <summary>
        /// 获取或设置当前选择的平台类型
        /// </summary>
        [ObservableProperty]
        private PlatformType selectedPlatform = PlatformType.Douyin;

        public AdbClient adbClient;
        public DeviceClient deviceClient;
        private CancellationTokenSource? cancellationTokenSource;
        private bool isRunning;
        private PaddleOCREngine? paddleOCREngine;
        private string adbPath;
        public DeviceClient CurDevice
        {
            get => deviceClient;
        }
        private readonly IAdWatchingFactory _adWatchingFactory;
        private IAdWatching? _currentAdWatching;

        /// <summary>
        /// 初始化主视图模型的新实例
        /// </summary>
        #endregion

        #region Constructor
        public DeviceColtrolViewModel()
        {
            adbPath = FindAdbPath();
            InitializeAdb();
            InitializeTesseract();
            adbClient = new AdbClient();
            // 初始化广告观看工厂
            _adWatchingFactory = new AdWatchingFactory();
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
            var srv = new AdbServer();
            if (!srv.GetStatus().IsRunning)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
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

            RefreshDevicesCommand.Execute(null);
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
                        sb.AppendLine($"  - {times}");
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
            var path = "scrcpy\\scrcpy.exe";
            ProcessHelper.RunScrcpy(
                scrcpyPath: path,
                arguments: $"-s {SelectedDevice.Value.Serial} --window-title {SelectedDevice.Value.Name}({SelectedDevice.Value.Serial})  --no-audio --max-fps=60",//, $"{AdbPath} start-server");
                (s) =>  LogMessage($" {s}")
            );
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
                Devices.Load(new AdbClient().GetDevices());
                if (Devices.Count == 0)
                {
                    LogMessage("未找到设备");
                    StatusText = "未找到设备";
                }
                else
                {
                    LogMessage($"找到 {Devices.Count} 个设备");
                    SelectedDevice = Devices[0];
                    if (Devices.Any(a => a.Name.Contains("OP5627"))) {

                        SelectedDevice = Devices.FirstOrDefault(a => a.Name.Contains("OP5627"));
                    }
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
        private async Task OnStartAsync()
        {
            if (SelectedDevice == null)
            {
                MessageBox.Show("请选择设备和文件路径", "友情提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (isRunning)
            {
                return;
            }

            isRunning = true;
            IsStartEnabled = false;
            IsStopEnabled = true;
            TaskDataTypes.Clear();


            LogMessage("开始自动刷广告");
            deviceClient = new DeviceClient(adbClient, SelectedDevice.Value);
            try
            {
                cancellationTokenSource = new CancellationTokenSource();

                //_currentAdWatching = _adWatchingFactory.CreateAdWatching(SelectedPlatform, this);
                _currentAdWatching.UpdateAppPageEnum(CurPage);
                // 更新任务数据类型列表
                foreach (var task in _currentAdWatching.arrTask.Keys)
                {
                    TaskDataTypes.Add(task);
                }
                if (TaskDataTypes.Any())
                {
                    SelectedTaskDataType = TaskDataTypes.First();
                }
                await _currentAdWatching.AutomateAdWatching(cancellationTokenSource.Token);

            }
            catch (Exception ex)
            {
                LogMessage($"发生错误: {ex.Message}");
            }
            finally
            {
                isRunning = false;
                IsStartEnabled = true;
                IsStopEnabled = false;
            }
        }

        /// <summary>
        /// 停止自动化操作
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        [RelayCommand]
        private void Stop()
        {
            StopAutomation();
        }

        /// <summary>
        /// 停止自动化过程并重置UI状态
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        public void StopAutomation()
        {
            if (isRunning)
            {
                cancellationTokenSource?.Cancel();
                isRunning = false;

                IsStartEnabled = true;
                IsStopEnabled = false;

                LogMessage("已停止自动刷广告");
                StatusText = $"已连接: {SelectedDevice.Value.Serial}";
            }
        }


        #endregion

        #region Device Operation Methods
        public async Task ClickAsync(List<OCRPoint>? points = null, int delay = 0, string text = "", AppPageEnum curPage = AppPageEnum.Node)
        {
            CurPage = curPage;
            LastClickTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            // 检查是否重复点击
            //if (lastClickInfo.IsSameClick(points, text))
            //{
            //    LogMessage($"跳过重复点击：{text}");
            //    return;
            //}

            if (points != null)
            {
                LogMessage($"位置：{CurPage}{CurPage.GetDescription()}，点击了：{text}");
                if (points.Count == 1)
                {
                    Point center = new Point() { X = points[0].X, Y = points[0].Y };
                    await deviceClient.ClickAsync(center);
                }
                else
                {
                    Point center = points.CalculateTextBlockCenter();
                    await deviceClient.ClickAsync(center);
                }
            }
            else
            {
                LogMessage($"位置：{CurPage}{CurPage.GetDescription()}，点击了：{text}");
                await deviceClient.ClickBackButtonAsync();
            }

            if (delay > 0)
            {
                await Task.Delay(delay);
            }
        }
        public async Task SwipeAsync(int delay, string text = "", params int[] points)
        {
            LastClickTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            LogMessage($"位置：{CurPage}{CurPage.GetDescription()}，滑动了：{text}");
            await deviceClient.SwipeAsync(points[0], points[1], points[2], points[3], points[4]);

            if (delay > 0)
            {
                await Task.Delay(delay);
            }
        }
        public async Task ClickHomeButtonAsync(int delay = 0, string text = "")
        {
            LastClickTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            LogMessage($"位置：{CurPage}{CurPage.GetDescription()}，滑动了：{text}");
            await deviceClient.ClickHomeButtonAsync();

            if (delay > 0)
            {
                await Task.Delay(delay);
            }
        }
        public async Task SendTextAsync(int delay = 0, string text = "")
        {
            LastClickTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            LogMessage($"位置：{CurPage}{CurPage.GetDescription()}，发送本文：{text}");
            await deviceClient.AdbClient.ExecuteShellCommandAsync(SelectedDevice.GetValueOrDefault(), $"shell am broadcast -a clipper.set -e text \"{text}\"");
            await deviceClient.AdbClient.ExecuteShellCommandAsync(SelectedDevice.GetValueOrDefault(), $"input keyevent 279");
            if (delay > 0)
            {
                await Task.Delay(delay);
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
        public async Task<byte[]?> CaptureDeviceScreen()
        {
            if (SelectedDevice != null)
            {
                try
                {
                    using Framebuffer? framebuffer = await adbClient.GetFrameBufferAsync(SelectedDevice.Value, CancellationToken.None);
                    if (framebuffer != null)
                    {
                        return ImageHelper.FrameBufferDataToImageBytes(framebuffer.Data, (int)framebuffer.Header.Width, (int)framebuffer.Header.Height);
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
        // 添加OCR结果缓存，避免重复识别相同图像

        public async Task<OCRResult?> RecognizeText()
        {
            if (paddleOCREngine == null)
            {
                LogMessage("OCR引擎未初始化");
                return null;
            }
            byte[]? bitmap = await CaptureDeviceScreen();
            if (bitmap != null)
            {
                //await File.WriteAllBytesAsync("E:\\AdWPFAndroid\\ConsoleApp1\\screen.png", bitmap);
                var ocrResult = paddleOCREngine.DetectText(bitmap);
                return ocrResult;
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
    }
}