using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using static AdScrcpy.Core.WindowManager;

namespace AdScrcpy.Core
{

    public class ScreenEmbedder : HwndHost
    {
        private IntPtr _childHwnd;
        private readonly IntPtr _scrcpyHwnd;
        private const int GWL_STYLE = -16;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CHILD = 0x40000000;

        public ScreenEmbedder(IntPtr scrcpyHwnd)
        {
            _scrcpyHwnd = scrcpyHwnd;
        }

        public Bitmap CaptureWindow()
        {
            if (_childHwnd == IntPtr.Zero) return null;

            GetWindowRect(_childHwnd, out RECT rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                PrintWindow(_childHwnd, hdc, 0);
                g.ReleaseHdc(hdc);
            }

            return bmp;
        }
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            // 设置窗口为子窗口
            SetWindowLong(_scrcpyHwnd, GWL_STYLE, WS_VISIBLE | WS_CHILD);

            // 设置父窗口
            SetParent(_scrcpyHwnd, hwndParent.Handle);

            _childHwnd = _scrcpyHwnd;
            return new HandleRef(this, _childHwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            // 清理资源
            if (_childHwnd != IntPtr.Zero)
            {
                SetParent(_childHwnd, IntPtr.Zero);
                _childHwnd = IntPtr.Zero;
            }
        }

        public void UpdateSize(int width, int height)
        {
            if (_childHwnd != IntPtr.Zero)
            {
                MoveWindow(_childHwnd, 0, 0, width, height, true);
            }
        }

        #region Win32 API

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        #endregion
    }
}