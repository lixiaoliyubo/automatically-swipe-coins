using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdAuto.Extensions.Helper
{
    public class MinicapStreamer : IDisposable
    {
        private const int DefaultPort = 1717;
        private readonly string _minicapPath;
        private readonly string _minicapSoPath;
        private Process _minicapProcess;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private bool _isRunning;
        private bool _disposed;
        private string _lastKnownResolution;

        public event Action<Bitmap> OnFrameReceived;

        public MinicapStreamer()
        {
            _minicapPath = @"E:\开源工具学习源码\minicap\libs\arm64-v8a\minicap";
            _minicapSoPath = @"E:\开源工具学习源码\minicap\libs\arm64-v8a\minicap.so";

            if (!File.Exists(_minicapPath)) throw new FileNotFoundException($"minicap二进制文件不存在: {_minicapPath}");
            if (!File.Exists(_minicapSoPath)) throw new FileNotFoundException($"minicap.so库文件不存在: {_minicapSoPath}");
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_isRunning) throw new InvalidOperationException("Minicap流已经在运行");

            try
            {
                _isRunning = true;

                // 1. 部署minicap到设备
                await DeployMinicapAsync(cancellationToken);

                // 2. 获取设备分辨率（带重试）
                _lastKnownResolution = await GetDeviceResolutionWithRetryAsync(cancellationToken);
                Debug.WriteLine($"最终使用的分辨率: {_lastKnownResolution}");

                // 3. 启动minicap进程（带详细日志）
                StartMinicapProcess(_lastKnownResolution);

                // 4. 设置端口转发
                await SetupPortForwardingAsync(cancellationToken);

                // 5. 连接并开始接收帧
                await ConnectAndStreamFramesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动失败: {ex}");
                await StopAsync();
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;
            _isRunning = false;

            try
            {
                _networkStream?.Dispose();
                _tcpClient?.Dispose();

                if (_minicapProcess != null)
                {
                    try
                    {
                        if (!_minicapProcess.HasExited)
                        {
                            _minicapProcess.Kill();
                            await _minicapProcess.WaitForExitAsync();
                        }
                    }
                    finally
                    {
                        _minicapProcess.Dispose();
                    }
                }

                await RemovePortForwardingAsync();
            }
            finally
            {
                _networkStream = null;
                _tcpClient = null;
                _minicapProcess = null;
            }
        }

        private async Task DeployMinicapAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                ExecuteAdbCommand($"push \"{_minicapPath}\" /data/local/tmp/");
                ExecuteAdbCommand($"push \"{_minicapSoPath}\" /data/local/tmp/");
                ExecuteAdbCommand("shell chmod 755 /data/local/tmp/minicap");
            }, cancellationToken);

            void ExecuteAdbCommand(string arguments)
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process?.WaitForExit();
            }
        }

        private async Task<string> GetDeviceResolutionWithRetryAsync(CancellationToken cancellationToken, int maxRetries = 3)
        {
            Exception lastException = null;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var resolution = await GetDeviceResolutionAsync(cancellationToken);
                    if (!string.IsNullOrEmpty(resolution))
                        return resolution;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Debug.WriteLine($"获取分辨率失败 (尝试 {i + 1}/{maxRetries}): {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
            }

            throw new InvalidOperationException("无法获取设备分辨率", lastException);
        }

        private async Task<string> GetDeviceResolutionAsync(CancellationToken cancellationToken)
        {
            // 方法1: wm size
            var result = await ExecuteAdbShellCommand("wm size", cancellationToken);
            if (TryParseResolution(result, out var resolution))
                return resolution;

            // 方法2: dumpsys window
            result = await ExecuteAdbShellCommand("dumpsys window displays | grep -Eo \"cur=[0-9]+x[0-9]+\"", cancellationToken);
            if (TryParseResolution(result?.Replace("cur=", ""), out resolution))
                return resolution;

            // 方法3: getevent -p
            result = await ExecuteAdbShellCommand("getevent -p | grep -E \"Resolution: [0-9]+ [0-9]+\"", cancellationToken);
            if (TryParseResolution(result?.Replace("Resolution: ", "").Replace(" ", "x"), out resolution))
                return resolution;

            return "1080x1920"; // 默认值

            bool TryParseResolution(string input, out string resolution)
            {
                if (!string.IsNullOrEmpty(input))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(input, @"(\d+)x(\d+)");
                    if (match.Success)
                    {
                        resolution = $"{match.Groups[1].Value}x{match.Groups[2].Value}";
                        return true;
                    }
                }
                resolution = null;
                return false;
            }
        }

        private async Task<string> ExecuteAdbShellCommand(string command, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "adb",
                        Arguments = $"shell {command}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }, cancellationToken);
        }

        private void StartMinicapProcess(string resolution)
        {
            var arguments = $"shell LD_LIBRARY_PATH=/data/local/tmp /data/local/tmp/minicap -P {resolution}@{resolution}/0 -v -Q 75 -t";
            Debug.WriteLine($"启动minicap命令: adb {arguments}");

            _minicapProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            _minicapProcess.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine($"[Minicap] {e.Data}");
            };

            _minicapProcess.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine($"[Minicap ERR] {e.Data}");
            };

            _minicapProcess.Start();
            _minicapProcess.BeginOutputReadLine();
            _minicapProcess.BeginErrorReadLine();
        }

        private async Task SetupPortForwardingAsync(CancellationToken cancellationToken)
        {
            await ExecuteAdbCommand($"forward tcp:{DefaultPort} localabstract:minicap", cancellationToken);
            await Task.Delay(1500, cancellationToken); // 等待服务稳定
        }

        private async Task RemovePortForwardingAsync()
        {
            await ExecuteAdbCommand("forward --remove-all", CancellationToken.None);
        }

        private async Task ExecuteAdbCommand(string arguments, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process?.WaitForExit();
            }, cancellationToken);
        }

        private async Task ConnectAndStreamFramesAsync(CancellationToken cancellationToken)
        {
            _tcpClient = new TcpClient { NoDelay = true };
            await _tcpClient.ConnectAsync("127.0.0.1", DefaultPort);
            _networkStream = _tcpClient.GetStream();

            // 1. 读取并验证banner (24字节)
            var banner = await ReadExactAsync(_networkStream, 24, cancellationToken);
            var width = BitConverter.ToInt32(banner, 0);
            var height = BitConverter.ToInt32(banner, 4);

            // 如果banner分辨率无效，使用adb获取的分辨率
            if (width <= 0 || height <= 0)
            {
                var parts = _lastKnownResolution.Split('x');
                width = int.Parse(parts[0]);
                height = int.Parse(parts[1]);
                Debug.WriteLine($"使用备用分辨率: {width}x{height}");
            }

            var frameSize = width * height * 4;
            if (frameSize <= 0 || frameSize > 100 * 1024 * 1024)
                throw new InvalidOperationException($"无效的帧大小: {frameSize}字节");

            Debug.WriteLine($"帧信息 - 宽度: {width}, 高度: {height}, 帧大小: {frameSize}字节");

            // 2. 准备帧处理
            var header = new byte[4];
            var frameBuffer = new byte[frameSize];
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 读取帧头
                    await ReadExactAsync(_networkStream, header, cancellationToken);
                    var currentFrameSize = BitConverter.ToInt32(header, 0);

                    if (currentFrameSize != frameSize)
                    {
                        Debug.WriteLine($"帧大小不匹配: {currentFrameSize} != {frameSize}");
                        continue;
                    }

                    // 读取帧数据
                    await ReadExactAsync(_networkStream, frameBuffer, cancellationToken);

                    // 更新Bitmap
                    var bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, width, height),
                        ImageLockMode.WriteOnly,
                        PixelFormat.Format32bppArgb);

                    Marshal.Copy(frameBuffer, 0, bitmapData.Scan0, frameSize);
                    bitmap.UnlockBits(bitmapData);

                    // 触发事件（注意克隆bitmap避免资源冲突）
                    OnFrameReceived?.Invoke((Bitmap)bitmap.Clone());
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"帧处理错误: {ex.Message}");
                    if (_tcpClient?.Connected != true) break;
                }
            }
        }

        private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int count, CancellationToken cancellationToken)
        {
            var buffer = new byte[count];
            await ReadExactAsync(stream, buffer, cancellationToken);
            return buffer;
        }

        private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            int bytesRead = 0;
            while (bytesRead < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead, cancellationToken);
                if (read == 0) throw new EndOfStreamException();
                bytesRead += read;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // 确保资源释放
                _networkStream?.Dispose();
                _tcpClient?.Dispose();
                _minicapProcess?.Dispose();
            }
        }
    }
}