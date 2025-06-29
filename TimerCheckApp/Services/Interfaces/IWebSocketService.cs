using System;
using System.Threading.Tasks;
using TimerCheckApp.Models;

namespace TimerCheckApp.Services.Interfaces
{
    /// <summary>
    /// WebSocket服务接口
    /// </summary>
    public interface IWebSocketService
    {
        /// <summary>
        /// 连接状态变更事件
        /// </summary>
        event EventHandler<bool> ConnectionStatusChanged;

        /// <summary>
        /// 消息接收事件
        /// </summary>
        event EventHandler<WebSocketResponse> MessageReceived;

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接到WebSocket服务器
        /// </summary>
        /// <returns>连接是否成功</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// 断开WebSocket连接
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 发送消息到WebSocket服务器
        /// </summary>
        /// <param name="message">要发送的消息</param>
        /// <returns>发送是否成功</returns>
        bool SendMessage(string message);

        /// <summary>
        /// 获取所有设备信息
        /// </summary>
        /// <returns>是否成功发送请求</returns>
        bool GetDeviceList();

        /// <summary>
        /// 截图请求
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="onlyDeviceName">是否只使用设备名作为文件名</param>
        /// <returns>是否成功发送请求</returns>
        bool TakeScreenshot(string deviceIds, string savePath, bool onlyDeviceName = true);

        /// <summary>
        /// 屏幕控制事件
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="eventType">指针事件类型</param>
        /// <param name="x">X坐标(0-1)</param>
        /// <param name="y">Y坐标(0-1)</param>
        /// <param name="endX">结束X坐标(0-1)，仅滑动事件需要</param>
        /// <param name="endY">结束Y坐标(0-1)，仅滑动事件需要</param>
        /// <param name="delta">滚轮事件幅度，仅滚轮事件需要</param>
        /// <returns>是否成功发送请求</returns>
        bool SendPointerEvent(string deviceIds, PointerEventType eventType, double x, double y, double endX = 0, double endY = 0, int delta = 0);

        /// <summary>
        /// 发送点击事件（按下+松开）
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="x">X坐标(0-1)</param>
        /// <param name="y">Y坐标(0-1)</param>
        /// <returns>是否成功发送请求</returns>
        bool SendTapEvent(string deviceIds, double x, double y);

        /// <summary>
        /// 发送滑动事件
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="eventType">滑动事件类型</param>
        /// <param name="x">起始X坐标(0-1)</param>
        /// <param name="y">起始Y坐标(0-1)</param>
        /// <param name="endX">结束X坐标(0-1)</param>
        /// <param name="endY">结束Y坐标(0-1)</param>
        /// <returns>是否成功发送请求</returns>
        bool SendSwipeEvent(string deviceIds, PointerEventType eventType, double x, double y, double endX, double endY);
    }
}