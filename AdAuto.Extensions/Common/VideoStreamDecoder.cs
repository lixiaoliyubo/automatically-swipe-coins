using FFmpeg.AutoGen;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using Yuandl.Common.LogClass;

/// <summary>
/// 视频流解码命名空间
/// 提供视频流解码和帧数据处理的核心功能
/// </summary>
namespace AdAuto.Extensions.Common
{
    /// <summary>
    /// 帧数据类
    /// 用于存储和管理解码后的视频帧数据
    /// 实现了IDisposable接口以确保正确释放非托管资源
    /// </summary>
    /// <remarks>
    /// 该类主要负责：
    /// 1. 存储解码后的视频帧原始数据
    /// 2. 提供帧的基本信息（宽度、高度、帧号等）
    /// 3. 管理帧数据的生命周期
    /// 4. 确保非托管内存的正确释放
    /// </remarks>
    public unsafe class FrameData : IDisposable
    {
        private bool disposed;

        /// <summary>
        /// 帧数据的字节数组，采用BGRA32格式
        /// </summary>
        /// <remarks>
        /// 警告：此字段仅在OnFrame事件内有效。事件结束后，数据将被释放
        /// 如果需要在OnFrame事件外使用Data字段，请确保将其复制到其他位置
        /// 
        /// 数据格式说明：
        /// - 使用BGRA32像素格式
        /// - 每个像素占用4字节
        /// - 字节顺序：Blue、Green、Red、Alpha
        /// </remarks>
        public ReadOnlySpan<byte> Data
        {
            get
            {
                // This line might not be needed?
                if (disposed) throw new ObjectDisposedException(nameof(FrameData));
                return new ReadOnlySpan<byte>(data, length);
            }
        }

        /// <summary>
        /// 帧的宽度（像素）
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// 帧的高度（像素）
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// 帧序号，表示当前帧在视频序列中的位置
        /// </summary>
        public int FrameNumber { get; }

        /// <summary>
        /// 像素格式，定义了帧数据的颜色和位深度格式
        /// </summary>
        public AVPixelFormat PixelFormat { get; }

        /// <summary>
        /// 原始帧数据的指针
        /// </summary>
        private readonly byte* data;

        /// <summary>
        /// 帧数据的长度（字节）
        /// </summary>
        private readonly int length;

        /// <summary>
        /// 初始化帧数据对象
        /// </summary>
        /// <param name="data">指向帧数据的指针</param>
        /// <param name="length">帧数据的字节长度</param>
        /// <param name="width">帧的宽度（像素）</param>
        /// <param name="height">帧的高度（像素）</param>
        /// <param name="frameNumber">帧序号</param>
        /// <param name="pixelFormat">像素格式</param>
        /// <remarks>
        /// 构造函数负责初始化帧数据对象的所有属性
        /// 注意：data参数指向的内存由FFmpeg分配，将在Dispose时释放
        /// </remarks>
        public FrameData(byte* data, int length, int width, int height, int frameNumber, AVPixelFormat pixelFormat)
        {
            this.data = data;
            this.length = length;
            Width = width;
            Height = height;
            FrameNumber = frameNumber;
            PixelFormat = pixelFormat;
        }

        ~FrameData()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        /// <remarks>
        /// 实现IDisposable接口的核心方法
        /// 负责释放所有资源（托管和非托管）
        /// 
        /// 释放的资源包括：
        /// 1. 解析器上下文
        /// 2. 图像格式转换上下文
        /// 3. 解码器上下文
        /// 4. 帧缓冲区
        /// 5. 数据包缓冲区
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                ffmpeg.av_free(data);

                disposed = true;
            }
        }

        /// <summary>
        /// 实现IDisposable接口
        /// </summary>
        /// <remarks>
        /// 提供显式释放资源的方法
        /// 调用Dispose(true)释放所有资源
        /// 通知GC不再调用终结器
        /// </remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 视频流解码器类
    /// 负责解码H.264视频流，将其转换为可显示的帧数据
    /// </summary>
    /// <summary>
    /// 视频流解码器类
    /// 负责将H.264编码的视频流解码为原始帧数据
    /// </summary>
    /// <remarks>
    /// 主要功能：
    /// 1. 初始化和配置FFmpeg解码器
    /// 2. 解析H.264视频流数据
    /// 3. 将解码后的帧转换为BGRA32格式
    /// 4. 通过事件机制通知新帧的到达
    /// 5. 管理解码器和相关资源的生命周期
    /// </remarks>
    public unsafe class VideoStreamDecoder : IDisposable
    {
        /// <summary>
        /// 获取或设置关联的Scrcpy实例
        /// </summary>
        public Scrcpy? Scrcpy { get; set; }

        /// <summary>
        /// Number of the last decoded frame.
        /// </summary>
        /// <summary>
        /// 获取已解码的帧数量
        /// </summary>
        public int FrameCount { get; private set; }

        /// <summary>
        /// 当新的帧被解码完成时触发的事件
        /// </summary>
        public event EventHandler<FrameData>? OnFrame;

        /// <summary>
        /// 标记对象是否已被释放
        /// </summary>
        private bool disposed;

        /// <summary>
        /// 最后一帧的引用计数，用于管理帧数据的生命周期
        /// </summary>
        private int lastFrameRefCount;

        /// <summary>
        /// 用于同步访问最后一帧的锁对象
        /// </summary>
        private readonly object lastFrameLock = new();

        /// <summary>
        /// 图像格式转换上下文，用于转换不同的像素格式
        /// </summary>
        private SwsContext* swsContext = null;

        /// <summary>
        /// 存储最后一帧的数据
        /// </summary>
        private FrameData? lastFrame;

        /// <summary>
        /// H.264解码器实例
        /// </summary>
        private readonly AVCodec* codec;

        /// <summary>
        /// 解码器解析器上下文，用于解析H.264码流
        /// </summary>
        private readonly AVCodecParserContext* parser;

        /// <summary>
        /// 解码器上下文，包含解码器的配置和状态
        /// </summary>
        private readonly AVCodecContext* ctx;

        /// <summary>
        /// 用于存储解码后的原始帧数据
        /// </summary>
        private readonly AVFrame* frame;

        /// <summary>
        /// 用于存储编码的数据包
        /// </summary>
        private readonly AVPacket* packet;

        private static readonly ILogger _logger = LogFactory.CreateLogger<VideoStreamDecoder>();
        /// <summary>
        /// 初始化视频流解码器
        /// 设置H.264解码器并分配必要的资源
        /// </summary>
        /// <summary>
        /// 初始化视频流解码器
        /// </summary>
        /// <remarks>
        /// 构造函数执行以下初始化步骤：
        /// 1. 查找并初始化H.264解码器
        /// 2. 创建解码器解析器上下文
        /// 3. 分配解码器上下文并打开解码器
        /// 4. 分配帧和数据包缓冲区
        /// 
        /// 如果任何初始化步骤失败，将抛出异常
        /// </remarks>
        public VideoStreamDecoder()
        {
            codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (codec == null) throw new Exception("Couldn't find AVCodec for AV_CODEC_ID_H264.");

            parser = ffmpeg.av_parser_init((int)codec->id);
            if (parser == null) throw new Exception("Couldn't initialize AVCodecParserContext.");

            ctx = ffmpeg.avcodec_alloc_context3(codec);
            if (ctx == null) throw new Exception("Couldn't allocate AVCodecContext.");

            int ret = ffmpeg.avcodec_open2(ctx, codec, null);
            if (ret < 0) throw new Exception("Couldn't open AVCodecContext.");

            frame = ffmpeg.av_frame_alloc();
            if (frame == null) throw new Exception("Couldn't allocate AVFrame.");

            packet = ffmpeg.av_packet_alloc();
            if (frame == null) throw new Exception("Couldn't allocate AVPacket.");
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        /// <remarks>
        /// 在对象被垃圾回收时调用
        /// 通过调用Dispose(false)确保非托管资源被正确释放
        /// </remarks>
        ~VideoStreamDecoder()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// 获取最后一帧的数据
        /// </summary>
        /// <returns>最后解码的帧数据，如果没有则返回null</returns>
        public FrameData? GetLastFrame()
        {
            lock (lastFrameLock)
            {
                lastFrameRefCount++;
                return lastFrame;
            }
        }

        /// <summary>
        /// 解码视频数据
        /// </summary>
        /// <param name="data">要解码的视频数据字节数组</param>
        /// <param name="pts">显示时间戳，默认为-1</param>
        /// <summary>
        /// 解码视频数据
        /// </summary>
        /// <param name="data">要解码的H.264编码数据</param>
        /// <param name="pts">显示时间戳（可选）</param>
        /// <remarks>
        /// 解码过程：
        /// 1. 使用解析器将输入数据分割成完整的H.264帧
        /// 2. 将每个完整的帧送入解码器进行解码
        /// 3. 解码后的帧通过OnFrame事件发送给订阅者
        /// 
        /// 注意：
        /// - 输入数据可能包含多个视频帧
        /// - pts参数用于视频同步，-1表示使用默认时间戳
        /// </remarks>
        public void Decode(byte[] data, long pts = -1)
        {
            fixed (byte* dataPtr = data)
            {
                byte* ptr = dataPtr;
                int dataSize = data.Length;

                while (dataSize > 0)
                {
                    int ret = ffmpeg.av_parser_parse2(parser, ctx, &packet->data, &packet->size, ptr, dataSize, pts != -1 ? pts : ffmpeg.AV_NOPTS_VALUE, ffmpeg.AV_NOPTS_VALUE, 0);

                    if (ret < 0)
                        throw new Exception("Error while parsing.");

                    ptr += ret;
                    dataSize -= ret;

                    if (packet->size != 0)
                    {
                        DecodePacket();
                    }
                }
            }
        }

        /// <summary>
        /// 解码单个视频包
        /// </summary>
        /// <remarks>
        /// 实现步骤：
        /// 1. 将数据包发送给解码器
        /// 2. 从解码器接收解码后的帧
        /// 3. 将帧转换为BGRA32格式
        /// 4. 创建FrameData对象并触发OnFrame事件
        /// 
        /// 错误处理：
        /// - 记录解码错误信息
        /// - 跳过无法解码的帧
        /// - 处理格式转换失败的情况
        /// </remarks>
        private void DecodePacket()
        {
            int ret = ffmpeg.avcodec_send_packet(ctx, packet);

            if (ret != ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                if (ret < 0)
                {
                    byte[] errorMessageBytes = new byte[512];
                    fixed (byte* ptr = errorMessageBytes)
                    {
                        ffmpeg.av_strerror(ret, ptr, (ulong)errorMessageBytes.Length);
                        string errorMessage = new((sbyte*)ptr, 0, errorMessageBytes.Length - 1, Encoding.ASCII);
                        _logger.LogError("Error sending a packet for decoding. {@ErrorMessage}", errorMessage);
                    }

                    return;
                }

                while (ret >= 0)
                {
                    ret = ffmpeg.avcodec_receive_frame(ctx, frame);

                    if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
                        return;

                    FrameCount++;

                    if (Scrcpy != null)
                    {
                        Scrcpy.Width = frame->width;
                        Scrcpy.Height = frame->height;
                    }

                    int destSize = 4 * frame->width * frame->height;
                    int[] destStride = new int[] { 4 * frame->width };

                    // In my tests the code crashed when we use a C# byte-array (new byte[])
                    byte* destBufferPtr = (byte*)ffmpeg.av_malloc((ulong)destSize);
                    byte*[] dest = { destBufferPtr };

                    // This `free`s the old context if needed, so there is no leak here.
                    swsContext = ffmpeg.sws_getCachedContext(swsContext, frame->width, frame->height, ctx->pix_fmt, frame->width, frame->height, AVPixelFormat.AV_PIX_FMT_BGRA, ffmpeg.SWS_BICUBIC, null, null, null);

                    if (swsContext == null) throw new Exception("Couldn't allocate SwsContext.");

                    int outputSliceHeight = ffmpeg.sws_scale(swsContext, frame->data, frame->linesize, 0, frame->height, dest, destStride);

                    if (outputSliceHeight > 0)
                    {
                        // Poor man's reference counting.
                        lock (lastFrameLock)
                        {
                            //if (lastFrame != null && Interlocked.Read(ref lastFrameRefCount) == 0)
                            if (lastFrame != null && lastFrameRefCount == 0)
                            {
                                // We don't have to dispose it, but then the GC will remove all old frames after 'some time'.
                                // On my 32GB RAM computer the GC allowed the app to use up to 8GB before cleaning it up.
                                lastFrame.Dispose();
                            }
                            //Interlocked.Exchange(ref lastFrameRefCount, 0);
                            lastFrameRefCount = 0;
                        }

                        // FrameData takes ownership of the destBufferPtr and will free it when disposed!
                        lastFrame = new FrameData(destBufferPtr, destSize, frame->width, frame->height, ctx->frame_number, AVPixelFormat.AV_PIX_FMT_BGRA);
                        OnFrame?.Invoke(this, lastFrame);
                    }
                    else
                    {
                        _logger.LogWarning("outputSliceHeight == 0, not sure if this is bad?");

                        // Manually free the destBufferPtr when we don't create a FrameData object.
                        ffmpeg.av_free(destBufferPtr);
                    }
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        /// <remarks>
        /// 实现IDisposable接口的核心方法
        /// 负责释放所有资源（托管和非托管）
        /// 
        /// 释放的资源包括：
        /// 1. 解析器上下文
        /// 2. 图像格式转换上下文
        /// 3. 解码器上下文
        /// 4. 帧缓冲区
        /// 5. 数据包缓冲区
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                ffmpeg.av_parser_close(parser);
                ffmpeg.sws_freeContext(swsContext);

                fixed (AVCodecContext** ptr = &ctx)
                    ffmpeg.avcodec_free_context(ptr);

                fixed (AVFrame** ptr = &frame)
                    ffmpeg.av_frame_free(ptr);

                fixed (AVPacket** ptr = &packet)
                    ffmpeg.av_packet_free(ptr);

                disposed = true;
            }
        }

        /// <summary>
        /// 实现IDisposable接口
        /// </summary>
        /// <remarks>
        /// 提供显式释放资源的方法
        /// 调用Dispose(true)释放所有资源
        /// 通知GC不再调用终结器
        /// </remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
