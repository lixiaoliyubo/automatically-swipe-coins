using AdAuto.Extensions.Common;

/// <summary>
/// 触摸事件处理命名空间
/// </summary>
namespace AdAuto.Extensions
{
    /// <summary>
    /// 触摸事件辅助类
    /// 提供触摸事件坐标缩放等辅助功能
    /// </summary>
    public static class TouchHelper
    {
        /// <summary>
        /// 将触摸坐标根据屏幕尺寸进行缩放
        /// </summary>
        /// <param name="position">需要缩放的位置信息</param>
        /// <param name="width">目标屏幕宽度</param>
        /// <param name="height">目标屏幕高度</param>
        public static void ScaleToScreenSize(Position position, int width, int height)
        {
            // Lets assume that the scale on the X/Y is the same.
            double scale = (double)width / position.ScreenSize.Width;

            position.ScreenSize.Width = (ushort)width;
            position.ScreenSize.Height = (ushort)height;
            position.Point.X = (ushort)(scale * position.Point.X);
            position.Point.Y = (ushort)(scale * position.Point.Y);
        }
    }
}
