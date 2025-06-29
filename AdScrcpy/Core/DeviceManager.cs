using System.Diagnostics;

namespace AdScrcpy.Core
{
    public class DeviceManager
    {
        private const string AdbPath = "adb";

        public static List<string> GetConnectedDevices()
        {
            var devices = new List<string>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = AdbPath,
                    Arguments = "devices",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("\tdevice"))
                {
                    devices.Add(line.Split('\t')[0]);
                }
            }

            return devices;
        }

        public static string GetDeviceName(string deviceId)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = AdbPath,
                    Arguments = $"-s {deviceId} shell getprop ro.product.model",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string deviceName = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return string.IsNullOrEmpty(deviceName) ? deviceId : deviceName;
        }

        public static Process StartScrcpy(string deviceId, string title)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "scrcpy",
                    Arguments = $" -s {deviceId} --window-title \"{title}\" --max-fps 30 --video-bit-rate 2M --render-driver=opengl --turn-screen-off --no-audio",//--window-borderless 
                    UseShellExecute = true,
                    WorkingDirectory= "D:\\×Ô¶¯Ë¢½ð±Ò\\scrcpy\\",
                }
            };
            process.Start();
            return process;
        }
    }
}