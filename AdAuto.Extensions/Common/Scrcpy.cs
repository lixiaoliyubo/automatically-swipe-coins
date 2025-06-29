using AdAuto.Extensions.enums;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using Microsoft.Extensions.Logging;
using SharpAdbClient;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using Yuandl.Common.Helper;
using Yuandl.Common.LogClass;

/// <summary>
/// ScrcpyNet的核心命名空间
/// 包含了与Android设备交互的所有核心功能
/// </summary>
namespace AdAuto.Extensions.Common
{
    /// <summary>
    /// Scrcpy核心类
    /// 负责与Android设备建立连接、处理视频流和控制命令
    /// </summary>
    public class Scrcpy
    {
        public int Port  { get; set; } = 27183;
        /// <summary>
        /// 获取Android设备名称
        /// </summary>
        public string DeviceName { get; private set; } = "";

        /// <summary>
        /// 获取或设置视频流宽度
        /// </summary>
        public int Width { get; internal set; }

        /// <summary>
        /// 获取或设置视频流高度
        /// </summary>
        public int Height { get; internal set; }

        /// <summary>
        /// 获取或设置视频流比特率，默认为8Mbps
        /// </summary>
        public long Bitrate { get; set; } = 8000000;

        /// <summary>
        /// 获取或设置scrcpy服务器文件路径
        /// </summary>
        public string ScrcpyServerFile { get; set; } = "scrcpy/scrcpy-server.jar";

        /// <summary>
        /// 获取设备连接状态
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// 获取视频流解码器实例
        /// </summary>
        public VideoStreamDecoder VideoStreamDecoder { get; }

        private Thread? videoThread;
        private Thread? controlThread;
        private TcpClient? videoClient;
        private TcpClient? controlClient;
        private TcpListener? listener;
        private CancellationTokenSource? cts;

        private readonly AdbClient adb;
        private readonly DeviceData device;
        private readonly Channel<IControlMessage> controlChannel = Channel.CreateUnbounded<IControlMessage>();
        private static readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;
        private static readonly ILogger log = LogFactory.CreateLogger<Scrcpy>();

        /// <summary>
        /// 初始化Scrcpy实例
        /// </summary>
        /// <param name="device">Android设备信息，包含设备标识和连接信息</param>
        /// <param name="videoStreamDecoder">视频流解码器，如果为null则使用默认解码器</param>
        /// <remarks>
        /// 创建新的Scrcpy实例，初始化ADB客户端和视频流解码器
        /// 设置解码器与Scrcpy实例的关联
        /// </remarks>
        public Scrcpy(DeviceData device, VideoStreamDecoder? videoStreamDecoder = null)
        {
            adb = new AdbClient();
            this.device = device;
            VideoStreamDecoder = videoStreamDecoder ?? new VideoStreamDecoder();
            VideoStreamDecoder.Scrcpy = this;
        }

        //public void SetDecoder(VideoStreamDecoder videoStreamDecoder)
        //{
        //    this.videoStreamDecoder = videoStreamDecoder;
        //    this.videoStreamDecoder.Scrcpy = this;
        //}

        /// <summary>
        /// 启动Scrcpy服务
        /// </summary>
        /// <param name="timeoutMs">连接超时时间（毫秒），默认为5000ms</param>
        /// <exception cref="Exception">当连接已存在或连接超时时抛出异常</exception>
        /// <remarks>
        /// 1. 检查是否已连接
        /// 2. 设置移动端服务器
        /// 3. 启动TCP监听器
        /// 4. 等待视频和控制连接建立
        /// 5. 读取设备信息
        /// 6. 启动视频和控制线程
        /// </remarks>
        public void Start(long timeoutMs = 5000)
        {
            if (Connected)
                throw new Exception("Already connected.");

            MobileServerSetup();

            listener = new TcpListener(IPAddress.Loopback, Port);
            listener.Start();

            MobileServerStart();

            int waitTimeMs = 0;
            while (!listener.Pending())
            {
                Thread.Sleep(10);
                waitTimeMs += 10;

                if (waitTimeMs > timeoutMs)
                    throw new Exception("Timeout while waiting for server to connect.");
            }

            videoClient = listener.AcceptTcpClient();
            log.LogInformation("Video socket connected.");

            if (!listener.Pending())
                throw new Exception("Server is not sending a second connection request. Is 'control' disabled?");

            controlClient = listener.AcceptTcpClient();
            log.LogInformation("Control socket connected.");

            ReadDeviceInfo();

            cts = new CancellationTokenSource();

            videoThread = new Thread(VideoMain) { Name = "ScrcpyNet Video" };
            controlThread = new Thread(ControllerMain) { Name = "ScrcpyNet Controller" };

            videoThread.Start();
            controlThread.Start();

            Connected = true;

            MobileServerCleanup();
        }

        /// <summary>
        /// 停止Scrcpy服务
        /// </summary>
        /// <exception cref="Exception">当未连接时抛出异常</exception>
        /// <remarks>
        /// 1. 检查连接状态
        /// 2. 取消所有操作
        /// 3. 等待视频和控制线程结束
        /// 4. 停止TCP监听
        /// </remarks>
        public void Stop()
        {
            if (!Connected)
                throw new Exception("Not connected.");

            cts?.Cancel();
            videoThread?.Join();
            controlThread?.Join();
            listener?.Stop();
            VideoStreamDecoder?.Dispose();
        }

        /// <summary>
        /// 执行滑动手势
        /// </summary>
        /// <param name="x1">起始X坐标</param>
        /// <param name="y1">起始Y坐标</param>
        /// <param name="x2">结束X坐标</param>
        /// <param name="y2">结束Y坐标</param>
        /// <param name="speed">滑动持续时间（毫秒）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务</returns>
        /// <exception cref="Exception">当坐标无效或执行失败时抛出异常</exception>
        /// <remarks>
        /// 通过ADB执行input swipe命令实现滑动操作
        /// 支持设置滑动速度和取消操作
        /// </remarks>
        public async Task SwipeAsync(int x1, int y1, int x2, int y2, long speed, CancellationToken cancellationToken = default)
        {
            ConsoleOutputReceiver receiver = new() { };
            await adb.ExecuteRemoteCommandAsync($"input swipe {x1} {y1} {x2} {y2} {speed}", device, receiver, cancellationToken).ConfigureAwait(false);

            string result = receiver.ToString().Trim();

            if (result.StartsWith("java.lang."))
            {
                throw new Exception(result);
            }
            else if (result.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Coordinates of element is invalid");
            }
        }

        /// <summary>
        /// 执行点击操作
        /// </summary>
        /// <param name="point">点击坐标</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务</returns>
        /// <exception cref="Exception">当坐标无效或执行失败时抛出异常</exception>
        /// <remarks>
        /// 通过ADB执行input tap命令实现点击操作
        /// 支持取消操作
        /// </remarks>
        public async Task ClickAsync(Point point, CancellationToken cancellationToken = default)
        {
            ConsoleOutputReceiver receiver = new() { };
            await adb.ExecuteRemoteCommandAsync($"input tap {point.X} {point.Y}", device, receiver, cancellationToken).ConfigureAwait(false);

            string result = receiver.ToString().Trim();

            if (result.StartsWith("java.lang."))
            {
                throw new Exception(result);
            }
            else if (result.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Coordinates of element is invalid");
            }
        }

        /// <summary>
        /// 执行点击操作
        /// </summary>
        /// <param name="x">点击坐标</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务</returns>
        /// <exception cref="Exception">当坐标无效或执行失败时抛出异常</exception>
        /// <remarks>
        /// 通过ADB执行input tap命令实现点击操作
        /// 支持取消操作
        /// </remarks>
        public async Task ClickAsync(int x, int y, CancellationToken cancellationToken = default)
        {
            ConsoleOutputReceiver receiver = new() { };
            await adb.ExecuteRemoteCommandAsync($"input tap {x} {y}", device, receiver, cancellationToken).ConfigureAwait(false);

            string result = receiver.ToString().Trim();

            if (result.StartsWith("java.lang."))
            {
                throw new Exception(result);
            }
            else if (result.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Coordinates of element is invalid");
            }
        }
        /// <summary>
        /// Send key event to specific asynchronously. You can see key events here https://developer.android.com/reference/android/view/KeyEvent.
        /// </summary>
        /// <param name="key">The key event to send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public async Task SendKeyEventAsync(string key, CancellationToken cancellationToken = default)
        {
            ConsoleOutputReceiver receiver = new() { };
            await adb.ExecuteRemoteCommandAsync($"input keyevent {key}", device, receiver, cancellationToken).ConfigureAwait(false);

            string result = receiver.ToString().Trim();

            if (result.StartsWith("java.lang."))
            {
                throw new Exception(result);
            }
            else if (result.Contains("ERROR", StringComparison.OrdinalIgnoreCase)) // error or ERROR
            {
                throw new Exception("KeyEvent is invalid");
            }
        }
        /// <summary>
        /// Asynchronously click BACK button.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public Task ClickBackButtonAsync(CancellationToken cancellationToken = default) => SendKeyEventAsync("KEYCODE_BACK", cancellationToken);

        /// <summary>
        /// Asynchronously click HOME button.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public Task ClickHomeButtonAsync(CancellationToken cancellationToken = default) => SendKeyEventAsync("KEYCODE_HOME", cancellationToken);

        /// <summary>
        /// 发送控制命令到Android设备
        /// </summary>
        /// <param name="msg">要发送的控制消息</param>
        public void SendControlCommand(IControlMessage msg)
        {
            if (controlClient == null)
                log.LogWarning("SendControlCommand() called, but controlClient is null.");
            else
                controlChannel.Writer.TryWrite(msg);
        }
        /// <summary>
        /// 读取设备信息，包括设备名称和屏幕分辨率
        /// </summary>
        /// <exception cref="Exception">当videoClient为空或读取数据失败时抛出异常</exception>
        private void ReadDeviceInfo()
        {
            // 检查视频客户端连接是否有效
            if (videoClient == null)
                throw new Exception("Can't read device info when videoClient is null.");

            // 获取信息流并设置读取超时时间
            var infoStream = videoClient.GetStream();
            infoStream.ReadTimeout = 2000;

            // 读取68字节的设备信息头
            // 前64字节为设备名称，后4字节为屏幕分辨率（宽高各2字节）
            var deviceInfoBuf = pool.Rent(68);
            int bytesRead = infoStream.Read(deviceInfoBuf, 0, 68);

            // 验证是否成功读取完整的设备信息
            if (bytesRead != 68)
                throw new Exception($"Expected to read exactly 68 bytes, but got {bytesRead} bytes.");

            // 将缓冲区转换为Span以便高效处理
            var deviceInfoSpan = deviceInfoBuf.AsSpan();
            // 解析设备名称：从前64字节中提取UTF8编码的字符串，并移除末尾的空字符
            DeviceName = Encoding.UTF8.GetString(deviceInfoSpan[..64]).TrimEnd(new[] { '\0' });
            log.LogInformation("Device name: " + DeviceName);

            // 解析屏幕分辨率：从后4字节中提取宽度和高度（大端字节序）
            Width = BinaryPrimitives.ReadInt16BigEndian(deviceInfoSpan[64..]);
            Height = BinaryPrimitives.ReadInt16BigEndian(deviceInfoSpan[66..]);
            log.LogInformation($"Initial texture: {Width}x{Height}");

            // 释放租借的缓冲区
            pool.Return(deviceInfoBuf);
        }


        private void VideoMain()
        {
            // 检查视频客户端和取消令牌是否正确初始化
            if (videoClient == null) throw new Exception("videoClient is null.");
            if (cts == null) throw new Exception("cts is null.");

            // 获取视频流并设置读取超时时间
            var videoStream = videoClient.GetStream();
            videoStream.ReadTimeout = 2000;

            int bytesRead;
            // 分配12字节的缓冲区用于读取视频元数据
            var metaBuf = pool.Rent(12);

            // 主循环：持续处理视频流直到收到取消信号
            while (!cts.Token.IsCancellationRequested)
            {
                // 读取视频包的元数据（每个数据包都以12字节的元数据开始）
                try
                {
                    bytesRead = videoStream.Read(metaBuf, 0, 12);
                }
                catch (IOException ex)
                {
                    // 忽略超时错误，继续尝试读取
                    if (ex.InnerException is SocketException x && x.SocketErrorCode == SocketError.TimedOut)
                        continue;
                    throw new Exception(ex.Message);
                }

                // 验证元数据读取是否完整
                if (bytesRead != 12)
                {
                    throw new Exception($"Expected to read exactly 12 bytes, but got {bytesRead} bytes.");
                }

                // 解析元数据：提取时间戳和数据包大小
                var metaSpan = metaBuf.AsSpan();
                var presentationTimeUs = BinaryPrimitives.ReadInt64BigEndian(metaSpan);
                var packetSize = BinaryPrimitives.ReadInt32BigEndian(metaSpan[8..]);

                // 分配缓冲区并读取完整的视频帧数据
                var packetBuf = pool.Rent(packetSize);
                var pos = 0;
                var bytesToRead = packetSize;

                // 循环读取直到获取完整的数据包
                while (bytesToRead != 0 && !cts.Token.IsCancellationRequested)
                {
                    bytesRead = videoStream.Read(packetBuf, pos, bytesToRead);

                    // 检查是否成功读取数据
                    if (bytesRead == 0)
                        throw new Exception("Unable to read any bytes.");

                    // 更新读取位置和剩余字节数
                    pos += bytesRead;
                    bytesToRead -= bytesRead;
                }

                // 如果未被取消，则解码视频帧并记录处理时间
                if (!cts.Token.IsCancellationRequested)
                {
                    //Log.Verbose($"Presentation Time: {presentationTimeUs}us, PacketSize: {packetSize} bytes");
                    VideoStreamDecoder?.Decode(packetBuf, presentationTimeUs);
                    log.LogDebug("Received and decoded a packet in {@ElapsedMilliseconds} ms");
                }

                pool.Return(packetBuf);
            }
        }

        private async void ControllerMain()
        {
            // Both of these should never happen.
            if (controlClient == null) throw new Exception("controlClient is null.");
            if (cts == null) throw new Exception("cts is null.");

            var stream = controlClient.GetStream();

            try
            {
                await foreach (var cmd in controlChannel.Reader.ReadAllAsync(cts.Token))
                {
                    ControllerSend(stream, cmd);
                }
            }
            catch (OperationCanceledException) { }
        }

        // This needs to be in a separate method, because we can't use a Span<byte> inside an async function.
        private void ControllerSend(NetworkStream stream, IControlMessage cmd)
        {
            log.LogDebug("Sending control message: {@ControlMessage}", cmd.Type);
            var bytes = cmd.ToBytes();
            stream.Write(bytes);
        }

        private void MobileServerSetup()
        {
            MobileServerCleanup();

            // Push scrcpy-server.jar
            UploadMobileServer();

            // Create port reverse rule
            adb.CreateReverseForward(device, "localabstract:scrcpy", $"tcp:{Port}", true);
        }

        /// <summary>
        /// Remove ADB forwards/reverses.
        /// </summary>
        private void MobileServerCleanup()
        {
            // Remove any existing network stuff.
            adb.RemoveAllForwards(device);
            adb.RemoveAllReverseForwards(device);
        }

        /// <summary>
        /// 启动Android设备上的scrcpy服务器
        /// </summary>
        private void MobileServerStart()
        {
            log.LogInformation("Starting scrcpy server...");

            var cts = new CancellationTokenSource();

            // 设置scrcpy服务器版本号
            string version = "1.23";
            // 设置最大帧率，控制视频流的流畅度
            int maxFramerate = 60;
            // 设置视频方向锁定模式，Unlocked表示允许自由旋转
            ScrcpyLockVideoOrientation orientation = ScrcpyLockVideoOrientation.Unlocked; // -1 means allow rotate
            // 是否启用设备控制功能
            bool control = true;
            // 是否在设备上显示触摸点
            bool showTouches = true;
            // 是否保持设备屏幕常亮
            bool stayAwake = true;

            // 构建启动scrcpy服务器的命令行参数列表
            var cmds = new List<string>
            {
                // 设置服务器jar包的类路径
                "CLASSPATH=/data/local/tmp/scrcpy-server.jar",
                // 使用app_process启动Java进程
                "app_process",

                // 未使用的参数
                "/",

                // 指定服务器入口类
                "com.genymobile.scrcpy.Server",

                // 配置服务器参数
                version,                    // 服务器版本
                "log_level=debug",          // 设置日志级别为debug
                $"bit_rate={Bitrate}"      // 设置视频流比特率
            };

            // 配置最大帧率（如果不为0）
            if (maxFramerate != 0)
                cmds.Add($"max_fps={maxFramerate}");

            // 配置视频方向锁定（如果不是自由旋转模式）
            if (orientation != ScrcpyLockVideoOrientation.Unlocked)
                cmds.Add($"lock_video_orientation={(int)orientation}");

            // 禁用音频传输
            cmds.Add("audio=false");
            // 禁用隧道转发
            cmds.Add("tunnel_forward=false");
            // 启用/禁用设备控制
            cmds.Add($"control={control}");
            // 设置显示ID
            cmds.Add("display_id=0");
            // 配置是否显示触摸点
            cmds.Add($"show_touches={showTouches}");
            // 配置是否保持屏幕常亮
            cmds.Add($"stay_awake={stayAwake}");
            // 关闭时不自动关闭设备屏幕
            cmds.Add("power_off_on_close=false");
            // 出错时自动降低分辨率
            cmds.Add("downsize_on_error=true");
            // 退出时自动清理资源
            cmds.Add("cleanup=true");

            // 将所有参数组合成完整的命令字符串
            string command = string.Join(" ", cmds);

            log.LogInformation("Start command: " + command);
            // 异步执行命令启动服务器
            _ = adb.ExecuteRemoteCommandAsync(command, device, null, cts.Token);
        }

        private void UploadMobileServer()
        {
            using SyncService service = new(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)), device);
            using Stream stream = File.OpenRead(ScrcpyServerFile);
            service.Push(stream, "/data/local/tmp/scrcpy-server.jar", 444, DateTime.Now, null, CancellationToken.None);
        }
    }
}
