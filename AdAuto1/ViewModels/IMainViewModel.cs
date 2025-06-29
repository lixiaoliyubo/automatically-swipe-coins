using AdAuto.enums;
using AdvancedSharpAdbClient.DeviceCommands;
using PaddleOCRSharp;

namespace AdAuto.ViewModels
{
    /// <summary>
    /// 主视图模型接口，定义应用程序的核心业务逻辑
    /// </summary>
    public interface IMainViewModel
    {
        /// <summary>
        /// 获取当前设备信息
        /// </summary>
        DeviceClient CurDevice { get; }

        /// <summary>
        /// 获取或设置当前页面
        /// </summary>
        AppPageEnum CurPage { get; set; }

        /// <summary>
        /// 执行文本识别
        /// </summary>
        /// <returns>OCR识别结果</returns>
        Task<OCRResult?> RecognizeText();

        /// <summary>
        /// 获取屏幕截图
        /// </summary>
        /// <returns></returns>
        Task<byte[]?> CaptureDeviceScreen();
        /// <summary>
        /// 记录日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        void LogMessage(string message);

        /// <summary>
        /// 停止自动化过程并重置UI状态
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        void StopAutomation();

        /// <summary>
        /// 执行点击操作
        /// </summary>
        /// <param name="points">点击坐标</param>
        /// <param name="delay">延迟时间</param>
        /// <param name="text">点击文本描述</param>
        /// <param name="curPage">当前页面</param>
        /// <returns>异步任务</returns>
        Task ClickAsync(List<OCRPoint>? points = null, int delay = 0, string text = "", AppPageEnum curPage = AppPageEnum.Node);

        /// <summary>
        /// 执行滑动操作
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="text">滑动文本描述</param>
        /// <param name="points">起始X坐标</param>
        /// <returns>异步任务</returns>
        Task SwipeAsync(int delay, string text = "", params int[] points);

        /// <summary>
        /// 返回首页
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        Task ClickHomeButtonAsync(int delay = 0, string text = "");

        /// <summary>
        /// 发送本文
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        Task SendTextAsync(int delay = 0, string text = "");

        /// <summary>
        /// 更新任务数据显示
        /// </summary>
        void UpdateTaskData();
    }
}