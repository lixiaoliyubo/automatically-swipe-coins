using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1;

public class AndroidScreenCapturer
{
    private readonly string _adbPath = @"adb.exe"; // ADB 路径
    private readonly string _deviceId; // 设备ID（如 `adb devices` 获取）
    private readonly string _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // 桌面路径

    public AndroidScreenCapturer(string deviceId)
    {
        _deviceId = deviceId;
    }

    // 获取屏幕截图（二进制数据）
    public byte[] CaptureScreen()
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _adbPath,
            Arguments = $"-s {_deviceId} exec-out screencap -p", // `-p` 表示 PNG 格式
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processStartInfo))
        using (var memoryStream = new MemoryStream())
        {
            process.StandardOutput.BaseStream.CopyTo(memoryStream);
            return memoryStream.ToArray(); // 返回 PNG 二进制数据
        }
    }

    // 保存截图到桌面（按时间戳命名）
    public void SaveScreenToDesktop(byte[] screenData)
    {
        string fileName = Path.Combine("新建文件夹", $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        string filePath = Path.Combine(_desktopPath, fileName);
        File.WriteAllBytes(filePath, screenData);
        Console.WriteLine($"截图已保存到: {filePath}");
    }

}
