using System.Text;
using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using AdScrcpy.Core;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace AdScrcpy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Process? _scrcpyProcess;
        private IntPtr _scrcpyHandle;
        private string _selectedDeviceId = string.Empty;
        private readonly OcrProcessor _ocrProcessor;
        private readonly System.Timers.Timer _captureTimer;
        private readonly string _screenshotDir;
        private ScreenEmbedder? _screenEmbedder;

        public MainWindow()
        {
            InitializeComponent();
            _ocrProcessor = new OcrProcessor();
            _screenshotDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            Directory.CreateDirectory(_screenshotDir);

            _captureTimer = new()
            {
                Interval = 2000, // 1秒间隔
                AutoReset = true
            };
            _captureTimer.Elapsed += async (s, e) => 
            {
                try
                {
                    await Task.Run(() => CaptureTimer_Tick());
                }
                catch { /* 错误处理 */ }
            };
            // 窗口大小改变时更新
            SizeChanged += (s, e) => UpdateEmbedderSize();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDeviceList();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _captureTimer.Stop();
            _ocrProcessor.Dispose();
            _screenEmbedder?.Dispose();
            if (_scrcpyProcess != null && !_scrcpyProcess.HasExited)
            {
                _scrcpyProcess.Kill();
            }
        }

        private void RefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            RefreshDeviceList();
        }

        private void RefreshDeviceList()
        {
            var devices = DeviceManager.GetConnectedDevices();
            DeviceList.ItemsSource = devices;
        }

        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceList.SelectedItem is string deviceId)
            {
                _selectedDeviceId = deviceId;
                StartScrcpy();
            }
        }

        private void StartScrcpy()
        {
            if (_scrcpyProcess != null && !_scrcpyProcess.HasExited)
            {
                _scrcpyProcess.Kill();
                _captureTimer.Stop();
            }

            string deviceName = DeviceManager.GetDeviceName(_selectedDeviceId);
            string windowTitle = $"Scrcpy - {deviceName}";
            _scrcpyProcess = DeviceManager.StartScrcpy(_selectedDeviceId, windowTitle);

            // 2. 等待窗口创建并获取句柄
            System.Threading.Thread.Sleep(4000); // 等待窗口创建

            var scrcpyHwnd = FindWindowByProcessId(_scrcpyProcess.Id);
            if (scrcpyHwnd == IntPtr.Zero)
            {
                System.Windows.MessageBox.Show("找不到Scrcpy窗口");
                return;
            }

            _screenEmbedder?.Dispose();
            _screenEmbedder = new ScreenEmbedder(scrcpyHwnd)
            {
                //MaintainAspectRatio = true
            };
            ScrcpyContainer.Children.Add(_screenEmbedder);

            // 更新窗口大小
            //_screenEmbedder.UpdatePosition(new Rect(0, 0, ScrcpyContainer.ActualWidth, ScrcpyContainer.ActualHeight));

            _captureTimer.Start();

            // 初始设置大小
            UpdateEmbedderSize();

            // 窗口大小改变时更新
            SizeChanged += (s, e) => UpdateEmbedderSize();

            // 监听大小变化事件
            ScrcpyContainer.SizeChanged += HostGrid_SizeChanged;

        }
        private DateTime _lastSizeChangeTime = DateTime.MinValue;

        private void HostGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if ((DateTime.Now - _lastSizeChangeTime).TotalMilliseconds < 100)
                return;

            _lastSizeChangeTime = DateTime.Now;
            UpdateEmbedderSize();
        }
        private void CaptureTimer_Tick()
        {
            if (_screenEmbedder?.Handle != IntPtr.Zero)
            {
                try
                {
                    // 1. 捕获当前窗口图像
                    using (var bitmap = _screenEmbedder?.CaptureWindow())
                    {
                        if (bitmap == null) return;

                        // 2. 生成文件名
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string screenshotPath = System.IO.Path.Combine(_screenshotDir, $"screenshot_{timestamp}.png");

                        // 3. 保存图像
                       bitmap.Save(screenshotPath, ImageFormat.Png);

                        Debug.WriteLine($"已保存截图: {screenshotPath}");

                        var result = _ocrProcessor.ProcessImage(bitmap);
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]识别结果：");
                        if (result.TextBlocks != null)
                        {
                            foreach (var block in result.TextBlocks)
                            {
                                sb.AppendLine(block.Text);
                            }
                        }
                        sb.AppendLine();
                        OcrResult.Dispatcher.Invoke(() =>
                        {
                            OcrResult.Text = sb.ToString();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"截图失败: {ex.Message}");
                }
                //using var bitmap = WindowManager.CaptureWindow(_scrcpyHandle);
                //var result = _ocrProcessor.ProcessImage(bitmap);

                //string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                //string screenshotPath = System.IO.Path.Combine(_screenshotDir, $"screenshot_{timestamp}.png");
                //WindowManager.SaveBitmap(bitmap, screenshotPath);

                //StringBuilder sb = new StringBuilder();
                //sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]识别结果：");
                //if (result.TextBlocks != null)
                //{
                //    foreach (var block in result.TextBlocks)
                //    {
                //        sb.AppendLine(block.Text);
                //    }
                //}
                //sb.AppendLine();

                //OcrResult.Text = sb.ToString() + OcrResult.Text;
            }
        }
        private void UpdateEmbedderSize()
        {
            if (_screenEmbedder == null) return;

            // 获取宿主容器的实际可用大小
            var hostWidth = (int)ScrcpyContainer.ActualWidth;
            var hostHeight = (int)ScrcpyContainer.ActualHeight;

            // 更新嵌入窗口大小
            _screenEmbedder.UpdateSize(hostWidth, hostHeight);
        }

        private IntPtr FindWindowByProcessId(int processId)
        {
            IntPtr foundHwnd = IntPtr.Zero;
            EnumWindows((hwnd, lParam) =>
            {
                GetWindowThreadProcessId(hwnd, out int windowProcessId);
                if (windowProcessId == processId)
                {
                    var className = new System.Text.StringBuilder(256);
                    GetClassName(hwnd, className, className.Capacity);

                    if (className.ToString().Contains("SDL"))
                    {
                        foundHwnd = hwnd;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);

            return foundHwnd;
        }
        #region Win32 API

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        #endregion
    }
}