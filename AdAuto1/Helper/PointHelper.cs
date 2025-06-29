using PaddleOCRSharp;
using System.Drawing;

namespace AdAuto.Helper
{
    public static class PointHelper
    {
        /// <summary>
        /// 计算OCR识别文本块的几何中心点
        /// </summary>
        /// <param name="boxPoints">文本块的四个顶点坐标（顺时针/逆时针顺序）</param>
        /// <returns>中心点坐标</returns>
        /// <exception cref="ArgumentException">当顶点数量不为4时抛出异常</exception>
        public static Point CalculateTextBlockCenter(this IList<OCRPoint> boxPoints)
        {
            // 参数校验
            if (boxPoints == null || boxPoints.Count != 4)
            {
                throw new ArgumentException("BoxPoints must contain exactly 4 vertices", nameof(boxPoints));
            }
            // 计算X/Y坐标平均值（使用LINQ优化计算）
            int centerX = (int)boxPoints.Average(p => p.X);
            int centerY = (int)boxPoints.Average(p => p.Y);

            return new Point() { X = centerX, Y = centerY };
        }

        /// <summary>
        /// 计算OCR识别文本块的几何中心点（浮点版本）
        /// </summary>
        /// <param name="boxPoints">文本块的四个顶点坐标</param>
        /// <returns>精确的中心点坐标</returns>
        public static System.Drawing.PointF CalculateTextBlockCenterF(IList<OCRPoint> boxPoints)
        {
            if (boxPoints == null || boxPoints.Count != 4)
            {
                throw new ArgumentException("BoxPoints must contain exactly 4 vertices", nameof(boxPoints));
            }

            float centerX = (float)boxPoints.Average(p => p.X);
            float centerY = (float)boxPoints.Average(p => p.Y);

            return new System.Drawing.PointF(centerX, centerY);
        }
    }
}
