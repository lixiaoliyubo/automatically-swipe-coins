using System;
using System.Runtime.InteropServices;

namespace AdAuto.Extensions.Helper
{
    public class MouseOperations
    {
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_MOVE = 0x0001;

        public static void Click(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
        }

        public static void Swipe(int startX, int startY, int endX, int endY, int duration = 500)
        {
            // 移动到起始位置
            SetCursorPos(startX, startY);
            mouse_event(MOUSEEVENTF_LEFTDOWN, startX, startY, 0, 0);

            // 计算移动步骤
            int steps = duration / 10;
            double xStep = (endX - startX) / (double)steps;
            double yStep = (endY - startY) / (double)steps;

            // 执行滑动
            for (int i = 1; i <= steps; i++)
            {
                int currentX = startX + (int)(xStep * i);
                int currentY = startY + (int)(yStep * i);
                SetCursorPos(currentX, currentY);
                System.Threading.Thread.Sleep(10);
            }

            // 释放鼠标
            mouse_event(MOUSEEVENTF_LEFTUP, endX, endY, 0, 0);
        }
    }
}