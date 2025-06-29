using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace AdAuto.Helper;

public class ImageHelper
{

    /// <summary>
    /// 将Framebuffer数据转换为图片字节数组(测试可用)
    /// </summary>
    /// <param name="frameBufferData">Framebuffer原始数据</param>
    /// <param name="width">图像宽度</param>
    /// <param name="height">图像高度</param>
    /// <param name="format">输出图片格式，默认为PNG</param>
    /// <returns>转换后的图片字节数组</returns>
    public static byte[]? FrameBufferDataToImageBytes(byte[] frameBufferData, int width, int height, ImageFormat format = null)
    {
        if (frameBufferData == null || frameBufferData.Length == 0)
            return null;

        if (format == null)
            format = ImageFormat.Png;

        using (Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
        {
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                #region 以下方法一
                // 创建一个新的缓冲区来修正颜色通道
                byte[] correctedBuffer = new byte[bitmapData.Stride * height];

                // 遍历每个像素，修正颜色通道顺序（RGBA -> BGRA或其他需要的转换）
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int sourceIndex = (y * width + x) * 4; // 假设每个像素4字节
                        int destIndex = y * bitmapData.Stride + x * 4;

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
                }
                // 将修正后的数据复制到位图
                System.Runtime.InteropServices.Marshal.Copy(correctedBuffer, 0, bitmapData.Scan0, correctedBuffer.Length);

                #endregion




                #region 以下方法二
                // 计算需要复制的数据大小
                //int dataSize = Math.Min(frameBufferData.Length, bitmapData.Stride * height);

                // 将Framebuffer数据复制到Bitmap
                //System.Runtime.InteropServices.Marshal.Copy(frameBufferData, 0, bitmapData.Scan0, dataSize);
                #endregion

            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
            //bitmap.Save("C:\\Users\\Administrator\\Desktop\\1111111111.png");

            // 将Bitmap转换为指定格式的图片字节数组
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, format);
                GC.Collect();
                return ms.ToArray();
            }
        }
    }


    /// <summary>
    /// 将 Bitmap 对象转换为 BitmapImage 对象。如果输入的 Bitmap 对象为 null，则返回 null；否则使用 MemoryStream 将 Bitmap 对象保存为 Bmp 格式，然后创建 BitmapImage 对象并从 MemoryStream 中加载数据，最后返回 BitmapImage 对象。
    /// </summary>
    /// <param name="bitmap">要转换的 Bitmap 对象。</param>
    /// <returns>转换后的 BitmapImage 对象，如果输入为 null 则返回 null。</returns>
    public static BitmapImage BitmapToImageSource(Bitmap bitmap)
    {
        if (bitmap == null) return null;
        using (MemoryStream memory = new MemoryStream())
        {
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            bitmap.Dispose();
            memory.Position = 0;
            BitmapImage bitmapimage = new BitmapImage();
            bitmapimage.BeginInit();
            bitmapimage.StreamSource = memory;
            bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapimage.EndInit();

            return bitmapimage;
        }
    }




    /// <summary>
    /// 将 Base64 编码的字符串转换为 BitmapSource 对象。首先将 Base64 字符串转换为字节数组，然后使用 MemoryStream 读取字节数组，创建 BitmapImage 并从 MemoryStream 中加载数据，最后返回 BitmapImage。
    /// </summary>
    /// <param name="base64String">Base64 编码的字符串。</param>
    /// <returns>BitmapSource 对象，表示输入的 Base64 字符串转换后的结果。</returns>
    public static BitmapSource Base64ToBitmapSource(string base64String)
    {
        // 将 Base64 字符串转换为字节数组
        byte[] imageBytes = Convert.FromBase64String(base64String);

        // 创建 MemoryStream 来读取字节数组
        using (MemoryStream memoryStream = new MemoryStream(imageBytes))
        {
            // 创建 BitmapImage 并从 MemoryStream 中加载数据
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 确保流在使用后可以被释放
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // 可以选择冻结以提高性能

            return bitmapImage;
        }
    }
}
