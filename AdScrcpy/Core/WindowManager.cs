using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AdScrcpy.Core
{
    public class WindowManager
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern bool SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_NOOWNERZORDER = 0x0200;
        public const int GWL_STYLE = -16;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_MAXIMIZEBOX = 0x00010000;
        public const int WS_SYSMENU = 0x00080000;
        public const int WS_CHILD = 0x40000000;

        public static IntPtr GetScrcpyWindow(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("Window title cannot be null or empty.", nameof(title));
            }

            IntPtr handle = FindWindow(null, title);
            if (handle == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            // 验证窗口句柄的有效性
            RECT rect;
            if (!GetWindowRect(handle, out rect))
            {
                return IntPtr.Zero;
            }

            return handle;
        }

        public static void EmbedWindow(IntPtr scrcpyHandle, IntPtr parentHandle, int x, int y, int width, int height)
        {
            // 移除窗口边框和标题栏
            int style = GetWindowLong(scrcpyHandle, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
            style |= WS_CHILD;
            SetWindowLong(scrcpyHandle, GWL_STYLE, style);

            // 设置父窗口
            SetParent(scrcpyHandle, parentHandle);

            // 调整窗口位置和大小
            SetWindowPos(scrcpyHandle, IntPtr.Zero, x, y, width, height, 
                SWP_SHOWWINDOW | SWP_FRAMECHANGED | SWP_NOOWNERZORDER);
        }

        public static Bitmap CaptureWindow(IntPtr handle)
        {
            GetWindowRect(handle, out RECT rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                PrintWindow(handle, hdc, 0);
                graphics.ReleaseHdc(hdc);
            }

            return bitmap;
        }

        public static void SaveBitmap(Bitmap bitmap, string filePath)
        {
            bitmap.Save(filePath, ImageFormat.Png);
        }
    }
}