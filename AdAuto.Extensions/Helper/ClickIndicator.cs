using AdAuto.Extensions.Common;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Point = System.Drawing.Point;

namespace AdAuto.Extensions.Helper
{
    public class ClickIndicator : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
            ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pptSrc,
            int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
            int x, int y, int nWidth, int nHeight, IntPtr hWndParent,
            IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_POPUP = unchecked((int)0x80000000);

        private IntPtr _hwnd;
        private readonly int _size = 20;
        private bool _disposed;

        public ClickIndicator()
        {
            CreateOverlayWindow();
        }

        private void CreateOverlayWindow()
        {
            _hwnd = CreateWindowEx(
                WS_EX_LAYERED | WS_EX_TRANSPARENT,
                "static",
                null,
                WS_VISIBLE | WS_POPUP,
                0, 0, _size, _size,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        public void Show(Point location)
        {
            using (var bitmap = new Bitmap(_size, _size))
            using (var screenDc = Graphics.FromHwnd(IntPtr.Zero))
            using (var memDc = Graphics.FromImage(bitmap))
            {
                IntPtr hScreenDc = IntPtr.Zero;
                IntPtr hMemDc = IntPtr.Zero;

                try
                {
                    hScreenDc = screenDc.GetHdc();
                    hMemDc = memDc.GetHdc();

                    memDc.Clear(Color.Transparent);
                    using (var brush = new SolidBrush(Color.FromArgb(128, Color.Red)))
                    {
                        memDc.FillEllipse(brush, 0, 0, _size, _size);
                    }

                    var blend = new BLENDFUNCTION
                    {
                        BlendOp = 0,
                        BlendFlags = 0,
                        SourceConstantAlpha = 128,
                        AlphaFormat = 1
                    };

                    var size = new Size(_size, _size);
                    var ptSrc = new Point(0, 0);
                    var ptDst = new Point(location.X - _size / 2, location.Y - _size / 2);

                    UpdateLayeredWindow(_hwnd, hScreenDc, ref ptDst, ref size,
                        hMemDc, ref ptSrc, 0, ref blend, 2);
                }
                finally
                {
                    if (hMemDc != IntPtr.Zero)
                    {
                        memDc.ReleaseHdc(hMemDc);
                    }
                    if (hScreenDc != IntPtr.Zero)
                    {
                        screenDc.ReleaseHdc(hScreenDc);
                    }
                }
            }
        }

        public void Hide()
        {
            var size = new Size(0, 0);
            var ptSrc = new Point(0, 0);
            var ptDst = new Point(0, 0);
            var blend = new BLENDFUNCTION();

            using (var screenDc = Graphics.FromHwnd(IntPtr.Zero))
            {
                var hScreenDc = screenDc.GetHdc();
                UpdateLayeredWindow(_hwnd, hScreenDc, ref ptDst, ref size,
                    IntPtr.Zero, ref ptSrc, 0, ref blend, 2);
                screenDc.ReleaseHdc(hScreenDc);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_hwnd != IntPtr.Zero)
                {
                    DestroyWindow(_hwnd);
                    _hwnd = IntPtr.Zero;
                }
                _disposed = true;
            }
        }
    }
}