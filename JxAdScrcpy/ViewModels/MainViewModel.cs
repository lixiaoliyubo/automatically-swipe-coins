using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JxAdScrcpy.Services;
using Microsoft.Win32;

namespace JxAdScrcpy.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly WebSocketService _webSocketService;

        [ObservableProperty]
        private ObservableCollection<DeviceInfo> _devices = new();

        [ObservableProperty]
        private DeviceInfo? _selectedDevice;

        [ObservableProperty]
        private bool _isConnected;

        [ObservableProperty]
        private string _status = "未连接";

        [ObservableProperty]
        private string _clipboardContent = string.Empty;

        [ObservableProperty]
        private string _adbCommandResult = string.Empty;

        [ObservableProperty]
        private string _adbCommand = string.Empty;

        [ObservableProperty]
        private string _clipboardText = string.Empty;

        public MainViewModel()
        {
            _webSocketService = new WebSocketService();
            _webSocketService.DevicesUpdated += OnDevicesUpdated;
            _webSocketService.CommandResponse += OnCommandResponse;
            _webSocketService.ClipboardContent += OnClipboardContent;
            _webSocketService.AdbCommandResult += OnAdbCommandResult;
        }

        [RelayCommand]
        private void Connect()
        {
            if (!IsConnected)
            {
                if (_webSocketService.Connect())
                {
                    IsConnected = true;
                    Status = "已连接";
                }
                else
                {
                    Status = "连接失败";
                }
            }
            else
            {
                _webSocketService.Disconnect();
                IsConnected = false;
                Status = "未连接";
                Devices.Clear();
            }
        }

        [RelayCommand]
        private void RefreshDevices()
        {
            if (IsConnected)
            {
                _webSocketService.GetDeviceList();
            }
        }

        [RelayCommand]
        private void TakeScreenshot()
        {
            if (IsConnected && SelectedDevice != null)
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PNG图片|*.png",
                    Title = "选择截图保存位置",
                    FileName = $"{SelectedDevice.DeviceId}.png"
                };

                if (dialog.ShowDialog() == true)
                {
                    var path = System.IO.Path.GetDirectoryName(dialog.FileName);
                    if (path != null)
                    {
                        _webSocketService.TakeScreenshot(SelectedDevice.DeviceId, path, true);
                        Status = $"正在截图: {SelectedDevice.Name}";
                    }
                }
            }
        }

        [RelayCommand]
        private void TakeAllScreenshots()
        {
            if (IsConnected && Devices.Count > 0)
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "选择截图保存文件夹"
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _webSocketService.TakeScreenshot("all", dialog.SelectedPath, true);
                    Status = "正在截图所有设备";
                }
            }
        }

        [RelayCommand]
        private void GetClipboard()
        {
            if (IsConnected && SelectedDevice != null)
            {
                _webSocketService.GetClipboard(SelectedDevice.DeviceId);
                Status = $"正在获取剪贴板: {SelectedDevice.Name}";
            }
        }

        [RelayCommand]
        private void WriteClipboard()
        {
            if (IsConnected && SelectedDevice != null && !string.IsNullOrEmpty(ClipboardText))
            {
                _webSocketService.WriteClipboard(SelectedDevice.DeviceId, ClipboardText);
                Status = $"正在写入剪贴板: {SelectedDevice.Name}";
            }
        }

        [RelayCommand]
        private void ExecuteAdbCommand()
        {
            if (IsConnected && SelectedDevice != null && !string.IsNullOrEmpty(AdbCommand))
            {
                _webSocketService.ExecuteAdbCommand(SelectedDevice.DeviceId, AdbCommand);
                Status = $"正在执行ADB命令: {SelectedDevice.Name}";
            }
        }

        [RelayCommand]
        private void ShowSingleWindow()
        {
            if (IsConnected && SelectedDevice != null)
            {
                _webSocketService.ShowSingleWindow(SelectedDevice.DeviceId);
                Status = $"正在打开独立窗口: {SelectedDevice.Name}";
            }
        }

        [RelayCommand]
        private void TapScreen()
        {
            if (IsConnected && SelectedDevice != null)
            {
                // 点击屏幕中心
                _webSocketService.TapScreen(SelectedDevice.DeviceId, 0.5, 0.5);
                Status = $"点击屏幕: {SelectedDevice.Name}";
            }
        }

        [RelayCommand]
        private void SwipeUp()
        {
            if (IsConnected && SelectedDevice != null)
            {
                // 从屏幕中下方向上滑动
                _webSocketService.SwipeScreen(SelectedDevice.DeviceId, 0.5, 0.7, 0.5, 0.3);
                Status = $"向上滑动: {SelectedDevice.Name}";
            }
        }

        [RelayCommand]
        private void SwipeDown()
        {
            if (IsConnected && SelectedDevice != null)
            {
                // 从屏幕中上方向下滑动
                _webSocketService.SwipeScreen(SelectedDevice.DeviceId, 0.5, 0.3, 0.5, 0.7);
                Status = $"向下滑动: {SelectedDevice.Name}";
            }
        }

        [RelayCommand]
        private void SwipeLeft()
        {
            if (IsConnected && SelectedDevice != null)
            {
                // 从屏幕右侧向左滑动
                _webSocketService.SwipeScreen(SelectedDevice.DeviceId, 0.7, 0.5, 0.3, 0.5);
                Status = $"向左滑动: {SelectedDevice.Name}";
            }
        }

        [RelayCommand]
        private void SwipeRight()
        {
            if (IsConnected && SelectedDevice != null)
            {
                // 从屏幕左侧向右滑动
                _webSocketService.SwipeScreen(SelectedDevice.DeviceId, 0.3, 0.5, 0.7, 0.5);
                Status = $"向右滑动: {SelectedDevice.Name}";
            }
        }

        [RelayCommand]
        private void BackButton()
        {
            if (IsConnected && SelectedDevice != null)
            {
                // 发送返回键事件
                _webSocketService.SendPointerEvent(SelectedDevice.DeviceId, 3, 0, 0);
                Status = $"返回键: {SelectedDevice.Name}";
            }
        }

        private void OnDevicesUpdated(object? sender, List<DeviceInfo> devices)
        {
            // 在UI线程上更新设备列表
            App.Current.Dispatcher.Invoke(() =>
            {
                Devices.Clear();
                foreach (var device in devices)
                {
                    Devices.Add(device);
                }

                if (Devices.Count > 0 && SelectedDevice == null)
                {
                    SelectedDevice = Devices[0];
                }

                Status = $"已连接 {Devices.Count} 台设备";
            });
        }

        private void OnCommandResponse(object? sender, string response)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Status = response;
            });
        }

        private void OnClipboardContent(object? sender, string content)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ClipboardContent = content;
                Status = "已获取剪贴板内容";
            });
        }

        private void OnAdbCommandResult(object? sender, Dictionary<string, List<string>> result)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var sb = new System.Text.StringBuilder();
                foreach (var device in result)
                {
                    sb.AppendLine($"设备: {device.Key}");
                    foreach (var line in device.Value)
                    {
                        sb.AppendLine($"  {line}");
                    }
                }
                AdbCommandResult = sb.ToString();
                Status = "ADB命令执行完成";
            });
        }

        public void Dispose()
        {
            if (IsConnected)
            {
                _webSocketService.Disconnect();
            }
            (_webSocketService as IDisposable)?.Dispose();
        }
    }
}