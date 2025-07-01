using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.ViewModels;

/// <summary>
/// 主窗口视图模型
/// </summary>
public partial class MainViewModel : ObservableObject
{
    #region 字段

    private readonly IDeviceService _deviceService;
    private readonly ILogService _logService;
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _dailyCleanupTimer;
    private readonly DispatcherTimer _nightlyDeviceCheckTimer;
    private readonly IWebSocketService _webSocketService;
    private CancellationTokenSource? _nightlyDeviceCheckCts;

    #endregion

    #region 属性

    /// <summary>
    /// 设备列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DeviceModel> _devices = new();

    /// <summary>
    /// 选中的设备
    /// </summary>
    [ObservableProperty]
    private DeviceModel? _selectedDevice;

    /// <summary>
    /// 日志列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<LogEntry> _logs;

    /// <summary>
    /// 是否显示所有日志
    /// </summary>
    [ObservableProperty]
    private bool _showAllLogs = true;

    /// <summary>
    /// 日志文本，用于在TextBox中显示
    /// </summary>
    [ObservableProperty]
    private string _logsText;

    /// <summary>
    /// WebSocket日志文本，用于在TextBox中显示
    /// </summary>
    [ObservableProperty]
    private string _webSocketLogsText;

    /// <summary>
    /// WebSocket连接状态
    /// </summary>
    [ObservableProperty]
    private bool _isWebSocketConnected;

    /// <summary>
    /// WebSocket设备列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DeviceInfo> _webSocketDevices = new();

    /// <summary>
    /// 选中的WebSocket设备
    /// </summary>
    [ObservableProperty]
    private DeviceInfo? _selectedWebSocketDevice;

    /// <summary>
    /// 截图保存路径
    /// </summary>
    [ObservableProperty]
    private string _screenshotSavePath;

    public Color _systemColor;
    public Color SystemColor
    {
        get => _systemColor;
        set
        {

            ApplicationAccentColorManager.Apply(value);
            SetProperty(ref _systemColor, value);
        }
    }

    [ObservableProperty]
    private bool _isFlyoutOpen;

    /// <summary>
    /// 是否启用夜间设备检测（0点到8点）
    /// </summary>
    [ObservableProperty]
    private bool _enableNightlyDeviceCheck = true;


    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="deviceService">设备服务</param>
    /// <param name="logService">日志服务</param>
    /// <param name="webSocketService">WebSocket服务</param>
    public MainViewModel(IDeviceService deviceService, ILogService logService, IWebSocketService webSocketService)
    {
        _deviceService = deviceService;
        _logService = logService;
        _webSocketService = webSocketService;
        _logs = _logService.LogEntries;
        _logsText = _logService.LogsText;
        _webSocketLogsText = _logService.WebSocketLogsText;
        _screenshotSavePath = Path.Combine(Path.GetTempPath(), "Screenshots");
        Directory.CreateDirectory(_screenshotSavePath);
        SystemColor = ApplicationAccentColorManager.GetSystemAccent();

        // 订阅日志服务的属性变更事件
        ((INotifyPropertyChanged)_logService).PropertyChanged += LogService_PropertyChanged;


        // 注册WebSocket事件
        RegisterWebSocketEvents();

        // 设置设备刷新定时器 - 使用自适应间隔
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _refreshTimer.Tick += RefreshDevices_Tick;
        _refreshTimer.Start();

        // 设置每日清理定时器 - 优化检查间隔
        // 计算到下一个整点的时间
        var now = DateTime.Now;
        var nextHour = now.Date.AddHours(now.Hour + 1);
        var initialDelay = nextHour - now;

        _dailyCleanupTimer = new DispatcherTimer
        {
            // 每小时检查一次，而不是每分钟
            Interval = TimeSpan.FromHours(1)
        };
        _dailyCleanupTimer.Tick += DailyCleanup_Tick;

        // 使用单次触发的定时器来对齐到整点
        var alignmentTimer = new DispatcherTimer
        {
            Interval = initialDelay
        };
        alignmentTimer.Tick += (s, e) =>
        {
            alignmentTimer.Stop();
            _dailyCleanupTimer.Start();
            // 立即执行一次检查
            DailyCleanup_Tick(null, EventArgs.Empty);
        };
        alignmentTimer.Start();

        // 初始加载设备
        RefreshDevicesCommand.Execute(null);
        
        // 初始化夜间设备检测定时器
        _nightlyDeviceCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _nightlyDeviceCheckTimer.Tick += NightlyDeviceCheck_Tick;
        
        // 启动夜间设备检测
        StartNightlyDeviceCheck();
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 刷新设备列表定时器事件
    /// </summary>
    private void RefreshDevices_Tick(object? sender, EventArgs e)
    {
        RefreshDevicesCommand.Execute(null);
    }

    /// <summary>
    /// 每日清理定时器事件
    /// </summary>
    private void DailyCleanup_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        // 每天凌晨0点执行清理
        if (now.Hour == 0)
        {
            _logService.LogInfo("执行每日清理任务", "System");
            CleanupAllCommand.Execute(null);
            
            // 0点时启动夜间设备检测
            if (EnableNightlyDeviceCheck && !_nightlyDeviceCheckTimer.IsEnabled)
            {
                StartNightlyDeviceCheck();
            }
        }
    }
    
    /// <summary>
    /// 夜间设备检测定时器事件
    /// </summary>
    private void NightlyDeviceCheck_Tick(object? sender, EventArgs e)
    {
        // 检查是否在0点到8点之间
        var now = DateTime.Now;
        if (now.Hour >= 0 && now.Hour < 8)
        {
            CheckAndStartDevices();
        }
        else
        {
            // 不在指定时间范围内，停止定时器
            _nightlyDeviceCheckTimer.Stop();
            _logService.LogInfo("已退出夜间设备检测时间段，停止检测", "System");
        }
    }
    
    /// <summary>
    /// 检查并启动未运行的设备
    /// </summary>
    private async void CheckAndStartDevices()
    {
        if (!EnableNightlyDeviceCheck || Devices.Count == 0)
        {
            return;
        }
        
        // 检查是否有设备未运行
        var nonRunningDevices = Devices.Where(d => !d.IsRunning).ToList();
        if (nonRunningDevices.Count > 0)
        {
            string message = $"夜间检测到{nonRunningDevices.Count}个设备未运行，等待启动...";
            _logService.LogWarning(message, "System");
            
            // 播放文本提示
            await _deviceService.PlayTextAsync(message);
            
            // 等待语音播放完成
            await Task.Delay(5000);
            
            //foreach (var device in nonRunningDevices)
            //{
            //    _logService.LogInfo($"启动设备: {device.Name} ({device.Id})", "System");
            //    StartDeviceCheck(device);
            //}

        }
    }
    
    /// <summary>
    /// 启动夜间设备检测
    /// </summary>
    private void StartNightlyDeviceCheck()
    {
        if (!EnableNightlyDeviceCheck)
        {
            return;
        }
        
        var now = DateTime.Now;
        // 只在0点到8点之间启动定时器
        if (now.Hour >= 0 && now.Hour < 8)
        {
            if (!_nightlyDeviceCheckTimer.IsEnabled)
            {
                _nightlyDeviceCheckTimer.Start();
                _logService.LogInfo("启动夜间设备检测（0点到8点，每10秒检测一次）", "System");
                
                // 立即执行一次检查
                CheckAndStartDevices();
            }
        }
        else
        {
            _logService.LogInfo("当前不在夜间设备检测时间段（0点到8点），将在下次进入该时间段时自动启动", "System");
        }
    }
    
    /// <summary>
    /// 停止夜间设备检测
    /// </summary>
    private void StopNightlyDeviceCheck()
    {
        if (_nightlyDeviceCheckTimer.IsEnabled)
        {
            _nightlyDeviceCheckTimer.Stop();
            _logService.LogInfo("停止夜间设备检测", "System");
        }
    }

    /// <summary>
    /// 选中设备变更处理
    /// </summary>
    partial void OnSelectedDeviceChanged(DeviceModel? value)
    {
        if (value != null && !ShowAllLogs)
        {
            Logs = _logService.GetLogs(value.Id);
        }
    }
    
    /// <summary>
    /// 夜间设备检测启用状态变更处理
    /// </summary>
    partial void OnEnableNightlyDeviceCheckChanged(bool value)
    {
        if (value)
        {
            StartNightlyDeviceCheck();
        }
        else
        {
            StopNightlyDeviceCheck();
        }
    }

    /// <summary>
    /// 日志服务属性变更事件处理
    /// </summary>
    private void LogService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILogService.LogsText))
        {
            if (ShowAllLogs)
            {
                LogsText = _logService.LogsText;
            }
            else
            {
                Logs = _logService.GetLogs(SelectedDevice.Id);
                LogsText = _logService.GenerateLogText(Logs);
            }
        }
        else if (e.PropertyName == nameof(ILogService.WebSocketLogsText))
        {
            WebSocketLogsText = _logService.WebSocketLogsText;
        }
    }

    /// <summary>
    /// 注册WebSocket事件
    /// </summary>
    private void RegisterWebSocketEvents()
    {
        // 先取消可能存在的旧订阅，防止重复订阅
        _webSocketService.ConnectionStatusChanged -= WebSocketConnectionStatusChanged;
        _webSocketService.MessageReceived -= WebSocketMessageReceived;

        // 连接状态变更事件
        _webSocketService.ConnectionStatusChanged += WebSocketConnectionStatusChanged;
        _webSocketService.MessageReceived += WebSocketMessageReceived;
    }


    private void WebSocketConnectionStatusChanged(object? sender, bool isConnected)
    {
        IsWebSocketConnected = isConnected;
        _logService.LogInfo($"WebSocket连接状态: {(isConnected ? "已连接" : "已断开")}", string.Empty, true);
    }
    private void WebSocketMessageReceived(object? sender, WebSocketResponse response)
    {
        if (response.Code == 200)
        {
            try
            {
                if (response.result.Contains("\"deviceId\""))
                {
                    var devices = JsonSerializer.Deserialize<List<DeviceInfo>>(response.result);
                    if (devices == null)
                    {
                        _logService.LogWarning("WebSocket设备列表解析失败", string.Empty, true);
                        return;
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        // 高效更新集合，避免不必要的UI刷新
                        UpdateWebSocketDevices(devices);
                        _logService.LogInfo($"收到WebSocket设备列表，共{devices.Count}个设备", string.Empty);
                    });
                }
            }
            catch (JsonException ex)
            {
                _logService.LogError($"解析WebSocket消息失败: {ex.Message}", string.Empty, true);
            }
        }
    }

    /// <summary>
    /// 高效更新WebSocket设备列表
    /// </summary>
    /// <param name="newDevices">新的设备列表</param>
    private void UpdateWebSocketDevices(List<DeviceInfo> newDevices)
    {
        // 创建查找字典以便快速查找
        var existingDevices = WebSocketDevices.ToDictionary(d => d.Id);
        var newDeviceDict = newDevices.ToDictionary(d => d.Id);

        // 移除不再存在的设备
        for (int i = WebSocketDevices.Count - 1; i >= 0; i--)
        {
            var device = WebSocketDevices[i];
            if (!newDeviceDict.ContainsKey(device.Id))
            {
                WebSocketDevices.RemoveAt(i);
            }
        }

        // 更新现有设备并添加新设备
        foreach (var device in newDevices)
        {
            if (existingDevices.TryGetValue(device.Id, out var existingDevice))
            {
                // 更新现有设备的属性
                int index = WebSocketDevices.IndexOf(existingDevice);
                if (index >= 0)
                {
                    WebSocketDevices[index] = device;
                }
            }
            else
            {
                // 添加新设备
                WebSocketDevices.Add(device);
            }
        }
    }
    #endregion

    #region UI命令

    [RelayCommand]
    private void OnColorPickerButton()
    {
        IsFlyoutOpen = true;
    }

    [RelayCommand]
    private void OnToggleThemeButton()
    {
        ApplicationTheme currentTheme = ApplicationThemeManager.GetAppTheme();

        ApplicationThemeManager.Apply(
            currentTheme == ApplicationTheme.Light ? ApplicationTheme.Dark : ApplicationTheme.Light
        );
    }

    [RelayCommand]
    private void OnRestartBtn()
    {
        //_appContext.Application.Restart();
    }

    #endregion

    #region 设备管理命令

    /// <summary>
    /// 刷新设备列表命令
    /// </summary>
    [RelayCommand]
    private void RefreshDevices()
    {
        var currentDevices = _deviceService.GetDevices();

        // 更新现有设备状态
        foreach (var device in currentDevices)
        {
            DeviceModel? existingDevice = Devices.FirstOrDefault(d => d.Id == device.Id);
            if (existingDevice != null)
            {
                existingDevice.Status = device.Status;
                existingDevice.Name = device.Name;
            }
            else
            {
                Devices.Add(device);
            }
        }

        // 移除已断开的设备
        var disconnectedDevices = Devices.Where(d => !currentDevices.Any(cd => cd.Id == d.Id)).ToList();
        foreach (var device in disconnectedDevices)
        {
            _deviceService.StopDeviceCheck(device);
            Devices.Remove(device);
        }
    }

    /// <summary>
    /// 启动设备检查命令
    /// </summary>
    [RelayCommand]
    private void StartDeviceCheck(DeviceModel device)
    {
        if (device != null)
        {
            _deviceService.StartDeviceCheck(device, ScreenshotSavePath);
        }
    }

    /// <summary>
    /// 停止设备检查命令
    /// </summary>
    [RelayCommand]
    private void StopDeviceCheck(DeviceModel device)
    {
        if (device != null)
        {
            _deviceService.StopDeviceCheck(device);
        }
    }

    /// <summary>
    /// 清理指定设备上的所有应用命令
    /// </summary>
    [RelayCommand]
    private async Task CleanDeviceApps(DeviceModel device)
    {
        if (device != null)
        {
            // 如果设备正在运行检查，先停止检查
            bool wasRunning = device.IsRunning;
            if (wasRunning)
            {
                _deviceService.StopDeviceCheck(device);
            }

            // 清理设备上的所有应用
            await Task.Run(() => _deviceService.CleanDeviceRunningApps(device.Id));

            // 如果之前在运行检查，重新启动检查
            if (wasRunning)
            {
                _deviceService.StartDeviceCheck(device, ScreenshotSavePath);
            }
        }
    }

    /// <summary>
    /// 清理所有命令
    /// </summary>
    [RelayCommand]
    private async Task CleanupAll()
    {
        // 停止所有设备检查
        foreach (var device in Devices)
        {
            _deviceService.StopDeviceCheck(device);
        }

        // 清理所有运行中的应用
        await Task.Run(() => _deviceService.CleanAllRunningApps());

        // 清理日志
        _logService.ClearLogs();

        // 重新启动设备检查
        foreach (var device in Devices.Where(d => d.IsRunning))
        {
            _deviceService.StartDeviceCheck(device, ScreenshotSavePath);
        }
    }

    #endregion

    #region 日志管理命令

    /// <summary>
    /// 查看设备日志命令
    /// </summary>
    [RelayCommand]
    private void ViewDeviceLogs(DeviceModel device)
    {
        if (device != null)
        {
            ShowAllLogs = false;
            Logs = _logService.GetLogs(device.Id);
            LogsText = _logService.GenerateLogText(Logs);
        }
    }

    /// <summary>
    /// 查看所有日志命令
    /// </summary>
    [RelayCommand]
    private void ViewAllLogs()
    {
        ShowAllLogs = true;
        Logs = _logService.LogEntries;
    }

    #endregion

    #region 日志滚动命令

    /// <summary>
    /// 滚动到日志末尾命令
    /// </summary>
    [RelayCommand]
    private void ScrollToEnd(object dataGrid)
    {
        if (dataGrid is Yuandl.ThemeUI.Controls.DataGrid grid && grid.Items.Count > 0)
        {
            grid.ScrollIntoView(grid.Items[grid.Items.Count - 1]);
        }
        else if (dataGrid is System.Windows.Controls.DataGrid standardGrid && standardGrid.Items.Count > 0)
        {
            standardGrid.ScrollIntoView(standardGrid.Items[standardGrid.Items.Count - 1]);
        }
    }

    #endregion

    #region WebSocket命令

    /// <summary>
    /// 连接WebSocket服务器命令
    /// </summary>
    [RelayCommand]
    private async Task ConnectWebSocket()
    {
        var result = await _webSocketService.ConnectAsync();
        if (result)
        {
            _logService.LogInfo("WebSocket连接成功", string.Empty, true);
            // 连接成功后获取设备列表
            GetWebSocketDevicesCommand.Execute(null);
        }
        else
        {
            _logService.LogError("WebSocket连接失败", string.Empty, true);
        }
    }

    /// <summary>
    /// 断开WebSocket连接命令
    /// </summary>
    [RelayCommand]
    private void DisconnectWebSocket()
    {
        _webSocketService.Disconnect();
        _logService.LogInfo("WebSocket已断开连接", string.Empty, true);
    }

    /// <summary>
    /// 获取WebSocket设备列表命令
    /// </summary>
    [RelayCommand]
    private void GetWebSocketDevices()
    {
        if (!IsWebSocketConnected)
        {
            _logService.LogWarning("WebSocket未连接，无法获取设备列表", string.Empty, true);
            return;
        }

        _webSocketService.GetDeviceList();
        _logService.LogInfo("已发送获取设备列表请求", string.Empty, true);
    }

    /// <summary>
    /// 截图命令
    /// </summary>
    [RelayCommand]
    private void TakeScreenshot()
    {
        if (!IsWebSocketConnected)
        {
            _logService.LogWarning("WebSocket未连接，无法截图", string.Empty, true);
            return;
        }


        // 确保截图保存目录存在
        if (!Directory.Exists(ScreenshotSavePath))
        {
            Directory.CreateDirectory(ScreenshotSavePath);
        }

        // 发送截图请求
        _webSocketService.TakeScreenshot(SelectedWebSocketDevice?.Id ?? "all", ScreenshotSavePath, true);
        _logService.LogInfo($"已发送截图请求，设备: {SelectedWebSocketDevice?.Name ?? "所有设备"}", string.Empty, true);
    }

    /// <summary>
    /// 发送点击事件命令
    /// </summary>
    [RelayCommand]
    private void SendTapEvent(string coordinates)
    {
        if (!IsWebSocketConnected || SelectedWebSocketDevice == null)
        {
            _logService.LogWarning("WebSocket未连接或未选择设备", string.Empty, true);
            return;
        }

        try
        {
            // 解析坐标，格式为"x,y"，例如"0.5,0.5"
            var parts = coordinates.Split(',');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out double x) &&
                double.TryParse(parts[1], out double y))
            {
                // 发送点击事件（按下+松开）
                _webSocketService.SendTapEvent(SelectedWebSocketDevice.Id, x, y);
                _logService.LogInfo($"已发送点击事件，坐标: ({x}, {y})", string.Empty, true);
            }
            else
            {
                _logService.LogWarning("坐标格式错误，应为\"x,y\"，例如\"0.5,0.5\"", string.Empty, true);
            }
        }
        catch (Exception ex)
        {
            _logService.LogError($"发送点击事件失败: {ex.Message}", string.Empty, true);
        }
    }

    /// <summary>
    /// 发送指针事件命令
    /// </summary>
    [RelayCommand]
    private void SendPointerEvent(string parameters)
    {
        if (!IsWebSocketConnected || SelectedWebSocketDevice == null)
        {
            _logService.LogWarning("WebSocket未连接或未选择设备", string.Empty, true);
            return;
        }

        try
        {
            // 解析参数，格式为"eventType,x,y[,endX,endY]"，例如"0,0.5,0.5"或"6,0.5,0.5,0.5,0.8"
            var parts = parameters.Split(',');

            // 基本参数：事件类型、x、y
            if (parts.Length >= 3 &&
                Enum.TryParse<PointerEventType>(parts[0], out PointerEventType eventType) &&
                double.TryParse(parts[1], out double x) &&
                double.TryParse(parts[2], out double y))
            {
                // 滑动事件需要额外的endX和endY参数
                if ((eventType == PointerEventType.SwipeUp ||
                     eventType == PointerEventType.SwipeDown ||
                     eventType == PointerEventType.SwipeLeft ||
                     eventType == PointerEventType.SwipeRight) &&
                    parts.Length >= 5)
                {
                    if (double.TryParse(parts[3], out double endX) &&
                        double.TryParse(parts[4], out double endY))
                    {
                        // 发送滑动事件
                        _webSocketService.SendSwipeEvent(SelectedWebSocketDevice.Id, eventType, x, y, endX, endY);
                        _logService.LogInfo($"已发送滑动事件，类型: {eventType}，起始坐标: ({x}, {y})，结束坐标: ({endX}, {endY})", string.Empty, true);
                        return;
                    }
                }

                // 发送普通指针事件
                _webSocketService.SendPointerEvent(SelectedWebSocketDevice.Id, eventType, x, y);
                _logService.LogInfo($"已发送指针事件，类型: {eventType}，坐标: ({x}, {y})", string.Empty);
            }
            else
            {
                _logService.LogWarning("参数格式错误，应为\"eventType,x,y[,endX,endY]\"，例如\"0,0.5,0.5\"或\"6,0.5,0.5,0.5,0.8\"", string.Empty, true);
            }
        }
        catch (Exception ex)
        {
            _logService.LogError($"发送指针事件失败: {ex.Message}", string.Empty);
        }
    }

    #endregion

    #region 资源清理

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        // 停止所有定时器
        if (_refreshTimer != null && _refreshTimer.IsEnabled)
        {
            _refreshTimer.Stop();
        }
        
        if (_dailyCleanupTimer != null && _dailyCleanupTimer.IsEnabled)
        {
            _dailyCleanupTimer.Stop();
        }
        
        // 停止夜间设备检测
        StopNightlyDeviceCheck();
        
        // 取消任何正在进行的操作
        if (_nightlyDeviceCheckCts != null)
        {
            _nightlyDeviceCheckCts.Cancel();
            _nightlyDeviceCheckCts.Dispose();
            _nightlyDeviceCheckCts = null;
        }
        
        // 断开WebSocket连接
        if (IsWebSocketConnected)
        {
            _webSocketService.Disconnect();
        }
        
        _logService.LogInfo("应用资源已清理", "System");
    }

    #endregion
}