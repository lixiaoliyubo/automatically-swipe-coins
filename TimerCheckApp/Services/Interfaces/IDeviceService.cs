using System.Collections.ObjectModel;
using AdvancedSharpAdbClient;
using PaddleOCRSharp;

namespace TimerCheckApp.Services.Interfaces
{
    /// <summary>
    /// 设备服务接口
    /// </summary>
    public interface IDeviceService
    {
        AdbClient CurAdbClient { get; }
        /// <summary>
        /// 获取当前运行的应用信息
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>当前应用包名</returns>
        string GetCurrentApp(string deviceId);
        /// <summary>
        /// 获取所有连接的设备
        /// </summary>
        /// <returns>设备模型列表</returns>
        ObservableCollection<DeviceModel> GetDevices();

        /// <summary>
        /// 启动设备检查任务
        /// </summary>
        /// <param name="device">设备模型</param>
        /// <param name="screenshotSaveDir">保存截图文件夹位置</param>
        void StartDeviceCheck(DeviceModel device, string screenshotSaveDir);

        /// <summary>
        /// 使用ADB进行截图
        /// </summary>
        /// <param name="device">设备信息</param>
        /// <returns>是否截图成功</returns>
        Task<byte[]?> CaptureWithAdb(DeviceModel device);
        /// <summary>
        /// 执行OCR识别获取本文
        /// </summary>
        /// <param name="device">当前设备</param>
        /// <returns></returns>
        Task<OCRResult?> GetOCRText(DeviceModel device);
        /// <summary>
        /// 停止设备检查任务
        /// </summary>
        /// <param name="device">设备模型</param>
        void StopDeviceCheck(DeviceModel device);

        /// <summary>
        /// 清理指定设备上的所有运行中的应用
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        void CleanDeviceRunningApps(string deviceId);

        /// <summary>
        /// 清理所有运行中的应用
        /// </summary>
        void CleanAllRunningApps();

        /// <summary>
        /// 释放资源
        /// </summary>
        void Dispose();

        /// <summary>
        /// 获取设备屏幕尺寸
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>尺寸元组 (宽度, 高度)</returns>
        (int width, int height) GetDeviceSize(string deviceId);

        /// <summary>
        /// 启动指定设备的投屏（scrcpy）
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>是否成功启动</returns>
        bool StartScreenMirror(string deviceId);

        /// <summary>
        /// 播放提示音
        /// </summary>
        /// <param name="fileName">音频文件名</param>
        Task PlayAudioAsync(string fileName);
        
        /// <summary>
        /// 播放文本内容（文本转语音）
        /// </summary>
        /// <param name="text">要播放的文本内容</param>
        /// <returns>播放任务</returns>
        Task PlayTextAsync(string text);

        /// <summary>
        /// 停止所有音频播放
        /// </summary>
        void StopAllAudio();
    }
}