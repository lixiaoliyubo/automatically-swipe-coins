using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Yuandl.Common.Helper;

namespace AdAuto.Extensions.Helper;

public class ImageHelper
{
    /// <summary>
    /// 对输入的位图进行填充处理。如果输入位图的高度和宽度都分别大于等于给定的最小高度和最小宽度，则直接返回该位图；否则创建一个新的位图，新位图的大小为输入位图的大小加上 16，并确保新位图的宽度和高度分别大于等于给定的最小宽度加 16 和最小高度加 16。然后将输入位图绘制到新位图的特定位置，并返回新位图。
    /// </summary>
    /// <param name="image">要进行处理的位图。</param>
    /// <param name="minW">最小宽度，默认值为 64。</param>
    /// <param name="minH">最小高度，默认值为 64。</param>
    /// <returns>处理后的位图。如果输入位图满足尺寸要求则直接返回，否则返回经过填充处理后的新位图。</returns>
    public static Bitmap PadImage(Bitmap image, int minW = 64, int minH = 64)
    {
        if (image.Height >= minH && image.Width >= minW)
        {
            return image;
        }

        int width = Math.Max(image.Width + 16, minW + 16);
        int height = Math.Max(image.Height + 16, minH + 16);

        // Create a compatible bitmap
        Bitmap destination = new(width, height, image.PixelFormat);
        using Graphics gd = Graphics.FromImage(destination);

        gd.Clear(image.GetPixel(0, 0));
        gd.DrawImageUnscaled(image, 8, 8);

        return destination;
    }

    /// <summary>
    /// 将 BitmapSource 转换为 Base64 编码的字符串。首先使用 MemoryStream 保存图像数据，创建 PNG 编码器并将 BitmapSource 写入流中，然后将 MemoryStream 的内容转换为字节数组，最后将字节数组转换为 Base64 字符串并返回。
    /// </summary>
    /// <param name="bitmapSource">要转换的 BitmapSource 对象。</param>
    /// <returns>Base64 编码的字符串，表示输入的 BitmapSource 对象转换后的结果。</returns>
    public static string BitmapSourceToBase64(BitmapSource bitmapSource)
    {
        // 使用 MemoryStream 来保存图像数据
        using (MemoryStream memoryStream = new MemoryStream())
        {
            // 创建 PNG 编码器并将 BitmapSource 写入流
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);

            // 将 MemoryStream 的内容转换为字节数组
            byte[] imageBytes = memoryStream.ToArray();

            // 将字节数组转换为 Base64 字符串
            return Convert.ToBase64String(imageBytes);
        }
    }



    /// <summary>
    /// 将 Bitmap 转换为 Base64 编码的字符串。首先使用 MemoryStream 将 Bitmap 以 PNG 格式保存，获取字节数组，然后将字节数组转换为 Base64 字符串并返回。
    /// </summary>
    /// <param name="bitmap">要转换的 Bitmap 对象。</param>
    /// <returns>Base64 编码的字符串，表示输入的 Bitmap 对象转换后的结果。</returns>
    public static string BitmapToBase64(Bitmap bitmap)
    {
        using MemoryStream memoryStream = new MemoryStream();
        // 保存为 PNG 格式
        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

        // 获取字节数组
        byte[] imageBytes = memoryStream.ToArray();

        // 转换为 Base64 字符串
        return Convert.ToBase64String(imageBytes);
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

    /// <summary>
    /// 获取指定屏幕区域的图像，并将其转换为 ImageSource 对象。首先创建一个与屏幕区域大小相同、像素格式为 Format32bppArgb 的 Bitmap 对象，然后使用 Graphics 对象从屏幕指定区域复制图像到 Bitmap 对象中，最后将 Bitmap 对象转换为 ImageSource 对象并返回。
    /// </summary>
    /// <param name="screenRectangle">要获取图像的屏幕区域。</param>
    /// <returns>ImageSource 对象，表示指定屏幕区域的图像转换后的结果。</returns>
    public static ImageSource GetWindowBoundsImage(Rect screenRectangle)
    {
        using Bitmap bmp = new(screenRectangle.Width.ToInt32(), screenRectangle.Height.ToInt32(), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using Graphics g = Graphics.FromImage(bmp);

        g.CopyFromScreen(screenRectangle.Left.ToInt32(), screenRectangle.Top.ToInt32(), 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
        return BitmapToImageSource(bmp);
    }

    /// <summary>
    /// 获取指定屏幕区域中的一个选定区域的图像，并进行填充处理后返回 Bitmap 对象。首先创建一个与选定区域大小相同、像素格式为 Format32bppArgb 的 Bitmap 对象，然后使用 Graphics 对象从屏幕指定区域的选定位置复制图像到 Bitmap 对象中，接着调用 PadImage 方法对 Bitmap 对象进行填充处理，最后返回处理后的 Bitmap 对象。
    /// </summary>
    /// <param name="screenRectangle">屏幕区域。</param>
    /// <param name="selectedRegion">选定的区域。</param>
    /// <returns>经过处理后的 Bitmap 对象，表示指定屏幕区域中选定区域的图像并经过填充处理后的结果。</returns>
    public static Bitmap GetRegionAsBitmap(Rect screenRectangle, Rectangle selectedRegion)
    {
        Bitmap bmp = new(
            selectedRegion.Width,
            selectedRegion.Height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using Graphics g = Graphics.FromImage(bmp);

        g.CopyFromScreen(
            screenRectangle.Left.ToInt32() + selectedRegion.Left.ToInt32(),
            screenRectangle.Top.ToInt32() + selectedRegion.Top.ToInt32(),
            0,
            0,
            bmp.Size,
            CopyPixelOperation.SourceCopy);

        bmp = PadImage(bmp);
        return bmp;
    }


    public static readonly char[] Separator = new char[] { '\n', '\r' };
    // 这是一个静态只读的字符数组，可能用于分割字符串等操作。

    /// <summary>
    /// 将字节数组转换为Bitmap对象。
    /// </summary>
    /// <param name="data">图像数据的字节数组</param>
    /// <param name="width">图像宽度</param>
    /// <param name="height">图像高度</param>
    /// <param name="pixelFormat">像素格式，默认为Format32bppArgb</param>
    /// <returns>转换后的Bitmap对象</returns>
    public static Bitmap ByteArrayToBitmap(byte[] data, int width, int height, System.Drawing.Imaging.PixelFormat pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb)
    {
        try
        {
            var bitmap = new Bitmap(width, height, pixelFormat);

            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                pixelFormat);

            try
            {
                System.Runtime.InteropServices.Marshal.Copy(data, 0, bitmapData.Scan0, Math.Min(data.Length, bitmapData.Stride * height));
                return bitmap;
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert byte array to bitmap: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将 WriteableBitmap 转换为 byte[]
    /// </summary>
    /// <param name="writeableBitmap">要转换的 WriteableBitmap</param>
    /// <returns>包含像素数据的字节数组</returns>
    public static byte[] WriteableBitmapToByteArray(WriteableBitmap wb)
    {

        // 计算所需缓冲区大小
        int stride = wb.BackBufferStride;
        int bufferSize = stride * wb.PixelHeight;
        byte[] pixels = new byte[bufferSize];

        // 锁定位图
        wb.Lock();
        try
        {
            // 将像素数据复制到字节数组
            System.Runtime.InteropServices.Marshal.Copy(
                wb.BackBuffer,
                pixels,
                0,
                bufferSize);
        }
        finally
        {
            wb.Unlock();
        }

        return pixels;
    }
    /// <summary>
    /// 将 BitmapSource 对象转换为 Bitmap 对象。创建一个新的 Bitmap 对象，大小与 BitmapSource 对象相同，然后将 BitmapSource 对象的像素数据复制到新的 Bitmap 对象中，并返回新的 Bitmap 对象。
    /// </summary>
    /// <param name="source">要转换的 BitmapSource 对象。</param>
    /// <returns>转换后的 Bitmap 对象。</returns>
    public static Bitmap BitmapSourceToBitmap(BitmapSource source)
    {
        Bitmap bmp = new(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        BitmapData data = bmp.LockBits(
          new Rectangle(System.Drawing.Point.Empty, bmp.Size),
          ImageLockMode.WriteOnly,
          System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        source.CopyPixels(
          Int32Rect.Empty,
          data.Scan0,
          data.Height * data.Stride,
          data.Stride);
        bmp.UnlockBits(data);
        GC.Collect();
        return bmp;
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
    /// 将 BitmapSource 对象转换为字节数组。使用 MemoryStream 和指定的编码器（这里是 PNG 编码器）将 BitmapSource 对象保存到流中，然后将流转换为字节数组并返回。
    /// </summary>
    /// <param name="bitmapSource">要转换的 BitmapSource 对象。</param>
    /// <returns>字节数组，表示输入的 BitmapSource 对象转换后的结果。</returns>
    public static byte[] ImageSourceToByteArray(BitmapSource bitmapSource)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            BitmapEncoder encoder = new PngBitmapEncoder(); // 可根据需要选择编码格式
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(stream);
            return stream.ToArray();
        }
        throw new ArgumentException("Invalid ImageSource type.");
    }
    /// <summary>
    /// 保存图片到文件
    /// </summary>
    /// <param name="image">图片数据</param>
    /// <param name="filePath">保存路径</param>
    public static void SaveImageToFile(BitmapSource image, string filePath)
    {
        BitmapEncoder encoder = GetBitmapEncoder(filePath);
        encoder.Frames.Add(BitmapFrame.Create(image));

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            encoder.Save(stream);
        }
    }

    /// <summary>
    /// 根据文件扩展名获取图片编码器
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>图片编码器</returns>
    private static BitmapEncoder GetBitmapEncoder(string filePath)
    {
        var extName = Path.GetExtension(filePath).ToLower();
        if (extName.Equals(".png"))
        {
            return new PngBitmapEncoder();
        }
        else
        {
            return new JpegBitmapEncoder();
        }
    }    

    /// <summary>
    /// 判断 BitmapSource 是否有效
    /// </summary>
    /// <param name="bitmapSource"></param>
    /// <returns></returns>
    public static bool IsBitmapSourceValid(BitmapSource? bitmapSource)
    {
        if (bitmapSource == null)
        {
            return false;
        }

        // 检查图像尺寸是否有效
        if (bitmapSource.PixelWidth == 0 || bitmapSource.PixelHeight == 0)
        {
            return false;
        }

        // 如果是 WriteableBitmap，检查它是否被冻结
        if (bitmapSource is WriteableBitmap writeableBitmap && writeableBitmap.IsFrozen)
        {
            return false;
        }

        return true;
    }
    /// <summary>
    /// 保存 Base64 图片的方法
    /// </summary>
    /// <param name="base64String"></param>
    /// <param name="filePath"></param>
    public static void SaveBase64Image(string base64String, string filePath)
    {
        try
        {
            // 移除可能存在的 Base64 数据前缀
            if (base64String.StartsWith("data:image"))
            {
                int commaIndex = base64String.IndexOf(',');
                if (commaIndex > 0)
                {
                    base64String = base64String.Substring(commaIndex + 1);
                }
            }

            // 将 Base64 字符串解码为字节数组
            byte[] imageBytes = Convert.FromBase64String(base64String);

            // 将字节数组保存为文件
            File.WriteAllBytes(filePath, imageBytes);

        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存图片时出错: {ex.Message}");
        }
    }


    /// <summary>
    /// wb保存（测试可用）
    /// </summary>
    /// <param name="wb"></param>
    /// <param name="filePath"></param>
    /// <exception cref="NotSupportedException"></exception>
    public void SaveWriteableBitmap(WriteableBitmap wb, string filePath)
    {
        // 转换为System.Drawing.Bitmap

        wb.Lock(); byte[] pixels = new byte[16];
        try
        {
            using (Bitmap bitmap = new Bitmap(
                    wb.PixelWidth,
                    wb.PixelHeight,
                    wb.BackBufferStride,
                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb, // 匹配WPF的Pbgra32
                    wb.BackBuffer))
            {

                // 根据扩展名选择图像格式
                ImageFormat format = Path.GetExtension(filePath).ToLower() switch
                {
                    ".png" => ImageFormat.Png,
                    ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                    ".bmp" => ImageFormat.Bmp,
                    ".gif" => ImageFormat.Gif,
                    ".tiff" => ImageFormat.Tiff,
                    _ => throw new NotSupportedException("不支持的图片格式")
                };

                bitmap.Save(filePath, format);
            }
        }
        finally
        {
            wb.Unlock();
        }
    }
}
