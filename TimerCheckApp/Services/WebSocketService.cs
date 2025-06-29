using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebSocketSharp;
using TimerCheckApp.Models;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Services
{
    /// <summary>
    /// WebSocket服务类，负责与WebSocket服务器通信
    /// </summary>
    public class WebSocketService : IWebSocketService
    {
        private readonly ILogService _logService;
        private WebSocket _webSocket;
        private readonly string _serverUrl;
        private bool _isConnected;
        
        // 重连相关字段
        private int _reconnectAttempts = 0;
        private readonly int _maxReconnectAttempts = 5;
        private readonly int _reconnectDelayMs = 2000;
        private readonly object _reconnectLock = new object();
        private bool _isReconnecting = false;
        private System.Threading.Timer _reconnectTimer;

        /// <summary>
        /// 连接状态变更事件
        /// </summary>
        public event EventHandler<bool> ConnectionStatusChanged;

        /// <summary>
        /// 消息接收事件
        /// </summary>
        public event EventHandler<WebSocketResponse> MessageReceived;
        

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    ConnectionStatusChanged?.Invoke(this, _isConnected);
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="serverUrl">WebSocket服务器地址</param>
        public WebSocketService(ILogService logService, string serverUrl = "ws://127.0.0.1:33332")
        {
            _logService = logService;
            _serverUrl = serverUrl;
            _isConnected = false;
        }

        /// <summary>
        /// 连接到WebSocket服务器
        /// </summary>
        /// <returns>连接是否成功</returns>
        public async Task<bool> ConnectAsync()
        {
            // 使用锁防止并发连接请求
            lock (_reconnectLock)
            {
                if (_isReconnecting)
                {
                    _logService.LogInfo("WebSocket正在重连中，请稍后再试", string.Empty, true);
                    return false;
                }
                
                if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
                {
                    _logService.LogInfo("WebSocket已连接", string.Empty, true);
                    return true;
                }
                
                _isReconnecting = true;
            }
            
            try
            {
                // 清理旧的WebSocket连接
                if (_webSocket != null)
                {
                    UnregisterWebSocketEvents();
                    _webSocket.Close();
                    _webSocket = null;
                }

                _webSocket = new WebSocket(_serverUrl);
                
                // 设置事件处理程序
                RegisterWebSocketEvents();

                // 连接到服务器
                _webSocket.Connect();

                // 等待连接完成
                var tcs = new TaskCompletionSource<bool>();
                EventHandler<bool> handler = null;
                handler = (s, connected) =>
                {
                    ConnectionStatusChanged -= handler;
                    tcs.TrySetResult(connected);
                    
                    // 连接成功，重置重连计数
                    if (connected)
                    {
                        _reconnectAttempts = 0;
                    }
                };

                ConnectionStatusChanged += handler;

                // 设置超时（增加到10秒，提高在网络不稳定情况下的成功率）
                var timeoutTask = Task.Delay(10000).ContinueWith(_ =>
                {
                    ConnectionStatusChanged -= handler;
                    tcs.TrySetResult(false);
                    return false;
                });

                var result = await Task.WhenAny(tcs.Task, timeoutTask) == tcs.Task
                    ? await tcs.Task
                    : false;
                
                if (!result)
                {
                    // 连接失败，尝试重连
                    TryReconnect();
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"连接WebSocket服务器时出错: {ex.Message}", string.Empty, true);
                IsConnected = false;
                
                // 发生异常，尝试重连
                TryReconnect();
                
                return false;
            }
            finally
            {
                lock (_reconnectLock)
                {
                    _isReconnecting = false;
                }
            }
        }

        /// <summary>
        /// 注册WebSocket事件处理程序
        /// </summary>
        private void RegisterWebSocketEvents()
        {
            if (_webSocket == null) return;
            
            _webSocket.OnOpen += WebSocket_OnOpen;
            _webSocket.OnClose += WebSocket_OnClose;
            _webSocket.OnError += WebSocket_OnError;
            _webSocket.OnMessage += WebSocket_OnMessage;
        }
        
        /// <summary>
        /// 注销WebSocket事件处理程序
        /// </summary>
        private void UnregisterWebSocketEvents()
        {
            if (_webSocket == null) return;
            
            _webSocket.OnOpen -= WebSocket_OnOpen;
            _webSocket.OnClose -= WebSocket_OnClose;
            _webSocket.OnError -= WebSocket_OnError;
            _webSocket.OnMessage -= WebSocket_OnMessage;
        }
        
        private void WebSocket_OnOpen(object sender, EventArgs e)
        {
            IsConnected = true;
            _logService.LogInfo("WebSocket连接成功", string.Empty, true);
        }
        
        private void WebSocket_OnClose(object sender, CloseEventArgs e)
        {
            IsConnected = false;
            _logService.LogWarning($"WebSocket连接关闭: {e.Reason}", string.Empty, true);
            
            // 非正常关闭时尝试重连
            if (e.Code != 1000) // 1000是正常关闭的代码
            {
                TryReconnect();
            }
        }
        
        private void WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            IsConnected = false;
            _logService.LogError($"WebSocket错误: {e.Message}", string.Empty, true);
            
            // 发生错误时尝试重连
            TryReconnect();
        }
        
        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Data != null)
            {
                try
                {
                    var response = JsonSerializer.Deserialize<WebSocketResponse>(e.Data);
                    MessageReceived?.Invoke(this, response);
                }
                catch (Exception ex)
                {
                    _logService.LogError($"解析WebSocket消息失败: {ex.Message}, 原始消息: {e.Data}", string.Empty, true);
                }
            }
        }
        
        /// <summary>
        /// 尝试重新连接WebSocket
        /// </summary>
        private void TryReconnect()
        {
            lock (_reconnectLock)
            {
                if (_isReconnecting || _reconnectAttempts >= _maxReconnectAttempts)
                {
                    return;
                }
                
                _isReconnecting = true;
                _reconnectAttempts++;
                
                // 使用指数退避策略计算延迟时间
                int delay = _reconnectDelayMs * (int)Math.Pow(2, _reconnectAttempts - 1);
                _logService.LogInfo($"尝试第 {_reconnectAttempts} 次重连，将在 {delay}ms 后重试", string.Empty, true);
                
                // 停止现有的重连定时器
                _reconnectTimer?.Dispose();
                
                // 创建新的重连定时器
                _reconnectTimer = new System.Threading.Timer(async _ =>
                {
                    try
                    {
                        _logService.LogInfo("执行重连...", string.Empty, true);
                        await ConnectAsync();
                    }
                    finally
                    {
                        lock (_reconnectLock)
                        {
                            _isReconnecting = false;
                        }
                    }
                }, null, delay, Timeout.Infinite);
            }
        }
        
        /// <summary>
        /// 断开WebSocket连接
        /// </summary>
        public void Disconnect()
        {
            try
            {
                // 停止重连尝试
                lock (_reconnectLock)
                {
                    _reconnectTimer?.Dispose();
                    _reconnectTimer = null;
                    _reconnectAttempts = 0;
                    _isReconnecting = false;
                }
                
                if (_webSocket != null)
                {
                    if (_webSocket.ReadyState == WebSocketState.Open)
                    {
                        _webSocket.Close(1000, "正常关闭");
                        _logService.LogInfo("WebSocket连接已正常关闭", string.Empty, true);
                    }
                    
                    UnregisterWebSocketEvents();
                    _webSocket = null;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"关闭WebSocket连接时出错: {ex.Message}", string.Empty, true);
            }
            finally
            {
                IsConnected = false;
            }
        }

        /// <summary>
        /// 发送消息到WebSocket服务器
        /// </summary>
        /// <param name="message">要发送的消息</param>
        /// <returns>发送是否成功</returns>
        public bool SendMessage(string message)
        {
            try
            {
                if (_webSocket == null || _webSocket.ReadyState != WebSocketState.Open)
                {
                    _logService.LogWarning("WebSocket未连接，尝试重新连接后发送消息", string.Empty, true);
                    
                    // 如果WebSocket未连接，尝试重新连接
                    Task.Run(async () =>
                    {
                        bool connected = await ConnectAsync();
                        if (connected)
                        {
                            // 连接成功后重新发送消息
                            _logService.LogInfo("WebSocket重新连接成功，重新发送消息", string.Empty, true);
                            return SendMessage(message);
                        }
                        else
                        {
                            _logService.LogWarning("WebSocket重新连接失败，无法发送消息", string.Empty, true);
                            return false;
                        }
                    });
                    
                    return false;
                }

                _webSocket.Send(message);
                _logService.LogInfo($"发送消息: {message}", string.Empty, true);
                return true;
            }
            catch (Exception ex)
            {
                _logService.LogError($"发送WebSocket消息时出错: {ex.Message}", string.Empty, true);
                IsConnected = false; // 发送失败时更新连接状态
                
                // 发生异常时尝试重连
                TryReconnect();
                
                return false;
            }
        }

        /// <summary>
        /// 获取所有设备信息
        /// </summary>
        /// <returns>是否成功发送请求</returns>
        public bool GetDeviceList()
        {
            var request = new { action = "list" };
            var json = JsonSerializer.Serialize(request);
            return SendMessage(json);
        }

        /// <summary>
        /// 截图请求
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="onlyDeviceName">是否只使用设备名作为文件名</param>
        /// <returns>是否成功发送请求</returns>
        public bool TakeScreenshot(string deviceIds, string savePath, bool onlyDeviceName = true)
        {
            var request = new
            {
                action = "screen",
                comm = new
                {
                    deviceIds,
                    savePath,
                    onlyDeviceName = onlyDeviceName ? 1 : 0
                }
            };

            var json = JsonSerializer.Serialize(request);
            return SendMessage(json);
        }

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
        public bool SendPointerEvent(string deviceIds, PointerEventType eventType, double x, double y, double endX = 0, double endY = 0, int delta = 0)
        {
            var request = new
            {
                action = "PointerEvent",
                comm = new
                {
                    deviceIds,
                    mask = ((int)eventType).ToString(),
                    x = x.ToString(),
                    y = y.ToString(),
                    endx = endX.ToString(),
                    endy = endY.ToString(),
                    delta = delta.ToString()
                }
            };

            var json = JsonSerializer.Serialize(request);
            return SendMessage(json);
        }
        
        /// <summary>
        /// 发送点击事件（按下+松开）
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="x">X坐标(0-1)</param>
        /// <param name="y">Y坐标(0-1)</param>
        /// <returns>是否成功发送请求</returns>
        public bool SendTapEvent(string deviceIds, double x, double y)
        {
            // 发送按下事件
            var downResult = SendPointerEvent(deviceIds, PointerEventType.Down, x, y);
            
            // 短暂延迟
            System.Threading.Thread.Sleep(50);
            
            // 发送松开事件
            var upResult = SendPointerEvent(deviceIds, PointerEventType.Up, x, y);
            
            return downResult && upResult;
        }
        
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
        public bool SendSwipeEvent(string deviceIds, PointerEventType eventType, double x, double y, double endX, double endY)
        {
            // 验证事件类型是否为滑动类型
            if (eventType != PointerEventType.SwipeUp && 
                eventType != PointerEventType.SwipeDown && 
                eventType != PointerEventType.SwipeLeft && 
                eventType != PointerEventType.SwipeRight)
            {
                throw new ArgumentException("事件类型必须是滑动类型", nameof(eventType));
            }
            
            return SendPointerEvent(deviceIds, eventType, x, y, endX, endY);
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                // 停止重连定时器
                _reconnectTimer?.Dispose();
                _reconnectTimer = null;
                
                // 断开连接并清理资源
                Disconnect();
                
                // 清除事件订阅
                ConnectionStatusChanged = null;
                MessageReceived = null;
            }
            catch (Exception ex)
            {
                _logService.LogError($"释放WebSocketService资源时出错: {ex.Message}", string.Empty, true);
            }
        }

        /// <summary>
        /// 获取剪切板内容
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>是否成功发送请求</returns>
        public bool GetClipboard(string deviceId)
        {
            var request = new
            {
                action = "getclipboard",
                comm = new
                {
                    deviceIds = deviceId
                }
            };

            var json = JsonSerializer.Serialize(request);
            return SendMessage(json);
        }

        /// <summary>
        /// 发送内容到剪切板
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="content">要发送的内容</param>
        /// <returns>是否成功发送请求</returns>
        public bool WriteClipboard(string deviceIds, string content)
        {
            var request = new
            {
                action = "writeclipboard",
                comm = new
                {
                    deviceIds,
                    content
                }
            };

            var json = JsonSerializer.Serialize(request);
            return SendMessage(json);
        }

        /// <summary>
        /// 执行ADB命令
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="command">要执行的ADB命令</param>
        /// <returns>是否成功发送请求</returns>
        public bool ExecuteAdbCommand(string deviceIds, string command)
        {
            var request = new
            {
                action = "adb",
                comm = new
                {
                    deviceIds,
                    command
                }
            };

            var json = JsonSerializer.Serialize(request);
            return SendMessage(json);
        }

        /// <summary>
        /// 执行批处理脚本
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="filePath">脚本文件路径</param>
        /// <returns>是否成功发送请求</returns>
        public bool ExecuteBatScript(string deviceIds, string filePath)
        {
            var request = new
            {
                action = "ExecuteBat",
                comm = new
                {
                    deviceIds,
                    filePath
                }
            };

            var json = JsonSerializer.Serialize(request);
            return SendMessage(json);
        }

        /// <summary>
        /// 停止批处理脚本
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="filePath">脚本文件路径</param>
        /// <returns>是否成功发送请求</returns>
        public bool StopBatScript(string deviceIds, string filePath)
        {
            var request = new
            {
                action = "StopBat",
                comm = new
                {
                    deviceIds,
                    filePath
                }
            };

            var json = JsonSerializer.Serialize(request);
            return SendMessage(json);
        }

        /// <summary>
        /// 执行AutoX.js脚本
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="filePath">脚本文件路径</param>
        /// <returns>是否成功发送请求</returns>
        public bool ExecuteAutoJsScript(string deviceIds, string filePath)
        {
            var request = new
            {
                action = "ExecuteAutoJs",
                comm = new
                {
                    deviceIds,
                    filePath
                }
            };

            var json = JsonSerializer.Serialize(request);
            return SendMessage(json);
        }

        /// <summary>
        /// 停止AutoX.js脚本
        /// </summary>
        /// <param name="deviceIds">设备ID列表，使用"all"表示所有设备</param>
        /// <param name="filePath">脚本文件路径</param>
        /// <returns>是否成功发送请求</returns>
        public bool StopAutoJsScript(string deviceIds, string filePath)
        {
            var request = new
            {
                action = "StopAutoJs",
                comm = new
                {
                    deviceIds,
                    filePath
                }
            };

            var json = JsonSerializer.Serialize(request);
            return SendMessage(json);
        }

        /// <summary>
        /// 打开独立窗口
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>是否成功发送请求</returns>
        public bool ShowSingleWindow(string deviceId)
        {
            var request = new
            {
                action = "showsinglewindow",
                comm = new
                {
                    deviceIds = deviceId
                }
            };

            var json = JsonSerializer.Serialize(request);
            return SendMessage(json);
        }
    }
}