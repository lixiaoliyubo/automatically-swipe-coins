using System.Diagnostics;
using System.Text;
using Yuandl.Common.Helper;

namespace AdAuto.Extensions.Helper;

public class ProcessHelper
{


    /// <summary>
    /// 启动 应用 并捕获其输出信息。
    /// </summary>
    /// <param name="exePath">scrcpy.exe 的路径</param>
    /// <param name="param">参数</param>
    /// <returns>标准输出信息</returns>
    public static async Task<string> StartExE(string exePath, string param = "")
    {
        string output = string.Empty;
        string error = string.Empty;

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = param,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            UseShellExecute = false,//是否使用操作系统shell启动
            CreateNoWindow = true
        };

        try
        {
            using Process? process = Process.Start(startInfo);

            if (process == null)
            {
                return "无法启动进程";
            }

            // 读取标准输出和标准错误流
            error = await process.StandardError.ReadToEndAsync();
            output = await process.StandardOutput.ReadToEndAsync();


            // 先等待进程退出
            await process.WaitForExitAsync();
            // 如果有错误信息，添加到输出中
            if (!error.IsNotEmptyOrNull())
            {
                output += "\n错误信息:\n" + error;
            }

        }
        catch (Exception ex)
        {
            output = $"启动 Process 时出错: {ex.ToString()}";
        }

        return output;
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