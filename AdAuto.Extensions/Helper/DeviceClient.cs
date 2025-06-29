using AdAuto.Extensions.Common;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Point = System.Drawing.Point;

namespace AdAuto.Extensions.Helper
{
    public class DeviceClientx
    {
        private readonly string deviceSerial;
        private readonly string deviceModel;
        private readonly string deviceProduct;
        private readonly int deviceWidth;
        private readonly int deviceHeight;
        private IntPtr scrcpyWindow;

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rectangle rect);
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        [DllImport("gdi32.dll")]
        private static extern int DeleteDC(IntPtr hdc);
        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, int nFlags);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hwnd);


        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public static IntPtr FindScrcpyWindow(string deviceName)
        {
            IntPtr foundWindow = IntPtr.Zero;
            EnumWindowsProc enumProc = (IntPtr hWnd, IntPtr lParam) =>
            {
                StringBuilder windowTitle = new StringBuilder(256);
                GetWindowText(hWnd, windowTitle, 256);

                if (windowTitle.ToString().Contains(deviceName))
                {
                    foundWindow = hWnd;
                    return false; // 找到窗口，停止枚举
                }
                return true; // 继续枚举
            };

            EnumWindows(enumProc, IntPtr.Zero);
            return foundWindow;
        }


        public void Dispose()
        {
        }

        private void InitializeScrcpyWindow()
        {
            scrcpyWindow = FindWindow(null,deviceProduct);
            if (scrcpyWindow == IntPtr.Zero)
            {
                throw new InvalidOperationException("未找到 Scrcpy 窗口！");
            }
        }

        private Point ConvertToDeviceCoordinates(Point windowPoint)
        {
            var windowRect = new Rectangle();
            GetWindowRect(scrcpyWindow, ref windowRect);
            int windowWidth = Math.Abs(windowRect.Width - windowRect.X);
            int windowHeight = Math.Abs(windowRect.Height - windowRect.Y);

            // 计算缩放比例
            float scaleX = (float)deviceWidth / windowWidth;
            float scaleY = (float)deviceHeight / windowHeight;

            // 转换坐标
            int deviceX = (int)(windowPoint.X * scaleX);
            int deviceY = (int)(windowPoint.Y * scaleY);

            return new Point(deviceX, deviceY);
        }

        public DeviceClientx(DeviceInfo device)
        {
            this.deviceSerial = device.Serial;
            this.deviceModel = device.Model;
            this.deviceProduct = device.Product;
            this.deviceWidth = device.Width;
            this.deviceHeight = device.Height;
            InitializeScrcpyWindow();
        }

        public async Task ClickAsync(Point point)
        {
            var devicePoint = ConvertToDeviceCoordinates(point);
            // 使用adb shell input tap命令执行点击
            await ProcessHelper.ExecuteAdbCommand($"-s {deviceSerial} shell input tap {devicePoint.X} {devicePoint.Y}");
            await Task.Delay(500); // 点击后等待500ms
        }

        public async Task Swipe(int startX, int startY, int endX, int endY, int duration)
        {
            var startPoint = ConvertToDeviceCoordinates(new Point(startX, startY));
            var endPoint = ConvertToDeviceCoordinates(new Point(endX, endY));
            // 使用adb shell input swipe命令执行滑动
            await ProcessHelper.ExecuteAdbCommand($"-s {deviceSerial} shell input swipe {startPoint.X} {startPoint.Y} {endPoint.X} {endPoint.Y} {duration}");
        }

        public async Task ClickBackButtonAsync()
        {
            // 使用adb shell input keyevent命令模拟返回键
            await ProcessHelper.ExecuteAdbCommand($"-s {deviceSerial} shell input keyevent KEYCODE_BACK");
            await Task.Delay(500); // 点击后等待500ms
        }

        public  Bitmap GetShotCutImage()
        {
            IntPtr hWnd = scrcpyWindow;
            var hscrdc = GetWindowDC(hWnd);
            var windowRect = new Rectangle();
            GetWindowRect(hWnd, ref windowRect);
            int width = Math.Abs(windowRect.Width - windowRect.X);
            int height = Math.Abs(windowRect.Height - windowRect.Y);
            var hbitmap = CreateCompatibleBitmap(hscrdc, width, height);
            var hmemdc = CreateCompatibleDC(hscrdc);
            SelectObject(hmemdc, hbitmap);
            PrintWindow(hWnd, hmemdc, 0);
            var bmp = Image.FromHbitmap(hbitmap);
            DeleteDC(hscrdc);
            DeleteDC(hmemdc);
            return bmp;
        }
    }
}