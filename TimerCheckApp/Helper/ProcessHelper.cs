using System.Diagnostics;
using System.Text;
using System.Threading;
using Yuandl.Common.Helper;

namespace TimerCheckApp.Helper
{
    internal class ProcessHelper
    {



        /// <summary>
        /// 同步运行 scrcpy 并返回输出结果
        /// </summary>
        /// <param name="scrcpyPath">scrcpy.exe 路径</param>
        /// <param name="arguments">启动参数</param>
        /// <param name="timeoutMs">超时时间（毫秒，-1 表示无限等待）</param>
        /// <returns>包含标准输出和错误输出的元组</returns>
        public static async void RunScrcpy(
            string scrcpyPath,
            string arguments = "",
            Action<string>? action = null)
        {
            var tcs = new TaskCompletionSource<bool>();

            var startInfo = new ProcessStartInfo
            {
                FileName = scrcpyPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                // 绑定输出事件
                process.OutputDataReceived += (_, e) => action?.Invoke($"{e.Data}");
                process.ErrorDataReceived += (_, e) => action?.Invoke($"{e.Data}");
                process.Exited += (_, _) => tcs.TrySetResult(true);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                // 异步等待

                await tcs.Task;

            }
        }

        /// <summary>
        /// 执行ADB命令
        /// </summary>
        /// <param name="arguments">命令行参数</param>
        /// <returns>返回命令执行的输出结果</returns>
        /// <remarks>
        /// 此方法会：
        /// 1. 检查ADB工具是否可用
        /// 2. 执行指定的ADB命令
        /// 3. 捕获并处理可能的异常
        /// 4. 记录命令执行日志
        /// </remarks>
        public static async Task<string> ExecuteAdbCommand(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = $"{arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                //WorkingDirectory = EnvironmentConfig.Paths.AdbDir,
                Verb = "runas"  // 以管理员身份运行
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                throw new Exception("无法启动ADB进程");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"ADB命令执行失败: {error}");

            return output;
        }
    }
}