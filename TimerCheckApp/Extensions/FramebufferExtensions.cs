using AdvancedSharpAdbClient.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PaddleOCRSharp;
using Point = System.Drawing.Point;

namespace TimerCheckApp.Extensions
{
    /// <summary>
    /// Framebuffer扩展方法类
    /// </summary>
    public static class FramebufferExtensions
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
        /// <summary>
        /// 将Framebuffer数据转换为图片字节数组(测试可用)
        /// </summary>
        /// <returns>转换后的图片字节数组</returns>
        public static byte[]? ToBitmapAsync(this Framebuffer framebuffer)
        {
            try
            {
                // 参数验证
                if (framebuffer == null)
                    return null;
                    
                byte[] frameBufferData = framebuffer.Data;
                int width = (int)framebuffer.Header.Width;
                int height = (int)framebuffer.Header.Height;
                
                // 验证数据有效性
                if (frameBufferData == null || frameBufferData.Length == 0 || width <= 0 || height <= 0)
                    return null;
                
                // 使用PNG格式
                ImageFormat format = ImageFormat.Png;
                
                // 创建位图并处理
                using (Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    BitmapData bitmapData = null;
                    try
                    {
                        // 锁定位图内存
                        bitmapData = bitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.WriteOnly,
                            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            
                        // 创建一个新的缓冲区来修正颜色通道
                        byte[] correctedBuffer = new byte[bitmapData.Stride * height];
                        
                        // 计算实际可处理的像素数
                        int maxPixels = Math.Min(frameBufferData.Length / 4, width * height);
                        
                        // 遍历每个像素，修正颜色通道顺序（RGBA -> BGRA或其他需要的转换）
                        for (int i = 0; i < maxPixels; i++)
                        {
                            int y = i / width;
                            int x = i % width;
                            
                            int sourceIndex = i * 4; // 假设每个像素4字节
                            int destIndex = y * bitmapData.Stride + x * 4;
                            
                            // 确保索引在有效范围内
                            if (sourceIndex + 3 < frameBufferData.Length && destIndex + 3 < correctedBuffer.Length)
                            {
                                // 交换颜色通道 (可能需要根据实际情况调整)
                                // 假设源数据是RGBA，目标需要BGRA
                                correctedBuffer[destIndex + 0] = frameBufferData[sourceIndex + 2]; // B <- R
                                correctedBuffer[destIndex + 1] = frameBufferData[sourceIndex + 1]; // G <- G
                                correctedBuffer[destIndex + 2] = frameBufferData[sourceIndex + 0]; // R <- B
                                correctedBuffer[destIndex + 3] = frameBufferData[sourceIndex + 3]; // A <- A
                            }
                        }
                        
                        // 将修正后的数据复制到位图
                        System.Runtime.InteropServices.Marshal.Copy(correctedBuffer, 0, bitmapData.Scan0, correctedBuffer.Length);
                        
                        // 释放缓冲区
                        correctedBuffer = null;
                    }
                    finally
                    {
                        // 确保解锁位图
                        if (bitmapData != null)
                        {
                            bitmap.UnlockBits(bitmapData);
                        }
                    }
                    
                    // 将Bitmap转换为指定格式的图片字节数组
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, format);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception)
            {
                // 发生异常时返回null
                return null;
            }
            finally
            {
                // 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

    }
}
